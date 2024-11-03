using Android.Content;
using Android.Content.Res;
using AndroidX.Activity.Result;
using AndroidX.AppCompat.App;
using ComputerUtils.Logging;
using Java.Lang;
using Java.Util.Logging;
using Object = Java.Lang.Object;

namespace ComputerUtils
{
    public class AndroidCore
    {
        public static Context context { get; set; } = null;
        public static AssetManager assetManager { get; set; } = null;
        public static ActivityResultLauncher launcher { get; set; } = null;
        public static AppCompatActivity activity { get; set; } = null;
        public static ActivityResultLauncher installLauncher { get; set; } = null;
            
    }
    
    public class InstallLaucherResult: Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Object? result)
        {
            if (result is ActivityResult activityResult)
            {
                Logging.Logger.Log("Installation activity result code: " + ((ActivityResult)result).ResultCode.ToString(), LoggingType.Debug);
            }
        }
    }
}