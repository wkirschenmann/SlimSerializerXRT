using System;
using System.Diagnostics;

namespace TestData.DataModel
{
  [Serializable]
  internal sealed class AllStructQualifiers<T> : IEquatable<AllStructQualifiers<T>> where T:struct
  {
    private T Value { get; }
    private T[] ArrayValue { get; }
    private T? BoxedValue { get; }
    private T?[] BoxedArrayValue { get; }

    public AllStructQualifiers(int seed, Func<Random, T> initializer)
    {
      var ran = new Random(seed);
      Value = initializer(ran);
      ArrayValue = new T[ran.Next(1, 512)];
      for (var i = 0; i < ArrayValue.Length; ++i)
      {
        ArrayValue[i] = initializer(ran);
      }

      if (ran.NextDouble() > 0.5)
      {
        BoxedValue = initializer(ran);
      }

      BoxedArrayValue = new T?[ran.Next(1, 512)];
      for (var i = 0; i < BoxedArrayValue.Length; ++i)
      {
        if (ran.NextDouble() > 0.5)
        {
          BoxedArrayValue[i] = initializer(ran);
        }
      }
    }
    public bool Equals(AllStructQualifiers<T> other)
    {
      if (other is null) return false;
      if (ReferenceEquals(this, other)) return true;
      return Value.Equals(other.Value) && Equals(ArrayValue, other.ArrayValue) && Nullable.Equals(BoxedValue, other.BoxedValue) && Equals(BoxedArrayValue, other.BoxedArrayValue);
    }

    public override bool Equals(object obj)
    {
      if (obj is null) return false;
      if (ReferenceEquals(this, obj)) return true;
      return obj is AllStructQualifiers<T> otherAsq && Equals(otherAsq);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        var hashCode = Value.GetHashCode();
        hashCode = (hashCode * 397) ^ (ArrayValue != null ? ArrayValue.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ BoxedValue.GetHashCode();
        hashCode = (hashCode * 397) ^ (BoxedArrayValue != null ? BoxedArrayValue.GetHashCode() : 0);
        return hashCode;
      }
    }

    public static bool operator ==(AllStructQualifiers<T> left, AllStructQualifiers<T> right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(AllStructQualifiers<T> left, AllStructQualifiers<T> right)
    {
      return !Equals(left, right);
    }

    public void AssertEqual(AllStructQualifiers<T> other)
    {
      Debug.Assert(!(other is null));

      Debug.Assert(Value.Equals(other.Value), $"Expected {Value} but got {other.Value}");

      Debug.Assert(ArrayValue.Length == other.ArrayValue.Length, $"Expected {ArrayValue.Length} but got {other.ArrayValue.Length}");
      for (var i = 0; i < ArrayValue.Length; i++)
      {
        Debug.Assert(ArrayValue[i].Equals(other.ArrayValue[i]), $"Expected {ArrayValue[i]} but got {other.ArrayValue[i]}");
      }

      Debug.Assert(BoxedValue.Equals(other.BoxedValue), $"Expected {BoxedValue} but got {other.BoxedValue}");
      
      Debug.Assert(BoxedArrayValue.Length == other.BoxedArrayValue.Length, $"Expected {BoxedArrayValue.Length} but got {other.BoxedArrayValue.Length}");
      for (var i = 0; i < BoxedArrayValue.Length; i++)
      {
        Debug.Assert(BoxedArrayValue[i].Equals(other.BoxedArrayValue[i]), $"Expected {BoxedArrayValue[i]} but got {other.BoxedArrayValue[i]}");
      }
    }
  }
}