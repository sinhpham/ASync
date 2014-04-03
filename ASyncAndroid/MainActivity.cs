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
using Java.Interop;

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
        }

        [Export]
        public void GenDBClicked(View v)
        {
            var tf1 = FindViewById<EditText>(Resource.Id.editText1);
            var size = int.Parse(tf1.Text);

            Console.WriteLine("Gen db");
            GenClientData(size);
            Console.WriteLine("Done gen db");
        }

        [Export]
        public void CheckDbClicked(View v)
        {
            Console.WriteLine("Check db");
            var tf1 = FindViewById<EditText>(Resource.Id.editText1);
            var tf2 = FindViewById<EditText>(Resource.Id.editText2);

            var size = int.Parse(tf1.Text);
            var changedPer = int.Parse(tf2.Text);
            CheckDbData(size, changedPer);
            Console.WriteLine("Done checking db");
        }

        [Export]
        public void GenBFClicked(View v)
        {
            Console.WriteLine("Gen BF");
            GenBfFileFromDb();
            Console.WriteLine("Done gen bf");
        }

        [Export]
        public async void DownloadClicked(View v)
        {
            Console.WriteLine("Download clicked", ((Button)v).Text);
            var tf1 = FindViewById<EditText>(Resource.Id.editText1);
            var tf2 = FindViewById<EditText>(Resource.Id.editText2);

            var serverFile = tf1.Text;
            var localFile = tf2.Text;

            await NetworkManager.FtpDownload("ftp://10.81.4.120/" + serverFile, localFile);

            Console.WriteLine("Done downloading");
        }

        [Export]
        public void Patch1Clicked(View v)
        {
            Console.WriteLine("Patch 1 and gen ibf");
            PatchAndGenIBFFile();
            Console.WriteLine("Done generating ibf file");
        }

        [Export]
        public async void UploadClicked(View v)
        {
            Console.WriteLine("Upload clicked");

            var tf1 = FindViewById<EditText>(Resource.Id.editText1);
            var fileName = tf1.Text;
            var docFolder = DbManager.AppDir;
            using (var file = File.OpenRead(Path.Combine(docFolder, fileName)))
            {
                await NetworkManager.FtpUpload("ftp://10.81.4.120", file, fileName);
            }

            Console.WriteLine("Done uploading");

        }

        [Export]
        public void Patch2Clicked(View v)
        {
            Console.WriteLine("Patch2 clicked");
            ClientApplyPatch2();
            Console.WriteLine("Done patch 2");
        }

        [Export]
        public void DisposeDbClicked(View v)
        {
            Console.WriteLine("Dispose db clicked");
            DbManager.Dispose();
            Console.WriteLine("Done disposing db");
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
        }

        private static bool CheckDbData(int size, int changedPercent)
        {
            using (var trans = DbManager.Engine.GetTransaction())
            {
                foreach (var item in DataGen.Gen(size, changedPercent))
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
            return true;
        }

        private static void GenBfFileFromDb()
        {
            using (var trans = DbManager.Engine.GetTransaction())
            {
                var clientDic = trans.SelectForward<string, string>("t1").Select(t => new KeyValuePair<string, string>(t.Key, t.Value));
                var docFolder = DbManager.AppDir;
                using (var bffile = File.OpenWrite(Path.Combine(docFolder, "bffile.dat")))
                {
                    KeyValSync.ClientGenBfFile(clientDic, (int)trans.Count("t1"), bffile);
                }
            }
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
        }

        static void ClientApplyPatch2()
        {
            using (var trans = DbManager.Engine.GetTransaction())
            {
                var docFolder = DbManager.AppDir;
                using (var patch2File = File.OpenRead(Path.Combine(docFolder, "patch2file.dat")))
                {
                    KeyValSync.ClientPatch<string, string>(currItem => trans.Insert("t1", currItem.Key, currItem.Value), patch2File);
                }
                trans.Commit();
            }
        }
    }
}


