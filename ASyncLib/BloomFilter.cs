﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASyncLib
{
    [ProtoContract]
    public class BloomFilter
    {
        public BloomFilter()
        {

        }

        public BloomFilter(int bitLength, ICollection<IHashFunc> hashFunctions)
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
        ICollection<IHashFunc> _hFuncs;
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

        public void SetHashFunctions(ICollection<IHashFunc> hashFunctions)
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
            if (BitLength == 0)
            {
                return false;
            }
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

        public static ICollection<IHashFunc> DefaultHashFuncs(int num = 5)
        {
            var hList = new List<IHashFunc>();
            for (var i = 0; i < num; ++i)
            {
                var mmh = new MurmurHash3_x86_32();
                mmh.Seed = (uint)i;
                hList.Add(mmh);
            }

            return hList;
        }
    }
}
