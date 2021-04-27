/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

namespace SlimSerializer.Core
{
  /// <summary>
  /// Reads primitives and other supported types from Slim-format stream. Use factory method of SlimFormat instance to create a new instance of SlimReader class
  /// </summary>
  public class SlimReader : ReadingStreamer
  {
    protected internal SlimReader() : base(null){ }

    /// <summary>
    /// Returns SlimFormat that this reader implements
    /// </summary>
    public override StreamerFormat Format => SlimFormat.Instance;

    public override bool ReadBool()
    {
      var b = m_Stream.ReadByte();
      if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadBool(): eof");

      return b!=0;
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
      if (len>SlimFormat.MAX_BYTE_ARRAY_LEN)
        throw new SlimException(StringConsts.SLIM_READ_X_ARRAY_MAX_SIZE_ERROR.Args(len, "bytes", SlimFormat.MAX_BYTE_ARRAY_LEN));

      var buf = new byte[len];

      ReadFromStream(buf, len);

      return buf;
    }


    public override int[] ReadIntArray()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var len = this.ReadInt();
      if (len>SlimFormat.MAX_INT_ARRAY_LEN)
        throw new SlimException(StringConsts.SLIM_READ_X_ARRAY_MAX_SIZE_ERROR.Args(len, "ints", SlimFormat.MAX_INT_ARRAY_LEN));

      var result = new int[len];


      for(int i=0; i<len; i++)
        result[i] = this.ReadInt();

      return result;
    }


    public override long[] ReadLongArray()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var len = this.ReadInt();
      if (len>SlimFormat.MAX_LONG_ARRAY_LEN)
        throw new SlimException(StringConsts.SLIM_READ_X_ARRAY_MAX_SIZE_ERROR.Args(len, "longs", SlimFormat.MAX_LONG_ARRAY_LEN));

      var result = new long[len];


      for(int i=0; i<len; i++)
        result[i] = this.ReadLong();

