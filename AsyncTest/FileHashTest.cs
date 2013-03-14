using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.IO;
using ASync;
using System.Text;

namespace AsyncTest
{
    [TestClass]
    public class FileHashTest
    {
        [TestMethod]
        public void FileHashCorrectness()
        {
            var str = "1234567890";

            var ret = new ConcurrentQueue<uint>();
            var ret2 = new ConcurrentQueue<uint>();
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                FileHash.StreamToHashValues(ms, ret);
            }
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                FileHash.StreamToHashValuesNaive(ms, ret2);
            }

            CollectionAssert.AreEqual(ret, ret2);
        }
    }
}
