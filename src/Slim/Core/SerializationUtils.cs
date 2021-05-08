/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Linq.Expressions;

using System.Threading;

namespace Slim.Core
{
  /// <summary>
  /// Provides misc serialization-related functions that are really low-level and not intended to be used by developers.
  /// Methods are thread-safe
  /// </summary>
  internal static class SerializationUtils
  {

    /// <summary>
    /// Returns .ctor(SerializationInfo, StreamingContext) that complies with ISerializable concept, or null.
    /// </summary>
    public static ConstructorInfo GetISerializableCtorInfo(Type type)
    {
      Contract.Requires(!(type is null), $"{nameof(type)} is not null");
      return type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                                        null,
                                        new Type[] { typeof(SerializationInfo), typeof(StreamingContext) },
                                        null);
    }


    private static volatile Dictionary<Type, Func<object>> _sCreateFuncCache = new Dictionary<Type, Func<object>>();

    /// <summary>
    /// Create new object instance for type using serialization constructors or default ctor
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static object MakeNewObjectInstance(Type type)
    {
      Func<object> f;
      if (!_sCreateFuncCache.TryGetValue(type, out f))
      {
        var ctorEmpty = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                                        null,
                                        Type.EmptyTypes,
                                        null);

        //20150717 DKh added SlimDeserializationCtorSkipAttribute
        var skipAttr = ctorEmpty != null ?
                        ctorEmpty.GetCustomAttributes<SlimDeserializationCtorSkipAttribute>(false).FirstOrDefault()
                        : null;


        //20150715 DKh look for ISerializable .ctor
        var ctorSer = GetISerializableCtorInfo(type);

        //20150715 DKh, the empty .ctor SHOULD NOT be called for types that have SERIALIZABLE .ctor which is called later(after object init)
        if (ctorEmpty != null && skipAttr == null && ctorSer == null)
          f = Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
        else
          f = () => FormatterServices.GetUninitializedObject(type);

        var cache = new Dictionary<Type, Func<object>>(_sCreateFuncCache);
        cache[type] = f;
        Thread.MemoryBarrier();
        _sCreateFuncCache = cache;
      }

      return f();
    }


    //20150124 Added caching
    private static volatile Dictionary<Type, FieldInfo[]> _sFieldCache = new Dictionary<Type, FieldInfo[]>();
    private static FieldInfo[] GetGetSerializableFields(Type type)
    {
      //20140926 DKh +DeclaredOnly
      var local = type.GetFields(BindingFlags.DeclaredOnly |
                                  BindingFlags.Instance |
                                  BindingFlags.NonPublic |
                                  BindingFlags.Public)
                      //DKh 20130801 removed readonly constraint
                      //DKh 20181129 filter-out Inject fields
                      .Where(fi => !fi.IsNotSerialized)
                      .OrderBy(fi => fi.Name)//DKh 20130730
                      .ToArray();

      var bt = type.BaseType;//null for Object

      if (bt == null || bt == typeof(object)) return local;

      return GetSerializableFields(type.BaseType).Concat(local).ToArray();//20140926 DKh parent+child reversed order, was: child+parent
    }

    /// <summary>
    /// Gets all serializable fields for type in parent->child declaration order, sub-ordered by case
    ///  within the segment
    /// </summary>
    public static IEnumerable<FieldInfo> GetSerializableFields(Type type)
    {
      FieldInfo[] result;
      if (!_sFieldCache.TryGetValue(type, out result))
      {
        result = GetGetSerializableFields(type);
        var dict = new Dictionary<Type, FieldInfo[]>(_sFieldCache);
        dict[type] = result;
        Thread.MemoryBarrier();
        _sFieldCache = dict;
      }
      return result;
    }

    /// <summary>
    /// Finds methods decorated by [On(De)Seriali(zing/zed)]
    /// </summary>
    /// <param name="t">A type whose methods to search</param>
    /// <param name="atype">Attribute type to search</param>
    /// <returns>List(MethodInfo) that qualifies or NULL if none found</returns>
    public static List<MethodInfo> FindSerializationAttributedMethods(Type t, Type atype)
    {
      var list = new List<MethodInfo>();

      while (t != null && t != typeof(object))
      {
        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(mi => Attribute.IsDefined(mi, atype) &&
                                        mi.ReturnType == typeof(void) &&
                                        mi.GetParameters().Length == 1 &&
                                        mi.GetParameters()[0].ParameterType == typeof(StreamingContext));
        foreach (var m in methods)
          list.Add(m);

        t = t.BaseType;
      }

      if (list.Count > 0)
      {
        list.Reverse(); //because we harvest methods from child -> parent but need to call parent->child order
        return list;
      }

      return null;
    }

    /// <summary>
    /// Calls method in the list that was returned by a call to FindSerializationAttributedMethods
    /// </summary>
    /// <param name="methods">list that was returned by a call to FindSerializationAttributedMethods</param>
    /// <param name="instance">Instance to invoke methods on</param>
    /// <param name="streamingContext">Streaming Context</param>
    public static void InvokeSerializationAttributedMethods(List<MethodInfo> methods, object instance, StreamingContext streamingContext)
    {
      if (instance == null) return;

      for (var i = 0; i < methods.Count; i++)
        try
        {
          methods[i].Invoke(instance, new object[] { streamingContext });
        }
        catch (TargetInvocationException ie)
        {  //20131219 DKh
          if (ie.InnerException != null) throw ie.InnerException;
          throw;
        }
    }


    /// <summary>
    /// Performs an action on each element of a possibly multidimensional array
    /// </summary>
    public static void WalkArrayWrite(Array arr, Action<object> each)
    {
      var rank = arr.Rank;

      if (rank == 1)
      {
        var i = arr.GetLowerBound(0);
        var top = arr.GetUpperBound(0);
        for (; i <= top; i++)
          each(arr.GetValue(i));
        return;
      }


      var idxs = new int[rank];
      DoDimensionGetValue(arr, idxs, 0, each);
    }

    /// <summary>
    /// Performs an action on each element of a possibly multidimensional array
    /// </summary>
    public static void WalkArrayRead<T>(Array arr, Func<T> each)
    {
      Contract.Requires(!(arr is null), $"{nameof(arr)} is not null");
      var rank = arr.Rank;

      if (rank == 1)
      {
        var i = arr.GetLowerBound(0);
        var top = arr.GetUpperBound(0);
        for (; i <= top; i++)
          arr.SetValue(each(), i);
        return;
      }


      var idxs = new int[rank];
      DoDimensionSetValue<T>(arr, idxs, 0, each);
    }


    private static void DoDimensionGetValue(Array arr, int[] idxs, int di, Action<object> each)
    {
      var bot = arr.GetLowerBound(di);
      var top = arr.GetUpperBound(di);
      for (idxs[di] = bot; idxs[di] <= top; idxs[di]++)
      {
        if (di < idxs.Length - 1)
          DoDimensionGetValue(arr, idxs, di + 1, each);
        else
          each(arr.GetValue(idxs));
      }
      idxs[di] = top;
    }

    private static void DoDimensionSetValue<T>(Array arr, int[] idxs, int di, Func<T> each)
    {
      var bot = arr.GetLowerBound(di);
      var top = arr.GetUpperBound(di);
      for (idxs[di] = bot; idxs[di] <= top; idxs[di]++)
      {
        if (di < idxs.Length - 1)
          DoDimensionSetValue(arr, idxs, di + 1, each);
        else
          arr.SetValue(each(), idxs);
      }
      idxs[di] = top;
    }
  }
}
