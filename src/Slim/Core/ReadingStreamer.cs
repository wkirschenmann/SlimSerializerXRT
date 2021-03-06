/*<FILE_LICENSE>
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Diagnostics.CodeAnalysis;

namespace Slim.Core
{
  /// <summary>
  /// Reads primitives from stream
  /// </summary>
  [SuppressMessage("ReSharper", "UnusedMember.Global")]
  [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
  internal abstract class ReadingStreamer : Streamer
  {

    #region Protected

    protected int ReadFromStream(byte[] buffer, int count)
    {
      if (count <= 0) return 0;
      var total = 0;
      do
      {
        var got = Stream.Read(buffer, total, count - total);
        if (got == 0) //EOF
          throw new SlimException(StringConsts.StreamCorruptedError + "ReadFromStream(Need: {0}; Got: {1})".Args(count, total));
        total += got;
      } while (total < count);

      return total;
    }


    #endregion

    #region Public

    public abstract bool ReadBool();
    public abstract bool? ReadNullableBool();

    public byte ReadByte()
    {
      var b = Stream.ReadByte();
      if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadByte(): eof");

      return (byte)b;
    }

    public abstract byte? ReadNullableByte();



    public abstract byte[] ReadByteArray();
    public abstract int[] ReadIntArray();
    public abstract long[] ReadLongArray();
    public abstract double[] ReadDoubleArray();
    public abstract float[] ReadFloatArray();
    public abstract decimal[] ReadDecimalArray();


    public abstract char ReadChar();
    public abstract char? ReadNullableChar();

    public abstract char[] ReadCharArray();
    public abstract string[] ReadStringArray();

    public abstract decimal ReadDecimal();
    public abstract decimal? ReadNullableDecimal();

    public abstract double ReadDouble();
    public abstract double? ReadNullableDouble();

    public abstract float ReadFloat();
    public abstract float? ReadNullableFloat();

    public abstract int ReadInt();
    public abstract int? ReadNullableInt();

    public abstract long ReadLong();
    public abstract long? ReadNullableLong();

    public abstract sbyte ReadSByte();
    public abstract sbyte? ReadNullableSByte();


    public abstract short ReadShort();
    public abstract short? ReadNullableShort();

    public abstract string ReadString();

    public abstract uint ReadUInt();
    public abstract uint? ReadNullableUInt();

    public abstract ulong ReadULong();
    public abstract ulong? ReadNullableULong();

    public abstract ushort ReadUShort();
    public abstract ushort? ReadNullableUShort();

    public abstract MetaHandle ReadMetaHandle();
    public abstract MetaHandle? ReadNullableMetaHandle();

    public abstract DateTime ReadDateTime();
    public abstract DateTime? ReadNullableDateTime();

    public abstract TimeSpan ReadTimeSpan();
    public abstract TimeSpan? ReadNullableTimeSpan();

    public abstract Guid ReadGuid();
    public abstract Guid? ReadNullableGuid();

    public abstract VarIntStr ReadVarIntStr();
    public abstract VarIntStr? ReadNullableVarIntStr();
    #endregion
  }
}
