using System;
using System.Collections.Generic;

namespace TestData.DataModel
{

  [Serializable]
  public class MyKeyClass : IEquatable<MyKeyClass>
  {
    public string Value { get; set; }

    public override bool Equals(object obj)
    {
      return Equals(obj as MyKeyClass);
    }

    public bool Equals(MyKeyClass other)
    {
      return !(other is null) &&
             Value == other.Value;
    }

    public override int GetHashCode() => -1937169414 + Value is null ? 0 : Value.GetHashCode();

    public override string ToString()
    {
      return $"MyKeyClass-{Value}";
    }

    public static bool operator ==(MyKeyClass lhs, MyKeyClass rhs) => !(lhs is null) && lhs.Equals(rhs);

    public static bool operator !=(MyKeyClass left, MyKeyClass right) => !(left == right);
  }

}
