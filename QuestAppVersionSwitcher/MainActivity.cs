﻿using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Webkit;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using ComputerUtils.Android;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using Google.Android.Material.Snackbar;
using QuestAppVersionSwitcher.Core;
using System.IO;
using Android.Content;
using Android.Database;
using Android.Provider;
using AndroidX.Activity.Result.Contract;
using ComputerUtils.Android.AndroidTools;
using ComputerUtils.Android.Webserver;
using AlertDialog = Android.App.AlertDialog;

namespace QuestAppVersionSwitcher
{
    [Activity(Theme = "@style/AppTheme", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : AppCompatActivity
    {
        public static int pickFileCode = 1;
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

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == pickFileCode && resultCode == Result.Ok)
            {
                // Get the URI of the selected file
                Android.Net.Uri uri = data.Data;
                Logger.Log(uri.ToString());

                // Convert the URI to a file path
                string path = GetRealPathFromURI(uri);

                // Start apk install
                Logger.Log("Selected apk for installation: " + path);
                AndroidService.InitiateInstallApk(path);
            }
        }
        
        private string GetRealPathFromURI(Android.Net.Uri uri)
        {
            return HttpServer.DecodeUrlString(uri.ToString()
                .Replace("content://com.android.externalstorage.documents/document/primary%3A", "/sdcard/"));
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}