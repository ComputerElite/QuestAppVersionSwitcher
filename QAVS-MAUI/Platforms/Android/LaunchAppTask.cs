using ComputerUtils.AndroidTools;
using Java.Util;
using QuestAppVersionSwitcher.Core;

namespace QuestAppVersionSwitcher
{
    public class LaunchAppTask : TimerTask
    {
        public override void Run()
        {
            AndroidService.LaunchApp(CoreService.coreVars.currentApp);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}