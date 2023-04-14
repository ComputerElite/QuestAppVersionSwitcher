using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ComputerUtils.Android.Encryption;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
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
        
        public DownloadRequest request = null;
        public Thread updateThread = null;
        public bool canceled { get; set; } = false;
        public bool error { get; set; } = false;
        public bool done { get; set; } = false;

        public GameDownloadManager(DownloadRequest r)
        {
            request = r;
        }

        public void StartDownload()
        {
            id = DateTime.Now.Ticks.ToString();
            version = request.version;
            gameName = request.app;
            packageName = request.packageName;
            status = "Preparing download for " + gameName + " " + version;
            
            DownloadManager m = new DownloadManager();
            m.StartDownload(request.binaryId, request.password, request.version, request.app, request.parentId, false, request.packageName);;
            m.DownloadFinishedEvent += DownloadCompleted;
            m.DownloadErrorEvent += DownloadError;
            m.isCancelable = false;
            
            //Get OBBs via Oculus api
            try
            {
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
            status = gameName + " " + version;
            downloadManagers.Add(m);
            filesToDownload = 1 + obbsToDo.Count;
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
        }

        public void UpdateManagersAndProgress()
        {
            progress = filesDownloaded / (double)filesToDownload;
            progressString = (progress * 100).ToString("F") + "%";
            
            for (int i = 0; i < 10 - downloadManagers.Count; i++)
            {
                if (obbsToDo.Count <= 0) return;
                DownloadManager m = new DownloadManager();
                m.DownloadFinishedEvent += DownloadCompleted;
                m.DownloadErrorEvent += DownloadError;
                m.isCancelable = false;
                m.StartDownload(obbsToDo[0].id, request.password, request.version, request.app, request.parentId, true, request.packageName, obbsToDo[0].name);
                downloadManagers.Add(m);
                obbsToDo.RemoveAt(0);
            }
        }

        private void DownloadError(DownloadManager manager)
        {
            Cancel();
            error = true;
            canceled = false;
            status = "An unknown error occurred during the download of " + gameName + " " + version + ". Please try again.";
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
        }

        public void DownloadCompleted(DownloadManager m)
        {
            filesDownloaded++;
            downloadManagers.Remove(m);
            this.backupName = m.backupName;
            string backupDir = CoreService.coreVars.QAVSBackupDir + m.packageName + "/" + m.backupName + "/";
            if(m.isObb)
            {
                string obbDir = backupDir + "obb/" + m.packageName + "/";
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