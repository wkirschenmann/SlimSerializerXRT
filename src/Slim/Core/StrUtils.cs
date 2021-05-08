/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Globalization;

namespace Slim.Core
{
  /// <summary>
  /// Provides core string utility functions used by the majority of projects
  /// </summary>
  public static class StrUtils
  {
    public static readonly string[] WinUnixLineBrakes = new string[] { "\r\n", "\n" };

    /// <summary>
    /// Shortcut helper for string.Format(tpl, params object[] args)
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static string Args(this string tpl, params object[] args)
    {
      return string.Format(CultureInfo.InvariantCulture, tpl, args);
    }

    /// <summary>
    /// Helper function that calls string.IsNullOrWhiteSpace()
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrWhiteSpace(this string s)
    {
      return string.IsNullOrWhiteSpace(s);
    }
  }
}
