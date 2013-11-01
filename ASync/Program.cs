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
using ProtoBuf;
using NLog;

namespace ASync
{

    [ProtoContract]
    public class PatchData
    {
        [ProtoMember(1)]
        public int HashValue { get; set; }
        [ProtoMember(2)]
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
        [Option('i', "input", HelpText = "Input file name - new file", Required = true)]
        public string Input { get; set; }
        [Option('o', "bffile", HelpText = "Output bloom filter file name", Required = true)]
        public string BFFile { get; set; }
    }

    class CharacteristicPolynomialSubOptions
    {
        [Option('i', "input", HelpText = "Input file name - old file", Required = true)]
        public string Input { get; set; }
        [Option('b', "bffile", HelpText = "Bloom filter file name", Required = true)]
        public string BFFile { get; set; }
        [Option('o', "ouputFile", HelpText = "Output file name", Required = true)]
        public string Ouput { get; set; }
        [Option('a', "addx", HelpText = "Additional x values", Required = false)]
        public int AdditionalXVal { get; set; }
    }

    class DeltaFileSubOptions
    {
        [Option('i', "input", HelpText = "Input file name - new file", Required = true)]
        public string Input { get; set; }
        [Option('c', "cpfile", HelpText = "Characteristic Polynomial file name", Required = true)]
        public string CPFile { get; set; }
        [Option('o', "ouputFile", HelpText = "Output file name", Required = true)]
        public string Ouput { get; set; }
    }

    class PatchFileSubOptions
    {
        [Option('i', "input", HelpText = "Input file name - old file", Required = true)]
        public string Input { get; set; }
        [Option('d', "deltafile", HelpText = "Delta file name", Required = true)]
        public string DeltaFile { get; set; }
        [Option('o', "ouputFile", HelpText = "Output file name", Required = true)]
        public string Ouput { get; set; }
    }

    [ProtoContract]
    class CPData
    {
        [ProtoMember(1)]
        public int SetCount { get; set; }
        [ProtoMember(2)]
        public List<int> CPValues { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var clientDic = new Dictionary<string, string>();
            for (var i = 0; i < 1000; ++i)
            {
                clientDic.Add(i.ToString(), i.ToString());
            }

            var serverDic = new Dictionary<string, string>();
            for (var i = 0; i < 1100; ++i)
            {
                serverDic.Add(i.ToString(), (i).ToString());
            }
            var a = AreTheSame(clientDic, serverDic);

            using (var f = File.Create("clientDic.dat"))
            {
                Serializer.Serialize(f, clientDic);
            }

            using (var f = File.Create("serverDic.dat"))
            {
                Serializer.Serialize(f, serverDic);
            }

            SyncDic(clientDic, serverDic);

            var ans = AreTheSame(clientDic, serverDic);
        }



        static void SyncDic<TKey, TValue>(Dictionary<TKey, TValue> clientDic, Dictionary<TKey, TValue> serverDic)
        {
            var bf = new BloomFilter(clientDic.Count * 8, BloomFilter.DefaultHashFuncs());
            var hFunc = new MurmurHash3_x86_32();

            // Client side.
            foreach (var item in clientDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var hv = hFunc.ComputeHash(bBlock);

                bf.Add(hv);
            }

            // Server side
            var hitNum = 0;
            var patchDic = new Dictionary<TKey, TValue>();
            foreach (var item in serverDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var hv = hFunc.ComputeHash(bBlock);
                if (!bf.Contains(hv))
                {
                    patchDic.Add(item.Key, item.Value);
                }
                else
                {
                    hitNum++;
                }
            }

            var wrongNum = (int)Math.Ceiling(serverDic.Count * bf.FalsePositive);

            //var d0 = Helper.EstimateD0(bf.Count, serverDic.Count, hitNum, bf);

            // Client side
            foreach (var item in patchDic)
            {
                clientDic[item.Key] = item.Value;
            }
            // Phase 2: using set reconciliation
            var _cp = new CharacteristicPolynomial(FieldOrder);
            var xVal = GenXValues(wrongNum + VerificationNum);

            var clientSet = new List<int>();
            foreach (var item in clientDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var hv = hFunc.ComputeHash(bBlock);
                var hIntValue = BitConverter.ToInt32(hv, 0);
                clientSet.Add(hIntValue);
            }

            var cpClientSet = _cp.Calc(clientSet, xVal);

            // Server side
            var serverSet = new List<int>();
            var hashToKey = new Dictionary<int, TKey>();
            foreach (var item in serverDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var hv = hFunc.ComputeHash(bBlock);
                var hIntValue = BitConverter.ToInt32(hv, 0);
                serverSet.Add(hIntValue);
                hashToKey.Add(hIntValue, item.Key);
            }
            var cpServerSet = _cp.Calc(serverSet, xVal);

            var cpaocpb = _cp.Div(cpClientSet, cpServerSet);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                serverDic.Count - clientDic.Count,
                out p, out q);
            var missingFromOldList = _cp.Factoring(p);

