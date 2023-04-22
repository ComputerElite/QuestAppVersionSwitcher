using Android;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Webkit;
using AndroidX.Core.App;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.Mods;
using System;
using System.IO;
using System.Net.Security;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Android.Content.PM;
using Android.Media.Audiofx;
using Android.OS;
using Android.Provider;
using AndroidX.Activity.Result;
using Com.Xamarin.Formsviewgroup;
using ComputerUtils.Android;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin.Essentials;
using Environment = Android.OS.Environment;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Object = Java.Lang.Object;

namespace QuestAppVersionSwitcher.Core
{
    public class CoreService
    {
        public static WebView browser = null;
        public static QAVSWebserver qAVSWebserver = new QAVSWebserver();
        public static CoreVars coreVars = new CoreVars();
        public static Version version = Assembly.GetExecutingAssembly().GetName().Version;
        public static string ua = "Mozilla/5.0 (X11; Linux x86_64; Quest) AppleWebKit/537.36 (KHTML, like Gecko) OculusBrowser/23.2.0.4.49.401374055 SamsungBrowser/4.0 Chrome/104.0.5112.111 VR Safari/537.36";
        public static ActivityResultLauncher launcher;
        public static bool started = false;

        public static void Start()
        {
            //Set webbrowser settings
            browser.SetWebChromeClient(new WebChromeClient());
            browser.Settings.JavaScriptEnabled = true;
            browser.Settings.AllowContentAccess = true;
            browser.Settings.CacheMode = CacheModes.Default;
            browser.Focusable = true;
            browser.Settings.SetSupportZoom(false);
            browser.Settings.MediaPlaybackRequiresUserGesture = false;
            browser.Settings.DomStorageEnabled = true;
            browser.Settings.UserAgentString = ua;
            browser.Settings.DatabaseEnabled = true;
            browser.Settings.DatabasePath = "/data/data/" + browser.Context.PackageName + "/databases/";
            browser.Settings.LoadWithOverviewMode = true;
            browser.Settings.UseWideViewPort = true;
            browser.Settings.AllowFileAccess = true;
            browser.Settings.JavaScriptCanOpenWindowsAutomatically = true;
            browser.SetWebViewClient(new QAVSWebViewClient());
            browser.Settings.DefaultTextEncodingName = "utf-8";
            browser.AddJavascriptInterface(new QAVSJavascriptInterface(), "QAVSJavascript");
            browser.SetDownloadListener(new DownloadListener());
            CookieManager.Instance.SetAcceptThirdPartyCookies(browser, true);
            
            // Accept every ssl certificate, may be a security risk but it's the only way to get the mod list (CoPilot)
            Logger.displayLogInConsole = true;
            
            // Create all directories and files
            if (!started)
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSDir);
                FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSBackupDir);
                FileManager.RecreateDirectoryIfExisting(coreVars.QAVSTmpDowngradeDir);
                FileManager.RecreateDirectoryIfExisting(coreVars.QAVSTmpPatchingDir);
                FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSPatchingFilesDir);
                FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSModAssetsDir);
                FileManager.RecreateDirectoryIfExisting(coreVars.QAVSTmpModsDir);
                Logger.SetLogFile(coreVars.QAVSDir + "qavslog.log");
                ExternalFilesDownloader.DownloadUrl(
                    "https://raw.githubusercontent.com/Lauriethefish/QuestPatcher/main/QuestPatcher.Core/Resources/file-copy-paths.json",
                    coreVars.QAVSFileCopiesFile);
                if (!File.Exists(coreVars.QAVSConfigLocation))
                    File.WriteAllText(coreVars.QAVSConfigLocation, JsonSerializer.Serialize(coreVars));
                coreVars = JsonSerializer.Deserialize<CoreVars>(File.ReadAllText(coreVars.QAVSConfigLocation));
                if (!File.Exists(coreVars.QAVSUIConfigLocation))
                    File.WriteAllText(coreVars.QAVSUIConfigLocation, "{}");
                QAVSWebserver.uiConfig = JsonConvert.DeserializeObject(File.ReadAllText(coreVars.QAVSUIConfigLocation));
                QAVSModManager.Init();
                CoreVars.cosmetics = Cosmetics.LoadCosmetics();
            }

            qAVSWebserver.Start();
            started = true;
        }
    }

    public class ManageStoragePermissionCallback : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Java.Lang.Object result)
        {
            AndroidCore.context.StartActivity(typeof(SplashScreen));
        }
    }
}