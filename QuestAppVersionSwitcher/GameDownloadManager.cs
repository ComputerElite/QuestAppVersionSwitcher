using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
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
        
        public DownloadRequest request { get; set; } = null;
        public Thread updateThread = null;
        public bool canceled { get; set; } = false;

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
            
            DownloadManager m = new DownloadManager();
            m.StartDownload(request.binaryId, request.password, request.version, request.app, request.parentId, false, request.packageName);;
            m.DownloadFinishedEvent += DownloadCompleted;
            m.isCancelable = false;
            
            this.backupName = gameName + " " + version + " Downgraded";
            status = gameName + " " + version;
            downloadManagers.Add(m);
            QAVSWebserver.managers.Add(m);
            obbsToDo = request.obbList;
            filesToDownload = 1 + request.obbList.Count;
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
            status = "Done, restore " + backupName + " to install the downgraded game";
            textColor = "#00FF00";
        }

        public void UpdateManagersAndProgress()
        {
            for(int i = 0; i < downloadManagers.Count; i++)
            {
                DownloadManager m = downloadManagers[i];
                if(m.canceled)
                {
                    downloadManagers.RemoveAt(i);
                    QAVSWebserver.managers.Remove(m);
                    i--;
                }
            }

            progress = filesDownloaded / (double)filesToDownload;
            progressString = (progress * 100).ToString("F") + "%";
            
            for (int i = 0; i < 30 - downloadManagers.Count; i++)
            {
                if (obbsToDo.Count <= 0) return;
                DownloadManager m = new DownloadManager();
                m.StartDownload(obbsToDo[0].id, request.password, request.version, request.app, request.parentId, true, request.packageName, obbsToDo[0].name);
                m.DownloadFinishedEvent += DownloadCompleted;
                m.isCancelable = false;
                downloadManagers.Add(m);
                QAVSWebserver.managers.Add(m);
                obbsToDo.RemoveAt(0);
            }
        }

        public void Cancel()
        {
            textColor = "#FF0000";
            obbsToDo.Clear();
            canceled = true;
            foreach (DownloadManager d in downloadManagers)
            {
                d.StopDownload();
                QAVSWebserver.managers.Remove(d);
            }
        }

        public void DownloadCompleted(DownloadManager m)
        {
            filesDownloaded++;
            QAVSWebserver.managers.Remove(m);
            downloadManagers.Remove(m);
            if(m.isObb)
            {
                string bbackupDir = CoreService.coreVars.QAVSBackupDir + m.packageName + "/" + m.backupName + "/obb/";
                FileManager.CreateDirectoryIfNotExisting(bbackupDir);
                FileManager.DeleteFileIfExisting(bbackupDir + m.obbFileName);
                File.Move(m.tmpPath, bbackupDir + m.obbFileName);
                Logger.Log("Moved obb");
                return;
            }
            // Is apk
            string packageName = QAVSWebserver.GetAPKPackageName(m.tmpPath);
            string backupDir = CoreService.coreVars.QAVSBackupDir + packageName + "/" + m.backupName + "/";
            FileManager.RecreateDirectoryIfExisting(backupDir);
            File.Move(m.tmpPath, backupDir + "app.apk");
        }
    }
}