            var patchPhase2 = new Dictionary<TKey, TValue>();
            foreach (var hValue in missingFromOldList)
            {
                var key = hashToKey[hValue];
                patchPhase2[key] = serverDic[key];
            }

            // Client side
            foreach (var item in patchPhase2)
            {
                clientDic[item.Key] = item.Value;
            }

        }

        static void SyncDicNaive<TKey, TValue>(Dictionary<TKey, TValue> clientDic, Dictionary<TKey, TValue> serverDic)
        {
            var hFunc = new MurmurHash3_x86_32();

            // Client side.
            var clientHValues = new List<int>();
            foreach (var item in clientDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var hv = hFunc.ComputeHash(bBlock);
                var hIntValue = BitConverter.ToInt32(hv, 0);

                clientHValues.Add(hIntValue);
            }

            // Server side.
            var clientHVSet = new HashSet<int>();
            foreach (var hv in clientHValues)
            {
                clientHVSet.Add(hv);
            }
            var patchDic = new Dictionary<TKey, TValue>();
            foreach (var item in serverDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var hv = hFunc.ComputeHash(bBlock);
                var hIntValue = BitConverter.ToInt32(hv, 0);

                if (!clientHVSet.Contains(hIntValue))
                {
                    patchDic.Add(item.Key, item.Value);
                }
            }

            // Client side
            foreach (var item in patchDic)
            {
                clientDic[item.Key] = item.Value;
            }
        }

        static bool AreTheSame<TKey, TValue>(Dictionary<TKey, TValue> dic1, Dictionary<TKey, TValue> dic2)
        {
            if (dic1.Count != dic2.Count)
            {
                return false;
            }
            foreach (var item in dic1)
            {
                if (!dic2.ContainsKey(item.Key))
                {
                    return false;
                }
                else if (!dic2[item.Key].Equals(item.Value))
                {
                    return false;
                }
            }

            return true;
        }

