using System;
using System.Collections.Generic;
using System.Linq;

namespace DataModel
{
  [Serializable]
  public class MyDataSet
  {
    public Dictionary<MyKeyClass, MyValueClass> Value { get; set; }

    public string Name { get; set; }
    public override string ToString()
    {
      return "MyDataSet[" + Name + "]\n{\n" + string.Join(",\n", Value.Select(kvp => kvp.Key + ", " + kvp.Value)) + "\n}";
    }
  }

}
