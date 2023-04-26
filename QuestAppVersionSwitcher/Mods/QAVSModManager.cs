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
                await modManager.LoadModsForCurrentApp();
            }
            catch (Exception e)
            {
                Logger.Log("Exception while loading mods for current app " + e, LoggingType.Error);
            }
            BroadcastModsAndStatus();
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
            AddRunningOperation(new QAVSOperation { type = QAVSOperationType.ModInstall, name = "Installing " + installQueue[0].filename, operationId = operationId});
            
            try
            {
                IMod mod = modManager.TryParseMod(installQueue[0].path).Result;
                UpdateOperationModId(operationId, mod.Id);
                mod.Install().Wait();
                MarkOperationAsDone(operationId);
            } catch (Exception e)
            {
                MarkOperationAsDone(operationId);
                MarkOperationAsError(operationId);
                operationId = operations;
                operations++;
                AddRunningOperation(new QAVSOperation { type = QAVSOperationType.Error, name = "Error installing mod: " + e.Message + "\n\nTo remove this message restart QuestAppVersionSwitcher", operationId = operationId, isDone = true, error = true});
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
            AddRunningOperation(new QAVSOperation {type = QAVSOperationType.QueuedModInstall, name = "Mod install queued: " + fileName, operationId = operationId});
            installQueue.Add(new QueuedMod(path, fileName, operationId));
            InstallFirstModFromQueue();
        }

        public static void UninstallMod(string id)
        {
            int operationId = operations;
            operations++;
            AddRunningOperation(new QAVSOperation { type = QAVSOperationType.ModUninstall, name = "Unnstalling " + id, operationId = operationId, modId = id});

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
            MarkOperationAsDone(operationId);
        }

        public static void DeleteMod(string id)
        {
            int operationId = operations;
            operations++;
            AddRunningOperation(new QAVSOperation { type = QAVSOperationType.ModDelete, name = "Deleting " + id, operationId = operationId, modId = id});
            foreach (IMod m in modManager.AllMods)
            {
                if (m.Id == id)
                {
                    modManager.DeleteMod(m);
                    modManager.ForceSave();
                    break;
                }
            }
            MarkOperationAsDone(operationId);
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
            AddRunningOperation(new QAVSOperation {type = QAVSOperationType.ModDownload, name = "Downloading mod: " + fileName, operationId = operationId});
            m.DownloadFinishedEvent += (manager) =>
            {
                //CoreService.browser.EvaluateJavascript("ShowToast('Downloaded, now installing', '#FFFFFF', '#222222')", null);
                Thread t = new Thread(() =>
                {
                    MarkOperationAsDone(operationId);
                    InstallMod(modPath, fileName + extension);
                });
                t.Start();
            };
            m.DownloadCanceled += manager =>
            {
                MarkOperationAsDone(operationId);
            };
            m.StartDownload(url, modPath);
            QAVSWebserver.managers.Add(m);
        }

        public static void EnableMod(string id)
        {
            int operationId = operations;
            operations++;
            AddRunningOperation(new QAVSOperation { type = QAVSOperationType.ModInstall, name = "Installing " + id, operationId = operationId, modId = id});

            foreach (IMod m in modManager.AllMods)
            {
                if (m.Id == id)
                {
                    try
					{
						m.Install().Wait();
						modManager.ForceSave();
                        MarkOperationAsDone(operationId);
					} catch(Exception e)
					{
                        MarkOperationAsDone(operationId);
                        MarkOperationAsError(operationId);
						operationId = operations;
						operations++;
						AddRunningOperation(new QAVSOperation { type = QAVSOperationType.Error, name = "Error enabling mod: " + e.Message + "\n\nTo remove this message restart QuestAppVersionSwitcher", operationId = operationId, isDone = true, error = true});
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
                Logger.Log(m.Id + " = " + id);
                if (m.Id == id)
                {
                    Logger.Log("found mod");
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