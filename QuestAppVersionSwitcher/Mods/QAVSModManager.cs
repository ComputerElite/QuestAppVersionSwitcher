using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QuestAppVersionSwitcher.Mods
{
    public class ModsAndLibs
    {
        public List<IMod> mods { get; set; } = new List<IMod>();
        public List<IMod> libs { get; set; } = new List<IMod>();
        public List<QAVSOperation> operations { get; set; } = new List<QAVSOperation>();
    }

    public enum QAVSOperationType
    {
        ModInstall,
        ModUninstall,
        ModDisable,
        ModDelete,
        DependencyDownload,
        Other,
		Error
	}

    public class QAVSOperation
    {
        public QAVSOperationType type { get; set; } = QAVSOperationType.ModInstall;
        public string name { get; set; } = "";
    }

    public class QAVSModManager
    {
        public static ModManager modManager;
        public static OtherFilesManager otherFilesManager;
        public static JsonSerializerOptions options;
        public static int operations = 0;
        public static Dictionary<int, QAVSOperation> runningOperations = new Dictionary<int, QAVSOperation>();

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

        public static bool SupportsFormat(string extension)
        {
            if (extension.ToLower() == ".qmod") return true;
            return false;
        }

        public static void Update()
        {
            modManager.LoadModsForCurrentApp();
        }

        public static void InstallMod(byte[] modBytes, string fileName)
        {
            TempFile f = new TempFile(Path.GetExtension(fileName));
            File.WriteAllBytes(f.Path, modBytes);
            InstallMod(f.Path, fileName);
        }

        public static void InstallMod(string path, string fileName)
        {
            int operationId = operations;
            operations++;
            runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.ModInstall, name = "Installing " + fileName });

            if(!SupportsFormat(Path.GetExtension(fileName)))
            {
                File.Move(path, Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + fileName);
                path = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + fileName;
				CoreVars.cosmetics.InstallCosmetic(CoreService.coreVars.currentApp, Path.GetExtension(fileName), path, true);
				runningOperations.Remove(operationId);
				return;   
            }
            try
			{
				IMod mod = modManager.TryParseMod(path).Result;
				mod.Install().Wait();
				runningOperations.Remove(operationId);
			} catch (Exception e)
			{
				runningOperations.Remove(operationId);
                operationId = operations;
				operations++;
				runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.Error, name = "Error installing mod: " + e.Message + "\n\nTo remove this message restart QuestAppVersionSwitcher" });
			}
            modManager.ForceSave();
        }

        public static void UninstallMod(string id)
        {
            int operationId = operations;
            operations++;
            runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.ModUninstall, name = "Unnstalling " + id });

            foreach (IMod m in modManager.AllMods)
            {
                if(m.Id == id)
                {
                    m.Uninstall();
                    modManager.ForceSave();
                    break;
                }
            }

            runningOperations.Remove(operationId);
        }

        public static void DeleteMod(string id)
        {
            int operationId = operations;
            operations++;
            runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.ModDelete, name = "Deleting " + id });
            foreach (IMod m in modManager.AllMods)
            {
                if (m.Id == id)
                {
                    modManager.DeleteMod(m);
                    modManager.ForceSave();
                    break;
                }
            }
            runningOperations.Remove(operationId);
        }

        public static void EnableMod(string id)
        {
            int operationId = operations;
            operations++;
            runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.ModInstall, name = "Installing " + id });

            foreach (IMod m in modManager.AllMods)
            {
                if (m.Id == id)
                {
                    try
					{
						m.Install().Wait();
						modManager.ForceSave();
						runningOperations.Remove(operationId);
					} catch(Exception e)
					{
						runningOperations.Remove(operationId);
						operationId = operations;
						operations++;
						runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.Error, name = "Error enabling mod: " + e.Message + "\n\nTo remove this message restart QuestAppVersionSwitcher" });
					}
                    break;
                }
            }
        }

        public static string GetMods()
        {
            return JsonSerializer.Serialize(new ModsAndLibs
            {
                mods = modManager.Mods,
                libs = modManager.Libraries,
                operations = runningOperations.Values.ToList()
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