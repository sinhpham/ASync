using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    class Helper
    {
        public static int EstimateD0(int sizeA, int sizeB, int quasiIntersectionN0, BloomFilter bf)
        {
            var k = bf.NHashFuncs;
            var m = bf.BitLength;

            var epow = - ((double)k * (double)sizeA / (double)m);
            var epowVal = Math.Pow(Math.E, epow);
            var div = 1 - Math.Pow(1 - epowVal, k);

            var d = sizeA - sizeB + 2 * (sizeB - quasiIntersectionN0) / div;
            return (int)Math.Ceiling(d);
        }
    }
}
