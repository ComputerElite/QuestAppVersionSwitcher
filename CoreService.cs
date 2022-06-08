using Android;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Webkit;
using AndroidX.Core.App;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
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
        public static WebView browser = null;
        public static QAVSWebserver qAVSWebserver = new QAVSWebserver();
        public static CoreVars coreVars = new CoreVars();
        public static Version version = Assembly.GetExecutingAssembly().GetName().Version;
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
            browser.Settings.UserAgentString = "Mozilla/5.0 (X11; Linux x86_64; Quest) AppleWebKit/537.36 (KHTML, like Gecko) OculusBrowser/21.2.0.1.37.371181431 SamsungBrowser/4.0 Chrome/100.0.4896.160 VR Safari/537.36";
            browser.Settings.DatabaseEnabled = true;
            browser.Settings.DatabasePath = "/data/data/" + browser.Context.PackageName + "/databases/";
            browser.Settings.LoadWithOverviewMode = true;
            browser.Settings.UseWideViewPort = true;
            CookieManager.Instance.SetAcceptThirdPartyCookies(browser, true);

            // Create all directories and files
            FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSDir);
            FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSBackupDir);
            FileManager.RecreateDirectoryIfExisting(coreVars.QAVDTmpDowngradeDir);
            if (!File.Exists(coreVars.QAVSConfigLocation)) File.WriteAllText(coreVars.QAVSConfigLocation, JsonSerializer.Serialize(coreVars));
            coreVars = JsonSerializer.Deserialize<CoreVars>(File.ReadAllText(coreVars.QAVSConfigLocation));
            qAVSWebserver.Start();
        }
    }
}