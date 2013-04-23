using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    [ProtoContract]
    public class BloomFilter
    {
        public BloomFilter()
        {

        }

        public BloomFilter(int bitLength, ICollection<HashAlgorithm> hashFunctions)
        {
            if (bitLength % 8 != 0)
            {
                throw new ArgumentException("bit length should be divisible by 8");
            }

            _hFuncs = hashFunctions;
            var bfLengthInByte = bitLength / 8;
            _byteArr = new byte[bfLengthInByte];
            Count = 0;
        }

        [ProtoMember(1)]
        byte[] _byteArr;
        ICollection<HashAlgorithm> _hFuncs;
        public int BitLength { get { return _byteArr.Length * 8; } }
        public int NHashFuncs { get { return _hFuncs.Count; } }
        [ProtoMember(2)]
        public int Count { get; private set; }
        public double FalsePositive
        {
            get
            {
                var inner = Math.Pow(1 - 1.0 / BitLength, NHashFuncs * Count);

                var ret = Math.Pow(1 - inner, NHashFuncs);
                return ret;
            }
        }

        public void SetHashFunctions(ICollection<HashAlgorithm> hashFunctions)
        {
            _hFuncs = hashFunctions;
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
                SetBit(_byteArr, idx);
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
                if (!TestBit(_byteArr, idx))
                {
                    return false;
                }
            }
            return true;
        }

        private void SetBit(byte[] byteArr, int bitIdx)
        {
            var bytePos = bitIdx / 8;
            var bitInBytePos = bitIdx & 0x7; // Keep last 3 bits.
            var maskByte = (byte)(1 << bitInBytePos);
            byteArr[bytePos] |= maskByte;
        }

        private bool TestBit(byte[] byteArr, int bitIdx)
        {
            var bytePos = bitIdx / 8;
            var bitInBytePos = bitIdx & 0x7; // Keep last 3 bits.
            var maskByte = (byte)(1 << bitInBytePos);
            if ((byteArr[bytePos] & maskByte) == 0)
            {
                return false;
            }
            return true;
        }

        public static ICollection<HashAlgorithm> DefaultHashFuncs()
        {
            //var h1 = new SameHash();
            var hList = new List<HashAlgorithm>();
            //hList.Add(h1);
            for (var i = 0; i < 5; ++i)
            {
                var mmh = new MurmurHash3_x86_32();
                mmh.Seed = (uint)i;
                hList.Add(mmh);
            }

            return hList;
        }
    }

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
}
