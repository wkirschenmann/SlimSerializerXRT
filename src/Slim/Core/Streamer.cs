/*<FILE_LICENSE>
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.IO;
using System.Text;

namespace Slim.Core
{
  /// <summary>
  /// Represents a base for stream readers and writers.
  /// Streamer object instances ARE NOT THREAD-safe
  /// </summary>
  public abstract class Streamer
  {
    public static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(false, false);

    protected Streamer()
    {
      if (SlimFormat.TsBuff32 is null)
      {
        SlimFormat.TsBuff32 = new byte[32];
      }
    }

#pragma warning disable CA1822 // Mark members as static
    protected byte[] GetBuff32() // buffer lifecycle is handled by the constructor
#pragma warning restore CA1822 // Mark members as static
    {
      return SlimFormat.TsBuff32;
    }

    protected Stream Stream { get; private set; }
    protected Encoding Encoding { get; } = Utf8Encoding;


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
