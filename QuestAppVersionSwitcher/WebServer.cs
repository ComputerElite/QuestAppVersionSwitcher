using Android.Content.Res;
using ComputerUtils.Android.Logging;
using ComputerUtils.Android.Webserver;
using QuestAppVersionSwitcher.ClientModels;
using QuestAppVersionSwitcher.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ComputerUtils.Android.AndroidTools;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.VarUtils;
using ComputerUtils.Android.Encryption;
using QuestPatcher.Axml;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Android.Webkit;
using Xamarin.Essentials;
using CookieManager = Android.Webkit.CookieManager;
using Android.Content;
using ComputerUtils.Android;
using QuestAppVersionSwitcher.Mods;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Android.App;
using Android.OS;
using Android.Provider;
using Socket = System.Net.Sockets.Socket;
using Exception = System.Exception;
using String = System.String;
using Thread = System.Threading.Thread;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using ComputerUtils.Updating;
using Fleck;
using Java.Util;
using Newtonsoft.Json;
using QuestAppVersionSwitcher.DiffDowngrading;
using QuestPatcher.Zip;
using DownloadStatus = QuestAppVersionSwitcher.ClientModels.DownloadStatus;
using Environment = Android.OS.Environment;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Math = System.Math;
using Path = System.IO.Path;
using WebView = Android.Webkit.WebView;

namespace QuestAppVersionSwitcher
{
    public enum LoggedInStatus
    {
        NotLoggedIn = 0,
        SessionInvalid = 1,
        LoggedIn = 2
    }
    
    public class QAVSWebserver
    {
        HttpServer server = new HttpServer();
        ComputerUtils.Android.Webserver.WebsocketServer wsServer = new WebsocketServer();
        public static readonly char[] ReservedChars = new char[] { '|', '\\', '?', '*', '<', '&', '\'', ':', '>', '+', '[', ']', '/', '\'', ' ' };
        public static List<DownloadManager> managers = new List<DownloadManager>();
        public static List<GameDownloadManager> gameDownloadManagers = new List<GameDownloadManager>();
        public static SHA256 hasher = SHA256.Create();
        public static PatchStatus patchStatus = new PatchStatus();
        public static dynamic uiConfig = null;

        public LoggedInStatus GetLoggedInStatus()
        {
            if(CoreService.coreVars.token == "") return LoggedInStatus.NotLoggedIn;
            return LoggedInStatus.LoggedIn;
        }
        
        public static string GetRealPathFromURI(Android.Net.Uri uri)
        {
            return HttpServer.DecodeUrlString(uri.ToString()
                .Replace("content://com.android.externalstorage.documents/document/primary%3A", "/sdcard/"));
        }

