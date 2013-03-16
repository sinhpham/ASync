using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    public class FileHash
    {
        public FileHash(int hashBlock)
        {
            _hashBlock = hashBlock;
            BufferSize = 2 * hashBlock;
        }

        readonly int BufferSize;

        readonly int _hashBlock;
        public int HashBlock { get { return _hashBlock; } }

        public void StreamToHashValuesNaive(Stream inputStream, BlockingCollection<uint> hashValues)
        {
            // For testing purpose.
            var tempBuff = ReadFully(inputStream);
            var buff = new byte[tempBuff.Length + HashBlock - 1];
            Array.Copy(tempBuff, buff, tempBuff.Length);
            var offset = 0;
            while (offset + HashBlock <= buff.Length)
            {
                var hv = Adler32Checksum.Calculate(buff, offset, HashBlock);
                hashValues.Add(hv);
                ++offset;
            }
        }

        public void StreamToHashValues(Stream inputStream, BlockingCollection<uint> hashValues)
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
                    CalcForHashEndIndex(ref currHashEndIdx, prevBuffer, buffer, ref prevHashValue, hashValues);
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
                    CalcForHashEndIndex(ref currHashEndIdx, prevBuffer, buffer, ref prevHashValue, hashValues);
                }
            }
            hashValues.CompleteAdding();
        }

        private void CalcForHashEndIndex(ref int currHashEndIdx, byte[] prevBuffer, byte[] buffer, ref uint prevHashValue, BlockingCollection<uint> hashValues)
        {
            currHashEndIdx += 1;
            var hashStartIdx = currHashEndIdx - HashBlock;

            var outByte = hashStartIdx < 0 ? prevBuffer[BufferSize + hashStartIdx] : buffer[hashStartIdx];
            var hv = Adler32Checksum.Roll(outByte, buffer[currHashEndIdx], prevHashValue, HashBlock);

            hashValues.Add(hv);
            prevHashValue = hv;
        }

        private static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                var read = 0;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
