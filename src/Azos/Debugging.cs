/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.IO;
using System.Diagnostics;

namespace Azos
{
  /// <summary>
  /// Facilitates debugging tasks enabled by DEBUG conditional define
  /// </summary>
  public static class Debug
  {
    public const string DEBUG = "DEBUG";

    [Conditional(DEBUG)]
    public static void Assert(
                              bool condition,
                              string text
                             )
    {
      Debugging.Assert(condition, text);
    }


    /// <summary>
    /// Facilitates debugging tasks that do not depend on any conditional defines
    /// </summary>
    public static class Debugging
    {

      public static void Assert(bool condition, string text)
      {
        if (condition) return;

          var frame = new StackFrame(1, true);
          var m = frame.GetMethod();
          var from = string.Format("{0}.{1} at [{2}:{3}]",
                            m.DeclaringType.FullName, m.Name,
                            Path.GetFileName(frame.GetFileName()), frame.GetFileLineNumber());

        if (string.IsNullOrWhiteSpace(text))
          text = StringConsts.ASSERTION_ERROR; // Could be either Debug or Trace

        throw new DebugAssertionException(text + ":  " + from, from);
      }
    }
  }
}
