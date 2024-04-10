using System.Collections.Generic;

namespace QuestAppVersionSwitcher.DiffDowngrading
{
    public class DiffDowngradeEntry : FileDiffDowngradeEntry
    {
        public string SV { get; set; }
        public string TV { get; set; }
        public string appid { get; set; }

        public string GetDowngradeBaseName()
        {
            return appid + "." + SV + "TO" + TV + ".";
        }
        public List<FileDiffDowngradeEntry> otherFiles { get; set; }
    }

    public class FileDiffDowngradeEntry
    {
        public string sourceFilename { get; set; } = "";
        public string diffFilename { get; set; } = "";
        public string outputFilename { get; set; } = "";
        public FileDiffDowngradeEntryType type = FileDiffDowngradeEntryType.Apk;
        public bool isXDelta3 { get; set; }
        public long TargetByteSize { get; set; }
        public long SourceByteSize { get; set; }
        public string SSHA256 { get; set; }
        public string DSHA256 { get; set; }
        public string TSHA256 { get; set; }
        public string download { get; set; }
        public bool isDirectDownload { get; set; }

        public void Set(FileDiffDowngradeEntry e)
        {
            this.sourceFilename = e.sourceFilename;
            this.outputFilename = e.outputFilename;
            this.type = e.type;
            this.isXDelta3 = e.isXDelta3;
            this.TargetByteSize = e.TargetByteSize;
            this.SourceByteSize = e.SourceByteSize;
            this.SSHA256 = e.SSHA256;
            this.DSHA256 = e.DSHA256;
            this.TSHA256 = e.TSHA256;
            this.download = e.download;
            this.isDirectDownload = e.isDirectDownload;
        }
    }
    
    public enum FileDiffDowngradeEntryType
    {
        Unknown = -1,
        Apk = 0,
        Obb = 1
    }

    public class DiffDowngradeEntryContainer
    {
        public List<DiffDowngradeEntry> versions { get; set; }
    }
}