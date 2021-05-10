/*<FILE_LICENSE>
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Globalization;

namespace Slim.Core
{
  /// <summary>
  /// Provides core string utility functions used by the majority of projects
  /// </summary>
  public static class StrUtils
  {
    /// <summary>
    /// Shortcut helper for string.Format(tpl, params object[] args)
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static string Args(this string tpl, params object[] args)
    {
      return string.Format(CultureInfo.InvariantCulture, tpl, args);
    }
  }
}
