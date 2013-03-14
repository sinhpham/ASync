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
            var _cp = new CharacteristicPolynomial(97);

            var sa = new List<int> { 1, 2, 9, 12, 33 };
            var sb = new List<int> { 1, 2, 9, 10, 12, 28 };
            var xVal = new List<int> { -1, -2, -3, -4, -5 };

            var cpa = _cp.Calc(sa, xVal);
            var cpb = _cp.Calc(sb, xVal);
            var cpaocpb = _cp.Div(cpa, cpb);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                sa.Count - sb.Count,
                out p, out q);

            var pFactors = _cp.Factoring(p);
            var qFactors = _cp.Factoring(q);
        }

        [DllImport("libs/NTLLib.dll")]
        public static extern int testpi(int a);
    }
}