﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    public class FileHash
    {
        public FileHash(int hashBlock)
        {
            BufferSize = 1024 * 512;  // 512kb buffer size
            _hashBlock = hashBlock;

            if (HashBlock > BufferSize)
            {
                throw new ArgumentException("Hashblock should be smaller than buffer size");
            }
        }

        readonly int BufferSize;

        readonly int _hashBlock;
        public int HashBlock { get { return _hashBlock; } }

        public void StreamToHashValuesNaive(Stream inputStream, List<uint> hashValues)
        {
            // For testing purpose.
            var ms = inputStream as MemoryStream;
            if (ms == null)
            {
                throw new InvalidDataException("stream should be a memory stream");
            }

            var buff = ms.GetBuffer();
            var offset = 0;
            while (offset + HashBlock <= buff.Length)
            {
                var hv = Adler32Checksum.Calculate(buff, offset, HashBlock);
                hashValues.Add(hv);
                ++offset;
            }
            while (offset != buff.Length)
            {
                // Remaining data.
                var currBuff = new byte[HashBlock];
                Array.Copy(buff, offset, currBuff, 0, buff.Length - offset);
                var hv = Adler32Checksum.Calculate(currBuff, 0, HashBlock);
                hashValues.Add(hv);
                ++offset;
            }
        }

        public void StreamToUInt32Naive(Stream inputStream, List<uint> hashValues)
        {
            // For testing purpose.
            var ms = inputStream as MemoryStream;
            if (ms == null)
            {
                throw new InvalidDataException("stream should be a memory stream");
            }

            var buff = ms.GetBuffer();
            var offset = 0;
            while (offset + HashBlock <= buff.Length)
            {
                var hv = BitConverter.ToUInt32(buff, offset);
                hashValues.Add(hv);
                ++offset;
            }
            while (offset != buff.Length)
            {
                // Remaining data.
                var currBuff = new byte[4];
                Array.Copy(buff, offset, currBuff, 0, buff.Length - offset);
                var hv = BitConverter.ToUInt32(currBuff, 0);
                hashValues.Add(hv);
                ++offset;
            }
        }

        public void StreamToUInt32HashValues(Stream inputStream, BlockingCollectionDataChunk<uint> hashValues)
        {
            // Read the source file into a byte array. 
            var buffer = new byte[BufferSize];
            var starting = true;
            var prevHashValue = 0U;

            var byteRead = 0;
            var currHashEndIdx = -1;
            var hEndIdx = -1;

            while ((byteRead = inputStream.Read(buffer, 0, BufferSize)) != 0)
            {
                if (starting)
                {
                    var hv = BitConverter.ToUInt32(buffer, 0);
                    hashValues.Add(hv);
                    prevHashValue = hv;
                    starting = false;
                    currHashEndIdx = 4 - 1;
                }
                if (byteRead < BufferSize)
                {
                    Array.Clear(buffer, byteRead, BufferSize - byteRead);
                }
                hEndIdx = byteRead + 4 - 2;

                while (currHashEndIdx < BufferSize - 1 && currHashEndIdx != hEndIdx)
                {
                    CalcHashEndIdxUInt32(ref currHashEndIdx, buffer, ref prevHashValue, hashValues);
                }
                currHashEndIdx -= BufferSize;
                hEndIdx -= BufferSize;
            }
            if (currHashEndIdx != hEndIdx)
            {
                // Need an empty final block.
                Array.Clear(buffer, 0, BufferSize);

                while (currHashEndIdx != hEndIdx)
                {
                    CalcHashEndIdxUInt32(ref currHashEndIdx, buffer, ref prevHashValue, hashValues);
                }
            }
            hashValues.CompleteAdding();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalcHashEndIdxUInt32(ref int currHashEndIdx, byte[] buffer, ref uint prevHashValue, BlockingCollectionDataChunk<uint> hashValues)
        {
            currHashEndIdx += 1;
            var inByte = buffer[currHashEndIdx];
            var bitMask = (uint)(inByte << 24);

            var hv = (prevHashValue >> 8) | bitMask;

            hashValues.Add(hv);
            prevHashValue = hv;
        }

        public void StreamToHashValues(Stream inputStream, BlockingCollectionDataChunk<uint> hashValues)
        {
            // Read the source file into a byte array. 
            var prevBuffer = new byte[BufferSize];
            var buffer = new byte[BufferSize];
            var starting = true;
            var prevHashValue = 0U;

            var byteRead = 0;
            var currHashEndIdx = -1;
            var hEndIdx = -1;

            while ((byteRead = inputStream.Read(buffer, 0, BufferSize)) != 0)
            {
                if (starting)
                {
                    var hv = Adler32Checksum.Calculate(buffer, 0, HashBlock);
                    hashValues.Add(hv);
                    prevHashValue = hv;
                    starting = false;
                    currHashEndIdx = HashBlock - 1;
                }
                if (byteRead < BufferSize)
                {
                    Array.Clear(buffer, byteRead, BufferSize - byteRead);
                }
                hEndIdx = byteRead + HashBlock - 2;

                while (currHashEndIdx < BufferSize - 1 && currHashEndIdx != hEndIdx)
                {
                    CalcAdlerHashEndIdx(ref currHashEndIdx, prevBuffer, buffer, ref prevHashValue, hashValues);
                }
                currHashEndIdx -= BufferSize;
                hEndIdx -= BufferSize;
                // Swap 2 buffers
                var temp = buffer;
                buffer = prevBuffer;
                prevBuffer = temp;
            }
            if (currHashEndIdx != hEndIdx)
            {
                // Need an empty final block.
                Array.Clear(buffer, 0, BufferSize);

                while (currHashEndIdx != hEndIdx)
                {
                    CalcAdlerHashEndIdx(ref currHashEndIdx, prevBuffer, buffer, ref prevHashValue, hashValues);
                }
            }
            hashValues.CompleteAdding();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalcAdlerHashEndIdx(ref int currHashEndIdx, byte[] prevBuffer, byte[] buffer, ref uint prevHashValue, BlockingCollectionDataChunk<uint> hashValues)
        {
            currHashEndIdx += 1;
            var hashStartIdx = currHashEndIdx - HashBlock;

            var outByte = hashStartIdx < 0 ? prevBuffer[BufferSize + hashStartIdx] : buffer[hashStartIdx];
            var hv = Adler32Checksum.Roll(outByte, buffer[currHashEndIdx], prevHashValue, HashBlock);

            hashValues.Add(hv);
            prevHashValue = hv;
        }
    }
}
