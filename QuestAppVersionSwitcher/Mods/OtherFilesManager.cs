using QuestAppVersionSwitcher.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QuestAppVersionSwitcher.Mods
{
    public class OtherFilesManager
    {
        public List<FileCopyType> CurrentDestinations
        {
            get
            {
                // If file copy types for this app are available in the index, return those
                if (_copyIndex.TryGetValue(CoreService.coreVars.currentApp, out var copyTypes))
                {
                    return copyTypes;
                }
                // Otherwise, return a list of no types to avoid throwing exceptions/null
                return _noTypesAvailable;
            }
        }
        private readonly List<FileCopyType> _noTypesAvailable = new List<FileCopyType>();

        private readonly Dictionary<string, List<FileCopyType>> _copyIndex;


        public OtherFilesManager()
        {
            var copyIndex = JsonSerializer.Deserialize<Dictionary<string, List<FileCopyType>>>(File.ReadAllText(CoreService.coreVars.QAVSFileCopiesFile));
            Debug.Assert(copyIndex != null);
            _copyIndex = copyIndex;
        }

        /// <summary>
        /// Gets the file copy destinations that can support files of the given extension
        /// </summary>
        /// <param name="extension"></param>
        /// <returns>The list of file copy destinations that work with the extension</returns>
        public List<FileCopyType> GetFileCopyTypes(string extension)
        {
            // Sanitise the extension to remove periods and make it lower case
            extension = extension.Replace(".", "").ToLower();

            return CurrentDestinations.Where(copyType => copyType.SupportedExtensions.Contains(extension)).ToList();
        }

        /// <summary>
        /// Adds the given file copy.
        /// </summary>
        /// <param name="packageId">The package ID that files of this type are intended for</param>
        /// <param name="type">The <see cref="FileCopyType"/> to add</param>
        public void RegisterFileCopy(string packageId, FileCopyType type)
        {
            if (!_copyIndex.TryGetValue(packageId, out var copyTypes))
            {
                copyTypes = new List<FileCopyType>();
                _copyIndex[packageId] = copyTypes;
            }

            copyTypes.Add(type);
        }

        /// <summary>
        /// Removes the given file copy.
        /// </summary>
        /// <param name="packageId">The package ID that files of this type are intended for</param>
        /// <param name="type">The <see cref="FileCopyType"/> to remove</param>
        public void RemoveFileCopy(string packageId, FileCopyType type)
        {
            _copyIndex[packageId].Remove(type);
        }
    }
}