﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

using Azos.Apps;
using Azos.Scripting;
using Azos.IO.Archiving;
using Azos.Data;
using Azos.Log;
using Azos.Security;

namespace Azos.Tests.Nub.IO.Archiving
{
  [Runnable]
  public class DefaultVolumeTests
  {
    public static ICryptoManager NopCrypto => NOPApplication.Instance.SecurityManager.Cryptography;


    [Run]
    public void Metadata_Basic()
    {
      var ms = new MemoryStream();
      var meta = VolumeMetadataBuilder.Make("Volume-1")
                                      .SetVersion(123,456)
                                      .SetDescription("My volume");
      var v1 = new DefaultVolume(NopCrypto, meta, ms);
      var id = v1.Metadata.Id;
      v1.Dispose();//it closes the stream

      Aver.IsTrue(ms.Length>0);

      var v2 = new DefaultVolume(NopCrypto, ms);
      Aver.AreEqual(id, v2.Metadata.Id);
      Aver.AreEqual("Volume-1", v2.Metadata.Label);
      Aver.AreEqual("My volume", v2.Metadata.Description);
      Aver.AreEqual(123, v2.Metadata.VersionMajor);
      Aver.AreEqual(456, v2.Metadata.VersionMinor);
      Aver.IsTrue(v2.Metadata.Channel.IsZero);
      Aver.IsFalse(v2.Metadata.IsCompressed);
      Aver.IsFalse(v2.Metadata.IsEncrypted);
    }

    [Run]
    public void Metadata_AppSection()
    {
      var ms = new MemoryStream();
      var meta = VolumeMetadataBuilder.Make("V1")
                                      .SetApplicationSection(app => { app.AddAttributeNode("a", 1); })
                                      .SetApplicationSection(app => { app.AddAttributeNode("b", -7); })
                                      .SetApplicationSection(app => { app.AddChildNode("sub{ q=true b=-9 }".AsLaconicConfig()); });

      var v1 = new DefaultVolume(NopCrypto, meta, ms);
      var id = v1.Metadata.Id;
      v1.Dispose();//it closes the stream

      Aver.IsTrue(ms.Length > 0);

      var v2 = new DefaultVolume(NopCrypto, ms);

      v2.Metadata.See();

      Aver.AreEqual(id, v2.Metadata.Id);
      Aver.AreEqual("V1", v2.Metadata.Label);

      Aver.IsTrue(v2.Metadata.SectionApplication.Exists);
      Aver.AreEqual(1, v2.Metadata.SectionApplication.Of("a").ValueAsInt());
      Aver.AreEqual(-7, v2.Metadata.SectionApplication.Of("b").ValueAsInt());
      Aver.AreEqual(true, v2.Metadata.SectionApplication["sub"].Of("q").ValueAsBool());
      Aver.AreEqual(-9, v2.Metadata.SectionApplication["sub"].Of("b").ValueAsInt());
    }

    [Run("compress=null     pad=1000 remount=false")]
    [Run("compress=gzip     pad=1000 remount=false")]
    [Run("compress=gzip-max pad=1000 remount=false")]

