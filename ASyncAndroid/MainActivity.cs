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
using System.Diagnostics;
using ProtoBuf;

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

        Dictionary<string, string> _clientDic = new Dictionary<string, string>();

        [Export]
        public void GenDBClicked(View v)
        {
            var tf1 = FindViewById<EditText>(Resource.Id.editText1);
            var tf2 = FindViewById<EditText>(Resource.Id.editText2);

            var size = int.Parse(tf1.Text);
            var changedPer = int.Parse(tf2.Text);
            Console.WriteLine("Gen db");
            //RunFunctionTimed(() => GenClientData(size, changedPer));

            RunFunctionTimed(() =>
            {
                foreach (var item in DataGen.Gen(size,changedPer))
                {
                    _clientDic.Add(item.Key, item.Value);
                }
            });
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
            //RunFunctionTimed(() => CheckDbData(size, changedPer));
            RunFunctionTimed(() =>
            {
                foreach (var item in DataGen.Gen(size, changedPer))
                {
                    var inDbVal = _clientDic[item.Key];
                    if (inDbVal != item.Value)
                    {
                        throw new InvalidDataException();
                    }
                }
                if (_clientDic.Count != size + changedPer / 2 * size / 100)
                {
                    throw new InvalidDataException();
                }
            });

            Console.WriteLine("Done checking db");
        }

        [Export]
        public void GenBFClicked(View v)
        {
            Console.WriteLine("Gen BF");
            //RunFunctionTimed(() => GenBfFileFromDb());

            RunFunctionTimed(() =>
            {
                var docFolder = DbManager.AppDir;
                using (var bffile = File.Create(Path.Combine(docFolder, Helper.BFFileName)))
                {
                    KeyValSync.ClientGenBfFile(_clientDic, _clientDic.Count, bffile);
                }
            });
            Console.WriteLine("Done gen bf");
        }

        [Export]
        public async void UploadBfClicked(View v)
        {
            Console.WriteLine("Uploading bf file");

            var sw = new Stopwatch();
            sw.Start();
            using (var file = File.OpenRead(Path.Combine(DbManager.AppDir, Helper.BFFileName)))
            {
                await NetworkManager.FtpUpload(NetworkManager.FtpServer, file, Helper.BFFileName);
            }
            sw.Stop();

            Console.WriteLine("Done uploading bf in {0}", sw.Elapsed);
        }

        [Export]
        public async void DownloadP1Clicked(View v)
        {
            Console.WriteLine("Download p1 clicked", ((Button)v).Text);

            var sw = new Stopwatch();
            sw.Start();
            await NetworkManager.FtpDownload(NetworkManager.FtpServer + Helper.P1FileName, Helper.P1FileName);
            sw.Stop();

            Console.WriteLine("Done downloading p1 in {0}", sw.Elapsed);
        }

        [Export]
        public void Patch1Clicked(View v)
        {
            Console.WriteLine("Patch 1 and gen ibf");
            //RunFunctionTimed(() => PatchAndGenIBFFile());
            RunFunctionTimed(() =>
            {
                var docFolder = DbManager.AppDir;
                using (var patch1File = File.OpenText(Path.Combine(docFolder, Helper.P1FileName)))
                {
                    using (var ibffile = File.Create(Path.Combine(docFolder, Helper.IBFFileName)))
                    {
                        var d0 = int.Parse(patch1File.ReadLine());
                        var patchItems = Helper.ReadLinesFromTextStream(patch1File).Select(str =>
                        {
                            var strArr = str.Split(' ');
                            return new KeyValuePair<string, string>(strArr[0], strArr[1]);
                        });
                        KeyValSync.ClientPatchAndGenIBFFile(_clientDic, currItem => _clientDic[currItem.Key] = currItem.Value, patchItems, d0, ibffile);
                    }
                }
            });

            Console.WriteLine("Done generating ibf file");
        }

        [Export]
        public async void UploadIbfClicked(View v)
        {
            Console.WriteLine("Uploading ibf file");

            var sw = new Stopwatch();
            sw.Start();
            using (var file = File.OpenRead(Path.Combine(DbManager.AppDir, Helper.IBFFileName)))
            {
                await NetworkManager.FtpUpload(NetworkManager.FtpServer, file, Helper.IBFFileName);
            }
            sw.Stop();

            Console.WriteLine("Done uploading ibf in {0}", sw.Elapsed);
        }

        [Export]
        public async void DownloadP2Clicked(View v)
        {
            Console.WriteLine("Download p2 clicked", ((Button)v).Text);

            var sw = new Stopwatch();
            sw.Start();
            await NetworkManager.FtpDownload(NetworkManager.FtpServer + Helper.P2FileName, Helper.P2FileName);
            sw.Stop();

            Console.WriteLine("Done downloading p2 in {0}", sw.Elapsed);
        }

        [Export]
        public void Patch2Clicked(View v)
        {
            Console.WriteLine("Patch2 clicked");
            //RunFunctionTimed(() => ClientApplyPatch2());

            RunFunctionTimed(() =>
            {
                var docFolder = DbManager.AppDir;
                using (var patch2File = File.OpenRead(Path.Combine(docFolder, Helper.P2FileName)))
                {
                    var patchDic = Serializer.Deserialize<Dictionary<string, string>>(patch2File);
                    KeyValSync.ClientApplyPatch<string, string>(currItem => _clientDic[currItem.Key] = currItem.Value, patchDic);
                }
            });
            Console.WriteLine("Done patch 2");
        }

        [Export]
        public void DisposeDbClicked(View v)
        {
            Console.WriteLine("Dispose db clicked");
            DbManager.Dispose();
            Console.WriteLine("Done disposing db");
        }

        [Export]
        public void GenIbfFromOriClicked(View v)
        {
            Console.WriteLine("Gen ibf from original client dic clicked");
            var tf1 = FindViewById<EditText>(Resource.Id.editText1);
            var tf2 = FindViewById<EditText>(Resource.Id.editText2);

            var size = int.Parse(tf1.Text);
            var changedPer = int.Parse(tf2.Text);

            var diffSize = changedPer * size / 100;

            RunFunctionTimed(() =>
            {
                var docFolder = DbManager.AppDir;
                using (var ibffile = File.Create(Path.Combine(docFolder, Helper.IBFFileName)))
                {

                    KeyValSync.ClientGenIBF(_clientDic, diffSize, ibffile);
                }
            });

            Console.WriteLine("Done");
        }

        private static void GenClientData(int size, int changedPer)
        {
            using (var trans = DbManager.Engine.GetTransaction())
            {
                trans.Technical_SetTable_OverwriteIsNotAllowed(DbManager.DefaultTableName);
                foreach (var item in DataGen.Gen(size, changedPer))
                {
                    trans.Insert(DbManager.DefaultTableName, item.Key, item.Value);
                }
                trans.Commit();
            }
        }

        private static bool CheckDbData(int size, int changedPercent)
        {
            using (var trans = DbManager.Engine.GetTransaction())
            {
                trans.Technical_SetTable_OverwriteIsNotAllowed(DbManager.DefaultTableName);
                foreach (var item in DataGen.Gen(size, changedPercent))
                {
                    var inDbVal = trans.Select<string, string>(DbManager.DefaultTableName, item.Key);
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
                trans.Technical_SetTable_OverwriteIsNotAllowed(DbManager.DefaultTableName);
                var clientDic = trans.SelectForward<string, string>(DbManager.DefaultTableName).Select(t => new KeyValuePair<string, string>(t.Key, t.Value));
                var docFolder = DbManager.AppDir;
                using (var bffile = File.Create(Path.Combine(docFolder, "bffile.dat")))
                {
                    KeyValSync.ClientGenBfFile(clientDic, (int)trans.Count(DbManager.DefaultTableName), bffile);
                }
            }
        }

        private static void PatchAndGenIBFFile()
        {
            Console.WriteLine("Starting patching and generating ibf file");
            using (var trans = DbManager.Engine.GetTransaction())
            {
                trans.Technical_SetTable_OverwriteIsNotAllowed(DbManager.DefaultTableName);
                var clientDic = trans.SelectForward<string, string>(DbManager.DefaultTableName).Select(t => new KeyValuePair<string, string>(t.Key, t.Value));
                var docFolder = DbManager.AppDir;
                using (var patch1File = File.OpenText(Path.Combine(docFolder, "patch1file.dat")))
                {
                    using (var ibffile = File.Create(Path.Combine(docFolder, "ibffile.dat")))
                    {
                        var d0 = int.Parse(patch1File.ReadLine());
                        var patchItems = Helper.ReadLinesFromTextStream(patch1File).Select(str =>
                        {
                            var strArr = str.Split(' ');
                            return new KeyValuePair<string, string>(strArr[0], strArr[1]);
                        });

                        KeyValSync.ClientPatchAndGenIBFFile(clientDic, currItem => trans.Insert(DbManager.DefaultTableName, currItem.Key, currItem.Value), patchItems, d0, ibffile);
                    }
                }
                trans.Commit();
            }
        }

        static void ClientApplyPatch2()
        {
            using (var trans = DbManager.Engine.GetTransaction())
            {
                trans.Technical_SetTable_OverwriteIsNotAllowed(DbManager.DefaultTableName);
                var docFolder = DbManager.AppDir;
                using (var patch2File = File.OpenRead(Path.Combine(docFolder, "patch2file.dat")))
                {
                    var patchDic = Serializer.Deserialize<Dictionary<string, string>>(patch2File);
                    KeyValSync.ClientApplyPatch<string, string>(currItem => trans.Insert(DbManager.DefaultTableName, currItem.Key, currItem.Value), patchDic);
                }
                trans.Commit();
            }
        }

        static void RunFunctionTimed(Action act)
        {
            var sw = new Stopwatch();
            sw.Start();
            act();
            sw.Stop();
            Console.WriteLine("Done in {0}", sw.Elapsed);
        }

        static async void RunFunctionTimedAsync(Func<Task> asyncFunc)
        {
            var sw = new Stopwatch();
            sw.Start();
            await asyncFunc();
            sw.Stop();
            Console.WriteLine("Done in {0}", sw.Elapsed);
        }
    }
}


