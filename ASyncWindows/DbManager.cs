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
                    var docFolder = "./db/";
                    engine = new DBreezeEngine(docFolder);
                }
                return engine;
            }
        }

        public static string DefaultTableName
        {
            get
            {
                return "t1";
            }
        }

        public static void Dispose()
        {
            if (engine != null)
            {
                engine.Dispose();
            }
        }
    }
}
