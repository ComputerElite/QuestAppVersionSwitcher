using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace QuestAppVersionSwitcher.Mods
{
    public class ModsAndLibs
    {
        public List<IMod> mods { get; set; } = new List<IMod>();
        public List<IMod> libs { get; set; } = new List<IMod>();
    }
    public class QAVSModManager
    {
        public static ModManager modManager;
        public static OtherFilesManager otherFilesManager;
        public static bool operationOngoing = false;
        public static JsonSerializerOptions options;

        public static void Init()
        {
            otherFilesManager = new OtherFilesManager();
            modManager = new ModManager(otherFilesManager);
            modManager.RegisterModProvider(new QModProvider(modManager));
            options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new ModConverter());
            Update();
        }

        public static void Update()
        {
            modManager.LoadModsForCurrentApp();
        }

        public static void InstallMod(byte[] modBytes, string fileName)
        {
            TempFile f = new TempFile(Path.GetExtension(fileName));
            File.WriteAllBytes(f.Path, modBytes);
            IMod mod = modManager.TryParseMod(f.Path).Result;
            mod.Install();
            modManager.ForceSave();
        }

        public static void UninstallMod(string id)
        {
            foreach(IMod m in modManager.AllMods)
            {
                if(m.Id == id)
                {
                    m.Uninstall();
                    modManager.ForceSave();
                    break;
                }
            }
        }

        public static void DeleteMod(string id)
        {
            foreach (IMod m in modManager.AllMods)
            {
                if (m.Id == id)
                {
                    modManager.DeleteMod(m);
                    modManager.ForceSave();
                    break;
                }
            }
        }

        public static void EnableMod(string id)
        {
            foreach (IMod m in modManager.AllMods)
            {
                if (m.Id == id)
                {
                    m.Install();
                    modManager.ForceSave();
                    break;
                }
            }
        }

        public static string GetMods()
        {
            return JsonSerializer.Serialize(new ModsAndLibs
            {
                mods = modManager.Mods,
                libs = modManager.Libraries
            });
        }

        public static byte[] GetModCover(string id)
        {
            foreach (IMod m in modManager.AllMods)
            {
                if (m.Id == id)
                {
                    return m.OpenCover();
                }
            }
            return new byte[0];
        }
    }
}