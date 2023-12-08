using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ComputerUtils.Android.Encryption;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using ComputerUtils.Android.VarUtils;
using Newtonsoft.Json;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using Org.BouncyCastle.Bcpg.OpenPgp;
using QuestAppVersionSwitcher.ClientModels;
using QuestAppVersionSwitcher.Core;

namespace QuestAppVersionSwitcher
{
    public class GameDownloadManager
    {
        public virtual string packageName { get; set; } = "";
        public virtual string version { get; set; } = "";
        public virtual string gameName { get; set; } = "";
        public virtual long filesToDownload { get; set; } = 0;
        public virtual long filesDownloaded { get; set; } = 0;

        public virtual double progress
        {
            get
            {
                if(totalBytes == 0) return 0;
                return downloadedBytes / (double)totalBytes;
            }
        }

        public virtual string progressString
        {
            get
            {
                return String.Format("{0:0.#}", progress * 100) + "%";
            }
        }
        public virtual string id { get; set; } = "";
        public virtual string status { get; set; } = "";
        public virtual string textColor { get; set; } = "#FFFFFF";
        public virtual string backupName { get; set; } = "";

        public virtual long totalBytes { get; set; } = 0;
        public virtual long downloadedBytes { get; set; } = 0;
        public virtual long eTASeconds { get; set; } = 0;
        public virtual long speed { get; set; } = 0;

        public string speedString
        {
            get
            {
                return SizeConverter.ByteSizeToString(speed, 1) + "/s";
            }
        }

        public string eTAString
        {
            get
            {
                return SizeConverter.SecondsToBetterString(eTASeconds);
            }
        }

        public string downloadedBytesString
        {
            get
            {
                return SizeConverter.ByteSizeToString(downloadedBytes);
            }
        }
        public string totalBytesString
        {
            get
            {
                return SizeConverter.ByteSizeToString(totalBytes);
            }
        }

        private long downloadedFilesTotalBytes = 0;
        
        public virtual List<DownloadManager> downloadManagers { get; set; } = new List<DownloadManager>();
        public virtual List<ObbEntry> obbsToDo { get; set; } = new List<ObbEntry>();
        public virtual List<ObbEntry> allObbs { get; set; } = new List<ObbEntry>();
        [JsonIgnore]
        public DownloadRequest request = null;
        [JsonIgnore]
        public Thread updateThread = null;
        public virtual bool canceled { get; set; } = false;
        public virtual bool error { get; set; } = false;
        public virtual bool entitlementError { get; set; } = false;
        public virtual bool done { get; set; } = false;
        public virtual int maxConcurrentDownloads { get; set; } = 1;
        public virtual int maxConcurrentConnections { get; set; } = 10;

        public GameDownloadManager(DownloadRequest r)
        {
            request = r;
        }
        
        public GameDownloadManager()
        {
        }

