using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASyncLib;
using System.IO;

namespace ASyncWindows
{
    class Program
    {
        static void Main(string[] args)
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
    }
}
