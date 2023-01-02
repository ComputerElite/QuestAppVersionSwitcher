using ComputerUtils.Android.FileManaging;
using QuestAppVersionSwitcher.Core;
using System;

namespace QuestAppVersionSwitcher.Mods
{
    public class TempFile
    {
        public string Path = "";
        public TempFile()
        {
            Path = CoreService.coreVars.QAVSTmpModsDir + DateTime.Now.Ticks + ".tmp";
        }

        public TempFile(string extension)
        {
            Path = CoreService.coreVars.QAVSTmpModsDir + DateTime.Now.Ticks + extension;
        }
    }
}