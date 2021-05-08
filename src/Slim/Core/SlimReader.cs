/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

namespace Slim.Core
{
  /// <summary>
  /// Reads primitives and other supported types from Slim-format stream. Use factory method of SlimFormat instance to create a new instance of SlimReader class
  /// </summary>
  internal class SlimReader : ReadingStreamer
  {

    public override bool ReadBool()
    {
      var b = Stream.ReadByte();
      if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadBool(): eof");

      return b != 0;
    }

    public override bool? ReadNullableBool()
    {
      var has = this.ReadBool();

      if (has) return this.ReadBool();

      return null;
    }


    public override byte? ReadNullableByte()
    {
      var has = this.ReadBool();

      if (has) return this.ReadByte();

      return null;
    }


    public override byte[] ReadByteArray()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var len = this.ReadInt();
      if (len > SlimFormat.MaxByteArrayLen)
        throw new SlimException(StringConsts.ReadXArrayMaxSizeError.Args(len, "bytes", SlimFormat.MaxByteArrayLen));

      var buf = new byte[len];

      ReadFromStream(buf, len);

      return buf;
    }


    public override int[] ReadIntArray()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var len = this.ReadInt();
      if (len > SlimFormat.MaxIntArrayLen)
        throw new SlimException(StringConsts.ReadXArrayMaxSizeError.Args(len, "ints", SlimFormat.MaxIntArrayLen));

      var result = new int[len];


      for (var i = 0; i < len; i++)
        result[i] = this.ReadInt();

