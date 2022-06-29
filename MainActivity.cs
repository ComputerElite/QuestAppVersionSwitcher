using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Webkit;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using ComputerUtils.Android;
using ComputerUtils.Android.Logging;
using Google.Android.Material.Snackbar;
using QuestAppVersionSwitcher.Core;

namespace QuestAppVersionSwitcher
{
    [Activity(Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        WebView webView;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            //Get webView WebView from Main Layout  
            webView = FindViewById<WebView>(Resource.Id.webView);
            CoreService.browser = webView;
            AndroidCore.context = this;
            AndroidCore.assetManager = this.Assets;

            // Start all services
            CoreService core = new CoreService();
            core.Start();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}