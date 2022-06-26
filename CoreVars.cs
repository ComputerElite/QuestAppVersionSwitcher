using System.IO;
using System.Text.Json;

namespace QuestAppVersionSwitcher.Core
{
    public class CoreVars // aka config
    {
        public string currentApp { get; set; } = "";
        public int serverPort { get; set; } = 50002;
        public string token { get; set; } = "";
        public bool openLoginInstantly { get; set; } = false;
        public string password { get; set; } = "";
        public readonly string QAVSDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/";
        public readonly string QAVDTmpDowngradeDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/tmpDowngrade/";
        public readonly string QAVSBackupDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/Backups/";
        public readonly string QAVSConfigLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/config.json";
        public readonly string AndroidAppLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Android/data/";
        public readonly string AndroidObbLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Android/obb/";
        public static string oculusLoginUrl = "https://auth.oculus.com/login/?redirect_uri=https%3A%2F%2Fsecure.oculus.com%2F";
        public void Save()
        {
            File.WriteAllText(QAVSConfigLocation, JsonSerializer.Serialize(this));
        }
    }
}