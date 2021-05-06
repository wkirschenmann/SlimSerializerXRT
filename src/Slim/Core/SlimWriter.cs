/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

namespace Slim.Core
{
  /// <summary>
  /// Writes primitives and other supported types to Slim-format stream. Use factory method of SlimFormat instance to create a new instance of SlimWriter class
  /// </summary>
  public class SlimWriter : WritingStreamer
  {
    protected internal SlimWriter() : base(null) { }


    /// <summary>
    /// Returns SlimFormat that this writer implements
    /// </summary>
    public override StreamerFormat Format => SlimFormat.Instance;


    public override void Flush()
    {
      Stream.Flush();
    }


    public override void Write(bool value)
    {
      Stream.WriteByte(value ? (byte)0xff : (byte)0);
    }

    public override void Write(bool? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }

    public override void Write(byte? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        this.Write(value.Value);
        return;
      }
      this.Write(false);
    }


    public override void Write(byte[] buffer)
    {
      if (buffer == null)
      {
        this.Write(false);
        return;
      }
      this.Write(true);
      var len = buffer.Length;
      if (len > SlimFormat.MaxByteArrayLen)
        throw new SlimException(StringConsts.WriteXArrayMaxSizeError.Args(len, "bytes", SlimFormat.MaxByteArrayLen));

      this.Write(len);
      Stream.Write(buffer, 0, len);
    }


    public override void Write(int[] value)
    {
      if (value == null)
      {
        this.Write(false);
        return;
      }
      this.Write(true);
      var len = value.Length;
      if (len > SlimFormat.MaxIntArrayLen)
        throw new SlimException(StringConsts.WriteXArrayMaxSizeError.Args(len, "ints", SlimFormat.MaxIntArrayLen));

      this.Write(len);
      for (int i = 0; i < len; i++)
        this.Write(value[i]); //WITH compression
    }


    public override void Write(long[] value)
    {
      if (value == null)
      {
        this.Write(false);
        return;
      }
      this.Write(true);
      var len = value.Length;
      if (len > SlimFormat.MaxLongArrayLen)
        throw new SlimException(StringConsts.WriteXArrayMaxSizeError.Args(len, "longs", SlimFormat.MaxLongArrayLen));

      this.Write(len);
      for (int i = 0; i < len; i++)
        this.Write(value[i]); //WITH compression
    }


    public override void Write(double[] value)
    {
      if (value == null)
      {
        this.Write(false);
        return;
      }
      this.Write(true);
      var len = value.Length;
      if (len > SlimFormat.MaxDoubleArrayLen)
        throw new SlimException(StringConsts.WriteXArrayMaxSizeError.Args(len, "doubles", SlimFormat.MaxDoubleArrayLen));

      this.Write(len);
      for (int i = 0; i < len; i++)
        this.Write(value[i]);
    }

    public override void Write(float[] value)
    {
      if (value == null)
      {
        this.Write(false);
        return;
      }
      this.Write(true);
      var len = value.Length;
      if (len > SlimFormat.MaxFloatArrayLen)
        throw new SlimException(StringConsts.WriteXArrayMaxSizeError.Args(len, "floats", SlimFormat.MaxFloatArrayLen));

      this.Write(len);
      for (int i = 0; i < len; i++)
        this.Write(value[i]);
    }

    public override void Write(decimal[] value)
    {
      if (value == null)
      {
        this.Write(false);
        return;
      }
      this.Write(true);
      var len = value.Length;
      if (len > SlimFormat.MaxDecimalArrayLen)
        throw new SlimException(StringConsts.WriteXArrayMaxSizeError.Args(len, "decimals", SlimFormat.MaxDecimalArrayLen));

      this.Write(len);
      for (int i = 0; i < len; i++)
        this.Write(value[i]);
    }


    public override void Write(char ch)
    {
      this.Write((short)ch);
    }

    public override void Write(char? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }



    public override void Write(char[] buffer)
    {
      if (buffer == null)
      {
        this.Write(false);
        return;
      }

      var buf = Encoding.GetBytes(buffer);
      this.Write(buf);
    }


    public override void Write(string[] array)
    {
      if (array == null)
      {
        this.Write(false);
        return;
      }
      this.Write(true);
      var len = array.Length;
      if (len > SlimFormat.MaxStringArrayCnt)
        throw new SlimException(StringConsts.WriteXArrayMaxSizeError.Args(len, "strings", SlimFormat.MaxStringArrayCnt));

      this.Write(len);
      for (int i = 0; i < len; i++)
        this.Write(array[i]);
    }

    public override void Write(decimal value)
    {
      var bits = decimal.GetBits(value);
      this.Write(bits[0]);
      this.Write(bits[1]);
      this.Write(bits[2]);

      byte sign = (bits[3] & 0x80000000) != 0 ? (byte)0x80 : (byte)0x00;
      byte scale = (byte)((bits[3] >> 16) & 0x7F);

      this.Write((byte)(sign | scale));
    }

    public override void Write(decimal? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }


    public unsafe override void Write(double value)
    {
      ulong core = *(ulong*)(&value);

      Buff32[0] = (byte)core;
      Buff32[1] = (byte)(core >> 8);
      Buff32[2] = (byte)(core >> 16);
      Buff32[3] = (byte)(core >> 24);
      Buff32[4] = (byte)(core >> 32);
      Buff32[5] = (byte)(core >> 40);
      Buff32[6] = (byte)(core >> 48);
      Buff32[7] = (byte)(core >> 56);

      Stream.Write(Buff32, 0, 8);
    }

    public override void Write(double? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }


