using Android.Content.Res;
using ComputerUtils.Android.Logging;
using ComputerUtils.Android.Webserver;
using QuestAppVersionSwitcher.ClientModels;
using QuestAppVersionSwitcher.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
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
using System.Net.Http;
using Java.Net;
using Java.Interop;
using CookieManager = Android.Webkit.CookieManager;
using Android.Content;
using Android.App;
using ComputerUtils.Android;
using QuestAppVersionSwitcher.Mods;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Socket = System.Net.Sockets.Socket;
using Java.Lang;
using Exception = System.Exception;
using String = System.String;
using Thread = System.Threading.Thread;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using ComputerUtils.Updating;
using Org.BouncyCastle.Math.EC.Endo;
using Android.Widget;
using Xamarin.Forms;
using DownloadStatus = QuestAppVersionSwitcher.ClientModels.DownloadStatus;
using Environment = Android.OS.Environment;
using Math = System.Math;
using Path = System.IO.Path;
using WebView = Android.Webkit.WebView;

namespace QuestAppVersionSwitcher
{
    public class QAVSWebViewClient : WebViewClient
    {
        public string navButtonsScript = "var qavsInjectionDiv = document.createElement(\"div\");qavsInjectionDiv.style = \"color: #EEEEEE; position: fixed; top: 10px; right: 10px; background-color: #414141; border-radius: 5px; padding: 5px; display: flex; z-index: 50000;\"; qavsInjectionDiv.innerHTML += `<div style=\"border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none;\" onclick=\"history.go(-1)\">Back</div><div style=\"border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none;\" onclick=\"history.go(1)\">Forward</div><div style=\"border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none;\" onclick=\"location = 'http://localhost:" + CoreService.coreVars.serverPort + "'\">QuestAppVersionSwitcher</div><div style=\\\"border-radius: 5px; font-size: 100%; background-color: #5B5B5B; width: fit-content; height: fit-content; padding: 5px; cursor: pointer; flex-shrink: 0; user-select: none;\\\" onclick=\\\"location = 'https://oculus.com/experiences/quest'\\\">Oculus (Login)</div>`; document.body.appendChild(qavsInjectionDiv)";
        public string toastCode = "var QAVSScript = document.createElement(\"script\");QAVSScript.innerHTML = `var QAVSToastsE = document.createElement(\"div\");document.body.appendChild(QAVSToastsE);let QAVStoasts = 0;let currentQAVSToasts = 0;function ShowToast(msg, color, bgc) {    QAVStoasts++;    currentQAVSToasts++;    let QAVStoastId = QAVStoasts;    QAVSToastsE.innerHTML += \\`<div id=\"QAVStoast\\${QAVStoastId}\" style=\"background-color: \\${bgc}; color: \\${color}; padding: 5px; height: 100px; width: 250px; position: fixed; bottom: \\${(currentQAVSToasts - 1) * 120 + 20}px; right: 30px; border-radius: 10px\">\\${msg}</div>\\`;    setTimeout(() => {        document.getElementById(\\`QAVStoast\\${QAVStoastId}\\`).remove();        currentQAVSToasts--;    }, 5000)}`; document.body.appendChild(QAVSScript);";
        // Grab token
        public override void OnPageFinished(WebView view, string url)
        {
            CookieManager.Instance.Flush();
            if (url.Split("?")[0].Contains("oculus.com"))
            {
                // click login button
                view.EvaluateJavascript("setTimeout(() => {var mySpans = document.getElementsByTagName(\"svg\");for(var i=0;i<mySpans.length;i++){if(mySpans[i].ariaLabel == 'Open Side Navigation Menu'){mySpans[i].parentElement.click();break;}}setTimeout(() => { mySpans = document.getElementsByTagName(\"h6\"); for (var i = 0; i < mySpans.length; i++) { if (mySpans[i].innerHTML == 'Log in / Sign up') { mySpans[i].click(); break; } } }, 600)}, 1000)", null);
                
                // send token to QAVS
                view.EvaluateJavascript("var ws = new WebSocket('ws://localhost:" + CoreService.coreVars.serverPort + "/' + document.body.innerHTML.substr(document.body.innerHTML.indexOf(\"accessToken\"), 200).split('\"')[2]);", null);
            }
            else if (url.StartsWith("https://auth.meta.com/settings"))
            {
                view.LoadUrl("https://oculus.com/experiences/quest");
            }
            else if(!url.ToLower().Contains("localhost") && !url.ToLower().Contains("http://127.0.0.1"))
            {
                view.EvaluateJavascript(navButtonsScript, null);
                view.EvaluateJavascript(toastCode, null);
            }
        }