      return result;
    }


    public override double[] ReadDoubleArray()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var len = this.ReadInt();
      if (len>SlimFormat.MAX_DOUBLE_ARRAY_LEN)
        throw new SlimException(StringConsts.SLIM_READ_X_ARRAY_MAX_SIZE_ERROR.Args(len, "doubles", SlimFormat.MAX_DOUBLE_ARRAY_LEN));

      var result = new double[len];


      for(int i=0; i<len; i++)
        result[i] = this.ReadDouble();

      return result;
    }


    public override float[] ReadFloatArray()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var len = this.ReadInt();
      if (len>SlimFormat.MAX_FLOAT_ARRAY_LEN)
        throw new SlimException(StringConsts.SLIM_READ_X_ARRAY_MAX_SIZE_ERROR.Args(len, "floats", SlimFormat.MAX_FLOAT_ARRAY_LEN));

      var result = new float[len];


      for(int i=0; i<len; i++)
        result[i] = this.ReadFloat();

      return result;
    }

    public override decimal[] ReadDecimalArray()
    {
      var has = this.ReadBool();
      if (!has) return null;

      var len = this.ReadInt();
      if (len>SlimFormat.MAX_DECIMAL_ARRAY_LEN)
        throw new SlimException(StringConsts.SLIM_READ_X_ARRAY_MAX_SIZE_ERROR.Args(len, "decimals", SlimFormat.MAX_DECIMAL_ARRAY_LEN));

      var result = new decimal[len];


      for(int i=0; i<len; i++)
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
      byte[] buf = this.ReadByteArray();
      if (buf==null) return null;

      return m_Encoding.GetChars(buf);
    }


    public override string[] ReadStringArray()
    {
      var has = this.ReadBool();
      if (!has) return null;
      var len = this.ReadInt();

      if (len>SlimFormat.MAX_STRING_ARRAY_CNT)
        throw new SlimException(StringConsts.SLIM_READ_X_ARRAY_MAX_SIZE_ERROR.Args(len, "strings", SlimFormat.MAX_STRING_ARRAY_CNT));


      var result = new string[len];

      for(int i=0; i<len; i++)
        result[i] = this.ReadString();

      return result;
    }

    public override decimal ReadDecimal()
    {
      var bits_0 = this.ReadInt();
      var bits_1 = this.ReadInt();
      var bits_2 = this.ReadInt();
      var bits_3 = this.ReadByte();
      return new Decimal(bits_0,
                          bits_1,
                          bits_2,
                          (bits_3 & 0x80) != 0,
                          (byte)(bits_3 & 0x7F));
    }

    public override decimal? ReadNullableDecimal()
    {
      var has = this.ReadBool();

      if (has) return this.ReadDecimal();

      return null;
    }


    public unsafe override double ReadDouble()
    {
      ReadFromStream(m_Buff32, 8);

      uint seg1 = (uint)((int)m_Buff32[0] |
                          (int)m_Buff32[1] << 8 |
                          (int)m_Buff32[2] << 16 |
                          (int)m_Buff32[3] << 24);

      uint seg2 = (uint)((int)m_Buff32[4] |
                          (int)m_Buff32[5] << 8 |
                          (int)m_Buff32[6] << 16 |
                          (int)m_Buff32[7] << 24);

      ulong core = (ulong)seg2 << 32 | (ulong)seg1;

      return *(double*)(&core);
    }

    public override double? ReadNullableDouble()
    {
      var has = this.ReadBool();

      if (has) return this.ReadDouble();

      return null;
    }


    public unsafe override float ReadFloat()
    {
      ReadFromStream(m_Buff32, 4);

      uint core = (uint)((int)m_Buff32[0] |
                          (int)m_Buff32[1] << 8 |
                          (int)m_Buff32[2] << 16 |
                          (int)m_Buff32[3] << 24);
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
      int result = 0;
      var b = m_Stream.ReadByte();
      if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadInt(): eof");

      var neg = ((b & 1) != 0);


      var has = (b & 0x80) > 0;
      result |= ((b & 0x7f) >> 1);
      var bitcnt = 6;

      while(has)
      {
          if (bitcnt>31)
          throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadInt()");

          b = m_Stream.ReadByte();
          if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadInt(): eof");
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
      var b = m_Stream.ReadByte();
      if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadLong(): eof");

      var neg = ((b & 1) != 0);


      var has = (b & 0x80) > 0;
      result |= ((long)(b & 0x7f) >> 1);
      var bitcnt = 6;

      while(has)
      {
          if (bitcnt>63)
          throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadLong()");

          b = m_Stream.ReadByte();
          if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadLong(): eof");
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
      var b = m_Stream.ReadByte();
      if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadSByte(): eof");
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
      var b = m_Stream.ReadByte();
      if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadShort(): eof");

      var neg = ((b & 1) != 0);


      var has = (b & 0x80) > 0;
      result |= (short)((b & 0x7f) >> 1);
      var bitcnt = 6;

      while(has)
      {
          if (bitcnt>15)
          throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadShort()");

          b = m_Stream.ReadByte();
          if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadShort(): eof");
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
      if (bsz<SlimFormat.STR_BUF_SZ)
      {
        if (SlimFormat.ts_StrBuff==null) SlimFormat.ts_StrBuff = new byte[SlimFormat.STR_BUF_SZ];
        ReadFromStream(SlimFormat.ts_StrBuff, bsz);
        return m_Encoding.GetString(SlimFormat.ts_StrBuff, 0, bsz);
      }


      if (bsz>SlimFormat.MAX_BYTE_ARRAY_LEN)
        throw new SlimException(StringConsts.SLIM_READ_X_ARRAY_MAX_SIZE_ERROR.Args(bsz, "string bytes", SlimFormat.MAX_BYTE_ARRAY_LEN));

      var buf = new byte[bsz];

      ReadFromStream(buf, bsz);

      return m_Encoding.GetString(buf);
    }

    public override uint ReadUInt()
    {
      uint result = 0;
      var bitcnt = 0;
      var has = true;

      while(has)
      {
          if (bitcnt>31)
          throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadUInt()");

          var b = m_Stream.ReadByte();
          if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadUInt(): eof");
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

      while(has)
      {
          if (bitcnt>63)
          throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadULong()");

          var b = m_Stream.ReadByte();
          if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadULong(): eof");
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

      while(has)
      {
          if (bitcnt>31)
          throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadUShort()");

          var b = m_Stream.ReadByte();
          if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadUShort(): eof");
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
      uint handle = 0;
      var b = m_Stream.ReadByte();
      if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadMetaHandle(): eof");

      var meta = ((b & 1) != 0);


      var has = (b & 0x80) > 0;
      handle |= ((uint)(b & 0x7f) >> 1);
      var bitcnt = 6;

      while(has)
      {
          if (bitcnt>31)
          throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadMetaHandle()");

          b = m_Stream.ReadByte();
          if (b<0) throw new SlimException(StringConsts.SLIM_STREAM_CORRUPTED_ERROR + "ReadMetaHandle(): eof");
          has = (b & 0x80) > 0;
          handle |= (uint)(b & 0x7f) << bitcnt;
          bitcnt += 7;
      }

      if (meta)
      {
          var sv = ReadString();
          if (sv!=null)
          return new MetaHandle(true, handle, new VarIntStr( sv ));
          else
          return new MetaHandle(true, handle, new VarIntStr( ReadUInt() ));
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
      var ticks = (long) m_Stream.ReadBEUInt64();
      var kind = (DateTimeKind) m_Stream.ReadByte();
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
      if (str!=null) return new VarIntStr(str);

      return new VarIntStr( this.ReadUInt() );
    }

    public override VarIntStr? ReadNullableVarIntStr()
    {
      var has = this.ReadBool();

      if (has) return this.ReadVarIntStr();

      return null;
    }


  }
}
