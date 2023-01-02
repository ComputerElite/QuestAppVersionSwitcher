using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuestAppVersionSwitcher.Mods
{
    public class FileCopyType
    {
        /// <summary>
        /// Name of the file copy, singular. E.g. "gorilla tag hat"
        /// </summary>
        public string NameSingular { get; set; }

        /// <summary>
        /// Name of the file copy, plural. E.g. "gorilla tag hats"
        /// </summary>
        public string NamePlural { get; set; }

        /// <summary>
        /// Path to copy files to/list files from
        /// </summary>
        public string Path { get; set; }


        /// <summary>
        /// List of support file extensions for this file copy destination
        /// </summary>
        public List<string> SupportedExtensions { get; set; }

        public List<string> ExistingFiles { get; } = new List<string>();

        /// <summary>
        /// Whether the loading attempt has finished successfully or not.
        /// </summary>
        public bool HasLoaded
        {
            get => _hasLoaded;
            private set
            {
                if (_hasLoaded != value)
                {
                    _hasLoaded = value;
                }
            }
        }
        private bool _hasLoaded;

        /// <summary>
        /// Whether or not the last loading attempt failed
        /// </summary>
        public bool LoadingFailed
        {
            get => _loadingFailed;
            private set
            {
                if (_loadingFailed != value)
                {
                    _loadingFailed = value;
                }
            }
        }
        private bool _loadingFailed;


        /// <summary>
        /// Loads the contents of this destination, replacing the old contents.
        /// </summary>
        public async Task LoadContents()
        {
            HasLoaded = false;
            LoadingFailed = false;
            try
            {
                if (!Directory.Exists(Path)) Directory.CreateDirectory(Path);

                List<string> currentFiles = Directory.GetFiles(Path).ToList();
                ExistingFiles.Clear();
                foreach (string file in currentFiles)
                {
                    ExistingFiles.Add(file);
                }
            }
            catch (Exception)
            {
                LoadingFailed = true;
                throw; // Rethrow for calling UI to handle if they want to
            }
            finally
            {
                HasLoaded = true;
            }
        }

        /// <summary>
        /// Copies a file to this destination
        /// </summary>
        /// <param name="localPath">The path of the file on the PC</param>
        public async Task PerformCopy(string localPath)
        {
            if (!Directory.Exists(Path)) Directory.CreateDirectory(Path);

            string destinationPath = System.IO.Path.Combine(Path, System.IO.Path.GetFileName(localPath));

            File.Copy(localPath, destinationPath);
            if (!ExistingFiles.Contains(destinationPath))
            {
                ExistingFiles.Add(destinationPath);
            }
        }

        /// <summary>
        /// Removes the copied file name and deletes it from the ExistingFiles list (no need to refresh the list to take effect)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task RemoveFile(string name)
        {
            File.Delete(name);
            ExistingFiles.Remove(name);
        }
    }
}