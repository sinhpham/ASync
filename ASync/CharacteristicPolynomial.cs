using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    class CharacteristicPolynomial
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

        private int DivGF(int a, int b, int p)
        {
            var ib = InversionGF(b, p);
            return (a * ib) % p;
        }

        private int InversionGF(int a, int p)
        {
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
    }
}
