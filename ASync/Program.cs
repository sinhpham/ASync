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
            var list = new List<int> { 9, 21, 19, 13, 26, 18, 18, 25, 24, 16, 17, 35, 37, 42 };
            var lm = new LocalMaxima(2);
            var t = lm.LocalMaxima2(list);
        }
    }
}