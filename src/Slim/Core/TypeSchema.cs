/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace Slim.Core
{
  internal delegate void DynSerialize(TypeSchema schema, SlimWriter writer, TypeRegistry typeRegistry, RefPool refs, object instance, StreamingContext context, bool serializeForFramework);
  internal delegate void DynDeserialize(TypeSchema schema, SlimReader reader, TypeRegistry typeRegistry, RefPool refs, ref object instance, StreamingContext context);


  internal class TypeSchema
  {
    private ConcurrentDictionary<Type, TypeDescriptor> m_Dict2 = new ConcurrentDictionary<Type, TypeDescriptor>();
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

      TypeDescriptor td = GetTypeDescriptorCachedOrMake(type);

      if (td.IsArray) //need to write array dimensions
      {
        writer.Write(Arrays.ArrayToDescriptor((Array)instance, type, typeHandle));
      }
      
//#if !NETFRAMEWORK
      if (type.IsGenericType && 
          type.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
          type.GetGenericArguments()[0] == typeof(string))
      {
        var field = type.GetField("_comparer", BindingFlags.Instance | BindingFlags.NonPublic);
        var comparer = field?.GetValue(instance);
        if (comparer != null && comparer.GetType().Name == "NonRandomizedStringEqualityComparer")
        {
          var entries = type.GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
          var length = entries.GetType().GetProperty("Length").GetValue(entries);
          var resize = type.GetMethod("Resize",
                                               BindingFlags.Instance | BindingFlags.NonPublic,
                                               null,
                                               new[] {typeof(Int32), typeof(Boolean)},
                                               new ParameterModifier[] { });

          field.SetValue(instance, EqualityComparer<string>.Default);
          resize.Invoke(instance, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] {length, true},
            CultureInfo.InvariantCulture);
        }
      }
//#endif // !NETFRAMEWORK

      td.SerializeInstance(writer, registry, refs, instance, streamingContext, serializationForFrameWork);
    }


    public void WriteRefMetaHandle(SlimWriter writer, TypeRegistry registry, RefPool refs, object instance, StreamingContext streamingContext, bool serializationForFrameWork)
    {

      var mh = refs.GetHandle(instance, registry, Format, out Type tInstance, serializationForFrameWork);
      writer.Write(mh);

      if (mh.IsInlinedValueType)
      {
        var wa = Format.GetWriteActionForType(tInstance);
        if (wa != null)
          wa(writer, instance);
        else
          this.Serialize(writer, registry, refs, instance, streamingContext, serializationForFrameWork);
      }
      else
      if (mh.IsInlinedRefType)
      {
        var wa = Format.GetWriteActionForRefType(tInstance);
        if (wa != null)
          wa(writer, instance);
        else
          throw new SlimException($"Internal error {nameof(WriteRefMetaHandle)}: no write action for ref type, but ref mhandle is inlined");
      }
    }

    public object ReadRefMetaHandle(SlimReader reader, TypeRegistry registry, RefPool refs, StreamingContext streamingContext)
    {
      var mh = reader.ReadMetaHandle();

      if (mh.IsInlinedValueType)
      {
        var tboxed = registry.GetOrAddType(mh.Metadata.Value);//adding this type to registry if it is not there yet

        var ra = Format.GetReadActionForType(tboxed);
        if (ra != null)
          return ra(reader);
        else
          return this.Deserialize(reader, registry, refs, streamingContext);
      }

      return refs.HandleToReference(mh, registry, Format, reader);
    }

    public object Deserialize(SlimReader reader, TypeRegistry registry, RefPool refs, StreamingContext streamingContext, Type valueType = null)
    {
      return DeserializeRootOrInner(reader, registry, refs, streamingContext, false, valueType);
    }

    public object DeserializeRootOrInner(SlimReader reader, TypeRegistry registry, RefPool refs, StreamingContext streamingContext, bool root, Type valueType = null)
    {
      Type type = valueType;
      if (type == null)
      {
        var thandle = reader.ReadVarIntStr();
        if (thandle.StringValue != null)//need to search for possible array descriptor
        {
          var ip = thandle.StringValue.IndexOf('|');//array descriptor start
          if (ip > 0)
          {
            var typeName = thandle.StringValue.Substring(0, ip);
            if (TypeRegistry.IsNullHandle(typeName)) return null;
            type = registry[typeName];
          }
          else
          {
            if (TypeRegistry.IsNullHandle(thandle)) return null;
            type = registry.GetOrAddType(thandle);
          }
        }
        else
        {
          if (TypeRegistry.IsNullHandle(thandle)) return null;
          type = registry.GetOrAddType(thandle);
        }
      }

      //we get here if we have a boxed value of directly-handled type
      var ra = Format.GetReadActionForType(type) ?? Format.GetReadActionForRefType(type);//20150503 DKh fixed root byte[] slow
      if (ra != null)
        return ra(reader);


      TypeDescriptor td = GetTypeDescriptorCachedOrMake(type);

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


      TypeDescriptor td = GetTypeDescriptorCachedOrMake(type);

      if (type.IsArray)
        reader.ReadString();//skip array descriptor as we already know it from prior-allocated metahandle

      td.DeserializeInstance(reader, registry, refs, ref instance, streamingContext);
    }

  }

}
