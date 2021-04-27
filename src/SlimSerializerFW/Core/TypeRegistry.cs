/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace SlimSerializer.Core
{
  /// <summary>
  /// Provides a registry of types, types that do not need to be described in a serialization stream
  /// </summary>
  [Serializable]
  public sealed class TypeRegistry : IEnumerable<Type>, ISerializable
  {
    #region CONSTS
    /// <summary>
    /// Denotes a special type which is object==null
    /// </summary>
    public static readonly VarIntStr NULL_HANDLE = new VarIntStr(0);

    #endregion

    #region STATIC

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static bool IsNullHandle(VarIntStr handle)
    {
      return handle.IntValue == 0 && IsNullHandle(handle.StringValue);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static bool IsNullHandle(string handle)
    {
      if (handle == null) return true;
      var l = handle.Length;
      if (l == 0) return true;
      return l == 2 && handle[0] == '$' && handle[1] == 'N';
    }


#if NETFRAMEWORK

    //20140701 DKh - speed optimization
    private const int STR_HNDL_POOL_SIZE = 512;
    internal readonly static string[] STR_HNDL_POOL;

#endif


    #endregion

    #region .ctors

    private struct NULL_HANDLE_FAKE_TYPE { }


    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("tps", m_List.Skip(4/*system type count see ctor*/).Select(t => t.AssemblyQualifiedName).ToArray());
    }


    /// <summary>
    /// Initializes TypeRegistry with types from other sources
    /// </summary>
    public TypeRegistry(params IEnumerable<Type>[] others)
    {
      //WARNING!!! These types MUST be at the following positions always at the pre-defined index:
      add(typeof(NULL_HANDLE_FAKE_TYPE));//must be at index zero - NULL HANDLE
      add(typeof(object));//must be at index 1 - object(not null)
      add(typeof(object[]));//must be at index 2
      add(typeof(byte[]));

      if (others != null)
        foreach (var t in others.Where(_ => !(_ is null)).SelectMany(_=>_).Where(_ => !(_ is null)))
          Add(t);
    }

    #endregion

    #region Fields
    private Dictionary<Type, int> m_Types = new Dictionary<Type, int>(0xff);
    private List<Type> m_List = new List<Type>(0xff);

    private ulong m_CSum;

    #endregion


    #region Properties

    /// <summary>
    /// How many items in the registry
    /// </summary>
    public int Count { get { return m_List.Count; } }

    /// <summary>
    /// Returns quick checksum of type registry contents.
    /// It is updated when new types get added into the registry
    /// </summary>
    public ulong CSum { get { return m_CSum; } }



    private static volatile Dictionary<string, Type> s_Types = new Dictionary<string, Type>(StringComparer.Ordinal);

    /// <summary>
    /// Returns type by handle i.e. VarIntStr(1) or VarIntStr("full name"). Throws in case of error
    /// </summary>
    public Type this[VarIntStr handle]
    {
      get
      {
        try
        {
          if (IsNullHandle(handle)) return typeof(object);

          if (handle.StringValue == null)
          {
            var idx = (int)handle.IntValue;
            if (idx < m_List.Count)
              return m_List[idx];
            throw new Exception();
          }

          Type result;
          if (!s_Types.TryGetValue(handle.StringValue, out result))
          {
            result = Type.GetType(handle.StringValue, true);
            var dict = new Dictionary<string, Type>(s_Types, StringComparer.Ordinal);
            dict[handle.StringValue] = result;
            System.Threading.Thread.MemoryBarrier();
            s_Types = dict;//atomic
          }

          bool added;
          getTypeIndex(result, out added);
          return result;
        }
        catch
        {
          throw new SlimException("TypeRegistry[handle] is invalid: " + handle.ToString());
        }
      }
    }

    /// <summary>
    /// Returns type by handle i.e. '$11' or full name. Throws in case of error
    /// </summary>
    public Type this[string handle]
    {
      get
      {
        try
        {
          if (IsNullHandle(handle)) return typeof(object);

          if (handle[0] == '$')
          {
            // var idx = int.Parse(handle.Substring(1));
            //20140701 DKh speed improvement
            var idx = quickParseInt(handle);
            if (idx < m_List.Count) return m_List[idx];
            throw new Exception();
          }

          Type result;
          if (!s_Types.TryGetValue(handle, out result))
          {
            result = Type.GetType(handle, true);
            var dict = new Dictionary<string, Type>(s_Types, StringComparer.Ordinal);
            dict[handle] = result;
            System.Threading.Thread.MemoryBarrier();
            s_Types = dict;//atomic
          }

          bool added;
          getTypeIndex(result, out added);
          return result;
        }
        catch
        {
          throw new SlimException("TypeRegistry[handle] is invalid: " + (handle ?? CoreConsts.NULL_STRING));
        }
      }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static int quickParseInt(string str)
    {
      int result = 0;
      var l = str.Length;
      for (var i = 1; i < l; i++) //0=$, starts at index 1
      {
        var d = str[i] - '0';
        if (d < 0 || d > 9) throw new SlimException("Invalid type handle int: " + str);
        result *= 10;
        result += d;
      }

      return result;
    }


    #endregion

    #region Public

    /// <summary>
    /// Adds the type if it not already in registry and returns true
    /// </summary>
    public bool Add(Type type)
    {
      var idx = 0;
      if (m_Types.TryGetValue(type, out idx)) return false;
      add(type);
      return true;
    }



    /// <summary>
    /// Returns a VarIntStr with the type index formatted as handle if type exists in registry, or fully qualified type name otherwise
    /// </summary>
    public VarIntStr GetTypeHandle(Type type, bool serializationForFrameWork)
    {
      bool added;
      var idx = getTypeIndex(type, out added);
      if (!added)
        return new VarIntStr((uint)idx);

      if (serializationForFrameWork)
        return new VarIntStr(type.AssemblyQualifiedName.Replace(
          "System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
          "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
          ));

      return new VarIntStr(type.AssemblyQualifiedName);
    }



    public IEnumerator<Type> GetEnumerator()
    {
      return m_List.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return m_List.GetEnumerator();
    }


    #endregion

    #region .pvt .impl

    private int add(Type t)
    {
      m_List.Add(t);
      var idx = m_List.Count - 1;
      m_Types.Add(t, idx);

      var tn = t.FullName;
      var len = tn.Length;
      int csum = (((byte)tn[0]) << 16) |
                   (((byte)tn[len - 1]) << 8) |
                   (len & 0xff);

      m_CSum += (ulong)csum; //unchecked is not needed as there is never going to be> 4,000,000,000 types in registry
      return idx;
    }


    private int getTypeIndex(Type type, out bool added)
    {

      added = false;
      var idx = 0;
      if (m_Types.TryGetValue(type, out idx)) return idx;

      added = true;
      idx = add(type);

      return idx;
    }


    #endregion


  }

}
