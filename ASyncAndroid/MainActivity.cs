using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using ASyncLib;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ASyncAndroid
{
    [Activity(Label = "ASyncAndroid", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.myButton);
            
            button.Click += async delegate
            {
                //GenClientData(5000000);
                //GenBfFileFromDb();
                await DownloadPatch1File();


                var a = 0;
            };
        }

        private static void GenClientData(int size)
        {
            using (var trans = DbManager.Engine.GetTransaction())
            {
                foreach (var item in DataGen.Gen(size, 0))
                {
                    trans.Insert("t1", item.Key, item.Value);
                }
                trans.Commit();
            }
            DbManager.Dispose();
        }

        private static void GenBfFileFromDb()
        {
            using (var trans = DbManager.Engine.GetTransaction())
            {
                var clientDic = trans.SelectForward<string, string>("t1").Select(t => new KeyValuePair<string, string>(t.Key, t.Value));
                var docFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                using (var bffile = File.OpenWrite(Path.Combine(docFolder, "bffile.dat")))
                {
                    KeyValSync.ClientGenBfFile(clientDic, (int)trans.Count("t1"), bffile);
                }
            }
            DbManager.Dispose();
        }

        private static async Task UploadBfFileToServer()
        {
            var docFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            using (var bffile = File.OpenRead(Path.Combine(docFolder, "bffile.dat")))
            {
                await NetworkManager.FtpUpload("ftp://10.81.4.120", bffile, "bffile.dat");
            }
        }

        private static async Task DownloadPatch1File()
        {
            var docFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
           
            await NetworkManager.FtpDownload("ftp://10.81.4.120/patch1file.dat", "patch1file.dat");
        }
    }
}


