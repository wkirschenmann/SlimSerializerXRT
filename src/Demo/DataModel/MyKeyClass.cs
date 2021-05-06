using System;
using System.Collections.Generic;

namespace DataModel
{

  [Serializable]
  public class MyKeyClass : IEquatable<MyKeyClass>
  {
    public MyKeyClass(){}

    public string Value { get; set; }

    public override bool Equals(object obj)
    {
      return Equals(obj as MyKeyClass);
    }

    public bool Equals(MyKeyClass other)
    {
      return other != null &&
             Value == other.Value;
    }

    public override int GetHashCode()
    {
      return -1937169414 + EqualityComparer<string>.Default.GetHashCode(Value);
    }

    public override string ToString()
    {
      return $"MyKeyClass-{Value}";
    }

    public static bool operator ==(MyKeyClass left, MyKeyClass right)
    {
      return EqualityComparer<MyKeyClass>.Default.Equals(left, right);
    }

    public static bool operator !=(MyKeyClass left, MyKeyClass right)
    {
      return !(left == right);
    }
  }

}
