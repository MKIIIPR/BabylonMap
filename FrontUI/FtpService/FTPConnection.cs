using FluentFTP;
using System;
using System.IO;

namespace FrontUI.FtpService
{
    public class FTPConnection
    {
        private readonly string _ftpServer = "ftp.deinedomain.com";
        private readonly string _username = "deinBenutzername";
        private readonly string _password = "deinPasswort";

        /// <summary>
        /// Wandelt einen Base64-String in eine Datei um und gibt den Pfad zurück.
        /// </summary>
        public static string ConvertBase64ToFile(string base64String, string filePath)
        {
            try
            {
                // Base64-String dekodieren
                byte[] fileBytes = Convert.FromBase64String(base64String);

                // Datei speichern
                File.WriteAllBytes(filePath, fileBytes);
                Console.WriteLine($"Datei erfolgreich gespeichert unter: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Konvertieren des Base64-Strings: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lädt eine lokale Datei per FTP hoch.
        /// </summary>
        public bool UploadFile(string localFilePath, string remoteFileName)
        {
            try
            {
                using var client = new FtpClient(_ftpServer, _username, _password);
                client.EncryptionMode = FtpEncryptionMode.Explicit;
                client.ValidateCertificate += (control, e) => e.Accept = true;
                client.Connect();

                // Synchroner Upload
                FtpStatus result = client.UploadFile(localFilePath, remoteFileName, FtpRemoteExists.Overwrite);
                client.Disconnect();
                return result == FtpStatus.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim FTP-Upload: {ex.Message}");
                return false;
            }
        }
    }

    public class FileUploader
    {
        /// <summary>
        /// Wandelt einen Base64-String in eine Datei um, lädt diese per FTP hoch und löscht anschließend die lokale Datei.
        /// </summary>
        public void UploadBase64File(string base64String)
        {
            // Erstelle einen temporären Dateipfad (z.B. im Temp-Verzeichnis)
            string tempFilePath = Path.Combine(Path.GetTempPath(), "temp_image.png");

            // Base64 in Datei umwandeln
            string createdFilePath = FTPConnection.ConvertBase64ToFile(base64String, tempFilePath);
            if (createdFilePath == null)
            {
                Console.WriteLine("Die Datei konnte nicht erstellt werden.");
                return;
            }

            // Instanz der FTP-Verbindung
            var ftpConn = new FTPConnection();

            // Beispiel: Upload der Datei ins Verzeichnis "/remote_path/" unter dem Namen "image.png"
            bool uploadSuccess = ftpConn.UploadFile(createdFilePath, "/remote_path/image.png");

            if (uploadSuccess)
            {
                Console.WriteLine("Datei erfolgreich hochgeladen.");
            }
            else
            {
                Console.WriteLine("Fehler beim Hochladen der Datei.");
            }

            // Optional: Lösche die temporäre Datei
            try
            {
                if (File.Exists(createdFilePath))
                {
                    File.Delete(createdFilePath);
                    Console.WriteLine("Temporäre Datei gelöscht.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Löschen der temporären Datei: {ex.Message}");
            }
        }
    }
}
