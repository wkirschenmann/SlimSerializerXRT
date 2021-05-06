/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using Slim.Core;

namespace Slim
{
  /// <summary>
  /// Implements Slim serialization algorithm that relies on an injectable SlimFormat-derivative (through .ctor) paremeter.
  /// This class was designed for highly-efficient serialization of types without versioning.
  /// SlimSerializer supports a concept of "known types" that save space by not emitting their names into stream.
  /// Performance note:
  /// This serializer yields on average 1/4 serialization and 1/2 deserialization times while compared to BinaryFormatter.
  /// Serialization of Record-instances usually takes 1/6 of BinaryFormatter time.
  /// Format takes 1/10 space for records and 1/2 for general object graphs.
  /// Such performance is achieved because of dynamic compilation of type-specific serialization/deserialization methods.
  /// This type is thread-safe for serializations/deserializations when TypeMode is set to "PerCall"
  /// </summary>
  [SlimSerializationProhibited]
  public class SlimSerializer : ISlimSerializer
  {
    #region CONSTS

    public const ushort Header = (ushort)0xCAFE;

    #endregion

    #region .ctor


    public SlimSerializer() : this(SlimFormat.Instance)
    {

    }

    public SlimSerializer(params IEnumerable<Type>[] globalTypes) : this(SlimFormat.Instance, globalTypes)
    {

    }

    public SlimSerializer(SlimFormat format)
    {
      if (format == null)
        throw new SlimException(StringConsts.ArgumentError + "SlimSerializer.ctor(format=null)");

      m_Format = format;
      m_TypeMode = TypeRegistryMode.PerCall;
    }

    public SlimSerializer(SlimFormat format, params IEnumerable<Type>[] globalTypes) : this(format)
    {
      m_GlobalTypes = globalTypes;
    }


    internal SlimSerializer(TypeRegistry global, SlimFormat format) : this(format)
    {
      GlobalTypeRegistry = global;
      m_GlobalTypes = new IEnumerable<Type>[] { global.ToArray() };
      m_SkipTypeRegistryCrosschecks = true;
    }


    #endregion

    #region Fields

    internal readonly TypeRegistry GlobalTypeRegistry;

    private SlimFormat m_Format;
    private IEnumerable<Type>[] m_GlobalTypes;
    private TypeRegistryMode m_TypeMode;

    private TypeRegistry m_BatchTypeRegistry;
    private int m_BatchTypeRegistryPriorCount;

    private bool m_SkipTypeRegistryCrosschecks;

    /// <summary>
    /// Associates arbitrary owner object with this instance. Slim serializer does not use this field internally for any purpose
    /// </summary>
    public object Owner;

    #endregion

    #region Properties



    public SlimFormat Format => m_Format;

    /// <summary>
    /// Gets/sets how serializer handles type information between calls to Serialize/Deserialize.
    /// Setting this to "Batch" makes this serializer instance not thread-safe for calling Serialize/Deserialize.
    /// This property itself is not thread-safe, that is - it should be only set once by control/initiating thread
    /// </summary>
    public TypeRegistryMode TypeMode
    {
      get => m_TypeMode;
      set
      {
        if (m_TypeMode == value) return;

        if (value == TypeRegistryMode.Batch)
        {
          m_BatchTypeRegistry = new TypeRegistry(m_GlobalTypes);
          m_BatchTypeRegistryPriorCount = m_BatchTypeRegistry.Count;
        }
        else
          m_BatchTypeRegistry = null;

        m_TypeMode = value;
      }
    }

    /// <summary>
    /// Returns true when TypeMode is "PerCall"
    /// </summary>
    public bool IsThreadSafe => m_TypeMode == TypeRegistryMode.PerCall;


    /// <summary>
    /// Returns true if last call to Serialize or Deserialize in batch mode added more types to type registry.
    /// This call is only valid in TypeMode = "Batch" and is inherently not thread-safe
    /// </summary>
    public bool BatchTypesAdded => m_TypeMode == TypeRegistryMode.Batch && m_BatchTypeRegistryPriorCount != m_BatchTypeRegistry.Count;

    /// <summary>
    /// ADVANCED FEATURE! Developers do not use.
    /// Returns type registry used in batch.
    /// This call is only valid in TypeMode = "Batch" and is inherently not thread-safe.
    /// Be careful not to mutate the returned object
    /// </summary>
    public TypeRegistry BatchTypeRegistry => m_BatchTypeRegistry;

    #endregion


    #region Public

