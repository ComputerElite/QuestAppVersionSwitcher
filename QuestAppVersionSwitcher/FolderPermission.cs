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
            Intent intent = new Intent(Intent.ActionOpenDocumentTree)
                .PutExtra(
                    DocumentsContract.ExtraInitialUri,
                    Uri.Parse(RemapPathForApi300OrAbove(dirInExtenalStorage)));
            l.Launch(intent);
        }

        public static string RemapPathForApi300OrAbove(string path)
        {
            string suffix = path;
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
            Stream file = GetOutputStream(to);
            file.Write(File.ReadAllBytes(from));
            file.Close();
            file.Dispose();
        }

        public static void CreateDirectory(string dir)
        {
            DocumentFile parent = GetAccessToFile(Directory.GetParent(dir).FullName);
            parent.CreateDirectory(Path.GetFileName(dir));
        }

        /// <summary>
        /// ONLY WORKS FOR /sdcard/Android/data/...!!!!!!!
        /// </summary>
        /// <param name="dir">Expected as /sdcard/Android/data/...</param>
        /// <returns></returns>
        public static DocumentFile GetAccessToFile(string dir)
        {
            string start = "/sdcard/Android/data/" + CoreService.coreVars.currentApp;
            string diff = dir.Replace(start + "/", "");
            string[] dirs = diff.Split('/');
            DocumentFile startDir = DocumentFile.FromTreeUri(AndroidCore.context, Uri.Parse(RemapPathForApi300OrAbove(start).Replace("com.android.externalstorage.documents/document/", "com.android.externalstorage.documents/tree/")));
            DocumentFile currentDir = startDir;
            foreach (string dirName in dirs)
            {
                if (currentDir.FindFile(dirName) == null) currentDir.CreateDirectory(dirName); // Create directory if it doesn't exist
                currentDir = currentDir.FindFile(dirName);
            }
            return currentDir;
        }

        public static Stream GetOutputStream(string path)
        {
            DocumentFile directory = GetAccessToFile(Directory.GetParent(path).FullName);
            string name = Path.GetFileName(path);
            if (directory.FindFile(name) != null) directory.FindFile(name).Delete();
            return AndroidCore.context.ContentResolver.OpenOutputStream(directory.CreateFile("application/octet-stream", name).Uri);
        }

        public static void Delete(string path)
        {
            DocumentFile directory = GetAccessToFile(Directory.GetParent(path).FullName);
            string name = Path.GetFileName(path);
            if (directory.FindFile(name) != null) directory.FindFile(name).Delete();
        }

        public static void CreateDirectoryIfNotExisting(string path)
        {
            DocumentFile directory = GetAccessToFile(Directory.GetParent(path).FullName);
            string name = Path.GetFileName(path);
            if (directory.FindFile(name) == null) directory.CreateDirectory(name);
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