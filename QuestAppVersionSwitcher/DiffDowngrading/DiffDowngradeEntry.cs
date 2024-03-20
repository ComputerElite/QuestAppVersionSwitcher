using System.Collections.Generic;

namespace QuestAppVersionSwitcher.DiffDowngrading
{
    public class DiffDowngradeEntry
    {
        public string SV { get; set; }
        public string TV { get; set; }
        public string SSHA256 { get; set; }
        public string DSHA256 { get; set; }
        public string TSHA256 { get; set; }
        public string download { get; set; }
        public bool isDirectDownload { get; set; }
        public string appid { get; set; }
        public bool isXDelta3 { get; set; }
        public long TargetByteSize { get; set; }
        public long SourceByteSize { get; set; }
        public List<DiffDowngradeFilePart> parts { get; set; }
    }

    public class DiffDowngradeFilePart
    {
        public string filename { get; set; } = "";
        public bool isApk { get; set; } = false;
        public long sourceByteStartIndex { get; set; } = 0;
        public long sourceByteLength { get; set; } = 0;
        public long targetByteStartIndex { get; set; } = 0;
        public long targetByteLength { get; set; } = 0;
    }

    public class DiffDowngradeEntryContainer
    {
        public List<DiffDowngradeEntry> versions { get; set; }
    }
}