    /// <summary>
    /// Resets type registry to initial state (which is based on global types) for TypeMode = "Batch",
    /// otherwise does nothing. This method is not thread-safe
    /// </summary>
    public void ResetCallBatch()
    {
      if (m_TypeMode != TypeRegistryMode.Batch) return;
      m_BatchTypeRegistry = new TypeRegistry(m_GlobalTypes);
      m_BatchTypeRegistryPriorCount = m_BatchTypeRegistry.Count;
    }

    private int m_SerializeNestLevel;
    private SlimWriter m_CachedWriter;

    public bool SerializeForFramework { get; set; } = false;

    public void Serialize(Stream stream, object root)
    {
      try
      {
        var singleThreaded = m_TypeMode == TypeRegistryMode.Batch;

        SlimWriter writer;

        if (!singleThreaded || m_SerializeNestLevel > 0)
          writer = m_Format.MakeWritingStreamer();
        else
        {
          writer = m_CachedWriter;
          if (writer == null)
          {
            writer = m_Format.MakeWritingStreamer();
            m_CachedWriter = writer;
          }
        }

        var pool = ReservePool(SerializationOperation.Serializing);
        try
        {
          m_SerializeNestLevel++;
          writer.BindStream(stream);

          Serialize(writer, root, pool, SerializeForFramework);
        }
        finally
        {
          writer.UnbindStream();
          m_SerializeNestLevel--;
          ReleasePool(pool);
        }
      }
      catch (Exception error)
      {
        throw new SlimException(StringConsts.SerializationExceptionError + error.ToMessageWithType(), error);
      }
    }

    private int m_DeserializeNestLevel;
    private SlimReader m_CachedReader;

    public object Deserialize(Stream stream)
    {
      try
      {
        var singleThreaded = m_TypeMode == TypeRegistryMode.Batch;

        SlimReader reader;

        if (!singleThreaded || m_DeserializeNestLevel > 0)
          reader = m_Format.MakeReadingStreamer();
        else
        {
          reader = m_CachedReader;
          if (reader == null)
          {
            reader = m_Format.MakeReadingStreamer();
            m_CachedReader = reader;
          }
        }

        var pool = ReservePool(SerializationOperation.Deserializing);
        try
        {
          m_DeserializeNestLevel++;
          reader.BindStream(stream);

          return Deserialize(reader, pool);
        }
        finally
        {
          reader.UnbindStream();
          m_DeserializeNestLevel--;
          ReleasePool(pool);
        }
      }
      catch (Exception error)
      {
        throw new SlimException(StringConsts.DeserializationExceptionError + error.ToMessageWithType(), error);
      }
    }

    #endregion



    #region .pvt .impl

    [ThreadStatic]
    private static RefPool[] _tsPools;
    [ThreadStatic]
    private static int _tsPoolFreeIdx;

    private static RefPool ReservePool(SerializationOperation mode)
    {
      RefPool result = null;
      if (_tsPools == null)
      {
        _tsPools = new RefPool[8];
        for (var i = 0; i < _tsPools.Length; i++)
          _tsPools[i] = new RefPool();
        _tsPoolFreeIdx = 0;
      }

      if (_tsPoolFreeIdx < _tsPools.Length)
      {
        result = _tsPools[_tsPoolFreeIdx];
        _tsPoolFreeIdx++;
      }
      else
        result = new RefPool();

      result.Acquire(mode);
      return result;
    }

    private static void ReleasePool(RefPool pool)
    {
      if (_tsPoolFreeIdx == 0) return;
      pool.Release();
      _tsPoolFreeIdx--;
      _tsPools[_tsPoolFreeIdx] = pool;
    }


    private void Serialize(SlimWriter writer, object root, RefPool pool, bool serializationForFrameWork)
    {
      if (root is Type)
        root = new RootTypeBox { TypeValue = (Type)root };

      var scontext = new StreamingContext();
      var registry = (m_TypeMode == TypeRegistryMode.PerCall) ? new TypeRegistry(m_GlobalTypes) : m_BatchTypeRegistry;
      var type = root != null ? root.GetType() : typeof(object);
      var isValType = type.IsValueType;


      WriteHeader(writer);
      var rcount = registry.Count;
      m_BatchTypeRegistryPriorCount = rcount;

      if (!m_SkipTypeRegistryCrosschecks)
      {
        writer.Write((uint)rcount);
        writer.Write(registry.CSum);
      }


      //Write root in pool if it is reference type
      if (!isValType && root != null)
        pool.Add(root);

      m_Format.TypeSchema.Serialize(writer, registry, pool, root, scontext, serializationForFrameWork);


      if (root == null) return;

      var i = 1;

      if (!isValType) i++;

      //Write all the rest of objects. The upper bound of this loop may increase as objects are written
      //0 = NULL
      //1 = root IF root is ref type
      var ts = m_Format.TypeSchema;
      for (; i < pool.Count; i++)
      {
        var instance = pool[i];
        var tinst = instance.GetType();
        if (!m_Format.IsRefTypeSupported(tinst))
          ts.Serialize(writer, registry, pool, instance, scontext, serializationForFrameWork);
      }

    }

