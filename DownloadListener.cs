using Android.Webkit;
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
			CoreService.browser.EvaluateJavascript("ShowToast('Downloading mod', '#FFFFFF', '#222222')", null);
            string modPath = CoreService.coreVars.QAVSTmpModsDir + "downloadedmod" + DateTime.Now.Ticks + ".qmod";
            DownloadManager m = new DownloadManager();
            m.DownloadFinishedEvent += (manager) =>
            {
                CoreService.browser.EvaluateJavascript("ShowToast('Downloaded, now installing', '#FFFFFF', '#222222')", null);
                Thread t = new Thread(() =>
                {
                    QAVSModManager.InstallMod(File.ReadAllBytes(modPath), Path.GetFileName(modPath));
                    File.Delete(modPath);
                });
                t.Start();
            };
            m.StartDownload(url, modPath);
            QAVSWebserver.managers.Add(m);
        }
    }
}