        public bool HasEntitlementFor(string id)
        {
            try
            {
                Logger.Log("Requesting entitlements");
                ViewerData<OculusUserWrapper> user = GraphQLClient.GetActiveEntitelments();
                if(user == null || user.data == null || user.data.viewer == null || user.data.viewer.user == null || user.data.viewer.user.active_entitlements == null || user.data.viewer.user.active_entitlements.nodes == null)
                {
                    throw new Exception("Fetching of active entitlements failed");
                }
                List<Entitlement> userEntitlements = user.data.viewer.user.active_entitlements.nodes;

                if (userEntitlements.Count <= 0)
                {
                    Logger.Log("User has 0 entitlements, In doubt: return true");
                    return true;
                }
                foreach(Entitlement entitlement in userEntitlements)
                {
                    if(entitlement.item.id == id)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Logger.Log("In doubt: return true: " + e);
                return true;
            }
        }

        private DownloadManager apkDownloadManager;

        public void StartDownload()
        {
            id = DateTime.Now.Ticks.ToString();
            version = request.version;
            gameName = request.app;
            packageName = request.packageName;
            status = "Preparing download for " + gameName + " " + version;
            
            // Set token for requests
            GraphQLClient.retryTimes = 1;
            GraphQLClient.log = false;
            GraphQLClient.oculusStoreToken = PasswordEncryption.Decrypt(CoreService.coreVars.token, request.password);
            
            // Check entitlements
            /*
            if(!HasEntitlementFor(request.parentId))
            {
                Logger.Log("User has no entitlement for " + request.parentId);
                SetEntitlementError();
                return;
            }
            */
            
            try
            {
                //Get OBBs via Oculus api
                AndroidBinary b = GraphQLClient.GetBinaryDetails(request.binaryId).data.node;
                if (b.obb_binary != null)
                {
                    obbsToDo.Add(new ObbEntry
                    {
                        id = b.obb_binary.id,
                        name = b.obb_binary.file_name,
                        sizeNumerical = b.obb_binary.sizeNumerical
                    });
                }
                foreach (AssetFile assetFile in b.asset_files.nodes)
                {
                    if(!assetFile.is_required) continue;
                    obbsToDo.Add(new ObbEntry
                    {
                        id = assetFile.id,
                        name = assetFile.file_name,
                        sizeNumerical = assetFile.sizeNumerical
                    });
                }
            }
            catch (Exception e)
            {
                obbsToDo = request.obbList;
            }

            allObbs = new List<ObbEntry>(obbsToDo);
            
            this.backupName = gameName + " " + version + " Downgraded";
            foreach (char r in QAVSWebserver.ReservedChars)
            {
                this.backupName = this.backupName.Replace(r, '_');
            }
            status = gameName + " " + version;
            
            // apk download
            filesToDownload = 1 + obbsToDo.Count;
            UpdateMaxConnections();
            
            
            apkDownloadManager = new DownloadManager();
            apkDownloadManager.connections = maxConcurrentConnections;
            apkDownloadManager.StartDownload(request.binaryId, request.password, request.version, request.app, request.parentId, false, request.packageName);
            apkDownloadManager.DownloadFinishedEvent += DownloadCompleted;
            apkDownloadManager.NotFoundDownloadErrorEvent += NotFoundDownloadError;
            apkDownloadManager.DownloadErrorEvent += DownloadError;
            apkDownloadManager.isCancelable = false;
            downloadManagers.Add(apkDownloadManager);
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
            status = "The meta account you are currently signed in with does not own " + gameName + ".\nPlease log out and sign back in with the account that has purchased this title via the tools & options tab.";
        }

        public void Done()
        {
            status = "Download completed: " + gameName + " " + version;
            textColor = "#00FF00";
            done = true;
            UpdateManagersAndProgress();
            QAVSWebserver.BroadcastDownloads(true);
            string backupDir = CoreService.coreVars.QAVSBackupDir + this.packageName + "/" + this.backupName + "/";
            BackupInfo info = BackupManager.GetBackupInfo(backupDir, true); // Populate info.json correctly
            CoreService.coreVars.Save();
        }

        private long lastBytes = 0;
        private List<long> lastBytesPerSec = new List<long>();
        private DateTime lastUpdate = DateTime.Now;

        public void UpdateManagersAndProgress()
        {
            UpdateMaxConnections();
            totalBytes = apkDownloadManager.total + allObbs.Select(x => x.sizeNumerical).Sum();
            downloadedBytes = downloadedFilesTotalBytes + apkDownloadManager.done + downloadManagers.Where(x => x.isObb).Select(x => x.done).Sum();
            
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
                if (obbsToDo.Count <= 0) return;
                DownloadManager m = new DownloadManager();
                m.connections = maxConcurrentConnections;
                m.DownloadFinishedEvent += DownloadCompleted;
                m.NotFoundDownloadErrorEvent += NotFoundDownloadError;
                m.DownloadErrorEvent += DownloadError;
                m.isCancelable = false;
                m.StartDownload(obbsToDo[0].id, request.password, request.version, request.app, request.parentId, true, request.packageName, obbsToDo[0].name);
                downloadManagers.Add(m);
                obbsToDo.RemoveAt(0);
            }
            QAVSWebserver.BroadcastDownloads(false);
        }

        private void UpdateMaxConnections()
        {
            maxConcurrentDownloads = filesToDownload - filesDownloaded > 1 ? 2 : 1;
            maxConcurrentConnections = 10 / maxConcurrentDownloads; // Oculus only supports 10 connections per IP, 
        }

        private void DownloadError(DownloadManager manager)
        {
            Cancel();
            error = true;
            canceled = false;
            status = "An unknown error occurred during the download of " + gameName + " " + version + ". Please try again.";
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
            obbsToDo.Clear();
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
            downloadedFilesTotalBytes += m.total;
            downloadManagers.Remove(m);
            string backupDir = CoreService.coreVars.QAVSBackupDir + this.packageName + "/" + this.backupName + "/";
            if(m.isObb)
            {
                string obbDir = backupDir + "obb/" + this.packageName + "/";
                FileManager.CreateDirectoryIfNotExisting(obbDir);
                FileManager.DeleteFileIfExisting(obbDir + m.obbFileName);
                File.Move(m.tmpPath, obbDir + m.obbFileName);
                Logger.Log("Moved obb");
                return;
            }
            // Is apk
            FileManager.CreateDirectoryIfNotExisting(backupDir);
            FileManager.DeleteFileIfExisting(backupDir + "app.apk");
            File.Move(m.tmpPath, backupDir + "app.apk");
            Logger.Log("Moved apk");
        }
    }
}