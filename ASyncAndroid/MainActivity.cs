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
                Console.WriteLine("Starting...");
                //var d = Android.OS.Environment.ExternalStorageDirectory + "/async/db";
                //Console.WriteLine(d);

                //GenClientData(5000000);
                //CheckDbData(5000000);
                //Console.WriteLine("Done generating client db");
                //GenBfFileFromDb();
//                await DownloadPatch1File();
//                Console.WriteLine("Done downloading patch1 file");

//                PatchAndGenIBFFile();
//                Console.WriteLine("Donw generating ibf file");

                await UploadIBfFileToServer();
                Console.WriteLine("Done uploading ibf file");

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

        private static bool CheckDbData(int size)
        {
            using (var trans = DbManager.Engine.GetTransaction())
            {
                foreach (var item in DataGen.Gen(size, 0))
                {
                    var inDbVal = trans.Select<string, string>("t1", item.Key);
                    if (!inDbVal.Exists)
                    {
                        throw new InvalidDataException();
                    }
                    if (inDbVal.Value != item.Value)
                    {
                        throw new InvalidDataException();
                    }
                }
            }
            DbManager.Dispose();
            return true;
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
            var docFolder = DbManager.AppDir;
            using (var bffile = File.OpenRead(Path.Combine(docFolder, "bffile.dat")))
            {
                await NetworkManager.FtpUpload("ftp://10.81.4.120", bffile, "bffile.dat");
            }
        }

        private static async Task DownloadPatch1File()
        {
            await NetworkManager.FtpDownload("ftp://10.81.4.120/patch1filetext.dat", "patch1file.dat");
        }

        private static void PatchAndGenIBFFile()
        {
            Console.WriteLine("Starting patching and generating ibf file");
            using (var trans = DbManager.Engine.GetTransaction())
            {
                var clientDic = trans.SelectForward<string, string>("t1").Select(t => new KeyValuePair<string, string>(t.Key, t.Value));
                var docFolder = DbManager.AppDir;
                using (var patch1File = File.OpenText(Path.Combine(docFolder, "patch1file.dat")))
                {
                    using (var ibffile = File.OpenWrite(Path.Combine(docFolder, "ibffile.dat")))
                    {
                        var d0 = int.Parse(patch1File.ReadLine());
                        var patchItems = Helper.ReadLinesFromTextStream(patch1File).Select(str =>
                        {
                            var strArr = str.Split(' ');
                            return new KeyValuePair<string, string>(strArr[0], strArr[1]);
                        });

                        KeyValSync.ClientPatchAndGenIBFFile(clientDic, currItem => trans.Insert("t1", currItem.Key, currItem.Value), patchItems, d0, ibffile);
                    }
                }
                trans.Commit();
            }
            DbManager.Dispose();
        }

        private static async Task UploadIBfFileToServer()
        {
            var docFolder = DbManager.AppDir;
            using (var bffile = File.OpenRead(Path.Combine(docFolder, "ibffile.dat")))
            {
                await NetworkManager.FtpUpload("ftp://10.81.4.120", bffile, "ibffile.dat");
            }
        }
    }
}


