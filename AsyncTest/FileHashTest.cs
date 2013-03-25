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
                var byteArr = Encoding.UTF8.GetBytes(str);

                var fh = new FileHash(5);

                var ret = new BlockingCollectionDataChunk<uint>(2);
                var ret2 = new List<uint>();

                using (var ms = new MemoryStream(byteArr))
                {
                    fh.StreamToHashValues(ms, ret);
                }
                using (var ms = new MemoryStream(byteArr, 0, byteArr.Length, true, true))
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
            var byteArr = Encoding.UTF8.GetBytes(str);

            var fh = new FileHash(5);

            var ret = new BlockingCollectionDataChunk<uint>(4);
            var ret2 = new List<uint>();

            using (var ms = new MemoryStream(byteArr))
            {
                fh.StreamToHashValues(ms, ret);
            }
            using (var ms = new MemoryStream(byteArr, 0, byteArr.Length, true, true))
            {
                fh.StreamToHashValuesNaive(ms, ret2);
            }
            var retList = ret.ToList();

            Assert.AreEqual(retList.Count, str.Length);
            CollectionAssert.AreEqual(retList, ret2);
        }

        [TestMethod]
        public void FileHashUInt32Correctness()
        {
            for (var i = 0; i < 100; ++i)
            {
                var sb = new StringBuilder();
                for (var j = 0; j < i; ++j)
                {
                    sb.Append(j);
                }
                var str = sb.ToString();
                var byteArr = Encoding.UTF8.GetBytes(str);

                var fh = new FileHash(5);

                var ret = new BlockingCollectionDataChunk<uint>(2);
                var ret2 = new List<uint>();

                using (var ms = new MemoryStream(byteArr))
                {
                    fh.StreamToUInt32HashValues(ms, ret);
                }
                using (var ms = new MemoryStream(byteArr, 0, byteArr.Length, true, true))
                {
                    fh.StreamToUInt32Naive(ms, ret2);
                }
                var retList = ret.ToList();


                Assert.AreEqual(retList.Count, str.Length);
                CollectionAssert.AreEqual(retList, ret2);
            }
        }

        [TestMethod]
        public void FileHashUInt32Correctness2()
        {
            var str = "asdf";
            var byteArr = Encoding.UTF8.GetBytes(str);

            var fh = new FileHash(5);

            var ret = new BlockingCollectionDataChunk<uint>(4);
            var ret2 = new List<uint>();

            using (var ms = new MemoryStream(byteArr))
            {
                fh.StreamToUInt32HashValues(ms, ret);
            }
            using (var ms = new MemoryStream(byteArr, 0, byteArr.Length, true, true))
            {
                fh.StreamToUInt32Naive(ms, ret2);
            }
            var retList = ret.ToList();

            Assert.AreEqual(retList.Count, str.Length);
            CollectionAssert.AreEqual(retList, ret2);
        }
    }
}
