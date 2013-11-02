using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    class KeyValSync
    {
        // Only use the last 24 bits
        const int HashBitMask = 0xFFFFFFF;
        const int VerificationNum = 1;
        const int FieldOrder = 2147483647;

        public static void SyncDic<TKey, TValue>(Dictionary<TKey, TValue> clientDic, Dictionary<TKey, TValue> serverDic)
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

            var estimatedDo = Helper.EstimateD0(clientDic.Count, serverDic.Count, hitNum, bf) + 6;
            //var wrongNum = (int)Math.Ceiling(serverDic.Count * bf.FalsePositive);

            var d0 = estimatedDo - patchDic.Count;

            // Client side
            foreach (var item in patchDic)
            {
                clientDic[item.Key] = item.Value;
            }

            SyncDicSetRecon(clientDic, serverDic, d0);
        }

        static void SyncDicSetRecon<TKey, TValue>(Dictionary<TKey, TValue> clientDic, Dictionary<TKey, TValue> serverDic, int d0)
        {
            var hFunc = new MurmurHash3_x86_32();
            // Phase 2: using set reconciliation
            var _cp = new CharacteristicPolynomial(FieldOrder);
            var xVal = GenXValues(FieldOrder, d0);

            var clientSet = new List<int>();
            foreach (var item in clientDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var hv = hFunc.ComputeHash(bBlock);
                var hIntValue = BitConverter.ToInt32(hv, 0) & HashBitMask;
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
                var hIntValue = BitConverter.ToInt32(hv, 0) & HashBitMask;
                serverSet.Add(hIntValue);
                hashToKey.Add(hIntValue, item.Key);
            }
            var cpServerSet = _cp.Calc(serverSet, xVal);

            var cpaocpb = _cp.Div(cpClientSet, cpServerSet);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                clientDic.Count - serverDic.Count,
                out p, out q);
            var missingFromOldList = _cp.Factoring(q);

            var patchDic = new Dictionary<TKey, TValue>();
            foreach (var hValue in missingFromOldList)
            {
                var key = hashToKey[hValue];
                patchDic[key] = serverDic[key];
            }

            // Client side
            foreach (var item in patchDic)
            {
                clientDic[item.Key] = item.Value;
            }

        }

        public static void SyncDicNaive<TKey, TValue>(Dictionary<TKey, TValue> clientDic, Dictionary<TKey, TValue> serverDic)
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

        static List<int> GenXValues(int fieldOrder, int num)
        {
            var ret = new List<int>(num);
            var addConst = fieldOrder - num;

            for (var i = 0; i < num; ++i)
            {
                ret.Add(i + addConst);
            }

            return ret;
        }
    }
}
