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
                    using (var fileStreamContent = new StreamContent(fStream, 4096*8))
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

        public static async Task<string> MyUploader(string strFileToUpload, string strUrl)
        {
            string strFileFormName = "file";
            Uri oUri = new Uri(strUrl);
            string strBoundary = "----------" + DateTime.Now.Ticks.ToString("x");

            // The trailing boundary string
            
            byte[] boundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + strBoundary + "\r\n");

            // The post message header
            StringBuilder sb = new StringBuilder();
            sb.Append("--");
            sb.Append(strBoundary);
            sb.Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"");
            sb.Append(strFileFormName);
            sb.Append("\"; filename=\"");
            sb.Append(Path.GetFileName(strFileToUpload));
            sb.Append("\"");
            sb.Append("\r\n");
            sb.Append("Content-Type: ");
            sb.Append("application/octet-stream");
            sb.Append("\r\n");
            sb.Append("\r\n");
            string strPostHeader = sb.ToString();
            byte[] postHeaderBytes = Encoding.UTF8.GetBytes(strPostHeader);

            // The WebRequest
            HttpWebRequest oWebrequest = (HttpWebRequest)WebRequest.Create(oUri);
            oWebrequest.ContentType = "multipart/form-data; boundary=" + strBoundary;
            oWebrequest.Method = "POST";

            // This is important, otherwise the whole file will be read to memory anyway...
            oWebrequest.AllowWriteStreamBuffering = false;

            // Get a FileStream and set the final properties of the WebRequest
            FileStream oFileStream = new FileStream(strFileToUpload, FileMode.Open, FileAccess.Read);
            long length = postHeaderBytes.Length + oFileStream.Length + boundaryBytes.Length;
            oWebrequest.ContentLength = length;
            Stream oRequestStream = await oWebrequest.GetRequestStreamAsync();

            // Write the post header
            oRequestStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);

            // Stream the file contents in small pieces (4096 bytes, max).
            byte[] buffer = new Byte[checked((uint)Math.Min(4096, (int)oFileStream.Length))];
            int bytesRead = 0;
            while ((bytesRead = oFileStream.Read(buffer, 0, buffer.Length)) != 0)
                oRequestStream.Write(buffer, 0, bytesRead);
            oFileStream.Close();

            // Add the trailing boundary
            oRequestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
            WebResponse oWResponse = await oWebrequest.GetResponseAsync();
            Stream s = oWResponse.GetResponseStream();
            StreamReader sr = new StreamReader(s);
            String sReturnString = sr.ReadToEnd();

            // Clean up
            oFileStream.Close();
            oRequestStream.Close();
            s.Close();
            sr.Close();

            return sReturnString;
        }

        private void RunClicked(object sender, RoutedEventArgs e)
        {
            var clientDic = new Dictionary<string, string>();

            var x = MyUploader("Data/50000-clientDic.dat", "http://10.81.4.120:8080/aaa/").Result;

            //Upload("http://10.81.4.120:8080/aaa/", "Data/500000-clientDic.dat");
            //DownloadFile("http://10.81.4.120:8080/aaa/50000-clientDic.dat");

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