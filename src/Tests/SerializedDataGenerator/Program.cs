using System;
using System.IO;
using TestData.DataModel;

namespace SerializedDataGenerator
{
  public static class Filenames
  {
    public static readonly string MyDataSetFromCore = "MyDataSetFromCore.slim";
    public static readonly string MyDataSetFromFramework = "MyDataSetFromFramework.slim";
    public static readonly string AllTypesDataFromCore = "AllTypesDataFromCore.slim";
    public static readonly string AllTypesDataFromFramework = "AllTypesDataFromFramework.slim";
  }

  public static class DataSets
  {
    public static readonly AllTypesData AllTypesData = new AllTypesData(10);
    public static readonly MyDataSet MyDataSet = TestData.DataModel.ReferenceData.DataSet1;
  }

  public static class Program
  {
    public static void Main()
    {
#if NET451
      MyDataSet(Filenames.MyDataSetFromFramework);
      AllTypesData(Filenames.AllTypesDataFromFramework);
#endif //NET451
#if NETCORE21
      MyDataSet(Filenames.MyDataSetFromCore);
      AllTypesData(Filenames.AllTypesDataFromCore);
#endif //NETCORE21
    }

    public static void MyDataSet(string filename)
    {
      using (var stream = File.Create(filename))
      {
        Console.WriteLine($"Creating file {filename}");
        var serializer = new Slim.SlimSerializer() { SerializeForFramework = true };
        Console.WriteLine($"Serializing {nameof(MyDataSet)}");
        serializer.Serialize(stream, DataSets.MyDataSet);
      }
      Console.WriteLine($"File {filename} closed");
    }

    
    public static void AllTypesData(string filename)
    {
      Console.WriteLine($"Creating file {filename}");
      using (var stream = File.Create(filename))
      {
        var serializer = new Slim.SlimSerializer() {SerializeForFramework = true};
        Console.WriteLine($"Serializing {nameof(AllTypesData)}");
        serializer.Serialize(stream, DataSets.AllTypesData);
      }
      Console.WriteLine($"File {filename} closed");
    }
  }
}
