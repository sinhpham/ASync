using System;
using DBreeze;
using System.Security.Cryptography;
using System.IO;

namespace ASyncAndroid
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
                    var docFolder = Path.Combine(AppDir, "db");
                    engine = new DBreezeEngine(docFolder);
                }
                return engine;
            }
        }

        public static string AppDir
        {
            get
            {
                return Android.OS.Environment.ExternalStorageDirectory + "/async";
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

