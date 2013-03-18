using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace ASync
{
    class Program
    {
        static void Main(string[] args)
        {
            var fn = "test.dat";

            var partH = new BlockingCollectionDataChunk<uint>();
            ProcessFile(fn, partH);

            var partHNaive = new BlockingCollectionDataChunk<uint>();
            ProcessFileNaive(fn, partHNaive);

            var l1 = partH.ToList();
            var l2 = partHNaive.ToList();

            Debug.Assert(l1.SequenceEqual(l2), "error");
        }

        static void ProcessFileNaive(string filename, BlockingCollectionDataChunk<uint> partitionHash)
        {
            var rollingHash = new List<uint>();
            var localMaximaPos = new List<int>();

            var fileBytes = File.ReadAllBytes(filename);
            using (var ms = new MemoryStream(fileBytes, 0, fileBytes.Length, true, true))
            {
                var fh = new FileHash(1024);
                fh.StreamToHashValuesNaive(ms, rollingHash);
            }

            var lm = new LocalMaxima(512 * 1024);
            lm.CalcUsingNaive(rollingHash, localMaximaPos);

            var localMaximaPosBC = new BlockingCollectionDataChunk<int>();
            foreach (var pos in localMaximaPos)
            {
                localMaximaPosBC.Add(pos);
            }
            localMaximaPosBC.CompleteAdding();

            var mmh = new MurmurHash3_x86_32();
            var fph = new FileParitionHash(mmh);
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fph.ProcessStream(fs, localMaximaPosBC, partitionHash);
            }
        }

        static void ProcessFile(string filename, BlockingCollectionDataChunk<uint> partitionHash)
        {
            var rollingHash = new BlockingCollectionDataChunk<uint>();
            var localMaximaPos = new BlockingCollectionDataChunk<int>();

            var sw = new Stopwatch();
            sw.Start();

            Task.Run(() =>
            {
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var fh = new FileHash(1024);
                    fh.StreamToHashValues(fs, rollingHash);
                }
            });

            Task.Run(() =>
            {
                var lm = new LocalMaxima(512 * 1024);
                lm.CalcUsingBlockAlgo(rollingHash, localMaximaPos);
            });

            Task.Run(() =>
            {
                var mmh = new MurmurHash3_x86_32();
                var fph = new FileParitionHash(mmh);
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fph.ProcessStream(fs, localMaximaPos, partitionHash);
                }
            });

            var count = 0;
            foreach (var i in partitionHash.BlockingCollection.GetConsumingEnumerable())
            {
                count += i.DataSize;
                //Console.WriteLine("File par hash: {0}", i);
            }
            sw.Stop();

            Console.WriteLine("Number of partitions: {0}", count);
            Console.WriteLine("Time: {0} ms", sw.ElapsedMilliseconds);
        }
    }
}