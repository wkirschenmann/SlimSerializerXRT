// // Copyright (c) ANEO. All rights reserved.
// // Licensed under the Apache License, Version 2.0

using System;
using System.IO;

using NUnit.Framework;

namespace SlimTests
{
  namespace Nullable
  {
    public enum Options
    {
      Default = 0,
      Other   = 1,
    }

    public class NullableEnumerableContainer
    {
      public Options? Data { get; set; }
    }

    public class EnumerableContainer
    {
      public Options Data { get; set; }
    }

    public class NullableIntContainer
    {
      public int? Data { get; set; }
    }

    public class SerializationTests
    {
      
      [Test]
      [TestCase(Options.Default)]
      [TestCase(Options.Other)]
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
      [TestCase(null)]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
      public void SerializeAndDeserializeNullableEnumerableTest(Options? data)
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
      [TestCase(Options.Default)]
      [TestCase(Options.Other)]
      public void SerializeAndDeserializeEnumerableTest(Options data)
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
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
      [TestCase(null)]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
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
