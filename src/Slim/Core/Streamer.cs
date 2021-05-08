/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Text;
using System.IO;

namespace Slim.Core
{
  /// <summary>
  /// Represents a base for stream readers and writers.
  /// Streamer object instances ARE NOT THREAD-safe
  /// </summary>
  public abstract class Streamer
  {
    public static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(false, false);

    protected Streamer(Encoding encoding = null)
    {
      Encoding = encoding ?? Utf8Encoding;

      Buff32 = SlimFormat.TsBuff32;
      if (Buff32 == null)
      {
        var buf = new byte[32];
        Buff32 = buf;
        SlimFormat.TsBuff32 = buf;
      }

    }

    protected byte[] Buff32 { get; private set; }

    protected Stream Stream { get; private set; }
    protected Encoding Encoding { get; private set; }



    /// <summary>
    /// Returns format that this streamer implements
    /// </summary>
    public abstract StreamerFormat Format
    {
      get;
    }



    /// <summary>
    /// Sets the stream as the target for output/input.
    /// This call must be coupled with UnbindStream()
    /// </summary>
    public void BindStream(Stream stream)
    {
      if (stream == null)
        throw new SlimException(StringConsts.ArgumentError + GetType().FullName + ".BindStream(stream==null)");

      if (Stream != null && Stream != stream)
        throw new SlimException(StringConsts.ArgumentError + GetType().FullName + " must unbind prior stream first");

      Stream = stream;
    }

    /// <summary>
    /// Unbinds the current stream. This call is coupled with BindStream(stream)
    /// </summary>
    public void UnbindStream()
    {
      if (Stream == null) return;

      if (this is WritingStreamer) Stream.Flush();
      Stream = null;
    }

  }
}
