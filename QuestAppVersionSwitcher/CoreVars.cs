using System.Collections.Generic;
using System.IO;
using System.Threading;
using ComputerUtils.Android.AndroidTools;
using Java.Lang;
using Newtonsoft.Json;
using QuestPatcher.QMod;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace QuestAppVersionSwitcher.Core
{
    public class StrippedConfig
    {
        public string currentApp { get; set; } = "";
        public string currentAppName { get; set; } = "";
        public int serverPort { get; set; } = 50002;

        public int wsPort
        {
            get
            {
                return serverPort + 1;
            }
        }
        public int loginStep { get; set; } = 0;

        public bool passwordSet
        {
            get
            {
                return QAVSWebserver.GetSHA256OfString(AndroidService.GetDeviceID()) != password;
            }
        }
        [JsonIgnore]
        public string password { get; set; } = "";

        public List<DownloadedApp> downloadedApps { get; set; } = new List<DownloadedApp>();
    }

    public class DownloadedApp
    {
        public string apkSHA256 { get; set; } = "";
        public string package { get; set; } = "";
        public string version { get; set; } = "";
        public string binaryId { get; set; } = "";
    }

    public class CoreVars : StrippedConfig // aka config
    {
        public string qavsVersion { get; set; } = "";
        public List<string> accessFolders { get; set; } = new List<string>();
        public string token { get; set; } = "";
        public PatchingPermissions patchingPermissions = new PatchingPermissions();
		public static Cosmetics cosmetics = new Cosmetics();
		public readonly string QAVSDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/";
        public readonly string QAVSPermTestDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher-permTest/";
        public readonly string QAVSTmpDowngradeDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/tmpDowngrade/";
        public readonly string QAVSTmpPatchingDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/tmpPatching/";
        public readonly string QAVSTmpModsDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/tmpMods/";
        public readonly string QAVSModsDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/Mods/";
        public readonly string QAVSModAssetsDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/ModAssets/";
        public readonly string QAVSPatchingFilesDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/patchingFiles/";
        public readonly string QAVSBackupDir = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/Backups/";
        public readonly string QAVSConfigLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/config.json";
        public readonly string QAVSUIConfigLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/QuestAppVersionSwitcher/uiconfig.json";
        public readonly string AndroidAppLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Android/data/";
        public readonly string AndroidObbLocation = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Android/obb/";
        public static string fileDir = "";
        public static string oculusLoginUrl = "https://auth.oculus.com/login/?redirect_uri=https%3A%2F%2Fsecure.oculus.com%2F";
        public static ReaderWriterLock locker = new ReaderWriterLock();
        
        public void Save()
        {
            try
            {
                // Aquire a writer lock to make sure no other thread is writing to the file
                locker.AcquireWriterLock(10000); //You might wanna change timeout value 
                File.WriteAllText(QAVSConfigLocation, JsonSerializer.Serialize(this));
            }
            finally
            {
                locker.ReleaseWriterLock();
                QAVSWebserver.BroadcastConfig();
            }
        }
    }

    public class PatchingPermissions
    {
        public bool externalStorage { get; set; } = true;
        public bool handTracking { get; set; } = true;
        public bool debug { get; set; } = true;
        public bool openXR { get; set; } = true;
        public ModLoader modloader { get; set; } = ModLoader.Scotland2;
        public List<string> otherPermissions { get; set; } = new List<string>();
        public List<UsesFeature> otherFeatures { get; set; } = new List<UsesFeature>();
        public HandTrackingVersion handTrackingVersion { get; set; }
    }

    public class UsesFeature
    {
        public string name { get; set; } = "";
        public bool required { get; set; } = true;
    }

    public enum HandTrackingVersion
    {
        Default,
        [Deprecated]
        V1,
        [Deprecated]
        V1HighFrequency,
        V2,
        V2_1
    }
}