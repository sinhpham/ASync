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
        const int HashNumForIBF = 3;
        static IHashFunc hFunc = new MurmurHash3_x86_32();

        public static void ClientGenBfFile<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> clientDic, int clientNumOfItems, Stream clientBFFile)
        {
            // Using 8 bits per item.
            var bf = new BloomFilter(clientNumOfItems * 8, BloomFilter.DefaultHashFuncs());

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

        public static void ServerGenPatch1File<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> serverDic, int serverNumOfItems, Stream clientBFFile, Stream patch1File)
        {
            var bf = Serializer.Deserialize<BloomFilter>(clientBFFile);

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

            var estimatedD0 = Helper.EstimateD0(bf.Count, serverNumOfItems, hitNum, bf) + 20;
            var remainingD0 = estimatedD0 - patchDic.Count;

            using (var sw = new StreamWriter(patch1File, Encoding.UTF8, 4096, true))
            {
                sw.WriteLine(remainingD0);
                foreach (var item in patchDic)
                {
                    sw.WriteLine(string.Format("{0} {1}", item.Key, item.Value));
                }
            }
        }

        public static void ClientPatchAndGenIBFFile<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> clientDic, Action<KeyValuePair<TKey, TValue>> patchingAct,
            IEnumerable<KeyValuePair<TKey, TValue>> patchItems, int estimatedDiff, Stream ibfFile)
        {
            // Apply patch 1.
            ClientApplyPatch(patchingAct, patchItems);
            
            // Phase 2: using invertible bloom filter
            ClientGenIBF(clientDic, estimatedDiff, ibfFile);
        }

        public static void ClientGenIBF<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> clientDic, int estimatedDiff, Stream ibfFile)
        {
            // We need approx 1.5 * d0 for ibf to decode, use 2 here.
            var ibf = new IBF(estimatedDiff * 2, BloomFilter.DefaultHashFuncs(HashNumForIBF));
            foreach (var item in clientDic)
            {
                var id = KeyValToId(item);

                ibf.Add(id);
            }

            Serializer.Serialize(ibfFile, ibf);
        }

        public static void ServerGenPatch2FromIBF<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> serverDic, Func<TKey, TValue> readingAct,
            Stream clientIBFFile, Stream patch2File)
        {
            var clientIBF = Serializer.Deserialize<IBF>(clientIBFFile);

            clientIBF.SetHashFunctions(BloomFilter.DefaultHashFuncs(HashNumForIBF));

            var serverIBF = new IBF(clientIBF.Size, BloomFilter.DefaultHashFuncs(HashNumForIBF));
            var idToKey = new Dictionary<long, TKey>();

            foreach (var item in serverDic)
            {
                var id = KeyValToId(item);

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
                patchDic[key] = readingAct(key);
            }

            Serializer.Serialize(patch2File, patchDic);
        }

        public static void ClientApplyPatch<TKey, TValue>(Action<KeyValuePair<TKey, TValue>> patchingAct, IEnumerable<KeyValuePair<TKey, TValue>> patchItems)
        {
            //var patchDic = new Dictionary<TKey, TValue>();
            //patchDic = Serializer.Deserialize<Dictionary<TKey, TValue>>(patch2File);

            foreach (var item in patchItems)
            {
                patchingAct(item);
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

        public static long KeyValToId<TKey, TValue>(KeyValuePair<TKey, TValue> item)
        {
            var hFunc = new MurmurHash3_x64_128();

            var block = item.Key + "-" + item.Value;
            var bBlock = Helper.GetBytes(block);

            var id = BitConverter.ToInt64(hFunc.ComputeHash(bBlock), 0);

            return id;
        }
    }
}
