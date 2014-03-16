using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using ASyncLib;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace AsyncTest
{
    //[TestClass]
    //public class FilePartitionHashTest
    //{
    //    [TestMethod]
    //    public void FilePartitionHashTestCorrectness1()
    //    {
    //        var positions = new BlockingCollectionDataChunk<int>(3);
    //        var outHashValues = new BlockingCollectionDataChunk<uint>(3);
    //        var mmh = new MurmurHash3_x86_32();
    //        var fph = new FileParitionHash(mmh);
    //        var fci = new BlockingCollectionDataChunk<FileChunkInfo>();

    //        var strStream = "abcdef";
    //        positions.Add(2);
    //        positions.Add(5);
    //        positions.CompleteAdding();

    //        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(strStream)))
    //        {
    //            fph.ProcessStream(ms, positions, outHashValues, fci);
    //        }

    //        var h1 = BitConverter.ToUInt32(mmh.ComputeHash(Encoding.UTF8.GetBytes(strStream), 0, 3), 0);
    //        var h2 = BitConverter.ToUInt32(mmh.ComputeHash(Encoding.UTF8.GetBytes(strStream), 3, 3), 0);


    //        CollectionAssert.AreEqual(new List<uint> { h1, h2 }, outHashValues.ToList());
    //    }
    //}
}
