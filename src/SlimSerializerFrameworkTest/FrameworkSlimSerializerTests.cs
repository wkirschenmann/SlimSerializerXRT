using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.IO;
using TestData.DataModel;

namespace SlimSerializerFrameworkTest
{
  [TestClass]
  public class FrameworkSlimSerializerTests
  {
    [TestMethod]
    public void FrameworkSerializationTest()
    {
      using (var stream = new MemoryStream())
      {
        var serializer = new Slim.SlimSerializer(){SerializeForFramework = true};
        serializer.Serialize(stream, TestData.DataModel.ReferenceData.DataSet1);
        var res = stream.ToArray();
        
        var str = "{" + BitConverter.ToString(res).Replace("-", ", 0x") + "}";

        CollectionAssert.AreEqual(TestData.SerializedData.ReferenceData.FromFrameworkData, res);
      }
    }

    [TestMethod]
    public void FrameworkDeserializationFromFrameworkTest()
    {
      var serializer = new Slim.SlimSerializer(){SerializeForFramework = true};
      var res = serializer.Deserialize(new MemoryStream(TestData.SerializedData.ReferenceData.FromFrameworkData));

      //var comp = TestData.DataModel.ReferenceData.DataSet1.Equals(res);

      Assert.AreEqual(TestData.DataModel.ReferenceData.DataSet1, res);
    }

    [TestMethod]
    public void FrameworkDeserializationFromNetCoreTest()
    {
      var serializer = new Slim.SlimSerializer(){SerializeForFramework = true};
      var res = serializer.Deserialize(new MemoryStream(TestData.SerializedData.ReferenceData.FromNetCoreData));

      //var comp = TestData.DataModel.ReferenceData.DataSet1.Equals(res);

      Assert.AreEqual(TestData.DataModel.ReferenceData.DataSet1, res);
    }
  }
}
