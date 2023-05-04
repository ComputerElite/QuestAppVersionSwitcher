using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ComputerUtils.Android.Encryption;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
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
        public string packageName { get; set; } = "";
        public string version { get; set; } = "";
        public string gameName { get; set; } = "";
        public long filesToDownload { get; set; } = 0;
        public long filesDownloaded { get; set; } = 0;
        public double progress { get; set; }
        public string progressString { get; set; }
        public string id { get; set; } = "";
        public string status { get; set; } = "";
        public string textColor { get; set; } = "#FFFFFF";
        public string backupName { get; set; } = "";
        
        public List<DownloadManager> downloadManagers { get; set; } = new List<DownloadManager>();
        public List<ObbEntry> obbsToDo { get; set; } = new List<ObbEntry>();
        [JsonIgnore]
        public DownloadRequest request = null;
        [JsonIgnore]
        public Thread updateThread = null;
        public bool canceled { get; set; } = false;
        public bool error { get; set; } = false;
        public bool entitlementError { get; set; } = false;
        public bool done { get; set; } = false;
        public int maxConcurrentDownloads { get; set; } = 1;
        public int maxConcurrentConnections { get; set; } = 10;

        public GameDownloadManager(DownloadRequest r)
        {
            request = r;
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

        public void StartDownload()
        {
            id = DateTime.Now.Ticks.ToString();
            version = request.version;
            gameName = request.app;
            packageName = request.packageName;
            status = "Preparing download for " + gameName + " " + version;
            
            // Check entitlements
            if(!HasEntitlementFor(request.parentId))
            {
                Logger.Log("User has no entitlement for " + request.parentId);
                entitlementError = true;
                status = "The meta account you are currently signed in with does not own " + gameName + ".\nPlease log out and sign back in with the account that has purchased this title via the tools & options tab.";
                return;
            }
            
            try
            {
                //Get OBBs via Oculus api
                GraphQLClient.retryTimes = 1;
                GraphQLClient.log = false;
                GraphQLClient.oculusStoreToken = PasswordEncryption.Decrypt(CoreService.coreVars.token, request.password);
                AndroidBinary b = GraphQLClient.GetBinaryDetails(request.binaryId).data.node;
                if (b.obb_binary != null)
                {
                    obbsToDo.Add(new ObbEntry
                    {
                        id = b.obb_binary.id,
                        name = b.obb_binary.file_name
                    });
                }
                foreach (AssetFile assetFile in b.asset_files.nodes)
                {
                    if(!assetFile.is_required) continue;
                    obbsToDo.Add(new ObbEntry
                    {
                        id = assetFile.id,
                        name = assetFile.file_name
                    });
                }
            }
            catch (Exception e)
            {
                obbsToDo = request.obbList;
            }
            
            this.backupName = gameName + " " + version + " Downgraded";
            foreach (char r in QAVSWebserver.ReservedChars)
            {
                this.backupName = this.backupName.Replace(r, '_');
            }
            status = gameName + " " + version;
            
            // apk download
            filesToDownload = 1 + obbsToDo.Count;
            UpdateMaxConnections();
            
            
            DownloadManager m = new DownloadManager();
            m.connections = maxConcurrentConnections;
            m.StartDownload(request.binaryId, request.password, request.version, request.app, request.parentId, false, request.packageName);
            m.DownloadFinishedEvent += DownloadCompleted;
            m.DownloadErrorEvent += DownloadError;
            m.isCancelable = false;
            downloadManagers.Add(m);
            updateThread = new Thread(() =>
            {
                while (filesDownloaded < filesToDownload)
                {
                    if (canceled) return;
                    UpdateManagersAndProgress();
                    Thread.Sleep(1000);
                }

                Done();
            });
            updateThread.Start();
        }

        public void Done()
        {
            status = "Download completed: " + gameName + " " + version;
            textColor = "#00FF00";
            done = true;
            UpdateManagersAndProgress();
            QAVSWebserver.BroadcastDownloads(true);
            string backupDir = CoreService.coreVars.QAVSBackupDir + this.packageName + "/" + this.backupName + "/";
            QAVSWebserver.GetBackupInfo(backupDir, true); // Populate info.json correctly
        }

        public void UpdateManagersAndProgress()
        {
            progress = filesDownloaded / (double)filesToDownload;
            progressString = (progress * 100).ToString("F") + "%";

            UpdateMaxConnections();
            
            for (int i = 0; i < maxConcurrentDownloads - downloadManagers.Count; i++)
            {
                if (obbsToDo.Count <= 0) return;
                DownloadManager m = new DownloadManager();
                m.connections = maxConcurrentConnections;
                m.DownloadFinishedEvent += DownloadCompleted;
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