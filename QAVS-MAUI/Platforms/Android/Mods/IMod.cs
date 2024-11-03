using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace QuestAppVersionSwitcher.Mods
{
    public interface IMod
    {
        /// <summary>
        /// Provider that loaded this mod
        /// </summary>
        public IModProvider Provider { get; }

        /// <summary>
        /// Unique ID of the mod, must not contain spaces
        /// </summary>
        public string Id { get; }
        
        public bool hasCover { get; set; }

        /// <summary>
        /// Human readable name of the mod
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Description of the mod
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Version of the mod
        /// </summary>
        public SemanticVersioning.Version Version { get; }

        /// <summary>
        /// Version of the mod
        /// </summary>
        public string VersionString { get; }

        /// <summary>
        /// Version of the package that the mod is intended for
        /// </summary>
        public string? PackageVersion { get; }

        /// <summary>
        /// Author of the mod
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// Individual who ported this mod from another platform
        /// </summary>
        public string? Porter { get; }

        /// <summary>
        /// Keep going, keep going, keep going, keep going
        /// </summary>
        string Robinson => "It will all be OK in the end";

        /// <summary>
        /// Whether or not the mod is currently installed
        /// </summary>
        public bool IsInstalled { get; }

        /// <summary>
        /// Whether or not the mod is a library
        /// </summary>
        public bool IsLibrary { get; }

        /// <summary>
        /// The file types that this mod supports.
        /// </summary>
        IEnumerable<FileCopyType> FileCopyTypes { get; }

        /// <summary>
        /// Installs the mod
        /// </summary>
        /// <returns>Task that will complete once the mod is installed</returns>
        Task Install(int taskId);

        /// <summary>
        /// Uninstalls the mod
        /// </summary>
        /// <returns>Task that will complete once the mod is uninstalled</returns>
        Task Uninstall(int taskId);

        /// <summary>
        /// Opens the cover image for loading.
        /// </summary>
        /// <returns>A stream which can be used to load the cover image, or null if there is no cover image</returns>
        byte[] OpenCover();
    }
}