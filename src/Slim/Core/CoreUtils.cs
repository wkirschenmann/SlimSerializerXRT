/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Text;

namespace Slim.Core
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
      return error == null ? null : "[{0}] {1}".Args(error.GetType().FullName, error.Message);
    }
  }
}
