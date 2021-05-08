using DataModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slim;

namespace ClientDeserializer
{
  class Program
  {
    static void Main(string[] args)
    {
      const string filename = "..\\..\\..\\glibData.out";
      //const string filename = "..\\..\\..\\clientData.out";

      MyDataSet dataSet;
      using (var inputfile = System.IO.File.OpenRead(filename))
      {
        var serializer = new SlimSerializer.SlimSerializer();
        dataSet = serializer.Deserialize(inputfile) as MyDataSet;
      }
      Console.WriteLine("Data read:");
      Console.WriteLine(dataSet);
    }
  }
}
