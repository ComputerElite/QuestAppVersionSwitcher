using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using ComputerUtils.Logging;
using DiffCreator;

namespace QuestAppVersionSwitcher.DiffDowngrading
{
    public class DiffCreator
    {
        public static DiffDowngradeEntry CreateDiff(string sourceBackup, string targetBackup,
            string outputDir)
        {
            DiffDowngradeEntry baseEntry = new DiffDowngradeEntry();
            if (!sourceBackup.EndsWith(Path.DirectorySeparatorChar)) sourceBackup += Path.DirectorySeparatorChar;
            if (!targetBackup.EndsWith(Path.DirectorySeparatorChar)) targetBackup += Path.DirectorySeparatorChar;
            if (!outputDir.EndsWith(Path.DirectorySeparatorChar)) outputDir += Path.DirectorySeparatorChar;
            if(!Directory.Exists(targetBackup)) Directory.CreateDirectory(targetBackup);

            Logger.Log("Creating diff");
            if(!File.Exists(sourceBackup + "app.apk") || !File.Exists(targetBackup + "app.apk"))
            {
                Logger.Log("App.apk not found in source or target backup");
                return null;
            }

            PatchingStatus sPatching = Apkutils.GetPatchingStatus(ZipFile.OpenRead(sourceBackup + "app.apk"));
            baseEntry.SV = sPatching.version;
            baseEntry.appid = sPatching.package;
            baseEntry.TV = Apkutils.GetPatchingStatus(ZipFile.OpenRead(targetBackup + "app.apk")).version;
            baseEntry.isXDelta3 = true;

            // Create entries
            baseEntry.Set(CreateDiffOfFile(baseEntry, sourceBackup + "app.apk", targetBackup + "app.apk", outputDir));
            // add obbs and other files
            List<string> allSourceFiles = new List<string>();
            List<string> allTargetFiles = new List<string>();
            if (Directory.Exists(sourceBackup + "/obb"))
            {
                allSourceFiles = Directory.GetFiles(sourceBackup + "/obb").ToList();
                allSourceFiles.Insert(0, sourceBackup + "app.apk");
            }

            if (Directory.Exists(targetBackup + "/obb"))
            {
                allTargetFiles = Directory.GetFiles(targetBackup + "/obb").ToList();
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
            Logger.Log("\n\nSummary:");
            Logger.Log("Diff for " + baseEntry.SV + " (apk) -> " + baseEntry.TV + " (apk) created");
            foreach (FileDiffDowngradeEntry otherFile in baseEntry.otherFiles)
            {
                Logger.Log("Diff for " + otherFile.sourceFilename + " -> " + otherFile.outputFilename + " created");
            }
            return baseEntry;
        }

        public static FileDiffDowngradeEntry CreateDiffOfFile(DiffDowngradeEntry baseEntry, string sourcePath,
            string targetPath,
            string outputPath)
        {
            FileDiffDowngradeEntry e = new FileDiffDowngradeEntry();
            e.sourceFilename = Path.GetFileName(sourcePath);
            e.outputFilename = Path.GetFileName(targetPath);
            e.diffFilename = baseEntry.GetDowngradeBaseName() + e.sourceFilename + ".xdelta3";
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
            Logger.Log("Encoding diff file for " + e.sourceFilename + " -> " + e.outputFilename + " to " +
                       e.diffFilename);
            Process.Start("xdelta3.exe", "-e -s \"" + sourcePath + "\" \"" + targetPath + "\" \"" + diffPath + "\"").WaitForExit();

            e.DSHA256 = Utils.GetSHA256OfFile(diffPath);
            e.DiffByteSize = new FileInfo(diffPath).Length;
            return e;
        }
    }
}