﻿using System.Collections.Generic;

namespace QuestAppVersionSwitcher.ClientModels
{
    public class PatchingStatus
    {
        public bool isPatched { get; set; } = false;
        public bool canBePatched { get; set; } = true; // Not implemented yet.
        public string version { get; set; } = "";
        public string versionCode { get; set; } = "";
        public ModdedJson moddedJson { get; set; } = null;
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

    public class AppBackup
    {
        public string backupName { get; set; } = "";
        public string backupLocation { get; set; } = "";
        public bool containsGamedata { get; set; } = false;
        public long backupSize { get; set; } = 0;
        public string backupSizeString { get; set; } = "";

        public AppBackup(string name, bool gamedata, string location, long size, string sizestr)
        {
            this.backupName = name;
            this.containsGamedata = gamedata;
            this.backupLocation = location;
            this.backupSize = size;
            this.backupSizeString = sizestr;
        }
    }

    public class BackupList
    {
        public List<AppBackup> backups { get; set; } = new List<AppBackup>();
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
        public string packageName { get; set; } = "";
    }

    public class DownloadProgress
    {
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
    }
}