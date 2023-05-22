using ComputerUtils.Android.VarUtils;

namespace QuestAppVersionSwitcher
{
    public class AndroidDevice
    {
        public int sdkVersion { get; set; } = 0;
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
    }
}