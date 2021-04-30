/*<FILE_LICENSE>
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimSerializer.Core
{
  internal static class EnumerableExt
  {
    internal static IEnumerable<T> Add<T>(this IEnumerable<T> source, IEnumerable<T> elements)
    {
      foreach (var s in source)
      {
        yield return s;
      }

      foreach (var e in elements)
      {
        yield return e;
      }
    }

    internal static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, T elem)
    {
      yield return elem;
      foreach (var s in source)
      {
        yield return s;
      }
    }
  }

  /// <summary>
  /// Provides core utility functions used by the majority of projects
  /// </summary>
  internal static class CoreUtils
  {
    /// <summary>
    /// Writes exception message with exception type
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    internal static string ToMessageWithType(this Exception error) => 
      error == null ? null : "[{0}] {1}".Args(error.GetType().FullName, error.Message);

    /// <summary>
    /// Returns the name of the type with expanded generic argument names.
    /// This helper is useful for printing class names to logs/messages.
    ///   List'1[System.Object]  ->  List&lt;Object&gt;
    /// </summary>
    internal static string DisplayNameWithExpandedGenericArgs(this Type type)
    {
      var genericArguments = type.GetGenericArguments();

      if (genericArguments.Length == 0)
      {
        return type.Name;
      }

      var sb = new StringBuilder();

      for (var i = 0; i < genericArguments.Length; i++)
      {
        if (i > 0) sb.Append(", ");
        sb.Append(genericArguments[i].DisplayNameWithExpandedGenericArgs());
      }

      var nm = type.Name;
      var idx = nm.IndexOf('`');
      if (idx >= 0)
        nm = nm.Substring(0, idx);


      return "{0}<{1}>".Args(nm, sb.ToString());
    }
  }
}
