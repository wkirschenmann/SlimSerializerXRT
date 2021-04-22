/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Azos.IO;

namespace Azos.Serialization.Slim
{

    internal class _ISerializableFixup
    {
       public object Instance;
       public SerializationInfo Info;
    }

    internal class _OnDeserializedCallback
    {
       public object Instance;
       public TypeDescriptor Descriptor;
    }





    internal class RefPool
    {
       public const int POOL_CAPACITY = 1024;
       public const int LARGE_TRIM_THRESHOLD = 16 * 1024;


       public void Acquire(SerializationOperation mode)
       {
          m_Mode = mode;
       }

       public void Release()
       {
          if (m_Mode==SerializationOperation.Serializing)
          {
            m_List.Clear();
            m_Dict.Clear();
          }
          else//Deserialization
          {
            m_List.Clear();
            m_Fixups.Clear();
            m_OnDeserializedCallbacks.Clear();
          }
       }


       private SerializationOperation m_Mode;
       private QuickRefList m_List = new QuickRefList(POOL_CAPACITY);
       private Dictionary<object, int> m_Dict = new Dictionary<object,int>(POOL_CAPACITY, Collections.ReferenceEqualityComparer<object>.Instance);
       private List<_ISerializableFixup> m_Fixups = new List<_ISerializableFixup>();
       private List<_OnDeserializedCallback> m_OnDeserializedCallbacks = new List<_OnDeserializedCallback>();

       public int Count { get { return m_List.Count;} }

       public List<_ISerializableFixup> Fixups { get { return m_Fixups; }}

       public List<_OnDeserializedCallback> OnDeserializedCallbacks { get { return m_OnDeserializedCallbacks; }}

       public object this[int i] {  get { return m_List[i];  } }

       public bool Add(object reference)
       {
         if (m_Mode==SerializationOperation.Serializing)
         {
           bool added;
           getIndex(reference, out added);
           return added;
         }
         else
         {
           if (reference==null) return false;
           m_List.Add(reference);
           return true;
         }
       }

       public void AddISerializableFixup(object instance, SerializationInfo info)
       {
         Debug.Assert(m_Mode == SerializationOperation.Deserializing, "AddISerializableFixup() called while serializing", DebugAction.Throw);

         m_Fixups.Add( new _ISerializableFixup{ Instance = instance, Info = info } );
       }

       public void AddOnDeserializedCallback(object instance, TypeDescriptor descriptor)
       {
         Debug.Assert(m_Mode == SerializationOperation.Deserializing, "AddOnDeserializedCallback() called while serializing", DebugAction.Throw);

         m_OnDeserializedCallbacks.Add( new _OnDeserializedCallback{ Instance = instance, Descriptor = descriptor } );
       }

       /// <summary>
       /// Emits MetaHandle that contains type handle for reference handle only when this referenced is added to pool for the first time.
       /// Emits inlined string for strings and inlined value types for boxed objects.
       /// Emits additional array dimensions info for array refernces who's types are emitted for the first time
       /// </summary>
       public MetaHandle GetHandle(object reference, TypeRegistry treg, SlimFormat format, out Type type, bool serializationForFrameWork)
       {
         Debug.Assert(m_Mode == SerializationOperation.Serializing, "GetHandle() called while deserializing", DebugAction.Throw);

         if (reference==null)
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

         bool added;

         uint handle = (uint)getIndex(reference, out added);

         if (added)
         {
              var th =  treg.GetTypeHandle(type, serializationForFrameWork);

              if (format.IsRefTypeSupported(type))//20150305 Refhandle inline
                return MetaHandle.InlineRefType(th);

              if (type.IsArray)//write array header like so:  "System.int[,]|0~10,0~12" or "$3|0~10,0~12"
              {
                //DKh 20130712 Removed repetitive code that was refactored into Arrays class
                var arr = (Array)reference;
                th = new VarIntStr( Arrays.ArrayToDescriptor(arr, type, th) );
              }

              return new MetaHandle(handle, th);
         }
         return new MetaHandle(handle);
       }


       /// <summary>
       /// Returns object reference for supplied metahandle
       /// </summary>
       public object HandleToReference(MetaHandle handle, TypeRegistry treg, SlimFormat format, SlimReader reader)
       {
         Debug.Assert(m_Mode == SerializationOperation.Deserializing, "HandleToReference() called while serializing", DebugAction.Throw);

         if (handle.IsInlinedString) return handle.Metadata.Value.StringValue;
         if (handle.IsInlinedTypeValue)
         {
           var tref = treg[ handle.Metadata.Value ];//adding this type to registry if it is not there yet
           return tref;
         }

