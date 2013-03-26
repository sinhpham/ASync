using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    public static class Helper
    {
        public static int EstimateD0(int sizeA, int sizeB, int quasiIntersectionN0, BloomFilter bf)
        {

            var div = 1 - bf.FalsePositive;

            var d = sizeA - sizeB + 2 * (sizeB - quasiIntersectionN0) / div;
            return (int)Math.Ceiling(d);
        }

        public static List<T> ToList<T>(this BlockingCollectionDataChunk<T> collection)
        {
            var ret = new List<T>();
            foreach (var chunk in collection.BlockingCollection)
            {
                for (var i = 0; i < chunk.DataSize; ++i)
                {
                    ret.Add(chunk.Data[i]);
                }
            }
            return ret;
        }

        public static int SizeOfBF(BloomFilter bf)
        {
            return 4 + (int)Math.Ceiling(bf.BitLength / 8.0);
        }

        public static int SizeCPB(List<int> cpb)
        {
            return cpb.Count * 4;
        }

        public static int SizeOfPatchFile(List<PatchData> pf)
        {
            var ret = 0;
            foreach (var pd in pf)
            {
                ret += SizeOfPatchData(pd);
            }
            return ret;
        }

        public static int SizeOfPatchData(PatchData pd)
        {
            if (pd.Data == null)
            {
                return 4; // Only hash value
            }
            return pd.Data.Length + 4 + 4; // Data length + data size + hash value;
        }
    }
}