    public unsafe override void Write(float value)
    {
      uint core = *(uint*)(&value);
      Buff32[0] = (byte)core;
      Buff32[1] = (byte)(core >> 8);
      Buff32[2] = (byte)(core >> 16);
      Buff32[3] = (byte)(core >> 24);
      Stream.Write(Buff32, 0, 4);
    }

    public override void Write(float? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }


    public override void Write(int value)
    {
      byte b = 0;

      if (value < 0)
      {
        b = 1;
        value = ~value;//turn off minus bit but dont +1
      }

      b = (byte)(b | ((value & 0x3f) << 1));
      value = value >> 6;
      var has = value != 0;
      if (has)
        b = (byte)(b | 0x80);
      Stream.WriteByte(b);
      while (has)
      {
        b = (byte)(value & 0x7f);
        value = value >> 7;
        has = value != 0;
        if (has)
          b = (byte)(b | 0x80);
        Stream.WriteByte(b);
      }
    }

    public override void Write(int? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }

    public override void Write(long value)
    {
      byte b = 0;

      if (value < 0)
      {
        b = 1;
        value = ~value;//turn off minus bit but dont +1
      }

      b = (byte)(b | ((value & 0x3f) << 1));
      value = value >> 6;
      var has = value != 0;
      if (has)
        b = (byte)(b | 0x80);
      Stream.WriteByte(b);
      while (has)
      {
        b = (byte)(value & 0x7f);
        value = value >> 7;
        has = value != 0;
        if (has)
          b = (byte)(b | 0x80);
        Stream.WriteByte(b);
      }
    }

    public override void Write(long? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }


    public override void Write(sbyte value)
    {
      Stream.WriteByte((byte)value);
    }

    public override void Write(sbyte? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }


    public override void Write(short value)
    {
      byte b = 0;

      if (value < 0)
      {
        b = 1;
        value = (short)~value;//turn off minus bit but dont +1
      }

      b = (byte)(b | ((value & 0x3f) << 1));
      value = (short)(value >> 6);
      var has = value != 0;
      if (has)
        b = (byte)(b | 0x80);
      Stream.WriteByte(b);
      while (has)
      {
        b = (byte)(value & 0x7f);
        value = (short)(value >> 7);
        has = value != 0;
        if (has)
          b = (byte)(b | 0x80);
        Stream.WriteByte(b);
      }
    }

    public override void Write(short? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }


    public override void Write(string value)
    {
      if (value == null)
      {
        this.Write(false);
        return;
      }

      this.Write(true);

      var len = value.Length;
      if (len > SlimFormat.MaxStrLen)//This is much faster than Encoding.GetByteCount()
      {
        var buf = Encoding.GetBytes(value);
        this.Write(buf.Length);
        Stream.Write(buf, 0, buf.Length);
        return;
      }

      //try to reuse pre-allocated buffer
      if (SlimFormat.TsStrBuff == null) SlimFormat.TsStrBuff = new byte[SlimFormat.StrBufSz];
      var bcnt = Encoding.GetBytes(value, 0, len, SlimFormat.TsStrBuff, 0);

      this.Write(bcnt);
      Stream.Write(SlimFormat.TsStrBuff, 0, bcnt);
    }

    public override void Write(uint value)
    {
      var has = true;
      while (has)
      {
        byte b = (byte)(value & 0x7f);
        value = value >> 7;
        has = value != 0;
        if (has)
          b = (byte)(b | 0x80);
        Stream.WriteByte(b);
      }
    }

    public override void Write(uint? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }

    public override void Write(ulong value)
    {
      var has = true;
      while (has)
      {
        byte b = (byte)(value & 0x7f);
        value = value >> 7;
        has = value != 0;
        if (has)
          b = (byte)(b | 0x80);
        Stream.WriteByte(b);
      }
    }

    public override void Write(ulong? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }


    public override void Write(ushort value)
    {
      var has = true;
      while (has)
      {
        byte b = (byte)(value & 0x7f);
        value = (ushort)(value >> 7);
        has = value != 0;
        if (has)
          b = (byte)(b | 0x80);
        Stream.WriteByte(b);
      }
    }

    public override void Write(ushort? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }



    public override void Write(MetaHandle value)
    {
      var meta = value.Metadata.HasValue;

      var handle = value.m_Handle;

      byte b = 0;

      if (meta) b = 1;

      b = (byte)(b | ((handle & 0x3f) << 1));
      handle = (handle >> 6);
      var has = handle != 0;
      if (has)
        b = (byte)(b | 0x80);
      Stream.WriteByte(b);
      while (has)
      {
        b = (byte)(handle & 0x7f);
        handle = (handle >> 7);
        has = handle != 0;
        if (has)
          b = (byte)(b | 0x80);
        Stream.WriteByte(b);
      }

      if (meta)
      {
        var vis = value.Metadata.Value;
        this.Write(vis.StringValue);

        if (vis.StringValue == null)
          this.Write(vis.IntValue);
      }
    }


    public override void Write(MetaHandle? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }



    public override void Write(DateTime value)
    {
      //Prior to 20150626 DKh
      //this.Write(value.ToBinary());
      Stream.WriteBeuInt64((ulong)value.Ticks);
      Stream.WriteByte((byte)value.Kind);
    }

    public override void Write(DateTime? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }


    public override void Write(TimeSpan value)
    {
      this.Write(value.Ticks);
    }

    public override void Write(TimeSpan? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }

    public override void Write(Guid value)
    {
      this.Write(value.ToByteArray());
    }

    public override void Write(Guid? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }

    public override void Write(VarIntStr value)
    {
      this.Write(value.StringValue);
      if (value.StringValue == null)
        this.Write(value.IntValue);
    }

    public override void Write(VarIntStr? value)
    {
      if (value.HasValue)
      {
        this.Write(true);
        Write(value.Value);
        return;
      }
      this.Write(false);
    }

  }
}
