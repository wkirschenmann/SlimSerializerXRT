using System;
using System.Collections.Generic;
using System.Linq;

namespace TestData.DataModel
{
  internal class MvcComparerInternals : IEqualityComparer<KeyValuePair<string, double[]>>,
    IEqualityComparer<KeyValuePair<string, DateTime>>,
    IEqualityComparer<KeyValuePair<string, string>>
  {
    public static readonly MvcComparerInternals Instance = new MvcComparerInternals();
    public bool Equals(KeyValuePair<string, double[]> x, KeyValuePair<string, double[]> y)
    {
      return x.Key != null && x.Key.Equals(y.Key, StringComparison.Ordinal) && x.Value.SequenceEqual(y.Value);
    }

    public int GetHashCode(KeyValuePair<string, double[]> obj)
    {
      return (obj.Key is null ? 0 : obj.Key.GetHashCode()) + (obj.Value is null ? 0 : obj.Value.GetHashCode());
    }

    public bool Equals(KeyValuePair<string, DateTime> x, KeyValuePair<string, DateTime> y)
    {
      return x.Key != null && x.Key.Equals(y.Key, StringComparison.Ordinal) && x.Value.Equals(y.Value);
    }

    public int GetHashCode(KeyValuePair<string, DateTime> obj)
    {
      return (obj.Key is null ? 0 : obj.Key.GetHashCode()) + (obj.Value.GetHashCode());
    }

    public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
    {
      return x.Key != null && x.Key.Equals(y.Key, StringComparison.Ordinal) && x.Value.Equals(y.Value, StringComparison.Ordinal);
    }

    public int GetHashCode(KeyValuePair<string, string> obj)
    {
      return (obj.Key is null ? 0 : obj.Key.GetHashCode()) + (obj.Value is null ? 0 : obj.Value.GetHashCode());    }
  }

  [Serializable]
  public class MyValueClass : IEquatable<MyValueClass>
  {
    public Dictionary<string, double[]> Values1 { get; set; } = new Dictionary<string, double[]>(StringComparer.Ordinal);
    public Dictionary<string, DateTime> Values2 { get; set; } = new Dictionary<string, DateTime>(StringComparer.Ordinal);
    public Dictionary<string, string> Values3 { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public override string ToString()
    {
      return "MyValueClass{\n" +
        "Values1=\n{\n\t" +
          "{" + string.Join("},\n\t{", Values1.Select(kvp => kvp.Key + ", {" + string.Join(", ", kvp.Value) + "}")) + "}" +
        "},\n" +
        "Values2=\n{\n\t" +
          "{" + string.Join("},\n\t{", Values2.Select(kvp => $"{kvp.Key}, {kvp.Value}")) + "}" +
        "},\n" +
        "Values3=\n{\n\t" +
          "{" + string.Join("},\n\t{", Values3.Select(kvp => $"{kvp.Key}, {kvp.Value}")) + "}" +
        "}";
    }

    public bool Equals(MyValueClass other)
    {
      if (other is null) return false;
      if (ReferenceEquals(this, other)) return true;
      return Values1.OrderByDescending(kvp=>kvp.Key)
                    .SequenceEqual(other.Values1.OrderByDescending(kvp=>kvp.Key),MvcComparerInternals.Instance) &&
             Values2.OrderByDescending(kvp=>kvp.Key)
                    .SequenceEqual(other.Values2.OrderByDescending(kvp=>kvp.Key),MvcComparerInternals.Instance) &&
             Values3.OrderByDescending(kvp=>kvp.Key)
                    .SequenceEqual(other.Values3.OrderByDescending(kvp=>kvp.Key),MvcComparerInternals.Instance)
             ;
    }

    public override bool Equals(object obj)
    {
      if (obj is null) return false;
      if (ReferenceEquals(this, obj)) return true;
      return obj.GetType() == this.GetType() && Equals((MyValueClass) obj);
    }

    public static bool operator ==(MyValueClass lhs, MyValueClass rhs) => lhs != null && lhs.Equals(rhs);

    public static bool operator !=(MyValueClass lhs, MyValueClass rhs) => !(lhs == rhs);

    public override int GetHashCode()
    {
      unchecked
      {
        var hashCode = (Values1 != null ? Values1.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Values2 != null ? Values2.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Values3 != null ? Values3.GetHashCode() : 0);
        return hashCode;
      }
    }
  }

}
