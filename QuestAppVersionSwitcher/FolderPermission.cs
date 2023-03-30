using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.OS.Storage;
using Android.Provider;
using AndroidX.Activity.Result.Contract;
using ComputerUtils.Android;
using Google.Android.Material.Dialog;
using Java.IO;
using Java.Util.Logging;
using QuestAppVersionSwitcher.Core;
using Logger = ComputerUtils.Android.Logging.Logger;

namespace QuestAppVersionSwitcher
{
    public class FolderPermission
    {
        public static void openDirectory(string dirInExtenalStorage)
        {
            Logger.Log("Converting api path to api 30+ path");
            Logger.Log("path is " + RemapPathForApi300OrAbove(dirInExtenalStorage));
            Intent intent = new Intent(Intent.ActionOpenDocumentTree)
                .PutExtra(
                    DocumentsContract.ExtraInitialUri,
                    Uri.Parse(RemapPathForApi300OrAbove(dirInExtenalStorage)));
            Logger.Log("Gonna start now");
            AndroidCore.context.StartActivity(intent);
        }

        public static string RemapPathForApi300OrAbove(string path)
        {
            string suffix = path.Substring(Environment.ExternalStorageDirectory.AbsolutePath.Length);
            string documentId = "STORAGE_PRIMARY:" + suffix.Substring(1);
            return DocumentsContract.BuildDocumentUri(
                "com.android.externalstorage.documents",
                documentId
            ).ToString();
        }
    }
}