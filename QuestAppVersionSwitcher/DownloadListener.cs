using Android.Webkit;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using Java.Interop;
using Java.Lang;
using QuestAppVersionSwitcher.Core;
using QuestAppVersionSwitcher.Mods;
using System;
using System.IO;
using System.Net;

namespace QuestAppVersionSwitcher
{
    public class DownloadListener : Java.Lang.Object, IDownloadListener
    {
        public void OnDownloadStart(string url, string userAgent, string contentDisposition, string mimetype, long contentLength)
		{
			Logger.Log("Downloading mod from " + url);
            if (url.StartsWith("blob"))
            {
                Logger.Log("Blob URL detected, opening " + QAVSJavascriptInterface.getBase64StringFromBlobUrl(url, mimetype));
                CoreService.browser.LoadUrl(QAVSJavascriptInterface.getBase64StringFromBlobUrl(url, mimetype));
                return;
            }
            // Split cotentDisposition to get the filename
            string[] split = contentDisposition.Split("filename=");
            string filename = split[1].Replace("\"", "");
            QAVSModManager.InstallModFromUrl(url, filename);
        }
    }
}