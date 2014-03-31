using DBreeze;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ASyncWindows
{
    public static class DbManager
    {
        static DBreezeEngine engine = null;

        public static DBreezeEngine Engine
        {
            get
            {
                if (engine == null)
                {
                    var docFolder = "./";
                    engine = new DBreezeEngine(docFolder);
                }
                return engine;
            }
        }

        public static void Dispose()
        {
            if (engine != null)
            {
                engine.Dispose();
            }
        }

        public static void GenDataSet(int baseSize, int changedPercent)
        {
            var hFunc = SHA1.Create();

            using (var tran = DbManager.Engine.GetTransaction())
            {
                for (var i = 0; i < baseSize; ++i)
                {
                    var str = BitConverter.ToString(hFunc.ComputeHash(BitConverter.GetBytes(i)));
                    var valStr = str + str;
                    tran.Insert("t1", i.ToString(), valStr);
                }
                tran.Commit();
            }
        }

        public static void CheckDataSet(int baseSize, int changedPercent)
        {
            var hFunc = SHA1.Create();

            using (var tran = DbManager.Engine.GetTransaction())
            {
                for (var i = 0; i < baseSize; ++i)
                {
                    var str = BitConverter.ToString(hFunc.ComputeHash(BitConverter.GetBytes(i)));
                    var valStr = str + str;

                    var storedVal = tran.Select<string, string>("t1", i.ToString()).Value;
                    if (storedVal != valStr)
                    {
                        throw new InvalidDataException();
                    }
                }
            }
        }
    }
}
