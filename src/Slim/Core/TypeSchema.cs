/*<FILE_LICENSE>
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
    private readonly ConcurrentDictionary<Type, TypeDescriptor> m_Dict2 = new ConcurrentDictionary<Type, TypeDescriptor>();
    private TypeDescriptor GetTypeDescriptorCachedOrMake(Type tp) => m_Dict2.GetOrAdd(tp, t => new TypeDescriptor(this, t));

    public TypeSchema(SlimFormat format)
    {
      Format = format;
    }

    public SlimFormat Format { get; }

    public void Serialize(SlimWriter writer, TypeRegistry registry, RefPool refs, object instance,
      StreamingContext streamingContext, bool serializationForFrameWork, Type type = null)
    {
      var typeHandle = new VarIntStr(0);

      if (type == null)
      {
        if (instance == null)
        {
          writer.Write(TypeRegistry.NullHandle); //object type null
          return;
        }

        type = instance.GetType();

        //Write type name. Full or compressed. Full Type names are assembly-qualified strings, compressed are string in form of
        // $<name_table_index> i.e.  $1 <--- get string[1]
        typeHandle = registry.GetTypeHandle(type, serializationForFrameWork);
        writer.Write(typeHandle);
      }

      //we get here if we have a boxed value of directly-handled type
      var wa = Format.GetWriteActionForType(type) ??
               Format.GetWriteActionForRefType(type); //20150503 DKh fixed root byte[] slow
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


      {
#warning This code block is meant to compensate discrepancies between net452 and coreclr behavior with Dictionary<,>. This code needs to be removed when removing net 451 support 
        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
            type.GetGenericArguments()[0] == typeof(string))
        {
          var field = type.GetField("_comparer", BindingFlags.Instance | BindingFlags.NonPublic);
          var comparer = field?.GetValue(instance);
          if (comparer != null && comparer.GetType().Name == "NonRandomizedStringEqualityComparer")
          {
            field.SetValue(instance, EqualityComparer<string>.Default);
            var entries = type.GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
            if (entries != null)
            {
              var length = entries.GetType().GetProperty("Length").GetValue(entries);
              var resize = type.GetMethod("Resize",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(int), typeof(bool) },
                new ParameterModifier[] { });
              resize.Invoke(instance, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { length, true },
                CultureInfo.InvariantCulture);
            }
          }
        }
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
          throw new SlimException($"Internal error {nameof(WriteRefMetaHandle)}: no write action for ref type, but ref metaHandle is inlined");
      }
    }

    public object ReadRefMetaHandle(SlimReader reader, TypeRegistry registry, RefPool refs, StreamingContext streamingContext)
    {
      var metaHandle = reader.ReadMetaHandle();

      if (metaHandle.IsInlinedValueType)
      {
        // ReSharper disable once PossibleInvalidOperationException
        var typeBoxed = registry.GetOrAddType(metaHandle.Metadata.Value);//adding this type to registry if it is not there yet

        var ra = Format.GetReadActionForType(typeBoxed);
        return ra != null ? ra(reader) : Deserialize(reader, registry, refs, streamingContext);
      }

      return refs.HandleToReference(metaHandle, registry, Format, reader);
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
        var typeHandle = reader.ReadVarIntStr();
        if (typeHandle.StringValue != null)//need to search for possible array descriptor
        {
          var ip = typeHandle.StringValue.IndexOf('|');//array descriptor start
          if (ip > 0)
          {
            var typeName = typeHandle.StringValue.Substring(0, ip);
            if (TypeRegistry.IsNullHandle(typeName)) return null;
            type = registry[typeName];
          }
          else
          {
            if (TypeRegistry.IsNullHandle(typeHandle)) return null;
            type = registry.GetOrAddType(typeHandle);
          }
        }
        else
        {
          if (TypeRegistry.IsNullHandle(typeHandle)) return null;
          type = registry.GetOrAddType(typeHandle);
        }
      }

      //we get here if we have a boxed value of directly-handled type
      var ra = Format.GetReadActionForType(type) ?? Format.GetReadActionForRefType(type);//20150503 DKh fixed root byte[] slow
      if (ra != null)
        return ra(reader);


      var td = GetTypeDescriptorCachedOrMake(type);

      var instance = td.IsArray ? 
                           Arrays.DescriptorToArray(reader.ReadString(), type) : 
                           SerializationUtils.MakeNewObjectInstance(type);

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

      reader.ReadVarIntStr();//skip type as we already know it from prior-allocated metaHandle


      var td = GetTypeDescriptorCachedOrMake(type);

      if (type.IsArray)
        reader.ReadString();//skip array descriptor as we already know it from prior-allocated metaHandle

      td.DeserializeInstance(reader, registry, refs, ref instance, streamingContext);
    }

  }

}
