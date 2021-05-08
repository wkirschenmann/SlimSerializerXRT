using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TestData.DataModel
{
  [Serializable]
  public class MyDataSet : IEquatable<MyDataSet>
  {
    public Dictionary<MyKeyClass, MyValueClass> Value { get; set; }

    public string Name { get; set; }
    public override string ToString()
    {
      return "MyDataSet[" + Name + "]\n{\n" + string.Join(",\n", Value.Select(kvp => kvp.Key + ", " + kvp.Value)) + "\n}";
    }

    public bool Equals(MyDataSet other)
    {
      if (other is null) return false;
      if (ReferenceEquals(this, other)) return true;
      return Name == other.Name && 
             Value.Count == other.Value.Count && 
             Value.Zip(other.Value, (_1, _2)=>
               {
                 return _1.Key.Equals(_2.Key) && _1.Value.Equals(_2.Value);
               })
                  .All(_=>_);
    }

    public static bool operator ==(MyDataSet lhs, MyDataSet rhs) => !(lhs is null) && lhs.Equals(rhs);

    public static bool operator !=(MyDataSet lhs, MyDataSet rhs) => !(lhs == rhs);

    public override bool Equals(object obj)
    {
      if (obj is null) return false;
      if (ReferenceEquals(this, obj)) return true;
      return obj.GetType() == this.GetType() && Equals((MyDataSet) obj);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0);
      }
    }



  }

}
