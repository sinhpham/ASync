using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASyncLib
{
    public class StrataEstimator
    {
        public StrataEstimator()
        {
            _ibfList = new List<IBF>(32);
            for (var i = 0; i < 32; ++i)
            {
                _ibfList.Add(new IBF(80, BloomFilter.DefaultHashFuncs(3)));
            }
        }
        List<IBF> _ibfList;

        public void Encode<TKey, TValue>(Dictionary<TKey, TValue> dic)
        {
            foreach (var item in dic)
            {
                var id = KeyValSync.KeyValToId(item);

                var i = NumTrailingBinaryZeros(id);

                _ibfList[i].Add(id);
            }
        }

        public int Estimate()
        {
            var count = 0;
            for (var i = _ibfList.Count - 1; i >= -1; --i)
            {
                var amb = new List<long>();
                var bma = new List<long>();
                if (i < 0 || !_ibfList[i].Decode(amb, bma))
                {
                    return (int)(Math.Pow(2.0, i + 1) * count);
                }
            }
            throw new InvalidOperationException();
        }

        public static StrataEstimator operator -(StrataEstimator curr, StrataEstimator x)
        {
            var ret = new StrataEstimator();

            for (var i = 0; i < curr._ibfList.Count; ++i)
            {
                ret._ibfList[i] = curr._ibfList[i] - x._ibfList[i];
            }
            return ret;
        }

        public static int NumTrailingBinaryZeros(long n)
        {
            var mask = 1;
            for (var i = 0; i < 64; i++, mask <<= 1)
            {
                if ((n & mask) != 0)
                {
                    return i;
                }
            }

            return 64;
        }
    }
}
