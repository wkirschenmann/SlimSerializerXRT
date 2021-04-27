/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Text;

namespace SlimSerializer.Core
{
  /// <summary>
  /// Provides core utility functions used by the majority of projects
  /// </summary>
  public static class CoreUtils
  {
    /// <summary>
    /// Writes exception message with exception type
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static string ToMessageWithType(this Exception error)
    {
      if (error == null) return null;
      return "[{0}] {1}".Args(error.GetType().FullName, error.Message);
    }

    /// <summary>
    /// Returns the name of the type with expanded generic argument names.
    /// This helper is useful for printing class names to logs/messages.
    ///   List'1[System.Object]  ->  List&lt;Object&gt;
    /// </summary>
    public static string DisplayNameWithExpandedGenericArgs(this Type type)
    {

      var gargs = type.GetGenericArguments();

      if (gargs.Length == 0)
      {
        return type.Name;
      }

      var sb = new StringBuilder();

      for (int i = 0; i < gargs.Length; i++)
      {
        if (i > 0) sb.Append(", ");
        sb.Append(gargs[i].DisplayNameWithExpandedGenericArgs());
      }

      var nm = type.Name;
      var idx = nm.IndexOf('`');
      if (idx >= 0)
        nm = nm.Substring(0, idx);


      return "{0}<{1}>".Args(nm, sb.ToString());
    }
  }
}
