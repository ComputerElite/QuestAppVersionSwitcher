using System.IO;
using System.Text.Json;
using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.Core;
using xdelta3.net:

namespace QuestAppVersionSwitcher.DiffDowngrading
{
    public class DiffCreator
    {
        public static void CreateDiff(string appId, string sourceBackup, string targetBackup, string outputDir)
        {
            DiffDowngradeEntry e = new DiffDowngradeEntry();
            e.appid = appId;
            // add apk entry
            
            string sourcePath = CoreService.coreVars.QAVSTmpDowngradeDir + "source";
            string targetPath = CoreService.coreVars.QAVSTmpDowngradeDir + "target";
            FileStream sourceStream = File.Create(sourcePath);
            FileStream targetStream = File.Create(targetPath);
            long sourceIndexCounter = 0;
            long targetIndexCounter = 0;
            e.parts.Add(GetPart(sourceBackup + "app.apk", targetBackup + "app.apk", ref sourceStream, ref targetStream, ref sourceIndexCounter, ref targetIndexCounter, true));
            
            // Add obbs
            // ToDo
            
            Logger.Log("Creating diff");
            sourceStream.Flush();
            targetStream.Flush();
            sourceStream.Dispose();
            targetStream.Dispose();
            e.SSHA256 = Utils.GetSHA256OfFile(sourcePath);
            e.TSHA256 = Utils.GetSHA256OfFile(targetPath);
            e.SV = BackupManager.GetBackupInfo(sourceBackup).gameVersion;
            e.TV = BackupManager.GetBackupInfo(targetBackup).gameVersion;
            e.isXDelta3 = true;
            e.SourceByteSize = sourceIndexCounter;
            e.TargetByteSize = targetIndexCounter;
            Logger.Log("Index entry: " + JsonSerializer.Serialize(e));
            
            //Xdelta3Lib.Encode()
        }
        
        public static DiffDowngradeFilePart GetPart(string sourceFilePath, string targetFilePath, ref FileStream sourceStream, ref FileStream targetStream, ref long sourceIndexCounter, ref long targetIndexCounter, bool isApk)
        {
            FileStream sourceFileStream = File.OpenRead(sourceFilePath);
            FileStream targetFileStream = File.OpenRead(targetFilePath);
            DiffDowngradeFilePart part = new DiffDowngradeFilePart();
            part.filename = Path.GetFileName(sourceFilePath);
            part.sourceByteStartIndex = sourceIndexCounter;
            part.sourceByteLength = sourceFileStream.Length;
            part.targetByteStartIndex = targetIndexCounter;
            part.targetByteLength = targetFileStream.Length;
            part.isApk = isApk;
            sourceIndexCounter += sourceFileStream.Length;
            targetIndexCounter += targetFileStream.Length;
            Logger.Log("Adding part " + part.filename + " to diff: " + JsonSerializer.Serialize(part));
            
            sourceFileStream.CopyTo(sourceStream);
            targetFileStream.CopyTo(targetStream);
            sourceFileStream.Dispose();
            targetFileStream.Dispose();
            return part;
        }
    }
}