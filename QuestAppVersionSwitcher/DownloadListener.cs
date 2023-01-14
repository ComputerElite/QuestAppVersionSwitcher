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
			CoreService.browser.EvaluateJavascript("ShowToast('Downloading', '#FFFFFF', '#222222')", null);
			string extension = Path.GetExtension(url.Split('?')[0]);
			string fileName = "downloaded" + DateTime.Now.Ticks;
			string modPath = CoreService.coreVars.QAVSTmpModsDir + fileName + extension;
			DownloadManager m = new DownloadManager();
            m.DownloadFinishedEvent += (manager) =>
            {
                CoreService.browser.EvaluateJavascript("ShowToast('Downloaded, now installing', '#FFFFFF', '#222222')", null);
                Thread t = new Thread(() =>
                {
					QAVSModManager.InstallMod(modPath, Path.GetFileName(modPath));
                    FileManager.DeleteFileIfExisting(modPath);
                });
                t.Start();
            };
            m.StartDownload(url, modPath);
            QAVSWebserver.managers.Add(m);
        }
    }
}