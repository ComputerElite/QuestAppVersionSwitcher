using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ComputerUtils.Android.FileManaging;
using Java.Lang;
using Org.BouncyCastle.Asn1.Pkcs;
using Exception = System.Exception;

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
        ModInstall = 0,
        ModUninstall = 1,
        ModDisable = 2,
        ModDelete = 3,
        DependencyDownload = 4,
        Other = 5,
		Error = 6,
        ModDownload = 7,
        QueuedModInstall = 8
    }

    public class QAVSOperation
    {
        public QAVSOperationType type { get; set; } = QAVSOperationType.ModInstall;
        public string name { get; set; } = "";
        public int operationId { get; set; } = 0;
        public bool isDone { get; set; } = false;
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
            modManager.Reset();
            modManager.LoadModsForCurrentApp();
        }

        public static void InstallMod(byte[] modBytes, string fileName, string cosmeticsType = "")
        {
            TempFile f = new TempFile(Path.GetExtension(fileName));
            File.WriteAllBytes(f.Path, modBytes);
            InstallMod(f.Path, fileName, cosmeticsType);
        }

        public static bool installingMod = false;
        public static List<QueuedMod> installQueue = new List<QueuedMod>();

        public class QueuedMod
        {
            public string path;
            public string filename;
            public int queuedOperationId;

            public QueuedMod(string path, string filename, int operationId)
            {
                this.path = path;
                this.filename = filename;
                queuedOperationId = operationId;
            }
        }

        public static void InstallFirstModFromQueue()
        {
            if (installingMod || installQueue.Count <= 0) return;
            runningOperations[installQueue[0].queuedOperationId].isDone = true;
            installingMod = true;
            int operationId = operations;
            operations++;
            runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.ModInstall, name = "Installing " + installQueue[0].filename, operationId = operationId});
            
            try
            {
                IMod mod = modManager.TryParseMod(installQueue[0].path).Result;
                mod.Install().Wait();
                runningOperations[operationId].isDone = true;
            } catch (Exception e)
            {
                runningOperations[operationId].isDone = true;
                operationId = operations;
                operations++;
                runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.Error, name = "Error installing mod: " + e.Message + "\n\nTo remove this message restart QuestAppVersionSwitcher", operationId = operationId });
            }
            modManager.ForceSave();
            FileManager.DeleteFileIfExisting(installQueue[0].path);
            installQueue.RemoveAt(0);
            installingMod = false;
            InstallFirstModFromQueue();
        }

        public static void InstallMod(string path, string fileName, string cosmeticsType = "")
        {
            if(!SupportsFormat(Path.GetExtension(fileName)) || cosmeticsType != "")
            {
                File.Move(path, Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + fileName);
                path = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + fileName;
                if (cosmeticsType != "")
                {
                    CoreVars.cosmetics.InstallCosmeticById(CoreService.coreVars.currentApp, cosmeticsType, path, true);
                }
                else
                {
                    CoreVars.cosmetics.InstallCosmeticByExtension(CoreService.coreVars.currentApp, Path.GetExtension(fileName), path, true);
                }
                FileManager.DeleteFileIfExisting(path);
                return;
            }
            int operationId = operations;
            operations++;
            runningOperations.Add(operationId, new QAVSOperation {type = QAVSOperationType.QueuedModInstall, name = "Mod install queued: " + fileName, operationId = operationId});
            installQueue.Add(new QueuedMod(path, fileName, operationId));
            InstallFirstModFromQueue();
        }

        public static void UninstallMod(string id)
        {
            int operationId = operations;
            operations++;
            runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.ModUninstall, name = "Unnstalling " + id, operationId = operationId });

            foreach (IMod m in modManager.AllMods)
            {
                if(m.Id == id)
                {
                    m.Uninstall();
                    modManager.ForceSave();
                    break;
                }
            }

            // {"success": true, "msg": "Success/Error: blah blah"}
            runningOperations[operationId].isDone = true;
        }

        public static void DeleteMod(string id)
        {
            int operationId = operations;
            operations++;
            runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.ModDelete, name = "Deleting " + id, operationId = operationId });
            foreach (IMod m in modManager.AllMods)
            {
                if (m.Id == id)
                {
                    modManager.DeleteMod(m);
                    modManager.ForceSave();
                    break;
                }
            }
            runningOperations[operationId].isDone = true;
        }

        public static void InstallModFromUrl(string url, string filename = "")
        {
            string extension = filename != "" ? Path.GetExtension(filename) : Path.GetExtension(url.Split('?')[0]);
            string fileName = filename != "" ? Path.GetFileNameWithoutExtension(filename) : "downloaded" + DateTime.Now.Ticks;
            if (extension == "") extension = ".qmod";
            string modPath = CoreService.coreVars.QAVSTmpModsDir + fileName + "-" + DateTime.Now.Ticks + extension;
            if(File.Exists(modPath)) File.Delete(modPath);
            DownloadManager m = new DownloadManager();
            int operationId = operations;
            operations++;
            runningOperations.Add(operationId, new QAVSOperation {type = QAVSOperationType.ModDownload, name = "Downloading mod: " + fileName, operationId = operationId});
            m.DownloadFinishedEvent += (manager) =>
            {
                //CoreService.browser.EvaluateJavascript("ShowToast('Downloaded, now installing', '#FFFFFF', '#222222')", null);
                Thread t = new Thread(() =>
                {
                    runningOperations[operationId].isDone = true;
                    InstallMod(modPath, fileName + extension);
                });
                t.Start();
            };
            m.DownloadCanceled += manager =>
            {
                runningOperations[operationId].isDone = true;
            };
            m.StartDownload(url, modPath);
            QAVSWebserver.managers.Add(m);
        }

        public static void EnableMod(string id)
        {
            int operationId = operations;
            operations++;
            runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.ModInstall, name = "Installing " + id, operationId = operationId });

            foreach (IMod m in modManager.AllMods)
            {
                if (m.Id == id)
                {
                    try
					{
						m.Install().Wait();
						modManager.ForceSave();
                        runningOperations[operationId].isDone = true;
					} catch(Exception e)
					{
                        runningOperations[operationId].isDone = true;
						operationId = operations;
						operations++;
						runningOperations.Add(operationId, new QAVSOperation { type = QAVSOperationType.Error, name = "Error enabling mod: " + e.Message + "\n\nTo remove this message restart QuestAppVersionSwitcher", operationId = operationId });
					}
                    break;
                }
            }
        }

        public static string GetMods()
        {
            return JsonSerializer.Serialize(GetModsAndLibs());
        }
        
        public static ModsAndLibs GetModsAndLibs()
        {
            return new ModsAndLibs
            {
                mods = modManager.Mods,
                libs = modManager.Libraries,
                operations = runningOperations.Values.ToList()
            };
        }
        

        public static string GetOperations()
        {
            return JsonSerializer.Serialize(runningOperations.Values);
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

        public static void DeleteAllMods()
        {
            FileManager.RecreateDirectoryIfExisting(modManager.ModsExtractPath);
            FolderPermission.DeleteDirectoryContent(modManager.ModsPath);
            FolderPermission.DeleteDirectoryContent(modManager.LibsPath);
            File.Delete(modManager.ConfigPath);
            modManager.Reset();
        }
    }
}