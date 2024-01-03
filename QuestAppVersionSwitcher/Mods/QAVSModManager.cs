using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ComputerUtils.Android;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Webserver;
using Java.Lang;
using Org.BouncyCastle.Asn1.Pkcs;
using AndroidX.Work;
using QuestAppVersionSwitcher.ClientModels;
using QuestPatcher.QMod;
using Exception = System.Exception;
using Logger = ComputerUtils.Android.Logging.Logger;

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
        public int taskId { get; set; } = 0;
        public string modId { get; set; } = "";
        public bool isDone { get; set; } = false;
        public bool error { get; set; } = false;
    }

    public class QAVSModManager
    {
        public static ModManager modManager;
        public static OtherFilesManager otherFilesManager;
        public static JsonSerializerOptions options;
        public static int operations = 0;
        public static int tasks = 0;
        public static Dictionary<int, QAVSOperation> runningOperations = new Dictionary<int, QAVSOperation>();
        
        public static void AddRunningOperation(QAVSOperation operation)
        {
            runningOperations.Add(operation.operationId, operation);
            BroadcastOperation(operation.operationId);
            //BroadcastModsAndStatus();
        }

        public static void MarkOperationAsError(int operationId)
        {
            runningOperations[operationId].error = true;
            BroadcastOperation(operationId);
            //BroadcastModsAndStatus();
        }
        public static void MarkOperationAsDone(int operationId)
        {
            runningOperations[operationId].isDone = true;
            if (runningOperations[operationId].type == QAVSOperationType.ModDelete
                || runningOperations[operationId].type == QAVSOperationType.ModDisable
                || runningOperations[operationId].type == QAVSOperationType.ModInstall
                || runningOperations[operationId].type == QAVSOperationType.ModUninstall)
            {
                // on mod install, uninstall, disable or delete, send all mods
                BroadcastModsAndStatus();
            }
            else
            {
                BroadcastOperation(operationId);
            }
        }

        public static void UpdateOperationModId(int operationId, string modId)
        {
            runningOperations[operationId].modId = modId;
            BroadcastOperation(operationId);
            //BroadcastModsAndStatus();
        }

        public static void BroadcastModsAndStatus()
        {
            QAVSWebserver.BroadcaseMessageOnWebSocket(new QAVSWebsocketMessage<ModsAndLibs>("/api/mods/mods", GetModsAndLibs()));
        }
        
        public static void BroadcastModloader()
        {
            QAVSWebserver.BroadcaseMessageOnWebSocket(new QAVSWebsocketMessage<ModLoaderResponse>("/api/mods/modloader", new ModLoaderResponse {modloader = modManager.usedModLoader, success = true}));
        }

        public static void BroadcastOperation(int operationId)
        {
            QAVSWebserver.BroadcaseMessageOnWebSocket(new QAVSWebsocketMessage<QAVSOperation>("/api/mods/operation/" + operationId, runningOperations[operationId]));
        }

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

        public static async void Update()
        {
            try
            {
                modManager.Reset();
                // Get modloader used
                ModdedJson json = PatchingManager.GetModdedJson();
                if (json != null)
                {
                    switch (json.modloaderName)
                    {
                        case "QuestLoader":
                            modManager.usedModLoader = ModLoader.QuestLoader;
                            break;
                        case "Scotland2":
                            modManager.usedModLoader = ModLoader.Scotland2;
                            break;
                        default:
                            modManager.usedModLoader = ModLoader.QuestLoader;
                            break;
                    }
                }

                PatchingStatus status = PatchingManager.GetPatchingStatus(CoreService.coreVars.currentApp);
                modManager.otherValidPackageIds.Clear();
                modManager.otherValidPackageIds.Add(status.copyOf);

                BroadcastModloader();
                await modManager.LoadModsForCurrentApp();
            }
            catch (Exception e)
            {
                Logger.Log("Exception while loading mods for current app " + e, LoggingType.Error);
            }
            BroadcastModsAndStatus();
        }

        public static int InstallMod(byte[] modBytes, string fileName, string cosmeticsType = "")
        {
            TempFile f = new TempFile(Path.GetExtension(fileName));
            File.WriteAllBytes(f.Path, modBytes);
            return InstallMod(f.Path, fileName, cosmeticsType);
        }

        public static bool installingMod = false;
        public static List<QueuedMod> installQueue = new List<QueuedMod>();

        public class QueuedMod
        {
            public string path;
            public string filename;
            public int queuedOperationId;
            public int taskId;

            public QueuedMod(string path, string filename, int operationId, int taskId)
            {
                this.path = path;
                this.filename = filename;
                queuedOperationId = operationId;
                this.taskId = taskId;
            }
        }

        public static void InstallFirstModFromQueue()
        {
            if (installingMod || installQueue.Count <= 0) return;
            runningOperations[installQueue[0].queuedOperationId].isDone = true;
            installingMod = true;
            int operationId = operations;
            operations++;
            AddRunningOperation(new QAVSOperation { type = QAVSOperationType.ModInstall, name = "Installing " + installQueue[0].filename, operationId = operationId, taskId = installQueue[0].taskId});
            string modId = "";
            string modName = "";
            try
            {
                IMod mod = modManager.TryParseMod(installQueue[0].path, installQueue[0].taskId).Result;
                modId = mod.Id;
                modName = mod.Name;
                UpdateOperationModId(operationId, mod.Id);
                mod.Install(installQueue[0].taskId).Wait();
                MarkOperationAsDone(operationId);
            } catch (Exception e)
            {
                MarkOperationAsDone(operationId);
                MarkOperationAsError(operationId);
                operationId = operations;
                operations++;
                if (modId != "")
                {
                    DeleteMod(modId);
                }
                AddRunningOperation(new QAVSOperation { type = QAVSOperationType.Error, name = "Error installing mod " + modName + ": " + e.Message, operationId = operationId, taskId = installQueue[0].taskId, isDone = true, error = true, modId = modId});
            }
            modManager.ForceSave();
            FileManager.DeleteFileIfExisting(installQueue[0].path);
            installQueue.RemoveAt(0);
            installingMod = false;
            InstallFirstModFromQueue();
        }

        public static int InstallMod(string path, string fileName, string cosmeticsType = "", int taskId = -1)
        {
            fileName = HttpServer.DecodeUrlString(fileName);
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
                return -1;
            }
            int operationId = operations;
            if (taskId == -1)
            {
                taskId = tasks;
                tasks++;
            }
            operations++;
            AddRunningOperation(new QAVSOperation {type = QAVSOperationType.QueuedModInstall, name = "Mod install queued: " + fileName, operationId = operationId, taskId = taskId});
            installQueue.Add(new QueuedMod(path, fileName, operationId, taskId));
            // Start the mod instal on a new thread so we can return the taskId before the mod is installed
            Thread t = new Thread(() =>
            {
                InstallFirstModFromQueue();
            });
            t.Start();
            return taskId;
        }

        public static int UninstallMod(string id)
        {
            int operationId = operations;
            int taskId = tasks;
            tasks++;
            operations++;
            AddRunningOperation(new QAVSOperation { type = QAVSOperationType.ModUninstall, name = "Unnstalling " + id, operationId = operationId, modId = id, taskId = taskId});
            // Start the mod uninstall on a new thread so we can return the taskId before the mod is installed
            Thread t = new Thread(() =>
            {
                foreach (IMod m in modManager.AllMods)
                {
                    if (m.Id == id)
                    {
                        try
                        {
                            m.Uninstall(taskId).Wait();
                            modManager.ForceSave();
                            MarkOperationAsDone(operationId);
                        }
                        catch (Exception e)
                        {
                            MarkOperationAsDone(operationId);
                            MarkOperationAsError(operationId);
                            operationId = operations;
                            operations++;
                            AddRunningOperation(new QAVSOperation
                            {
                                type = QAVSOperationType.Error,
                                name = "Error uninstalling mod " + m.Name + ": " + e.Message,
                                operationId = operationId, taskId = taskId, isDone = true, error = true
                            });
                        }

                        break;
                    }
                }
            });
            t.Start();
            return taskId;
        }

        public static int DeleteMod(string id)
        {
            int operationId = operations;
            int taskId = tasks;
            tasks++;
            operations++;
            AddRunningOperation(new QAVSOperation { type = QAVSOperationType.ModDelete, name = "Deleting " + id, operationId = operationId, modId = id, taskId = taskId});
            // Start the mod deletion on a new thread so we can return the taskId before the mod is installed
            Thread t = new Thread(() =>
            {
                foreach (IMod m in modManager.AllMods)
                {
                    if (m.Id == id)
                    {
                        try
                        {
                            modManager.DeleteMod(m, taskId).Wait();
                            modManager.ForceSave();
                            MarkOperationAsDone(operationId);
                        }
                        catch (Exception e)
                        {
                            MarkOperationAsDone(operationId);
                            MarkOperationAsError(operationId);
                            operationId = operations;
                            operations++;
                            AddRunningOperation(new QAVSOperation
                            {
                                type = QAVSOperationType.Error,
                                name = "Error deleting mod " + m.Name + ": " + e.Message,
                                operationId = operationId, taskId = taskId, isDone = true, error = true
                            });
                        }

                        break;
                    }
                }
            });
            t.Start();
            return taskId;
        }

        public static int InstallModFromUrl(string url, string filename = "")
        {
            string extension = filename != "" ? Path.GetExtension(filename) : Path.GetExtension(url.Split('?')[0]);
            string fileName = filename != "" ? Path.GetFileNameWithoutExtension(filename) : "downloaded" + DateTime.Now.Ticks;
            if (extension == "") extension = ".qmod";
            string modPath = CoreService.coreVars.QAVSTmpModsDir + fileName + "-" + DateTime.Now.Ticks + extension;
            if(File.Exists(modPath)) File.Delete(modPath);
            DownloadManager m = new DownloadManager();
            int operationId = operations;
            operations++;
            int taskId = tasks;
            tasks++;
            AddRunningOperation(new QAVSOperation {type = QAVSOperationType.ModDownload, name = "Downloading mod: " + fileName, taskId = taskId, operationId = operationId});
            m.DownloadFinishedEvent += (manager) =>
            {
                //CoreService.browser.EvaluateJavascript("ShowToast('Downloaded, now installing', '#FFFFFF', '#222222')", null);
                Thread t = new Thread(() =>
                {
                    MarkOperationAsDone(operationId);
                    InstallMod(modPath, fileName + extension, "", taskId);
                });
                t.Start();
            };
            m.DownloadCanceled += manager =>
            {
                MarkOperationAsDone(operationId);
            };
            m.StartDownload(url, modPath);
            QAVSWebserver.managers.Add(m);
            return taskId;
        }

        public static int EnableMod(string id)
        {
            int operationId = operations;
            int taskId = tasks;
            tasks++;
            operations++;
            AddRunningOperation(new QAVSOperation { type = QAVSOperationType.ModInstall, name = "Installing " + id, operationId = operationId, taskId = taskId, modId = id});
            // start the mod enabling on a new thread to return the Task id before mod install
            Thread t = new Thread(() =>
            {
                foreach (IMod m in modManager.AllMods)
                {
                    if (m.Id == id)
                    {
                        try
                        {
                            m.Install(taskId).Wait();
                            modManager.ForceSave();
                            MarkOperationAsDone(operationId);
                        }
                        catch (Exception e)
                        {
                            MarkOperationAsDone(operationId);
                            MarkOperationAsError(operationId);
                            operationId = operations;
                            operations++;
                            AddRunningOperation(new QAVSOperation
                            {
                                type = QAVSOperationType.Error,
                                name = "Error enabling mod " + m.Name + ": " + e.Message,
                                taskId = taskId, operationId = operationId, isDone = true, error = true, modId = id
                            });
                        }

                        break;
                    }
                }
            });
            t.Start();
            return taskId;
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

        public static void DeleteAllMods(bool deleteOnlyPersistent = false)
        {
            GeneralPurposeWorker.ExecuteWork(() =>
            {
                Logger.Log("Haha mods go brrrr, yeeeeeeeeeeeeeeeeeeeeeeeeeeet. We doin this on a background thread, hell yeah.");
                FileManager.RecreateDirectoryIfExisting(modManager.ModsExtractPath);
                if (modManager.usedModLoader == ModLoader.QuestLoader && !deleteOnlyPersistent)
                {
                    FolderPermission.DeleteDirectoryContent(modManager.QuestLoaderModsPath);
                    FolderPermission.DeleteDirectoryContent(modManager.QuestLoaderLibsPath);
                }
                if (modManager.usedModLoader == ModLoader.Scotland2 || deleteOnlyPersistent)
                {
                    FileManager.RecreateDirectoryIfExisting(modManager.Scotland2ModsPath);
                    FileManager.RecreateDirectoryIfExisting(modManager.Scotland2LibsPath);
                    FileManager.RecreateDirectoryIfExisting(modManager.Scotland2LateModsPath);
                }
                File.Delete(modManager.ConfigPath);
                modManager.Reset();
            });
        }
    }
}