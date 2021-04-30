/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace SlimSerializer.Core
{
  internal delegate void DynSerialize(TypeSchema schema, SlimWriter writer, TypeRegistry typeRegistry, RefPool refs, object instance, StreamingContext context, bool serializeForFramework);
  internal delegate void DynDeserialize(TypeSchema schema, SlimReader reader, TypeRegistry typeRegistry, RefPool refs, ref object instance, StreamingContext context);


  /// <summary>
  /// Type descriptor dynamically compiles serialization/deserialization expressions for a particular type
  /// </summary>
  internal class TypeDescriptor
  {
    public TypeDescriptor(TypeSchema schema, Type type)
    {
      if (Attribute.IsDefined(type, typeof(SlimSerializationProhibitedAttribute)))
        throw new SlimException(type);

      Schema = schema;
      Format = schema.Format;
      Type = type;

      IsPrimitive = Format.IsTypeSupported(type);
      IsArray = type.IsArray;
      IsPrimitiveArray = IsArray && IsPrimitive;

      if (!IsArray)
        Fields = SerializationUtils.GetSerializableFields(type).ToArray();


      //If type implements ISerializable than that should be used instead of dyn methods
      if (type.GetInterfaces().Contains(typeof(ISerializable)))
      {
        CustomIsSerializable = true;
      }
      else
      {
        m_Serialize = MakeSerialize();
        m_Deserialize = MakeDeserialize();
      }

      //Query for "On..." family of attributes
      m_MethodsOnSerializing = FindAttributedMethods(typeof(OnSerializingAttribute));
      m_MethodsOnSerialized = FindAttributedMethods(typeof(OnSerializedAttribute));
      m_MethodsOnDeserializing = FindAttributedMethods(typeof(OnDeserializingAttribute));
      m_MethodsOnDeserialized = FindAttributedMethods(typeof(OnDeserializedAttribute));

    }

    public readonly TypeSchema Schema;
    public readonly SlimFormat Format;
    public readonly Type Type;
    public readonly FieldInfo[] Fields;
    public readonly bool CustomIsSerializable;
    public readonly bool IsPrimitive;
    public readonly bool IsArray;
    public readonly bool IsPrimitiveArray;

    private readonly DynSerialize m_Serialize;
    private readonly DynDeserialize m_Deserialize;


    private readonly List<MethodInfo> m_MethodsOnSerializing;
    private readonly List<MethodInfo> m_MethodsOnSerialized;
    private readonly List<MethodInfo> m_MethodsOnDeserializing;
    private readonly List<MethodInfo> m_MethodsOnDeserialized;

    public void SerializeInstance(SlimWriter writer,
                                  TypeRegistry registry,
                                  RefPool refs,
                                  object instance,
                                  StreamingContext streamingContext,
                                  bool serializationForFrameWork)
    {
      if (m_MethodsOnSerializing != null)
        InvokeAttributedMethods(m_MethodsOnSerializing, instance, streamingContext);

      if (m_Serialize != null)
      {
        m_Serialize(Schema, writer, registry, refs, instance, streamingContext, serializationForFrameWork);
      }
      else
      {
        var isz = instance as ISerializable;
        var info = new SerializationInfo(Type, new FormatterConverter());
        isz.GetObjectData(info, streamingContext);

        SerializeInfo(writer, registry, refs, info, streamingContext, serializationForFrameWork);
      }

      if (m_MethodsOnSerialized != null)
        InvokeAttributedMethods(m_MethodsOnSerialized, instance, streamingContext);
    }

    public void DeserializeInstance(SlimReader reader, TypeRegistry registry, RefPool refs, ref object instance, StreamingContext streamingContext)
    {
      if (m_MethodsOnDeserializing != null)
        InvokeAttributedMethods(m_MethodsOnDeserializing, instance, streamingContext);

      if (m_Deserialize != null)
      {
        m_Deserialize(Schema, reader, registry, refs, ref instance, streamingContext);
      }
      else
      {
        var info = DeserializeInfo(reader, registry, refs, streamingContext);

        //20171223 DKh Handle SerializationInfo.SetType() redefinition of serialized type
        if (instance.GetType() != info.ObjectType)
        {
          instance = FormatterServices.GetUninitializedObject(info.ObjectType);//ref instance is re-allocated with a different type
        }

        refs.AddISerializableFixup(instance, info);
      }

      if (m_MethodsOnDeserialized != null)
      {
        refs.AddOnDeserializedCallback(instance, this);
      }
    }


    public void InvokeOnDeserializedCallbak(object instance, StreamingContext streamingContext)
    {
      if (m_MethodsOnDeserialized != null)
        InvokeAttributedMethods(m_MethodsOnDeserialized, instance, streamingContext);
    }


    private static void InvokeAttributedMethods(List<MethodInfo> methods, object instance, StreamingContext streamingContext)
    {
      //20130820 DKh refactored into common code
      SerializationUtils.InvokeSerializationAttributedMethods(methods, instance, streamingContext);
    }

    private List<MethodInfo> FindAttributedMethods(Type type)
    {
      //20130820 DKh refactored into common code
      return SerializationUtils.FindSerializationAttributedMethods(Type, type);
    }

    private DynSerialize MakeSerialize()
    {
      var walkArrayWrite = typeof(TypeDescriptor).GetMethod(nameof(TypeDescriptor.WalkArrayWrite), BindingFlags.NonPublic | BindingFlags.Static);

      var pSchema = Expression.Parameter(typeof(TypeSchema));
      var pWriter = Expression.Parameter(typeof(SlimWriter));
      var pTReg = Expression.Parameter(typeof(TypeRegistry));
      var pRefs = Expression.Parameter(typeof(RefPool));
      var pInstance = Expression.Parameter(typeof(object));
      var pStreamingContext = Expression.Parameter(typeof(StreamingContext));
      var pSerializeForFramework = Expression.Parameter(typeof(bool));

      var expressions = new List<Expression>();

      var instance = Expression.Variable(Type, "instance");

      expressions.Add(Expression.Assign(instance, Expression.Convert(pInstance, Type)));


      if (IsPrimitive)
      {
        expressions.Add(Expression.Call(pWriter,
                                Format.GetWriteMethodForType(Type),
                                instance));
      }
      else if (IsArray)
      {
        var elementType = Type.GetElementType();

        if (Format.IsTypeSupported(elementType))//array element type
        {  //spool whole array into writer using primitive types

          var pElement = Expression.Parameter(typeof(object));
          expressions.Add(Expression.Call(walkArrayWrite,
                                            instance,
                                            Expression.Lambda(Expression.Call(pWriter,
                                                                            Format.GetWriteMethodForType(elementType),
                                                                            Expression.Convert(pElement, elementType)), pElement)
                                          )
            );
        }
        else
        {  //spool whole array using TypeSchema because objects may change type
          var pElement = Expression.Parameter(typeof(object));

          if (!elementType.IsValueType)//reference type
            expressions.Add(Expression.Call(walkArrayWrite,
                                            instance,
                                            Expression.Lambda(
                                              Expression.Call(
                                                pSchema,
                                                typeof(TypeSchema).GetMethod(nameof(TypeSchema.WriteRefMetaHandle)),
                                                pWriter,
                                                pTReg,
                                                pRefs,
                                                Expression.Convert(pElement, typeof(object)),
                                                pStreamingContext, 
                                                pSerializeForFramework),
                                              pElement)
                                            )
                          );
          else
            expressions.Add(Expression.Call(walkArrayWrite,
                                            instance,
                                            Expression.Lambda(Expression.Call(pSchema,
                                                              typeof(TypeSchema).GetMethod(nameof(TypeSchema.Serialize)),
                                                              pWriter,
                                                              pTReg,
                                                              pRefs,
                                                              pElement,
                                                              pStreamingContext,
                                                              pSerializeForFramework,
                                                              Expression.Constant(elementType)//valueType
                                                              ), pElement)
                                    )
            );
        }
      }
      else
      {
        foreach (var field in Fields)
        {
          Expression expr = null;
          var t = field.FieldType;

          if (Format.IsTypeSupported(t))
          {
            expr = Expression.Call(pWriter,
                                    Format.GetWriteMethodForType(t),
                                    Expression.Field(instance, field));
          }
          else
          if (t.IsEnum)
          {
            expr = Expression.Call(pWriter,
                                    Format.GetWriteMethodForType(typeof(int)),
                                    Expression.Convert(Expression.Field(instance, field), typeof(int)));

          }
          else // complex type ->  struct or reference
          {
            if (!t.IsValueType)//reference type -> write metahandle
            {
              expr = Expression.Call(pSchema,
                                        typeof(TypeSchema).GetMethod(nameof(TypeSchema.WriteRefMetaHandle)),
                                        pWriter,
                                        pTReg,
                                        pRefs,
                                        Expression.Convert(Expression.Field(instance, field), typeof(object)),
                                        pStreamingContext,
                                        pSerializeForFramework);
            }
            else
              expr = Expression.Call(pSchema,
                                      typeof(TypeSchema).GetMethod(nameof(TypeSchema.Serialize)),
                                      pWriter,
                                      pTReg,
                                      pRefs,
                                      Expression.Convert(Expression.Field(instance, field), typeof(object)),
                                      pStreamingContext,
                                      pSerializeForFramework,
                                      Expression.Constant(field.FieldType));//valueType

          }

          expressions.Add(expr);
        }//foreach
      }

      var body = Expression.Block(new ParameterExpression[] { instance }, expressions);

      return Expression.Lambda<DynSerialize>(body, pSchema, pWriter, pTReg, pRefs, pInstance, pStreamingContext, pSerializeForFramework).Compile();
    }

    private void SerializeInfo(SlimWriter writer, TypeRegistry registry, RefPool refs, SerializationInfo info, StreamingContext streamingContext, bool serializationForFrameWork)
    {
      writer.Write(registry.GetTypeHandle(info.ObjectType, serializationForFrameWork));//20171223 DKh
      writer.Write(info.MemberCount);

      var senum = info.GetEnumerator();
      while (senum.MoveNext())
      {
        writer.Write(senum.Name);
        writer.Write(registry.GetTypeHandle(senum.ObjectType, serializationForFrameWork));
        Schema.Serialize(writer, registry, refs, senum.Value, streamingContext, serializationForFrameWork);
      }
    }

    private SerializationInfo DeserializeInfo(SlimReader reader, TypeRegistry registry, RefPool refs, StreamingContext streamingContext)
    {
      //20171223 DKh
      var visInfo = reader.ReadVarIntStr();
      var tInfo = registry[visInfo];
      var info = new SerializationInfo(tInfo, new FormatterConverter());

      //20171223 DKh
      //var info = new SerializationInfo(Type, new FormatterConverter());

      var cnt = reader.ReadInt();

      for (var i = 0; i < cnt; i++)
      {
        var name = reader.ReadString();

        var vis = reader.ReadVarIntStr();
        var type = registry[vis];
        var obj = Schema.Deserialize(reader, registry, refs, streamingContext);

        info.AddValue(name, obj, type);
      }

      return info;
    }


    //20130816 DKh refactored into SerializationUtils
    private static void WalkArrayWrite(Array arr, Action<object> each) => SerializationUtils.WalkArrayWrite(arr, each);
    private static void WalkArrayRead<T>(Array arr, Func<T> each) => SerializationUtils.WalkArrayRead<T>(arr, each);


    private DynDeserialize MakeDeserialize()
    {
      var pSchema = Expression.Parameter(typeof(TypeSchema));
      var pReader = Expression.Parameter(typeof(SlimReader));
      var pTReg = Expression.Parameter(typeof(TypeRegistry));
      var pRefs = Expression.Parameter(typeof(RefPool));
      var pInstance = Expression.Parameter(typeof(object).MakeByRefType());
      var pStreamingContext = Expression.Parameter(typeof(StreamingContext));

      var expressions = new List<Expression>();

      var instance = Expression.Variable(Type, "instance");

      expressions.Add(Expression.Assign(instance, Expression.Convert(pInstance, Type)));


      if (IsPrimitive)
      {
        expressions.Add(Expression.Assign
                                  (instance, Expression.Call(pReader, Format.GetReadMethodForType(Type)))
                        );

      }
      else if (IsArray)
      {
        var elmType = Type.GetElementType();
        var walkArrayRead = typeof(TypeDescriptor).GetMethod(nameof(TypeDescriptor.WalkArrayRead), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(elmType);

        if (Format.IsTypeSupported(elmType))//array element type
        {
          //spool whole array from reader using primitive types
          expressions.Add(Expression.Call(walkArrayRead,
                                            instance,
                                            Expression.Lambda(Expression.Call(pReader, Format.GetReadMethodForType(elmType)))
                                          )
            );
        }
        else
        {  //spool whole array using TypeSchema because objects may change type
          if (!elmType.IsValueType)//reference type
            expressions.Add(Expression.Call(walkArrayRead,
                                            instance,
                                            Expression.Lambda(
                                                            Expression.Convert(
                                                                              Expression.Call(pSchema,
                                                                                          typeof(TypeSchema).GetMethod("readRefMetaHandle"),
                                                                                          pReader,
                                                                                          pTReg,
                                                                                          pRefs,
                                                                                          pStreamingContext),
                                                                              elmType
                                                                              )
                                                            )
                                          )
                          );
          else
            expressions.Add(Expression.Call(walkArrayRead,
                                            instance,
                                            Expression.Lambda(
                                                            Expression.Convert(
                                                                              Expression.Call(pSchema,
                                                                                      typeof(TypeSchema).GetMethod("Deserialize"),
                                                                                      pReader,
                                                                                      pTReg,
                                                                                      pRefs,
                                                                                      pStreamingContext,
                                                                                      Expression.Constant(elmType)),//valueType
                                                                            elmType
                                                                            )
                                                              )
                                          )
                          );
        }
      }
      else//loop through fields
      {
        foreach (var field in Fields)
        {
          var t = field.FieldType;

          Expression assignmentTargetExpression;
          if (field.IsInitOnly)//readonly fields must be assigned using reflection
          {
            assignmentTargetExpression = Expression.Variable(t, "readonlyFieldValue");
          }
          else
            assignmentTargetExpression = Expression.Field(instance, field);


          Expression expr;
          if (Format.IsTypeSupported(t))
          {
            expr = Expression.Assign(
                                        assignmentTargetExpression,
                                        Expression.Call(pReader, Format.GetReadMethodForType(t))
                                      );
          }
          else
          if (t.IsEnum)
          {
            expr = Expression.Assign(
                                        assignmentTargetExpression,
                                        Expression.Convert(
                                              Expression.Call(pReader, Format.GetReadMethodForType(typeof(int))),
                                              field.FieldType
                                              )
                                      );
          }
          else // complex type ->  struct or reference
          {
            if (!t.IsValueType)//reference type -> read metahandle
            {
              expr = Expression.Assign(
                                        assignmentTargetExpression,

                                        Expression.Convert(
                                                Expression.Call(pSchema,
                                                            typeof(TypeSchema).GetMethod(nameof(TypeSchema.ReadRefMetaHandle)),
                                                            pReader,
                                                            pTReg,
                                                            pRefs,
                                                            pStreamingContext),
                                            field.FieldType)
                                        );
            }
            else
            {
              expr = Expression.Assign(
                                        assignmentTargetExpression,
                                        Expression.Convert(
                                                Expression.Call(pSchema,
                                                                typeof(TypeSchema).GetMethod(nameof(TypeSchema.Deserialize)),
                                                                pReader,
                                                                pTReg,
                                                                pRefs,
                                                                pStreamingContext,
                                                                Expression.Constant(field.FieldType)),//valueType
                                                field.FieldType)
                                        );
            }
          }

          if (assignmentTargetExpression is ParameterExpression expression)//readonly fields
          {
            if (Type.IsValueType)//20150405DKh added
            {
              var vBoxed = Expression.Variable(typeof(object), "vBoxed");
              var box = Expression.Assign(vBoxed, Expression.TypeAs(instance, typeof(object)));//box the value type
              var setField = Expression.Call(Expression.Constant(field),
                                                  typeof(FieldInfo).GetMethod("SetValue", new Type[] { typeof(object), typeof(object) }),
                                                  vBoxed, //on boxed struct
                                                  Expression.Convert(assignmentTargetExpression, typeof(object))
                                            );
              var swap = Expression.Assign(instance, Expression.Unbox(vBoxed, Type));
              expressions.Add(
                  Expression.Block
                  (new ParameterExpression[] { expression, vBoxed },
                    box,
                    expr,
                    setField,
                    swap
                  )
              );
            }
            else
            {

              var setField = Expression.Call(Expression.Constant(field),
                                                  typeof(FieldInfo).GetMethod("SetValue", new Type[] { typeof(object), typeof(object) }),
                                                  instance,
                                                  Expression.Convert(assignmentTargetExpression, typeof(object))
                                            );
              expressions.Add(Expression.Block(new ParameterExpression[] { expression }, expr, setField));
            }
          }
          else
            expressions.Add(expr);
        }//foreach
      }//loop through fields


      expressions.Add(Expression.Assign(pInstance, Expression.Convert(instance, typeof(object))));

      var body = Expression.Block(new ParameterExpression[] { instance }, expressions);
      return Expression.Lambda<DynDeserialize>(body, pSchema, pReader, pTReg, pRefs, pInstance, pStreamingContext).Compile();
    }
  }//TypeDef


  internal class TypeSchema
  {
    private readonly ConcurrentDictionary<Type, TypeDescriptor> m_Dict2 = new ConcurrentDictionary<Type, TypeDescriptor>();
    private TypeDescriptor GetTypeDescriptorCachedOrMake(Type tp) => m_Dict2.GetOrAdd(tp, t => new TypeDescriptor(this, t));

    public TypeSchema(SlimFormat format)
    {
      Format = format;
    }

    public SlimFormat Format { get; }

    public void Serialize(SlimWriter writer, TypeRegistry registry, RefPool refs, object instance, StreamingContext streamingContext, bool serializationForFrameWork, Type type = null)
    {
      var typeHandle = new VarIntStr(0);

      if (type == null)
      {
        if (instance == null)
        {
          writer.Write(TypeRegistry.NullHandle);//object type null
          return;
        }

        type = instance.GetType();

        //Write type name. Full or compressed. Full Type names are assembly-qualified strings, compressed are string in form of
        // $<name_table_index> i.e.  $1 <--- get string[1]
        typeHandle = registry.GetTypeHandle(type, serializationForFrameWork);
        writer.Write(typeHandle);
      }

      //we get here if we have a boxed value of directly-handled type
      var wa = Format.GetWriteActionForType(type) ?? Format.GetWriteActionForRefType(type);//20150503 DKh fixed root byte[] slow
      if (wa != null)
      {
        wa(writer, instance);
        return;
      }

      var td = GetTypeDescriptorCachedOrMake(type);

      if (td.IsArray) //need to write array dimensions
      {
        writer.Write(Arrays.ArrayToDescriptor((Array)instance, type, typeHandle));
      }

      td.SerializeInstance(writer, registry, refs, instance, streamingContext, serializationForFrameWork);
    }


    public void WriteRefMetaHandle(SlimWriter writer, TypeRegistry registry, RefPool refs, object instance, StreamingContext streamingContext, bool serializationForFrameWork)
    {

      var mh = refs.GetHandle(instance, registry, Format, out var tInstance, serializationForFrameWork);
      writer.Write(mh);

      if (mh.IsInlinedValueType)
      {
        var wa = Format.GetWriteActionForType(tInstance);
        if (wa != null)
          wa(writer, instance);
        else
          Serialize(writer, registry, refs, instance, streamingContext, serializationForFrameWork);
      }
      else
      if (mh.IsInlinedRefType)
      {
        var wa = Format.GetWriteActionForRefType(tInstance);
        if (wa != null)
          wa(writer, instance);
        else
          throw new SlimException("Internal error writeRefMetaHandle: no write action for ref type, but ref mhandle is inlined");
      }
    }

    public object ReadRefMetaHandle(SlimReader reader, TypeRegistry registry, RefPool refs, StreamingContext streamingContext)
    {
      var mh = reader.ReadMetaHandle();

      if (mh.IsInlinedValueType)
      {
        var tboxed = registry[mh.Metadata.Value];//adding this type to registry if it is not there yet

        var ra = Format.GetReadActionForType(tboxed);
        if (ra != null)
          return ra(reader);
        else
          return Deserialize(reader, registry, refs, streamingContext);
      }

      return refs.HandleToReference(mh, registry, Format, reader);
    }

    public object Deserialize(SlimReader reader, TypeRegistry registry, RefPool refs, StreamingContext streamingContext, Type valueType = null)
    {
      return DeserializeRootOrInner(reader, registry, refs, streamingContext, false, valueType);
    }

    public object DeserializeRootOrInner(SlimReader reader, TypeRegistry registry, RefPool refs, StreamingContext streamingContext, bool root, Type valueType = null)
    {
      var type = valueType;
      if (type == null)
      {
        var tHandle = reader.ReadVarIntStr();
        if (tHandle.StringValue != null)//need to search for possible array descriptor
        {
          var ip = tHandle.StringValue.IndexOf('|');//array descriptor start
          if (ip > 0)
          {
            var tname = tHandle.StringValue.Substring(0, ip);
            if (TypeRegistry.IsNullHandle(tname)) return null;
            type = registry[tname];
          }
          else
          {
            if (TypeRegistry.IsNullHandle(tHandle)) return null;
            type = registry[tHandle];
          }
        }
        else
        {
          if (TypeRegistry.IsNullHandle(tHandle)) return null;
          type = registry[tHandle];
        }
      }

      //we get here if we have a boxed value of directly-handled type
      var ra = Format.GetReadActionForType(type) ?? Format.GetReadActionForRefType(type);//20150503 DKh fixed root byte[] slow
      if (ra != null)
        return ra(reader);


      var td = GetTypeDescriptorCachedOrMake(type);

      object instance;
      if (td.IsArray)
        instance = Arrays.DescriptorToArray(reader.ReadString(), type);
      else
        instance = SerializationUtils.MakeNewObjectInstance(type);

      if (root)
        if (!type.IsValueType)//if this is a reference type
        {
          refs.Add(instance);
        }

      td.DeserializeInstance(reader, registry, refs, ref instance, streamingContext);


      return instance;
    }


    public void DeserializeRefTypeInstance(object instance, SlimReader reader, TypeRegistry registry, RefPool refs, StreamingContext streamingContext)
    {
      if (instance == null) throw new SlimException("DeserRefType(null)");

      var type = instance.GetType();

      reader.ReadVarIntStr();//skip type as we already know it from prior-allocated metahandle


      var td = GetTypeDescriptorCachedOrMake(type);

      if (type.IsArray)
        reader.ReadString();//skip array descriptor as we already know it from prior-allocated metahandle

      td.DeserializeInstance(reader, registry, refs, ref instance, streamingContext);
    }

  }

}