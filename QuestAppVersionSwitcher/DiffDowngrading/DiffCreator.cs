using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.Core;
using xdelta3.net;

namespace QuestAppVersionSwitcher.DiffDowngrading
{
    public class DiffCreator
    {
        public static DiffDowngradeEntry CreateDiff(string appId, string sourceBackup, string targetBackup, string outputDir)
        {
            DiffDowngradeEntry baseEntry = new DiffDowngradeEntry();
            baseEntry.appid = appId;
            if (!sourceBackup.EndsWith(Path.DirectorySeparatorChar)) sourceBackup += Path.DirectorySeparatorChar;
            if (!targetBackup.EndsWith(Path.DirectorySeparatorChar)) targetBackup += Path.DirectorySeparatorChar;
            if (!outputDir.EndsWith(Path.DirectorySeparatorChar)) outputDir += Path.DirectorySeparatorChar;
            
            Logger.Log("Creating diff");
            baseEntry.SV = BackupManager.GetBackupInfo(sourceBackup).gameVersion;
            baseEntry.TV = BackupManager.GetBackupInfo(targetBackup).gameVersion;
            baseEntry.isXDelta3 = true;
            
            // Create entries
            baseEntry.Set(CreateDiffOfFile(baseEntry, sourceBackup + "app.apk", targetBackup + "app.apk", outputDir));
            // add obbs and other files
            string[] sourceDirFiles = Directory.GetFiles(sourceBackup + "/obb/" + appId + "/");
            string[] targetDirFiles = Directory.GetFiles(targetBackup + "/obb/" + appId + "/");
            for(int i = 0; i < targetDirFiles.Length; i++)
            {
                // generate one diff for every target backup obbs
                baseEntry.otherFiles.Add(CreateDiffOfFile(baseEntry, sourceDirFiles[i % sourceDirFiles.Length], targetDirFiles[i], outputDir));
            }
            
            // The diff file is now created
            File.WriteAllText(outputDir + "version.json", JsonSerializer.Serialize(baseEntry));
            return baseEntry;
        }

        public static FileDiffDowngradeEntry CreateDiffOfFile(DiffDowngradeEntry baseEntry, string sourcePath, string targetPath,
            string outputPath)
        {
            FileDiffDowngradeEntry e = new FileDiffDowngradeEntry();
            e.sourceFilename = Path.GetFileName(sourcePath);
            e.outputFilename = Path.GetFileName(targetPath);
            e.diffFilename = baseEntry.GetDowngradeBaseName() + e.sourceFilename + ".diff";
            e.type = e.sourceFilename.ToLower().EndsWith(".apk") ? FileDiffDowngradeEntryType.Apk : FileDiffDowngradeEntryType.Obb;
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
                using(Stream targetStream = File.OpenRead(targetPath))
                {
                    using(Stream diffStream = File.OpenWrite(diffPath))
                    {
                        VCDiff.Encoders.VcEncoder encoder = new VCDiff.Encoders.VcEncoder(sourceStream, targetStream, diffStream);
                        Logger.Log("Encoding diff file for " + e.sourceFilename + " -> " + e.outputFilename + " to " + e.diffFilename);
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