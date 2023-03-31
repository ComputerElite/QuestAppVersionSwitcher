using System.IO;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.OS.Storage;
using Android.Provider;
using Android.Views.TextClassifiers;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.DocumentFile.Provider;
using ComputerUtils.Android;
using ComputerUtils.Android.FileManaging;
using Google.Android.Material.Dialog;
using Java.IO;
using Java.Lang;
using Java.Util.Logging;
using QuestAppVersionSwitcher.Core;
using File = System.IO.File;
using Logger = ComputerUtils.Android.Logging.Logger;

namespace QuestAppVersionSwitcher
{
    public class FolderPermission
    {
        public static ActivityResultLauncher l = null;
        public static void openDirectory(string dirInExtenalStorage)
        {
            if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2)
            {
                FileManager.CreateDirectoryIfNotExisting(dirInExtenalStorage);
            }
            Logger.Log(RemapPathForApi300OrAbove(dirInExtenalStorage));
            Intent intent = new Intent(Intent.ActionOpenDocumentTree)
                .PutExtra(
                    DocumentsContract.ExtraInitialUri,
                    Uri.Parse(RemapPathForApi300OrAbove(dirInExtenalStorage)));
            l.Launch(intent);
        }

        public static string RemapPathForApi300OrAbove(string path)
        {
            string suffix = path;
            Logger.Log(suffix);
            if (suffix.StartsWith("/sdcard")) suffix = suffix.Substring("/sdcard".Length);
            if (path.StartsWith(Environment.ExternalStorageDirectory.AbsolutePath))
            {
                suffix = path.Substring(Environment.ExternalStorageDirectory.AbsolutePath.Length);
            }
            string documentId = "primary:" + suffix.Substring(1);
            return DocumentsContract.BuildDocumentUri(
                "com.android.externalstorage.documents",
                documentId
            ).ToString();
        }

        public static void Copy(string from, string to)
        {
            Stream file =
                AndroidCore.context.ContentResolver.OpenOutputStream(Uri.Parse(RemapPathForApi300OrAbove(to)));
            StreamWriter sw = new StreamWriter(file);
            sw.Write(File.ReadAllBytes(from));
            sw.Dispose();
        }

        public static void CreateDirectory(string dir)
        {
            Logger.Log(RemapPathForApi300OrAbove(Directory.GetParent(dir).FullName));
            DocumentFile parent = DocumentFile.FromTreeUri(AndroidCore.context, Uri.Parse(RemapPathForApi300OrAbove(Directory.GetParent(dir).FullName)));
            Logger.Log(parent.CanWrite().ToString());
            parent.CreateDirectory(Path.GetFileName(dir));
        }
    }
    
    public class FolderPermissionCallback : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Result resultCode, Intent data)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                if (data.Data != null)
                {
                    AndroidCore.context.ContentResolver.TakePersistableUriPermission(
                        data.Data,
                        ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                }
            }
        }
        public void OnActivityResult(Object result)
        {
            if (result is ActivityResult activityResult && Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                if (activityResult.Data.Data != null)
                {
                    AndroidCore.context.ContentResolver.TakePersistableUriPermission(
                        activityResult.Data.Data,
                        ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                }
            }
        }
    }
}