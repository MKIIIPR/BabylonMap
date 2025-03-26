
using System.Net;
using System;
using System.IO;using System.Threading.Tasks;

namespace AshesMapBib.FTPService
{
    public class FtpService
    {
        private readonly string _ftpServer = "ftp://ftp.deinedomain.com";
        private readonly string _username = "deinBenutzername";
        private readonly string _password = "deinPasswort";

        public async Task<bool> UploadFileAsync(string localFilePath, string remoteFileName)
        {
            try
            {
                string ftpFullPath = $"{_ftpServer}/{remoteFileName}";

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpFullPath);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(_username, _password);
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;

                byte[] fileContents = await File.ReadAllBytesAsync(localFilePath);
                request.ContentLength = fileContents.Length;

                using Stream requestStream = await request.GetRequestStreamAsync();
                await requestStream.WriteAsync(fileContents, 0, fileContents.Length);

                using FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync();
                Console.WriteLine($"Upload Status: {response.StatusDescription}");

                return response.StatusCode == FtpStatusCode.ClosingData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Upload: {ex.Message}");
                return false;
            }
        }
    }

}
