namespace DiffCreator;

public class PatchingStatus
{
        public bool isPatched { get; set; } = false;
        public bool isInstalled { get; set; } = true;
        public bool canBePatched { get; set; } = true; // Not implemented yet.
        public string version { get; set; } = "";
        public string copyOf { get; set; } = "";
        public string versionCode { get; set; } = "";
        public string package { get; set; } = "";
}