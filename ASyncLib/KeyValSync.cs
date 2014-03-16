using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ASyncLib
{
    public class KeyValSync
    {
        // Only use the last 24 bits
        const int HashBitMask = 0xFFFFFFF;

        public static void ClientGenBfFile<TKey, TValue>(Dictionary<TKey, TValue> clientDic, Stream clientBFFile)
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

            Serializer.Serialize(clientBFFile, bf);

            Debug.WriteLine("Expected bloom filter size: {0} bytes", bf.BitLength / 8 + 4);
        }

        public static void ServerGenPatch1File<TKey, TValue>(Dictionary<TKey, TValue> serverDic, Stream clientBFFile, Stream patch1File)
        {
            var hFunc = new MurmurHash3_x86_32();
            BloomFilter bf;

            bf = Serializer.Deserialize<BloomFilter>(clientBFFile);

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


            Serializer.Serialize(patch1File, Tuple.Create(remainingD0, patchDic));
        }

        public static void ClientPatchAndGenIBFFile<TKey, TValue>(Dictionary<TKey, TValue> clientDic, Stream patch1File, Stream ibfFile)
        {
            var d0 = 0;
            var patchDic = new Dictionary<TKey, TValue>();

            var t = Serializer.Deserialize<Tuple<int, Dictionary<TKey, TValue>>>(patch1File);
            d0 = t.Item1;
            patchDic = t.Item2;
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


            Serializer.Serialize(ibfFile, ibf);
        }

        public static void ServerGenPatch2FromIBF<TKey, TValue>(Dictionary<TKey, TValue> serverDic, Stream clientIBFFile, Stream patch2File)
        {
            IBF clientIBF;

            clientIBF = Serializer.Deserialize<IBF>(clientIBFFile);

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


            Serializer.Serialize(patch2File, patchDic);
        }

        public static void ClientPatch<TKey, TValue>(Dictionary<TKey, TValue> clientDic, Stream patch2File)
        {
            var patchDic = new Dictionary<TKey, TValue>();
            patchDic = Serializer.Deserialize<Dictionary<TKey, TValue>>(patch2File);

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
}
