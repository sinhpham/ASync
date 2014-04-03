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
            //GenPatch1File();

            GenPatch2File();


            var a = 0;

        }

        

        private static void GenServerData(int size, int changedPercent)
        {
            // Gen data for server
            using (var trans = DbManager.Engine.GetTransaction())
            {
                foreach (var item in DataGen.Gen(size, changedPercent))
                {
                    trans.Insert("t1", item.Key, item.Value);
                }
                trans.Commit();
            }
            DbManager.Dispose();
        }

        private static void GenPatch1File()
        {
            using (var tran = DbManager.Engine.GetTransaction())
            {
                var serverDic = tran.SelectForward<string, string>("t1").Select(t => new KeyValuePair<string, string>(t.Key, t.Value));
                using (var bffile = File.OpenRead("bffile.dat"))
                {
                    using (var pfile = File.OpenWrite("patch1file.dat"))
                    {
                        KeyValSync.ServerGenPatch1File(serverDic, (int)tran.Count("t1"), bffile, pfile);
                    }
                }
            }
        }

        private static void GenPatch2File()
        {
            using (var tran = DbManager.Engine.GetTransaction())
            {
                var serverDic = tran.SelectForward<string, string>("t1").Select(t => new KeyValuePair<string, string>(t.Key, t.Value));

                using (var ibfFile = File.OpenRead("ibffile.dat"))
                {
                    using (var p2File = File.OpenWrite("patch2file.dat"))
                    {
                        KeyValSync.ServerGenPatch2FromIBF(serverDic, key =>
                        {
                            var v = tran.Select<string, string>("t1", key);
                            if (!v.Exists)
                            {
                                throw new InvalidDataException();
                            }
                            return v.Value;
                        }, ibfFile, p2File);
                    }
                }
            }
            DbManager.Dispose();
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
