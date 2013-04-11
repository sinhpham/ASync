using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    [ProtoContract]
    public class DeltaData
    {
        [ProtoMember(1)]
        public int HashValue { get; set; }

        [ProtoMember(2)]
        public byte[] Data { get; set; }

        public int ExpectedSize
        {
            get
            {
                if (Data != null)
                {
                    return Data.Length + 4;
                }
                return 8;
            }
        }
    }

    public class ASyncFixedBlock
    {
        public static int BlockSize = 2048;
        public static int BloomFilterRatio = 24;

        public static void GenBFFileFromFixedBlockOfOldFile(string oldFile, string bfFile)
        {
            var buff = new byte[BlockSize];
            var fLength = 0;


            using (var fs = new FileStream(oldFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fLength = (int)fs.Length;
                var nBlocks = fLength / BlockSize;
                var m = nBlocks * BloomFilterRatio;

                m += m % 8 == 0 ? 0 : 8 - (m % 8);

                var hList = BloomFilter.DefaultHashFuncs();

                var bf = new BloomFilter(m, hList);

                var byteRead = 0;

                for (var i = 0; i < nBlocks; ++i)
                {
                    byteRead = fs.Read(buff, 0, BlockSize);
                    if (byteRead != BlockSize)
                    {
                        throw new InvalidDataException();
                    }
                    bf.Add(buff, 0, byteRead);
                }
                // Serialize bf.
                using (var file = File.Create(bfFile))
                {
                    Serializer.Serialize(file, bf);
                }
            }
        }

        public static void GenDeltaFileFromBFFixedSize(string currFile, string bfFile, string deltaFile)
        {
            BloomFilter bf;
            using (var file = File.OpenRead(bfFile))
            {
                bf = Serializer.Deserialize<BloomFilter>(file);
            }
            bf.SetHashFunctions(BloomFilter.DefaultHashFuncs());

            // Hack, do not work for very large file.
            var fileBytes = File.ReadAllBytes(currFile);
            var currIdx = 0;
            var deltaDataList = new List<DeltaData>();
            var currDD = new DeltaData();
            var currRawData = new List<byte>();

            var hFunc = new MurmurHash3_x86_32();

            while (currIdx + BlockSize < fileBytes.Length)
            {
                if (bf.Contains(fileBytes, currIdx, BlockSize))
                {
                    if (currRawData.Count != 0)
                    {
                        currDD.Data = currRawData.ToArray();
                        currRawData.Clear();
                        deltaDataList.Add(currDD);
                    }
                    deltaDataList.Add(new DeltaData()
                    {
                        HashValue = BitConverter.ToInt32(hFunc.ComputeHash(fileBytes, currIdx, BlockSize), 0)
                    });
                    currDD = new DeltaData();
                    currIdx += BlockSize;
                }
                else
                {
                    currRawData.Add(fileBytes[currIdx]);
                    currIdx++;
                }
            }

            if (currIdx != fileBytes.Length)
            {
                for (var i = currIdx; i < fileBytes.Length; ++i)
                {
                    currRawData.Add(fileBytes[i]);
                }
            }

            if (currRawData.Count != 0)
            {
                currDD.Data = currRawData.ToArray();
                currRawData.Clear();
                deltaDataList.Add(currDD);
            }

            var es = 0;
            foreach (var d in deltaDataList)
            {
                es += d.ExpectedSize;
            }

            using (var file = File.Create(deltaFile))
            {
                Serializer.Serialize(file, deltaDataList);
            }
        }

        public static void GenMissingHashFile(string oldFile, string deltaFile, string missingHashFile)
        {
            // Delta data from server.
            List<DeltaData> deltaDataList;
            using (var fs = File.OpenRead(deltaFile))
            {
                deltaDataList = Serializer.Deserialize<List<DeltaData>>(fs);
            }

            // Construct all hash values from client.
            var existingHashValues = new HashSet<int>();
            var buff = new byte[BlockSize];
            var hFunc = new MurmurHash3_x86_32();
            using (var fs = new FileStream(oldFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var byteRead = 0;
                while ((byteRead = fs.Read(buff, 0, BlockSize)) != 0)
                {
                    var hv = BitConverter.ToInt32(hFunc.ComputeHash(buff, 0, byteRead), 0);
                    existingHashValues.Add(hv);
                }
            }

            var missingList = new List<int>();
            foreach (var dd in deltaDataList)
            {
                if (dd.Data == null)
                {
                    var currHash = dd.HashValue;
                    if (!existingHashValues.Contains(currHash))
                    {
                        missingList.Add(currHash);
                    }
                }
            }

            using (var fs = File.Create(missingHashFile))
            {
                Serializer.Serialize(fs, missingList);
            }
        }

        public static void GenDeltaFromMissing(string currFile, string missingHashFile, string missingDeltaFile)
        {
            List<int> missingList;
            using (var fs = File.OpenRead(missingHashFile))
            {
                missingList = Serializer.Deserialize<List<int>>(fs);
            }

            var missingSet = new HashSet<int>(missingList);


            // HACK.
            var fileBytes = File.ReadAllBytes(currFile);
            var currIdx = 0;
            var deltaDataList = new Dictionary<int, byte[]>();

            var hFunc = new MurmurHash3_x86_32();
            while (missingSet.Count != 0 && currIdx + BlockSize < fileBytes.Length)
            {
                var hv = BitConverter.ToInt32(hFunc.ComputeHash(fileBytes, currIdx, BlockSize), 0);
                if (missingSet.Contains(hv))
                {
                    var data = new byte[BlockSize];
                    for (var i = currIdx; i < currIdx + BlockSize; ++i)
                    {
                        data[i - currIdx] = fileBytes[i];
                    }

                    deltaDataList.Add(hv, data);
                    currIdx += BlockSize;

                    missingSet.Remove(hv);
                }
                else
                {
                    currIdx++;
                }
            }

            using (var fs = File.Create(missingDeltaFile))
            {
                Serializer.Serialize(fs, deltaDataList);
            }
        }

        public static void PatchFile(string oldFile, string deltaFile, string deltaMissingFile, string outFile)
        {
            // Delta data from server.
            List<DeltaData> deltaDataList;
            using (var fs = File.OpenRead(deltaFile))
            {
                deltaDataList = Serializer.Deserialize<List<DeltaData>>(fs);
            }
            // Missing delta data from server
            Dictionary<int, byte[]> missingDeltaData;
            using (var fs = File.OpenRead(deltaMissingFile))
            {
                missingDeltaData = Serializer.Deserialize<Dictionary<int, byte[]>>(fs);
            }

            // Construct new file.

            // Construct all hash values from client.
            var existingHashValues = new Dictionary<int, int>();
            var currBlockIdx = 0;
            var buff = new byte[BlockSize];
            var hFunc = new MurmurHash3_x86_32();
            using (var fs = new FileStream(oldFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var byteRead = 0;
                while ((byteRead = fs.Read(buff, 0, BlockSize)) != 0)
                {
                    var hv = BitConverter.ToInt32(hFunc.ComputeHash(buff, 0, byteRead), 0);
                    if (!existingHashValues.ContainsKey(hv))
                    {
                        existingHashValues.Add(hv, currBlockIdx);
                    }
                    currBlockIdx++;
                }
            }

            using (var fsout = new FileStream(outFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var fsOld = new FileStream(oldFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    for (var i = 0; i < deltaDataList.Count; ++i)
                    {
                        var currPatch = deltaDataList[i];
                        if (currPatch.Data == null)
                        {
                            // Existing data.
                            if (existingHashValues.ContainsKey(currPatch.HashValue))
                            {
                                var idx = existingHashValues[currPatch.HashValue];

                                fsOld.Position = idx * BlockSize;

                                var fcData = new byte[BlockSize];
                                var bRead = fsOld.Read(fcData, 0, BlockSize);
                                //if (bRead != currFileChunkInfo.Length)
                                //{
                                //    throw new InvalidDataException();
                                //}
                                fsout.Write(fcData, 0, bRead);
                            }
                            else
                            {
                                // Should be in the missing delta file.
                                var d = missingDeltaData[currPatch.HashValue];
                                fsout.Write(d, 0, d.Length);
                            }
                        }
                        else
                        {
                            // New data.
                            var newdataBytes = currPatch.Data.ToArray();
                            fsout.Write(newdataBytes, 0, newdataBytes.Length);
                        }
                    }
                }
            }
        }


        public static void Sync(string oldFile, string currFile, string outFile)
        {
            var bfFile = "bf.dat";
            var deltaFile = "delta.dat";
            var mHashFile = "missingH.dat";
            var mDeltaFile = "missingDelta.dat";


            GenBFFileFromFixedBlockOfOldFile(oldFile, bfFile);
            GenDeltaFileFromBFFixedSize(currFile, bfFile, deltaFile);
            GenMissingHashFile(oldFile, deltaFile, mHashFile);
            GenDeltaFromMissing(currFile, mHashFile, mDeltaFile);
            PatchFile(oldFile, deltaFile, mDeltaFile, outFile);
        }
    }
}
