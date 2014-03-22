using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DatasetGen
{
    class Program
    {
        static void Main(string[] args)
        {
            // Data: 5M, 0.5M, 0.05M items
            // Change: 50%, 20%, 4%
            GenDataSet(550000, 4);
        }

        static void GenDataSet(int baseSize, int changedPercent)
        {
            var clientFn = string.Format("{0}-clientDic.dat", baseSize);
            var serverFn = string.Format("{0}-{1}changed-serverDic.dat", baseSize, changedPercent);

            // Gen client file.
            CreateDicFile(baseSize, clientFn, 0, 0);
            // Gen server file.
            //CreateDicFile(baseSize, serverFn, changedPercent / 2, changedPercent / 2);
        }

        static void CreateDicFile(int size, string name, int addedPercent, int modifiedPercent)
        {
            var hFunc = SHA1.Create();
            var dic = new Dictionary<string, string>();
            // Original
            for (var i = 0; i < size; ++i)
            {
                var str = BitConverter.ToString(hFunc.ComputeHash(BitConverter.GetBytes(i)));
                var valStr = str + str;
                dic.Add(i.ToString(), valStr);
            }
            // Add
            var addedCount = size / 100 * addedPercent;
            for (var i = 0; i < addedCount; ++i)
            {
                var str = BitConverter.ToString(hFunc.ComputeHash(BitConverter.GetBytes(i)));
                var valStr = str + str;
                dic.Add((size + i).ToString(), valStr);
            }
            // Modified
            var modifiedCount = size / 100 * modifiedPercent;
            for (var i = 0; i < modifiedCount; ++i)
            {
                var str = BitConverter.ToString(hFunc.ComputeHash(BitConverter.GetBytes(-i - 1)));
                var valStr = str + str;
                dic[i.ToString()] = valStr;
            }
            using (var f = File.Create(name))
            {
                Serializer.Serialize(f, dic);
            }
        }
    }
}