    private object Deserialize(SlimReader reader, RefPool pool)
    {
      object root = null;

      var scontext = new StreamingContext();
      var registry = (m_TypeMode == TypeRegistryMode.PerCall) ? new TypeRegistry(m_GlobalTypes) : m_BatchTypeRegistry;

      {
        var rcount = registry.Count;
        m_BatchTypeRegistryPriorCount = rcount;

        ReadHeader(reader);
        if (!m_SkipTypeRegistryCrosschecks)
        {
          if (reader.ReadUInt() != rcount)
            throw new SlimException(StringConsts.TregCountError);
          if (reader.ReadULong() != registry.CSum)
            throw new SlimException(StringConsts.TregCsumError);
        }

        //Read root
        //Deser will add root to pool[1] if its ref-typed
        //------------------------------------------------
        root = m_Format.TypeSchema.DeserializeRootOrInner(reader, registry, pool, scontext, root: true);
        if (root == null) return null;
        if (root is RootTypeBox) return ((RootTypeBox)root).TypeValue;


        var type = root.GetType();
        var isValType = type.IsValueType;

        var i = 1;

        if (!isValType) i++;

        //Read all the rest of objects. The upper bound of this loop may increase as objects are read and their references added to pool
        //0 = NULL
        //1 = root IF root is ref type
        //-----------------------------------------------
        var ts = m_Format.TypeSchema;
        for (; i < pool.Count; i++)
        {
          var instance = pool[i];
          var tinst = instance.GetType();
          if (!m_Format.IsRefTypeSupported(tinst))
            ts.DeserializeRefTypeInstance(instance, reader, registry, pool, scontext);
        }

      }

      //perform fixups for ISerializable
      //---------------------------------------------
      var fxps = pool.Fixups;
      for (var i = 0; i < fxps.Count; i++)
      {
        var fixup = fxps[i];
        var t = fixup.Instance.GetType();
        var ctor = SerializationUtils.GetISerializableCtorInfo(t);

        if (ctor == null) continue;//20171223 DKh ISerializable does not mandate the .ctor(info, context),
                                   //for example, in net-core they use info.SetType() to redefine comparers
                                   //so the actual comparer does not have a .ctor at all (which is very odd)

        //Before 20171223 DKh change
        //if (ctor==null)
        // throw new SlimDeserializationException(StringConsts.SLIM_ISERIALIZABLE_MISSING_CTOR_ERROR + t.FullName);

        ctor.Invoke(fixup.Instance, new object[] { fixup.Info, scontext });
      }


      //20150214 DD - fixing deserialization problem of Dictionary(InvariantStringComparer)
      //before 20150214 this was AFTER OnDeserialization
      //invoke OnDeserialized-decorated methods
      //--------------------------------------------
      var odc = pool.OnDeserializedCallbacks;
      for (int i = 0; i < odc.Count; i++)
      {
        var cb = odc[i];
        cb.Descriptor.InvokeOnDeserializedCallbak(cb.Instance, scontext);
      }

      //before 20150214 this was BEFORE OnDeserializedCallbacks
      //invoke IDeserializationCallback
      //---------------------------------------------
      for (int i = 1; i < pool.Count; i++)//[0]=null
      {
        var dc = pool[i] as IDeserializationCallback;
        if (dc != null)
          try
          {
            dc.OnDeserialization(this);
          }
          catch (Exception error)
          {
            throw new SlimException(StringConsts.DeserializeCallbackError + error.ToMessageWithType(), error);
          }
      }



      return root;
    }



    private void WriteHeader(SlimWriter writer)
    {
      writer.Write((byte)0);
      writer.Write((byte)0);
      writer.Write((byte)((Header >> 8) & 0xff));
      writer.Write((byte)(Header & 0xff));
    }

    private void ReadHeader(SlimReader reader)
    {
      if (reader.ReadByte() != 0 ||
          reader.ReadByte() != 0 ||
          reader.ReadByte() != (byte)((Header >> 8) & 0xff) ||
          reader.ReadByte() != (byte)(Header & 0xff)
         ) throw new SlimException(StringConsts.BadHeaderError);
    }

    private struct RootTypeBox
    {
      public Type TypeValue;
    }

    #endregion

  }


}
