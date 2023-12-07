namespace QuestAppVersionSwitcher.DiffDowngrading
{
    public class DiffDownloadRequest
    {
        public string packageName { get; set; } = "";
        public string targetSha { get; set; } = "";
        public string targetVersion { get; set; } = "";
        public string sourceSha { get; set; } = "";
        
    }
}