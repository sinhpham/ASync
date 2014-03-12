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

            using (var f = File.Create("clientDic.dat"))
            {
                Serializer.Serialize(f, clientDic);
            }

            using (var f = File.Create("serverDic.dat"))
            {
                Serializer.Serialize(f, serverDic);
            }
        }
    }
}
