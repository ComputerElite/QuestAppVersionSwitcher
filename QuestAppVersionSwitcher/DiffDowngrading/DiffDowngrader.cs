using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using ComputerUtils.Android.AndroidTools;
using ComputerUtils.Android.Encryption;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using ComputerUtils.Android.VarUtils;
using JetBrains.Annotations;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using QuestAppVersionSwitcher.ClientModels;
using QuestAppVersionSwitcher.Core;
using QuestAppVersionSwitcher.Mods;

namespace QuestAppVersionSwitcher.DiffDowngrading
{
    public class DiffDowngrader : GameDownloadManager
    {
        public override string packageName { get; set; } = "";
        public override string version { get; set; } = "";
        public override string gameName { get; set; } = "";
        public override long filesToDownload { get; set; } = 0;
        public override long filesDownloaded { get; set; } = 0;

        public override double progress
        {
            get
            {
                if(totalBytes == 0) return 0;
                return downloadedBytes / (double)totalBytes;
            }
        }

        public override string progressString
        {
            get
            {
                return String.Format("{0:0.#}", progress * 100) + "%";
            }
        }
        public override string id { get; set; } = "";
        public override string status { get; set; } = "";
        public override string textColor { get; set; } = "#FFFFFF";
        public override string backupName { get; set; } = "";

        public override long totalBytes { get; set; } = 0;
        public override long downloadedBytes { get; set; } = 0;
        public override long eTASeconds { get; set; } = 0;
        public override long speed { get; set; } = 0;

        private long downloadedFilesTotalBytes = 0;
        public override bool canceled { get; set; } = false;
        public override bool error { get; set; } = false;
        public override bool entitlementError { get; set; } = false;
        public override bool done { get; set; } = false;
        public string targetVersion { get; set; } = "";
        [CanBeNull] public DiffDowngradeEntry entry { get; set; } = null;

        public DiffDowngrader(DiffDownloadRequest r)
        {
            this.packageName = r.packageName;
            this.targetVersion = r.targetVersion;
            
            // parse https://raw.githubusercontent.com/ComputerElite/APKDowngrader/main/versions.json
            string json = ExternalFilesDownloader.DownloadStringWithTimeout("https://raw.githubusercontent.com/ComputerElite/APKDowngrader/main/versions.json", 10000);
            DiffDowngradeEntryContainer entries = JsonSerializer.Deserialize<DiffDowngradeEntryContainer>(json);
            foreach (DiffDowngradeEntry e in entries.versions)
            {
                if (e.appid == r.packageName && e.TSHA256 == r.targetSha && e.SSHA256 == r.sourceSha)
                {
                    entry = e;
                    break;
                }
            }
        }

        private DownloadManager diffFileDownloadManager;
        public Thread updateThread;
        public string diffFileDownloadPath = "";

        public void StartDownload()
        {
            id = DateTime.Now.Ticks.ToString();
            diffFileDownloadPath = CoreService.coreVars.QAVSTmpDowngradeDir + id + ".xdelta3";
            version = this.targetVersion;
            gameName = this.packageName;
            packageName = this.packageName;
            status = "Downloading patch for " + gameName + " " + version;

            this.backupName = gameName + " " + version + " Downgraded";
            foreach (char r in QAVSWebserver.ReservedChars)
            {
                this.backupName = this.backupName.Replace(r, '_');
            }
            status = gameName + " " + version;
            
            // apk download
            filesToDownload = 1;
            
            
            diffFileDownloadManager = new DownloadManager();
            diffFileDownloadManager.connections = 10;
            diffFileDownloadManager.StartDownload(entry.download, diffFileDownloadPath);
            diffFileDownloadManager.DownloadFinishedEvent += DownloadCompleted;
            diffFileDownloadManager.NotFoundDownloadErrorEvent += NotFoundDownloadError;
            diffFileDownloadManager.DownloadErrorEvent += DownloadError;
            diffFileDownloadManager.isCancelable = false;
            downloadManagers.Add(diffFileDownloadManager);
            updateThread = new Thread(() =>
            {
                while (filesDownloaded < filesToDownload)
                {
                    if (canceled) return;
                    UpdateManagersAndProgress();
                    Thread.Sleep(500);
                }

                Done();
            });
            updateThread.Start();
        }

