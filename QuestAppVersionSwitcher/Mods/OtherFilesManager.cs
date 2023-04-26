using System;
using QuestAppVersionSwitcher.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Java.Util.Logging;

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
            _copyIndex = new Dictionary<string, List<FileCopyType>>();
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
            ComputerUtils.Android.Logging.Logger.Log("supported: " + String.Join(", ", type.SupportedExtensions));
            ComputerUtils.Android.Logging.Logger.Log(type.Path);
            ComputerUtils.Android.Logging.Logger.Log(type.NameSingular);
            CoreVars.cosmetics.AddCopyType(packageId, type);
        }

        /// <summary>
        /// Removes the given file copy.
        /// </summary>
        /// <param name="packageId">The package ID that files of this type are intended for</param>
        /// <param name="type">The <see cref="FileCopyType"/> to remove</param>
        public void RemoveFileCopy(string packageId, FileCopyType type)
        {
            CoreVars.cosmetics.RemoveCopyType(packageId, type);
        }
    }
}