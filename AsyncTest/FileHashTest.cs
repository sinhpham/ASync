using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.IO;
using ASync;
using System.Text;
using System.Collections.Generic;

namespace AsyncTest
{
    [TestClass]
    public class FileHashTest
    {
        [TestMethod]
        public void FileHashCorrectness()
        {
            for (var i = 0; i < 100; ++i)
            {
                var sb = new StringBuilder();
                for (var j = 0; j < i; ++j)
                {
                    sb.Append(j);
                }
                var str = sb.ToString();

                var fh = new FileHash(5);

                var ret = new BlockingCollectionDataChunk<uint>(2);
                var ret2 = new List<uint>();

                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
                {
                    fh.StreamToHashValues(ms, ret);
                }
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
                {
                    fh.StreamToHashValuesNaive(ms, ret2);
                }
                var retList = ret.ToList();
                

                Assert.AreEqual(retList.Count, str.Length);
                CollectionAssert.AreEqual(retList, ret2);
            }
        }

        [TestMethod]
        public void FileHashCorrectness2()
        {
            var str = "asdf";

            var fh = new FileHash(5);

            var ret = new BlockingCollectionDataChunk<uint>(4);
            var ret2 = new List<uint>();

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                fh.StreamToHashValues(ms, ret);
            }
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                fh.StreamToHashValuesNaive(ms, ret2);
            }
            var retList = ret.ToList();

            Assert.AreEqual(retList.Count, str.Length);
            CollectionAssert.AreEqual(retList, ret2);
        }
    }
}
