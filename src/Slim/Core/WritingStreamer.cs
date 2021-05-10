/*<FILE_LICENSE>
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

namespace Slim.Core
{
  /// <summary>
  /// Writes primitives to stream
  /// </summary>
  internal abstract class WritingStreamer : Streamer
  {
    #region Public
    public abstract void Flush();

    public abstract void Write(bool value);
    public abstract void Write(bool? value);


    public void Write(byte value)
    {
      Stream.WriteByte(value);
    }

    public abstract void Write(byte? value);



    public abstract void Write(byte[] buffer);
    public abstract void Write(int[] value);
    public abstract void Write(long[] value);
    public abstract void Write(double[] value);
    public abstract void Write(float[] value);
    public abstract void Write(decimal[] value);


    public abstract void Write(char ch);
    public abstract void Write(char? value);


    public abstract void Write(char[] buffer);
    public abstract void Write(string[] array);

    public abstract void Write(decimal value);
    public abstract void Write(decimal? value);



    public abstract void Write(double value);
    public abstract void Write(double? value);

    public abstract void Write(float value);
    public abstract void Write(float? value);

    public abstract void Write(int value);
    public abstract void Write(int? value);

    public abstract void Write(long value);
    public abstract void Write(long? value);

    public abstract void Write(sbyte value);
    public abstract void Write(sbyte? value);

    public abstract void Write(short value);
    public abstract void Write(short? value);

    public abstract void Write(string value);

    public abstract void Write(uint value);
    public abstract void Write(uint? value);

    public abstract void Write(ulong value);
    public abstract void Write(ulong? value);

    public abstract void Write(ushort value);
    public abstract void Write(ushort? value);

    public abstract void Write(MetaHandle value);
    public abstract void Write(MetaHandle? value);

    public abstract void Write(DateTime value);
    public abstract void Write(DateTime? value);

    public abstract void Write(TimeSpan value);
    public abstract void Write(TimeSpan? value);

    public abstract void Write(Guid value);
    public abstract void Write(Guid? value);

    public abstract void Write(VarIntStr value);
    public abstract void Write(VarIntStr? value);

    #endregion
  }
}
