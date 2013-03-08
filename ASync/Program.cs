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
            var sa = new List<int> { 1, 2, 9, 12, 33 };
            var sb = new List<int> { 1, 2, 9, 10, 12, 28 };
            var xVal = new List<int> { -1, -2, -3, -4, -5 };
            var f = 97;

            var cpa = cp.Calc(sa, xVal, f);
            var cpb = cp.Calc(sb, xVal, f);
            var cpaocpb = cp.Div(cpa, cpb, f);


        }



        static void WriteFileHashValues(Stream fileStream, Stream outStream)
        {
            var blockSize = 3;
            var windowsSize = 2;
        }
    }
}