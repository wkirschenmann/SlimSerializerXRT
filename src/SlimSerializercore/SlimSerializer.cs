/*<FILE_LICENSE>
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;

using SlimSerializer.Core;

namespace SlimSerializer
{
  /// <summary>
  /// Implements Slim serialization algorithm that relies on an injectable SlimFormat-derivative (through .ctor) parameter.
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

    public const int Header = 0xCAFE;

    #endregion

    #region .ctor


    public SlimSerializer() : this(SlimFormat.Instance)
    {

    }

    public SlimSerializer(params IEnumerable<Type>[] globalTypes) : this(SlimFormat.Instance, globalTypes)
    {

    }

    internal SlimSerializer(SlimFormat format)
    {
      Contract.Requires(!(format is null), $"{nameof(format)} is not null");
      Format = format;
      m_TypeMode = TypeRegistryMode.PerCall;
    }

    internal SlimSerializer(SlimFormat format, params IEnumerable<Type>[] globalTypes) : this(format)
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

    private readonly IEnumerable<Type>[] m_GlobalTypes;
    private TypeRegistryMode m_TypeMode;

    private TypeRegistry m_BatchTypeRegistry;
    private int m_BatchTypeRegistryPriorCount;

    private readonly bool m_SkipTypeRegistryCrosschecks;

    #endregion

    #region Properties



    internal SlimFormat Format { get; }

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

    public bool SerializeForFramework { get; set; }

    public void Serialize(Stream stream, object root)
    {
      try
      {
        var singleThreaded = m_TypeMode == TypeRegistryMode.Batch;

        SlimWriter writer;

        if (!singleThreaded || m_SerializeNestLevel > 0)
          writer = Format.MakeWritingStreamer();
        else
        {
          writer = m_CachedWriter;
          if (writer == null)
          {
            writer = Format.MakeWritingStreamer();
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
          reader = Format.MakeReadingStreamer();
        else
        {
          reader = m_CachedReader;
          if (reader == null)
          {
            reader = Format.MakeReadingStreamer();
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
      RefPool result;
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
      if (root is Type rootType)
        root = new RootTypeBox { TypeValue = rootType };

      var streamingContext = new StreamingContext();
      var registry = (m_TypeMode == TypeRegistryMode.PerCall) ? new TypeRegistry(m_GlobalTypes) : m_BatchTypeRegistry;
      var type = root != null ? root.GetType() : typeof(object);
      var isValType = type.IsValueType;


      WriteHeader(writer);
      var registryCount = registry.Count;
      m_BatchTypeRegistryPriorCount = registryCount;

      if (!m_SkipTypeRegistryCrosschecks)
      {
        writer.Write((uint)registryCount);
        writer.Write(registry.CheckSum);
      }


      //Write root in pool if it is reference type
      if (!isValType && root != null)
        pool.Add(root);

      Format.TypeSchema.Serialize(writer, registry, pool, root, streamingContext, serializationForFrameWork, null);


      if (root == null) return;

      var i = 1;

      if (!isValType) i++;

      //Write all the rest of objects. The upper bound of this loop may increase as objects are written
      //0 = NULL
      //1 = root IF root is ref type
      var ts = Format.TypeSchema;
      for (; i < pool.Count; i++)
      {
        var instance = pool[i];
        var instanceType = instance.GetType();
        if (!Format.IsRefTypeSupported(instanceType))
          ts.Serialize(writer, registry, pool, instance, streamingContext, serializationForFrameWork);
      }

    }

    private object Deserialize(SlimReader reader, RefPool pool)
    {
      object root;

      var streamingContext = new StreamingContext();
      var registry = (m_TypeMode == TypeRegistryMode.PerCall) ? new TypeRegistry(m_GlobalTypes) : m_BatchTypeRegistry;

      {
        var registryCount = registry.Count;
        m_BatchTypeRegistryPriorCount = registryCount;

        ReadHeader(reader);
        if (!m_SkipTypeRegistryCrosschecks)
        {
          if (reader.ReadUInt() != registryCount)
            throw new SlimException(StringConsts.TregCountError);
          if (reader.ReadULong() != registry.CheckSum)
            throw new SlimException(StringConsts.TregCsumError);
        }

        //Read root
        //Deser will add root to pool[1] if its ref-typed
        //------------------------------------------------
        root = Format.TypeSchema.DeserializeRootOrInner(reader, registry, pool, streamingContext, root: true);
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (root == null) return null;
        if (root is RootTypeBox box) return box.TypeValue;


        var type = root.GetType();
        var isValType = type.IsValueType;

        var i = 1;

        if (!isValType) i++;

        //Read all the rest of objects. The upper bound of this loop may increase as objects are read and their references added to pool
        //0 = NULL
        //1 = root IF root is ref type
        //-----------------------------------------------
        var ts = Format.TypeSchema;
        for (; i < pool.Count; i++)
        {
          var instance = pool[i];
          var instanceType = instance.GetType();
          if (!Format.IsRefTypeSupported(instanceType))
            ts.DeserializeRefTypeInstance(instance, reader, registry, pool, streamingContext);
        }

      }

      //perform fixUps for ISerializable
      //---------------------------------------------
      var poolFixUps = pool.FixUps;
      foreach (var fixUp in poolFixUps)
      {
        var t = fixUp.Instance.GetType();
        var ctor = SerializationUtils.GetISerializableCtorInfo(t);

        if (ctor == null) continue;//20171223 DKh ISerializable does not mandate the .ctor(info, context),
        //for example, in net-core they use info.SetType() to redefine comparers
        //so the actual comparer does not have a .ctor at all (which is very odd)

        //Before 20171223 DKh change
        //if (ctor==null)
        // throw new SlimDeserializationException(StringConsts.SLIM_ISERIALIZABLE_MISSING_CTOR_ERROR + t.FullName);

        ctor.Invoke(fixUp.Instance, new object[] { fixUp.Info, streamingContext });
      }


      //20150214 DD - fixing deserialization problem of Dictionary(InvariantStringComparer)
      //before 20150214 this was AFTER OnDeserialization
      //invoke OnDeserialized-decorated methods
      //--------------------------------------------
      var odc = pool.OnDeserializedCallbacks;
      foreach (var cb in odc)
      {
        cb.Descriptor.InvokeOnDeserializedCallbak(cb.Instance, streamingContext);
      }

      //before 20150214 this was BEFORE OnDeserializedCallbacks
      //invoke IDeserializationCallback
      //---------------------------------------------
      for (var i = 1; i < pool.Count; i++)//[0]=null
      {
        if (pool[i] is IDeserializationCallback dc)
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



    private static void WriteHeader(WritingStreamer writer)
    {
      writer.Write((byte)0);
      writer.Write((byte)0);
      writer.Write((byte)((Header >> 8) & 0xff));
      writer.Write((byte)(Header & 0xff));
    }

    private static void ReadHeader(ReadingStreamer reader)
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
