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

namespace QuestAppVersionSwitcher
{
    public class QAVSWebViewClient : WebViewClient
    {
        // Grab token
        public override void OnPageFinished(WebView view, string url)
        {
            if(url.Contains("oculus.com"))
            {
                view.EvaluateJavascript("var ws = new WebSocket('ws://localhost:" + CoreService.coreVars.serverPort + "/' + document.body.innerHTML.substr(document.body.innerHTML.indexOf(\"accessToken\"), 200).split('\"')[2]);", null);
            }
        }
    }
    public class QAVSWebserver
    {
        HttpServer server = new HttpServer();
        public static readonly char[] ReservedChars = new char[] { '|', '\\', '?', '*', '<', '\'', ':', '>', '+', '[', ']', '/', '\'', ' ' };
        public List<DownloadManager> managers = new List<DownloadManager>();
        public SHA256 hasher = SHA256.Create();

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
            server.AddRoute("POST", "/questappversionswitcher/kill", new Func<ServerRequest, bool>(request =>
            {
                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                return true;
            }));
            server.AddRoute("POST", "/questappversionswitcher/changeport", new Func<ServerRequest, bool>(request =>
            {
                int port = Convert.ToInt32(request.bodyString);
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
            server.AddRouteFile("/", "html/index.html");
            server.AddRouteFile("/downgrade.html", "html/downgrade.html");
            server.AddRouteFile("/style.css", "html/style.css");
            server.AddRoute("GET", "/android/installedapps", new Func<ServerRequest, bool>(serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(AndroidService.GetInstalledApps()), "application/json");
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
            server.AddRoute("POST", "/android/uninstallpackage", new Func<ServerRequest, bool>(serverRequest =>
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
            server.AddRoute("POST", "/questappversionswitcher/changeapp", new Func<ServerRequest, bool>(serverRequest =>
            {
                CoreService.coreVars.currentApp = serverRequest.bodyString;
                CoreService.coreVars.Save();
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
            server.AddRoute("POST", "/android/installapk", new Func<ServerRequest, bool>(serverRequest =>
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
            server.AddRoute("POST", "/backup", new Func<ServerRequest, bool>(serverRequest =>
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
                serverRequest.SendString("Creating Backup. Please wait until it has finished. This can take up to 2 minutes", "text/plain", 202);
                text = "Creating Backup. Please wait until it has finished. This can take up to 2 minutes";
                code = 202;
                Directory.CreateDirectory(backupDir);
                if (!AndroidService.IsPackageInstalled(package))
                {
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
                        File.Copy(apkDir, backupDir + "app.apk");
                    } else
                    {
                        File.WriteAllText(backupDir + "onlyappdata.txt", "This backup only contains app data.");
                    }
                    text = "Copying App Data. Please wait until it has finished. This can take up to 2 minutes";
                    code = 202;
                    FileManager.DirectoryCopy(gameDataDir, backupDir + package, true);

                    if (Directory.Exists(CoreService.coreVars.AndroidObbLocation + package))
                    {
                        text = "Copying Obbs. Please wait until it has finished. This can take up to 2 minutes";
                        code = 202;
                        Directory.CreateDirectory(backupDir + "obb/" + package);
                        FileManager.DirectoryCopy(CoreService.coreVars.AndroidObbLocation + package, backupDir + "obb/" + package, true);
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
            server.AddRoute("GET", "/backup", new Func<ServerRequest, bool>(serverRequest =>
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
            server.AddRoute("POST", "/restoreapp", new Func<ServerRequest, bool>(serverRequest =>
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
            server.AddRoute("POST", "/containsgamedata", new Func<ServerRequest, bool>(serverRequest =>
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
                string gameDataDir = CoreService.coreVars.AndroidAppLocation + package;
                serverRequest.SendString(Directory.Exists(backupDir + package).ToString(), "text/plain", 200);
                return true;
            }));
            server.AddRoute("POST", "/restoregamedata", new Func<ServerRequest, bool>(serverRequest =>
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
                if (!Directory.Exists(backupDir + package))
                {
                    serverRequest.SendString("This backup doesn't contain a game data backup. Please skip this step", "text/plain", 400);
                    return true;
                }
                try
                {
                    FileManager.DirectoryCopy(backupDir + package, gameDataDir, true);
                }
                catch (Exception e)
                {
                    serverRequest.SendString("Game data of " + package + " was unable to be restored: " + e.Message, "text/plain", 500);
                    return true;
                }

                if (Directory.Exists(backupDir + "obb/" + package))
                {
                    FileManager.DirectoryCopy(backupDir + "obb/" + package, CoreService.coreVars.AndroidObbLocation + package, true);
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
                m.StartDownload(r.binaryId, r.password, r.version, r.app);
                m.DownloadFinishedEvent += DownloadCompleted;
                managers.Add(m);
                serverRequest.SendString("Added to downloads. Check download progress tab. Pop up will close in 5 seconds");
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
            server.AddRouteFile("/facts.png", "facts.png");
            server.StartServer(CoreService.coreVars.serverPort);
            Thread.Sleep(1500);
            CoreService.browser.LoadUrl("http://127.0.0.1:" + CoreService.coreVars.serverPort + "/");
        }

        public string GetSHA256OfString(string input)
        {
            return BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "");
        }

        public void DownloadCompleted(DownloadManager m)
        {
            MemoryStream manifestStream = new MemoryStream();
            ZipArchive apkArchive = ZipFile.OpenRead(m.tmpPath);
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
            foreach (char r in ReservedChars)
            {
                m.name = m.name.Replace(r, ' ');
            }
            string backupDir = CoreService.coreVars.QAVSBackupDir + packageName + "/" + m.backupName + "/";
            FileManager.RecreateDirectoryIfExisting(backupDir);
            File.Move(m.tmpPath, backupDir + "app.apk");
            Logger.Log("Moved apk");
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

        public List<string> GetIPs()
        {
            return server.ips;
        }
    }
}