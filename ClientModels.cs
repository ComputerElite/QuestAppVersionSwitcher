using System.Collections.Generic;

namespace QuestAppVersionSwitcher.ClientModels
{
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
}