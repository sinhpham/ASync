using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    class KeyValSync
    {
        // Only use the last 24 bits
        const int HashBitMask = 0xFFFFFFF;

        public static void ClientGenBfFile<TKey, TValue>(Dictionary<TKey, TValue> clientDic, string clientBFFile)
        {
            // Using 8 bits per item.
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

            using (var file = File.Create(clientBFFile))
            {
                Serializer.Serialize(file, bf);
            }

            Debug.WriteLine("Expected bloom filter size: {0} bytes", bf.BitLength / 8 + 4);
        }

        public static void ServerGenPatch1File<TKey, TValue>(Dictionary<TKey, TValue> serverDic, string clientBFFile, string patch1File)
        {
            var hFunc = new MurmurHash3_x86_32();
            BloomFilter bf;
            using (var file = File.OpenRead(clientBFFile))
            {
                bf = Serializer.Deserialize<BloomFilter>(file);
            }
            bf.SetHashFunctions(BloomFilter.DefaultHashFuncs());

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

            var estimatedDo = Helper.EstimateD0(bf.Count, serverDic.Count, hitNum, bf) + 20;
            var remainingD0 = estimatedDo - patchDic.Count;

            using (var file = File.Create(patch1File))
            {
                Serializer.Serialize(file, Tuple.Create(remainingD0, patchDic));
            }
        }

        public static void ClientPatchAndGenIBFFile<TKey, TValue>(Dictionary<TKey, TValue> clientDic, string patch1File, string ibfFile)
        {
            var d0 = 0;
            var patchDic = new Dictionary<TKey, TValue>();
            using (var file = File.OpenRead(patch1File))
            {
                var t = Serializer.Deserialize<Tuple<int, Dictionary<TKey, TValue>>>(file);
                d0 = t.Item1;
                patchDic = t.Item2;
            }
            // Apply patch 1.
            foreach (var item in patchDic)
            {
                clientDic[item.Key] = item.Value;
            }

            // Phase 2: using invertible bloom filter
            var ibf = new IBF(2 * d0, BloomFilter.DefaultHashFuncs(3));
            var hFunc = new MurmurHash3_x64_128();
            foreach (var item in clientDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var id = BitConverter.ToInt64(hFunc.ComputeHash(bBlock), 0);

                ibf.Add(id);
            }

            using (var file = File.Create(ibfFile))
            {
                Serializer.Serialize(file, ibf);
            }
        }

        public static void ServerGenPatch2FromIBF<TKey, TValue>(Dictionary<TKey, TValue> serverDic, string clientIBFFile, string patch2File)
        {
            IBF clientIBF;
            using (var file = File.OpenRead(clientIBFFile))
            {
                clientIBF = Serializer.Deserialize<IBF>(file);
            }

            clientIBF.SetHashFunctions(BloomFilter.DefaultHashFuncs(3));

            var hFunc = new MurmurHash3_x64_128();
            var serverIBF = new IBF(clientIBF.Size, BloomFilter.DefaultHashFuncs(3));
            var idToKey = new Dictionary<long, TKey>();

            foreach (var item in serverDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var id = BitConverter.ToInt64(hFunc.ComputeHash(bBlock), 0);

                serverIBF.Add(id);
                idToKey.Add(id, item.Key);
            }

            var sIBF = clientIBF - serverIBF;
            var idSmC = new List<long>();
            var idCmS = new List<long>();
            if (!sIBF.Decode(idCmS, idSmC))
            {
                throw new Exception("Decoding ibf failed");
            }

            var patchDic = new Dictionary<TKey, TValue>();
            foreach (var hValue in idSmC)
            {
                var key = idToKey[hValue];
                patchDic[key] = serverDic[key];
            }

            using (var file = File.Create(patch2File))
            {
                Serializer.Serialize(file, patchDic);
            }
        }

        public static void ClientPatch<TKey, TValue>(Dictionary<TKey, TValue> clientDic, string patch2File)
        {
            var patchDic = new Dictionary<TKey, TValue>();
            using (var file = File.OpenRead(patch2File))
            {
                patchDic = Serializer.Deserialize<Dictionary<TKey, TValue>>(file);
            }

            foreach (var item in patchDic)
            {
                clientDic[item.Key] = item.Value;
            }
        }

        public static bool AreTheSame<TKey, TValue>(Dictionary<TKey, TValue> dic1, Dictionary<TKey, TValue> dic2)
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
    }

    class KeyValSyncNaive
    {
        public static void ClientGenHashFile<TKey, TValue>(Dictionary<TKey, TValue> clientDic, string hFile)
        {
            var hFunc = new MurmurHash3_x86_32();

            // Client side.
            var clientHValues = new HashSet<int>();
            foreach (var item in clientDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var hv = hFunc.ComputeHash(bBlock);
                var hIntValue = BitConverter.ToInt32(hv, 0);

                clientHValues.Add(hIntValue);
            }

            using (var file = File.Create(hFile))
            {
                using (var bw = new BinaryWriter(file))
                {
                    foreach (var v in clientHValues)
                    {
                        bw.Write(v);
                    }
                }
            }

            Debug.WriteLine("Expected size: {0} bytes", clientHValues.Count * 4);
        }

        public static void ServerGenPatchFile<TKey, TValue>(Dictionary<TKey, TValue> serverDic, string clientHashFile, string serverPatchFile)
        {
            var clientHValues = new HashSet<int>();
            using (var file = File.OpenRead(clientHashFile))
            {
                using (var br = new BinaryReader(file))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        var currV = br.ReadInt32();
                        clientHValues.Add(currV);
                    }
                }
            }

            var hFunc = new MurmurHash3_x86_32();

            var patchDic = new Dictionary<TKey, TValue>();
            foreach (var item in serverDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var hv = hFunc.ComputeHash(bBlock);
                var hIntValue = BitConverter.ToInt32(hv, 0);

                if (!clientHValues.Contains(hIntValue))
                {
                    patchDic.Add(item.Key, item.Value);
                }
            }

            using (var file = File.Create(serverPatchFile))
            {
                Serializer.Serialize(file, patchDic);
            }
        }

        public static void ClientPatch<TKey, TValue>(Dictionary<TKey, TValue> clientDic, string patchFile)
        {
            var patchDic = new Dictionary<TKey, TValue>();
            using (var file = File.OpenRead(patchFile))
            {
                patchDic = Serializer.Deserialize<Dictionary<TKey, TValue>>(file);
            }

            foreach (var item in patchDic)
            {
                clientDic[item.Key] = item.Value;
            }
        }
    }
}
