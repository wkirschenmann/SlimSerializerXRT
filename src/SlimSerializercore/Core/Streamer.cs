/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Text;
using System.IO;

namespace SlimSerializer.Core
{
  /// <summary>
  /// Represents a base for stream readers and writers.
  /// Streamer object instances ARE NOT THREAD-safe
  /// </summary>
  internal abstract class Streamer
  {
    public static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(false, false);

    protected Streamer(Encoding encoding = null)
    {
      this.encoding = encoding ?? Utf8Encoding;

      Buff32 = SlimFormat.TsBuff32;
      if (Buff32 == null)
      {
        var buf = new byte[32];
        Buff32 = buf;
        SlimFormat.TsBuff32 = buf;
      }

    }

    protected byte[] Buff32;

    protected Stream stream;
    protected Encoding encoding;



    /// <summary>
    /// Returns format that this streamer implements
    /// </summary>
    internal abstract StreamerFormat Format
    {
      get;
    }


    /// <summary>
    /// Returns underlying stream if it is bound or null
    /// </summary>
    public Stream Stream => stream;

    /// <summary>
    /// Returns stream string encoding
    /// </summary>
    public Encoding Encoding => encoding;


    /// <summary>
    /// Sets the stream as the target for output/input.
    /// This call must be coupled with UnbindStream()
    /// </summary>
    public void BindStream(Stream stream)
    {
      if (stream == null)
        throw new SlimException(StringConsts.ArgumentError + GetType().FullName + ".BindStream(stream==null)");

      if (this.stream != null && this.stream != stream)
        throw new SlimException(StringConsts.ArgumentError + GetType().FullName + " must unbind prior stream first");

      this.stream = stream;
    }

    /// <summary>
    /// Unbinds the current stream. This call is coupled with BindStream(stream)
    /// </summary>
    public void UnbindStream()
    {
      if (stream == null) return;

      if (this is WritingStreamer) stream.Flush();
      stream = null;
    }

  }
}
