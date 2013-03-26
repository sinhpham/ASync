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
using CommandLine;
using CommandLine.Text;
using System.Reflection;

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

    class Options
    {
        [VerbOption("genbf", HelpText = "Generate Bloom Filter file")]
        public BloomFilterSubOptions GenBFVerb { get; set; }

        [VerbOption("gencp", HelpText = "Generate characteristic polynomial file")]
        public CharacteristicPolynomialSubOptions GenCPVerb { get; set; }

        [VerbOption("gend", HelpText = "Generate delta file")]
        public DeltaFileSubOptions GenDVerb { get; set; }

        [VerbOption("patch", HelpText = "Patch file")]
        public PatchFileSubOptions PatchVerb { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }

    class BloomFilterSubOptions
    {
        [Option('i', "input", HelpText = "Input file name", Required=true)]
        public string Input { get; set; }
        [Option('o', "bffile", HelpText = "Output bloom filter file name", Required = true)]
        public string BFFile { get; set; }
    }

    class CharacteristicPolynomialSubOptions
    {
        [Option('i', "input", HelpText = "Input file name", Required = true)]
        public string Input { get; set; }
        [Option('b', "bffile", HelpText = "Bloom filter file name", Required = true)]
        public string BFFile { get; set; }
        [Option('o', "ouputFile", HelpText = "Output file name", Required = true)]
        public string Ouput { get; set; }
    }

    class DeltaFileSubOptions
    {
        [Option('i', "input", HelpText = "Input file name", Required = true)]
        public string Input { get; set; }
        [Option('c', "cpfile", HelpText = "Characteristic Polynomial file name", Required = true)]
        public string BFFile { get; set; }
        [Option('o', "ouputFile", HelpText = "Output file name", Required = true)]
        public string Ouput { get; set; }
    }

    class PatchFileSubOptions
    {
        [Option('i', "input", HelpText = "Input file name", Required = true)]
        public string Input { get; set; }
        [Option('d', "deltafile", HelpText = "Delta file name", Required = true)]
        public string BFFile { get; set; }
        [Option('o', "ouputFile", HelpText = "Output file name", Required = true)]
        public string Ouput { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            string invokedVerb = "";
            object invokedVerbInstance = null;

            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options,
                (verb, subOptions) =>
                {
                    // if parsing succeeds the verb name and correct instance
                    // will be passed to onVerbCommand delegate (string,object)
                    invokedVerb = verb;
                    invokedVerbInstance = subOptions;
                }))
            {
                if (invokedVerb != "")
                {
                    Console.Write(options.GetUsage(invokedVerb));
                }
                else
                {
                    Console.Write(options.GetUsage());
                }
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            var a = 0;
            switch (invokedVerb)
            {
                case "genbf":
                    var bfOptions = (BloomFilterSubOptions)invokedVerbInstance;
                    GenBFFile(bfOptions.Input, bfOptions.BFFile);
                    break;
                case "gencp":
                    break;
                case "gend":
                    break;
                case "patch":
                    break;
            }
        }

        // Only use the last 24 bits
        const int HashBitMask = 0xFFFFFF;

        static void GenBFFile(string inputFile, string bfFile)
        {
            var setNew = new List<int>();
            var fciNew = new List<FileChunkInfo>();
            ProcessFile(inputFile, setNew, fciNew);
            for (var i = 0; i < setNew.Count; ++i)
            {
                setNew[i] = setNew[i] & HashBitMask;
            }

            var bf = GenerateBF(setNew);

            // Serialize bf.

        }

        static void GenCPFile(string input, string bfFile, string cpFile)
        {

        }

        static void Sync(string oldFileName, string newFileName, string outputFileName)
        {
            var setNew = new List<int>();
            var fciNew = new List<FileChunkInfo>();
            ProcessFile(newFileName, setNew, fciNew);
            for (var i = 0; i < setNew.Count; ++i)
            {
                setNew[i] = setNew[i] & HashBitMask;
            }
            var setOld = new List<int>();
            var fciOld = new List<FileChunkInfo>();
            ProcessFile(oldFileName, setOld, fciOld);
            for (var i = 0; i < setOld.Count; ++i)
            {
                setOld[i] = setOld[i] & HashBitMask;
            }

            // On device A.
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
            var d0 = (int)(Helper.EstimateD0(bf.Count, setOld.Count, n0, bf) + 3);

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

            var bfSize = Helper.SizeOfBF(bf);
            var cpbSize = Helper.SizeCPB(cpb);
            var pfSize = Helper.SizeOfPatchFile(patchFile);

            var totalBandwidth = bfSize + cpbSize + pfSize;

            Console.WriteLine("Total: {0} bytes", totalBandwidth);
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
                    var fh = new FileHash(64);
                    fh.StreamToHashValues(fs, rollingHash);
                }
            });

            Task.Run(() =>
            {
                var lm = new LocalMaxima(512);
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