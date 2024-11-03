using Android.OS;
using ComputerUtils.VarUtils;
using Environment = Android.OS.Environment;

namespace QuestAppVersionSwitcher
{
    public class AndroidDevice
    {
        public int sdkVersion { get; set; } = 0;
        public string device { get; set; } = "";
        public long freeSpace { get; set; } = 0;

        public string freeSpaceString
        {
            get
            {
                return SizeConverter.ByteSizeToString(freeSpace);
            }
        }
        public long totalSpace { get; set; } = 0;

        public string totalSpaceString
        {
            get
            {
                return SizeConverter.ByteSizeToString(totalSpace);
            }
        }

        public static AndroidDevice GetCurrent()
        {
            return new AndroidDevice()
            {
                sdkVersion = (int)Build.VERSION.SdkInt,
                device = Build.Device,
                freeSpace = Environment.ExternalStorageDirectory.UsableSpace,
                totalSpace = Environment.ExternalStorageDirectory.TotalSpace,
            };
        }
    }
}