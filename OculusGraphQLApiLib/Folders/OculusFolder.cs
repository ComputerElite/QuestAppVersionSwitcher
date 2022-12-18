using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Folders
{
    public class OculusFolder
    {
        public static string GetSoftwareDirectory(string oculusFolder, string canonicalName)
        {
            return oculusFolder + Path.DirectorySeparatorChar + "Software" + Path.DirectorySeparatorChar + canonicalName + Path.DirectorySeparatorChar;
        }

        public static string GetManifestPath(string oculusFolder, string canonicalName)
        {
            return oculusFolder + Path.DirectorySeparatorChar + "Manifests" + Path.DirectorySeparatorChar + canonicalName + ".json";
        }
        
    }
}
