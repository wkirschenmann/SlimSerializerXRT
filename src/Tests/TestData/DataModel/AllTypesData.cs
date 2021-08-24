using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestData.DataModel
{
  [Serializable]
  public class AllTypesData : IEquatable<AllTypesData>
  {
    private readonly AllStructQualifiers<bool>     m_BoolValues;
    private readonly AllStructQualifiers<byte>     m_ByteValues;
    private readonly AllStructQualifiers<sbyte>    m_SbyteValues;
    private readonly AllStructQualifiers<short>    m_ShortValues;
    private readonly AllStructQualifiers<ushort>   m_UshortValues;
    private readonly AllStructQualifiers<int>      m_IntValues;
    private readonly AllStructQualifiers<uint>     m_UintValues;
    private readonly AllStructQualifiers<long>     m_LongValues;
    private readonly AllStructQualifiers<ulong>    m_UlongValues;
    private readonly AllStructQualifiers<char>     m_CharValues;
    private readonly AllStructQualifiers<double>   m_DoubleValues;
    private readonly AllStructQualifiers<float>    m_FloatValues;
    private readonly AllStructQualifiers<decimal>  m_DecimalValues;
    private readonly AllStructQualifiers<DateTime> m_DateTimeValues;
    private readonly AllStructQualifiers<TimeSpan> m_TimeSpanValues;
    private readonly AllStructQualifiers<Guid>     m_GuidValues;
    private readonly string                        m_StringVal;

    private static unsafe T Initializer<T>(Random r) where T : unmanaged
    {
      var buf = new byte[sizeof(T)];
      r.NextBytes(buf);
      fixed (byte* ptr = &buf[0])
      {
        return *(T*) ptr;
      }
    }

    public override bool Equals(object obj)
    {
      return Equals(obj as AllTypesData);
    }

    public bool Equals(AllTypesData other)
    {
      return other != null &&
             EqualityComparer<AllStructQualifiers<bool>>.Default.Equals(m_BoolValues, other.m_BoolValues) &&
             EqualityComparer<AllStructQualifiers<byte>>.Default.Equals(m_ByteValues, other.m_ByteValues) &&
             EqualityComparer<AllStructQualifiers<sbyte>>.Default.Equals(m_SbyteValues, other.m_SbyteValues) &&
             EqualityComparer<AllStructQualifiers<short>>.Default.Equals(m_ShortValues, other.m_ShortValues) &&
             EqualityComparer<AllStructQualifiers<ushort>>.Default.Equals(m_UshortValues, other.m_UshortValues) &&
             EqualityComparer<AllStructQualifiers<int>>.Default.Equals(m_IntValues, other.m_IntValues) &&
             EqualityComparer<AllStructQualifiers<uint>>.Default.Equals(m_UintValues, other.m_UintValues) &&
             EqualityComparer<AllStructQualifiers<long>>.Default.Equals(m_LongValues, other.m_LongValues) &&
             EqualityComparer<AllStructQualifiers<ulong>>.Default.Equals(m_UlongValues, other.m_UlongValues) &&
             EqualityComparer<AllStructQualifiers<char>>.Default.Equals(m_CharValues, other.m_CharValues) &&
             EqualityComparer<AllStructQualifiers<double>>.Default.Equals(m_DoubleValues, other.m_DoubleValues) &&
             EqualityComparer<AllStructQualifiers<float>>.Default.Equals(m_FloatValues, other.m_FloatValues) &&
             EqualityComparer<AllStructQualifiers<decimal>>.Default.Equals(m_DecimalValues, other.m_DecimalValues) &&
             EqualityComparer<AllStructQualifiers<DateTime>>.Default.Equals(m_DateTimeValues, other.m_DateTimeValues) &&
             EqualityComparer<AllStructQualifiers<TimeSpan>>.Default.Equals(m_TimeSpanValues, other.m_TimeSpanValues) &&
             EqualityComparer<AllStructQualifiers<Guid>>.Default.Equals(m_GuidValues, other.m_GuidValues) &&
             m_StringVal == other.m_StringVal;
    }

    public override int GetHashCode()
    {
      var hashCode = -1021751340;
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<bool>>.Default.GetHashCode(m_BoolValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<byte>>.Default.GetHashCode(m_ByteValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<sbyte>>.Default.GetHashCode(m_SbyteValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<short>>.Default.GetHashCode(m_ShortValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<ushort>>.Default.GetHashCode(m_UshortValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<int>>.Default.GetHashCode(m_IntValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<uint>>.Default.GetHashCode(m_UintValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<long>>.Default.GetHashCode(m_LongValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<ulong>>.Default.GetHashCode(m_UlongValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<char>>.Default.GetHashCode(m_CharValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<double>>.Default.GetHashCode(m_DoubleValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<float>>.Default.GetHashCode(m_FloatValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<decimal>>.Default.GetHashCode(m_DecimalValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<DateTime>>.Default.GetHashCode(m_DateTimeValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<TimeSpan>>.Default.GetHashCode(m_TimeSpanValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<AllStructQualifiers<Guid>>.Default.GetHashCode(m_GuidValues);
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(m_StringVal);
      return hashCode;
    }

    public AllTypesData(int seed)
    {
      m_BoolValues = new AllStructQualifiers<bool>(seed, r => r.Next(0, 100) > 50);
      m_ByteValues = new AllStructQualifiers<byte>(seed, Initializer<byte>);
      m_SbyteValues = new AllStructQualifiers<sbyte>(seed, Initializer<sbyte>);
      m_ShortValues = new AllStructQualifiers<short>(seed, Initializer<short>);
      m_UshortValues = new AllStructQualifiers<ushort>(seed, Initializer<ushort>);
      m_IntValues = new AllStructQualifiers<int>(seed, Initializer<int>);
      m_UintValues = new AllStructQualifiers<uint>(seed, Initializer<uint>);
      m_LongValues = new AllStructQualifiers<long>(seed, Initializer<long>);
      m_UlongValues = new AllStructQualifiers<ulong>(seed, Initializer<ulong>);
      m_CharValues = new AllStructQualifiers<char>(seed, r => (char) Initializer<byte>(r));
      m_DoubleValues = new AllStructQualifiers<double>(seed, r => r.NextDouble());
      m_FloatValues = new AllStructQualifiers<float>(seed, r => (float) r.NextDouble());
      m_DecimalValues = new AllStructQualifiers<decimal>(seed, r => (decimal) r.NextDouble());
      m_DateTimeValues = new AllStructQualifiers<DateTime>(seed,
        r => new DateTime(
          r.Next(1, 2050), r.Next(1, 12), r.Next(1, 29), 
          r.Next(24), r.Next(60), r.Next(60)));
      m_TimeSpanValues =
        new AllStructQualifiers<TimeSpan>(seed, r => 
          new TimeSpan(r.Next(29), r.Next(24), r.Next(60), r.Next(1000)));
      m_GuidValues = new AllStructQualifiers<Guid>(seed, r =>
      {
        var bytes = new byte[16];
        r.NextBytes(bytes);
        return new Guid(bytes);
      });
      m_StringVal = seed.ToString(CultureInfo.InvariantCulture);
    }

    public static bool operator ==(AllTypesData left, AllTypesData right)
    {
      return EqualityComparer<AllTypesData>.Default.Equals(left, right);
    }

    public static bool operator !=(AllTypesData left, AllTypesData right)
    {
      return !(left == right);
    }

    
    [Conditional("DEBUG")]
    public void AssertEqual(object oth)
    {
      Debug.Assert(!(oth is null));
      var other = oth as AllTypesData;
      Debug.Assert(!(other is null));
      m_BoolValues.AssertEqual(other.m_BoolValues);
      m_ByteValues.AssertEqual(other.m_ByteValues);
      m_SbyteValues.AssertEqual(other.m_SbyteValues);
      m_ShortValues.AssertEqual(other.m_ShortValues);
      m_UshortValues.AssertEqual(other.m_UshortValues);
      m_IntValues.AssertEqual(other.m_IntValues);
      m_UintValues.AssertEqual(other.m_UintValues);
      m_LongValues.AssertEqual(other.m_LongValues);
      m_UlongValues.AssertEqual(other.m_UlongValues);
      m_CharValues.AssertEqual(other.m_CharValues);
      m_DoubleValues.AssertEqual(other.m_DoubleValues);
      m_FloatValues.AssertEqual(other.m_FloatValues);
      m_DecimalValues.AssertEqual(other.m_DecimalValues);
      m_DateTimeValues.AssertEqual(other.m_DateTimeValues);
      m_TimeSpanValues.AssertEqual(other.m_TimeSpanValues);
      m_GuidValues.AssertEqual(other.m_GuidValues);
      Debug.Assert(m_StringVal == other.m_StringVal);
    }
  }
}
