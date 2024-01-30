using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ComputerUtils.Android.Logging;
using ComputerUtils.Android.VarUtils;
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
                QAVSModManager.UpdateRunningOperation(operationId,
                    operationPrefix + " (" + SizeConverter.ByteSizeToString(downloader.downloadedBytes) + " / " +
                    SizeConverter.ByteSizeToString(downloader.totalBytes) + ")");
            };
            downloader.DownloadFileInternal(downloadUrlString, path, 1); // Internal method blocks the thread until download is complete
        }

        public static string DownloadStringWithTimeout(string url, int timeout)
        {
            HttpWebRequest r = new HttpWebRequest(new Uri(url));
            r.Timeout = timeout;
            WebResponse res = r.GetResponse();
            try
            {
                using (StreamReader reader = new StreamReader(res.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Logger.Log("Error while downloading string from " + url + ": " + e, LoggingType.Error);
                return "";
            }
        }
    }
}