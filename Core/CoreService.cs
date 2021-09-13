using Android;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Webkit;
using AndroidX.Core.App;
using ComputerUtils.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Xamarin.Essentials;

namespace QuestAppVersionSwitcher.Core
{
    public class CoreService
    {
        public static AssetManager assetManager = null;
        public static WebView browser = null;
        public static QPWebserver qPWebserver = new QPWebserver();
        public static CoreVars coreVars = new CoreVars();
        public static Version version = Assembly.GetExecutingAssembly().GetName().Version;
        public static Context context = null;
        public void Start()
        {
            // Check permissions and request if needed
            if (Permissions.CheckStatusAsync<Permissions.StorageWrite>().Result != PermissionStatus.Granted)
            {
                if (Permissions.RequestAsync<Permissions.StorageWrite>().Result != PermissionStatus.Granted) return;
            }
            if (Permissions.CheckStatusAsync<Permissions.StorageRead>().Result != PermissionStatus.Granted)
            {
                if (Permissions.RequestAsync<Permissions.StorageRead>().Result != PermissionStatus.Granted) return;
            }
            //Set webbrowser settings
            browser.SetWebChromeClient(new WebChromeClient());
            browser.Settings.JavaScriptEnabled = true;
            browser.Settings.AllowContentAccess = true;
            browser.Settings.CacheMode = CacheModes.Default;
            browser.Focusable = true;
            browser.Settings.MediaPlaybackRequiresUserGesture = false;
            browser.Settings.DomStorageEnabled = true;
            browser.Settings.DatabaseEnabled = true;
            browser.Settings.DatabasePath = "/data/data/" + browser.Context.PackageName + "/databases/";
            browser.Settings.LoadWithOverviewMode = true;
            browser.Settings.UseWideViewPort = true;

            // Create all directories and files
            if (!Directory.Exists(coreVars.QAVSDir)) Directory.CreateDirectory(coreVars.QAVSDir);
            if (!Directory.Exists(coreVars.QAVSBackupDir)) Directory.CreateDirectory(coreVars.QAVSBackupDir);
            if (File.Exists(coreVars.QAVSConfigLocation))
            {
                coreVars = JsonSerializer.Deserialize<CoreVars>(File.ReadAllText(coreVars.QAVSConfigLocation));
            } else
            {
                File.WriteAllText(coreVars.QAVSConfigLocation, JsonSerializer.Serialize(coreVars));
            }
            qPWebserver.Start();
        }
    }
}