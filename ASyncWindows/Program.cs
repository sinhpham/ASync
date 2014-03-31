using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASyncLib;
using System.IO;
using ProtoBuf;

namespace ASyncWindows
{
    class Program
    {
        static void Main(string[] args)
        {
            var dic = new Dictionary<string, string>();
            foreach (var item in DataGen.Gen(5000000, 0))
            {
                dic.Add(item.Key, item.Value);
            }

            var dic2 = new Dictionary<string, string>();
            foreach (var item in DataGen.Gen(5000000, 10))
            {
                dic2.Add(item.Key, item.Value);
            }

            //using (var trans = DbManager.Engine.GetTransaction())
            //{
            //    foreach (var item in DataGen.Gen(5000000, 0))
            //    {
            //        trans.Insert("t1", item.Key, item.Value);
            //    }
            //}
            var d = NumeOfDiff(dic, dic2);

            var a = 0;
            //using (var tran = DbManager.Engine.GetTransaction())
            //{
            //    var serverDic = tran.SelectForward<string, string>("t1").Select(t => new KeyValuePair<string, string>(t.Key, t.Value));
            //    using (var bffile = File.OpenRead("bffile.dat"))
            //    {
            //        using (var pfile = File.OpenWrite("patch1file.dat"))
            //        {
            //            KeyValSync.ServerGenPatch1File(serverDic, (int)tran.Count("t1"), bffile, pfile);
            //        }
            //    }
            //}
        }

        static int NumeOfDiff(dynamic dic1, dynamic dic2)
        {
            var count = 0;
            foreach (var item in dic1)
            {
                if (!dic2.ContainsKey(item.Key))
                {
                    // Not found
                    count++;
                }
                else if (dic2[item.Key] != dic1[item.Key])
                {
                    // Diff
                    count++;
                }
            }
            count += Math.Abs(dic2.Count - dic1.Count);
            return count;
        }
    }
}