        public static void BroadcaseMessageOnWebSocket<T>(QAVSWebsocketMessage<T> qavsWebsocketMessage)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                if (!clients[i].IsAvailable)
                {
                    clients.RemoveAt(i);
                    i--;
                    continue;
                }
                clients[i].Send(JsonConvert.SerializeObject(qavsWebsocketMessage, Formatting.None, new JsonSerializerSettings()
                { 
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }));
            }
        }
        
        public static DateTime lastBroadcast = DateTime.Now;
        
        public static void BroadcastDownloads(bool forceBroadcast)
        {
            if(lastBroadcast.AddSeconds(.2) > DateTime.Now && !forceBroadcast) return;
            lastBroadcast = DateTime.Now;
            DownloadStatus status = new DownloadStatus();
            foreach (DownloadManager m in managers)
            {
                status.individualDownloads.Add(m);
            }
            foreach (GameDownloadManager gdm in gameDownloadManagers)
            {
                status.gameDownloads.Add(gdm);
            }
            BroadcaseMessageOnWebSocket(new QAVSWebsocketMessage<DownloadStatus>("/api/downloads", status));
        }

        public static void BroadcastPatchingStatus()
        {
            BroadcaseMessageOnWebSocket(new QAVSWebsocketMessage<PatchStatus>("/api/patching/patchstatus", patchStatus));
        }
        
        public static void BroadcaseBackupStatus()
        {
            BroadcaseMessageOnWebSocket(new QAVSWebsocketMessage<BackupStatus>("/api/backupstatus", backupStatus));
        }
        public static void BroadcastConfig()
        {
            BroadcaseMessageOnWebSocket(new QAVSWebsocketMessage<StrippedConfig>("/api/questappversionswitcher/config", (StrippedConfig)CoreService.coreVars));
        }
        public static void BroadcastUIConfig()
        {
            BroadcaseMessageOnWebSocket(new QAVSWebsocketMessage<dynamic>("/api/questappversionswitcher/uiconfig", uiConfig));
        }
        
        public static List<IWebSocketConnection> clients = new List<IWebSocketConnection>();
        public static BackupStatus backupStatus = new BackupStatus();
        
        public void Start()
        {
            wsServer.OnMessage = (socket, msg) =>
            {
                Logger.Log("Recieved message from " + socket.ConnectionInfo.ClientIpAddress + ": " + msg);
            };
            wsServer.OnOpen = (socket) =>
            {
                clients.Add(socket);
                socket.Send("Hello from QuestAppVersionSwitcher!");
            };
            wsServer.OnClose = (socket) =>
            {
                clients.Remove(socket);
            };
            wsServer.StartServer(CoreService.coreVars.wsPort);
            server.AddRoute("GET", "/api/currentsha256", serverRequest =>
            {
                string apkLoc = AndroidService.FindAPKLocation(CoreService.coreVars.currentApp);
                byte[] hash;
                using (FileStream fileStream = File.OpenRead(apkLoc))
                {
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        hash = sha256.ComputeHash(fileStream);
                    }
                }
                serverRequest.SendString(GenericResponse.GetResponse(BitConverter.ToString(hash).Replace("-", "").ToLower(), true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/downloaddiff", serverRequest =>
            {
                DiffDownloadRequest r = JsonSerializer.Deserialize<DiffDownloadRequest>(serverRequest.bodyString);

                DiffDowngrader gdm = new DiffDowngrader(r);
                gameDownloadManagers.Add(gdm);
                gdm.StartDownload();
                ChangeApp(gdm.packageName);
                serverRequest.SendString(GenericResponse.GetResponse("Downloading!", true), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/proxy", request =>
            {
                HttpWebRequest r = new HttpWebRequest(new Uri(request.queryString.Get("url")));
                r.UserAgent = "QuestAppVersionSwitcher/" + CoreService.version;
                HttpWebResponse res = (HttpWebResponse)r.GetResponse();
                try
                {
                    Stream resStream = res.GetResponseStream();
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    foreach (string header in res.Headers.AllKeys)
                    {
                        if (header.ToLower() != "etag") continue;
                        headers.Add(header, res.Headers[header]);
                    }
                    request.ForwardStream(resStream, res.ContentLength, res.ContentType, res.ContentEncoding == "" ? Encoding.Default : Encoding.GetEncoding(res.ContentEncoding), (int)res.StatusCode, true, headers);
                }
                catch (Exception e)
                {
                    Logger.Log("Error in proxy:\n" + e.ToString(), LoggingType.Warning);
                    request.SendString("", "text/plain", 500);
                }
                return true;
            });
            server.AddRoute("POST", "/api/base64", request =>
            {
                string fileMimeType = request.queryString.Get("mime");
                Logger.Log("fileMimeType of blob: " + fileMimeType);
                string extension = ".qmod";
                if (fileMimeType == "application/qmod") extension = ".qmod"; // future proof for more supported mime types
                Regex regex = new Regex("^data:" + fileMimeType + ";base64,");
                byte[] bytes = Convert.FromBase64String( regex.Replace(request.bodyString, ""));
                QAVSModManager.InstallMod(bytes, "mod" + extension, "");
                return true;
            });
            server.AddRoute("GET", "/api/mods/mods", request =>
            {
                request.SendString(QAVSModManager.GetMods(), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/mods/modloader", request =>
            {
                request.SendString(ModLoaderResponse.GetResponse("", QAVSModManager.modManager.usedModLoader, true), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/mods/operation/", request =>
            {
                try
                {
                    int id = int.Parse(request.pathDiff);
                    request.SendString(JsonSerializer.Serialize(QAVSModManager.runningOperations[id]), "application/json");
                }
                catch (Exception e)
                {
                    request.SendString("{}", "application/json", 404);
                }
                return true;
            });
            server.AddRoute("GET", "/api/mods/operations", request =>
            {
                request.SendString(QAVSModManager.GetOperations(), "application/json");
                return true;
            });
            server.AddRoute("DELETE", "/api/mods/operation", request =>
            {
                int operation = int.Parse(request.bodyString);
                if (!QAVSModManager.runningOperations.ContainsKey(operation))
                {
                    request.SendString(GenericResponse.GetResponse("A operation with the key " + operation + " does not exist", false), "application/json", 400);
                    return true;
                }

                QAVSModManager.runningOperations.Remove(operation);
                QAVSModManager.BroadcastModsAndStatus();
                request.SendString(GenericResponse.GetResponse("Removed operation " + operation + " from running Operations", true), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/patching/patchoptions", request =>
            {
                request.SendString(JsonSerializer.Serialize(CoreService.coreVars.patchingPermissions), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/patching/patchoptions", request =>
            {
                CoreService.coreVars.patchingPermissions = JsonSerializer.Deserialize<PatchingPermissions>(request.bodyString);
                request.SendString(GenericResponse.GetResponse("Set patch options", true), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/mods/operations", request =>
            {
                request.SendString(JsonSerializer.Serialize(QAVSModManager.runningOperations), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/install", request =>
            {
                string typeid = request.queryString.Get("typeid") ?? "";
                QAVSModManager.InstallMod(request.bodyBytes, request.queryString.Get("filename"), typeid);
                request.SendString(GenericResponse.GetResponse("Trying to install. Check running operations for status", true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/mods/installfromurl", request =>
            {
                int id = QAVSModManager.InstallModFromUrl(request.bodyString);
                request.SendString(ModResponse.GetResponse("Trying to install from " + request.bodyString, true, id), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/mods/cover/", request =>
            {
                Logger.Log("Getting cover of " + request.pathDiff);
                request.SendData(QAVSModManager.GetModCover(request.pathDiff), "image/xyz");
                return true;
            }, true, true, true, false, 0, true, 3600); // cache for 1 hours on client
            server.AddRoute("POST", "/api/mods/uninstall", request =>
            {
                int id = QAVSModManager.UninstallMod(request.queryString.Get("id"));
                request.SendString(ModResponse.GetResponse("Trying to uninstall", true, id), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/mods/enable", request =>
            {
                int id = QAVSModManager.EnableMod(request.queryString.Get("id"));
                request.SendString(ModResponse.GetResponse("Trying to enable", true, id), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/mods/delete", request =>
            {
                int id = QAVSModManager.DeleteMod(request.queryString.Get("id"));
                request.SendString(ModResponse.GetResponse("Trying to delete", true, id), "application/json");
                return true;
            }); 
            server.AddRoute("GET", "/api/patching/getmodstatus", request =>
            {
                string package = request.queryString.Get("package");
                string backup = request.queryString.Get("backup");
                PatchingStatus status;
                if (backup != null && backup != "")
                {
                    status = PatchingManager.GetPatchingStatusOfBackup(package, backup);
                }
                else
                {
                    // Check installed apk
                    status = PatchingManager.GetPatchingStatus(package);
                }
                request.SendString(JsonSerializer.Serialize(status), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/mods/deleteallmods", request =>
            {
                QAVSModManager.DeleteAllMods();
                request.SendString(GenericResponse.GetResponse("Deleted all mods", true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/patching/patchapk", request =>
            {
                patchStatus = new PatchStatus();
                patchStatus.totalOperations = 9;
                patchStatus.currentOperation = "Copying APK. This can take a bit";
                string package = request.queryString.Get("package");
                string backup = request.queryString.Get("backup");
                string apkPath = "";
                if (package != null && backup != null)
                {
                    
                    Logger.Log("Trying to patch apk from backup " + package + " - " + backup);
                    string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backup + "/";
                    if (!Directory.Exists(backupDir))
                    {
                        request.SendString(GenericResponse.GetResponse("Backup does not exist", false), "application/json", 400);
                        return true;
                    }
                    if (!File.Exists(backupDir + "app.apk"))
                    {
                        request.SendString(GenericResponse.GetResponse("Backup doesn't contain apk", false), "application/json", 400);
                        return true;
                    }
                    apkPath = backupDir + "app.apk";
                }
                else
                {
                    if (!AndroidService.IsPackageInstalled(CoreService.coreVars.currentApp))
                    {
                        request.SendString(GenericResponse.GetResponse(CoreService.coreVars.currentApp + "is not installed. Please install it", true), "application/json", 202);
                        return true;
                    }

                    apkPath = AndroidService.FindAPKLocation(CoreService.coreVars.currentApp);
                }         
                request.SendString(GenericResponse.GetResponse("Acknowledged. Check status at /patching/patchstatus", true), "application/json", 202);

                BroadcastPatchingStatus();
                Logger.Log("Using apk at  " + apkPath);
                try
                {
                    string appLocation = CoreService.coreVars.QAVSTmpPatchingDir + "app.apk";
                    FileManager.RecreateDirectoryIfExisting(CoreService.coreVars.QAVSTmpPatchingDir);
                    File.Copy(apkPath, appLocation);
                    
                    Stream apkStream = File.Open(appLocation, FileMode.Open);
                    ApkZip apk = ApkZip.Open(apkStream);
                    patchStatus.doneOperations = 1;
                    patchStatus.progress = .1;
                    BroadcastPatchingStatus();
                    Thread t = new Thread(() =>
                    {
                        PatchingManager.PatchAPK(apk, appLocation, request.queryString.Get("force") != null);
                    });
                    t.Start();
                }
                catch (Exception e)
                {
                    patchStatus.error = true;
                    patchStatus.errorText = "Error while patching:" + e;
                    BroadcastPatchingStatus();
                }
                return true;
            });
            server.AddRoute("GET", "/api/patching/patchstatus", serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(patchStatus), "application/json", 200);
                return true;
            });

            server.AddRoute("GET", "/api/questappversionswitcher/uiconfig", request =>
            {
                request.SendString(JsonConvert.SerializeObject(uiConfig), "application/json");
                return true;
            });server.AddRoute("POST", "/api/questappversionswitcher/uiconfig", request =>
            {
                uiConfig = JsonConvert.DeserializeObject(request.bodyString);
                File.WriteAllText(CoreService.coreVars.QAVSUIConfigLocation, JsonConvert.SerializeObject(uiConfig));
                BroadcastUIConfig();
                request.SendString(GenericResponse.GetResponse("Set UI config", true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/questappversionswitcher/kill", request =>
            {
                CookieManager.Instance.Flush();
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return true;
            });
            server.AddRoute("GET", "/api/questappversionswitcher/loggedinstatus", request =>
            {
                request.SendString(GenericResponse.GetResponse(((int)GetLoggedInStatus()).ToString(), true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/questappversionswitcher/changeport", request =>
            {
                int port = Convert.ToInt32(request.bodyString);
                if(port < 50000)
                {
                    request.SendString(GenericResponse.GetResponse("Port must be greater than 50000!", false), "application/json", 400);
                    return true;
                }
                if(port > 60000)
                {
                    request.SendString(GenericResponse.GetResponse("Port must be less than 60000!", false), "application/json", 400);
                    return true;
                }
                if (CoreService.coreVars.wsPort == port)
                {
                    request.SendString(GenericResponse.GetResponse("Port must not be the same as the websocket port", false), "application/json", 400);
                    return true;
                }
                CoreService.coreVars.serverPort = port;
                CoreService.coreVars.Save();
                request.SendString(GenericResponse.GetResponse("Changed port to " + request.bodyString + ". Restart QuestAppVersionSwitcher for the changes to take affect.", true), "application/json");
                return true;
            });
			/* FS loading for dev if wanted
            server.AddRoute("GET", "/script.js", new Func<ServerRequest, bool>(request =>
			{
				request.SendFileFS(CoreService.coreVars.QAVSDir + "script.js");
				return true;
            }));
			server.AddRoute("GET", "/style.css", new Func<ServerRequest, bool>(request =>
			{
				request.SendFileFS(CoreService.coreVars.QAVSDir + "style.css");
				return true;
			}));
			server.AddRoute("GET", "/", new Func<ServerRequest, bool>(request =>
			{
				request.SendFileFS(CoreService.coreVars.QAVSDir + "index.html");
				return true;
			}));
            */
			server.AddRoute("GET", "/api/cosmetics/types", request =>
            {
                string game = request.queryString.Get("game");
                if (game == null) game = CoreService.coreVars.currentApp;
                request.SendString(JsonSerializer.Serialize(CoreVars.cosmetics.GetCosmeticsGame(game)), "application/json");
                return true;
            });
			server.AddRoute("GET", "/api/cosmetics/getinstalled", request =>
            {
                string game = request.queryString.Get("game");
                if (game == null) game = CoreService.coreVars.currentApp;
                string typeid = request.queryString.Get("typeid");
                if (typeid == null)
                {
                    request.SendString(GenericResponse.GetResponse("No type id specified", false), "application/json", 400);
                    return true;
                }
                
                request.SendString(JsonSerializer.Serialize(CoreVars.cosmetics.GetInstalledCosmetics(game, typeid)), "application/json");
                return true;
            });
			server.AddRoute("DELETE", "/api/cosmetics/delete", request =>
            {
                string game = request.queryString.Get("game");
                if (game == null) game = CoreService.coreVars.currentApp;
                string typeid = request.queryString.Get("typeid");
                if (typeid == null)
                {
                    request.SendString(GenericResponse.GetResponse("No type id specified", false), "application/json", 400);
                    return true;
                }
                string filename = HttpServer.DecodeUrlString(request.queryString.Get("filename"));
                if (filename == null)
                {
                    request.SendString(GenericResponse.GetResponse("No filename specified", false), "application/json", 400);
                    return true;
                }
                CoreVars.cosmetics.RemoveCosmetic(game, typeid, filename);
                request.SendString(GenericResponse.GetResponse("Deleted", true), "application/json");
                return true;
            });

			server.AddRouteFile("/", "html/index.html");
            server.AddRouteFile("/scotlandforever.mp3", "html/scotlandforever.mp3");
			server.AddRouteFile("/setup", "html/setup.html");
            server.AddRouteFile("/flows/beat_saber_modding", "html/flows/beat_saber_modding.html");
            server.AddRouteFile("/inject.js", "html/qavs_inject.js", new Dictionary<string, string> { {"{0}", CoreService.coreVars.serverPort.ToString() } });
            server.AddRouteFile("/script.js", "html/script.js");
            server.AddRouteFile("/hiddenApps.json", "html/hiddenApps.json");
            server.AddRouteFile("/style.css", "html/style.css");
            server.AddRouteFile("/newstyle.css", "html/newstyle.css");
            server.AddRoute("GET", "/api/android/installedapps", serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(AndroidService.GetInstalledApps()), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/android/device", serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(new AndroidDevice()
                {
                    sdkVersion = (int)Build.VERSION.SdkInt,
                    device = Build.Device,
                    freeSpace = Environment.ExternalStorageDirectory.UsableSpace,
                    totalSpace = Environment.ExternalStorageDirectory.TotalSpace,
                }), "application/json");
                return true;
            });
			server.AddRoute("POST", "/api/android/launch", serverRequest =>
            {
                serverRequest.SendString(GenericResponse.GetResponse("Launching " + CoreService.coreVars.currentApp, true), "application/json");

                AndroidService.LaunchApp(CoreService.coreVars.currentApp);
                Timer t = new Timer();
                t.Schedule(new LaunchAppTask(), 2000);
                return true;
            });
			server.AddRoute("GET", "/api/android/installedappsandbackups", serverRequest =>
            {
                List<App> apps = AndroidService.GetInstalledApps();

                foreach (string f in Directory.GetDirectories(CoreService.coreVars.QAVSBackupDir))
                {
                    if (apps.FirstOrDefault(x => x.PackageName == Path.GetFileName(f)) == null)
                    {
                        apps.Add(new App("unknown", Path.GetFileName(f)));
                    }
                }
                serverRequest.SendString(JsonSerializer.Serialize(apps), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/android/getpackagelocation", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("package key needed", false), "application/json", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                string location = AndroidService.FindAPKLocation(package);
                if (location == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("package not found", false), "application/json", 404);
                }
                else
                {
                    serverRequest.SendString(GenericResponse.GetResponse(location, false), "application/json");
                }
                return true;
            });
            server.AddRoute("POST", "/api/android/uninstallpackage", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("package key needed", false), "application/json", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                if (!AndroidService.IsPackageInstalled(package) && serverRequest.queryString.Get("force") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("App is already uninstalled", true), "application/json", 230);
                    return true;
                }
                AndroidService.InitiateUninstallPackage(package);
                serverRequest.SendString(GenericResponse.GetResponse("Uninstall request sent", true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/questappversionswitcher/uploadlogs", request =>
            {
                Logger.Log("\n\n------Log upload requested------");
                QAVSReport report = new QAVSReport();
                report.androidVersion = (int)Build.VERSION.SdkInt;
                report.version = CoreService.version.ToString();
                report.modloaderMode = QAVSModManager.modManager.usedModLoader.ToString();
                report.userIsLoggedIn = GetLoggedInStatus() == LoggedInStatus.LoggedIn;
                report.reportTime = DateTime.Now;
                report.device = Build.Device;
                report.availableSpace = Environment.ExternalStorageDirectory.UsableSpace;
                QAVSModManager.Update();
                report.modsAndLibs = QAVSModManager.GetModsAndLibs();
                PatchingStatus status = PatchingManager.GetPatchingStatus();
                report.appStatus = status;
                Logger.Log("-------Status of selected app-------\n" + (status == null ? "Not installed" : JsonSerializer.Serialize(status, new JsonSerializerOptions
                {
                    WriteIndented = true
                })));
                string password = request.bodyString;
                if (password == "") password = AndroidService.GetDeviceID();

                if (report.userIsLoggedIn)
                {
                    try
                    {
                        if (GetSHA256OfString(password) != CoreService.coreVars.password)
                        {
                            request.SendString(GenericResponse.GetResponse("Password is wrong. Please try a different password or set a new one", false), "application/json", 403);
                            return true;
                        }
                        GraphQLClient.log = false;
                        GraphQLClient.oculusStoreToken = PasswordEncryption.Decrypt(CoreService.coreVars.token, password);
                        ViewerData<OculusUserWrapper> entitlements = GraphQLClient.GetActiveEntitelments();
                        foreach (Entitlement e in entitlements.data.viewer.user.active_entitlements.nodes)
                        {
                            report.userEntitlements.Add(e.id);
                        }
                    } catch
                    {
                        
                    }
                }
                Logger.Log("---Backups---");
                foreach (string app in Directory.GetDirectories(CoreService.coreVars.QAVSBackupDir))
                {
                    Logger.Log(Path.GetFileName(app));
                    foreach (string backup in Directory.GetDirectories(app))
                    {
                        Logger.Log("├── " + Path.GetFileName(backup));
                        foreach (string file in Directory.GetFiles(backup))
                        {
                            Logger.Log("|  ├── " + Path.GetFileName(file));
                        }
                        foreach (string dir in Directory.GetDirectories(backup))
                        {
                            if (dir == "obb")
                            {
                                FileManager.LogTree(dir, 2);
                            }
                            else
                            {
                                Logger.Log("|  ├── " + Path.GetFileName(dir));
                                Logger.Log("|  |  ├── Directory contents will not be shown");
                            }
                        }
                    }
                }
                //FileManager.LogTree(CoreService.coreVars.QAVSBackupDir, 0);
                report.log = FileManager.GetLastCharactersOfFile(CoreService.coreVars.QAVSDir + "qavslog.log", 2 * 1024 * 1024); // 2 MB
                WebRequest r = WebRequest.Create("https://oculusdb.rui2015.me/api/v1/qavsreport");
                r.Method = "POST";
                byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(report));
                r.GetRequestStream().Write(bytes, 0, bytes.Length);
                string id = new StreamReader(r.GetResponse().GetResponseStream()).ReadToEnd();
                request.SendString(GenericResponse.GetResponse(id, true), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/android/ispackageinstalled", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(IsAppInstalled.GetResponse("package key needed", false, false), "application/json", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                serverRequest.SendString(IsAppInstalled.GetResponse("", AndroidService.IsPackageInstalled(package), true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/questappversionswitcher/changeapp", serverRequest =>
            {
                ChangeAppRequest request = JsonSerializer.Deserialize<ChangeAppRequest>(serverRequest.bodyString);
                ChangeApp(request.packageName, request.name);
                serverRequest.SendString(GenericResponse.GetResponse("App changed to " + request.packageName, true), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/questappversionswitcher/config", serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize((StrippedConfig)CoreService.coreVars), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/questappversionswitcher/about", serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(new About { browserIPs = server.ips, version = CoreService.version.ToString() }), "application/json");
                return true;
            });
			server.AddRoute("POST", "/api/android/uploadandinstallapk", serverRequest =>
            {
                TempFile tmpFile = new TempFile();
                tmpFile.Path += ".apk";
                File.WriteAllBytes(tmpFile.Path, serverRequest.bodyBytes);
                string packageName = GetAPKPackageName(tmpFile.Path);
                string version = GetAPKVersion(tmpFile.Path);
                ChangeApp(packageName);
                string backupDir = CoreService.coreVars.QAVSBackupDir + packageName + "/" + version + "/";
                Logger.Log("Moving file");
                FileManager.CreateDirectoryIfNotExisting(backupDir);
                FileManager.DeleteFileIfExisting(backupDir + "app.apk");
                File.Move(tmpFile.Path, backupDir + "app.apk");

                serverRequest.SendString(GenericResponse.GetResponse("uploaded and selected app in backup tab", true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/android/installapkfromdisk", serverRequest => { 
                Intent chooseFile = new Intent(Intent.ActionGetContent);
                chooseFile.SetType("application/vnd.android.package-archive");
                chooseFile = Intent.CreateChooser(chooseFile, "Choose an apk to install");
                Logger.Log("Opening file picker for apk install");
                
                
                Action<Result, Intent> callback = (resultCode, intentData) =>
                {
                    if (resultCode != Result.Ok) return;
                    
                    // Get the URI of the selected file
                    Android.Net.Uri uri = intentData.Data;
                    Logger.Log(uri.ToString());

                    // Convert the URI to a file path
                    string path = GetRealPathFromURI(uri);

                    // Start apk install
                    Logger.Log("Selected apk for installation: " + path);
                    AndroidService.InitiateInstallApk(path);
                };
                
                int newRequestCode = ActivityResultCallbackRegistry.RegisterActivityResultCallback(callback);

                CoreService.mainActivity.StartActivityForResult(chooseFile, newRequestCode);
                serverRequest.SendString(GenericResponse.GetResponse("Opened file picker", true), "application/json");
                return true;
            });
			server.AddRoute("GET", "/api/backups", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("package key needed", false), "application/json", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                if (!IsNameFileNameSafe(package))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("You package contains a forbidden character. You can not backup it. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", false), "application/json", 400);
                    return true;
                }
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/";
                if (Directory.Exists(backupDir))
                {

                    serverRequest.SendString(JsonSerializer.Serialize(BackupManager.GetBackups(package)), "application/json");
                }
                else
                {
                    serverRequest.SendString(JsonSerializer.Serialize(new BackupList()), "application/json");
                }
                return true;
            });
            server.AddRoute("POST", "/api/backup", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("package key needed", false), "application/json", 400);
                    return true;
                }
                if (serverRequest.queryString.Get("backupname") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("backupname key needed", false), "application/json", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                string backupname = serverRequest.queryString.Get("backupname");
                if (!IsNameFileNameSafe(backupname))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("Your Backup name contains a forbidden character. Please remove them. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", false), "application/json", 400);
                    return true;
                }
                if (!IsNameFileNameSafe(package))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("Your package contains a forbidden character. You can not backup it. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", false), "application/json", 400);
                    return true;
                }
                if (backupname == "")
                {
                    serverRequest.SendString(GenericResponse.GetResponse("Your backup has to have a name. Please add one.", false), "application/json", 400);
                    return true;
                }
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                if (Directory.Exists(backupDir))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("A Backup with this name already exists. Please choose a different name", false), "application/json", 400);
                    return true;
                }
                Logger.Log("Creating backup in " + backupDir + " for " + package);
                serverRequest.SendString(GenericResponse.GetResponse("Creating Backup. Please wait until it has finished. This can take up to 2 minutes", true), "application/json", 202);
                backupStatus = new BackupStatus();
                backupStatus.currentOperation = "Creating Backup. Please wait until it has finished. This can take up to 2 minutes";
                backupStatus.totalOperations = 4;
                BroadcaseBackupStatus();
                Directory.CreateDirectory(backupDir);
                if (!AndroidService.IsPackageInstalled(package))
                {
                    Logger.Log(package + " is not installed. Aborting backup");
                    backupStatus.errorText = package + " is not installed. Please select a different app.";
                    backupStatus.error = true;
                    BroadcaseBackupStatus();
                    return true;
                }
                string apkDir = AndroidService.FindAPKLocation(package);
                string gameDataDir = CoreService.coreVars.AndroidAppLocation + package;
                

                try
                {
                    backupStatus.progress = .1;
                    backupStatus.doneOperations = 1;
                    if (serverRequest.queryString.Get("onlyappdata") == null)
                    {
                        backupStatus.currentOperation = "Copying APK. Please wait until it has finished. This can take up to 2 minutes";
                        BroadcaseBackupStatus();
                        Logger.Log("Copying APK from " + apkDir + " to " + backupDir + "app.apk");
                        File.Copy(apkDir, backupDir + "app.apk");
                    } else
                    {
                        Logger.Log("Only backing up app data. Skipping apk");
                        File.WriteAllText(backupDir + "onlyappdata.txt", "This backup only contains app data.");
                    }
                    backupStatus.doneOperations = 2;
                    backupStatus.progress = .4;
                    backupStatus.currentOperation = "Copying App Data. Please wait until it has finished. This can take up to 2 minutes";
                    BroadcaseBackupStatus();
                    try
                    {
                        if(Directory.Exists(gameDataDir)) FolderPermission.DirectoryCopy(gameDataDir, backupDir + package);
                    }
                    catch (Exception e)
                    {
                        backupStatus.errorText = e.ToString();
                        backupStatus.error = true;
                        BroadcaseBackupStatus();
                        return true;
                    }
                    backupStatus.doneOperations = 3;
                    backupStatus.progress = .6;
                    BroadcaseBackupStatus();

                    if (Directory.Exists(CoreService.coreVars.AndroidObbLocation + package))
                    {
                        backupStatus.currentOperation = "Copying Obbs. Please wait until it has finished. This can take up to 2 minutes";
                        BroadcaseBackupStatus();
                        Directory.CreateDirectory(backupDir + "obb/" + package);
                        FolderPermission.DirectoryCopy(CoreService.coreVars.AndroidObbLocation + package, backupDir + "obb/" + package);
                    }
                    backupStatus.doneOperations = 4;
                    backupStatus.progress = 1;
                    BroadcaseBackupStatus();
                }
                catch (Exception e)
                {
                    Logger.Log("Backup failed: " + e);
                    backupStatus.errorText = "Backup failed: " + e;
                    BroadcaseBackupStatus();
                    return true;
                }

                BackupManager.GetBackupInfo(backupDir, true); // make sure backup metadata is up to date

                backupStatus.done = true;
                backupStatus.currentOperation = "Backup of " + package + " with the name " + backupname + " finished";
                BroadcaseBackupStatus();
                return true;
            });
            server.AddRoute("GET", "/api/backupstatus", serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(backupStatus), "application/json", 200);
                return true;
            });
            server.AddRoute("DELETE", "/api/backup", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("package key needed", false), "application/json", 400);
                    return true;
                }
                if (serverRequest.queryString.Get("backupname") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("backupname key needed", false), "application/json", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                string backupname = serverRequest.queryString.Get("backupname");
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                if (!Directory.Exists(backupDir))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("The Backup you want to delete doesn't exist.", false), "application/json", 400);
                }
                Directory.Delete(backupDir, true);
                serverRequest.SendString(GenericResponse.GetResponse("Deleted " + backupname + " of " + package, true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/restoreapp", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("package key needed", false), "application/json", 400);
                    return true;
                }
                if (serverRequest.queryString.Get("backupname") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("backupname key needed", false), "application/json", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                string backupname = serverRequest.queryString.Get("backupname");
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                if (!Directory.Exists(backupDir))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("This backup doesn't exist", false), "application/json", 400);
                    return true;
                }
                if (!File.Exists(backupDir + "app.apk"))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("Critical: APK doesn't exist in Backup. This Backup is useless. Please restart the app and choose a different one.", false), "application/json", 500);
                    return true;
                }

                Logger.Log("Installing apk of backup " + backupname + " of " + package + " and clearing it's mod data");
                AndroidService.InitiateInstallApk(backupDir + "app.apk");
                QAVSModManager.DeleteAllMods(true);
                Thread checkApkThread = new Thread(() =>
                {
                    Logger.Log("Starting thread for checking if apk is installed and then reloading mods");
                    DateTime startTime = DateTime.Now;
                    while (DateTime.Now - new TimeSpan(0, 0, 20, 0) < startTime)
                    {
                        if (AndroidService.IsPackageInstalled(CoreService.coreVars.currentApp))
                        {
                            Logger.Log("Apk finally installed. Reloading mods");
                            QAVSModManager.Update();
                            break;
                        }
                        Thread.Sleep(5000);
                    }
                    Logger.Log("thread for checking if apk is installed and then reloading mods timed out.");
                });
                checkApkThread.Start();
                serverRequest.SendString(GenericResponse.GetResponse("Started apk install. Deleting existing mods in parallel", true), "application/json");
                QAVSModManager.DeleteAllMods();
                return true;
            });
            server.AddRoute("GET", "/api/backupinfo", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("package key needed", false), "application/json", 400);
                    return true;
                }
                if (serverRequest.queryString.Get("backupname") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("backupname key needed", false), "application/json", 400);
                    return true;
                }

                string package = serverRequest.queryString.Get("package");
                string backupname = serverRequest.queryString.Get("backupname");
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                if (!Directory.Exists(backupDir))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("This backup doesn't exist", false), "application/json", 400);
                    return true;
                }

                
                serverRequest.SendString(JsonSerializer.Serialize(BackupManager.GetBackupInfo(backupDir)), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/grantaccess", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("package key needed", false), "application/json", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2)
                {
                    FolderPermission.openDirectory(Environment.ExternalStorageDirectory.AbsolutePath + "/Android/data");
                    FolderPermission.openDirectory(Environment.ExternalStorageDirectory.AbsolutePath + "/Android/obb");
                }
                else
                {
                    FolderPermission.openDirectory(Environment.ExternalStorageDirectory.AbsolutePath + "/Android/data/" + package);
                    FolderPermission.openDirectory(Environment.ExternalStorageDirectory.AbsolutePath + "/Android/obb/" + package);
                }
                serverRequest.SendString(GenericResponse.GetResponse("Opened folder permission dialogues", true), "application/json", 200);
                return true;
            });
            server.AddRoute("GET", "/api/game/splashcover", request =>
            {
                string package = request.queryString.Get("package") ?? CoreService.coreVars.currentApp;
                string backupname = request.queryString.Get("backupname") ?? "";
                string apkLocation = AndroidService.FindAPKLocation(package);
                if (backupname != "")
                {
                    
                    string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                    if (!Directory.Exists(backupDir))
                    {
                        request.SendString(GenericResponse.GetResponse("This backup does not exist", false), "application/json", 404);
                        return true;
                    }

                    apkLocation = backupDir + "app.apk";
                    if (!File.Exists(apkLocation))
                    {
                        request.SendString(GenericResponse.GetResponse("This backup does not have an apk file", false),
                            "application/json", 404);
                        return true;
                    }
                }
                byte[] data = PatchingManager.GetSplashCover(apkLocation);
                request.SendData(data, "image/png", data == null ? 404 : 200);
                return true;
            });
            server.AddRoute("GET", "/api/gotaccess", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(GotAccess.GetResponse("package key needed", false, false), "application/json", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
                {
                    serverRequest.SendString(GotAccess.GetResponse("Android 10 doesn't need this check", true, true),
                        "application/json");

                }
                else if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2)
                {
                    bool gotAccess =
                        FolderPermission.GotAccessTo(
                            Environment.ExternalStorageDirectory.AbsolutePath + "/Android/obb") &&
                        FolderPermission.GotAccessTo(
                            Environment.ExternalStorageDirectory.AbsolutePath + "/Android/data");
                    serverRequest.SendString(GotAccess.GetResponse("", gotAccess, true), "application/json");
                }
                else
                {
                    bool gotAccess =
                        FolderPermission.GotAccessTo(Environment.ExternalStorageDirectory.AbsolutePath +
                                                     "/Android/obb/" + package) &&
                        FolderPermission.GotAccessTo(Environment.ExternalStorageDirectory.AbsolutePath +
                                                     "/Android/data/" + package);
                    serverRequest.SendString(GotAccess.GetResponse("", gotAccess, true), "application/json");
                }
                return true;
            });
            server.AddRoute("GET", "/api/hasmanagestorageappaccess", request =>
            {
                if (request.queryString.Get("package") == null)
                {
                    request.SendString(GotAccess.GetResponse("package key needed", false, false), "application/json", 400);
                    return true;
                }
                string package = request.queryString.Get("package");
                request.SendString(GotAccess.GetResponse("", AndroidService.HasManageExternalStoragePermission(package), true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/grantmanagestorageappaccess", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("package key needed", false), "application/json", 400);
                    return true;
                }

                if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
                {
                    // Not needed on A10 and below
                    serverRequest.SendString(GenericResponse.GetResponse("Not needed on A10. Continue as normal", true), "application/json", 200);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                Intent intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission, Android.Net.Uri.Parse("package:" + package));
                AndroidCore.context.StartActivity(intent);
                serverRequest.SendString(GenericResponse.GetResponse("opened settings", true), "application/json", 200);
                return true;
            });
            server.AddRoute("POST", "/api/restoregamedata", serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("package key needed", false), "application/json", 400);
                    return true;
                }
                if (serverRequest.queryString.Get("backupname") == null)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("backupname key needed", false), "application/json", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                string backupname = serverRequest.queryString.Get("backupname");
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                if (!Directory.Exists(backupDir))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("This backup doesn't exist", false), "application/json", 400);
                    return true;
                }
                if (!AndroidService.IsPackageInstalled(package))
                {
                    serverRequest.SendString(GenericResponse.GetResponse(package + " is not installed. Cannot restore game data", false), "application/json", 400);
                    return true;
                }
                string gameDataDir = CoreService.coreVars.AndroidAppLocation + package;
                
                if (Directory.Exists(backupDir + "obb/" + package))
                {
                    try
                    {
                        Logger.Log("Copying obbs of backup " + backupname + " of " + package);
                        FolderPermission.DirectoryCopy(backupDir + "obb/" + package, CoreService.coreVars.AndroidObbLocation + package);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e.ToString(), LoggingType.Error);
                        serverRequest.SendString(GenericResponse.GetResponse("Obbs of " + package + " were unable to get restored: " + e, false), "application/json", 500);
                        return true;
                    }
                }
                if (Directory.Exists(backupDir + package))
                {
                    try
                    {
                        Logger.Log("Copying appdata of backup " + backupname + " of " + package);
                        FolderPermission.DirectoryCopy(backupDir + package, gameDataDir);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e.ToString(), LoggingType.Error);
                        serverRequest.SendString(GenericResponse.GetResponse("App data of " + package + " was unable to get restored: " + e, false), "application/json", 500);
                        return true;
                    }
                }
                
                serverRequest.SendString(GenericResponse.GetResponse("Game data restored", true), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/allbackups", serverRequest =>
            {
                serverRequest.SendString(GenericResponse.GetResponse(SizeConverter.ByteSizeToString(FileManager.GetDirSize(CoreService.coreVars.QAVSBackupDir)), false), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/logout", request =>
            {
                CoreService.coreVars.token = "";
                CoreService.coreVars.password = "";
                CoreService.coreVars.Save();
                request.SendString(GenericResponse.GetResponse("Logged out", true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/token", serverRequest =>
            {
                TokenRequest r = JsonSerializer.Deserialize<TokenRequest>(serverRequest.bodyString);
                if (r.token.Contains("%"))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("You got your token from the wrong place. Go to the payload tab. Don't get it from the url.", false), "application/json", 400);
                    return true;
                }
                if (!r.token.StartsWith("OC"))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("Tokens must start with 'OC'. Please get a new one", false), "application/json", 400);
                    return true;
                }
                if (r.token.Contains("|"))
                {
                    serverRequest.SendString(GenericResponse.GetResponse("You seem to have entered a token of an application. Please get YOUR token. Usually this can be done by using another request in the network tab.", false), "application/json", 400);
                    return true;
                }
                
                if (r.password == "")
                {
                    r.password = AndroidService.GetDeviceID();
                }
                CoreService.coreVars.token = PasswordEncryption.Encrypt(r.token, r.password);
                CoreService.coreVars.password = GetSHA256OfString(r.password);
                CoreService.coreVars.Save();
                serverRequest.SendString(GenericResponse.GetResponse("Set token", true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/download", serverRequest =>
            {
                DownloadRequest r = JsonSerializer.Deserialize<DownloadRequest>(serverRequest.bodyString);
                if (r.password == "") r.password = AndroidService.GetDeviceID();
                if (GetSHA256OfString(r.password) != CoreService.coreVars.password)
                {
                    serverRequest.SendString(
                        GenericResponse.GetResponse(
                            "Password is wrong. Please try a different password or set a new one", false),
                        "application/json", 403);
                    return true;
                }

                GameDownloadManager gdm = new GameDownloadManager(r);
                gameDownloadManagers.Add(gdm);
                gdm.StartDownload();
                ChangeApp(gdm.packageName);
                serverRequest.SendString(GenericResponse.GetResponse("Downloading!", true), "application/json");
                return true;
            });
			server.AddRoute("POST", "/api/canceldownload", serverRequest =>
            {
                managers.Find(x => x.backupName == serverRequest.queryString.Get("name")).StopDownload();
                serverRequest.SendString(GenericResponse.GetResponse("Canceled download", true));
                return true;
            });
            server.AddRoute("POST", "/api/cancelgamedownload", serverRequest =>
            {
                gameDownloadManagers.Find(x => x.id == serverRequest.queryString.Get("id")).Cancel();
                serverRequest.SendString(GenericResponse.GetResponse("Canceled download", true));
                return true;
            });
			server.AddRoute("GET", "/api/downloads", serverRequest =>
            {
                DownloadStatus status = new DownloadStatus();
                foreach (DownloadManager m in managers)
                {
                    status.individualDownloads.Add(m);
                }
                foreach (GameDownloadManager gdm in gameDownloadManagers)
                {
                    status.gameDownloads.Add(gdm);
                }
                serverRequest.SendString(JsonSerializer.Serialize(status));
                return true;
            });
            
            server.AddRoute("POST", "/api/cleardownloads", serverRequest =>
            {
                gameDownloadManagers.Clear();
                managers.Clear();
                serverRequest.SendString(JsonSerializer.Serialize(GenericResponse.GetResponse("Cleared downloads", true)));
                return true;
            });
            server.AddRoute("GET", "/api/questappversionswitcher/checkupdate", request =>
            {
                Updater u = new Updater(CoreService.version.ToString().Substring(0, CoreService.version.ToString().Length - 2), "https://github.com/ComputerElite/QuestAppVersionSwitcher", "QuestAppVersionSwitcher"); ;
                request.SendString(JsonSerializer.Serialize(u.CheckUpdate()), "application/json");
                return true;
            });
			server.AddRoute("POST", "/api/questappversionswitcher/update", request =>
            {
                Updater u = new Updater(CoreService.version.ToString().Substring(0, CoreService.version.ToString().Length - 2), "https://github.com/ComputerElite/QuestAppVersionSwitcher", "QuestAppVersionSwitcher"); ;
                request.SendString(GenericResponse.GetResponse("Downloading apk, one second please", true), "application/json");
                
                TempFile tmpFile = new TempFile();
                tmpFile.Path += ".apk";
                u.DownloadLatestAPK(tmpFile.Path);
                string packageName = GetAPKPackageName(tmpFile.Path);
                string version = GetAPKVersion(tmpFile.Path);
                string backupDir = CoreService.coreVars.QAVSBackupDir + packageName + "/" + version + "/";
                Logger.Log("Moving file");
                FileManager.CreateDirectoryIfNotExisting(backupDir);
                FileManager.DeleteFileIfExisting(backupDir + "app.apk");
                File.Move(tmpFile.Path, backupDir + "app.apk");
                AndroidService.InitiateInstallApk(backupDir + "app.apk");
                return true;
            });
			server.AddRouteFile("/facts.png", "facts.png");
            server.StartServer(CoreService.coreVars.serverPort);

            if (CoreService.coreVars.loginStep == 1)
            {
                CoreService.coreVars.loginStep = 0;
                CoreService.coreVars.Save();
                CoreService.browser.LoadUrl("http://127.0.0.1:" + CoreService.coreVars.serverPort + "?loadoculus=true");
            }
            else CoreService.browser.LoadUrl("http://127.0.0.1:" + CoreService.coreVars.serverPort + "/setup");
            if (CoreService.started) return;
            Thread t = new Thread(() =>
            {
                try
                {

                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    IPAddress ip = IPAddress.Parse("232.0.53.6");
                    s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip));
                    s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
                    IPEndPoint ipep = new IPEndPoint(ip, 53500);
                    s.Connect(ipep);
                
                    while (true)
                    {
                        s.Send(Encoding.ASCII.GetBytes(JsonSerializer.Serialize(new MultiCastContent { ips = GetIPs(), port = CoreService.coreVars.serverPort })));
                        Thread.Sleep(5000);
                    }
                }
                catch (Exception e)
                {
                    Logger.Log("Couldn't set up multicase: " + e, LoggingType.Warning);
                }
            });
            t.Start();
        }

        private void ChangeApp(string packageName, string name = "")
        {
            Logger.Log("Setting selected app to " + packageName);
            CoreService.coreVars.currentApp = packageName;
            CoreService.coreVars.currentAppName = name == "" ? AndroidService.GetAppname(packageName) : name;
            CoreService.coreVars.Save();
            QAVSModManager.Update();
        }

        public static string GetSHA256OfString(string input)
        {
            return BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "");
        }

        public static string GetAPKPackageName(string path)
        {
			// Is apk
			MemoryStream manifestStream = new MemoryStream();
			ZipArchive apkArchive = ZipFile.OpenRead(path);
			apkArchive.GetEntry("AndroidManifest.xml").Open().CopyTo(manifestStream);
			manifestStream.Position = 0;
			AxmlElement manifest = AxmlLoader.LoadDocument(manifestStream);
			string packageName = "";
			foreach (AxmlAttribute a in manifest.Attributes)
			{
				if (a.Name == "package")
				{
					//Console.WriteLine("\nAPK Version is " + a.Value);
					Logger.Log("package is " + a.Value);
					packageName = a.Value.ToString();
				}
			}
            return packageName;
		}

		public string GetAPKVersion(string path)
		{
			// Is apk
			MemoryStream manifestStream = new MemoryStream();
			ZipArchive apkArchive = ZipFile.OpenRead(path);
			apkArchive.GetEntry("AndroidManifest.xml").Open().CopyTo(manifestStream);
			manifestStream.Position = 0;
			AxmlElement manifest = AxmlLoader.LoadDocument(manifestStream);
			string version = "";
			foreach (AxmlAttribute a in manifest.Attributes)
			{
				if (a.Name == "versionName")
				{
					//Console.WriteLine("\nAPK Version is " + a.Value);
					Logger.Log("version is " + a.Value);
					version = a.Value.ToString();
				}
			}
			return version;
		}

        public bool IsNameFileNameSafe(string name)
        {
            foreach (char c in ReservedChars)
            {
                if (name.Contains(c)) return false;
            }
            return true;
        }

        public static string MakeFileNameSafe(string name)
        {
            foreach (char c in ReservedChars)
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        public List<string> GetIPs()
        {
            return server.ips;
        }
    }
}