using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    class StrataEstimator
    {
        public StrataEstimator()
        {
            _ibfList = new List<IBF>(32);
            for (var i = 0; i < 32; ++i)
            {
                _ibfList.Add(new IBF());
            }
        }
        List<IBF> _ibfList;
        HashAlgorithm _hzFunc;

        public void Encode<TKey, TValue>(Dictionary<TKey, TValue> dic)
        {
            foreach (var item in dic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var val = BitConverter.ToInt32(_hzFunc.ComputeHash(bBlock), 0);

                var i = NumTrailingBinaryZeros(val);

                _ibfList[i].Add(val);
            }
        }

        public static int NumTrailingBinaryZeros(int n)
        {
            var mask = 1;
            for (var i = 0; i < 32; i++, mask <<= 1)
            {
                if ((n & mask) != 0)
                {
                    return i;
                }
            }

            return 32;
        }
    }
}
