/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace Slim.Core
{
  internal class RefPool
    {
      public const int PoolCapacity = 1024;
      public const int LargeTrimThreshold = 16 * 1024;


      public void Acquire(SerializationOperation mode)
      {
        m_Mode = mode;
      }

      public void Release()
      {
        if (m_Mode == SerializationOperation.Serializing)
        {
          m_List.Clear();
          m_Dict.Clear();
        }
        else //Deserialization
        {
          m_List.Clear();
          m_Fixups.Clear();
          m_OnDeserializedCallbacks.Clear();
        }
      }


      private SerializationOperation m_Mode;
      private QuickRefList m_List = new QuickRefList(PoolCapacity);

      private readonly Dictionary<object, int> m_Dict =
        new Dictionary<object, int>(PoolCapacity, ReferenceEqualityComparer<object>.Instance);

      private readonly List<SerializableFixup> m_Fixups = new List<SerializableFixup>();
      private readonly List<OnDeserializedCallback> m_OnDeserializedCallbacks = new List<OnDeserializedCallback>();

      public int Count => m_List.Count;

      public List<SerializableFixup> Fixups => m_Fixups;

      public List<OnDeserializedCallback> OnDeserializedCallbacks => m_OnDeserializedCallbacks;

      public object this[int i] => m_List[i];

      public bool Add(object reference)
      {
        if (m_Mode == SerializationOperation.Serializing)
        {
          GetIndex(reference, out var added);
          return added;
        }
        else
        {
          if (reference == null) return false;
          m_List.Add(reference);
          return true;
        }
      }

      public void AddISerializableFixup(object instance, SerializationInfo info)
      {
        Debug.Assert(m_Mode == SerializationOperation.Deserializing,
          "AddISerializableFixup() called while serializing");

        m_Fixups.Add(new SerializableFixup {Instance = instance, Info = info});
      }

      public void AddOnDeserializedCallback(object instance, TypeDescriptor descriptor)
      {
        Debug.Assert(m_Mode == SerializationOperation.Deserializing,
          "AddOnDeserializedCallback() called while serializing");

        m_OnDeserializedCallbacks.Add(new OnDeserializedCallback {Instance = instance, Descriptor = descriptor});
      }

      /// <summary>
      /// Emits MetaHandle that contains type handle for reference handle only when this referenced is added to pool for the first time.
      /// Emits inlined string for strings and inlined value types for boxed objects.
      /// Emits additional array dimensions info for array refernces who's types are emitted for the first time
      /// </summary>
      public MetaHandle GetHandle(object reference, TypeRegistry treg, SlimFormat format, out Type type,
        bool serializationForFrameWork)
      {
        Debug.Assert(m_Mode == SerializationOperation.Serializing, "GetHandle() called while deserializing");

        if (reference == null)
        {
          type = null;
          return new MetaHandle(0);
        }

        type = reference.GetType();

        if (type == typeof(string))
        {
          return MetaHandle.InlineString(reference as string);
        }

        if (reference is Type)
        {
          var thandle = treg.GetTypeHandle(reference as Type, serializationForFrameWork);
          return MetaHandle.InlineTypeValue(thandle);
        }


        if (type.IsValueType)
        {
          var vth = treg.GetTypeHandle(type, serializationForFrameWork);
          return MetaHandle.InlineValueType(vth);
        }

        var handle = GetIndex(reference, out var added);

        if (!added) return new MetaHandle(handle);

        var th = treg.GetTypeHandle(type, serializationForFrameWork);

        if (format.IsRefTypeSupported(type)) //20150305 Refhandle inline
          return MetaHandle.InlineRefType(th);

        if (type.IsArray) //write array header like so:  "System.int[,]|0~10,0~12" or "$3|0~10,0~12"
        {
          //DKh 20130712 Removed repetitive code that was refactored into Arrays class
          var arr = (Array) reference;
          th = new VarIntStr(Arrays.ArrayToDescriptor(arr, type, th));
        }

        return new MetaHandle(handle, th);
      }


      /// <summary>
      /// Returns object reference for supplied metahandle
      /// </summary>
      public object HandleToReference(MetaHandle handle, TypeRegistry treg, SlimFormat format, SlimReader reader)
      {
        Debug.Assert(m_Mode == SerializationOperation.Deserializing, "HandleToReference() called while serializing");
        Contract.Requires(!(handle.Metadata is null), $"{nameof(handle)}.{nameof(MetaHandle.Metadata)} is not null");
        if (handle.IsInlinedString)
        {
          return handle.Metadata.Value.StringValue;
        }

        if (handle.IsInlinedTypeValue)
        {
          var typeRef = treg.GetOrAddType(handle.Metadata.Value); //adding this type to registry if it is not there yet
          return typeRef;
        }

        if (handle.IsInlinedRefType)
        {
          var typeRef = treg.GetOrAddType(handle.Metadata.Value); //adding this type to registry if it is not there yet
          var ra = format.GetReadActionForRefType(typeRef);
          if (ra != null)
          {
            var inst = ra(reader);
            m_List.Add(inst);
            return inst;
          }

          throw new SlimException(
            "Internal error HandleToReference: no read action for ref type, but ref mhandle is inlined");
        }


        var idx = (int) handle.Handle;
        if (idx < m_List.Count) return m_List[idx];

        if (!handle.Metadata.HasValue)
          throw new SlimException(StringConsts.HndltorefMissingTypeNameError + handle);

        Type type;
        var metadata = handle.Metadata.Value;

        if (metadata.StringValue != null) //need to search for possible array descriptor
        {
          var ip = metadata.StringValue.IndexOf('|'); //array descriptor start
          if (ip > 0)
          {
            var typeName = metadata.StringValue.Substring(0, ip);
            if (TypeRegistry.IsNullHandle(typeName)) return null;
            type = treg[typeName];
          }
          else
          {
            if (TypeRegistry.IsNullHandle(metadata)) return null;
            type = treg.GetOrAddType(metadata);
          }
        }
        else
        {
          if (TypeRegistry.IsNullHandle(metadata)) return null;
          type = treg.GetOrAddType(metadata);
        }

        object instance;
        if (type.IsArray)
          //DKh 20130712 Removed repetitive code that was refactored into Arrays class
          instance = Arrays.DescriptorToArray(metadata.StringValue, type);
        else
          //20130715 DKh
          instance = SerializationUtils.MakeNewObjectInstance(type);

        m_List.Add(instance);
        return instance;
      }

      private int GetIndex(object reference, out bool added)
      {
        const int maxLinearSearch = 8; //linear search is faster than dict lookup

        added = false;
        if (reference == null) return 0;
        var cnt = m_List.Count;

        int idx;
        if (cnt < maxLinearSearch)
        {
          for (var i = 1; i < cnt; i++) //start form 1, skip NULL[0]
            if (object.ReferenceEquals(m_List[i], reference))
              return i;
        }
        else if (m_Dict.TryGetValue(reference, out idx)) return idx;


        added = true;
        m_List.Add(reference);
        cnt = m_List.Count;
        idx = cnt - 1;
        if (cnt < maxLinearSearch) return idx;
        if (cnt == maxLinearSearch)
        {
          //upgrade LIST->DICT
          for (var i = 1; i < cnt; i++) //start form 1, skip NULL[0]
            m_Dict.Add(m_List[i], i);
        }
        else
          m_Dict.Add(reference, idx);

        return idx;
      }

    } //RefPool



    //this class works faster than List<object> as it skips un-needed bound checks and array clears
}
