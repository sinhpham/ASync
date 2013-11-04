using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    class IBF
    {
        public IBF(int size, ICollection<HashAlgorithm> hashFunctions)
        {
            _hashSum = new int[size];
            _idSum = new int[size];
            _count = new int[size];
        }


        ICollection<HashAlgorithm> _hFuncs;
        int[] _hashSum;
        int[] _idSum;
        int[] _count;

        int Size
        {
            get
            {
                return _hashSum.Length;
            }
        }

        public void Add(int id, byte[] buffer)
        {
            Add(id, buffer, 0, buffer.Length);
        }

        public void Add(int id, byte[] buffer, int offset, int count)
        {
            var anotherHashFunc = new MurmurHash3_x86_32();
            anotherHashFunc.Seed = 123456789;

            foreach (var h in _hFuncs)
            {
                var idx = (int)(BitConverter.ToUInt32(h.ComputeHash(buffer, offset, count), 0) % Size);

                var hVal = BitConverter.ToInt32(anotherHashFunc.ComputeHash(buffer, offset, count), 0);
                _count[idx]++;
                _idSum[idx] ^= id;
                _hashSum[idx] ^= hVal;
            }
        }

        public void Remove(int id, byte[] buffer)
        {
            Remove(id, buffer, 0, buffer.Length);
        }

        public void Remove(int id, byte[] buffer, int offset, int count)
        {
            var anotherHashFunc = new MurmurHash3_x86_32();
            anotherHashFunc.Seed = 123456789;

            foreach (var h in _hFuncs)
            {
                var idx = (int)(BitConverter.ToUInt32(h.ComputeHash(buffer, offset, count), 0) % Size);

                var hVal = BitConverter.ToInt32(anotherHashFunc.ComputeHash(buffer, offset, count), 0);
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
                var anotherHashFunc = new MurmurHash3_x86_32();
                anotherHashFunc.Seed = 123456789;
                foreach (var h in _hFuncs)
                {
                    var bArr = BitConverter.GetBytes(currId);
                    var idx = (int)(BitConverter.ToUInt32(h.ComputeHash(bArr), 0) % Size);
                    var hVal = BitConverter.ToInt32(anotherHashFunc.ComputeHash(bArr), 0);

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
            var anotherHashFunc = new MurmurHash3_x86_32();
            anotherHashFunc.Seed = 123456789;

            var bArr = BitConverter.GetBytes(_idSum[idx]);
            var hVal = BitConverter.ToInt32(anotherHashFunc.ComputeHash(bArr), 0);

            return ((_count[idx] == 1 || _count[idx] == -1) && hVal == _hashSum[idx]);
        }
    }
}
