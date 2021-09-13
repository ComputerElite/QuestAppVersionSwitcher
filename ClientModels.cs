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

        public AppBackup(string name, bool gamedata, string location)
        {
            this.backupName = name;
            this.containsGamedata = gamedata;
            this.backupLocation = location;
        }
    }

    public class BackupList
    {
        public List<AppBackup> backups { get; set; } = new List<AppBackup>();
        public string lastRestored { get; set; } = "";
    }
}