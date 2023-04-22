using System.Collections.Generic;

namespace QuestAppVersionSwitcher.ClientModels
{
    public class PatchingStatus
    {
        public bool isPatched { get; set; } = false;
        public bool isInstalled { get; set; } = true;
        public bool canBePatched { get; set; } = true; // Not implemented yet.
        public string version { get; set; } = "";
        public string versionCode { get; set; } = "";
        public ModdedJson moddedJson { get; set; } = null;
    }

    public class DownloadStatus
    {
        public List<GameDownloadManager> gameDownloads { get; set; } = new List<GameDownloadManager>();
        public List<DownloadProgress> individualDownloads { get; set; } = new List<DownloadProgress>();
    }

    public class MessageAndValue<T>
    {
        public string msg { get; set; } = "";
        public T value { get; set; } = default(T);

        public MessageAndValue(string msg, T value)
        {
            this.msg = msg;
            this.value = value;
        }
    }

    public class ModdedJson
    {
        public string patcherName { get; set; } = "";
        public string patcherVersion { get; set; } = "0.0.0";
        public string modloaderName { get; set; } = "";
        public string modloaderVersion { get; set; } = "";
        public List<string> modifiedFiles { get; set; } = new List<string>();
    }

    public class About
    {
        public string version { get; set; } = "";
        public List<string> browserIPs { get; set; } = new List<string>();
    }

    public enum BackupInfoVersion
    {
        V1,
        V2,
        V3,
        V4
    }

    public class BackupList
    {
        public List<BackupInfo> backups { get; set; } = new List<BackupInfo>();
        public string lastRestored { get; set; } = "";
        public long backupsSize { get; set; } = 0;
        public string backupsSizeString { get; set; } = "";
    }

    public class TokenRequest
    {
        public string token { get; set; } = "";
        public string password { get; set; } = "";
    }

    public class DownloadRequest
    {
        public string binaryId { get; set; } = "";
        public string password { get; set; } = "";
        public string version { get; set; } = "";
        public string app { get; set; } = "";
        public string parentId { get; set; } = "";
        public bool isObb { get; set; } = false;
        public List<ObbEntry> obbList { get; set; } = new List<ObbEntry>();
        public string packageName { get; set; } = "";
    }

    public class ObbEntry
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
    }

    public class DownloadProgress
    {
        public string packageName { get; set; } = "";
        public string text { get; set; } = "";
        public string version { get; set; } = "";
        public double percentage { get; set; } = 0.0;
        public string percentageString { get; set; } = "";
        public long done { get; set; } = 0;
        public long total { get; set; } = 0;
        public long speed { get; set; } = 0;
        public long eTASeconds { get; set; } = 0;
        public string doneString { get; set; } = "0 Bytes";
        public string totalString { get; set; } = "0 Bytes";
        public string speedString { get; set; } = "0 Bytes/s";
        public string eTAString { get; set; } = "";
        public string name { get; set; } = "";
        public string backupName { get; set; } = "";
        public string textColor { get; set; } = "#EEEEEE";
        public bool isCancelable { get; set; } = true;
    }
}