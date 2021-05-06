using System;
using System.Collections.Generic;
using System.Linq;

namespace DataModel
{
  [Serializable]
  public class MyValueClass
  {
    public Dictionary<string, double[]> Values1 { get; set; } = new Dictionary<string, double[]>();
    public Dictionary<string, DateTime> Values2 { get; set; } = new Dictionary<string, DateTime>();
    public Dictionary<string, string> Values3 { get; set; } = new Dictionary<string, string>();

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
  }

}
