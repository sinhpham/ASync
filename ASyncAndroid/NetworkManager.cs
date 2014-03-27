﻿using System;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace ASyncAndroid
{
    public static class NetworkManager
    {
        static async Task FtpUpload(string url, Stream fileStream)
        {
            // Get the object used to communicate with the server.
            var request = (FtpWebRequest)WebRequest.Create(url + "/" + Path.GetFileName("uploaded.dat"));
            request.Method = WebRequestMethods.Ftp.UploadFile;

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential("asyncuser", (string)null);

            //using (var fileStream = File.OpenRead(filePath))
            {
                //request.ContentLength = fileStream.Length;
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
        }

        static async Task FtpDownload(string url, string filePath)
        {
            // Get the object used to communicate with the server.
            var request = (FtpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential("asyncuser", (string)null);

            var docFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
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

