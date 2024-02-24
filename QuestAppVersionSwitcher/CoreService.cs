using Android.Webkit;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.Mods;
using System;
using System.Net.Security;
using System.Net;
using System.Reflection;
using System.Text;
using Android.OS;
using Android.Systems;
using Android.Views;
using AndroidX.Activity.Result;
using ComputerUtils.Android;
using ComputerUtils.Android.AndroidTools;
using ComputerUtils.Android.Encryption;
using Java.IO;
using Newtonsoft.Json;
using OculusGraphQLApiLib;
using File = System.IO.File;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Object = Java.Lang.Object;

namespace QuestAppVersionSwitcher.Core
{
    public class CoreService
    {
        public static MainActivity mainActivity;
        public static WebView browser = null;
        public static QAVSWebserver qAVSWebserver = new QAVSWebserver();
        public static CoreVars coreVars = new CoreVars();
        public static Version version = Assembly.GetExecutingAssembly().GetName().Version;
        //public static string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36";
        public static string ua = "Mozilla/5.0 (X11; Linux x86_64; Quest) AppleWebKit/537.36 (KHTML, like Gecko) OculusBrowser/23.2.0.4.49.401374055 SamsungBrowser/4.0 Chrome/104.0.5112.111 VR Safari/537.36";
        public static ActivityResultLauncher launcher;
        public static bool started = false;

        public static void Start()
        {
            browser.SetWebChromeClient(new QAVSWebChromeClient(mainActivity));
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
            browser.OverScrollMode = OverScrollMode.Never;
            browser.Settings.AllowFileAccess = true;
            browser.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
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
                // Check if token is not FRL token, if so, reset it
                string token = PasswordEncryption.Decrypt(CoreService.coreVars.token, AndroidService.GetDeviceID());
                if (TokenTools.IsUserTokenValid(token))
                {
                    Logger.Log("Reset user token as it is not valid");
                    CoreService.coreVars.token = "";
                    CoreService.coreVars.Save();
                }
                
                
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSDir);
                File.WriteAllText(coreVars.QAVSDir + ".nomedia", "");
                Logger.SetLogFile(coreVars.QAVSDir + "qavslog.log");
                Logger.Log("\n\n\nQAVS Version: " + version + " starting up...\n\n\n");
                Logger.Log(Android.OS.Build.VERSION.Incremental);
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSBackupDir);
                FileManager.RecreateDirectoryIfExisting(coreVars.QAVSTmpDowngradeDir);
                FileManager.RecreateDirectoryIfExisting(coreVars.QAVSTmpPatchingDir);
                FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSPatchingFilesDir);
                FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSModAssetsDir);
                FileManager.RecreateDirectoryIfExisting(coreVars.QAVSTmpModsDir);
                Logger.Log("Device: " + Build.Device);
                if (!File.Exists(coreVars.QAVSConfigLocation))
                    File.WriteAllText(coreVars.QAVSConfigLocation, JsonSerializer.Serialize(coreVars));
                coreVars = JsonSerializer.Deserialize<CoreVars>(File.ReadAllText(coreVars.QAVSConfigLocation));
                coreVars.accessFolders.Clear();
                coreVars.qavsVersion = version.ToString();
                coreVars.Save();
                if (!File.Exists(coreVars.QAVSUIConfigLocation))
                    File.WriteAllText(coreVars.QAVSUIConfigLocation, "{}");
                QAVSWebserver.uiConfig = JsonConvert.DeserializeObject(File.ReadAllText(coreVars.QAVSUIConfigLocation));
                QAVSModManager.Init();
                CoreVars.cosmetics = Cosmetics.LoadCosmetics();
                
                // Load downgrade json
                Logger.Log("Loading Cosmetics from https://raw.githubusercontent.com/ComputerElite/QuestAppVersionSwitcher/main/Assets/downgrade.json");
                string downgrade = "{}";
                string jsonLoc = coreVars.QAVSDir + "downgrade.json";
                try
                {
                    downgrade = ExternalFilesDownloader.DownloadStringWithTimeout("https://raw.githubusercontent.com/ComputerElite/QuestAppVersionSwitcher/main/Assets/downgrade.json", 5000);
                    File.WriteAllText(jsonLoc, downgrade);
                    Logger.Log("Caching downgrade");
                } catch
                {
                    Logger.Log("Request failed, falling back to cache if existing");
                    if (File.Exists(jsonLoc)) downgrade = File.ReadAllText(jsonLoc);
                }
                coreVars.onlineDowngradeJson = new OnlineDowngradeJson();
                Logger.Log("Deserializing");
                try
                {
                    coreVars.onlineDowngradeJson = JsonSerializer.Deserialize<OnlineDowngradeJson>(downgrade);
                    Logger.Log("Deserialized successfully! UseDiffDowngrade: " + coreVars.onlineDowngradeJson.useDiffDowngrade);
                } catch(Exception e)
                {
                    Logger.Log("Error deserializing downgrade.json:\n" + e.ToString());
                }
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