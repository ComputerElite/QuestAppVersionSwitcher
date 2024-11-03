using System;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using ComputerUtils.Logging;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Android.Provider;
using Application = Android.App.Application;
using Process = System.Diagnostics.Process;
using String = System.String;

namespace ComputerUtils.AndroidTools
{
    public class AssetTools
    {
        public static byte[] GetAssetBytes(string assetName)
        {
            MemoryStream ms = new MemoryStream();
            AndroidCore.assetManager.Open(assetName).CopyTo(ms);
            return ms.ToArray();
        }

        public static string GetAssetString(string assetName)
        {
            return new StreamReader(AndroidCore.assetManager.Open(assetName)).ReadToEnd();
        }

        public static bool DoesAssetExist(string assetName)
        {
            //GetAllFiles("").ForEach(e => Logger.Log("\"" + e + "\" == \"" + assetName + "\" = " + (e == assetName).ToString(), LoggingType.Debug));
            return GetAllFiles("").Contains(assetName);
        }

        public static List<string> GetAllFiles(string folder)
        {
            List<string> files = new List<string>();
            if (!folder.EndsWith("/")) folder += "/";
            if (folder == "/") folder = "";
            foreach (string s in AndroidCore.assetManager.List(folder))
            {
                files.Add(folder + s);
                foreach (string ss in GetAllFiles(folder + s)) files.Add(ss);
            }
            return files;
        }

        public static List<string> GetAssetFolderFileList(string assetFolder)
        {
            return new List<string>(AndroidCore.assetManager.List(assetFolder));
        }
    }

    public class AndroidApp
    {
        public string PackageName { get; set; }
        public string AppName { get; set; }
        
        public AndroidApp(string appName, string packageName)
        {
            PackageName = packageName;
            AppName = appName;
        }
    }

    public class AndroidService
    {
        public static List<AndroidApp> GetInstalledApps(bool includeSystemApps = false)
        {
            List<AndroidApp> inApps = new List<AndroidApp>();
            IList<ApplicationInfo> apps = Application.Context.PackageManager.GetInstalledApplications(PackageInfoFlags.MatchAll);
            for (int i = 0; i < apps.Count; i++)
            {
                ApplicationInfo info = apps[i];
                if(info.PackageName == null || info.PackageName == Application.Context.PackageName || 
                   (!includeSystemApps && ((info.Flags & ApplicationInfoFlags.System) != 0 ||
                       info.PackageName.StartsWith("com.android") ||
                       info.PackageName.StartsWith("com.google") ||
                       info.PackageName.StartsWith("android")))) continue;
                inApps.Add(new AndroidApp(apps[i].LoadLabel(Application.Context.PackageManager), apps[i].PackageName));
            }
            return inApps;
        }
        
        public static string GetAppname(string packageName)
        {
            IList<ApplicationInfo> apps = Application.Context.PackageManager.GetInstalledApplications(PackageInfoFlags.MatchAll);
            for (int i = 0; i < apps.Count; i++)
            {
                ApplicationInfo info = apps[i];
                if(info.PackageName == null || info.PackageName == Application.Context.PackageName) continue;
                if(info.PackageName == packageName) return info.LoadLabel(Application.Context.PackageManager);
            }

            return packageName;
        }
        
		public static void LaunchApp(string packageName)
        {
            Intent intent = Application.Context.PackageManager.GetLaunchIntentForPackage(packageName);
            if(intent != null)
            {
				intent.SetFlags(ActivityFlags.NewTask |
                                ActivityFlags.NewDocument |
                                ActivityFlags.MultipleTask);
                Thread t = new Thread(() =>
                {

                    Thread.Sleep(650);
                    AndroidCore.context.StartActivity(intent);
                });
                Thread t2 = new Thread(() =>
                {

                    Thread.Sleep(800);
                    AndroidCore.context.StartActivity(intent);
                });
                AndroidCore.context.StartActivity(intent);
                t.Start();
                t2.Start();
			}
		}

