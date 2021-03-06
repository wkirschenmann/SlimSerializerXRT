/*<FILE_LICENSE>
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Diagnostics;
using System.IO;

namespace Slim.Core
{
  /// <summary>
  /// Facilitates debugging tasks enabled by DEBUG conditional define
  /// </summary>
  public static class Debug
  {
    [Conditional("DEBUG")]
    public static void Assert(bool condition, string text)
    {
      if (condition) return;

      var frame = new StackFrame(1, true);
      var m = frame.GetMethod();
      var from =
        $"{m.DeclaringType.FullName}.{m.Name} at [{Path.GetFileName(frame.GetFileName())}:{frame.GetFileLineNumber()}]";

      if (string.IsNullOrWhiteSpace(text))
        text = "Assertion failure";
      throw new SlimException(text + ":  " + from, from);
    }
  }
}
