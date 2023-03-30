using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.OS.Storage;
using AndroidX.Activity.Result.Contract;
using ComputerUtils.Android;
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
            string path = Environment.ExternalStorageDirectory.AbsolutePath + "/" + dirInExtenalStorage;
            File file = new File(path);
            string startDir = "";
            string finalDirPath = "";

            if (file.Exists()) {
                startDir = dirInExtenalStorage.Replace("/", "%2F");
            } 

            StorageManager sm = (StorageManager)AndroidCore.context.GetSystemService(Context.StorageService);

            Intent intent = sm.PrimaryStorageVolume.CreateOpenDocumentTreeIntent();


            Uri uri = (Uri)intent.GetParcelableExtra("android.provider.extra.INITIAL_URI");

            string scheme = uri.ToString();

            Logger.Log("INITIAL_URI scheme: " + scheme);

            scheme = scheme.Replace("/root/", "/document/");

            finalDirPath = scheme + "%3A" + startDir;

            uri = Uri.Parse(finalDirPath);

            intent.PutExtra("android.provider.extra.INITIAL_URI", uri);

            Logger.Log("uri: " + uri.ToString());

            try {
                AndroidCore.context.StartActivity(intent);
            } catch (ActivityNotFoundException ignored) {

            }
        }
    }
}