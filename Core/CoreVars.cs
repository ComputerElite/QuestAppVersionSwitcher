using System.IO;
using System.Text.Json;

namespace QuestAppVersionSwitcher.Core
{
    public class CoreVars // aka config
    {
        public string currentApp { get; set; } = "";
        public readonly string QAVSDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/";
        public readonly string QAVSBackupDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/Backups/";
        public readonly string QAVSConfigLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/config.json";
        public readonly string AndroidAppLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Android/data/";
        public readonly string AndroidObbLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Android/obb/";
        public void Save()
        {
            File.WriteAllText(QAVSConfigLocation, JsonSerializer.Serialize(this));
        }
    }
}