      return result;
    }


    public override long[] ReadLongArray()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var len = this.ReadInt();
      if (len > SlimFormat.MaxLongArrayLen)
        throw new SlimException(StringConsts.ReadXArrayMaxSizeError.Args(len, "longs", SlimFormat.MaxLongArrayLen));

      var result = new long[len];


      for (var i = 0; i < len; i++)
        result[i] = this.ReadLong();

      return result;
    }


    public override double[] ReadDoubleArray()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var len = this.ReadInt();
      if (len > SlimFormat.MaxDoubleArrayLen)
        throw new SlimException(StringConsts.ReadXArrayMaxSizeError.Args(len, "doubles", SlimFormat.MaxDoubleArrayLen));

      var result = new double[len];


      for (var i = 0; i < len; i++)
        result[i] = this.ReadDouble();

      return result;
    }


    public override float[] ReadFloatArray()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var len = this.ReadInt();
      if (len > SlimFormat.MaxFloatArrayLen)
        throw new SlimException(StringConsts.ReadXArrayMaxSizeError.Args(len, "floats", SlimFormat.MaxFloatArrayLen));

      var result = new float[len];


      for (var i = 0; i < len; i++)
        result[i] = this.ReadFloat();

      return result;
    }

    public override decimal[] ReadDecimalArray()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var len = this.ReadInt();
      if (len > SlimFormat.MaxDecimalArrayLen)
        throw new SlimException(StringConsts.ReadXArrayMaxSizeError.Args(len, "decimals", SlimFormat.MaxDecimalArrayLen));

      var result = new decimal[len];


      for (var i = 0; i < len; i++)
        result[i] = this.ReadDecimal();

      return result;
    }


    public override char ReadChar()
    {
      return (char)this.ReadShort();
    }

    public override char? ReadNullableChar()
    {
      var has = this.ReadBool();

      if (has) return this.ReadChar();

      return null;
    }


    public override char[] ReadCharArray()
    {
      var buf = this.ReadByteArray();
      if (buf == null) return null;

      return Encoding.GetChars(buf);
    }


    public override string[] ReadStringArray()
    {
      var has = this.ReadBool();
      if (!has) return null;
      var len = this.ReadInt();

      if (len > SlimFormat.MaxStringArrayCnt)
        throw new SlimException(StringConsts.ReadXArrayMaxSizeError.Args(len, "strings", SlimFormat.MaxStringArrayCnt));


      var result = new string[len];

      for (var i = 0; i < len; i++)
        result[i] = this.ReadString();

      return result;
    }

    public override decimal ReadDecimal()
    {
      var bits0 = this.ReadInt();
      var bits1 = this.ReadInt();
      var bits2 = this.ReadInt();
      var bits3 = this.ReadByte();
      return new Decimal(bits0,
                          bits1,
                          bits2,
                          (bits3 & 0x80) != 0,
                          (byte)(bits3 & 0x7F));
    }

    public override decimal? ReadNullableDecimal()
    {
      var has = this.ReadBool();

      if (has) return this.ReadDecimal();

      return null;
    }


    public override unsafe double ReadDouble()
    {
      ReadFromStream(GetBuff32(), 8);

      var seg1 = (uint)((int)GetBuff32()[0] |
                        (int)GetBuff32()[1] << 8 |
                        (int)GetBuff32()[2] << 16 |
                        (int)GetBuff32()[3] << 24);

      var seg2 = (uint)((int)GetBuff32()[4] |
                        (int)GetBuff32()[5] << 8 |
                        (int)GetBuff32()[6] << 16 |
                        (int)GetBuff32()[7] << 24);

      var core = (ulong)seg2 << 32 | (ulong)seg1;

      return *(double*)(&core);
    }

    public override double? ReadNullableDouble()
    {
      var has = this.ReadBool();

      if (has) return this.ReadDouble();

      return null;
    }


    public override unsafe float ReadFloat()
    {
      ReadFromStream(GetBuff32(), 4);

      var core = (uint)((int)GetBuff32()[0] |
                        (int)GetBuff32()[1] << 8 |
                        (int)GetBuff32()[2] << 16 |
                        (int)GetBuff32()[3] << 24);
      return *(float*)(&core);
    }

    public override float? ReadNullableFloat()
    {
      var has = this.ReadBool();

      if (has) return this.ReadFloat();

      return null;
    }


    public override int ReadInt()
    {
      var result = 0;
      var b = Stream.ReadByte();
      if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadInt(): eof");

      var neg = ((b & 1) != 0);


      var has = (b & 0x80) > 0;
      result |= ((b & 0x7f) >> 1);
      var bitcnt = 6;

      while (has)
      {
        if (bitcnt > 31)
          throw new SlimException(StringConsts.StreamCorruptedError + "ReadInt()");

        b = Stream.ReadByte();
        if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadInt(): eof");
        has = (b & 0x80) > 0;
        result |= (b & 0x7f) << bitcnt;
        bitcnt += 7;
      }

      return neg ? ~result : result;
    }


    public override int? ReadNullableInt()
    {
      var has = this.ReadBool();

      if (has) return this.ReadInt();

      return null;
    }

    public override long ReadLong()
    {
      long result = 0;
      var b = Stream.ReadByte();
      if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadLong(): eof");

      var neg = ((b & 1) != 0);


      var has = (b & 0x80) > 0;
      result |= ((long)(b & 0x7f) >> 1);
      var bitcnt = 6;

      while (has)
      {
        if (bitcnt > 63)
          throw new SlimException(StringConsts.StreamCorruptedError + "ReadLong()");

        b = Stream.ReadByte();
        if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadLong(): eof");
        has = (b & 0x80) > 0;
        result |= (long)(b & 0x7f) << bitcnt;
        bitcnt += 7;
      }

      return neg ? ~result : result;
    }


    public override long? ReadNullableLong()
    {
      var has = this.ReadBool();

      if (has) return this.ReadLong();

      return null;
    }


    public override sbyte ReadSByte()
    {
      var b = Stream.ReadByte();
      if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadSByte(): eof");
      return (sbyte)b;
    }

    public override sbyte? ReadNullableSByte()
    {
      var has = this.ReadBool();

      if (has) return this.ReadSByte();

      return null;
    }


    public override short ReadShort()
    {
      short result = 0;
      var b = Stream.ReadByte();
      if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadShort(): eof");

      var neg = ((b & 1) != 0);


      var has = (b & 0x80) > 0;
      result |= (short)((b & 0x7f) >> 1);
      var bitcnt = 6;

      while (has)
      {
        if (bitcnt > 15)
          throw new SlimException(StringConsts.StreamCorruptedError + "ReadShort()");

        b = Stream.ReadByte();
        if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadShort(): eof");
        has = (b & 0x80) > 0;
        result |= (short)((b & 0x7f) << bitcnt);
        bitcnt += 7;
      }

      return (short)(neg ? ~result : result);
    }

    public override short? ReadNullableShort()
    {
      var has = this.ReadBool();

      if (has) return ReadShort();

      return null;
    }

    public override string ReadString()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var bsz = this.ReadInt();
      if (bsz < SlimFormat.StrBufSz)
      {
        if (SlimFormat.TsStrBuff == null) SlimFormat.TsStrBuff = new byte[SlimFormat.StrBufSz];
        ReadFromStream(SlimFormat.TsStrBuff, bsz);
        return Encoding.GetString(SlimFormat.TsStrBuff, 0, bsz);
      }


      if (bsz > SlimFormat.MaxByteArrayLen)
        throw new SlimException(StringConsts.ReadXArrayMaxSizeError.Args(bsz, "string bytes", SlimFormat.MaxByteArrayLen));

      var buf = new byte[bsz];

      ReadFromStream(buf, bsz);

      return Encoding.GetString(buf);
    }

    public override uint ReadUInt()
    {
      uint result = 0;
      var bitcnt = 0;
      var has = true;

      while (has)
      {
        if (bitcnt > 31)
          throw new SlimException(StringConsts.StreamCorruptedError + "ReadUInt()");

        var b = Stream.ReadByte();
        if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadUInt(): eof");
        has = (b & 0x80) != 0;
        result |= (uint)(b & 0x7f) << bitcnt;
        bitcnt += 7;
      }

      return result;
    }

    public override uint? ReadNullableUInt()
    {
      var has = this.ReadBool();

      if (has) return this.ReadUInt();

      return null;
    }


    public override ulong ReadULong()
    {
      ulong result = 0;
      var bitcnt = 0;
      var has = true;

      while (has)
      {
        if (bitcnt > 63)
          throw new SlimException(StringConsts.StreamCorruptedError + "ReadULong()");

        var b = Stream.ReadByte();
        if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadULong(): eof");
        has = (b & 0x80) > 0;
        result |= (ulong)(b & 0x7f) << bitcnt;
        bitcnt += 7;
      }

      return result;
    }

    public override ulong? ReadNullableULong()
    {
      var has = this.ReadBool();

      if (has) return this.ReadULong();

      return null;
    }

    public override ushort ReadUShort()
    {
      ushort result = 0;
      var bitcnt = 0;
      var has = true;

      while (has)
      {
        if (bitcnt > 31)
          throw new SlimException(StringConsts.StreamCorruptedError + "ReadUShort()");

        var b = Stream.ReadByte();
        if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadUShort(): eof");
        has = (b & 0x80) > 0;
        result |= (ushort)((b & 0x7f) << bitcnt);
        bitcnt += 7;
      }

      return result;
    }

    public override ushort? ReadNullableUShort()
    {
      var has = this.ReadBool();

      if (has) return this.ReadUShort();

      return null;
    }


    public override MetaHandle ReadMetaHandle()
    {
      var handle = 0;
      var b = Stream.ReadByte();
      if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadMetaHandle(): eof");

      var meta = ((b & 1) != 0);


      var has = (b & 0x80) > 0;
      handle |= ((b & 0x7f) >> 1);
      var bitcnt = 6;

      while (has)
      {
        if (bitcnt > 31)
          throw new SlimException(StringConsts.StreamCorruptedError + "ReadMetaHandle()");

        b = Stream.ReadByte();
        if (b < 0) throw new SlimException(StringConsts.StreamCorruptedError + "ReadMetaHandle(): eof");
        has = (b & 0x80) > 0;
        handle |= (b & 0x7f) << bitcnt;
        bitcnt += 7;
      }

      if (meta)
      {
        var sv = ReadString();
        if (sv != null)
          return new MetaHandle(true, handle, new VarIntStr(sv));
        else
          return new MetaHandle(true, handle, new VarIntStr(ReadUInt()));
      }

      return new MetaHandle(true, handle);
    }


    public override MetaHandle? ReadNullableMetaHandle()
    {
      var has = this.ReadBool();

      if (has) return this.ReadMetaHandle();

      return null;
    }



    public override DateTime ReadDateTime()
    {
      var ticks = (long)Stream.ReadBeuInt64();
      var kind = (DateTimeKind)Stream.ReadByte();
      return new DateTime(ticks, kind);
    }

    public override DateTime? ReadNullableDateTime()
    {
      var has = this.ReadBool();

      if (has) return this.ReadDateTime();

      return null;
    }


    public override TimeSpan ReadTimeSpan()
    {
      var ticks = this.ReadLong();
      return TimeSpan.FromTicks(ticks);
    }

    public override TimeSpan? ReadNullableTimeSpan()
    {
      var has = this.ReadBool();

      if (has) return this.ReadTimeSpan();

      return null;
    }


    public override Guid ReadGuid()
    {
      var arr = this.ReadByteArray();
      return new Guid(arr);
    }

    public override Guid? ReadNullableGuid()
    {
      var has = this.ReadBool();

      if (has) return this.ReadGuid();

      return null;
    }

    public override VarIntStr ReadVarIntStr()
    {
      var str = this.ReadString();
      if (str != null) return new VarIntStr(str);

      return new VarIntStr(this.ReadUInt());
    }

    public override VarIntStr? ReadNullableVarIntStr()
    {
      var has = this.ReadBool();

      if (has) return this.ReadVarIntStr();

      return null;
    }


  }
}
