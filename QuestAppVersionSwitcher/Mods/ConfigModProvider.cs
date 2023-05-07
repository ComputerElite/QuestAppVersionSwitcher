using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QuestAppVersionSwitcher.Mods
{
    public abstract class ConfigModProvider : JsonConverter<IMod>, IModProvider
    {
        /// <summary>
        /// ID of this provider, used for distinguishing which mods are from which provider in the config.
        /// </summary>
        public abstract string ConfigSaveId { get; }

        public abstract string FileExtension { get; }
        public abstract Task<IMod> LoadFromFile(string modPath, int taskId);
        public abstract Task DeleteMod(IMod mod, int taskId);
        public abstract Task LoadMods();
        public abstract void ClearMods();
        public abstract Task LoadLegacyMods();
    }
}