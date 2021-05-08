/*<FILE_LICENSE>
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
  public class SlimSerializer
  {
    #region CONSTS

    public const short Header = unchecked((short)0xCAFE);

    #endregion

    #region Fields

    private SlimFormat Format { get; }= SlimFormat.Instance;

    #endregion


    #region Public

    public bool SerializeForFramework { get; set; }

    public void Serialize(Stream stream, object root)
    {
      try
      {
        SlimWriter writer;

          writer = Format.MakeWritingStreamer();

        var pool = ReservePool(SerializationOperation.Serializing);
        try
        {
          writer.BindStream(stream);

          Serialize(writer, root, pool, SerializeForFramework);
        }
        finally
        {
          writer.UnbindStream();
          ReleasePool(pool);
        }
      }
      catch (Exception error)
      {
        throw new SlimException(StringConsts.SerializationExceptionError + error.ToMessageWithType(), error);
      }
    }

    public object Deserialize(Stream stream)
    {
      try
      {
        var reader = Format.MakeReadingStreamer();

        var pool = ReservePool(SerializationOperation.Deserializing);
        try
        {
          reader.BindStream(stream);

          return Deserialize(reader, pool);
        }
        finally
        {
          reader.UnbindStream();
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
      if (_tsPools is null)
      {
        _tsPools = new RefPool[8];
        for (var i = 0; i < _tsPools.Length; i++)
          _tsPools[i] = new RefPool();
        _tsPoolFreeIdx = 0;
      }

      RefPool result;
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

      var scontext = new StreamingContext();
      var registry = new TypeRegistry();
      var type = root != null ? root.GetType() : typeof(object);
      var isValType = type.IsValueType;


      WriteHeader(writer);
      var rcount = registry.Count;

      writer.Write((uint)rcount);
      writer.Write(registry.CSum);


      //Write root in pool if it is reference type
      if (!isValType && root != null)
        pool.Add(root);

      Format.TypeSchema.Serialize(writer, registry, pool, root, scontext, serializationForFrameWork);


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
        var tinst = instance.GetType();
        if (!Format.IsRefTypeSupported(tinst))
          ts.Serialize(writer, registry, pool, instance, scontext, serializationForFrameWork);
      }

    }

    private object Deserialize(SlimReader reader, RefPool pool)
    {
      var streamingContext = new StreamingContext();
      var registry = new TypeRegistry();

      object root;
      {
        var registryCount = registry.Count;

        ReadHeader(reader);
        if (reader.ReadUInt() != registryCount)
          throw new SlimException(StringConsts.TregCountError);
        if (reader.ReadULong() != registry.CSum)
          throw new SlimException(StringConsts.TregCsumError);

        //Read root
        //Deser will add root to pool[1] if its ref-typed
        //------------------------------------------------
        root = Format.TypeSchema.DeserializeRootOrInner(reader, registry, pool, streamingContext, root: true);
        switch (root)
        {
          case null:
            return null;
          case RootTypeBox box:
            return box.TypeValue;
        }


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
          var tinst = instance.GetType();
          if (!Format.IsRefTypeSupported(tinst))
            ts.DeserializeRefTypeInstance(instance, reader, registry, pool, streamingContext);
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

        ctor.Invoke(fixup.Instance, new object[] { fixup.Info, streamingContext });
      }


      //20150214 DD - fixing deserialization problem of Dictionary(InvariantStringComparer)
      //before 20150214 this was AFTER OnDeserialization
      //invoke OnDeserialized-decorated methods
      //--------------------------------------------
      var odc = pool.OnDeserializedCallbacks;
      for (var i = 0; i < odc.Count; i++)
      {
        var cb = odc[i];
        cb.Descriptor.InvokeOnDeserializedCallback(cb.Instance, streamingContext);
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