        private void SetEntitlementError()
        {
            entitlementError = true;
            status = "This downgrade is not currently available. We're sorry for the inconvenience.";
        }

        public void Done()
        {
            status = "Download completed. Applying diff patch to current apk. Please wait up to 5 minutes.";
            downloadedBytes = totalBytes;
            UpdateManagersAndProgress();
            QAVSWebserver.BroadcastDownloads(true);
            string backupDir = CoreService.coreVars.QAVSBackupDir + this.packageName + "/" + this.backupName + "/";
            FileManager.RecreateDirectoryIfExisting(backupDir);
            // Get installed apk
            string appPath = AndroidService.FindAPKLocation(entry.appid);
            using (FileStream input = File.OpenRead(appPath))
            {
                using(FileStream patch = File.OpenRead(diffFileDownloadPath)) {
                    using (FileStream output = File.Create(backupDir + "app.apk"))
                    {
                        VCDiff.Decoders.VcDecoder decoder = new VCDiff.Decoders.VcDecoder(input, patch, output);
                        long bytesWritten;
                        Logger.Log("Decoding diff file for " + gameName + " " + version + " to " + backupDir + "app.apk");
                        decoder.Decode(out bytesWritten);
                        Logger.Log("Wrote " + bytesWritten + " bytes to " + backupDir + "app.apk");
                        decoder.Dispose();
                    }
                }
            }
            BackupInfo info = BackupManager.GetBackupInfo(backupDir, true); // Populate info.json correctly
            RealDone();
        }

        public void RealDone()
        {
            done = true;
            textColor = "#00FF00";
            status = "Downgrade done";
            downloadedBytes = totalBytes;
            QAVSWebserver.BroadcastDownloads(true);
        }

        private long lastBytes = 0;
        private List<long> lastBytesPerSec = new List<long>();
        private DateTime lastUpdate = DateTime.Now;

        public void UpdateManagersAndProgress()
        {
            totalBytes = diffFileDownloadManager.total;
            downloadedBytes = diffFileDownloadManager.done;
            
            // Speed
            double secondsPassed = (DateTime.Now - lastUpdate).TotalSeconds;
            long bytesPerSec = (long)Math.Round((downloadedBytes - lastBytes) / secondsPassed);
            lastBytesPerSec.Add(bytesPerSec);
            if (lastBytesPerSec.Count > 15) lastBytesPerSec.RemoveAt(0);
            lastBytes = downloadedBytes;
            long avg = 0;
            foreach (long l in lastBytesPerSec) avg += l;
            avg /= lastBytesPerSec.Count;
            lastUpdate = DateTime.Now;
            if(avg != 0) eTASeconds = (totalBytes - downloadedBytes) / avg;
            speed = bytesPerSec;
            QAVSWebserver.BroadcastDownloads(false);
        }

        private void DownloadError(DownloadManager manager)
        {
            Cancel();
            error = true;
            canceled = false;
            status = "An unknown error occurred during the download of " + gameName + " " + version + ". Please consult support.";
            QAVSWebserver.BroadcastDownloads(true);
        }
        
        private void NotFoundDownloadError(DownloadManager manager)
        {
            Cancel();
            SetEntitlementError();
            QAVSWebserver.BroadcastDownloads(true);
        }

        public void Cancel()
        {
            textColor = "#FF0000";
            canceled = true;
            foreach (DownloadManager d in downloadManagers)
            {
                d.StopDownload();
            }
            downloadManagers.Clear();
            QAVSWebserver.BroadcastDownloads(true);
        }

        public void DownloadCompleted(DownloadManager m)
        {
            filesDownloaded++;
            downloadManagers.Remove(m);
        }
    }
}