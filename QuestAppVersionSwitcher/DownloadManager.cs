using ComputerUtils.Android.Encryption;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using ComputerUtils.Android.VarUtils;
using QuestAppVersionSwitcher.ClientModels;
using QuestAppVersionSwitcher.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using Newtonsoft.Json;
using QuestAppVersionSwitcher.Mods;

namespace QuestAppVersionSwitcher
{
    public class DownloadManager : DownloadProgress
    {
        public delegate void DownloadFinished(DownloadManager manager);
        public event DownloadFinished DownloadFinishedEvent;
        public event DownloadFinished DownloadErrorEvent;
        public event DownloadFinished DownloadCanceled;
        public event DownloadFinished NotFoundDownloadErrorEvent;
        [JsonIgnore]
        public string tmpPath = "";
        [JsonIgnore]
        public bool isObb = false;
        [JsonIgnore]
		public FileDownloader downloader = new FileDownloader();
        [JsonIgnore]
        public bool canceled = false;
        [JsonIgnore]
        public string obbFileName = "";

        [JsonIgnore]
        public int connections = 1;
        
        public void StopDownload()
		{
			canceled = true;
			downloader.Cancel();
            if(DownloadCanceled != null) DownloadCanceled(this);
            SetEmpty(false);
			this.backupName = "Download Canceled";
			this.textColor = "#EE0000";
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
		}

		public void StartDownload(string binaryid, string password, string version, string app, string appId, bool isObb, string packageName, string obbFileName = "")
        {
            this.obbFileName = obbFileName;
            this.packageName = packageName;
            this.version = version;
            this.isObb = isObb;
            string decodedToken = PasswordEncryption.Decrypt(CoreService.coreVars.token, password);
            downloader = new FileDownloader();
            tmpPath = CoreService.coreVars.QAVSTmpDowngradeDir + DateTime.Now.Ticks + (isObb ? ".obb" : ".apk");
            List<long> lastBytesPerSec = new List<long>();
            DateTime lastUpdate = DateTime.Now;
            bool locked = false;
            long BytesToRecieve = 0;
            long lastBytes = 0;
            this.name = app + " " + version;
            this.backupName = this.name + " Downgraded";
            this.text = this.isObb ? obbFileName : app + ".apk";
            foreach (char r in QAVSWebserver.ReservedChars)
            {
                this.backupName = this.backupName.Replace(r, '_');
            }
            downloader.OnDownloadProgress = () =>
            {
                if (locked) return;
                if (canceled) return;

                locked = true;
                double secondsPassed = (DateTime.Now - lastUpdate).TotalSeconds;
                if (secondsPassed >= 0.2)
                {
                    BytesToRecieve = downloader.totalBytes;
                    long bytesPerSec = (long)Math.Round((downloader.downloadedBytes - lastBytes) / secondsPassed);
                    lastBytesPerSec.Add(bytesPerSec);
                    if (lastBytesPerSec.Count > 15) lastBytesPerSec.RemoveAt(0);
                    lastBytes = downloader.downloadedBytes;
                    long avg = 0;
                    foreach (long l in lastBytesPerSec) avg += l;
                    avg = avg / lastBytesPerSec.Count;
                    this.done = downloader.downloadedBytes;
                    this.total = BytesToRecieve;
                    this.speed = bytesPerSec;
                    if(avg != 0) this.eTASeconds = (downloader.totalBytes - downloader.downloadedBytes) / avg;
                    this.doneString = SizeConverter.ByteSizeToString(this.done);
                    this.totalString = SizeConverter.ByteSizeToString(this.total);
                    this.speedString = SizeConverter.ByteSizeToString(this.speed, 0) + "/s";
                    this.eTAString = SizeConverter.SecondsToBetterString(this.eTASeconds);
                    this.percentage = this.total == 0 ? 0 : this.done / (double)this.total;
                    this.percentageString = String.Format("{0:0.#}", this.percentage * 100) + "%";
                    lastUpdate = DateTime.Now;
                    QAVSWebserver.BroadcastDownloads(false);
                }
                locked = false;
            };
            downloader.OnDownloadComplete = () =>
            {
                if (canceled) return;
                
                DownloadFinishedEvent(this);
                this.backupName = "Done: restore " + backupName + " to downgrade your game any time";
                this.done = this.total;
                this.doneString = this.totalString;
                this.percentage = 1.0;
                this.percentageString = "100%";
                this.textColor = "#30e34b";
                QAVSWebserver.BroadcastDownloads(true);
            };
            downloader.OnDownloadError = () =>
            {
                if (File.Exists(tmpPath)) File.Delete(tmpPath);
                SetEmpty();
                if (downloader.exception.ToString().Contains("404"))
                {
                    if(NotFoundDownloadErrorEvent != null) NotFoundDownloadErrorEvent.Invoke(this);
                    this.backupName =
                        "404 not found. The file you tried to download does not exist";
                    this.textColor = "#EE0000";
                    QAVSWebserver.BroadcastDownloads(true);
                    return;
                }
                this.backupName =
                    "Unknown Error: Have you entered your token in the Tools & Options section? Doing this is needed. Otherwise you don't own the game you are trying to download.";
                this.textColor = "#EE0000";
                QAVSWebserver.BroadcastDownloads(true);
                if (DownloadErrorEvent != null) DownloadErrorEvent(this);
            };
            downloader.DownloadFile("https://securecdn.oculus.com/binaries/download/?id=" + binaryid + "&access_token=" + decodedToken, tmpPath, connections);
        }

