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
        public static string ua = "Mozilla/5.0 (X11; Linux x86_64; Quest) AppleWebKit/537.36 (KHTML, like Gecko) OculusBrowser/23.2.0.4.49.401374055 SamsungBrowser/4.0 Chrome/104.0.5112.111 VR Safari/537.36";
        public async void Start()
        {
			// Accept every ssl certificate, may be a security risk but it's the only way to get the mod list (CoPilot)
			ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
			// Check permissions and request if needed
			if (await Permissions.RequestAsync<Permissions.StorageWrite>() != PermissionStatus.Granted)
            {
                if (await Permissions.RequestAsync<Permissions.StorageWrite>() != PermissionStatus.Granted) return;
            }
            if (await Permissions.CheckStatusAsync<Permissions.StorageRead>() != PermissionStatus.Granted)
            {
                if (await Permissions.RequestAsync<Permissions.StorageRead>() != PermissionStatus.Granted) return;
            }

            //Set webbrowser settings
            browser.SetWebChromeClient(new WebChromeClient());
            browser.Settings.JavaScriptEnabled = true;
            browser.Settings.AllowContentAccess = true;
            browser.Settings.CacheMode = CacheModes.Default;
            browser.Focusable = true;
            browser.Settings.MediaPlaybackRequiresUserGesture = false;
            browser.Settings.DomStorageEnabled = true;
            browser.Settings.UserAgentString = ua;
            browser.Settings.DatabaseEnabled = true;
            browser.Settings.DatabasePath = "/data/data/" + browser.Context.PackageName + "/databases/";
            browser.Settings.LoadWithOverviewMode = true;
            browser.Settings.UseWideViewPort = true;
            browser.SetDownloadListener(new DownloadListener());
            CookieManager.Instance.SetAcceptThirdPartyCookies(browser, true);

            // Create all directories and files
            FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSDir);
            FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSBackupDir);
            FileManager.RecreateDirectoryIfExisting(coreVars.QAVSTmpDowngradeDir);
            FileManager.RecreateDirectoryIfExisting(coreVars.QAVSTmpPatchingDir);
            FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSPatchingFilesDir);
            FileManager.CreateDirectoryIfNotExisting(coreVars.QAVSModAssetsDir);
            FileManager.RecreateDirectoryIfExisting(coreVars.QAVSTmpModsDir);

            // Download file copies file
            ExternalFilesDownloader.DownloadUrl("https://raw.githubusercontent.com/Lauriethefish/QuestPatcher/main/QuestPatcher.Core/Resources/file-copy-paths.json", coreVars.QAVSFileCopiesFile);
            if (!File.Exists(coreVars.QAVSConfigLocation)) File.WriteAllText(coreVars.QAVSConfigLocation, JsonSerializer.Serialize(coreVars));
            coreVars = JsonSerializer.Deserialize<CoreVars>(File.ReadAllText(coreVars.QAVSConfigLocation));
            QAVSModManager.Init();
            qAVSWebserver.Start();
        }
    }
}