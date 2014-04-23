using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASyncLib
{
    public static class Helper
    {
        public static int EstimateD0(int sizeA, int sizeB, int quasiIntersectionN0, BloomFilter bf)
        {
            var div = 1 - bf.FalsePositive;

            var d = sizeA - sizeB + 2 * (sizeB - quasiIntersectionN0) / div;
            return (int)Math.Ceiling(d);
        }

        public static int SizeOfBF(BloomFilter bf)
        {
            return 4 + (int)Math.Ceiling(bf.BitLength / 8.0);
        }

        public static int SizeCPB(List<int> cpb)
        {
            return cpb.Count * 4;
        }

        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        public static IEnumerable<string> ReadLinesFromTextStream(StreamReader s)
        {
            string line;
            while ((line = s.ReadLine()) != null)
            {
                yield return line;
            }
        }

        public static string BFFileName
        {
            get
            {
                return "bffile.dat";
            }
        }

        public static string P1FileName
        {
            get
            {
                return "patch1.dat";
            }
        }

        public static string SEFileName
        {
            get
            {
                return "clientstrata.dat";
            }
        }

        public static string IBFFileName
        {
            get
            {
                return "ibffile.dat";
            }
        }

        public static string P2FileName
        {
            get
            {
                return "patch2.dat";
            }
        }
    }
}
