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
        const int BufferSize = 4;
        const int HashBlock = 3;
        const int LocalMaximaH = 512;

        public static void StreamToHashValuesNaive(Stream inputStream, ConcurrentQueue<uint> hashValues)
        {
            // For testing purpose.
            var buffer = ReadFully(inputStream);
            var offset = 0;
            while (offset + HashBlock <= buffer.Length)
            {
                var hv = Adler32Checksum.Calculate(buffer, offset, HashBlock);
                hashValues.Enqueue(hv);
                ++offset;
            }
        }

        public static void StreamToHashValues(Stream inputStream, ConcurrentQueue<uint> hashValues)
        {
            // Read the source file into a byte array. 

            var prevBuffer = new byte[BufferSize];
            var buffer = new byte[BufferSize];
            var starting = true;
            var prevHashValue = 0U;

            var byteRead = 0;

            while ((byteRead = inputStream.Read(buffer, 0, BufferSize)) != 0)
            {
                //if (byteRead < BufferSize)
                //{
                //    // Zero the remaining space.
                //    Array.Clear(buffer, byteRead, BufferSize - byteRead);
                //}
                var hashEndIdx = -1;
                if (starting)
                {
                    var hv = Adler32Checksum.Calculate(buffer, 0, HashBlock);
                    hashValues.Enqueue(hv);
                    prevHashValue = hv;
                    starting = false;
                    hashEndIdx = HashBlock - 1;
                }
                while (hashEndIdx != byteRead - 1)
                {
                    hashEndIdx += 1;
                    var hashStartIdx = hashEndIdx - HashBlock;

                    var outByte = hashStartIdx < 0 ? prevBuffer[BufferSize + hashStartIdx] : buffer[hashStartIdx];
                    var hv = Adler32Checksum.Roll(outByte, buffer[hashEndIdx], prevHashValue, HashBlock);

                    hashValues.Enqueue(hv);
                    prevHashValue = hv;
                }

                // Swap 2 buffers
                var temp = buffer;
                buffer = prevBuffer;
                prevBuffer = temp;
            }
        }

        public static byte[] ReadFully(Stream input)
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
