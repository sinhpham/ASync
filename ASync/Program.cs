using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Diagnostics;

namespace ASync
{
    class Program
    {
        static void Main(string[] args)
        {

        }

        static void WriteFileHashValues(Stream fileStream, Stream outStream)
        {
            var blockSize = 3;
            var windowsSize = 2;
        }
    }
}