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

    public class PatchData
    {
        public int HashValue { get; set; }
        public byte[] Data { get; set; }
    }

    public class FileChunkInfo
    {
        public int Pos { get; set; }
        public int Length { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Sync("fileOld.pdf", "fileNew.pdf", "fileout.pdf");
        }

        static void Sync(string oldFileName, string newFileName, string outputFileName)
        {
            // Only use the last 20 bits
            var bitMask = 0xFFFFFFF;

            var setNew = new List<int>();
            var fciNew = new List<FileChunkInfo>();
            ProcessFile(newFileName, setNew, fciNew);
            for (var i = 0; i < setNew.Count; ++i)
            {
                setNew[i] = setNew[i] & bitMask;
            }
            var setOld = new List<int>();
            var fciOld = new List<FileChunkInfo>();
            ProcessFile(oldFileName, setOld, fciOld);
            for (var i = 0; i < setOld.Count; ++i)
            {
                setOld[i] = setOld[i] & bitMask;
            }

            var bf = GenerateBF(setNew);

            // Send this bloom filter to device B, in device B
            var n0 = 0;
            foreach (var item in setOld)
            {
                var byteArr = BitConverter.GetBytes(item);
                if (bf.Contains(byteArr))
                {
                    n0++;
                }
            }
            var d0 = Helper.EstimateD0(bf.Count, setOld.Count, n0, bf);

            // Debug infor
            var snmso = setNew.Except(setOld).ToList();
            var somsn = setOld.Except(setNew).ToList();
            var diff = snmso.Count + somsn.Count;

            Debug.Assert(diff <= d0);

            // 46337 is the last prime number which ^2 < (2^31 - 1)
            // 1048583 is the smallest prime > 2^20
            var _cp = new CharacteristicPolynomial(2147483647);
            var xVal = new List<int>(d0);
            for (var i = 0; i < d0; ++i)
            {
                xVal.Add(i);
            }
            var cpb = _cp.Calc(setOld, xVal);

            // Send cpb to device A, in A:
            var cpa = _cp.Calc(setNew, xVal);
            var cpaocpb = _cp.Div(cpa, cpb);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                setNew.Count - setOld.Count,
                out p, out q);

            var missingFromOldList = _cp.Factoring(p);

            var checkList = missingFromOldList.Except(snmso).Count();
            Debug.Assert(checkList == 0);
            Debug.Assert(missingFromOldList.Count == snmso.Count);

            var missingSet = new HashSet<int>();
            foreach (var item in missingFromOldList)
            {
                missingSet.Add(item);
            }

            // Genereate patch file
            var patchFile = new List<PatchData>();
            using (var fs = new FileStream(newFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                for (var i = 0; i < setNew.Count; ++i)
                {
                    var currH = setNew[i];
                    var currPatch = new PatchData()
                    {
                        HashValue = currH
                    };
                    if (missingSet.Contains(currH))
                    {
                        // Need to include the actual data.
                        var currFileChunkInfo = fciNew[i];
                        fs.Position = currFileChunkInfo.Pos;
                        var fcData = new byte[currFileChunkInfo.Length];
                        var bRead = fs.Read(fcData, 0, currFileChunkInfo.Length);
                        if (bRead != currFileChunkInfo.Length)
                        {
                            throw new InvalidDataException();
                        }
                        currPatch.Data = fcData;
                    }
                    patchFile.Add(currPatch);
                }
            }

            // Send patch to device B to reconstruct the file.
            var existingSet = new Dictionary<int, int>();
            for (var i = 0; i < setOld.Count; ++i)
            {
                existingSet.Add(setOld[i], i);
            }

            using (var fsout = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var fsOld = new FileStream(oldFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    for (var i = 0; i < patchFile.Count; ++i)
                    {
                        var currPatch = patchFile[i];
                        if (currPatch.Data == null)
                        {
                            // Existing data.
                            var idx = existingSet[currPatch.HashValue];
                            var currFileChunkInfo = fciOld[idx];
                            fsOld.Position = currFileChunkInfo.Pos;

                            var fcData = new byte[currFileChunkInfo.Length];
                            var bRead = fsOld.Read(fcData, 0, currFileChunkInfo.Length);
                            if (bRead != currFileChunkInfo.Length)
                            {
                                throw new InvalidDataException();
                            }
                            fsout.Write(fcData, 0, fcData.Length);
                        }
                        else
                        {
                            // New data.
                            fsout.Write(currPatch.Data, 0, currPatch.Data.Length);
                        }
                    }
                }
            }
        }

        private static void TestFilePartition()
        {
            var fn = "test.dat";

            var partH = new List<int>();
            var fci = new List<FileChunkInfo>();
            ProcessFile(fn, partH, fci);

            var partHNaive = new List<int>();
            ProcessFileNaive(fn, partHNaive);


            if (!partH.SequenceEqual(partHNaive))
            {
                throw new InvalidDataException("wrong ans");
            }
        }

        static void TestReconciliation()
        {
            var setA = new List<int> { 1, 3, 5 };
            var setB = new List<int> { 3, 5 };

            // In device A
            var bf = GenerateBF(setA);

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

        static void ProcessFileNaive(string filename, List<int> partitionHash)
        {
            var rollingHash = new List<uint>();
            var localMaximaPos = new List<int>();
            var fciBC = new BlockingCollectionDataChunk<FileChunkInfo>();

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
                fph.ProcessStream(fs, localMaximaPosBC, ph, fciBC);
            }

            foreach (var items in ph.BlockingCollection.GetConsumingEnumerable())
            {
                for (var i = 0; i < items.DataSize; ++i)
                {
                    partitionHash.Add((int)items.Data[i]);
                }
            }
        }

        static void ProcessFile(string filename, List<int> partitionHash, List<FileChunkInfo> fci)
        {
            var rollingHash = new BlockingCollectionDataChunk<uint>();
            var localMaximaPos = new BlockingCollectionDataChunk<int>();
            var ph = new BlockingCollectionDataChunk<uint>();
            var fciBC = new BlockingCollectionDataChunk<FileChunkInfo>();

            //var sw = new Stopwatch();
            //sw.Start();

            Task.Run(() =>
            {
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var fh = new FileHash(4);
                    fh.StreamToUInt32HashValues(fs, rollingHash);
                }
            });

            Task.Run(() =>
            {
                var lm = new LocalMaxima(2048);
                lm.CalcUsingBlockAlgo(rollingHash, localMaximaPos);
            });

            Task.Run(() =>
            {
                var mmh = new MurmurHash3_x86_32();
                var fph = new FileParitionHash(mmh);
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fph.ProcessStream(fs, localMaximaPos, ph, fciBC);
                }
            });

            var count = 0;
            foreach (var items in ph.BlockingCollection.GetConsumingEnumerable())
            {
                count += items.DataSize;
                //Console.WriteLine("File par hash: {0}", i);
                for (var i = 0; i < items.DataSize; ++i)
                {
                    partitionHash.Add((int)items.Data[i]);
                }
            }

            foreach (var items in fciBC.BlockingCollection.GetConsumingEnumerable())
            {
                for (var i = 0; i < items.DataSize; ++i)
                {
                    fci.Add(items.Data[i]);
                }
            }
            //sw.Stop();

            //Console.WriteLine("Number of partitions: {0}", count);
            //Console.WriteLine("Time: {0} ms", sw.ElapsedMilliseconds);
        }
    }
}