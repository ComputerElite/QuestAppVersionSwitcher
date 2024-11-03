using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Activity.Result.Contract;
using ComputerUtils;
using ComputerUtils.Logging;
using QuestAppVersionSwitcher;
using QuestAppVersionSwitcher.Core;
using WebView = Android.Webkit.WebView;
using Resource = Microsoft.Maui.Resource;

namespace QuestAppVersionSwitcher;

[Activity(Theme = "@style/AppTheme",MainLauncher = false, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize, ScreenOrientation = ScreenOrientation.Landscape)]
public class MainActivity : MauiAppCompatActivity
{   
     WebView webView;
    protected override void OnCreate(Bundle savedInstanceState)
    {
        Logger.displayLogInConsole = true;
        Logger.Log("Code executing");
        Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
        base.OnCreate(savedInstanceState);
        // Set our view from the "main" layout resource
        SetContentView(Resource.Layout.activity_main);
        //this.RequestedOrientation = ScreenOrientation.Landscape;
        //Get webView WebView from Main Layout  
        webView = FindViewById<WebView>(Resource.Id.webView);
        CoreService.mainActivity = this;

        CoreVars.fileDir = "/sdcard/Android/data/com.ComputerElite.questappversionswitcher/files/";
        CoreService.browser = webView;
        AndroidCore.context = this;
        AndroidCore.activity = this;
        AndroidCore.assetManager = this.Assets;
        AndroidCore.installLauncher = RegisterForActivityResult(new ActivityResultContracts.StartActivityForResult(),
            new InstallLaucherResult());
        
        FolderPermission.l = AndroidCore.activity.RegisterForActivityResult(
            new ActivityResultContracts.StartActivityForResult(), new FolderPermissionCallback());
        
        
        CoreService.Start();
    }

    public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
    {
        // Check if the key event was the Back button and if there's history
        if ((keyCode == Keycode.Back) && webView.CanGoBack()) {
            webView.GoBack();
            return true;
        }
        // If it wasn't the Back key or there's no web page history, bubble up to the default
        // system behavior (probably exit the activity)
        return base.OnKeyDown(keyCode, e);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        ActivityResultCallbackRegistry.InvokeCallback(requestCode, resultCode, data);
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
    {
        //Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}