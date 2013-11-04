using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    [ProtoContract]
    public class IBF
    {
        public IBF()
        {

        }

        public IBF(int size, ICollection<HashAlgorithm> hashFunctions)
        {
            _hashSum = new int[size];
            _idSum = new int[size];
            _count = new int[size];

            _hFuncs = hashFunctions;
            _hcFunc = new MurmurHash3_x86_32()
            {
                Seed = 123456789
            };
        }

        HashAlgorithm _hcFunc;
        ICollection<HashAlgorithm> _hFuncs;
        [ProtoMember(1)]
        int[] _hashSum;
        [ProtoMember(2)]
        int[] _idSum;
        [ProtoMember(3)]
        int[] _count;

        public int Size
        {
            get
            {
                return _hashSum.Length;
            }
        }

        public void SetHashFunctions(ICollection<HashAlgorithm> hashFunctions)
        {
            _hFuncs = hashFunctions;
            _hcFunc = new MurmurHash3_x86_32()
            {
                Seed = 123456789
            };
        }

        public void Add(int id)
        {
            foreach (var h in _hFuncs)
            {
                var idx = CalcIdx(id, h);

                var hVal = CalcHcVal(id);
                _count[idx]++;
                _idSum[idx] ^= id;
                _hashSum[idx] ^= hVal;
            }
        }

        public bool Contains(int id)
        {
            foreach (var h in _hFuncs)
            {
                var idx = CalcIdx(id, h);
                if (_count[idx] == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void Remove(int id)
        {
            foreach (var h in _hFuncs)
            {
                var idx = CalcIdx(id, h);
                var hVal = CalcHcVal(id);
                _count[idx]--;
                _idSum[idx] ^= id;
                _hashSum[idx] ^= hVal;
            }
        }

        public IBF Substract(IBF x)
        {
            if (Size != x.Size)
            {
                throw new InvalidOperationException();
            }
            var ret = new IBF(Size, _hFuncs);

            for (var i = 0; i < Size; ++i)
            {
                ret._hashSum[i] = _hashSum[i] ^ x._hashSum[i];
                ret._idSum[i] = _idSum[i] ^ x._idSum[i];
                ret._count[i] = _count[i] - x._count[i];
            }
            return ret;
        }

        public bool Decode(List<int> amb, List<int> bma)
        {
            var pureListIdx = new Queue<int>();
            for (var i = 0; i < Size; ++i)
            {
                if (IsPure(i))
                {
                    pureListIdx.Enqueue(i);
                }
            }

            while (pureListIdx.Count != 0)
            {
                var currIdx = pureListIdx.Dequeue();
                if (!IsPure(currIdx))
                {
                    continue;
                }
                var currId = _idSum[currIdx];
                var currCount = _count[currIdx];
                if (currCount > 0)
                {
                    amb.Add(currId);
                }
                else
                {
                    bma.Add(currId);
                }

                foreach (var h in _hFuncs)
                {
                    var idx = CalcIdx(currId, h);
                    var hVal = CalcHcVal(currId);

                    _count[idx] -= currCount;
                    _idSum[idx] ^= currId;
                    _hashSum[idx] ^= hVal;

                    if (IsPure(idx))
                    {
                        pureListIdx.Enqueue(idx);
                    }
                }
            }
            for (var i = 0; i < Size; ++i)
            {
                if (_count[i] != 0 || _hashSum[i] != 0 || _idSum[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        bool IsPure(int idx)
        {
            var hVal = CalcHcVal(_idSum[idx]);

            return ((_count[idx] == 1 || _count[idx] == -1) && hVal == _hashSum[idx]);
        }

        int CalcIdx(int id, HashAlgorithm hFunc)
        {
            return (int)(BitConverter.ToUInt32(hFunc.ComputeHash(BitConverter.GetBytes(id)), 0) % Size);
        }

        int CalcHcVal(int id)
        {
            return BitConverter.ToInt32(_hcFunc.ComputeHash(BitConverter.GetBytes(id)), 0);
        }
    }
}
