using System.Collections.Generic;
using System.IO;

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

        public List<FileDiffDowngradeEntry> otherFiles { get; set; } = new List<FileDiffDowngradeEntry>();
    }

    public class FileDiffDowngradeEntry
    {
        public string sourceFilename { get; set; } = "";
        private string _diffFilename = "";

        public string diffFilename
        {
            get
            {
                if (_diffFilename == "") return Path.GetFileName(download.Split('?')[0]);
                return _diffFilename;
            }
            set
            {
                _diffFilename = value;
            }
        }

        public string outputFilename { get; set; } = "";
        public FileDiffDowngradeEntryType type = FileDiffDowngradeEntryType.Apk;
        public bool isXDelta3 { get; set; } = false;
        public long TargetByteSize { get; set; } = 0;
        public long DiffByteSize { get; set; } = 0;
        public long SourceByteSize { get; set; } = 0;
        public string SSHA256 { get; set; } = "";
        public string DSHA256 { get; set; } = "";
        public string TSHA256 { get; set; } = "";
        public string download { get; set; } = "";
        public bool isDirectDownload { get; set; } = false;

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