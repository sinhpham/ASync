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
            GenDataSet(10000000, 50);
        }

        static void GenDataSet(int baseSize, int changedPercent)
        {
            CreateDicFile(baseSize, string.Format("{0}-clientDic.dat", baseSize));

            var sSize = baseSize / 100 * changedPercent + baseSize;
            CreateDicFile(sSize, string.Format("{0}-{1}changed-serverDic.dat", sSize, changedPercent));
        }

        static void CreateDicFile(int size, string name)
        {
            var dic = new Dictionary<string, string>();
            for (var i = 0; i < size; ++i)
            {
                dic.Add(i.ToString(), i.ToString());
            }
            using (var f = File.Create(name))
            {
                Serializer.Serialize(f, dic);
            }
        }
    }
}
