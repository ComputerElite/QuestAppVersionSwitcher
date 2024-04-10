using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        
        public virtual List<DownloadManager> downloadManagers { get; set; } = new List<DownloadManager>();
        
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
        public List<FileDiffDowngradeEntry> diffsToDo { get; set; } = new List<FileDiffDowngradeEntry>();
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

        public void StartDownload()
        {
            version = this.targetVersion;
            gameName = this.packageName;
            packageName = this.packageName;
            status = "Downloading patches for " + gameName + " " + version;

            this.backupName = gameName + " " + version + " Downgraded";
            foreach (char r in QAVSWebserver.ReservedChars)
            {
                this.backupName = this.backupName.Replace(r, '_');
            }
            status = gameName + " " + version;
            
            // apk and obb download
            filesToDownload = entry.otherFiles.Count + 1;
            UpdateMaxConnections();
            
            diffFileDownloadManager = new DownloadManager();
            diffFileDownloadManager.connections = maxConcurrentConnections;
            diffFileDownloadManager.StartDownload(entry.download, CoreService.coreVars.QAVSTmpDowngradeDir + entry.diffFilename);
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
            diffsToDo.AddRange(entry.otherFiles);
            updateThread.Start();
        }
        
        public void UpdateManagersAndProgress()
        {
            UpdateMaxConnections();
            totalBytes = diffFileDownloadManager.total + entry.otherFiles.Select(x => x.DiffByteSize).Sum();
            downloadedBytes = downloadedFilesTotalBytes + (diffFileDownloadManager.downloadDone ? 0 : diffFileDownloadManager.done) + downloadManagers.Where(x => x.isObb).Select(x => x.done).Sum();
            
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
            
            
            for (int i = 0; i < maxConcurrentDownloads - downloadManagers.Count; i++)
            {
                if (diffsToDo.Count <= 0) return;
                DownloadManager m = new DownloadManager();
                m.connections = maxConcurrentConnections;
                m.DownloadFinishedEvent += DownloadCompleted;
                m.NotFoundDownloadErrorEvent += NotFoundDownloadError;
                m.DownloadErrorEvent += DownloadError;
                m.isCancelable = false;
                m.isObb = true;
                m.StartDownload(diffsToDo[0].download, CoreService.coreVars.QAVSTmpDowngradeDir + diffsToDo[0].diffFilename);
                downloadManagers.Add(m);
                diffsToDo.RemoveAt(0);
            }
            QAVSWebserver.BroadcastDownloads(false);
        }

        private void SetEntitlementError()
        {
            entitlementError = true;
            status = "This downgrade is not currently available. We're sorry for the inconvenience.";
        }

        public void Done()
        {
            int totalPatches = entry.otherFiles.Count + 1;
            int i = 1;
            status = "Download completed. Applying diff patches to game. Please wait up to 5 minutes (" + i + "/" + totalPatches + ")";
            downloadedBytes = totalBytes;
            UpdateManagersAndProgress();
            QAVSWebserver.BroadcastDownloads(true);
            string backupDir = CoreService.coreVars.QAVSBackupDir + this.packageName + "/" + this.backupName + "/";
            FileManager.RecreateDirectoryIfExisting(backupDir);
            // Get installed apk
            string appPath = AndroidService.FindAPKLocation(entry.appid);
            // apk
            ApplyPatch(appPath, CoreService.coreVars.QAVSTmpDowngradeDir + entry.diffFilename, backupDir + "app.apk");
            i++;
            // obbs
            foreach (FileDiffDowngradeEntry file in entry.otherFiles)
            {
                status = "Download completed. Applying diff patches to game. Please wait up to 5 minutes (" + i + "/" + totalPatches + ")";
                ApplyPatch(CoreService.coreVars.AndroidObbLocation + entry.appid + "/" + file.sourceFilename, CoreService.coreVars.QAVSTmpDowngradeDir + file.diffFilename, backupDir + "obb/" + entry.appid + "/" + file.outputFilename);
                i++;
            }
            BackupInfo info = BackupManager.GetBackupInfo(backupDir, true); // Populate info.json correctly
            RealDone();
        }
        
        public void ApplyPatch(string sourcePath, string diffPath, string outputPath)
        {
            using (FileStream sourceStream = File.OpenRead(sourcePath))
            {
                using (FileStream diffStream = File.OpenRead(diffPath))
                {
                    using (FileStream output = File.Create(outputPath))
                    {
                        VCDiff.Decoders.VcDecoder decoder = new VCDiff.Decoders.VcDecoder(sourceStream, diffStream, output);
                        long bytesWritten;
                        Logger.Log("Decoding diff file for " + sourcePath + " with " + diffPath + " to " + outputPath);
                        decoder.Decode(out bytesWritten);
                        Logger.Log("Wrote " + bytesWritten + " bytes to " + outputPath);
                        decoder.Dispose();
                    }
                }
            }
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