/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.IO;

namespace Slim.Core
{


  /// <summary>
  /// Provides IO-related utility extensions
  /// </summary>
  public static class IoUtils
  {

    /// <summary>
    /// Reads an integer encoded as big endian from buffer at the specified index
    /// </summary>
    public static ulong ReadBeuInt64(this Stream s)
    {
      UInt64 res = 0;
      for (var i = 0; i < 8; ++i)
      {
        var b = s.ReadByte();
        if (b < 0) throw new SlimException(StringConsts.StreamReadEofError + "ReadBEUInt64()");
        res = (res << 8) + (UInt64)b;
      }
      return res;
    }

    /// <summary>
    /// Writes an unsigned long integer encoded as big endian to the given stream
    /// </summary>
    public static void WriteBeuInt64(this Stream s, UInt64 value)
    {
      for (var i = 0; i < 8; ++i)
      {
        s.WriteByte((byte)(value >> ((7 - i) * 8)));
      }
    }

  }
}