    [Run("compress=null     pad=1000 remount=true")]
    [Run("compress=gzip     pad=1000 remount=true")]
    [Run("compress=gzip-max pad=1000 remount=true")]
    public void Page_Write_Read(string compress, int pad, bool remount)
    {
      var ms = new MemoryStream();
      var meta = VolumeMetadataBuilder.Make("Volume-1");

      if (compress.IsNotNullOrWhiteSpace())
      {
        meta.SetCompressionScheme(compress);
      }

      var v1 = new DefaultVolume(NopCrypto, meta, ms);

      var page = new Page(0);
      Aver.IsTrue(page.State == Page.Status.Unset);
      page.BeginWriting(new DateTime(1980, 7, 1, 15, 0, 0, DateTimeKind.Utc), Atom.Encode("app"), "dima@zhaba.com");
      Aver.IsTrue(page.State == Page.Status.Writing);
      var adr1 = page.Append(new ArraySegment<byte>(new byte[] { 1, 2, 3 }, 0, 3));
      Aver.AreEqual(0, adr1);
      var adr2 = page.Append(new ArraySegment<byte>(new byte[] { 4, 5 }, 0, 2));
      Aver.IsTrue(adr2 > 0);
      var adr3 = page.Append(new ArraySegment<byte>(new byte[pad]));
      Aver.IsTrue(adr3 > adr2);
      page.EndWriting();

      Aver.IsTrue(page.State == Page.Status.Written);
      var pid = v1.AppendPage(page);  //append to volume
      Aver.IsTrue(page.State == Page.Status.Written);


      "Written volume {0} Stream size is {1} bytes".SeeArgs(v1.Metadata.Id, ms.Length);

      if (remount)
      {
        v1.Dispose();
        v1 = new DefaultVolume(NopCrypto, ms);//re-mount existing data from stream
        "Re-mounted volume {0}".SeeArgs(v1.Metadata.Id);
      }

      page = new Page(0);//we could have reused the existing page but we re-allocate for experiment cleanness
      Aver.IsTrue(page.State == Page.Status.Unset);
      v1.ReadPage(pid, page);
      Aver.IsTrue(page.State == Page.Status.Reading);

      //page header read correctly
      Aver.AreEqual(new DateTime(1980, 7, 1, 15, 0, 0, DateTimeKind.Utc), page.CreateUtc);
      Aver.AreEqual(Atom.Encode("app"), page.CreateApp);
      Aver.AreEqual("dima@zhaba.com", page.CreateHost);


      var raw = page.Entries.ToArray();//all entry enumeration test
      Aver.AreEqual(4, raw.Length);

      Aver.IsTrue(raw[0].State == Entry.Status.Valid);
      Aver.IsTrue(raw[1].State == Entry.Status.Valid);
      Aver.IsTrue(raw[2].State == Entry.Status.Valid);
      Aver.IsTrue(raw[3].State == Entry.Status.EOF);
      Aver.AreEqual(0, raw[0].Address);
      Aver.IsTrue(raw[1].Address > 0);

      Aver.AreEqual(3, raw[0].Raw.Count);
      Aver.AreEqual(1, raw[0].Raw.Array[raw[0].Raw.Offset + 0]);
      Aver.AreEqual(2, raw[0].Raw.Array[raw[0].Raw.Offset + 1]);
      Aver.AreEqual(3, raw[0].Raw.Array[raw[0].Raw.Offset + 2]);

      Aver.AreEqual(2, raw[1].Raw.Count);
      Aver.AreEqual(4, raw[1].Raw.Array[raw[1].Raw.Offset + 0]);
      Aver.AreEqual(5, raw[1].Raw.Array[raw[1].Raw.Offset + 1]);

      Aver.AreEqual(pad, raw[2].Raw.Count);

      var one = page[adr1]; //indexer test
      Aver.IsTrue(one.State == Entry.Status.Valid);
      Aver.AreEqual(3, one.Raw.Count);
      Aver.AreEqual(1, one.Raw.Array[one.Raw.Offset + 0]);
      Aver.AreEqual(2, one.Raw.Array[one.Raw.Offset + 1]);
      Aver.AreEqual(3, one.Raw.Array[one.Raw.Offset + 2]);

      one = page[adr2];
      Aver.IsTrue(one.State == Entry.Status.Valid);
      Aver.AreEqual(2, one.Raw.Count);
      Aver.AreEqual(4, one.Raw.Array[one.Raw.Offset + 0]);
      Aver.AreEqual(5, one.Raw.Array[one.Raw.Offset + 1]);

      one = page[adr3];
      Aver.IsTrue(one.State == Entry.Status.Valid);
      Aver.AreEqual(pad, one.Raw.Count);
    }


    //[Run]
    //public void Page_Write_Corrupt_Read(string compress, int count)
    //{
    //  var ms = new MemoryStream();
    //  var meta = VolumeMetadataBuilder.Make("Volume-1");

    //  if (compress.IsNotNullOrWhiteSpace())
    //  {
    //    meta.SetCompressionScheme(compress);
    //  }

    //  var v1 = new DefaultVolume(NopCrypto, meta, ms);

    //  var page = new Page(0);
    //  Aver.IsTrue(page.State == Page.Status.Unset);
    //  page.BeginWriting(new DateTime(1980, 7, 1, 15, 0, 0, DateTimeKind.Utc), Atom.Encode("app"), "dima@zhaba.com");
    //  Aver.IsTrue(page.State == Page.Status.Writing);
    //  var adr1 = page.Append(new ArraySegment<byte>(new byte[] { 1, 2, 3 }, 0, 3));
    //  Aver.AreEqual(0, adr1);
    //  var adr2 = page.Append(new ArraySegment<byte>(new byte[] { 4, 5 }, 0, 2));
    //  Aver.IsTrue(adr2 > 0);
    //  var adr3 = page.Append(new ArraySegment<byte>(new byte[pad]));
    //  Aver.IsTrue(adr3 > adr2);
    //  page.EndWriting();

