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
                //DbManager.CheckDataSet(5000000, 50);


                //using (var tran = DbManager.Engine.GetTransaction())
                //{
                //    //var clientDic = tran.SelectForward<string, string>("t1").Select(t => new KeyValuePair<string, string>(t.Key, t.Value));

                //    var docFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                //    using (var bffile = File.OpenRead(Path.Combine(docFolder, "bffile.dat")))
                //    {
                //        await NetworkManager.FtpUpload("ftp://10.81.4.120", bffile, "bffile.dat");
                //        //KeyValSync.ClientGenBfFile(clientDic, (int)tran.Count("t1"), bffile);
                //    }
                //}
                var a = 0;
            };
        }
    }
}


