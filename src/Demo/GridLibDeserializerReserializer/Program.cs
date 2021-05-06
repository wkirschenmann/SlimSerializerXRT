using DataModel;

using System;
using Slim;

namespace GridLibDeserializerReserializer
{
  class Program
  {
    static void Main(string[] args)
    {
      const string inputFilename = "..\\..\\..\\..\\clientData.out";
      const string outputFilename = "..\\..\\..\\..\\glibData.out";

      var serializer = new Slim.SlimSerializer() { SerializeForFramework = true };
      MyDataSet dataSet;
      using(var inputfile = System.IO.File.OpenRead(inputFilename))
      {
        dataSet = (MyDataSet)serializer.Deserialize(inputfile);
      }
      Console.WriteLine("Data read:");
      Console.WriteLine(dataSet);

      dataSet.Name = "glibData";
      using (var outputfile = System.IO.File.OpenWrite(outputFilename))
      {
        serializer.Serialize(outputfile, dataSet);
      }

      Console.WriteLine($"Created file {outputFilename} with data=\n{dataSet}");

    }
  }
}
