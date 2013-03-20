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
    public class SameHash : HashAlgorithm
    {
        public override void Initialize()
        {

        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            HashValue = new byte[4];
            Array.Copy(array, 0, HashValue, 0, 4);
        }

        protected override byte[] HashFinal()
        {
            return HashValue;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            TestReconciliation();

        }

        private static void TestFilePartition()
        {
            var fn = "test.dat";

            var partH = new List<uint>();
            ProcessFile(fn, partH);

            var partHNaive = new List<uint>();
            ProcessFileNaive(fn, partHNaive);


            if (!partH.SequenceEqual(partHNaive))
            {
                throw new InvalidDataException("wrong ans");
            }
        }

        static void TestReconciliation()
        {
            var setA = new List<int> { 1, 3, 5 };
            var setB = new List<int> { 2, 5, 6 };

            // In device A
            var bf = GenerateBF(setA);
            var fp = bf.FalsePositive;

            // Send this bloom filter to device B, in device B
            var n0 = 0;
            foreach (var item in setB)
            {
                var byteArr = BitConverter.GetBytes(item);
                if (bf.Contains(byteArr))
                {
                    n0++;
                }
            }
            var d0 = Helper.EstimateD0(bf.Count, setB.Count, n0, bf);

            var _cp = new CharacteristicPolynomial(97);
            var xVal = new List<int>(d0);
            for (var i = 0; i < d0; ++i)
            {
                xVal.Add(i);
            }
            var cpb = _cp.Calc(setB, xVal);

            // Send cpb to device A, in A:
            var cpa = _cp.Calc(setA, xVal);
            var cpaocpb = _cp.Div(cpa, cpb);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                setA.Count - setB.Count,
                out p, out q);

            var samsb = _cp.Factoring(p);
            var sbmsa = _cp.Factoring(q);

        }

        static BloomFilter GenerateBF(List<int> values)
        {
            var n = values.Count;
            var m = n * 12;

            var h1 = new SameHash();
            var hList = new List<HashAlgorithm>();
            hList.Add(h1);
            for (var i = 0; i < 4; ++i)
            {
                var mmh = new MurmurHash3_x86_32();
                mmh.Seed = (uint)i;
                hList.Add(mmh);
            }

            var bf = new BloomFilter(m, hList);
            foreach (var value in values)
            {
                var byteArr = BitConverter.GetBytes(value);
                bf.Add(byteArr);
            }

            return bf;
        }

        static void ProcessFileNaive(string filename, List<uint> partitionHash)
        {
            var rollingHash = new List<uint>();
            var localMaximaPos = new List<int>();

            var fileBytes = File.ReadAllBytes(filename);
            using (var ms = new MemoryStream(fileBytes, 0, fileBytes.Length, true, true))
            {
                var fh = new FileHash(1024);
                fh.StreamToHashValuesNaive(ms, rollingHash);
            }

            var lm = new LocalMaxima(4 * 1024);
            lm.CalcUsingNaive(rollingHash, localMaximaPos);

            var localMaximaPosBC = new BlockingCollectionDataChunk<int>();
            foreach (var pos in localMaximaPos)
            {
                localMaximaPosBC.Add(pos);
            }
            localMaximaPosBC.CompleteAdding();

            var ph = new BlockingCollectionDataChunk<uint>();
            var mmh = new MurmurHash3_x86_32();
            var fph = new FileParitionHash(mmh);
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fph.ProcessStream(fs, localMaximaPosBC, ph);
            }

            foreach (var items in ph.BlockingCollection.GetConsumingEnumerable())
            {
                for (var i = 0; i < items.DataSize; ++i)
                {
                    partitionHash.Add(items.Data[i]);
                }
            }
        }

        static void ProcessFile(string filename, List<uint> partitionHash)
        {
            var rollingHash = new BlockingCollectionDataChunk<uint>();
            var localMaximaPos = new BlockingCollectionDataChunk<int>();
            var ph = new BlockingCollectionDataChunk<uint>();

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
                var lm = new LocalMaxima(4 * 1024);
                lm.CalcUsingBlockAlgo(rollingHash, localMaximaPos);
            });

            Task.Run(() =>
            {
                var mmh = new MurmurHash3_x86_32();
                var fph = new FileParitionHash(mmh);
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fph.ProcessStream(fs, localMaximaPos, ph);
                }
            });

            var count = 0;
            foreach (var items in ph.BlockingCollection.GetConsumingEnumerable())
            {
                count += items.DataSize;
                //Console.WriteLine("File par hash: {0}", i);
                for (var i = 0; i < items.DataSize; ++i)
                {
                    partitionHash.Add(items.Data[i]);
                }
            }
            sw.Stop();

            Console.WriteLine("Number of partitions: {0}", count);
            Console.WriteLine("Time: {0} ms", sw.ElapsedMilliseconds);
        }
    }
}