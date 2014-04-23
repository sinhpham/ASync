using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASyncLib;
using System.IO;
using ProtoBuf;
using System.Diagnostics;

namespace ASyncWindows
{
    class Program
    {
        static void Main(string[] args)
        {
            _dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "aaa"); ;

            //RunFunctionTimed(() => GenServerData(2000000, 50));
            //RunFunctionTimed(() => GenPatch1File());

            //RunFunctionTimed(() => GenPatch2File());
            var clientDic = DataGen.Gen(1000, 0).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);

            var clientEst = new StrataEstimator(true);
            clientEst.Encode(clientDic);
            using (var f = File.Create(Path.Combine(_dataDir, "clientstrata.dat")))
            {
                Serializer.Serialize(f, clientEst);
            }

            var serverDic = DataGen.Gen(1000, 4).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);

            var serverEst = new StrataEstimator(true);
            serverEst.Encode(serverDic);

            var n1 = 0;

            using (var f = File.OpenRead(Path.Combine(_dataDir, "clientstrata.dat")))
            {
                var readClientEst = Serializer.Deserialize<StrataEstimator>(f);
                var diffEst = serverEst - readClientEst;
                n1 = diffEst.Estimate();
            }

            var dEst = serverEst - clientEst;
            var n2 = dEst.Estimate();

            var a =  0;

        }

        static string _dataDir;
        static string DataDir
        {
            get
            {
                return _dataDir;
            }
        }

        private static void GenServerData(int size, int changedPercent)
        {
            // Gen data for server
            using (var trans = DbManager.Engine.GetTransaction())
            {
                trans.Technical_SetTable_OverwriteIsNotAllowed(DbManager.DefaultTableName);
                foreach (var item in DataGen.Gen(size, changedPercent))
                {
                    trans.Insert(DbManager.DefaultTableName, item.Key, item.Value);
                }
                trans.Commit();
            }
            DbManager.Dispose();
        }

        private static void GenPatch1File()
        {
            using (var trans = DbManager.Engine.GetTransaction())
            {
                trans.Technical_SetTable_OverwriteIsNotAllowed(DbManager.DefaultTableName);
                var serverDic = trans.SelectForward<string, string>(DbManager.DefaultTableName).Select(t => new KeyValuePair<string, string>(t.Key, t.Value));
                using (var bffile = File.OpenRead(Path.Combine(DataDir, "bffile.dat")))
                {
                    using (var pfile = File.OpenWrite(Path.Combine(DataDir, "patch1file.dat")))
                    {
                        KeyValSync.ServerGenPatch1File(serverDic, (int)trans.Count(DbManager.DefaultTableName), bffile, pfile);
                    }
                }
            }
        }

        private static void GenPatch2File()
        {
            using (var trans = DbManager.Engine.GetTransaction())
            {
                trans.Technical_SetTable_OverwriteIsNotAllowed(DbManager.DefaultTableName);
                var serverDic = trans.SelectForward<string, string>(DbManager.DefaultTableName).Select(t => new KeyValuePair<string, string>(t.Key, t.Value));

                using (var ibfFile = File.OpenRead(Path.Combine(DataDir, "ibffile.dat")))
                {
                    using (var p2File = File.OpenWrite(Path.Combine(DataDir, "patch2file.dat")))
                    {
                        KeyValSync.ServerGenPatch2FromIBF(serverDic, key =>
                        {
                            var v = trans.Select<string, string>(DbManager.DefaultTableName, key);
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

        static void RunFunctionTimed(Action act)
        {
            var sw = new Stopwatch();
            sw.Start();
            act();
            sw.Stop();
            Console.WriteLine("Done in {0}", sw.Elapsed);
        }
    }
}
