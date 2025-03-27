using FluentFTP;
using System;
using System.IO;

namespace FrontUI.FtpService
{
    public class FTPConnection
    {
        private readonly string _ftpServer = "invi.rocks";
        private readonly string _username = "beelzee";
        private readonly string _password = "8lDI$T*ybGrJz%BY19SF";

        /// <summary>
        /// Wandelt einen Base64-String in eine Datei um und gibt den Pfad zurück.
        /// </summary>
        public static string ConvertBase64ToFile(string base64String, string filePath)
        {
            try
            {
                // Falls der String mit "data:" beginnt, entferne den Präfix
                if (base64String.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    var commaIndex = base64String.IndexOf(',');
                    if (commaIndex >= 0)
                    {
                        base64String = base64String.Substring(commaIndex + 1);
                    }
                }

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
        /// Wandelt einen Base64-String in eine Datei um, lädt diese per FTP hoch und löscht die lokale Datei.
        /// Der Remote-Dateiname wird dabei verwendet.
        /// </summary>
        public void UploadBase64FileToRemote(string base64String, string remoteFileName)
        {
            // Erstelle einen temporären Dateipfad basierend auf dem Remote-Dateinamen
            string tempFilePath = Path.Combine(Path.GetTempPath(), remoteFileName);

            // Konvertiere den Base64-String in eine Datei
            string createdFilePath = FTPConnection.ConvertBase64ToFile(base64String, tempFilePath);
            if (createdFilePath == null)
            {
                Console.WriteLine("Die Datei konnte nicht erstellt werden.");
                return;
            }

            // Erstelle eine Instanz der FTP-Verbindung
            var ftpConn = new FTPConnection();
            // Beispiel: Datei wird ins Verzeichnis "/remote_path/" hochgeladen. Passe den Pfad ggf. an.
            bool uploadSuccess = ftpConn.UploadFile(createdFilePath, "/images/" + remoteFileName);

            if (uploadSuccess)
            {
                Console.WriteLine("Datei erfolgreich hochgeladen.");
            }
            else
            {
                Console.WriteLine("Fehler beim Hochladen der Datei.");
            }

            // Lösche die temporäre Datei nach dem Upload
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