        public static Dictionary<string, string> headers = new Dictionary<string, string>
        {
            ["sec-fetch-mode"] = "navigate",
            ["sec-fetch-site"] = "same-origin",
            ["sec-fetch-dest"] = "document",
            ["sec-ch-ua-platform"] = "\"Linux\"",
            ["sec-ch-ua"] = "\" Not A; Brand\";v=\"99\", \"Chromium\";v=\"104\"",
            ["sec-ch-ua-mobile"] = "?0",
            ["sec-fetch-user"] = "?1",
            ["cross-origin-opener-policy"] = "unsafe-none"
        };

        public override WebResourceResponse ShouldInterceptRequest(WebView view, IWebResourceRequest request)
        {
            
            foreach (KeyValuePair<string, string> p in QAVSWebViewClient.headers)
            {
                if(!request.RequestHeaders.ContainsKey(p.Key)) request.RequestHeaders.Add(p.Key, p.Value);
                else request.RequestHeaders[p.Key] = p.Value;
            }
            if (request.RequestHeaders.ContainsKey("document-policy")) request.RequestHeaders.Remove("document-policy");

            if (request.RequestHeaders.ContainsKey("document-domain")) request.RequestHeaders.Remove("document-domain");
            string cookie = CookieManager.Instance.GetCookie(request.Url.ToString());
            if (cookie != null) request.RequestHeaders["cookie"] = cookie;
            if (request.Method == "POST")
            {
                request.RequestHeaders["sec-fetch-mode"] = "cors";
                request.RequestHeaders["sec-fetch-dest"] = "empty";
            }
            if(request.Url.Path.Contains("consent") && request.Url.Host.Contains("oculus"))
            {
                Thread t = new Thread(() =>
                {
                    Thread.Sleep(1000);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        view.LoadUrl(CoreVars.oculusLoginUrl);
                    });
                });
                t.Start();
            }
            /*
            // somehow user webclient to handle the request and then change the response headers. No idea how to get the request body from the webview
            WebClient c = new WebClient();
            foreach (KeyValuePair<string, string> p in request.RequestHeaders)
            {
                c.Headers.Add(p.Key, p.Value);
            }
            byte[] responseData = new byte[0];
            if(request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH")
            {
                c.UploadData(request.Url.ToString(), request.Method, request.);
            } else
            {
                c.DownloadData(request.Url.ToString());
            }
            */
            return base.ShouldInterceptRequest(view, request);
        }

        public Dictionary<string, string> GetHeaders(IDictionary<string, IList<string>> h)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            foreach (KeyValuePair<string, IList<string>> p in h)
            {
                if (p.Value.Count > 0)
                {
                    if(p.Key != null) headers.Add(p.Key.ToLower(), p.Value[0]);
                }
            }
            return headers;
        }

        public override bool ShouldOverrideUrlLoading(WebView view, string url)
        {
            view.LoadUrl(url, headers);
            return true;
        }
	}

    public class AndroidDevice
    {
        public int sdkVersion { get; set; } = 0;
    }
    
    public enum LoggedInStatus
    {
        NotLoggedIn = 0,
        SessionInvalid = 1,
        LoggedIn = 2
    }
    
    public class ProgressResponse : GenericResponse
    {
        public double progress { get; set; } = 0;

        public string progressString
        {
            get
            {
                return (progress * 100).ToString("F1") + "%";
            }
        }
        public static string GetResponse(string msg, bool success, double progress)
        {
            ProgressResponse r = new ProgressResponse();
            r.msg = msg;
            r.success = success;
            r.progress = progress;
            return JsonSerializer.Serialize(r);
        }
    }

    public class GenericResponse
    {
        public string msg { get; set; } = "";
        public bool success { get; set; } = true;

        public static string GetResponse(string msg, bool success)
        {
            GenericResponse r = new GenericResponse();
            r.msg = msg;
            r.success = success;
            return JsonSerializer.Serialize(r);
        }
    }
    public class IsAppInstalled : GenericResponse
    {
        public bool isAppInstalled { get; set; } = false;
        public static string GetResponse(string msg, bool isAppInstalled, bool success)
        {
            IsAppInstalled r = new IsAppInstalled();
            r.msg = msg;
            r.isAppInstalled = isAppInstalled;
            r.success = success;
            return JsonSerializer.Serialize(r);
        }
    }
    public class GotAccess : GenericResponse
    {
        public bool gotAccess { get; set; } = false;
        public static string GetResponse(string msg, bool gotAccess, bool success)
        {
            GotAccess r = new GotAccess();
            r.msg = msg;
            r.gotAccess = gotAccess;
            r.success = success;
            return JsonSerializer.Serialize(r);
        }
    }

    public class BackupStatus
    {
        public bool done { get; set; } = false;
        public bool error { get; set; } = false;
        public string errorText { get; set; } = "";
        public string currentOperation { get; set; } = "";
        public int doneOperations { get; set; } = 0;
        public int totalOperations { get; set; } = 0;
        public double progress { get; set; } = 0;
        public string progressString
        {
            get
            {
                return (int)Math.Round(progress * 100) + "%";
            }
        }
    }

    public class PatchStatus : BackupStatus
    {
        public string backupName { get; set; } = "";
    }
    
    public class QAVSWebserver
    {
        HttpServer server = new HttpServer();
        public static readonly char[] ReservedChars = new char[] { '|', '\\', '?', '*', '<', '&', '\'', ':', '>', '+', '[', ']', '/', '\'', ' ' };
        public static List<DownloadManager> managers = new List<DownloadManager>();
        public static List<GameDownloadManager> gameDownloadManagers = new List<GameDownloadManager>();
        public SHA256 hasher = SHA256.Create();
        public static PatchStatus patchStatus = new PatchStatus();

        public LoggedInStatus GetLoggedInStatus()
        {
            if(CoreService.coreVars.token == "") return LoggedInStatus.NotLoggedIn;
            return LoggedInStatus.LoggedIn;
        }

        public void Start()
        {
            server.onWebsocketConnectRequest = uRL =>
            {
                if (uRL.Length <= 10) return;
                string token = uRL.Substring(1);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CoreService.browser.LoadUrl("http://127.0.0.1:" + CoreService.coreVars.serverPort + "?token=" + token);
                });
            };
            server.AddRoute("GET", "/api/mods/mods", request =>
            {
                request.SendString(QAVSModManager.GetMods(), "application/json");
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
                QAVSModManager.InstallModFromUrl(request.bodyString);
                request.SendString(GenericResponse.GetResponse("Trying to install from " + request.bodyString, true), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/mods/cover", request =>
            {
                request.SendData(QAVSModManager.GetModCover(request.queryString.Get("id")), "image/xyz");
                return true;
            });
            server.AddRoute("POST", "/api/mods/uninstall", request =>
            {
                QAVSModManager.UninstallMod(request.queryString.Get("id"));
                request.SendString(GenericResponse.GetResponse("Trying to uninstall", true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/mods/enable", request =>
            {
                QAVSModManager.EnableMod(request.queryString.Get("id"));
                request.SendString(GenericResponse.GetResponse("Trying to enable", true), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/mods/delete", request =>
            {
                QAVSModManager.DeleteMod(request.queryString.Get("id"));
                request.SendString(GenericResponse.GetResponse("Trying to delete", true), "application/json");
                return true;
            }); 
            server.AddRoute("GET", "/api/patching/getmodstatus", request =>
            {
                PatchingStatus status = PatchingManager.GetPatchingStatus();
                if(status == null)
                {
                    status = new PatchingStatus
                    {
                        isInstalled = false,
                        canBePatched = false,
                    };
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
                request.SendString(GenericResponse.GetResponse("Acknowledged. Check status at /patching/patchstatus", true), "application/json", 202);
                patchStatus = new PatchStatus();
                patchStatus.totalOperations = 9;
                patchStatus.currentOperation = "Copying APK. This can take a bit";
                if (!AndroidService.IsPackageInstalled(CoreService.coreVars.currentApp))
                {
                    patchStatus.errorText = CoreService.coreVars.currentApp +
                                            " is not installed. Please select a different app";
                    patchStatus.error = true;
                    return true;
                }
                string appLocation = CoreService.coreVars.QAVSTmpPatchingDir + "app.apk";
                FileManager.RecreateDirectoryIfExisting(CoreService.coreVars.QAVSTmpPatchingDir);
                File.Copy(AndroidService.FindAPKLocation(CoreService.coreVars.currentApp), appLocation);
                ZipArchive apkArchive = ZipFile.Open(appLocation, ZipArchiveMode.Update);
                patchStatus.doneOperations = 1;
                patchStatus.progress = .1;
                PatchingManager.PatchAPK(apkArchive, appLocation);
                return true;
            });
            server.AddRoute("GET", "/api/patching/patchstatus", serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(patchStatus), "application/json", 200);
                return true;
            });


            server.AddRoute("POST", "/api/questappversionswitcher/kill", request =>
            {
                CookieManager.Instance.Flush();
                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
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
                CoreService.coreVars.serverPort = port;
                CoreService.coreVars.Save();
                request.SendString(GenericResponse.GetResponse("Changed port to " +request.bodyString + ". Restart QuestAppVersionSwitcher for the changes to take affect.", true), "application/json");
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
                string filename = request.queryString.Get("filename");
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
            server.AddRouteFile("/script.js", "html/script.js");
            server.AddRouteFile("/hiddenApps.json", "html/hiddenApps.json");
            server.AddRouteFile("/style.css", "html/style.css");
            server.AddRoute("GET", "/api/android/installedapps", serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(AndroidService.GetInstalledApps()), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/android/device", serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(new AndroidDevice()
                {
                    sdkVersion = (int)Build.VERSION.SdkInt
                }), "application/json");
                return true;
            });
			server.AddRoute("POST", "/api/android/launch", serverRequest =>
            {
                serverRequest.SendString(GenericResponse.GetResponse("Launching " + CoreService.coreVars.currentApp, true), "application/json");
                AndroidService.LaunchApp(CoreService.coreVars.currentApp);
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
                if (!AndroidService.IsPackageInstalled(package))
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
                report.userIsLoggedIn = GetLoggedInStatus() == LoggedInStatus.LoggedIn;
                report.reportTime = DateTime.Now;
                report.availableSpace = Environment.ExternalStorageDirectory.UsableSpace;
                PatchingStatus status = PatchingManager.GetPatchingStatus();
                Logger.Log("-------Status of selected app-------\n" + (status == null ? "Not installed" : JsonSerializer.Serialize(status, new JsonSerializerOptions
                {
                    WriteIndented = true
                })));

                if (report.userIsLoggedIn)
                {
                    try
                    {
                        if (GetSHA256OfString(request.bodyString) != CoreService.coreVars.password)
                        {
                            request.SendString(GenericResponse.GetResponse("Password is wrong. Please try a different password or set a new one", false), "application/json", 403);
                            return true;
                        }
                        GraphQLClient.log = false;
                        GraphQLClient.oculusStoreToken = PasswordEncryption.Decrypt(CoreService.coreVars.token, request.bodyString);
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
                foreach (string game in Directory.GetDirectories(CoreService.coreVars.QAVSBackupDir))
                {
                    Logger.Log(Path.GetFileName(game));
                    foreach (string backup in Directory.GetDirectories(game))
                    {
                        Logger.Log("├──" + Path.GetFileName(backup));
                        foreach (string file in Directory.GetFiles(backup))
                        {
                            Logger.Log("|  ├──" + Path.GetFileName(file) + " (" + SizeConverter.ByteSizeToString(new FileInfo(file).Length) + ")");
                        }
                    }
                }
                report.log = Logger.log;
                request.SendString(JsonSerializer.Serialize(report), "application/json");
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
                ChangeApp(serverRequest.bodyString);
                serverRequest.SendString(GenericResponse.GetResponse("App changed to " + serverRequest.bodyString, true), "application/json");
                return true;
            });
            server.AddRoute("GET", "/api/questappversionswitcher/config", serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(CoreService.coreVars), "application/json");
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
                CoreService.coreVars.currentApp = packageName;
                CoreService.coreVars.Save();
                string backupDir = CoreService.coreVars.QAVSBackupDir + packageName + "/" + version + "/";
                Logger.Log("Moving file");
                FileManager.CreateDirectoryIfNotExisting(backupDir);
                FileManager.DeleteFileIfExisting(backupDir + "app.apk");
                File.Move(tmpFile.Path, backupDir + "app.apk");

                serverRequest.SendString(GenericResponse.GetResponse("uploaded and selected app in backup tab", true), "application/json");
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

                    serverRequest.SendString(JsonSerializer.Serialize(GetBackups(package)), "application/json");
                }
                else
                {
                    serverRequest.SendString("{}", "application/json");
                }
                return true;
            });
            BackupStatus backupStatus = new BackupStatus();
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
                Directory.CreateDirectory(backupDir);
                if (!AndroidService.IsPackageInstalled(package))
                {
                    Logger.Log(package + " is not installed. Aborting backup");
                    backupStatus.errorText = package + " is not installed. Please select a different app.";
                    backupStatus.error = true;
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
                    try
                    {
                        if(Directory.Exists(gameDataDir)) FolderPermission.DirectoryCopy(gameDataDir, backupDir + package);
                    }
                    catch (Exception e)
                    {
                        backupStatus.errorText = e.ToString();
                        backupStatus.error = true;
                        return true;
                    }
                    backupStatus.doneOperations = 3;
                    backupStatus.progress = .6;

                    if (Directory.Exists(CoreService.coreVars.AndroidObbLocation + package))
                    {
                        backupStatus.currentOperation = "Copying Obbs. Please wait until it has finished. This can take up to 2 minutes";
                        Directory.CreateDirectory(backupDir + "obb/" + package);
                        FolderPermission.DirectoryCopy(CoreService.coreVars.AndroidObbLocation + package, backupDir + "obb/" + package);
                    }
                    backupStatus.doneOperations = 4;
                    backupStatus.progress = 1;
                }
                catch (Exception e)
                {
                    Logger.Log("Backup failed: " + e);
                    backupStatus.errorText = "Backup failed: " + e;
                    return true;
                }

                GetBackupInfo(backupDir, true); // make sure backup metadata is up to date

                backupStatus.done = true;
                backupStatus.currentOperation = "Backup of " + package + " with the name " + backupname + " finished";
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

                Logger.Log("Installing apk of backup " + backupname + " of " + package);
                AndroidService.InitiateInstallApk(backupDir + "app.apk");
                serverRequest.SendString(GenericResponse.GetResponse("Started apk install", true), "application/json");
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

                
                serverRequest.SendString(JsonSerializer.Serialize(GetBackupInfo(backupDir)), "application/json");
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
                CoreService.coreVars.token = PasswordEncryption.Encrypt(r.token, r.password);
                CoreService.coreVars.password = GetSHA256OfString(r.password);
                CoreService.coreVars.Save();
                serverRequest.SendString(GenericResponse.GetResponse("Set token", false), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/download", serverRequest =>
            {
                DownloadRequest r = JsonSerializer.Deserialize<DownloadRequest>(serverRequest.bodyString);
                if (GetSHA256OfString(r.password) != CoreService.coreVars.password)
                {
                    serverRequest.SendString(GenericResponse.GetResponse("Password is wrong. Please try a different password or set a new one", false), "application/json", 403);
                    return true;
                }

                GameDownloadManager gdm = new GameDownloadManager(r);
                gameDownloadManagers.Add(gdm);
                gdm.StartDownload();
                ChangeApp(gdm.packageName);
                serverRequest.SendString(GenericResponse.GetResponse("Added to downloads. Check download progress tab.", true), "application/json");
                return true;
            });
			server.AddRoute("POST", "/api/canceldownload", serverRequest =>
            {
                managers.Find(x => x.backupName == serverRequest.queryString.Get("name")).StopDownload();
                return true;
            });
            server.AddRoute("POST", "/api/cancelgamedownload", serverRequest =>
            {
                gameDownloadManagers.Find(x => x.id == serverRequest.queryString.Get("id")).Cancel();
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

            if (CoreService.started) return;
            if (CoreService.coreVars.loginStep == 1)
            {
                CoreService.coreVars.loginStep = 0;
                CoreService.coreVars.Save();
                CoreService.browser.LoadUrl("http://127.0.0.1:" + CoreService.coreVars.serverPort + "?loadoculus=true");
            }
            else CoreService.browser.LoadUrl("http://127.0.0.1:" + CoreService.coreVars.serverPort + "/");
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

        private void ChangeApp(string packageName)
        {
            Logger.Log("Settings selected app to " + packageName);
            CoreService.coreVars.currentApp = packageName;
            CoreService.coreVars.Save();
            QAVSModManager.Update();
        }

        public void ShowWebsite()
        {
            throw new NotImplementedException();
        }

        public string GetSHA256OfString(string input)
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

        public BackupInfo GetBackupInfo(string path, bool loadAnyway = false)
        {
            BackupInfo info = new BackupInfo();
            string pathWithoutSlash = path.EndsWith(Path.DirectorySeparatorChar)
                ? path.Substring(0, path.Length - 1)
                : path;
            if (File.Exists(pathWithoutSlash + "/info.json") && !loadAnyway)
            {
                info = JsonSerializer.Deserialize<BackupInfo>(File.ReadAllText(pathWithoutSlash + "/info.json"));
                if (info.BackupInfoVersion < BackupInfoVersion.V1) return GetBackupInfo(path, true);
                return info;
            }

            info.backupName = Path.GetFileName(pathWithoutSlash);
            info.containsAppData = Directory.Exists(pathWithoutSlash + "/" + Directory.GetParent(pathWithoutSlash).Name);
            info.backupLocation = path;
            info.backupSize = FileManager.GetDirSize(pathWithoutSlash);
            info.backupSizeString = SizeConverter.ByteSizeToString(info.backupSize);
            info.containsApk = File.Exists(pathWithoutSlash + "/app.apk");
            if (info.containsApk)
            {
                ZipArchive apk = ZipFile.OpenRead(pathWithoutSlash + "/app.apk");
                PatchingStatus s = PatchingManager.GetPatchingStatus(apk);
                info.gameVersion = s.version;
                info.isPatchedApk = s.isPatched;
                apk.Dispose();
            }
            File.WriteAllText(pathWithoutSlash + "/info.json", JsonSerializer.Serialize(info));
            return info;
        }

		public BackupList GetBackups(string package)
        {
            string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/";
            BackupList backups = new BackupList();
            foreach (string d in Directory.GetDirectories(backupDir))
            {
                backups.backups.Add(GetBackupInfo(d));
                backups.backupsSize += backups.backups.Last().backupSize;
            }
            if (File.Exists(backupDir + "lastRestored.txt")) backups.lastRestored = File.ReadAllText(backupDir + "lastRestored.txt");
            backups.backupsSizeString = SizeConverter.ByteSizeToString(backups.backupsSize);
            return backups;
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

    public class BackupInfo
    {
        public BackupInfoVersion BackupInfoVersion { get; set; } = BackupInfoVersion.V1;
        public string backupName { get; set; } = "";
        public string backupLocation { get; set; } = "";
        public bool containsAppData { get; set; } = false;
        public bool isPatchedApk { get; set; } = false;
        public bool containsApk { get; set; } = false;
        public string gameVersion { get; set; } = "unknown";
        public long backupSize { get; set; } = 0;
        public string backupSizeString { get; set; } = "";
    }

    public class MultiCastContent
    {
		public string QAVSVersion { get { return CoreService.version.ToString(); } }
        public List<string> ips { get; set; }
        public int port { get; set; }
	}

	public class QAVSReport
	{
        public int androidVersion { get; set; }
		public string log { get; set; }
		public string version { get; set; }
		public DateTime reportTime { get; set; }
		public string reportId { get; set; }
		public bool userIsLoggedIn { get; set; }
        public List<string> userEntitlements { get; set; } = new List<string>();
		public long availableSpace { get; set; }
		public string availableSpaceString
		{
			get
			{
				return SizeConverter.ByteSizeToString(availableSpace);
			}
		}
	}
}