using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace QuestAppVersionSwitcher.Core
{
    public class CoreVars // aka config
    {
        public string currentApp { get; set; } = "";
        public int serverPort { get; set; } = 50002;
        public string token { get; set; } = "";
        public int loginStep { get; set; } = 0;
        public string password { get; set; } = "";
        public PatchingPermissions patchingPermissions = new PatchingPermissions();
        public readonly string QAVSDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/";
        public readonly string QAVSTmpDowngradeDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/tmpDowngrade/";
        public readonly string QAVSTmpPatchingDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/tmpPatching/";
        public readonly string QAVSTmpModsDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/tmpMods/";
        public readonly string QAVSModsDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/Mods/";
        public readonly string QAVSModAssetsDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/ModAssets/";
        public readonly string QAVSFileCopiesFile = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/ModAssets/file-copies.json";
        public readonly string QAVSPatchingFilesDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/patchingFiles/";
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

    public class PatchingPermissions
    {
        public bool externalStorage { get; set; } = true;
        public bool handTracking { get; set; } = true;
        public bool debug { get; set; } = true;
        public List<string> otherPermissions { get; set; } = new List<string>();
        public HandTrackingVersion handTrackingVersion { get; set; }
    }

    public enum HandTrackingVersion
    {
        None,
        V1,
        V1HighFrequency,
        V2
    }
}