        public void StartDownload(string url, string path)
        {
            WebClient downloader = new WebClient();
            downloader.Headers.Add("User-Agent", "QuestAppVersionSwitcher/" + CoreService.version.ToString());
            string p = new TempFile().Path;
            tmpPath = p;
            List<long> lastBytesPerSec = new List<long>();
            DateTime lastUpdate = DateTime.Now;
            bool locked = false;
            long BytesToRecieve = 0;
            long lastBytes = 0;
            this.name = Path.GetFileName(url);
            this.backupName = "";
            downloader.DownloadProgressChanged += (o, e) =>
            {
                if (locked) return;

                locked = true;
                double secondsPassed = (DateTime.Now - lastUpdate).TotalSeconds;
                if (secondsPassed >= 0.2)
                {
                    BytesToRecieve = e.TotalBytesToReceive;
                    string current = SizeConverter.ByteSizeToString(e.BytesReceived);
                    string total = SizeConverter.ByteSizeToString(BytesToRecieve);
                    long bytesPerSec = (long)Math.Round((e.BytesReceived - lastBytes) / secondsPassed);
                    lastBytesPerSec.Add(bytesPerSec);
                    if (lastBytesPerSec.Count > 5) lastBytesPerSec.RemoveAt(0);
                    lastBytes = e.BytesReceived;
                    long avg = 0;
                    foreach (long l in lastBytesPerSec) avg += l;
                    avg = avg / lastBytesPerSec.Count;
                    this.done = e.BytesReceived;
                    this.total = BytesToRecieve;
                    this.speed = bytesPerSec;
                    this.eTASeconds = (e.TotalBytesToReceive - e.BytesReceived) / avg;
                    this.doneString = SizeConverter.ByteSizeToString(this.done);
                    this.totalString = SizeConverter.ByteSizeToString(this.total);
                    this.speedString = SizeConverter.ByteSizeToString(this.speed, 0) + "/s";
                    this.eTAString = SizeConverter.SecondsToBetterString(this.eTASeconds);
                    this.percentage = this.done / (double)this.total;
                    this.percentageString = String.Format("{0:0.#}", this.percentage * 100) + "%";
                    lastUpdate = DateTime.Now;
                    QAVSWebserver.BroadcastDownloads(false);
                }
                locked = false;
            };
            downloader.DownloadFileCompleted += (o, e) =>
            {
                if(e.Error != null)
                {
                    Logger.Log(e.Error.ToString(), LoggingType.Warning);
                }
                File.Move(tmpPath, path);
                QAVSWebserver.managers.Remove(this);
                QAVSWebserver.BroadcastDownloads(true);
                DownloadFinishedEvent(this);
            };
            Logger.Log(tmpPath);
            downloader.DownloadFileAsync(new Uri(url), tmpPath);
        }

        public void SetEmpty(bool alsoSize = true)
        {
            
            this.speed = 0;
            this.eTASeconds = 0;
            if(alsoSize)
            {
                this.done = 0;
                this.total = 0;
                this.doneString = "";
                this.totalString = "";
                this.percentage = 0;
                this.percentageString = "";
            }
            this.speedString = "";
            this.eTAString = "";
        }
    }
}