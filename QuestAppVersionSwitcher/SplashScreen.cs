using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Webkit;
using AndroidX.Activity.Result.Contract;
using AndroidX.AppCompat.App;
using ComputerUtils.Android;
using Java.Util.Logging;
using QuestAppVersionSwitcher.Core;
using Xamarin.Essentials;
using Handler = Android.OS.Handler;

namespace QuestAppVersionSwitcher
{
    [Activity(Theme = "@style/AppTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize, ScreenOrientation = ScreenOrientation.Landscape)]
    public class SplashScreen : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            ComputerUtils.Android.Logging.Logger.displayLogInConsole = true;
            ComputerUtils.Android.Logging.Logger.Log(this.GetType().Name);
            ComputerUtils.Android.Logging.Logger.Log(this.GetType().FullName);
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.splash);
            //this.RequestedOrientation = ScreenOrientation.Landscape;
            WebView webView = FindViewById<WebView>(Resource.Id.webView);
            webView.LoadUrl("file:///android_asset/html/splash.html");
            AndroidCore.context = this;
            CoreService.launcher = RegisterForActivityResult(
                new ActivityResultContracts.StartActivityForResult(), new ManageStoragePermissionCallback());
            Handler h = new Handler();
            h.PostDelayed(() =>
            {
                // Check permissions and request if needed
                if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
                {
                    if (Permissions.CheckStatusAsync<Permissions.StorageWrite>().Result != PermissionStatus.Granted)
                    {
                        if (Permissions.RequestAsync<Permissions.StorageWrite>().Result != PermissionStatus.Granted) return;
                    }
                    if (Permissions.CheckStatusAsync<Permissions.StorageRead>().Result != PermissionStatus.Granted)
                    {
                        if (Permissions.RequestAsync<Permissions.StorageRead>().Result != PermissionStatus.Granted) return;
                    }
                }
                else
                {
                    try
                    {
                        // Try creating a directory in /sdcard/ to check if we got permission to write there
                        if (Directory.Exists(CoreService.coreVars.QAVSPermTestDir)) Directory.Delete(CoreService.coreVars.QAVSPermTestDir, true);
                        Directory.CreateDirectory(CoreService.coreVars.QAVSPermTestDir);
                        Directory.Delete(CoreService.coreVars.QAVSPermTestDir, true);
                    }
                    catch (Exception e)
                    {
                        // Manage storage permission
                        Android.Net.Uri uri = Android.Net.Uri.Parse("package:com.ComputerElite.questappversionswitcher");
                        Intent i = new Intent(Settings.ActionManageAppAllFilesAccessPermission, uri);
                        CoreService.launcher.Launch(i);
                        return;
                    }
                }
                AndroidCore.context.StartActivity(typeof(MainActivity));
            },200);
        }
    }
}