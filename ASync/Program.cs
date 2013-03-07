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
            var cp = new CharacteristicPolynomial();
            var sa = new List<int> { 1, 2, 5, 11, 19 };
            var sb = new List<int> { 1, 5, 7, 11 };
            var xVal = new List<int> { -1, -2, -3, -4 };

            var cpa = cp.Calc(sa, xVal, 67);
            var cpb = cp.Calc(sb, xVal, 67);
            var cpaocpb = cp.Div(cpa, cpb, 67);
        }

        

        static void WriteFileHashValues(Stream fileStream, Stream outStream)
        {
            var blockSize = 3;
            var windowsSize = 2;
        }
    }
}