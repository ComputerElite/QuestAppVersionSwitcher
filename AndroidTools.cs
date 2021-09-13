using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using System;
using System.Collections.Generic;
using Xamarin.Essentials;
using static Xamarin.Essentials.Permissions;

namespace QuestAppVersionSwitcher.Core
{
    public class AndroidService
    {
        public static List<App> GetInstalledApps()
        {
            List<App> inApps = new List<App>();
            IList<ApplicationInfo> apps = Android.App.Application.Context.PackageManager.GetInstalledApplications(PackageInfoFlags.MatchAll);
            for (int i = 0; i < apps.Count; i++)
            {
                inApps.Add(new App(apps[i].LoadLabel(Android.App.Application.Context.PackageManager), apps[i].PackageName));
            }
            return inApps;
        }

        public static string FindAPKLocation(string package)
        {
            try
            {
                ApplicationInfo applicationInfo = Android.App.Application.Context.PackageManager.GetApplicationInfo(package, PackageInfoFlags.MatchAll);
                return (applicationInfo != null) ? applicationInfo.SourceDir : null;
            }
            catch (PackageManager.NameNotFoundException)
            {
            }
            return null;
        }

        public static void InitiateUninstallPackage(string package)
        {
            Intent uninstallIntent = new Intent(Intent.ActionDelete, Android.Net.Uri.Parse("package:" + package));
            //uninstallIntent.AddFlags(ActivityFlags.NewTask);
            CoreService.context.StartActivity(uninstallIntent);
        }

        public static bool IsPackageInstalled(string package)
        {
            bool installed = false;
            foreach(App a in GetInstalledApps())
            {
                if (a.PackageName == package) { installed = true; break; }
            }
            return installed;
        }

        public static void InitiateInstallApk(string apkLocation)
        {
            Intent intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(FileProvider.GetUriForFile(CoreService.context, CoreService.context.PackageName + ".provider", new Java.IO.File(apkLocation)), "application/vnd.android.package-archive");
            //intent.SetFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);
            intent.SetFlags(ActivityFlags.GrantReadUriPermission);
            CoreService.context.StartActivity(intent);
        }
    }

    public class App
    {
        public string AppName { get; set; }
        public string PackageName { get; set; }

        public App(string appName, string packageName)
        {
            AppName = appName;
            PackageName = packageName;
        }
    }
}