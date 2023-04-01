using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using QuestPatcher.QMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace QuestAppVersionSwitcher.Mods
{
    public class QPMod : IMod
    {
        public IModProvider Provider => _provider;

        private readonly QModProvider _provider;

        public string Id => Manifest.Id;
        public string Name => Manifest.Name;
        public string? Description => Manifest.Description;
        public SemanticVersioning.Version Version => Manifest.Version;
        public string VersionString => Manifest.Version.ToString();
        public string? PackageVersion => Manifest.PackageVersion;
        public string Author => Manifest.Author;
        public string? Porter => Manifest.Porter;
        public bool IsLibrary => Manifest.IsLibrary;

        public IEnumerable<FileCopyType> FileCopyTypes { get; }

        public bool IsInstalled
        {
            get => _isInstalled;
            set
            {
                if (_isInstalled != value)
                {
                    _isInstalled = value;
                }
            }
        }

        private bool _isInstalled;

        internal QModManifest Manifest { get; }
        private readonly ModManager _modManager;

        public QPMod(QModProvider provider, QModManifest manifest, ModManager modManager)
        {
            _provider = provider;
            Manifest = manifest;
            _modManager = modManager;

            FileCopyTypes = manifest.CopyExtensions.Select(copyExt =>
            {
                string destination = copyExt.Destination;
                if (!destination.EndsWith(Path.DirectorySeparatorChar)) destination += Path.DirectorySeparatorChar;
                return new FileCopyType()
                {
                    NameSingular = $"{manifest.Name} .{copyExt.Extension} file",
                    NamePlural = $"{manifest.Name} .{copyExt.Extension} files",
                    Path = destination,
                    SupportedExtensions = new List<string> { "." + copyExt.Extension }
                };
            }).ToList();
        }

        public Task Install()
        {
            return Install(new List<string>());
        }

        private async Task Install(List<string> installedInBranch)
        {
            if (IsInstalled)
            {
                Logger.Log($"Mod {Id} is already installed. Not installing");
                return;
            }

            Logger.Log($"Installing mod {Id}");

            installedInBranch.Add(Id); // Add to the installed tree so that dependencies further down on us will trigger a recursive install error

            foreach (Dependency dependency in Manifest.Dependencies)
            {
                await PrepareDependency(dependency, installedInBranch);
            }

            string extractPath = _provider.GetExtractDirectory(Id);

            // Copy files to actually install the mod

            List<KeyValuePair<string, string>> copyPaths = new List<KeyValuePair<string, string>>();
            List<string> directoriesToCreate = new List<string>();
            foreach (string libraryPath in Manifest.LibraryFileNames)
            {
                Logger.Log($"Starting library file copy {libraryPath} . . .");
                copyPaths.Add(new KeyValuePair<string, string>(Path.Combine(extractPath, libraryPath), Path.Combine(_modManager.LibsPath, Path.GetFileName(libraryPath))));
            }

            foreach (string modPath in Manifest.ModFileNames)
            {
                Logger.Log($"Starting mod file copy {modPath} . . .");
                copyPaths.Add(new KeyValuePair<string, string>(Path.Combine(extractPath, modPath), Path.Combine(_modManager.ModsPath, Path.GetFileName(modPath))));
            }

            foreach (FileCopy fileCopy in Manifest.FileCopies)
            {
                Logger.Log($"Starting file copy {fileCopy.Name} to {fileCopy.Destination}");
                string? directoryName = Path.GetDirectoryName(fileCopy.Destination);
                if (directoryName != null)
                {
                    directoriesToCreate.Add(directoryName);
                }
                copyPaths.Add(new KeyValuePair<string, string>(Path.Combine(extractPath, fileCopy.Name), fileCopy.Destination));
            }

            foreach (string d in directoriesToCreate)
            {
                FileManager.CreateDirectoryIfNotExisting(d);
            }
            foreach(KeyValuePair<string, string> k in copyPaths)
            {
                try
                {
                    string dir = Directory.GetParent(k.Value).FullName;
                    // If file is in android folder use SAF
                    if (k.Value.Contains("Android/data") || k.Value.Contains("Android/obb"))
                    {
                        FolderPermission.CreateDirectoryIfNotExisting(dir);
                        FolderPermission.Copy(k.Key, k.Value);
                    }
                    else
                    {
                        // If file is not in android folder use normal file copy
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        File.Copy(k.Key, k.Value, true);
                    }
                } catch(Exception e)
                {
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
            }
            IsInstalled = true;
            installedInBranch.Remove(Id);
            Logger.Log("Install method finished");
            return;
        }

        public async Task Uninstall()
        {
            if (!IsInstalled)
            {
                Logger.Log($"Mod {Id} is already uninstalled. Not uninstalling");
                return;
            }

            Logger.Log($"Uninstalling mod {Id} . . .");
            
            List<string> filesToRemove = new List<string>();
            // Remove mod SOs so that the mod will not load
            foreach (string modFilePath in Manifest.ModFileNames)
            {
                Logger.Log($"Removing mod file {modFilePath}");
                filesToRemove.Add(Path.Combine(_modManager.ModsPath, Path.GetFileName(modFilePath)));
            }

            foreach (string libraryPath in Manifest.LibraryFileNames)
            {
                // Only remove libraries if they aren't used by another mod
                bool isUsedElsewhere = false;
                foreach (QPMod otherMod in _provider.ModsById.Values)
                {
                    if (otherMod != this && otherMod.IsInstalled && otherMod.Manifest.LibraryFileNames.Contains(libraryPath))
                    {
                        Logger.Log($"Other mod {otherMod.Id} still needs lib file {libraryPath}, not removing");
                        isUsedElsewhere = true;
                        break;
                    }
                }

                if (!isUsedElsewhere)
                {
                    Logger.Log("Removing library file " + libraryPath);
                    filesToRemove.Add(Path.Combine(_modManager.LibsPath, Path.GetFileName(libraryPath)));
                }
            }

            foreach (FileCopy fileCopy in Manifest.FileCopies)
            {
                Logger.Log("Removing copied file " + fileCopy.Destination);
                filesToRemove.Add(fileCopy.Destination);
            }

            foreach(string f in filesToRemove)
            {
                if (File.Exists(f))
                {
                    // If file is in android folder 
                    if (f.Contains("Android/data") || f.Contains("Android/obb"))
                    {
                        // Delete using SAF
                        if (File.Exists(f)) FolderPermission.Delete(f);
                    }
                    else
                    {
                        // If file is in normal folder
                        // Delete the file using normal method
                        File.Delete(f);
                    }
                }
                
            }

            IsInstalled = false;

            if (!Manifest.IsLibrary)
            {
                // Only disable the unused libraries, don't completely remove them
                // This is to avoid redownloading dependencies if the mod is uninstalled then reinstalled without unloading
                await _provider.CleanUnusedLibraries(true);
            }
        }

        public byte[] OpenCover()
        {
            if (Manifest.CoverImagePath == null)
            {
                return new byte[0];
            }

            string coverPath = Path.Combine(_provider.GetExtractDirectory(Id), Manifest.CoverImagePath);
            if (!File.Exists(coverPath)) return new byte[0];
            return File.ReadAllBytes(coverPath);
        }

        /// <summary>
        /// Checks that a dependency is installed, and that the installed version is within the correct version range.
        /// If it's not installed, we will attempt to download the dependency if it specifies a download path, otherwise this fails.
        /// Does sanity checking for cyclical dependencies and will also attempt to upgrade installed versions via the download link where possible.
        /// </summary>
        /// <param name="dependency">The dependency to install</param>
        /// <param name="installedInBranch">The number of mods that are currently downloading down this branch of the install "tree", used to check for cyclic dependencies</param>
        private async Task PrepareDependency(Dependency dependency, List<string> installedInBranch)
        {
            int operationId = QAVSModManager.operations;
            QAVSModManager.operations++;
            QAVSModManager.runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.DependencyDownload, name = "Downloading Dependency " + dependency.Id });
            Logger.Log($"Preparing dependency of {dependency.Id} version {dependency.VersionRange}");
            int existingIndex = installedInBranch.FindIndex(downloadedDep => downloadedDep == dependency.Id);
            if (existingIndex != -1)
            {
                string dependMessage = "";
                for (int i = existingIndex; i < installedInBranch.Count; i++)
                {
                    dependMessage += $"{installedInBranch[i]} depends on ";
                }
                dependMessage += dependency.Id;
                QAVSModManager.runningOperations.Remove(operationId);
                throw new InstallationException($"Recursive dependency detected: {dependMessage}");
            }

            _provider.ModsById.TryGetValue(dependency.Id, out QPMod? existing);
            // Could be significantly simpler but I want to do lots of logging since this behaviour can be confusing
            if (existing != null)
            {
                if (dependency.VersionRange.IsSatisfied(existing.Version))
                {
                    Logger.Log($"Dependency {dependency.VersionRange} is already loaded and within the version range");
                    if (!existing.IsInstalled)
                    {
                        Logger.Log($"Installing dependency {dependency.Id} . . .");
                        await existing.Install(installedInBranch);
                    }
                    QAVSModManager.runningOperations.Remove(operationId);
                    return;
                }

                if (dependency.DownloadUrlString != null)
                {
                    Logger.Log($"Dependency with ID {dependency.Id} is already installed but with an incorrect version ({existing.Version} does not intersect {dependency.VersionRange}). QuestPatcher will attempt to upgrade the dependency");
                }
                else
                {
                    QAVSModManager.runningOperations.Remove(operationId);
                    throw new InstallationException($"Dependency with ID {dependency.Id} is already installed but with an incorrect version ({existing.Version} does not intersect {dependency.VersionRange}). Upgrading was not possible as there was no download link provided");
                }
            }
            else if (dependency.DownloadUrlString == null)
            {
                QAVSModManager.runningOperations.Remove(operationId);
                throw new InstallationException($"Dependency {dependency.Id} is not installed, and the mod depending on it does not specify a download path if missing");
            }

            QPMod installedDependency;
            TempFile downloadFile = new TempFile();
            Logger.Log($"Downloading dependency {dependency.Id} . . .");
            try
            {
                ExternalFilesDownloader.DownloadUrl(dependency.DownloadUrlString, downloadFile.Path);
            }
            catch (WebException ex)
            {
                // Print a nicer error message
                QAVSModManager.runningOperations.Remove(operationId);
                throw new InstallationException($"Failed to download dependency from URL {dependency.DownloadIfMissing}: {ex.Message}", ex);
            }

            installedDependency = (QPMod)await _provider.LoadFromFile(downloadFile.Path);

            await installedDependency.Install(installedInBranch);

            Logger.Log("Installed Dependency");

            // Sanity checks that the download link actually pointed to the right mod
            if (dependency.Id != installedDependency.Id)
            {
                await _provider.DeleteMod(installedDependency);
                QAVSModManager.runningOperations.Remove(operationId);
                throw new InstallationException($"Downloaded dependency had ID {installedDependency.Id}, whereas the dependency stated ID {dependency.Id}");
            }

            if (!dependency.VersionRange.IsSatisfied(installedDependency.Version))
            {
                await _provider.DeleteMod(installedDependency);
                QAVSModManager.runningOperations.Remove(operationId);
                throw new InstallationException($"Downloaded dependency {installedDependency.Id} v{installedDependency.Version} was not within the version range stated in the dependency info ({dependency.VersionRange})");
            }
            QAVSModManager.runningOperations.Remove(operationId);
        }
    }
}