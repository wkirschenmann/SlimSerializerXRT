/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
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
    public static readonly VarIntStr NullHandle = new VarIntStr(0);

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

    //20140701 DKh - speed optimization
    private const int STR_HANDLE_POOL_SIZE = 512;

    internal static readonly string[] StrHandlePool = Enumerable.Range(1, STR_HANDLE_POOL_SIZE - 1)
                                                                .Select(i => $"${i}")
                                                                .Prepend("$N")
                                                                .ToArray();

    //20140701 DKh - speed optimization

    #endregion

    #region .ctors

    private struct NullHandleFakeType { }


    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("tps", m_List.Skip(4/*system type count see ctor*/).Select(t => t.AssemblyQualifiedName).ToArray());
    }

    private TypeRegistry(SerializationInfo info, StreamingContext context)
    {

    }



    /// <summary>
    /// Initializes TypeRegistry with types from other sources
    /// </summary>
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "<Pending>")]
    public TypeRegistry(params IEnumerable<Type>[] others)
    {
      //WARNING!!! These types MUST be at the following positions always at the pre-defined index:
      Add(typeof(NullHandleFakeType));//must be at index zero - NULL HANDLE
      Add(typeof(object));//must be at index 1 - object(not null)
      Add(typeof(object[]));//must be at index 2
      Add(typeof(byte[]));

      if (others != null)
        foreach (var t in others.Where(_ => !(_ is null)).SelectMany(_ => _).Where(_ => !(_ is null)))
          TryAdd(t);
    }

    #endregion

    #region Fields
    private readonly Dictionary<Type, int> m_Types = new Dictionary<Type, int>(0xff);
    private readonly List<Type> m_List = new List<Type>(0xff);

    #endregion


    #region Properties

    /// <summary>
    /// How many items in the registry
    /// </summary>
    public int Count => m_List.Count;

    /// <summary>
    /// Returns quick checksum of type registry contents.
    /// It is updated when new types get added into the registry
    /// </summary>
    public ulong CheckSum { get; private set; }


    private static volatile Dictionary<string, Type> _sTypes = new Dictionary<string, Type>(StringComparer.Ordinal);


    /// <summary>
    /// Returns type by HandleValue i.e. VarIntStr(1) or VarIntStr("full name"). Throws in case of error
    /// </summary>
    //TODO : Refactor to have a method instead of a property
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1043:Use Integral Or String Argument For Indexers", Justification = "Intentional")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "TODO : refactor to have a method")]
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
            throw new SlimException("TypeRegistry[HandleValue] is invalid: " + handle);
          }

          if (!_sTypes.TryGetValue(handle.StringValue, out var result))
          {
            result = Type.GetType(handle.StringValue, true);
            var dict = new Dictionary<string, Type>(_sTypes, StringComparer.Ordinal) {[handle.StringValue] = result};
            System.Threading.Thread.MemoryBarrier();
            _sTypes = dict;//atomic
          }

          GetTypeIndex(result, out var added);
          return result;
        }
        catch
        {
          throw new SlimException("TypeRegistry[HandleValue] is invalid: " + handle);
        }
      }
    }

    /// <summary>
    /// Returns type by HandleValue i.e. '$11' or full name. Throws in case of error
    /// </summary>
    //TODO : Refactor to have a method instead of a property
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "TODO : refactor to have a method")]
    public Type this[string handle]
    {
      get
      {
        try
        {
          if (IsNullHandle(handle)) return typeof(object);

          if (handle[0] == '$')
          {
            // var idx = int.Parse(HandleValue.Substring(1));
            //20140701 DKh speed improvement
            var idx = QuickParseInt(handle);
            if (idx < m_List.Count) return m_List[idx];
            throw new SlimException("TypeRegistry[HandleValue] is invalid: " + handle);
          }

          if (!_sTypes.TryGetValue(handle, out var result))
          {
            result = Type.GetType(handle, true);
            var dict = new Dictionary<string, Type>(_sTypes, StringComparer.Ordinal)
            {
              [handle] = result
            };
            System.Threading.Thread.MemoryBarrier();
            _sTypes = dict;//atomic
          }

          GetTypeIndex(result, out _);
          return result;
        }
        catch
        {
          throw new SlimException("TypeRegistry[HandleValue] is invalid: " + handle);
        }
      }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static int QuickParseInt(string str)
    {
      var result = 0;
      var l = str.Length;
      for (var i = 1; i < l; i++) //0=$, starts at index 1
      {
        var d = str[i] - '0';
        if (d < 0 || d > 9) throw new SlimException("Invalid type HandleValue int: " + str);
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
    public bool TryAdd(Type type)
    {
      Contract.Requires(!(type is null), $"{nameof(type)} is not null");
      if (m_Types.TryGetValue(type, out _)) return false;
      Add(type);
      return true;
    }




    /// <summary>
    /// Returns a VarIntStr with the type index formatted as HandleValue if type exists in registry, or fully qualified type name otherwise
    /// </summary>
    public VarIntStr GetTypeHandle(Type type, bool serializationForFrameWork)
    {
      Contract.Requires(!(type is null), $"{nameof(type)} is not null");
      var idx = GetTypeIndex(type, out var added);
      if (!added)
        return new VarIntStr(idx);

      if (serializationForFrameWork)
        // ReSharper disable once PossibleNullReferenceException
      {
        return new VarIntStr(type.AssemblyQualifiedName.Replace(
          "System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e",
          "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
      }

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

    private int Add(Type t)
    {
      m_List.Add(t);
      var idx = m_List.Count - 1;
      m_Types.Add(t, idx);

      var tn = t.FullName;
      var len = tn.Length;
      var checkSum = (ulong) ((((byte) tn[0]) << 16) |
                                (((byte) tn[len - 1]) << 8) |
                                (len & 0xff));

      CheckSum += checkSum; //unchecked is not needed as there is never going to be> 4,000,000,000 types in registry
      return idx;
    }


    private int GetTypeIndex(Type type, out bool added)
    {

      added = false;
      if (m_Types.TryGetValue(type, out var idx)) return idx;

      added = true;
      idx = Add(type);

      return idx;
    }


    #endregion


  }

}