         if (handle.IsInlinedRefType)
         {
             var tref = treg[ handle.Metadata.Value ];//adding this type to registry if it is not there yet
             var ra = format.GetReadActionForRefType(tref);
             if (ra!=null)
             {
               var inst = ra(reader);
               m_List.Add(inst);
               return inst;
             }
             else
              throw new SlimDeserializationException("Internal error HandleToReference: no read action for ref type, but ref mhandle is inlined");
         }


         int idx = (int)handle.Handle;
         if (idx<m_List.Count) return m_List[idx];

         if (!handle.Metadata.HasValue)
          throw new SlimDeserializationException(StringConsts.SLIM_HNDLTOREF_MISSING_TYPE_NAME_ERROR + handle.ToString());

         Type type;
         var metadata = handle.Metadata.Value;

         if (metadata.StringValue!=null)//need to search for possible array descriptor
         {
            var ip = metadata.StringValue.IndexOf('|');//array descriptor start
            if (ip>0)
            {
              var tname =  metadata.StringValue.Substring(0, ip);
              if (TypeRegistry.IsNullHandle(tname)) return null;
              type = treg[ tname ];
            }
            else
            {
              if (TypeRegistry.IsNullHandle(metadata)) return null;
              type = treg[ metadata ];
            }
         }
         else
         {
            if (TypeRegistry.IsNullHandle(metadata)) return null;
            type = treg[ metadata ];
         }

         object instance = null;

         if (type.IsArray)
              //DKh 20130712 Removed repetitive code that was refactored into Arrays class
              instance = Arrays.DescriptorToArray(metadata.StringValue, type);
         else
              //20130715 DKh
              instance = SerializationUtils.MakeNewObjectInstance(type);

         m_List.Add(instance);
         return instance;
       }

       private int getIndex(object reference, out bool added)
       {
           const int MAX_LINEAR_SEARCH = 8;//linear search is faster than dict lookup

           added = false;
           if (reference==null) return 0;

           int idx = -1;

           var cnt = m_List.Count;
           if (cnt<MAX_LINEAR_SEARCH)
           {
             for(var i=1; i<cnt; i++)//start form 1, skip NULL[0]
              if (object.ReferenceEquals(m_List[i], reference)) return i;
           }
           else
            if (m_Dict.TryGetValue(reference, out idx)) return idx;


           added = true;
           m_List.Add(reference);
           cnt = m_List.Count;
           idx = cnt - 1;
           if (cnt<MAX_LINEAR_SEARCH) return idx;
           if (cnt==MAX_LINEAR_SEARCH)
           {//upgrade LIST->DICT
            for(var i=1; i<cnt; i++)//start form 1, skip NULL[0]
              m_Dict.Add( m_List[i], i);
           }
           else
            m_Dict.Add(reference, idx);

           return idx;
      }

    }//RefPool



    //this class works faster than List<object> as it skips un-needed bound checks and array clears
    internal struct QuickRefList
    {
      public QuickRefList(int capacity)
      {
        if (Ambient.MemoryModel < Ambient.MemoryUtilizationModel.Regular) capacity = capacity / 2;
        m_InitialCapacity = capacity;
        m_Data = new object[ capacity ];
        m_Count = 1;//the "zeros" element is always NULL
      }

      private int m_InitialCapacity;
      private object[] m_Data;
      private int m_Count;


      public int Count{ get{ return m_Count;}}

      public object this[int i]{  get{ return m_Data[i];} }

      public void Clear()
      {
        var mm = Ambient.MemoryModel;
        var trimAt = mm < Ambient.MemoryUtilizationModel.Regular ?  RefPool.POOL_CAPACITY : RefPool.LARGE_TRIM_THRESHOLD;

        if (m_Count>trimAt) //We want to get rid of excess data when too much
        {                           //otherwise the array will get stuck in pool cache for a long time
          m_Data = new object[ m_InitialCapacity ];
        } else if (mm== Ambient.MemoryUtilizationModel.Tiny)
        {
           Array.Clear(m_Data, 0, m_Data.Length);
        }

        m_Count = 1;//[0]==null, dont clear //notice: no Array.Clear... for normal memory modes
      }

      public void Add(object reference)
      {
        var len = m_Data.Length;
        if (m_Count==len)
        {
          var newData = new object[2 * len];
          Array.Copy(m_Data, 0, newData, 0, len);
          m_Data = newData;
        }

        m_Data[m_Count] = reference;
        m_Count++;
      }

    }



}
