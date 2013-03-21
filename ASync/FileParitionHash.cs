using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    public class FileParitionHash
    {
        public FileParitionHash(HashAlgorithm ha)
        {
            _ha = ha;
        }

        HashAlgorithm _ha;
        byte[] buffer = new byte[1024];

        public void ProcessStream(Stream stream, BlockingCollectionDataChunk<int> positions,
            BlockingCollectionDataChunk<uint> partitionHashValues, BlockingCollectionDataChunk<FileChunkInfo> fileChunkInfo)
        {
            var prevPos = 0;
            var neededBytes = 0;

            foreach (var posChunk in positions.BlockingCollection.GetConsumingEnumerable())
            {
                for (var i = 0; i < posChunk.DataSize; ++i )
                {
                    var pos = posChunk.Data[i];
                    neededBytes = pos - prevPos + 1;
                    var hv = CalcStreamPortion(stream, neededBytes);
                    partitionHashValues.Add(hv);
                    fileChunkInfo.Add(new FileChunkInfo() { Pos = prevPos, Length = neededBytes });
                    // Prepare for next block.
                    prevPos = pos + 1;
                }
            }
            // Handle the last partition
            neededBytes = (int)stream.Length - prevPos;
            if (neededBytes != 0)
            {
                var lastHv = CalcStreamPortion(stream, neededBytes);
                partitionHashValues.Add(lastHv);
                fileChunkInfo.Add(new FileChunkInfo() { Pos = prevPos, Length = neededBytes });
            }
            partitionHashValues.CompleteAdding();
            fileChunkInfo.CompleteAdding();
        }

        private uint CalcStreamPortion(Stream stream, int neededBytes)
        {
            if (neededBytes > buffer.Length)
            {
                buffer = new byte[neededBytes];
            }
            // Read nBytesNeeded bytes.
            var bytesReadSoFar = 0;
            var r = 0;

            while ((r = stream.Read(buffer, bytesReadSoFar, neededBytes - bytesReadSoFar)) > 0 && bytesReadSoFar + r != neededBytes)
            {
                bytesReadSoFar += r;
            }
            if (bytesReadSoFar + r != neededBytes)
            {
                throw new InvalidDataException();
            }
            // Data ready for computing hash.
            var hvArr = _ha.ComputeHash(buffer, 0, neededBytes);
            var hv = BitConverter.ToUInt32(hvArr, 0);
            return hv;
        }
    }
}
