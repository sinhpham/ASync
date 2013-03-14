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
            var _cp = new CharacteristicPolynomial();

            var sa = new List<int> { 1, 2, 9, 12, 33 };
            var sb = new List<int> { 1, 2, 9, 10, 12, 28 };
            var xVal = new List<int> { -1, -2, -3, -4, -5 };
            var f = 97;

            var cpa = _cp.Calc(sa, xVal, f);
            var cpb = _cp.Calc(sb, xVal, f);
            var cpaocpb = _cp.Div(cpa, cpb, f);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                f, sa.Count - sb.Count,
                out p, out q);

            var pFactors = _cp.Factoring(p, f);
            var qFactors = _cp.Factoring(q, f);
        }

        [DllImport("libs/NTLLib.dll")]
        public static extern int testpi(int a);
    }
}