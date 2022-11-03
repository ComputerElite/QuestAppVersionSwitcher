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

namespace QuestAppVersionSwitcher
{
    public class DownloadManager : DownloadProgress
    {
        public delegate void DownloadFinished(DownloadManager manager);
        public event DownloadFinished DownloadFinishedEvent;
        public string tmpPath = "";
        public bool isObb = false;
        public string packageName = "";

        public void StartDownload(string binaryid, string password, string version, string app, string appId, bool isObb, string packageName)
        {
            this.packageName = packageName;
            this.isObb = isObb;
            string decodedToken = PasswordEncryption.Decrypt(CoreService.coreVars.token, password);
            WebClient downloader = new WebClient();
            tmpPath = CoreService.coreVars.QAVSTmpDowngradeDir + DateTime.Now.Ticks + (isObb ? ".obb" : ".apk");
            List<long> lastBytesPerSec = new List<long>();
            DateTime lastUpdate = DateTime.Now;
            bool locked = false;
            long BytesToRecieve = 0;
            long lastBytes = 0;
            this.name = app + " " + version;
            this.backupName = this.name + " Downgraded";
            foreach (char r in QAVSWebserver.ReservedChars)
            {
                this.backupName = this.backupName.Replace(r, '_');
            }
            downloader.DownloadProgressChanged += (o, e) =>
            {
                if (locked) return;

                locked = true;
                double secondsPassed = (DateTime.Now - lastUpdate).TotalSeconds;
                if (secondsPassed >= 0.5)
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
                }
                locked = false;
            };
            downloader.DownloadFileCompleted += (o, e) =>
            {
                if (e.Error != null)
                {
                    Logger.Log(e.Error.ToString(), LoggingType.Error);
                    if (File.Exists(tmpPath)) File.Delete(tmpPath);
                    SetEmpty();
                    this.backupName = "Unknown Error: Have you entered your token in the Tools & Options section? Doing this is needed. Otherwise you don't own the game you are trying to download.";
                    this.textColor = "#EE0000";
                }
                else
                {
                    DownloadFinishedEvent(this);
                    this.backupName = "Done: restore " + backupName + " to downgrade your game any time";
                    this.done = this.total;
                    this.doneString = this.totalString;
                    this.percentage = 1.0;
                    this.percentageString = "100%";
                    this.textColor = "#30e34b";
                }
            };
            downloader.DownloadFileAsync(new Uri("https://securecdn.oculus.com/binaries/download/?id=" + binaryid + "&access_token=" + decodedToken), tmpPath);
        }

        public void StartDownload(string url, string path)
        {
            WebClient downloader = new WebClient();
            downloader.Headers.Add("User-Agent", "QuestAppVersionSwitcher/" + CoreService.version.ToString());
            FileManager.CreateDirectoryIfNotExisting(CoreVars.fileDir);
            string p = CoreVars.fileDir + DateTime.Now.Ticks;
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
                if (secondsPassed >= 0.5)
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