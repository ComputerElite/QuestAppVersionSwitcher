using System.Collections.Generic;

namespace QuestAppVersionSwitcher.Mods
{
    public class ModConfig
    {
        public List<IMod> Mods { get; set; } = new List<IMod>();
    }
}