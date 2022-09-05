using System;
using System.Net;
using System.Threading.Tasks;

namespace QuestAppVersionSwitcher.Mods
{
    public class ExternalFilesDownloader
    {
        public static void DownloadUrl(string downloadUrlString, string path)
        {
            WebClient client = new WebClient();
            Uri uri = new Uri(downloadUrlString);
            client.DownloadFile(uri, path);
        }
    }
}