        // Only use the last 24 bits
        const int HashBitMask = 0xFFFFFF;
        const int VerificationNum = 1;
        const int FieldOrder = 2147483647;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        static void FullDirList(DirectoryInfo dir, List<FileInfo> fileList)
        {
            foreach (FileInfo f in dir.GetFiles("*.*"))
            {
                fileList.Add(f);
            }

            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                FullDirList(d, fileList);
            }
        }

        static bool CompareFolder(string folderA, string folderB)
        {
            var fileList = new List<FileInfo>();
            FullDirList(new DirectoryInfo(folderA), fileList);

            foreach (var fileAInfo in fileList)
            {
                var relativePath = fileAInfo.FullName.Remove(0, folderA.Length).Replace("\\", "/");

                Console.WriteLine(relativePath);

                var fileB = folderB + relativePath;

                var fileABytes = File.ReadAllBytes(fileAInfo.FullName);
                var fileBBytes = File.ReadAllBytes(fileB);

                if (fileABytes.Length != fileBBytes.Length)
                {
                    throw new InvalidDataException();
                }
                for (var i = 0; i < fileABytes.Length; ++i)
                {
                    if (fileABytes[i] != fileBBytes[i])
                    {
                        throw new InvalidDataException();
                    }
                }
            }
            return true;
        }

        static void SyncFolderBFFixedBlock(string newFolderPath, string oldFolderPath,
            string outputFolderPath, string outputAuthenticPath,
            string tempFolderPath)
        {
            var fileList = new List<FileInfo>();
            FullDirList(new DirectoryInfo(newFolderPath), fileList);

            foreach (var fNew in fileList)
            {
                var relativePath = fNew.FullName.Remove(0, newFolderPath.Length).Replace("\\", "/");

                var fOldFn = oldFolderPath + relativePath;
                if (File.Exists(fOldFn))
                {
                    Console.WriteLine(relativePath);

                    // Copy to authentic path
                    var authenFn = outputAuthenticPath + relativePath;
                    var authenFi = new FileInfo(authenFn);
                    authenFi.Directory.Create();
                    File.Copy(fNew.FullName, authenFn, true);

                    // Do synchronization
                    var bfFn = tempFolderPath + relativePath + ".bf.dat";
                    var deltaFn = tempFolderPath + relativePath + ".delta.dat";
                    var missingHVFn = tempFolderPath + relativePath + ".mh.dat";
                    var missingDelta = tempFolderPath + relativePath + ".md.dat";

                    var fileInfo = new FileInfo(bfFn);
                    fileInfo.Directory.Create();

                    var oFn = outputFolderPath + relativePath;
                    var oFileInfo = new FileInfo(oFn);
                    oFileInfo.Directory.Create();


                    ASyncFixedBlock.GenBFFileFromFixedBlockOfOldFile(fOldFn, bfFn);
                    ASyncFixedBlock.GenDeltaFileFromBFFixedSize(fNew.FullName, bfFn, deltaFn);
                    ASyncFixedBlock.GenMissingHashFile(fOldFn, deltaFn, missingHVFn);
                    ASyncFixedBlock.GenDeltaFromMissing(fNew.FullName, missingHVFn, missingDelta);
                    ASyncFixedBlock.PatchFile(fOldFn, deltaFn, missingDelta, oFn);
                }
            }
        }

        static void SyncFolder(string newFolderPath, string oldFolderPath,
            string outputFolderPath, string outputAuthenticPath,
            string tempFolderPath)
        {
            var fileList = new List<FileInfo>();
            FullDirList(new DirectoryInfo(newFolderPath), fileList);

            foreach (var fNew in fileList)
            {
                var relativePath = fNew.FullName.Remove(0, newFolderPath.Length).Replace("\\", "/");

                var fOldFn = oldFolderPath + relativePath;
                if (File.Exists(fOldFn))
                {
                    Console.WriteLine(relativePath);

                    // Copy to authentic path
                    var authenFn = outputAuthenticPath + relativePath;
                    var authenFi = new FileInfo(authenFn);
                    authenFi.Directory.Create();
                    File.Copy(fNew.FullName, authenFn, true);

                    // Do synchronization
                    var bfFn = tempFolderPath + relativePath + ".bf.dat";
                    var cpFn = tempFolderPath + relativePath + ".cp.dat";
                    var deltaFn = tempFolderPath + relativePath + ".delta.dat";

                    var fileInfo = new FileInfo(bfFn);
                    fileInfo.Directory.Create();
                    GenBFFile(fNew.FullName, bfFn);

                    var ok = false;
                    var additionalXValue = -1;
                    while (!ok)
                    {
                        ++additionalXValue;
                        if (additionalXValue > 0)
                        {
                            Console.WriteLine("Failed verification, inceases r to {0}", additionalXValue);
                        }

                        cpFn = tempFolderPath + relativePath + string.Format(".cp.dat{0}", additionalXValue);

                        GenCPFile(fOldFn, bfFn, cpFn, additionalXValue);

                        ok = GenDeltaFile(fNew.FullName, cpFn, deltaFn);
                    }

                    var oFn = outputFolderPath + relativePath;
                    var oFileInfo = new FileInfo(oFn);
                    oFileInfo.Directory.Create();
                    PatchFile(fOldFn, deltaFn, oFn);
                }
            }
        }



        static List<int> GenXValues(int num)
        {
            var ret = new List<int>(num);
            var addConst = 0x1000000;

            for (var i = 0; i < num; ++i)
            {
                ret.Add(i + addConst);
            }

            return ret;
        }

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
            using (var file = File.Create(bfFile))
            {
                Serializer.Serialize(file, bf);
            }
        }

        static void GenCPFile(string input, string bfFile, string cpFile, int additionalXValues)
        {
            BloomFilter bf;
            using (var file = File.OpenRead(bfFile))
            {
                bf = Serializer.Deserialize<BloomFilter>(file);
            }
            bf.SetHashFunctions(BloomFilter.DefaultHashFuncs());

            var setOld = new List<int>();
            var fciOld = new List<FileChunkInfo>();
            ProcessFile(input, setOld, fciOld);
            for (var i = 0; i < setOld.Count; ++i)
            {
                setOld[i] = setOld[i] & HashBitMask;
            }

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

            var _cp = new CharacteristicPolynomial(FieldOrder);
            var xVal = GenXValues(d0 + additionalXValues + VerificationNum);

            var cpb = _cp.Calc(setOld, xVal);

            var cpdata = new CPData
            {
                CPValues = cpb,
                SetCount = setOld.Count,
            };

            using (var file = File.Create(cpFile))
            {
                Serializer.Serialize(file, cpdata);
            }
        }

        static bool GenDeltaFile(string input, string cpFile, string deltaFile)
        {
            CPData cpd;
            using (var file = File.OpenRead(cpFile))
            {
                cpd = Serializer.Deserialize<CPData>(file);
            }

            var cpbVer = new List<int>();
            for (var i = 0; i < VerificationNum; ++i)
            {
                cpbVer.Add(cpd.CPValues[cpd.CPValues.Count - VerificationNum + i]);
            }
            cpd.CPValues.RemoveRange(cpd.CPValues.Count - VerificationNum, VerificationNum);

            var cpb = cpd.CPValues;

            var setNew = new List<int>();
            var fciNew = new List<FileChunkInfo>();
            ProcessFile(input, setNew, fciNew);
            for (var i = 0; i < setNew.Count; ++i)
            {
                setNew[i] = setNew[i] & HashBitMask;
            }

            var _cp = new CharacteristicPolynomial(FieldOrder);
            var d0 = cpb.Count;
            var xVal = GenXValues(d0);

            var cpa = _cp.Calc(setNew, xVal);
            var cpaocpb = _cp.Div(cpa, cpb);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                setNew.Count - cpd.SetCount,
                out p, out q);

            // TODO: verification.
            // If verification failed => return false;
            var xValVer = GenXValues(d0 + VerificationNum);
            xValVer.RemoveRange(0, d0);
            var cpaVer = _cp.Calc(setNew, xValVer);
            var cpaVeroCpbVer = _cp.Div(cpaVer, cpbVer);

            for (var i = 0; i < VerificationNum; ++i)
            {
                var pval = _cp.CalcCoeff(p, xValVer[i]);
                var qVal = _cp.CalcCoeff(q, xValVer[i]);
                var verNum = _cp.DivGF(pval, qVal);
                if (verNum != cpaVeroCpbVer[i])
                {
                    logger.Debug("Verification failed, need to increase d0");
                    return false;
                }
            }


            var missingFromOldList = _cp.Factoring(p);

            var missingSet = new HashSet<int>();
            foreach (var item in missingFromOldList)
            {
                missingSet.Add(item);
            }

            // Genereate delta file.
            var deltaData = new List<PatchData>();
            using (var fs = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read))
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
                    deltaData.Add(currPatch);
                }
            }

            using (var file = File.Create(deltaFile))
            {
                Serializer.Serialize(file, deltaData);
            }
            return true;
        }

        static void PatchFile(string input, string deltaFile, string output)
        {
            List<PatchData> deltaData;
            using (var file = File.OpenRead(deltaFile))
            {
                deltaData = Serializer.Deserialize<List<PatchData>>(file);
            }

            var setOld = new List<int>();
            var fciOld = new List<FileChunkInfo>();
            ProcessFile(input, setOld, fciOld);
            for (var i = 0; i < setOld.Count; ++i)
            {
                setOld[i] = setOld[i] & HashBitMask;
            }

            var existingSet = new Dictionary<int, int>();
            for (var i = 0; i < setOld.Count; ++i)
            {
                if (!existingSet.ContainsKey(setOld[i]))
                {
                    existingSet.Add(setOld[i], i);
                }
            }

            using (var fsout = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var fsOld = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    for (var i = 0; i < deltaData.Count; ++i)
                    {
                        var currPatch = deltaData[i];
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

            // Verification.
            var xValVer = new List<int>(VerificationNum);
            for (var i = 0; i < VerificationNum; ++i)
            {
                xValVer.Add(d0 + 1 + i);
            }
            var cpaVer = _cp.Calc(setNew, xValVer);
            var cpbVer = _cp.Calc(setOld, xValVer);
            var cpaVeroCpbVer = _cp.Div(cpaVer, cpbVer);

            for (var i = 0; i < VerificationNum; ++i)
            {
                var pval = _cp.CalcCoeff(p, xValVer[i]);
                var qVal = _cp.CalcCoeff(q, xValVer[i]);
                var verNum = _cp.DivGF(pval, qVal);
                if (verNum != cpaVeroCpbVer[i])
                {
                    logger.Debug("Verification failed, need to increase d0");
                    throw new InvalidOperationException();
                }
            }

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

            m += m % 8 == 0 ? 0 : 8 - (m % 8);

            var hList = BloomFilter.DefaultHashFuncs();

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

            //var fLength = 0;
            //using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            //{
            //    fLength = (int)fs.Length;
            //}

            //var lmWindow = fLength / (512);
            var lmWindow = 32 * 1024;


            Task.Run(() =>
            {
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var fh = new FileHash(16);
                    fh.StreamToHashValues(fs, rollingHash);
                }
            });

            Task.Run(() =>
            {
                var lm = new LocalMaxima(lmWindow);
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