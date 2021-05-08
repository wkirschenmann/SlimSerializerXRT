using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SlimSerializerCoreTest
{
  public class CoreSlimSerializerTests
  {
    [Test]
    public void CoreSerializationTest()
    {
      using (var stream = new MemoryStream())
      {
        var serializer = new Slim.SlimSerializer(){SerializeForFramework = true};
        serializer.Serialize(stream, TestData.DataModel.ReferenceData.DataSet1);
        var res = stream.ToArray();

        //var str = "{0x" + BitConverter.ToString(res).Replace("-", ", 0x") + "}";

        CollectionAssert.AreEqual(TestData.SerializedData.ReferenceData.FromNetCoreData, res);
      }
    }

    [Test]
    public void CoreDeserializationFromFrameworkTest()
    {
      var serializer = new Slim.SlimSerializer(){SerializeForFramework = true};
      var res = serializer.Deserialize(new MemoryStream(TestData.SerializedData.ReferenceData.FromFrameworkData));

      //var comp = TestData.DataModel.ReferenceData.DataSet1.Equals(res);

      Assert.AreEqual(TestData.DataModel.ReferenceData.DataSet1, res);
    }

    [Test]
    public void CoreDeserializationFromNetCoreTest()
    {
      var serializer = new Slim.SlimSerializer(){SerializeForFramework = true};
      var res = serializer.Deserialize(new MemoryStream(TestData.SerializedData.ReferenceData.FromNetCoreData));

      //var comp = TestData.DataModel.ReferenceData.DataSet1.Equals(res);

      Assert.AreEqual(TestData.DataModel.ReferenceData.DataSet1, res);
    }
  }
}