    //}



      // [Run]
      public void Metadata_Create_Multiple_Sections_Mount()
    {
      //var ms = new FileStream("c:\\azos\\archive.lar", FileMode.Create);//  new MemoryStream();
      var ms = new MemoryStream();

      var meta = VolumeMetadataBuilder.Make("20210115-1745-doctor")
                                      .SetVersion(99, 21)
                                      .SetDescription("B vs D De-Terminator for doctor bubblegumization")
                                      .SetChannel(Atom.Encode("dvop"))
                                      .SetCompressionScheme(DefaultVolume.COMPRESSION_SCHEME_GZIP_MAX)
                                  //    .SetEncryptionScheme("aes1")
                                      .SetApplicationSection(app => {
                                        app.AddChildNode("user").AddAttributeNode("id", 111222);
                                        app.AddChildNode("user").AddAttributeNode("id", 783945);
                                        app.AddAttributeNode("is-good", false);
                                        app.AddAttributeNode("king-signature", Platform.RandomGenerator.Instance.NextRandomWebSafeString(150, 150));
                                      })
                                      .SetApplicationSection(app => { app.AddAttributeNode("a", false); })
                                      .SetApplicationSection(app => { app.AddAttributeNode("b", true); })
                                      .SetCompressionSection(cmp => { cmp.AddAttributeNode("z", 41); });
                                   //   .SetEncryptionSection(enc => { enc.AddAttributeNode("z", 99); });

      var volume = new DefaultVolume(NOPApplication.Instance.SecurityManager.Cryptography, meta, ms);

      volume.Dispose();
     // ms.GetBuffer().ToDumpString(DumpFormat.Hex).See();

      volume = new DefaultVolume(NOPApplication.Instance.SecurityManager.Cryptography, ms);
      volume.Metadata.See();
    }


    [Run("!arch-log", "scheme=null          cnt=16000000 para=16")]
    [Run("!arch-log", "scheme=gzip          cnt=16000000 para=16")]
    [Run("!arch-log", "scheme=gzip-max      cnt=16000000 para=16")]
    public void Write_LogMessages(string scheme, int CNT, int PARA)
    {
      var msData = new FileStream("c:\\azos\\logging-{0}.lar".Args(scheme.Default("none")), FileMode.Create);
      var msIdxId = new FileStream("c:\\azos\\logging-{0}.guid.lix".Args(scheme.Default("none")), FileMode.Create);

      var meta = VolumeMetadataBuilder.Make("log messages")
                                      .SetVersion(1, 1)
                                      .SetDescription("Testing")
                                      .SetChannel(Atom.Encode("tezt"))
                                      .SetCompressionScheme(scheme);   // Add optional compression

      var volumeData = new DefaultVolume(NOPApplication.Instance.SecurityManager.Cryptography, meta, msData);
      var volumeIdxId = new DefaultVolume(NOPApplication.Instance.SecurityManager.Cryptography, meta, msIdxId);


      volumeData.PageSizeBytes = 1024 * 1024;
      volumeIdxId.PageSizeBytes = 128 * 1024;

      var time = Azos.Time.Timeter.StartNew();


      Parallel.For(0, PARA, _ => {

        var app = Azos.Apps.ExecutionContext.Application;

        using(var aIdxId = new GuidIdxAppender(volumeIdxId,
                                          NOPApplication.Instance.TimeSource,
                                          NOPApplication.Instance.AppId, "dima@zhaba"))
        {
          using(var appender = new LogMessageArchiveAppender(volumeData,
                                                 NOPApplication.Instance.TimeSource,
                                                 NOPApplication.Instance.AppId,
                                                 "dima@zhaba",
                                                 onPageCommit: (e, b) => aIdxId.Append(new GuidBookmark(e.Guid, b))))
          {

            for(var i=0; app.Active && i<CNT / PARA; i++)
            {
              var msg = FakeLogMessage.BuildRandom();
              appender.Append(msg);
            }

          }
        }
      });

      time.Stop();
      "Did {0:n0} in {1:n1} sec at {2:n2} ops/sec\n".SeeArgs(CNT, time.ElapsedSec, CNT / time.ElapsedSec);

      volumeIdxId.Dispose();
      volumeData.Dispose();

      "CLOSED all volumes\n".See();
    }


  }
}
