using System.IO.Compression;
using QuestPatcher.Axml;

namespace DiffCreator;

public class Apkutils
{
    public const string ManifestPath = "AndroidManifest.xml";
    
    public static PatchingStatus GetPatchingStatus(ZipArchive apk, string packageId = "")
    {
        PatchingStatus status = new PatchingStatus();
        MemoryStream manifestStream = new MemoryStream();
        using (Stream s = apk.GetEntry(ManifestPath).Open())
        {
            s.CopyTo(manifestStream);
        }
        manifestStream.Position = 0;
        AxmlElement manifest = AxmlLoader.LoadDocument(manifestStream);
        foreach (AxmlAttribute a in manifest.Attributes)
        {
            if (a.Name == "versionName")
            {
                status.version = a.Value.ToString();
            }
            if (a.Name == "versionCode")
            {
                status.versionCode = a.Value.ToString();
            }
            if(a.Name == "package")
            {
                packageId = a.Value.ToString();
            }
        }
        AxmlElement appElement = manifest.Children.Single(element => element.Name == "application");
        status.copyOf = null;
        foreach (AxmlElement e in appElement.Children)
        {
            if (e.Attributes.Any(x => x.Name == "name" && x.Value.ToString() == "QAVS.copyOf"))
            {
                status.copyOf = (string)e.Attributes.FirstOrDefault(x => x.Name == "value").Value;
                //Logger.Log("App is copy of " + status.copyOf);
            }
        }
        status.package = packageId;
        manifestStream.Close();
        manifestStream.Dispose();
        apk.Dispose();
        return status;
    }
}