using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using QuestAppVersionSwitcher.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using QuestPatcher.QMod;

namespace QuestAppVersionSwitcher.Mods
{
    public class ModManager
    {
        /// <summary>
        /// List of other package ids that are valid for the currently selected app
        /// </summary>
        public List<string> otherValidPackageIds { get; set; } = new List<string>();
        public List<IMod> Mods { get; } = new List<IMod>();
        public List<IMod> Libraries { get; } = new List<IMod>();

        public List<IMod> AllMods
        {
            get
            {
                return Mods.Concat(Libraries).ToList();
            }
        }
        private static readonly List<IMod> EmptyModList = new List<IMod>();
        public ModLoader usedModLoader = ModLoader.Scotland2;

        public string QuestLoaderModsPath => $"/sdcard/Android/data/{CoreService.coreVars.currentApp}/files/mods/";
        public string QuestLoaderLibsPath => $"/sdcard/Android/data/{CoreService.coreVars.currentApp}/files/libs/";
        public string Scotland2ModsPath => $"/sdcard/ModData/{CoreService.coreVars.currentApp}/Modloader/early_mods/";
        public string Scotland2LateModsPath => $"/sdcard/ModData/{CoreService.coreVars.currentApp}/Modloader/mods/";

        public string Scotland2LibsPath => $"/sdcard/ModData/{CoreService.coreVars.currentApp}/Modloader/libs/";

        public string ConfigPath => CoreService.coreVars.QAVSModsDir + $"{CoreService.coreVars.currentApp}/modsStatus.json";
        public string ModsExtractPath => GetModsExtractPath(CoreService.coreVars.currentApp);
        public string GetModsExtractPath(string package)
        {
            return  CoreService.coreVars.QAVSModsDir + package + "/installedMods/";
        }
        private readonly Dictionary<string, IModProvider> _modProviders = new Dictionary<string, IModProvider>();
        private readonly ModConverter _modConverter = new ModConverter();
        private readonly OtherFilesManager _otherFilesManager;
        private readonly JsonSerializerOptions _configSerializationOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private ModConfig? _modConfig;
        private bool _awaitingConfigSave;
        private bool loadingMods = false;


        public ModManager(OtherFilesManager otherFilesManager)
        {
            _configSerializationOptions.Converters.Add(_modConverter);
            _otherFilesManager = otherFilesManager;
        }

        private string NormalizeFileExtension(string extension)
        {
            string lower = extension.ToLower(); // Enforce lower case
            if (lower.StartsWith(".")) // Remove periods at the beginning
            {
                return lower.Substring(1);
            }
            return lower;
        }

        public void ForceSave()
        {
            _awaitingConfigSave = true;
            SaveMods();
        }

        public string GetModExtractPath(string id)
        {
            return Path.Combine(ModsExtractPath, id);
        }

        public void RegisterModProvider(IModProvider provider)
        {
            string extension = NormalizeFileExtension(provider.FileExtension);
            if (_modProviders.ContainsKey(extension))
            {
                throw new InvalidOperationException(
                    $"Attempted to add provider for extension {extension}, however a provider for this extension already existed");
            }

            if (provider is ConfigModProvider configProvider)
            {
                _modConverter.RegisterProvider(configProvider);
            }

            _modProviders[extension] = provider;
        }

        public async Task<IMod?> TryParseMod(string path, int taskId)
        {
            string extension = NormalizeFileExtension(Path.GetExtension(path));

            if (_modProviders.TryGetValue(extension, out IModProvider? provider))
            {
                return await provider.LoadFromFile(path, taskId);
            }

            return null;
        }

        public async Task DeleteMod(IMod mod, int taskId)
        {
            await mod.Provider.DeleteMod(mod, taskId);
        }

        public void Reset()
        {
            Mods.Clear();
            Libraries.Clear();
            _modConfig = null;
            foreach (IModProvider provider in _modProviders.Values)
            {
                provider.ClearMods();
            }

            _awaitingConfigSave = false;
        }

        public async Task CreateModDirectories()
        {
            if (usedModLoader == ModLoader.Scotland2)
            {
                FolderPermission.CreateDirectoryIfNotExisting(Scotland2ModsPath);
                FolderPermission.CreateDirectoryIfNotExisting(Scotland2LibsPath);
                FolderPermission.CreateDirectoryIfNotExisting(Scotland2LateModsPath);
            } else if(usedModLoader == ModLoader.QuestLoader)
            {
                FolderPermission.CreateDirectoryIfNotExisting(QuestLoaderModsPath);
                FolderPermission.CreateDirectoryIfNotExisting(QuestLoaderLibsPath);
            }
            FolderPermission.CreateDirectoryIfNotExisting(ModsExtractPath);
        }

        public async Task LoadModsForCurrentApp()
        {
            Logger.Log("Loading mods . . .");
            await CreateModDirectories();
            Logger.Log("Created directories");
            
            if (loadingMods) return;
            loadingMods = true;
            Logger.Log("Loading mods from disk");
            _modConfig = new ModConfig();
            foreach (var provider in _modProviders.Values)
            {
                await provider.LoadLegacyMods();
            }

            await SaveMods();

            foreach (IModProvider provider in _modProviders.Values)
            {
                await provider.LoadMods();
            }
            loadingMods = false;
        }

        public async Task SaveMods()
        {
            if (!_awaitingConfigSave)
            {
                return;
            }

            if (_modConfig is null)
            {
                Logger.Log("Could not save mods, mod config was null");
                return;
            }

            Logger.Log($"Saving {AllMods.Count} mods . . .");
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(_modConfig, _configSerializationOptions));
            _awaitingConfigSave = false;
        }

        internal void ModLoadedCallback(IMod mod)
        {
            mod.hasCover = mod.OpenCover().Length > 0;
            (mod.IsLibrary ? Libraries : Mods).Add(mod);
            _modConfig?.Mods.Add(mod);
            
            foreach (var copyType in mod.FileCopyTypes)
            {
                _otherFilesManager.RegisterFileCopy(CoreService.coreVars.currentApp, copyType);
            }
            _awaitingConfigSave = true;
        }

        internal void ModRemovedCallback(IMod mod)
        {
            (mod.IsLibrary ? Libraries : Mods).Remove(mod);
            foreach (var copyType in mod.FileCopyTypes)
            {
                _otherFilesManager.RemoveFileCopy(CoreService.coreVars.currentApp, copyType);
            }
            _modConfig?.Mods.Remove(mod);
            _awaitingConfigSave = true;
        }
    }
}