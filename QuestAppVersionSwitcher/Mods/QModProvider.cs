using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.Core;
using QuestPatcher.QMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuestAppVersionSwitcher.Mods
{
    public class QModProvider : ConfigModProvider
    {
        public override string ConfigSaveId => "qmod";

        public override string FileExtension => "qmod";

        public Dictionary<string, QPMod> ModsById { get; } = new Dictionary<string, QPMod>();

        private readonly ModManager _modManager;

        public QModProvider(ModManager modManager)
        {
            _modManager = modManager;
        }

        internal string GetExtractDirectory(string id)
        {
            return _modManager.GetModExtractPath(id);
        }

        private void AddMod(QPMod mod)
        {
            ModsById[mod.Id] = mod;
        }

        public override async Task<IMod> LoadFromFile(string modPath, int taskId)
        {
            await using Stream modStream = File.OpenRead(modPath);
            await using QMod qmod = await QMod.ParseAsync(modStream);

            // Check that the package ID is correct. We don't want people installing Beat Saber mods on Gorilla Tag!
            Logger.Log($"Mod ID: {qmod.Id}, Version: {qmod.Version}, Is Library: {qmod.IsLibrary}");
            if (qmod.PackageId != null && qmod.PackageId != CoreService.coreVars.currentApp)
            {
                throw new InstallationException($"Mod is intended for app {qmod.PackageId}, but {CoreService.coreVars.currentApp} is selected");
            }

            var mod = new QPMod(this, qmod.GetManifest(), _modManager);

            // Check if upgrading from a previous version is OK, or if we have to fail the import
            ModsById.TryGetValue(qmod.Id, out QPMod? existingInstall);
            bool needImmediateInstall = false;
            if (existingInstall != null)
            {
                if (existingInstall.Version == qmod.Version)
                {
                    Logger.Log($"Version of existing {existingInstall.Id} is the same as the installing version ({mod.Version})");
                }
                if (existingInstall.Version > qmod.Version)
                {
                    throw new InstallationException($"Version of existing {existingInstall.Id} ({existingInstall.Version}) is greater than installing version ({mod.Version}). Direct version downgrades are not permitted. This may be fixed by deleting all mods and libraries.");
                }
                // Uninstall the existing mod. May throw an exception if other mods depend on the older version
                needImmediateInstall = await PrepareVersionChange(existingInstall, mod, taskId);
            }

            string pushPath = Path.Combine("/data/local/tmp/", $"{qmod.Id}.temp.modextract");
            // Save the mod files to the quest for later installing
            Logger.Log("Pushing & extracting on to quest . . .");
            if (Directory.Exists(GetExtractDirectory(qmod.Id))) Directory.Delete(GetExtractDirectory(qmod.Id), true);
            ZipFile.ExtractToDirectory(modPath, GetExtractDirectory(qmod.Id));

            AddMod(mod);
            _modManager.ModLoadedCallback(mod);

            if (needImmediateInstall)
            {
                await mod.Install(taskId);
            }

            Logger.Log("Import complete");
            return mod;
        }

        /// <summary>
        /// Checks to see if upgrading from the installed version to the new version is safe.
        /// i.e. this will throw an install exception if a mod depends on the older version being present.
        /// If upgrading is safe, this will uninstall the currently installed version to prepare for the version upgrade
        /// </summary>
        /// <param name="currentlyInstalled">The installed version of the mod</param>
        /// <param name="newVersion">The version of the mod to be upgraded to</param>
        /// <returns>True if the mod had installed dependants, and thus needs to be immediately installed</returns>
        private async Task<bool> PrepareVersionChange(QPMod currentlyInstalled, QPMod newVersion, int taskId)
        {
            Debug.Assert(currentlyInstalled.Id == newVersion.Id);
            Logger.Log($"Attempting to upgrade {currentlyInstalled.Id} v{currentlyInstalled.Version} to {newVersion.Id} v{newVersion.Version}");

            bool didFailToMatch = false;
            StringBuilder errorBuilder = new StringBuilder();
            errorBuilder.AppendLine($"Failed to upgrade installation of mod {currentlyInstalled.Id} to {newVersion.Version}: ");
            bool installedDependants = false;
            foreach (QPMod mod in ModsById.Values)
            {
                if (!mod.IsInstalled)
                {
                    continue;
                }

                foreach (Dependency dependency in mod.Manifest.Dependencies)
                {
                    if (dependency.Id == currentlyInstalled.Id)
                    {
                        if (dependency.VersionRange.IsSatisfied(newVersion.Version))
                        {
                            installedDependants = true;
                        }
                        else
                        {
                            string errorLine = $"Dependency of mod {mod.Id} requires version range {dependency.VersionRange} of {currentlyInstalled.Id}, however the version of {currentlyInstalled.Id} being upgraded to ({newVersion.Version}) does not intersect this range";
                            errorBuilder.AppendLine(errorLine);

                            Logger.Log(errorLine);
                            didFailToMatch = true;
                        }
                    }
                }
            }

            if (didFailToMatch)
            {
                throw new InstallationException(errorBuilder.ToString());
            }
            else
            {
                Logger.Log($"Deleting old version of {newVersion.Id} to prepare for upgrade . . .");
                await DeleteMod(currentlyInstalled, taskId);
                return installedDependants;
            }
        }

        private QPMod AssertQMod(IMod genericMod)
        {
            if (genericMod is QPMod mod)
            {
                return mod;
            }
            else
            {
                throw new InvalidOperationException("Passed non-qmod to qmod provider function");
            }
        }

        public override async Task DeleteMod(IMod genericMod, int taskId)
        {
            QPMod mod = AssertQMod(genericMod);

            if (mod.IsInstalled)
            {
                Logger.Log($"Uninstalling mod {mod.Id} to prepare for removal . . .");
                await genericMod.Uninstall(taskId);
            }

            Logger.Log($"Removing mod {mod.Id} . . .");
            Directory.Delete(GetExtractDirectory(mod.Id), true);

            ModsById.Remove(mod.Id);
            _modManager.ModRemovedCallback(mod);

            if (!mod.Manifest.IsLibrary)
            {
                await CleanUnusedLibraries(false, taskId);
            }
        }

        /// <summary>
        /// Finds a list of mods which depend on this mod (i.e. ones with any dependency on this mod's ID)
        /// </summary>
        /// <param name="mod">The mod to check the dependant mods of</param>
        /// <param name="onlyInstalledMods">Whether to only include mods which are actually installed (enabled)</param>
        /// <returns>A list of all mods depending on the mod</returns>
        public List<QPMod> FindModsDependingOn(QPMod mod, bool onlyInstalledMods = false)
        {
            // Fun linq
            return ModsById.Values.Where(otherMod => otherMod.Manifest.Dependencies.Any(dependency => dependency.Id == mod.Id) && (!onlyInstalledMods || otherMod.IsInstalled)).ToList();
        }

        /// <summary>
        /// Uninstalls all libraries that are not depended on by another mod
        /// <param name="onlyDisable">Whether to only uninstall (disable) the libraries. If this is true, only mods that are enabled count as dependant mods as well</param>
        /// </summary>
        internal async Task CleanUnusedLibraries(bool onlyDisable, int taskId)
        {
            bool actionPerformed = true;
            while (actionPerformed) // Keep attempting to remove libraries until none get removed this iteration
            {
                actionPerformed = false;
                List<QPMod> unused = ModsById.Values.Where(mod => mod.Manifest.IsLibrary && FindModsDependingOn(mod, onlyDisable).Count == 0).ToList();

                // Uninstall any unused libraries this iteration
                foreach (QPMod mod in unused)
                {
                    if (mod.IsInstalled)
                    {
                        Logger.Log($"{mod.Id} is unused - " + (onlyDisable ? "uninstalling" : "unloading"));
                        actionPerformed = true;
                        await mod.Uninstall(taskId);
                    }
                    if (!onlyDisable)
                    {
                        actionPerformed = true;
                        await DeleteMod(mod, taskId);
                    }
                }
            }
        }

        public override IMod Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            QModManifest? manifest = JsonSerializer.Deserialize<QModManifest>(ref reader, options);
            if (manifest == null)
            {
                throw new NullReferenceException("Null manifest for mod");
            }
            var mod = new QPMod(this, manifest, _modManager);

            AddMod(mod);
            return mod;
        }

        public override void Write(Utf8JsonWriter writer, IMod value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, AssertQMod(value).Manifest, options);
        }

        public override async Task LoadMods()
        {
            Logger.Log("Checking mod status");
            List<string> modFiles = FolderPermission.GetFiles(_modManager.ModsPath);
            List<string> libFiles = FolderPermission.GetFiles(_modManager.LibsPath);
            for(int i = 0; i < modFiles.Count; i++)
            {
                modFiles[i] = Path.GetFileName(modFiles[i]);
            }
            for (int i = 0; i < libFiles.Count; i++)
            {
                libFiles[i] = Path.GetFileName(libFiles[i]);
            }
            foreach (QPMod mod in ModsById.Values)
            {
                SetModStatus(mod, modFiles, libFiles);
            }
        }

        private void SetModStatus(QPMod mod, List<string> modFiles, List<string> libFiles)
        {
            bool hasAllMods = mod.Manifest.ModFileNames.TrueForAll(modFiles.Contains);
            bool hasAllLibs = mod.Manifest.LibraryFileNames.TrueForAll(libFiles.Contains);
            // TODO: Should we also check that file copies are present?
            // TODO: This would be more expensive as we would have to check the files in more directories
            // TODO: Should we check that the files in mods/libs actually match the ones within the mod?

            mod.IsInstalled = hasAllMods && hasAllLibs;
        }

        public override void ClearMods()
        {
            ModsById.Clear();
        }

        public override async Task LoadLegacyMods()
        {
            List<string> legacyFolders = Directory.GetDirectories(_modManager.ModsExtractPath).ToList();
            Logger.Log($"Attempting to load {legacyFolders.Count} legacy mods");
            foreach (var legacyFolder in legacyFolders)
            {
                Logger.Log($"Loading legacy mod in {Path.GetFileName(legacyFolder)}");
                var modJsonPath = Path.Combine(legacyFolder, "mod.json");
                if(!File.Exists(modJsonPath))
                {
                    Logger.Log("No mod.json found for in " + legacyFolder + ", skipping");
                    continue;
                }

                await using var modJsonStream = File.OpenRead(modJsonPath);
                var manifest = await QModManifest.ParseAsync(modJsonStream);

                var mod = new QPMod(this, manifest, _modManager);

                AddMod(mod);
                _modManager.ModLoadedCallback(mod);
            }
        }
    }
}