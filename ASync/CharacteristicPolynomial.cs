using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    public class CharacteristicPolynomial
    {
        public List<int> Calc(List<int> set, List<int> xValues, int maxFieldValue)
        {
            var ret = new List<int>();
            foreach (var x in xValues)
            {
                var currValue = 1;
                for (var i = 0; i < set.Count; ++i)
                {
                    currValue = (currValue * (x - set[i])) % maxFieldValue;
                }
                currValue += currValue < 0 ? maxFieldValue : 0;
                ret.Add(currValue);
            }
            return ret;
        }

        public List<int> Div(List<int> cpa, List<int> cpb, int maxFiledValue)
        {
            // Calculate CPa/CPb.
            var ret = new List<int>();
            for (var i = 0; i < cpa.Count; ++i)
            {
                var currRet = DivGF(cpa[i], cpb[i], maxFiledValue);
                currRet += currRet < 0 ? maxFiledValue : 0;
                ret.Add(currRet);
            }
            return ret;
        }

        public List<int> Factoring(List<int> coeff, int maxFiledValue)
        {
            

            var arr = coeff.ToArray();

            var rSize = 0;
            var rPointer = Factoring(arr, coeff.Count, maxFiledValue, out rSize);

            var retArray = new int[rSize];
            Marshal.Copy(rPointer, retArray, 0, rSize);
            DeleteArrPtr(rPointer);

            var ret = new List<int>(retArray);
            return ret;
        }

        public void Interpolate(List<int> rf, List<int> xValues,
            int maxFieldValue, int d,
            out List<int> P, out List<int> Q)
        {
            var pSize = 0;
            var qSize = 0;

            var retPtr = Interpolate(rf.ToArray(), rf.Count, xValues.ToArray(), xValues.Count, maxFieldValue, d, out pSize, out qSize);

            var pArr = new int[pSize];
            var qArr = new int[qSize];
            Marshal.Copy(retPtr, pArr, 0, pSize);
            Marshal.Copy(retPtr + pSize * 4, qArr, 0, qSize);

            P = new List<int>(pArr);
            Q = new List<int>(qArr);

            DeleteArrPtr(retPtr);
        }

        private int DivGF(int a, int b, int p)
        {
            // Division over finite field 
            // p should be a prime number
            if (b == 0)
            {
                // TODO: handle div 0
                return 0;
            }

            var ib = InversionGF(b, p);
            return (a * ib) % p;
        }

        private int InversionGF(int a, int p)
        {
            // Inversion over finite field 
            // p should be a prime number
            var y1 = 1;
            var y2 = 0;
            var oriP = p;

            while (a != 1)
            {
                var q = p / a;

                var nextA = p - q * a;
                var nextP = a;
                var nextY2 = y1;
                var nextY1 = y2 - ((q * y1) % oriP);
                nextY1 += nextY1 < 0 ? oriP : 0;

                a = nextA;
                p = nextP;
                y1 = nextY1;
                y2 = nextY2;
            }
            return y1;
        }

        [DllImport("NTLLib.dll")]
        private static extern IntPtr Factoring(int[] array, int size, int maxFieldValue, out int returnArrSize);

        [DllImport("NTLLib.dll")]
        private static extern IntPtr Interpolate(int[] rfArr, int rfArrSize, int[] sampleArr, int sampleArrSize,
            int maxFieldValue, int d,
            out int retPSize, out int retQSize);

        [DllImport("NTLLib.dll")]
        private static extern void DeleteArrPtr(IntPtr ptr);
    }
}
