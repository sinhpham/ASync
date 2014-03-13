using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatasetGen
{
    class Program
    {
        static void Main(string[] args)
        {
            GenDataSet(1000000, 50);
        }

        static void GenDataSet(int baseSize, int changedPercent)
        {
            var clientDic = new Dictionary<string, string>();
            for (var i = 0; i < baseSize; ++i)
            {
                clientDic.Add(i.ToString(), i.ToString());
            }

            var sSize = baseSize / 100 * changedPercent + baseSize;
            var serverDic = new Dictionary<string, string>();
            for (var i = 0; i < sSize; ++i)
            {
                serverDic.Add(i.ToString(), (i).ToString());
            }

            using (var f = File.Create(string.Format("{0}-clientDic.dat", baseSize)))
            {
                Serializer.Serialize(f, clientDic);
            }

            using (var f = File.Create(string.Format("{0}-{1}changed-serverDic.dat", sSize, changedPercent)))
            {
                Serializer.Serialize(f, serverDic);
            }
        }
    }
}
