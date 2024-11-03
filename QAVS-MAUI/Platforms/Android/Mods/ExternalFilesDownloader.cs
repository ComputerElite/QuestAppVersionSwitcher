using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ComputerUtils.Logging;
using ComputerUtils.VarUtils;
using Java.Lang;
using Exception = System.Exception;

namespace QuestAppVersionSwitcher.Mods
{
    public class ExternalFilesDownloader
    {
        public static void DownloadUrl(string downloadUrlString, string path, int operationId, string operationPrefix)
        {
            FileDownloader downloader = new FileDownloader();
            downloader.OnDownloadProgress += () =>
            {
                if (operationId == -1) return;
                QAVSModManager.UpdateRunningOperation(operationId,
                    operationPrefix + " (" + SizeConverter.ByteSizeToString(downloader.downloadedBytes) + " / " +
                    SizeConverter.ByteSizeToString(downloader.totalBytes) + ")");
            };
            downloader.DownloadFileInternal(downloadUrlString, path, 1); // Internal method blocks the thread until download is complete
        }

        public static string DownloadStringWithTimeout(string url, int timeout)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMilliseconds(timeout);
                try
                {
                    HttpResponseMessage response = client.GetAsync(url).Result;
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().Result;
                }
                catch (Exception e)
                {
                    Logger.Log("Error while downloading string from " + url + ": " + e, LoggingType.Error);
                    return "";
                }
            }
        }
    }
}