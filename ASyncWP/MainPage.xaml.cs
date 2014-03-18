using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using ASyncWP.Resources;
using System.IO;
using ASyncLib;
using System.Net.Http;
using System.IO.IsolatedStorage;
using System.Text;
using ProtoBuf;
using System.Threading.Tasks;

namespace ASyncWP
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private async void DownloadFile(string url)
        {
            var wc = new WebClient();
            //wc.OpenReadTaskAsync()
            using (var c = new HttpClient())
            {
                var stream = await c.GetStreamAsync(url);
                using (var f = IsolatedStorageFile.GetUserStoreForApplication().CreateFile("downloaded.data"))
                {
                    await stream.CopyToAsync(f);
                    var a = 0;
                }
            }
        }

        private async Task<System.IO.Stream> Upload(string url, string filePath)
        {
            using (var fStream = File.OpenRead(filePath))
            {
                using (var client = new HttpClient())
                {
                    using (var fileStreamContent = new StreamContent(fStream))
                    {
                        var b = "--customBoundary";
                        using (var formData = new MultipartFormDataContent(b))
                        {
                            // Work around hfs - remove quotes from boundary
                            formData.Headers.Remove("Content-Type");
                            formData.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + b);

                            formData.Add(fileStreamContent, "file", Path.GetFileName(filePath));

                            var response = await client.PostAsync(url, formData);
                            if (!response.IsSuccessStatusCode)
                            {
                                return null;
                            }
                            var res = await response.Content.ReadAsStreamAsync();
                            return res;
                        }
                    }
                }
            }
        }

        private void RunClicked(object sender, RoutedEventArgs e)
        {
            var clientDic = new Dictionary<string, string>();

            //Upload("http://10.81.4.120:8080/aaa/", "Data/50000-clientDic.dat");
            DownloadFile("http://10.81.4.120:8080/aaa/50000-clientDic.dat");

            var _clientDic = new Dictionary<string, string>();
            var _serverDic = new Dictionary<string, string>();
            var _bffile = new MemoryStream();
            var _p1file = new MemoryStream();
            var _ibffile = new MemoryStream();
            var _p2file = new MemoryStream();

            for (var i = 0; i < 200; ++i)
            {
                _clientDic.Add(i.ToString(), i.ToString());
            }
            for (var i = 0; i < 250; ++i)
            {
                _serverDic.Add(i.ToString(), i.ToString());
            }

            KeyValSync.ClientGenBfFile(_clientDic, _bffile);
            _bffile.Position = 0;
            KeyValSync.ServerGenPatch1File(_serverDic, _bffile, _p1file);
            _p1file.Position = 0;
            KeyValSync.ClientPatchAndGenIBFFile(_clientDic, _p1file, _ibffile);
            _ibffile.Position = 0;
            KeyValSync.ServerGenPatch2FromIBF(_serverDic, _ibffile, _p2file);
            _p2file.Position = 0;
            KeyValSync.ClientPatch(_clientDic, _p2file);

            var ans = KeyValSync.AreTheSame(_clientDic, _serverDic);

            var a = 0;
        }
    }
}