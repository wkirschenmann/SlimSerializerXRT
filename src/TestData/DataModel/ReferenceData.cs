using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestData.DataModel
{
  public static class ReferenceData
  {

    public static readonly MyDataSet DataSet1 = InitData1();

    private static MyDataSet InitData1()
    {
      var output = new MyDataSet()
      {
        Name = "clientData"
      };

      var mv1 = new MyValueClass();
      mv1.Values1["key1Value1Key1"] = new double[] {1.0, 1.0, 1.0};
      mv1.Values1["key1Value1Key2"] = new double[] {1.0, 1.0, 2.0};
      mv1.Values2["key1Value2Key1"] = new DateTime(2001, 2, 1);
      mv1.Values2["key1Value2Key2"] = new DateTime(2001, 2, 2);
      mv1.Values3["key1Value3Key1"] = "key1Value3Data1";
      mv1.Values3["key1Value3Key2"] = "key1Value3Data2";
      output.Value[new MyKeyClass() {Value = "key1"}] = mv1;

      var mv2 = new MyValueClass();
      mv2.Values1["key2Value1Key1"] = new double[] {2.0, 1.0, 1.0};
      mv2.Values1["key2Value1Key2"] = new double[] {2.0, 1.0, 2.0};
      mv2.Values2["key2Value2Key1"] = new DateTime(2002, 2, 1);
      mv2.Values2["key2Value2Key2"] = new DateTime(2002, 2, 2);
      mv2.Values3["key2Value3Key1"] = "key2Value3Data1";
      mv2.Values3["key2Value3Key2"] = "key2Value3Data2";
      output.Value[new MyKeyClass() {Value = "key2"}] = mv2;
      return output;
    }
  };
}
