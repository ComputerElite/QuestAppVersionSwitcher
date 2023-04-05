using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ComputerUtils.Android.Logging;

namespace QuestAppVersionSwitcher.Mods
{
    public class ExternalFilesDownloader
    {
        public static void DownloadUrl(string downloadUrlString, string path)
        {
            WebClient client = new WebClient();
            Uri uri = new Uri(downloadUrlString);
            TempFile temp = new TempFile();
            try
            {
                client.DownloadFile(uri, temp.Path);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp.Path, path);
            }
            catch (Exception e)
            {
                Logger.Log("Error downloading file from " +downloadUrlString + ": " + e, LoggingType.Warning);
            }
        }
    }
}