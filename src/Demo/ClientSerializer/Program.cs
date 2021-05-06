using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataModel;

namespace ClientSerializer
{
  class Program
  {
    static void Main(string[] args)
    {
      const string filename = "..\\..\\..\\clientData.out";

      var data = new MyDataSet()
      {
        Name = "clientData",
        Value = new Dictionary<MyKeyClass, MyValueClass>()
        {
          {
            new MyKeyClass(){Value="key1"}, new MyValueClass()
            {
              Values1=new Dictionary<string, double[]>()
              {
                {"key1Value1Key1", new double[]{1.0, 1.0, 1.0} },
                {"key1Value1Key2", new double[]{1.0, 1.0, 2.0} },
              }, 
              Values2 = new Dictionary<string, DateTime>()
              {
                {"key1Value2Key1", new DateTime(2001, 2, 1)},
                {"key1Value2Key2", new DateTime(2001, 2, 2) },
              }, 
              Values3 = new Dictionary<string, string>()
              {
                {"key1Value3Key1", "key1Value3Data1"},
                {"key1Value3Key2", "key1Value3Data2" },
              }
            } 
          },
          {
            new MyKeyClass(){Value="key2"}, new MyValueClass()
            {
              Values1=new Dictionary<string, double[]>()
              {
                {"key2Value1Key1", new double[]{2.0, 1.0, 1.0} },
                {"key2Value1Key2", new double[]{2.0, 1.0, 2.0} },
              }, 
              Values2 = new Dictionary<string, DateTime>()
              {
                {"key2Value2Key1", new DateTime(2002, 2, 1)},
                {"key2Value2Key2", new DateTime(2002, 2, 2) },
              }, 
              Values3 = new Dictionary<string, string>()
              {
                {"key2Value3Key1", "key2Value3Data1"},
                {"key2Value3Key2", "key2Value3Data2" },
              }
            } 
          }
        }
      };


      using (var file = System.IO.File.Create(filename,0,FileOptions.WriteThrough))
      {
        var serializer = new SlimSerializer.SlimSerializer();
        serializer.Serialize(file, data);
      }

      Console.WriteLine($"Created file {filename} with data=\n{data}");

    }
  }
}
