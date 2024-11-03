using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using QuestAppVersionSwitcher.Core;

namespace QuestAppVersionSwitcher.DiffDowngrading
{
    public class DiffCreator
    {
        public static DiffDowngradeEntry CreateDiff(string appId, string sourceBackup, string targetBackup,
            string outputDir)
        {
            DiffDowngradeEntry baseEntry = new DiffDowngradeEntry();
            baseEntry.appid = appId;
            if (!sourceBackup.EndsWith(Path.DirectorySeparatorChar)) sourceBackup += Path.DirectorySeparatorChar;
            if (!targetBackup.EndsWith(Path.DirectorySeparatorChar)) targetBackup += Path.DirectorySeparatorChar;
            if (!outputDir.EndsWith(Path.DirectorySeparatorChar)) outputDir += Path.DirectorySeparatorChar;
            FileManager.CreateDirectoryIfNotExisting(outputDir);

            Logger.Log("Creating diff");
            baseEntry.SV = BackupManager.GetBackupInfo(sourceBackup, true).gameVersion;
            baseEntry.TV = BackupManager.GetBackupInfo(targetBackup, true).gameVersion;
            baseEntry.isXDelta3 = true;

            // Create entries
            baseEntry.Set(CreateDiffOfFile(baseEntry, sourceBackup + "app.apk", targetBackup + "app.apk", outputDir));
            // add obbs and other files
            List<string> allSourceFiles = new List<string>();
            List<string> allTargetFiles = new List<string>();
            if (Directory.Exists(sourceBackup + "/obb/" + appId))
            {
                allSourceFiles = Directory.GetFiles(sourceBackup + "/obb/" + appId + "/").ToList();
                allSourceFiles.Insert(0, sourceBackup + "app.apk");
            }

            if (Directory.Exists(targetBackup + "/obb/" + appId))
            {
                allTargetFiles = Directory.GetFiles(targetBackup + "/obb/" + appId + "/").ToList();
                allTargetFiles.Insert(0, targetBackup + "app.apk");
            }

            for (int i = 1; i < allTargetFiles.Count; i++)
            {
                // generate one diff for every target backup obbs
                baseEntry.otherFiles.Add(CreateDiffOfFile(baseEntry, allSourceFiles[i % allSourceFiles.Count],
                    allTargetFiles[i], outputDir));
            }

            // The diff file is now created
            Logger.Log("Writing version.json file");
            File.WriteAllText(outputDir + "version.json", JsonSerializer.Serialize(baseEntry));
            Logger.Log("Finished creating diff");
            return baseEntry;
        }

        public static FileDiffDowngradeEntry CreateDiffOfFile(DiffDowngradeEntry baseEntry, string sourcePath,
            string targetPath,
            string outputPath)
        {
            FileDiffDowngradeEntry e = new FileDiffDowngradeEntry();
            e.sourceFilename = Path.GetFileName(sourcePath);
            e.outputFilename = Path.GetFileName(targetPath);
            e.diffFilename = baseEntry.GetDowngradeBaseName() + e.sourceFilename + ".diff";
            e.type = e.sourceFilename.ToLower().EndsWith(".apk")
                ? FileDiffDowngradeEntryType.Apk
                : FileDiffDowngradeEntryType.Obb;
            e.isXDelta3 = true;
            e.SourceByteSize = new FileInfo(sourcePath).Length;
            e.TargetByteSize = new FileInfo(targetPath).Length;
            e.SSHA256 = Utils.GetSHA256OfFile(sourcePath);
            e.TSHA256 = Utils.GetSHA256OfFile(targetPath);
            e.download = "";
            e.isDirectDownload = true;
            string diffPath = outputPath + e.diffFilename;
            using (Stream sourceStream = File.OpenRead(sourcePath))
            {
                using (Stream targetStream = File.OpenRead(targetPath))
                {
                    using (Stream diffStream = File.OpenWrite(diffPath))
                    {
                        VCDiff.Encoders.VcEncoder encoder = new VCDiff.Encoders.VcEncoder(sourceStream, targetStream, diffStream);
                        Logger.Log("Encoding diff file for " + e.sourceFilename + " -> " + e.outputFilename + " to " +
                                   e.diffFilename);
                        encoder.Encode();
                        encoder.Dispose();
                    }
                }
            }

            e.DSHA256 = Utils.GetSHA256OfFile(diffPath);
            return e;
        }
    }
}