		public static string FindAPKLocation(string package)
        {
            try
            {
                ApplicationInfo applicationInfo = Application.Context.PackageManager.GetApplicationInfo(package, PackageInfoFlags.MatchAll);
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
            AndroidCore.context.StartActivity(uninstallIntent);
        }

        public static bool IsPackageInstalled(string package)
        {
            foreach (AndroidApp a in GetInstalledApps())
            {
                if (a.PackageName == package) return true;
            }
            return false;
        }
        
        public static bool HasManageExternalStoragePermission(string packageName)
        {
            PackageManager pm = AndroidCore.context.PackageManager;
            ApplicationInfo appInfo;
            try
            {
                appInfo = pm.GetApplicationInfo(packageName, 0);
            }
            catch (PackageManager.NameNotFoundException e)
            {
                return false;
            }
            AppOpsManager appOps = (AppOpsManager)AndroidCore.context.GetSystemService(Context.AppOpsService);
            AppOpsManagerMode mode = appOps.UnsafeCheckOpNoThrow("android:manage_external_storage", appInfo.Uid, appInfo.PackageName);
            return mode == AppOpsManagerMode.Allowed;
        }

        public static void InitiateInstallApk(string apkLocation)
        {
            Android.Net.Uri uri = FileProvider.GetUriForFile(
                AndroidCore.context,
                AndroidCore.context.PackageName + ".provider",
                new Java.IO.File(apkLocation)
            );
            Intent intent = new Intent(Intent.ActionInstallPackage);
            intent.SetDataAndType(uri, "application/vnd.android.package-archive");
            intent.AddFlags(ActivityFlags.GrantReadUriPermission );
            intent.PutExtra(Intent.ExtraReturnResult, true);
            AndroidCore.installLauncher.Launch(intent);
            /*
             SideQuest decompiled
            Uri uri = FileProvider.GetUriForFile(AndroidCore.context, AndroidCore.context.PackageName + ".provider", new Java.IO.File(apkLocation));
            Intent intent = new Intent("android.intent.action.INSTALL_PACKAGE");
            intent.SetDataAndType(uri, "application/vnd.android.package-archive");
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.PutExtra("android.intent.extra.INSTALLER_PACKAGE_NAME", AndroidCore.context.PackageName);
            intent.PutExtra("android.intent.extra.RETURN_RESULT", true);
            AndroidCore.context.StartActivity(intent);
            */
            /*
             * 
            PackageManager pm = AndroidCore.context.PackageManager;
            if (!pm.CanRequestPackageInstalls())
            {
                Intent ini = new Intent(Settings.ActionManageUnknownAppSources);
                ini.SetData(Uri.Parse("package:" + AndroidCore.context.PackageName));
                AndroidCore.context.StartActivity(ini);
                return;
            }
            PackageInstaller.SessionParams para = new PackageInstaller.SessionParams(PackageInstallMode.FullInstall);
            int sessionId = pm.PackageInstaller.CreateSession(para);
            PackageInstaller.Session session = pm.PackageInstaller.OpenSession(sessionId);
            Stream o = session.OpenWrite("myApp.apk", 0, -1);
            Stream i = File.OpenRead(apkLocation);
            i.CopyTo(o);
            session.Fsync(o);
            o.Close();
            i.Close();
            // Create a PendingIntent for the installation result
            Intent intent = new Intent(Intent.ActionInstallPackage);
            PendingIntent pendingIntent = PendingIntent.GetBroadcast(AndroidCore.context, sessionId, intent, PendingIntentFlags.Immutable);

            // Commit the session and start the installation process
            session.Commit(pendingIntent.IntentSender);
             */
        }

        public static string GetDeviceID()
        {
            return Settings.Secure.GetString(AndroidCore.context.ContentResolver, Settings.Secure.AndroidId);
        }
    }
}
