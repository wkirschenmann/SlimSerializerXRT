using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Slim.Core
{
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
        Serialize = MakeSerialize();
        Deserialize = MakeDeserialize();
      }

      //Query for "On..." family of attributes
      MethodsOnSerializing = FindAttributedMethods(typeof(OnSerializingAttribute));
      MethodsOnSerialized = FindAttributedMethods(typeof(OnSerializedAttribute));
      MethodsOnDeserializing = FindAttributedMethods(typeof(OnDeserializingAttribute));
      MethodsOnDeserialized = FindAttributedMethods(typeof(OnDeserializedAttribute));

    }

    public readonly TypeSchema Schema;
    public SlimFormat Format { get; }
    public readonly Type Type;
    public readonly FieldInfo[] Fields;
    public readonly bool CustomIsSerializable;
    public readonly bool IsPrimitive;
    public readonly bool IsArray;
    public readonly bool IsPrimitiveArray;

    private DynSerialize Serialize { get; }
    private DynDeserialize Deserialize { get; }


    private List<MethodInfo> MethodsOnSerializing { get; }
    private List<MethodInfo> MethodsOnSerialized { get; }
    private List<MethodInfo> MethodsOnDeserializing { get; }
    private List<MethodInfo> MethodsOnDeserialized { get; }


    public void SerializeInstance(SlimWriter writer, TypeRegistry registry, RefPool refs, object instance, StreamingContext streamingContext, bool serializationForFrameWork = false)
    {
      if (MethodsOnSerializing != null)
        InvokeAttributedMethods(MethodsOnSerializing, instance, streamingContext);

      if (Serialize != null)
      {
        Serialize(Schema, writer, registry, refs, instance, streamingContext, serializationForFrameWork);
      }
      else
      {
        var isz = instance as ISerializable;
        var info = new SerializationInfo(Type, new FormatterConverter());
        isz.GetObjectData(info, streamingContext);

        SerializeInfo(writer, registry, refs, info, streamingContext, serializationForFrameWork);
      }

      if (MethodsOnSerialized != null)
        InvokeAttributedMethods(MethodsOnSerialized, instance, streamingContext);
    }

    public void DeserializeInstance(SlimReader reader, TypeRegistry registry, RefPool refs, ref object instance, StreamingContext streamingContext)
    {
      if (MethodsOnDeserializing != null)
        InvokeAttributedMethods(MethodsOnDeserializing, instance, streamingContext);

      if (Deserialize != null)
      {
        Deserialize(Schema, reader, registry, refs, ref instance, streamingContext);
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

      if (MethodsOnDeserialized != null)
      {
        refs.AddOnDeserializedCallback(instance, this);
      }
    }


    public void InvokeOnDeserializedCallback(object instance, StreamingContext streamingContext)
    {
      if (MethodsOnDeserialized != null)
        InvokeAttributedMethods(MethodsOnDeserialized, instance, streamingContext);
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
      var walkArrayWrite = typeof(TypeDescriptor).GetMethod(nameof(WalkArrayWrite), BindingFlags.NonPublic | BindingFlags.Static);

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
        var elmType = Type.GetElementType();

        if (Format.IsTypeSupported(elmType))//array element type
        {  //spool whole array into writer using primitive types

          var pElement = Expression.Parameter(typeof(object));
          expressions.Add(Expression.Call(walkArrayWrite,
              instance,
              Expression.Lambda(Expression.Call(pWriter,
                Format.GetWriteMethodForType(elmType),
                Expression.Convert(pElement, elmType)), pElement)
            )
          );
        }
        else
        {  //spool whole array using TypeSchema because objects may change type
          var pElement = Expression.Parameter(typeof(object));

          if (!elmType.IsValueType)//reference type
            expressions.Add(Expression.Call(walkArrayWrite,
                instance,
                Expression.Lambda(Expression.Call(pSchema,
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
                  Expression.Constant(elmType)//valueType
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
      var tInfo = registry.GetOrAddType(visInfo);
      var info = new SerializationInfo(tInfo, new FormatterConverter());

      //20171223 DKh
      //var info = new SerializationInfo(Type, new FormatterConverter());

      var cnt = reader.ReadInt();

      for (var i = 0; i < cnt; i++)
      {
        var name = reader.ReadString();

        var vis = reader.ReadVarIntStr();
        var type = registry.GetOrAddType(vis);
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
        var walkArrayRead = typeof(TypeDescriptor).GetMethod(nameof(WalkArrayRead), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(elmType);

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
                      typeof(TypeSchema).GetMethod(nameof(TypeSchema.ReadRefMetaHandle)),
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
                      typeof(TypeSchema).GetMethod(nameof(TypeSchema.Deserialize)),
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
          Expression expr = null;
          var t = field.FieldType;

          Expression assignmentTargetExpression;
          if (field.IsInitOnly)//readonly fields must be assigned using reflection
          {
            assignmentTargetExpression = Expression.Variable(t, "readonlyFieldValue");
          }
          else
            assignmentTargetExpression = Expression.Field(instance, field);


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
                typeof(FieldInfo).GetMethod(nameof(FieldInfo.SetValue), new [] { typeof(object), typeof(object) }),
                vBoxed, //on boxed struct
                Expression.Convert(assignmentTargetExpression, typeof(object))
              );
              var swap = Expression.Assign(instance, Expression.Unbox(vBoxed, Type));
              expressions.Add(
                Expression.Block
                (new[] { expression, vBoxed },
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
                typeof(FieldInfo).GetMethod(nameof(FieldInfo.SetValue), new Type[] { typeof(object), typeof(object) }),
                instance,
                Expression.Convert(assignmentTargetExpression, typeof(object))
              );
              expressions.Add(Expression.Block(new [] { expression }, expr, setField));
            }
          }
          else
            expressions.Add(expr);
        }//foreach
      }//loop through fields


      expressions.Add(Expression.Assign(pInstance, Expression.Convert(instance, typeof(object))));

      var body = Expression.Block(new [] { instance }, expressions);
      return Expression.Lambda<DynDeserialize>(body, pSchema, pReader, pTReg, pRefs, pInstance, pStreamingContext).Compile();
    }
  }
}