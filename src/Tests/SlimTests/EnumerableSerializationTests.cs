// // Copyright (c) ANEO. All rights reserved.
// // Licensed under the Apache License, Version 2.0

using System;
using System.IO;

using NUnit.Framework;

namespace SlimTests
{
  namespace Nullable
  {
    public enum Option
    {
      Default = 0,
      Other   = 1,
    }

    public class NullableEnumerableContainer
    {
      public Option? Data;
    }

    public class EnumerableContainer
    {
      public Option Data;
    }

    class NullableIntContainer
    {
      public int? Data;
    }

    public class SerializationTests
    {
      
      [Test]
      [TestCase(Option.Default)]
      [TestCase(Option.Other)]
      [TestCase(null)]
      public void SerializeAndDeserializeNullableEnumerableTest(Option? data)
      {
        var test = new NullableEnumerableContainer {Data = data};

        var serializer = new Slim.SlimSerializer {SerializeForFramework = true};

        using (var stream = new MemoryStream())
        {
          serializer.Serialize(stream, test);
          var buffer = stream.ToArray();
          stream.Seek(0, SeekOrigin.Begin);
          var des = serializer.Deserialize(stream);
          Assert.AreEqual(data.HasValue, ((NullableEnumerableContainer)des).Data.HasValue);
          if(data.HasValue)
            Assert.AreEqual(data.Value, ((NullableEnumerableContainer)des).Data.Value);
        }
      }


      [Test]
      [TestCase(Option.Default)]
      [TestCase(Option.Other)]
      public void SerializeAndDeserializeEnumerableTest(Option data)
      {
        var test = new EnumerableContainer {Data = data};

        var serializer = new Slim.SlimSerializer {SerializeForFramework = true};

        using (var stream = new MemoryStream())
        {
          serializer.Serialize(stream, test);
          var buffer = stream.ToArray();
          stream.Seek(0, SeekOrigin.Begin);
          var des = serializer.Deserialize(stream);
          Assert.AreEqual(data, ((EnumerableContainer)des).Data);
        }
      }

      [Test]
      [TestCase(42)]
      [TestCase(null)]
      [TestCase(0)]
      public void SerializeAndDeserializeNullableIntTest(int? data)
      {
        var test = new NullableIntContainer { Data = data };

        var serializer = new Slim.SlimSerializer {SerializeForFramework = true};

        using (var stream = new MemoryStream())
        {
          serializer.Serialize(stream, test);
          var buffer = stream.ToArray();
          stream.Seek(0, SeekOrigin.Begin);
          var des = serializer.Deserialize(stream);
          Assert.AreEqual(data.HasValue, ((NullableIntContainer)des).Data.HasValue);
          if (data.HasValue)
            Assert.AreEqual(data.Value, ((NullableIntContainer)des).Data.Value);
        }
      }

    }
  }
}
