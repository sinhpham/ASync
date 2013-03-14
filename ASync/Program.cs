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

namespace ASync
{
    class Program
    {
        static void Main(string[] args)
        {
            var cp = new CharacteristicPolynomial();
            var x = cp.Factoring(new List<int> { 86,59,1 }, 97);

            List<int> p;
            List<int> q;
            cp.Interpolate(new List<int> { 26,13,56,24 }, new List<int> { -1, -2, -3, -4 }, 67, out p, out q);
        }

        static void WriteFileHashValues(Stream fileStream, Stream outStream)
        {
            var blockSize = 3;
            var windowsSize = 2;
        }

        [DllImport("NTLLib.dll")]
        public static extern int testpi(int a);
    }
}