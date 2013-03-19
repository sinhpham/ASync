using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    public class BloomFilter
    {
        public BloomFilter(int bitLength, ICollection<HashAlgorithm> hashFunctions)
        {
            _hFuncs = hashFunctions;
            _bitArray = new BitArray(bitLength);
            Count = 0;
        }

        public void Add(byte[] buffer)
        {
            Add(buffer, 0, buffer.Length);
        }

        public void Add(byte[] buffer, int offset, int count)
        {
            foreach (var h in _hFuncs)
            {
                var idx = (int)(BitConverter.ToUInt32(h.ComputeHash(buffer, offset, count), 0) % BitLength);
                _bitArray[idx] = true;
            }
            Count++;
        }

        public bool Contains(byte[] buffer)
        {
            return Contains(buffer, 0, buffer.Length);
        }

        public bool Contains(byte[] buffer, int offset, int count)
        {
            foreach (var h in _hFuncs)
            {
                var idx = (int)(BitConverter.ToUInt32(h.ComputeHash(buffer, offset, count), 0) % BitLength);
                if (!_bitArray[idx])
                {
                    return false;
                }
            }
            return true;
        }

        BitArray _bitArray;
        ICollection<HashAlgorithm> _hFuncs;
        public int BitLength { get { return _bitArray.Length; } }
        public int NHashFuncs { get { return _hFuncs.Count; } }
        public int Count { get; private set; }
        public double FalsePositive
        {
            get
            {
                var inner = Math.Pow(1 - 1.0/BitLength, NHashFuncs * Count);
                
                var ret = Math.Pow(1 - inner, NHashFuncs);
                return ret;
            }
        }
    }
}
