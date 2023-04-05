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
using Environment = Android.OS.Environment;

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
    public class QAVSWebserver
    {
        HttpServer server = new HttpServer();
        public static readonly char[] ReservedChars = new char[] { '|', '\\', '?', '*', '<', '\'', ':', '>', '+', '[', ']', '/', '\'', ' ' };
        public static List<DownloadManager> managers = new List<DownloadManager>();
        public SHA256 hasher = SHA256.Create();
        public static string patchText = "";
        public static int patchCode = 202;

        public LoggedInStatus GetLoggedInStatus()
        {
            if(CoreService.coreVars.token == "") return LoggedInStatus.NotLoggedIn;
            return LoggedInStatus.LoggedIn;
        }

        public void Start()
        {
            WebViewClient client = new WebViewClient();

            CoreService.browser.SetWebViewClient(new QAVSWebViewClient());
            server.onWebsocketConnectRequest = new Action<string>(uRL =>
            {
                if (uRL.Length <= 10) return;
                string token = uRL.Substring(1);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CoreService.browser.LoadUrl("http://127.0.0.1:" + CoreService.coreVars.serverPort + "?token=" + token);
                });
            });
            server.AddRoute("GET", "/google/", new Func<ServerRequest, bool>(request =>
            {
                WebClient c = new WebClient();
                c.Headers.Add("User-Agent", CoreService.ua);
                request.SendString(c.DownloadString("https://www.google.com/" + request.pathDiff));
                return true;
            }));
            server.AddRoute("GET", "/mods/mods", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(QAVSModManager.GetMods(), "application/json");
                return true;
            }));
            server.AddRoute("GET", "/patching/getpatchoptions", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(CoreService.coreVars.patchingPermissions), "application/json");
                return true;
            }));
            server.AddRoute("GET", "/patching/setpatchoptions", new Func<ServerRequest, bool>(request =>
            {
                CoreService.coreVars.patchingPermissions = JsonSerializer.Deserialize<PatchingPermissions>(request.queryString.Get("body"));
                request.SendString("Set patch options", "application/json");
                return true;
            }));
            server.AddRoute("GET", "/mods/operations", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(QAVSModManager.runningOperations), "application/json");
                return true;
            }));
            server.AddRoute("POST", "/mods/install", new Func<ServerRequest, bool>(request =>
            {
                QAVSModManager.InstallMod(request.bodyBytes, request.queryString.Get("filename"));
                request.SendString("Trying to install", "application/json");
                return true;
            }));
            server.AddRoute("POST", "/mods/installfromurl", new Func<ServerRequest, bool>(request =>
            {
                QAVSModManager.InstallModFromUrl(request.bodyString);
                request.SendString("Trying to install from " + request.bodyString, "application/json");
                return true;
            }));
            server.AddRoute("GET", "/mods/cover", new Func<ServerRequest, bool>(request =>
            {
                request.SendData(QAVSModManager.GetModCover(request.queryString.Get("id")), "image/xyz");
                return true;
            }));
            server.AddRoute("POST", "/mods/uninstall", new Func<ServerRequest, bool>(request =>
            {
                QAVSModManager.UninstallMod(request.queryString.Get("id"));
                request.SendString("Trying to uninstall", "application/json");
                return true;
            }));
            server.AddRoute("POST", "/mods/enable", new Func<ServerRequest, bool>(request =>
            {
                QAVSModManager.EnableMod(request.queryString.Get("id"));
                request.SendString("Trying to uninstall", "application/json");
                return true;
            }));
            server.AddRoute("POST", "/mods/delete", new Func<ServerRequest, bool>(request =>
            {
                QAVSModManager.DeleteMod(request.queryString.Get("id"));
                request.SendString("Trying to delete", "application/json");
                return true;
            }));
            //// Patching and modding
            /// QAVS
            /// - Backups
            /// - tmpDowngrade
            /// - tmpPatching
            ///     - apk
            ///     
            server.AddRoute("GET", "/patching/getmodstatus", new Func<ServerRequest, bool>(request =>
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
            }));
            server.AddRoute("GET", "/opensettings", new Func<ServerRequest, bool>(request =>
            {
                AndroidCore.context.StartActivity(AndroidCore.context.PackageManager.GetLaunchIntentForPackage("com.android.settings"));
                request.SendString("Alright", "application/json");
                return true;
            }));
            server.AddRoute("GET", "/deleteallmods", new Func<ServerRequest, bool>(request =>
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
            }));
            
            server.AddRoute("GET", "/patching/patchapk", new Func<ServerRequest, bool>(request =>
            {
                request.SendString("Acknowledged. Check status at /patching/patchstatus", "text/plain", 202);
                patchText = JsonSerializer.Serialize(new MessageAndValue<String>("Copying APK. This can take a bit", ""));
                patchCode = 202;
                if (!AndroidService.IsPackageInstalled(CoreService.coreVars.currentApp))
                {
                    patchText = CoreService.coreVars.currentApp + " is not installed. Please select a diffrent app";
                    patchCode = 400;
                    return true;
                }
                string appLocation = CoreService.coreVars.QAVSTmpPatchingDir + "app.apk";
                FileManager.RecreateDirectoryIfExisting(CoreService.coreVars.QAVSTmpPatchingDir);
                File.Copy(AndroidService.FindAPKLocation(CoreService.coreVars.currentApp), appLocation);
                ZipArchive apkArchive = ZipFile.Open(appLocation, ZipArchiveMode.Update);
                PatchingManager.PatchAPK(apkArchive, appLocation);
                return true;
            }));
            server.AddRoute("GET", "/patching/patchstatus", new Func<ServerRequest, bool>(serverRequest =>
            {
                serverRequest.SendString(patchText, "text/plain", patchCode);
                return true;
            }));


            server.AddRoute("GET", "/questappversionswitcher/kill", new Func<ServerRequest, bool>(request =>
            {
                CookieManager.Instance.Flush();
                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                return true;
            }));
            server.AddRoute("GET", "/questappversionswitcher/loggedinstatus", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(((int)GetLoggedInStatus()).ToString());
                return true;
            }));
            server.AddRoute("GET", "/questappversionswitcher/changeport", new Func<ServerRequest, bool>(request =>
            {
                int port = Convert.ToInt32(request.queryString.Get("body"));
                if(port < 50000)
                {
                    request.SendString("Port must be greater than 50000!", "text/plain", 400);
                    return true;
                }
                CoreService.coreVars.serverPort = port;
                CoreService.coreVars.Save();
                request.SendString("Changed port to " +request.bodyString + ". Restart QuestAppVersionSwitcher for the changes to take affect.");
                return true;
            }));
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
			server.AddRoute("GET", "/cosmetics/types", new Func<ServerRequest, bool>(request =>
			{
				string game = request.queryString.Get("game");
				if (game == null) game = CoreService.coreVars.currentApp;
				request.SendString(JsonSerializer.Serialize(CoreVars.cosmetics.GetCosmeticsGame(game)), "application/json");
				return true;
			}));
			server.AddRoute("GET", "/cosmetics/getinstalled", new Func<ServerRequest, bool>(request =>
			{
				string game = request.queryString.Get("game");
				if (game == null) game = CoreService.coreVars.currentApp;
				string type = request.queryString.Get("type");
				if (type == null)
				{
					request.SendString("No type specified", "text/plain", 400);
					return true;
				}
                
				request.SendString(JsonSerializer.Serialize(CoreVars.cosmetics.GetInstalledCosmetics(game, type)), "application/json");
				return true;
			}));
			server.AddRoute("GET", "/cosmetics/delete", new Func<ServerRequest, bool>(request =>
			{
				string game = request.queryString.Get("game");
				if (game == null) game = CoreService.coreVars.currentApp;
				string type = request.queryString.Get("type");
				if (type == null)
				{
					request.SendString("No type specified", "text/plain", 400);
					return true;
				}
				string filename = request.queryString.Get("filename");
				if (filename == null)
				{
					request.SendString("No filename specified", "text/plain", 400);
					return true;
				}
                CoreVars.cosmetics.RemoveCosmetic(game, type, filename);
				request.SendString("Deleted");
				return true;
			}));

			server.AddRouteFile("/", "html/index.html");
            server.AddRouteFile("/script.js", "html/script.js");
            server.AddRouteFile("/hiddenApps.json", "html/hiddenApps.json");
            server.AddRouteFile("/style.css", "html/style.css");
            server.AddRoute("GET", "/android/installedapps", new Func<ServerRequest, bool>(serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(AndroidService.GetInstalledApps()), "application/json");
                return true;
            }));
            server.AddRoute("GET", "/android/device", new Func<ServerRequest, bool>(serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(new AndroidDevice()
                {
                    sdkVersion = (int)Build.VERSION.SdkInt
                }), "application/json");
                return true;
            }));
			server.AddRoute("GET", "/android/launch", new Func<ServerRequest, bool>(serverRequest =>
			{
				serverRequest.SendString("Launching " + CoreService.coreVars.currentApp);
                AndroidService.LaunchApp(CoreService.coreVars.currentApp);
				return true;
			}));
			server.AddRoute("GET", "/android/installedappsandbackups", new Func<ServerRequest, bool>(serverRequest =>
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
            }));
            server.AddRoute("GET", "/android/getpackagelocation", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                string location = AndroidService.FindAPKLocation(package);
                if (location == null)
                {
                    serverRequest.SendString("package not found", "text/plain", 400);
                }
                else
                {
                    serverRequest.SendString(location);
                }
                return true;
            }));
            server.AddRoute("GET", "/android/uninstallpackage", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                if (!AndroidService.IsPackageInstalled(package))
                {
                    serverRequest.SendString("App is already uninstalled", "text/plain", 230);
                    return true;
                }
                AndroidService.InitiateUninstallPackage(package);
                serverRequest.SendString("Uninstall request sent");
                return true;
            }));
            server.AddRoute("POST", "/questappversionswitcher/uploadlogs", new Func<ServerRequest, bool>(request =>
            {
                Logger.Log("\n\n------Log upload requested------");
				QAVSReport report = new QAVSReport();
                report.version = CoreService.version.ToString();
				report.userIsLoggedIn = GetLoggedInStatus() == LoggedInStatus.LoggedIn;
                report.reportTime = DateTime.Now;
                report.availableSpace = Android.OS.Environment.ExternalStorageDirectory.UsableSpace;

				if (report.userIsLoggedIn)
                {
                    try
                    {
						if (GetSHA256OfString(request.bodyString) != CoreService.coreVars.password)
						{
							request.SendString("Password is wrong. Please try a different password or set a new one", "text/plain", 403);
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
                request.SendString(JsonSerializer.Serialize(report));
                return true;
			}));
            server.AddRoute("GET", "/android/ispackageinstalled", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                serverRequest.SendString(AndroidService.IsPackageInstalled(package).ToString());
                return true;
            }));
            server.AddRoute("GET", "/questappversionswitcher/changeapp", new Func<ServerRequest, bool>(serverRequest =>
            {
                CoreService.coreVars.currentApp = serverRequest.queryString.Get("body");
                CoreService.coreVars.Save();
                QAVSModManager.Update();
                serverRequest.SendString("App changed to " + serverRequest.bodyString);
                return true;
            }));
            server.AddRoute("GET", "/questappversionswitcher/config", new Func<ServerRequest, bool>(serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(CoreService.coreVars));
                return true;
            }));
            server.AddRoute("GET", "/questappversionswitcher/about", new Func<ServerRequest, bool>(serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(new About { browserIPs = server.ips, version = CoreService.version.ToString() }));
                return true;
            }));
            server.AddRoute("GET", "/android/installapk", new Func<ServerRequest, bool>(serverRequest =>
            {
                serverRequest.SendString("This Endpoint has been deactivated because of security concerns", "text/plain", 503);
                return true;

                // Deactivated cause of security resons
                if (serverRequest.queryString.Get("path") == null)
                {
                    serverRequest.SendString("path key needed", "text/plain", 400);
                    return true;
                }
                string apkPath = serverRequest.queryString.Get("path");
                AndroidService.InitiateInstallApk(apkPath);
                serverRequest.SendString("Install request sent");
                return true;
            }));
			server.AddRoute("POST", "/android/uploadandinstallapk", new Func<ServerRequest, bool>(serverRequest =>
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

				serverRequest.SendString("uploaded and selected app in backup tab");
				return true;
			}));
			server.AddRoute("GET", "/backups", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                if (!IsNameFileNameSafe(package))
                {
                    serverRequest.SendString("You package contains a forbidden character. You can not backup it. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
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
            }));
            int code = 202;
            string text = "";
            server.AddRoute("GET", "/backup", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                if (serverRequest.queryString.Get("backupname") == null)
                {
                    serverRequest.SendString("backupname key needed", "text/plain", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                string backupname = serverRequest.queryString.Get("backupname");
                if (!IsNameFileNameSafe(backupname))
                {
                    serverRequest.SendString("Your Backup name contains a forbidden character. Please remove them. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                if (!IsNameFileNameSafe(package))
                {
                    serverRequest.SendString("Your package contains a forbidden character. You can not backup it. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                if (backupname == "")
                {
                    serverRequest.SendString("Your backup has to have a name. Please add one.", "text/plain", 400);
                    return true;
                }
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                if (Directory.Exists(backupDir))
                {
                    serverRequest.SendString("A Backup with this name already exists. Please choose a different name", "text/plain", 400);
                    return true;
                }
                Logger.Log("Creating backup in " + backupDir + " for " + package);
                serverRequest.SendString("Creating Backup. Please wait until it has finished. This can take up to 2 minutes", "text/plain", 202);
                text = "Creating Backup. Please wait until it has finished. This can take up to 2 minutes";
                code = 202;
                Directory.CreateDirectory(backupDir);
                if (!AndroidService.IsPackageInstalled(package))
                {
                    Logger.Log(package + " is not installed. Aborting backup");
                    text = package + " is not installed. Please select a different app.";
                    code = 400;
                    return true;
                }
                string apkDir = AndroidService.FindAPKLocation(package);
                string gameDataDir = CoreService.coreVars.AndroidAppLocation + package;
                

                try
                {
                    if (serverRequest.queryString.Get("onlyappdata") == null)
                    {
                        text = "Copying APK. Please wait until it has finished. This can take up to 2 minutes";
                        code = 202;
                        Logger.Log("Copying APK from " + apkDir + " to " + backupDir + "app.apk");
                        File.Copy(apkDir, backupDir + "app.apk");
                    } else
                    {
                        Logger.Log("Only backing up app data. Skipping apk");
                        File.WriteAllText(backupDir + "onlyappdata.txt", "This backup only contains app data.");
                    }
                    text = "Copying App Data. Please wait until it has finished. This can take up to 2 minutes";
                    code = 202;
                    try
                    {
                        if(Directory.Exists(gameDataDir)) FolderPermission.DirectoryCopy(gameDataDir, backupDir + package);
                    }
                    catch (Exception e)
                    {
                        text = e.ToString();
                        code = 500;
                        return true;
                    }

                    if (Directory.Exists(CoreService.coreVars.AndroidObbLocation + package))
                    {
                        text = "Copying Obbs. Please wait until it has finished. This can take up to 2 minutes";
                        code = 202;
                        Directory.CreateDirectory(backupDir + "obb/" + package);
                        FolderPermission.DirectoryCopy(CoreService.coreVars.AndroidObbLocation + package, backupDir + "obb/" + package);
                    }
                }
                catch (Exception e)
                {
                    text = "Backup failed: " + e.Message;
                    code = 500;
                    return true;
                }

                text = "Backup of " + package + " with the name " + backupname + " finished";
                code = 200;
                return true;
            }));
            server.AddRoute("GET", "/isonlyappdata", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                if (serverRequest.queryString.Get("backupname") == null)
                {
                    serverRequest.SendString("backupname key needed", "text/plain", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                string backupname = serverRequest.queryString.Get("backupname");
                if (!IsNameFileNameSafe(backupname))
                {
                    serverRequest.SendString("Your Backup name contains a forbidden character. Please remove them. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                if (!IsNameFileNameSafe(package))
                {
                    serverRequest.SendString("Your package contains a forbidden character. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                if (backupname == "")
                {
                    serverRequest.SendString("The backup has to have a name. Please add one.", "text/plain", 400);
                    return true;
                }
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                if (!Directory.Exists(backupDir))
                {
                    serverRequest.SendString("This backup doesn't exist", "text/plain", 400);
                    return true;
                }
                serverRequest.SendString(File.Exists(backupDir + "onlyappdata.txt").ToString().ToLower());
                return true;
            }));
            server.AddRoute("GET", "/backupstatus", new Func<ServerRequest, bool>(serverRequest =>
            {
                serverRequest.SendString(text, "text/plain", code);
                return true;
            }));
            server.AddRoute("DELETE", "/backup", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                if (serverRequest.queryString.Get("backupname") == null)
                {
                    serverRequest.SendString("backupname key needed", "text/plain", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                string backupname = serverRequest.queryString.Get("backupname");
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                if (!Directory.Exists(backupDir))
                {
                    serverRequest.SendString("The Backup you want to delete doesn't exist.", "text/plain", 400);
                }
                Directory.Delete(backupDir, true);
                serverRequest.SendString("Deleted " + backupname + " of " + package);
                return true;
            }));
            server.AddRoute("GET", "/restoreapp", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                if (serverRequest.queryString.Get("backupname") == null)
                {
                    serverRequest.SendString("backupname key needed", "text/plain", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                string backupname = serverRequest.queryString.Get("backupname");
                if (!IsNameFileNameSafe(backupname))
                {
                    serverRequest.SendString("Your Backup name contains a forbidden character. Please remove them. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                if (!IsNameFileNameSafe(package))
                {
                    serverRequest.SendString("Your package contains a forbidden character. You can not restore it. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                if (!Directory.Exists(backupDir))
                {
                    serverRequest.SendString("This backup doesn't exist", "text/plain", 400);
                    return true;
                }
                if (!File.Exists(backupDir + "app.apk"))
                {
                    serverRequest.SendString("Critical: APK doesn't exist in Backup. This Backup is useless. Please restart the app and choose a different one.", "text/plain", 500);
                    return true;
                }
                AndroidService.InitiateInstallApk(backupDir + "app.apk");
                serverRequest.SendString("Started apk install", "text/plain", 200);
                return true;
            }));
            server.AddRoute("GET", "/backupinfo", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                if (serverRequest.queryString.Get("backupname") == null)
                {
                    serverRequest.SendString("backupname key needed", "text/plain", 400);
                    return true;
                }

                BackupInfo i = new BackupInfo();
                string package = serverRequest.queryString.Get("package");
                string backupname = serverRequest.queryString.Get("backupname");
                if (!IsNameFileNameSafe(backupname))
                {
                    serverRequest.SendString("You Backup name contains a forbidden character. Please remove them. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                if (!IsNameFileNameSafe(package))
                {
                    serverRequest.SendString("You package contains a forbidden character. You can not backup it. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                if (!Directory.Exists(backupDir))
                {
                    serverRequest.SendString("This backup doesn't exist", "text/plain", 400);
                    return true;
                }

                i.containsAppData = Directory.Exists(backupDir + package) || Directory.Exists(backupDir + "obb/" + package);
                i.isPatchedApk = File.Exists(backupDir + "isPatched.txt");
                serverRequest.SendString(JsonSerializer.Serialize(i), "text/plain", 200);
                return true;
            }));
            server.AddRoute("GET", "/grantaccess", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
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
                serverRequest.SendString("", "text/plain", 200);
                return true;
            }));
            
            server.AddRoute("GET", "/gotaccess", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
                {
                    serverRequest.SendString("True", "text/plain", 200);

                }
                else if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2)
                {
                    serverRequest.SendString((FolderPermission.GotAccessTo(Environment.ExternalStorageDirectory.AbsolutePath + "/Android/obb") && FolderPermission.GotAccessTo(Environment.ExternalStorageDirectory.AbsolutePath + "/Android/data")).ToString(), "text/plain", 200);
                }
                else
                {
                    serverRequest.SendString((FolderPermission.GotAccessTo(Environment.ExternalStorageDirectory.AbsolutePath + "/Android/obb/" + package) && FolderPermission.GotAccessTo(Environment.ExternalStorageDirectory.AbsolutePath + "/Android/data/" + package)).ToString(), "text/plain", 200);
                }
                return true;
            }));
            server.AddRoute("GET", "/grantmanagestorageappaccess", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                Intent intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission, Android.Net.Uri.Parse("package:" + package));
                AndroidCore.context.StartActivity(intent);
                serverRequest.SendString("", "text/plain", 200);
                return true;
            }));
            server.AddRoute("GET", "/restoregamedata", new Func<ServerRequest, bool>(serverRequest =>
            {
                if (serverRequest.queryString.Get("package") == null)
                {
                    serverRequest.SendString("package key needed", "text/plain", 400);
                    return true;
                }
                if (serverRequest.queryString.Get("backupname") == null)
                {
                    serverRequest.SendString("backupname key needed", "text/plain", 400);
                    return true;
                }
                string package = serverRequest.queryString.Get("package");
                string backupname = serverRequest.queryString.Get("backupname");
                if (!IsNameFileNameSafe(backupname))
                {
                    serverRequest.SendString("You Backup name contains a forbidden character. Please remove them. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                if (!IsNameFileNameSafe(package))
                {
                    serverRequest.SendString("You package contains a forbidden character. You can not backup it. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupname + "/";
                if (!Directory.Exists(backupDir))
                {
                    serverRequest.SendString("This backup doesn't exist", "text/plain", 400);
                    return true;
                }
                if (!AndroidService.IsPackageInstalled(package))
                {
                    serverRequest.SendString(package + " is not installed. Can not restore game data", "text/plain", 400);
                    return true;
                }
                string gameDataDir = CoreService.coreVars.AndroidAppLocation + package;
                
                if (Directory.Exists(backupDir + "obb/" + package))
                {
                    try
                    {
                        FolderPermission.DirectoryCopy(backupDir + "obb/" + package, CoreService.coreVars.AndroidObbLocation + package);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e.ToString(), LoggingType.Error);
                        serverRequest.SendString("Obbs of " + package + " were unable to be restored: " + e, "text/plain", 500);
                        return true;
                    }
                }
                if (!Directory.Exists(backupDir + package))
                {
                    serverRequest.SendString("This backup doesn't contain a game data backup. Please skip this step", "text/plain", 400);
                    return true;
                }
                try
                {
                    FolderPermission.DirectoryCopy(backupDir + package, gameDataDir);
                }
                catch (Exception e)
                {
                    Logger.Log(e.ToString(), LoggingType.Error);
                    serverRequest.SendString("App data of " + package + " was unable to be restored: " + e, "text/plain", 500);
                    return true;
                }
                serverRequest.SendString("Game data restored", "text/plain", 200);
                return true;
            }));
            server.AddRoute("GET", "/allbackups", new Func<ServerRequest, bool>(serverRequest =>
            {
                serverRequest.SendString(SizeConverter.ByteSizeToString(FileManager.GetDirSize(CoreService.coreVars.QAVSBackupDir)));
                return true;
            }));
            server.AddRoute("POST", "/token", new Func<ServerRequest, bool>(serverRequest =>
            {
                TokenRequest r = JsonSerializer.Deserialize<TokenRequest>(serverRequest.bodyString);
                if (r.token.Contains("%"))
                {
                    serverRequest.SendString("You got your token from the wrong place. Go to the payload tab. Don't get it from the url.", "text/plain", 400);
                    return true;
                }
                if (!r.token.StartsWith("OC"))
                {
                    serverRequest.SendString("Tokens must start with 'OC'. Please get a new one", "text/plain", 400);
                    return true;
                }
                if (r.token.Contains("|"))
                {
                    serverRequest.SendString("You seem to have entered a token of an application. Please get YOUR token. Usually this can be done by using another request in the network tab.", "text/plain", 400);
                    return true;
                }
                CoreService.coreVars.token = PasswordEncryption.Encrypt(r.token, r.password);
                SHA256 s = SHA256.Create();
                CoreService.coreVars.password = GetSHA256OfString(r.password);
                CoreService.coreVars.Save();
                serverRequest.SendString("Set token");
                return true;
            }));
            server.AddRoute("POST", "/download", new Func<ServerRequest, bool>(serverRequest =>
            {
                DownloadRequest r = JsonSerializer.Deserialize<DownloadRequest>(serverRequest.bodyString);
                if (GetSHA256OfString(r.password) != CoreService.coreVars.password)
                {
                    serverRequest.SendString("Password is wrong. Please try a different password or set a new one", "text/plain", 403);
                    return true;
                }
                DownloadManager m = new DownloadManager();
                m.StartDownload(r.binaryId, r.password, r.version, r.app, r.parentId, r.isObb, r.packageName);
                m.DownloadFinishedEvent += DownloadCompleted;
                managers.Add(m);
                serverRequest.SendString("Added to downloads. Check download progress tab.");
                return true;
            }));
			server.AddRoute("GET", "/canceldownload", new Func<ServerRequest, bool>(serverRequest =>
			{
				managers.Find(x => x.backupName == serverRequest.queryString.Get("name")).StopDownload();
				return true;
			}));
			server.AddRoute("GET", "/downloads", new Func<ServerRequest, bool>(serverRequest =>
            {
                List<DownloadProgress> progress = new List<DownloadProgress>();
                foreach (DownloadManager m in managers)
                {
                    progress.Add(m);
                }
                serverRequest.SendString(JsonSerializer.Serialize(progress));
                return true;
            }));
            server.AddRoute("GET", "/questappversionswitcher/checkupdate", new Func<ServerRequest, bool>(request =>
            {
                Updater u = new Updater(CoreService.version.ToString().Substring(0, CoreService.version.ToString().Length - 2), "https://github.com/ComputerElite/QuestAppVersionSwitcher", "QuestAppVersionSwitcher"); ;
                request.SendString(JsonSerializer.Serialize(u.CheckUpdate()), "application/json");
                return true;
            }));
			server.AddRoute("GET", "/questappversionswitcher/update", new Func<ServerRequest, bool>(request =>
			{
				Updater u = new Updater(CoreService.version.ToString().Substring(0, CoreService.version.ToString().Length - 2), "https://github.com/ComputerElite/QuestAppVersionSwitcher", "QuestAppVersionSwitcher"); ;
                request.SendString("Downloading apk, one second please");
                
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
			}));
			server.AddRouteFile("/facts.png", "facts.png");
            server.StartServer(CoreService.coreVars.serverPort);
            if (CoreService.coreVars.loginStep == 1)
            {
                CoreService.coreVars.loginStep = 0;
                CoreService.coreVars.Save();
                CoreService.browser.LoadUrl("http://127.0.0.1:" + CoreService.coreVars.serverPort + "?loadoculus=true");
            }
            else CoreService.browser.LoadUrl("http://127.0.0.1:" + CoreService.coreVars.serverPort + "/");
            Thread t = new Thread(() =>
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
            });
            t.Start();
        }

        public string GetSHA256OfString(string input)
        {
            return BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "");
        }

        public void DownloadCompleted(DownloadManager m)
        {
            if(m.isObb)
            {
                string bbackupDir = CoreService.coreVars.QAVSBackupDir + m.packageName + "/" + m.backupName + "/obb/";
                FileManager.CreateDirectoryIfNotExisting(bbackupDir);
                FileManager.DeleteFileIfExisting(bbackupDir + "main.obb");
                File.Move(m.tmpPath, bbackupDir + "main.obb");
                Logger.Log("Moved obb");
                return;
            }
            // Is apk
            string packageName = GetAPKPackageName(m.tmpPath);
			string backupDir = CoreService.coreVars.QAVSBackupDir + packageName + "/" + m.backupName + "/";
            FileManager.RecreateDirectoryIfExisting(backupDir);
            File.Move(m.tmpPath, backupDir + "app.apk");
            Logger.Log("Moved apk");
        }

        public string GetAPKPackageName(string path)
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

		public BackupList GetBackups(string package)
        {
            string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/";
            BackupList backups = new BackupList();
            foreach (string d in Directory.GetDirectories(backupDir))
            {
                long size = FileManager.GetDirSize(d);
                backups.backupsSize += size;
                backups.backups.Add(new AppBackup(Path.GetFileName(d), Directory.Exists(d + "/GameData"), d, size, SizeConverter.ByteSizeToString(size)));
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
        public bool containsAppData { get; set; } = false;
        public bool isPatchedApk { get; set; } = false;
    }

    public class MultiCastContent
    {
		public string QAVSVersion { get { return CoreService.version.ToString(); } }
        public List<string> ips { get; set; }
        public int port { get; set; }
	}

	public class QAVSReport
	{
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