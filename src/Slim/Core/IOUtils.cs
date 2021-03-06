/*<FILE_LICENSE>
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Diagnostics.Contracts;
using System.IO;

namespace Slim.Core
{


  /// <summary>
  /// Provides IO-related utility extensions
  /// </summary>
  internal static class IoUtils
  {

    /// <summary>
    /// Reads an integer encoded as big endian from buffer at the specified index
    /// </summary>
    internal static ulong ReadBeuInt64(this Stream s)
    {
      ulong res = 0;
      for (var i = 0; i < 8; ++i)
      {
        var b = s.ReadByte();
        if (b < 0) throw new SlimException(StringConsts.StreamReadEofError + "ReadBEUInt64()");
        res = (res << 8) + (ulong)b;
      }
      return res;
    }

    /// <summary>
    /// Writes an unsigned long integer encoded as big endian to the given stream
    /// </summary>
    public static void WriteBeuInt64(this Stream s, ulong value)
    {
      Contract.Requires(!(s is null), $"{nameof(s)} is not null");
      for (var i = 0; i < 8; ++i)
      {
        s.WriteByte((byte)(value >> ((7 - i) * 8)));
      }
    }

  }
}
