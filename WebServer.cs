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

namespace QuestAppVersionSwitcher
{
    public class QAVSWebserver
    {
        HttpServer server = new HttpServer();
        public static readonly char[] ReservedChars = new char[] { '|', '\\', '?', '*', '<', '\'', ':', '>', '+', '[', ']', '/', '\'', ' ' };

        public void Start()
        {
            server.AddRouteFile("/", "html/index.html");
            server.AddRouteFile("/style.css", "html/style.css");
            server.AddRoute("GET", "/android/installedapps", new Func<ServerRequest, bool>(serverRequest =>
            {
                serverRequest.SendString(JsonSerializer.Serialize(AndroidService.GetInstalledApps()), "application/json");
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
                if(location == null)
                {
                    serverRequest.SendString("package not found", "text/plain", 400);
                } else
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
                if(AndroidService.IsPackageInstalled(serverRequest.bodyString))
                {
                    CoreService.coreVars.currentApp = serverRequest.bodyString;
                    CoreService.coreVars.Save();
                    serverRequest.SendString("App changed to " + serverRequest.bodyString);
                } else
                {
                    serverRequest.SendString("The package " + serverRequest.bodyString + " isn't installed", "text/plain", 400);
                }
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
                serverRequest.SendString("This Endpoint has been deactivated", "text/plain", 503);
                return true;

                // Deactivated
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
                } else
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
                if(!IsNameFileNameSafe(backupname))
                {
                    serverRequest.SendString("Your Backup name contains a forbidden character. Please remove them. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                if (!IsNameFileNameSafe(package))
                {
                    serverRequest.SendString("Your package contains a forbidden character. You can not backup it. Forbidden characters are: " + String.Join(' ', ReservedChars) + "and space. Tip: replace spaces with _", "text/plain", 400);
                    return true;
                }
                if(backupname == "")
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
                if(!AndroidService.IsPackageInstalled(package))
                {
                    text = package + " is not installed. Please select a different app.";
                    code = 400;
                    return true;
                }
                string apkDir = AndroidService.FindAPKLocation(package);
                string gameDataDir = CoreService.coreVars.AndroidAppLocation + package;
                try
                {
                    text = "Copying APK. Please wait until it has finished. This can take up to 2 minutes";
                    code = 202;
                    File.Copy(apkDir, backupDir + "app.apk");
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
                } catch (Exception e)
                {
                    text = "Backup failed: " + e.Message;
                    code = 500;
                    return true;
                }
                
                text = "Backup of " + package + " with the name " + backupname + " finished";
                code = 200;
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
                if(!File.Exists(backupDir + "app.apk"))
                {
                    serverRequest.SendString("Critical: APK doesn't exist in Backup. This Backup is useless. Please restart the app and choose a different one.", "text/plain", 500);
                    return true;
                }
                AndroidService.InitiateInstallApk(backupDir + "app.apk");
                serverRequest.SendString("Started apk install", "text/plain", 200);
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
                if(!Directory.Exists(backupDir + package))
                {
                    serverRequest.SendString("This backup doesn't contain a game data backup. Please skip this step", "text/plain", 400);
                    return true;
                }
                try
                {
                    FileManager.DirectoryCopy(backupDir + package, gameDataDir, true);
                } catch (Exception e)
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
                serverRequest.SendString(ByteSizeToString(FileManager.GetDirSize(CoreService.coreVars.QAVSBackupDir)));
                return true;
            }));
            server.AddRouteFile("/facts.png", "facts.png");
            server.StartServer(50001);
            Thread.Sleep(1000);
            CoreService.browser.LoadUrl("http://127.0.0.1:50001/");
        }

        public BackupList GetBackups(string package)
        {
            string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/";
            BackupList backups = new BackupList();
            foreach (string d in Directory.GetDirectories(backupDir))
            {
                long size = FileManager.GetDirSize(d);
                backups.backupsSize += size;
                backups.backups.Add(new AppBackup(Path.GetFileName(d), Directory.Exists(d + "/GameData"), d, size, ByteSizeToString(size)));
            }
            if (File.Exists(backupDir + "lastRestored.txt")) backups.lastRestored = File.ReadAllText(backupDir + "lastRestored.txt");
            backups.backupsSizeString = ByteSizeToString(backups.backupsSize);
            return backups;
        }

        public string ByteSizeToString(long input, int decimals = 2)
        {
            // TB
            if (input > 1099511627776) return String.Format("{0:0." + new string('#', decimals) + "}", input / 1099511627776.0) + " TB";
            // GB
            else if (input > 1073741824) return String.Format("{0:0." + new string('#', decimals) + "}", input / 1073741824.0) + " GB";
            // MB
            else if (input > 1048576) return String.Format("{0:0." + new string('#', decimals) + "}", input / 1048576.0) + " MB";
            // KB
            else if (input > 1024) return String.Format("{0:0." + new string('#', decimals) + "}", input / 1024.0) + " KB";
            // Bytes
            else return input + " Bytes";
        }

        public bool IsNameFileNameSafe(string name)
        {
            foreach(char c in ReservedChars)
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