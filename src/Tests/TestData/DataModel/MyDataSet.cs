using System;
using System.Collections.Generic;
using System.Linq;

namespace TestData.DataModel
{
  [Serializable]
  public class MyDataSet : IEquatable<MyDataSet>
  {
    internal static MyDataSet InitData1()
    {
      var output = new MyDataSet()
      {
        Name = "clientData"
      };

      var mv1 = new MyValueClass();
      mv1.Values1["key1Value1Key1"] = new double[] { 1.0, 1.0, 1.0 };
      mv1.Values1["key1Value1Key2"] = new double[] { 1.0, 1.0, 2.0 };
      mv1.Values2["key1Value2Key1"] = new DateTime(2001, 2, 1);
      mv1.Values2["key1Value2Key2"] = new DateTime(2001, 2, 2);
      mv1.Values3["key1Value3Key1"] = "key1Value3Data1";
      mv1.Values3["key1Value3Key2"] = "key1Value3Data2";
      output.Value[new MyKeyClass() { Value = "key1" }] = mv1;

      var mv2 = new MyValueClass();
      mv2.Values1["key2Value1Key1"] = new double[] { 2.0, 1.0, 1.0 };
      mv2.Values1["key2Value1Key2"] = new double[] { 2.0, 1.0, 2.0 };
      mv2.Values2["key2Value2Key1"] = new DateTime(2002, 2, 1);
      mv2.Values2["key2Value2Key2"] = new DateTime(2002, 2, 2);
      mv2.Values3["key2Value3Key1"] = "key2Value3Data1";
      mv2.Values3["key2Value3Key2"] = "key2Value3Data2";
      output.Value[new MyKeyClass() { Value = "key2" }] = mv2;
      return output;
    }

    public Dictionary<MyKeyClass, MyValueClass> Value { get; } = new Dictionary<MyKeyClass, MyValueClass>();

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
             Value.OrderByDescending(_ => _.Key.Value)
                  .Zip(other.Value.OrderByDescending(_ => _.Key.Value),
                       (_1, _2) => _1.Key.Equals(_2.Key) && _1.Value.Equals(_2.Value))
                  .All(_ => _);
    }

    public static bool operator ==(MyDataSet lhs, MyDataSet rhs) => !(lhs is null) && lhs.Equals(rhs);

    public static bool operator !=(MyDataSet lhs, MyDataSet rhs) => !(lhs == rhs);

    public override bool Equals(object obj)
    {
      if (obj is null) return false;
      if (ReferenceEquals(this, obj)) return true;
      return obj.GetType() == GetType() && Equals((MyDataSet)obj);
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
