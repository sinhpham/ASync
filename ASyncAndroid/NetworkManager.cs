using System;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace ASyncAndroid
{
    public static class NetworkManager
    {
        public static async Task FtpUpload(string url, Stream fileStream, string fileName)
        {
            // Get the object used to communicate with the server.
            var request = (FtpWebRequest)WebRequest.Create(url + "/" + fileName);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential("asyncuser", (string)null);
            request.ContentLength = fileStream.Length;

            using (var requestStream = await request.GetRequestStreamAsync())
            {
                await fileStream.CopyToAsync(requestStream);
                requestStream.Close();

                using (var response = (FtpWebResponse)(await request.GetResponseAsync()))
                {
                    Console.WriteLine("Upload File Complete, status {0}", response.StatusDescription);
                }
            }
        }

        public static async Task FtpDownload(string url, string filePath)
        {
            // Get the object used to communicate with the server.
            var request = (FtpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential("asyncuser", (string)null);

            var docFolder = DbManager.AppDir;
            var filename = Path.Combine(docFolder, filePath);

            using (var fileStream = File.Create(filename))
            {
                using (var responseStr = ((FtpWebResponse)(await request.GetResponseAsync())).GetResponseStream())
                {
                    await responseStr.CopyToAsync(fileStream);
                    Console.WriteLine("Done downloading");
                }
            }
        }
    }
}

