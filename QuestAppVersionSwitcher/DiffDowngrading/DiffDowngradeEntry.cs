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
    }

    public class DiffDowngradeEntryContainer
    {
        public List<DiffDowngradeEntry> versions { get; set; }
    }
}