using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace ASync
{
    struct FileChunk
    {
        public int Offset { get; set; }
        public int Length { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var str = "1234567890";

            var hValues = new ConcurrentQueue<uint>();
            var hValues2 = new ConcurrentQueue<uint>();
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                FileHash.StreamToHashValues(ms, hValues);
            }
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                FileHash.StreamToHashValuesNaive(ms, hValues2);
            }
        }
    }
}