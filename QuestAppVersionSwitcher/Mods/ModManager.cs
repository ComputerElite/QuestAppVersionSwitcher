using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QuestAppVersionSwitcher.Mods
{
    public class ModManager
    {
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

        public string ModsPath => $"/sdcard/Android/data/{CoreService.coreVars.currentApp}/files/mods/";
        public string LibsPath => $"/sdcard/Android/data/{CoreService.coreVars.currentApp}/files/libs/";

        public string ConfigPath => CoreService.coreVars.QAVSModsDir + $"{CoreService.coreVars.currentApp}/modsStatus.json";
        public string ModsExtractPath => CoreService.coreVars.QAVSModsDir + $"{CoreService.coreVars.currentApp}/installedMods/";

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
            FolderPermission.CreateDirectoryIfNotExisting(ModsPath);
            FolderPermission.CreateDirectoryIfNotExisting(LibsPath);
            FileManager.CreateDirectoryIfNotExisting(ModsExtractPath);
        }

        public async Task LoadModsForCurrentApp()
        {
            Logger.Log("Loading mods . . .");
            await CreateModDirectories();
            Logger.Log("Created directories");

            // If a config file exists, we'll need to load our mods from it
            /*
            if (File.Exists(ConfigPath))
            {
                Logger.Log("Loading mods from quest mod config");

                try
                {
                    ModConfig? modConfig = JsonSerializer.Deserialize<ModConfig>(File.ReadAllText(ConfigPath), _configSerializationOptions);
                    if (modConfig != null)
                    {
                        modConfig.Mods.ForEach(ModLoadedCallback);
                        _modConfig = modConfig;
                        Logger.Log($"{AllMods.Count} mods loaded");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to load mods from quest config: {ex}");
                }
            }
            else
            {
            */
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