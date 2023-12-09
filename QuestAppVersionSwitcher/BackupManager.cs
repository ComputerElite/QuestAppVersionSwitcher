using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using ComputerUtils.Android.VarUtils;
using QuestAppVersionSwitcher.ClientModels;
using QuestAppVersionSwitcher.Core;
using Exception = Java.Lang.Exception;

namespace QuestAppVersionSwitcher
{
    public class BackupManager
    {
        public static List<string> calculating = new List<string>();
        public static SHA256 hasher = SHA256.Create();
        public static BackupInfo GetBackupInfo(string path, bool loadAnyway = false)
        {
            BackupInfo info = new BackupInfo();
            string pathWithoutSlash = GetPathWithoutSlash(path);
            FileManager.CreateDirectoryIfNotExisting(pathWithoutSlash);
            if (File.Exists(pathWithoutSlash + "/info.json") && !loadAnyway)
            {
                info = JsonSerializer.Deserialize<BackupInfo>(File.ReadAllText(pathWithoutSlash + "/info.json"));
                if (info.BackupInfoVersion < BackupInfoVersion.V6) return GetBackupInfo(path, true);
                return info;
            }

            info.backupName = Path.GetFileName(pathWithoutSlash);
            info.containsAppData = Directory.Exists(pathWithoutSlash + "/" + Directory.GetParent(pathWithoutSlash).Name);
            info.containsObbs = Directory.Exists(pathWithoutSlash + "/obb/" + Directory.GetParent(pathWithoutSlash).Name);
            info.backupLocation = path;
            info.backupSize = FileManager.GetDirSize(pathWithoutSlash);
            info.backupSizeString = SizeConverter.ByteSizeToString(info.backupSize);
            info.containsApk = File.Exists(pathWithoutSlash + "/app.apk");
            if (info.containsApk)
            {
                try
                {
                    ZipArchive apk = ZipFile.OpenRead(pathWithoutSlash + "/app.apk");
                    PatchingStatus s = PatchingManager.GetPatchingStatus(apk);
                    info.gameVersion = s.version;
                    info.isPatchedApk = s.isPatched;
                    info.moddedJson = PatchingManager.GetModdedJson(apk);
                    apk.Dispose();
                }
                catch (InvalidDataException e)
                {
                    info.isCorrupted = true;
                }
                // Calculate SHA 256 of apk file
                /*
                if (!calculating.Contains(pathWithoutSlash + "/app.apk"))
                {
                    Logger.Log("Calculating SHA256 of apk file " + pathWithoutSlash + "/app.apk");
                    calculating.Add(pathWithoutSlash + "/app.apk");
                    FileStream fs = File.OpenRead(pathWithoutSlash + "/app.apk");
                    info.apkSHA256 = BitConverter.ToString(hasher.ComputeHash(fs)).Replace("-", "");
                    fs.Dispose();
                    Logger.Log("Calculated SHA256 of apk file " + pathWithoutSlash + "/app.apk");
                }
                */
            }
            File.WriteAllText(pathWithoutSlash + "/info.json", JsonSerializer.Serialize(info));
            return info;
        }

		public static BackupList GetBackups(string package)
        {
            string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/";
            BackupList backups = new BackupList();
            foreach (string d in Directory.GetDirectories(backupDir))
            {
                backups.backups.Add(GetBackupInfo(d));
                backups.backupsSize += backups.backups.Last().backupSize;
            }
            backups.backupsSizeString = SizeConverter.ByteSizeToString(backups.backupsSize);
            return backups;
        }

        public static string GetPathWithoutSlash(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar)
                ? path.Substring(0, path.Length - 1)
                : path;
        }
    }
    
    public class BackupInfo
    {
        public bool isCorrupted { get; set; } = false;
        public BackupInfoVersion BackupInfoVersion { get; set; } = BackupInfoVersion.V6;
        public string backupName { get; set; } = "";
        public string backupLocation { get; set; } = "";
        public bool containsAppData { get; set; } = false;
        public bool containsObbs { get; set; } = false;
        public bool isPatchedApk { get; set; } = false;
        public bool containsApk { get; set; } = false;
        public string gameVersion { get; set; } = "unknown";
        public long backupSize { get; set; } = 0;
        public string backupSizeString { get; set; } = "";
        public string apkSHA256 { get; set; } = "";
        public ModdedJson moddedJson { get; set; } = null;

        public bool isDownloadedFromOculus
        {
            get
            {
                return backupName.EndsWith("_Downgraded");
            }
        }
    }
    
    public enum BackupInfoVersion
    {
        V1,
        V2,
        V3,
        V4,
        V5,
        V6
    }

    public class BackupList
    {
        public List<BackupInfo> backups { get; set; } = new List<BackupInfo>();
        public long backupsSize { get; set; } = 0;
        public string backupsSizeString { get; set; } = "";
    }
}