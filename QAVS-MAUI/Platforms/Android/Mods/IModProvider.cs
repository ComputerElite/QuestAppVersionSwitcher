﻿using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace QuestAppVersionSwitcher.Mods
{
    public interface IModProvider
    {
        /// <summary>
        /// File extension of mod files that can be loaded by this provider
        /// </summary>
        public string FileExtension { get; }

        /// <summary>
        /// Loads a mod from the given path and copies the files to the quest if necessary
        /// Whenever a mod is loaded, by this method or by dependency installation, it should call <see cref="ModManager.ModLoadedCallback"/>
        /// </summary>
        /// <param name="modPath">Path of the mod file</param>
        /// <returns>The loaded mod info</returns>
        Task<IMod> LoadFromFile(string modPath, int taskId);

        /// <summary>
        /// Deletes the given mod from the quest and removes it from this provider.
        /// Whenever a mod is loaded, by this method or by dependency installation, it should call <see cref="ModManager.ModRemovedCallback"/>
        /// </summary>
        /// <param name="mod">Mod to delete</param>
        /// <returns>Task completing when the mod is deleted</returns>
        Task DeleteMod(IMod mod, int taskId);

        /// <summary>
        /// Loads the mods from the quest.
        /// </summary>
        /// <returns>Task completing when all mods are loaded</returns>
        Task LoadMods();

        /// <summary>
        /// Clears currently loaded mods
        /// </summary>
        void ClearMods();

        /// <summary>
        /// Invoked if no mod config is found when loading mods.
        /// Loads mods in an outdated format.
        /// </summary>
        Task LoadLegacyMods();
    }
}