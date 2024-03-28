#nullable enable
using ComputerUtils.Android.AndroidTools;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.ClientModels;
using QuestAppVersionSwitcher.Core;
using QuestPatcher.Axml;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using QuestAppVersionSwitcher.Mods;
using QuestPatcher.QMod;
using QuestPatcher.Zip;

namespace QuestAppVersionSwitcher
{
    // A lot stolen from QuestPatcher
    public class PatchingManager
    {
        /// <summary>
        /// package id, gets set by PatchManifest
        /// </summary>
        private static string packageId = "";
        
        public const string QAVSTagName = "modded.json";
        public const string LegacyTagName = "modded";
        public static readonly string[] OtherTagNames = { "BMBF.modded", "modded" };
        public const string ManifestPath = "AndroidManifest.xml";
        public static readonly Uri AndroidNamespaceUri = new Uri("http://schemas.android.com/apk/res/android");

        public static readonly string mainScotlandLoaderVersion = "v0.1.0-alpha";
        public static readonly string scotland2Version = "v0.1.4";
        public static readonly string questLoaderVersion = "v1.3.0";

        // Attribute resource IDs, used during manifest patching
        public const int NameAttributeResourceId = 16842755;
        public const int RequiredAttributeResourceId = 16843406;
        public const int DebuggableAttributeResourceId = 16842767;
        public const int LegacyStorageAttributeResourceId = 16844291;
        public const int ValueAttributeResourceId = 16842788;
        private const int AuthoritiesAttributeResourceId = 16842776;

        // lib paths Scotland2
        public static string libMainScotlandPath = CoreService.coreVars.QAVSPatchingFilesDir + "libscotland.so";
        public static string libScotland2Path = CoreService.coreVars.QAVSPatchingFilesDir + "libsl2.so";
        public static string mainLoaderScotlandVersionLocation = CoreService.coreVars.QAVSPatchingFilesDir + "mainLoaderScotlandVersion.txt";
        public static string scotland2VersionLocation = CoreService.coreVars.QAVSPatchingFilesDir + "scotland2Version.txt";

        
        // lib paths QuestLoader
        public static string libMain32Path = CoreService.coreVars.QAVSPatchingFilesDir + "libmain32.so";
        public static string libMain64Path = CoreService.coreVars.QAVSPatchingFilesDir + "libmain64.so";
        public static string libModloader32Path = CoreService.coreVars.QAVSPatchingFilesDir + "libmodloader32.so";
        public static string libModloader64Path = CoreService.coreVars.QAVSPatchingFilesDir + "libmodloader64.so";
        public static string questLoaderVersionLocation = CoreService.coreVars.QAVSPatchingFilesDir + "QuestLoaderVersion.txt";
        
        public static void DownloadDependencies()
        {
            if (CoreService.coreVars.patchingPermissions.modloader == ModLoader.Scotland2)
            {
                string currentMainLoaderScotlandVersion = File.Exists(mainLoaderScotlandVersionLocation) ? File.ReadAllText(mainLoaderScotlandVersionLocation) : "";
                string currentSL2Version = File.Exists(scotland2VersionLocation) ? File.ReadAllText(scotland2VersionLocation) : "";
                DownloadFileIfMissing(currentMainLoaderScotlandVersion, mainScotlandLoaderVersion, libMainScotlandPath, "https://github.com/sc2ad/LibMainLoader/releases/download/" + mainScotlandLoaderVersion + "/libmain.so");
                DownloadFileIfMissing(currentSL2Version, scotland2Version, libScotland2Path, "https://github.com/sc2ad/scotland2/releases/download/" + scotland2Version + "/libsl2.so");
                File.WriteAllText(mainLoaderScotlandVersionLocation, mainScotlandLoaderVersion);
                File.WriteAllText(scotland2VersionLocation, scotland2Version);
            } else if (CoreService.coreVars.patchingPermissions.modloader == ModLoader.QuestLoader)
            {
                string currentVersion = File.Exists(questLoaderVersionLocation) ? File.ReadAllText(CoreService.coreVars.QAVSPatchingFilesDir + "QuestLoaderVersion.txt") : "";
                DownloadFileIfMissing(currentVersion, questLoaderVersion, libMain32Path, "https://github.com/sc2ad/QuestLoader/releases/download/" + questLoaderVersion + "/libmain32.so");
                DownloadFileIfMissing(currentVersion, questLoaderVersion, libMain64Path, "https://github.com/sc2ad/QuestLoader/releases/download/" + questLoaderVersion + "/libmain64.so");
                DownloadFileIfMissing(currentVersion, questLoaderVersion, libModloader32Path, "https://github.com/sc2ad/QuestLoader/releases/download/" + questLoaderVersion + "/libmodloader32.so");
                DownloadFileIfMissing(currentVersion, questLoaderVersion, libModloader64Path, "https://github.com/sc2ad/QuestLoader/releases/download/" + questLoaderVersion + "/libmodloader64.so");
                File.WriteAllText(questLoaderVersionLocation, questLoaderVersion);
            }
            QAVSWebserver.patchStatus.doneOperations = 2;
            QAVSWebserver.patchStatus.progress = .1;
            QAVSWebserver.BroadcastPatchingStatus();
        }

        public static void DownloadFileIfMissing(string currentVersion, string targetVersion, string filePath, string downloadLink)
        {
            if (!File.Exists(filePath) || currentVersion != targetVersion)
            {
                string fileName = Path.GetFileName(filePath);
                QAVSWebserver.patchStatus.currentOperation = "Downloading dependency " + fileName;
                QAVSWebserver.BroadcastPatchingStatus();
                Logger.Log(fileName + " doesn't exist. Downloading from " + downloadLink);
                WebClient c = new WebClient();
                c.DownloadFile(downloadLink, filePath);
            }
            else
            {
                Logger.Log(Path.GetFileName(filePath) + " exists. Not downloading");
            }
        }

        public static bool IsAPKModded(ApkZip apkArchive)
        {
            return apkArchive.ContainsFile(QAVSTagName) || OtherTagNames.Any(tagName => apkArchive.ContainsFile(tagName));
        }
        
        public static bool IsAPKModded(ZipArchive apkArchive)
        {
            return apkArchive.GetEntry(QAVSTagName) != null || OtherTagNames.Any(tagName => apkArchive.GetEntry(tagName) != null);
        }

        /// <summary>
        /// Gets the modded json of the currently selected app
        /// </summary>
        /// <returns></returns>
        [CanBeNull]
        public static ModdedJson GetModdedJson()
        {
            string? apkLocation = AndroidService.FindAPKLocation(CoreService.coreVars.currentApp);
            if (apkLocation == null) return null;
            using ZipArchive apk = ZipFile.OpenRead(apkLocation);
            ModdedJson? json = GetModdedJson(apk);
            return json;
        }

        [CanBeNull]
        public static ModdedJson GetModdedJson(ZipArchive apkArchive)
        {
            if (apkArchive.GetEntry(QAVSTagName) == null) return null;
            using Stream stream = apkArchive.GetEntry(QAVSTagName).Open();
            string json;
            using (var sr = new StreamReader(stream, Encoding.UTF8))
            {
                json = sr.ReadToEnd();
            }
            //Logger.Log(json);
            return JsonSerializer.Deserialize<ModdedJson>(json);
        }


        public static void PatchAPK(ApkZip apkArchive, string appLocation, bool forcePatch)
        {
            if (!forcePatch && IsAPKModded(apkArchive))
            {
                QAVSWebserver.patchStatus.done = true;
                QAVSWebserver.patchStatus.doneOperations = QAVSWebserver.patchStatus.totalOperations;
                QAVSWebserver.patchStatus.currentOperation = "App is already patched";
                QAVSWebserver.patchStatus.progress = 1;
                QAVSWebserver.BroadcastPatchingStatus();
                return;
            }
            DownloadDependencies();
            PatchManifest(apkArchive);
            QAVSWebserver.patchStatus.doneOperations = 3;
            QAVSWebserver.patchStatus.progress = .2;
            QAVSWebserver.BroadcastPatchingStatus();
            AddLibsAndPatchGame(apkArchive);
            QAVSWebserver.patchStatus.doneOperations = 5;
            QAVSWebserver.patchStatus.progress = .55;
            QAVSWebserver.patchStatus.currentOperation = "Signing apk";
            QAVSWebserver.BroadcastPatchingStatus();
            apkArchive.Dispose();
            Console.WriteLine("Opening patched apk to get patching status");
            
            QAVSWebserver.patchStatus.doneOperations = 8;
            QAVSWebserver.patchStatus.progress = .93;
            QAVSWebserver.patchStatus.currentOperation = "Registering game version in QAVS. This may take 5 minutes.";
            QAVSWebserver.BroadcastPatchingStatus();
            
            // Get app version
            Logger.Log("Opening patched apk to get patching status");
            ZipArchive a = ZipFile.OpenRead(appLocation);
            PatchingStatus status = GetPatchingStatus(a);
            // Move apk to correct backup folder
            string backupName = QAVSWebserver.MakeFileNameSafe(status.version) + "_patched";
            string backupDir = CoreService.coreVars.QAVSBackupDir + packageId + "/" + backupName + "/";
            FileManager.RecreateDirectoryIfExisting(backupDir);

            if (Directory.Exists(CoreService.coreVars.QAVSTmpPatchingObbDir))
            {
                Directory.CreateDirectory(backupDir + "obb/");
                Directory.Move(CoreService.coreVars.QAVSTmpPatchingObbDir, backupDir + "obb/" + packageId);
            }

            File.Move(appLocation, backupDir + "app.apk");
            Logger.Log("Moved apk");
            
            QAVSWebserver.patchStatus.doneOperations = 9;
            QAVSWebserver.patchStatus.progress = .96;
            QAVSWebserver.patchStatus.currentOperation = "Trying to copy obbs";
            QAVSWebserver.BroadcastPatchingStatus();

            QAVSWebserver.patchStatus.doneOperations = 10;
            QAVSWebserver.patchStatus.progress = 1;
            QAVSWebserver.patchStatus.done = true;
            QAVSWebserver.patchStatus.doneOperations = QAVSWebserver.patchStatus.totalOperations;
            QAVSWebserver.patchStatus.currentOperation = "Done";
            QAVSWebserver.patchStatus.backupName = backupName;
            QAVSWebserver.BroadcastPatchingStatus();

            BackupManager.GetBackupInfo(backupDir, true);
        }

        // Uses https://github.com/Lauriethefish/QuestUnstrippedUnity to download an appropriate unstripped libunity.so for beat saber if there is one
        public static bool AttemptDownloadUnstrippedUnity(string version)
        {
            Logger.Log("Checking index for unstrippedUnity");
            try
            {
                WebClient c = new WebClient();
                string libUnityIndexString = ExternalFilesDownloader.DownloadStringWithTimeout("https://raw.githubusercontent.com/Lauriethefish/QuestUnstrippedUnity/main/index.json", 10000);
                Dictionary<string, Dictionary<string, string>> index =
                    JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(libUnityIndexString);
                string appId = CoreService.coreVars.currentApp;
                if (index.ContainsKey(appId))
                {
                    if (index[appId].ContainsKey(version))
                    {
                        ExternalFilesDownloader.DownloadUrl("https://raw.githubusercontent.com/Lauriethefish/QuestUnstrippedUnity/main/versions/" +
                                                            index[appId][version] + ".so", CoreService.coreVars.QAVSTmpPatchingDir + "libunity.so", -1, "");
                        return true;
                    }
                    else
                    {
                        Logger.Log("No unstripped libunity found. It does exist for another version of the app");
                    }
                }
                else
                {
                    Logger.Log("No unstripped libunity found.", LoggingType.Warning);
                }
            }
            catch (Exception e)
            {
                Logger.Log("Failed to check index for unstripped libunity: " + e, LoggingType.Warning);
            }
            return false;
        }
        
        /// <summary>
        /// Copies the file with the given path into the APK.
        /// </summary>
        /// <param name="filePath">The path to the file to copy into the APK</param>
        /// <param name="apkFilePath">The name of the file in the APK to create</param>
        /// <param name="failIfExists">Whether to throw an exception if the file already exists</param>
        /// <param name="apk">The apk to copy the file into</param>
        /// <exception cref="PatchingException">If the file already exists in the APK, if configured to throw.</exception>
        private static void AddFileToApkSync(string filePath, string apkFilePath, ApkZip apk)
        {
            using var fileStream = File.OpenRead(filePath);
            apk.AddFile(apkFilePath, fileStream, CompressionLevel.Optimal);
        }

        public static void PatchUnityIl2CppApp(ApkZip apkArchive, ref ModdedJson moddedJson)
        {
            bool isApk64Bit = apkArchive.ContainsFile("lib/arm64-v8a/libil2cpp.so");

            QAVSWebserver.patchStatus.currentOperation = "Adding unstripped libunity to APK if available";
            QAVSWebserver.BroadcastPatchingStatus();
            string libpath = isApk64Bit ? "lib/arm64-v8a/" : "lib/armeabi-v7a/";
            string versionName = GetPatchingStatus(apkArchive).version;
            
            if (apkArchive.ContainsFile(libpath + "libunity.so") && AttemptDownloadUnstrippedUnity(versionName))
            {
                Logger.Log("Adding libunity.so to " + (apkArchive.ContainsFile("lib/arm64-v8a/libil2cpp.so") ? "lib/arm64-v8a/libunity.so" : "lib/armeabi-v7a/libunity.so"));
                
                AddFileToApkSync(CoreService.coreVars.QAVSTmpPatchingDir + "libunity.so", libpath + "libunity.so", apkArchive);
                moddedJson.modifiedFiles.Add(libpath + "libunity.so");
            }

            if (CoreService.coreVars.patchingPermissions.modloader == ModLoader.Scotland2)
            {
                // Copy scotland2 to correct location
                QAVSWebserver.patchStatus.progress = .35;
                QAVSWebserver.patchStatus.currentOperation = "Copying scotland2";
                QAVSWebserver.BroadcastPatchingStatus();
                FileManager.CreateDirectoryIfNotExisting("/sdcard/ModData/" + CoreService.coreVars.currentApp + "/Modloader");
                File.Copy(libScotland2Path, "/sdcard/ModData/" + CoreService.coreVars.currentApp + "/Modloader/libsl2.so", true);


                QAVSWebserver.patchStatus.progress = .45;
                QAVSWebserver.patchStatus.currentOperation = "Adding libmain";
                QAVSWebserver.BroadcastPatchingStatus();
                moddedJson.modifiedFiles.Add(libpath + "libmain.so");
                AddFileToApkSync(libMainScotlandPath, libpath + "libmain.so", apkArchive);

                moddedJson.modloaderName = "Scotland2";
                moddedJson.modloaderVersion = scotland2Version;
            } else if (CoreService.coreVars.patchingPermissions.modloader == ModLoader.QuestLoader)
            {
                QAVSWebserver.patchStatus.progress = .35;
                QAVSWebserver.patchStatus.currentOperation = "Adding modloader";
                QAVSWebserver.BroadcastPatchingStatus();
                AddFileToApkSync(isApk64Bit ? libModloader64Path : libModloader32Path, libpath + "libmodloader.so", apkArchive);
                moddedJson.modifiedFiles.Add(libpath + "libmodloader.so");

                QAVSWebserver.patchStatus.progress = .45;
                QAVSWebserver.patchStatus.currentOperation = "Adding libmain";
                QAVSWebserver.BroadcastPatchingStatus();
                moddedJson.modifiedFiles.Add(libpath + "libmain.so");
                AddFileToApkSync(isApk64Bit ? libMain64Path : libMain32Path, libpath + "libmain.so", apkArchive);

                moddedJson.modloaderName = "QuestLoader";
                moddedJson.modloaderVersion = questLoaderVersion;
            }
        }

        public static void AddLibsAndPatchGame(ApkZip apkArchive)
        {
            QAVSWebserver.patchStatus.progress = .25;
            QAVSWebserver.BroadcastPatchingStatus();

            ModdedJson moddedJson = new ModdedJson();

            if (!CoreService.coreVars.patchingPermissions.resignOnly)
            {
                /// Diffrent patching apps
                if(apkArchive.ContainsFile("lib/arm64-v8a/libil2cpp.so") || apkArchive.ContainsFile("lib/armeabi-v7a/libil2cpp.so"))
                {
                    // Patch Unity il2cpp game
                    PatchUnityIl2CppApp(apkArchive, ref moddedJson);
                }
            }

            QAVSWebserver.patchStatus.currentOperation = "Creating modding json";
            QAVSWebserver.patchStatus.progress = .5;
            QAVSWebserver.patchStatus.doneOperations = 4;
            QAVSWebserver.BroadcastPatchingStatus();
            moddedJson.modifiedFiles.Add(ManifestPath);
            apkArchive.AddFile(LegacyTagName, new MemoryStream(), null);
            moddedJson.patcherVersion = CoreService.version.ToString();
            moddedJson.patcherName = "QuestAppVersionSwitcher";

            string moddedJsonContent = JsonSerializer.Serialize(moddedJson, typeof(ModdedJson),
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });
            apkArchive.AddFile(QAVSTagName, new MemoryStream(Encoding.UTF8.GetBytes(moddedJsonContent)), null);
        }

        public static ISet<string> GetExistingChildren(AxmlElement manifest, string childNames)
        {
            HashSet<string> result = new HashSet<string>();

            foreach (AxmlElement element in manifest.Children)
            {
                if (element.Name != childNames) { continue; }

                List<AxmlAttribute> nameAttributes = element.Attributes.Where(attribute => attribute.Namespace == AndroidNamespaceUri && attribute.Name == "name").ToList();
                // Only add children with the name attribute
                if (nameAttributes.Count > 0) { result.Add((string)nameAttributes[0].Value); }
            }

            return result;
        }

        public static PatchingStatus GetPatchingStatus(string app = null)
        {
            if (app == null) app = CoreService.coreVars.currentApp;
            if (!AndroidService.IsPackageInstalled(app))
            {
                return new PatchingStatus
                {
                    isInstalled = false,
                    canBePatched = false,
                };
            }
            
            using ZipArchive apk = ZipFile.OpenRead(AndroidService.FindAPKLocation(app));
            PatchingStatus s =  GetPatchingStatus(apk);
            return s;
        }
        
        public static PatchingStatus GetPatchingStatusOfBackup(string package, string backupName)
        {
            string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupName + "/";
            using ZipArchive apk = ZipFile.OpenRead(backupDir + "app.apk");
            PatchingStatus s =  GetPatchingStatus(apk);
            return s;
        }
        
        /// <summary>
        /// WARNING! ONLY POPULATES VERSION AND VERSIONCODE
        /// </summary>
        /// <param name="apk"></param>
        /// <returns></returns>
        public static PatchingStatus GetPatchingStatus(ApkZip apk)
        {
            PatchingStatus status = new PatchingStatus();
            MemoryStream manifestStream = new MemoryStream();
            using (Stream s = apk.OpenReader(ManifestPath))
            {
                s.CopyTo(manifestStream);
            }
            manifestStream.Position = 0;
            AxmlElement manifest = AxmlLoader.LoadDocument(manifestStream);
            foreach (AxmlAttribute a in manifest.Attributes)
            {
                if (a.Name == "versionName")
                {
                    status.version = a.Value.ToString();
                }
                if (a.Name == "versionCode")
                {
                    status.versionCode = a.Value.ToString();
                }
            }
            manifestStream.Close();
            manifestStream.Dispose();
            return status;
        }

        public static PatchingStatus GetPatchingStatus(ZipArchive apk)
        {
            PatchingStatus status = new PatchingStatus();
            MemoryStream manifestStream = new MemoryStream();
            using (Stream s = apk.GetEntry(ManifestPath).Open())
            {
                s.CopyTo(manifestStream);
            }
            manifestStream.Position = 0;
            AxmlElement manifest = AxmlLoader.LoadDocument(manifestStream);
            string packageId = "";
            foreach (AxmlAttribute a in manifest.Attributes)
            {
                if (a.Name == "versionName")
                {
                    status.version = a.Value.ToString();
                }
                if (a.Name == "versionCode")
                {
                    status.versionCode = a.Value.ToString();
                }
            }
            AxmlElement appElement = manifest.Children.Single(element => element.Name == "application");
            status.copyOf = null;
            foreach (AxmlElement e in appElement.Children)
            {
                if (e.Attributes.Any(x => x.Name == "name" && x.Value.ToString() == "QAVS.copyOf"))
                {
                    status.copyOf = (string)e.Attributes.FirstOrDefault(x => x.Name == "value").Value;
                    //Logger.Log("App is copy of " + status.copyOf);
                }
            }
            status.isPatched = IsAPKModded(apk);
            status.moddedJson = GetModdedJson(apk);
            manifestStream.Close();
            manifestStream.Dispose();
            return status;
        }

        public static void AddNameAttribute(AxmlElement element, string name)
        {
            element.Attributes.Add(new AxmlAttribute("name", AndroidNamespaceUri, NameAttributeResourceId, name));
        }

        public static bool PatchManifest(ApkZip apkArchive)
        {
            QAVSWebserver.patchStatus.currentOperation = "Patching manifest";
            QAVSWebserver.BroadcastPatchingStatus();
            if (!apkArchive.ContainsFile(ManifestPath))
            {
                QAVSWebserver.patchStatus.error = true;
                QAVSWebserver.patchStatus.errorText = "Android Manifest doesn't exist. Cannot mod game";
                QAVSWebserver.BroadcastPatchingStatus();
                return false;
            }

            // The AMXL loader requires a seekable stream
            MemoryStream ms = new MemoryStream();
            using (Stream stream = apkArchive.OpenReader(ManifestPath))
            {
                stream.CopyTo(ms);
            }

            ms.Position = 0;
            AxmlElement manifest = AxmlLoader.LoadDocument(ms);

            // First we add permissions and features to the APK for modding
            List<string> addingPermissions = new List<string>();
            List<UsesFeature> addingFeatures = new List<UsesFeature>();
            PatchingPermissions permissions = CoreService.coreVars.patchingPermissions;
            addingFeatures.AddRange(permissions.otherFeatures);
            if (permissions.externalStorage)
            {
                // Technically, we only need READ_EXTERNAL_STORAGE and WRITE_EXTERNAL_STORAGE, but we also add MANAGE_EXTERNAL_STORAGE as this is what Android 11 needs instead
                addingPermissions.AddRange(new[]
                {
                    "android.permission.READ_EXTERNAL_STORAGE",
                    "android.permission.WRITE_EXTERNAL_STORAGE",
                    "android.permission.MANAGE_EXTERNAL_STORAGE"
                });
            }

            if (permissions.handTracking)
            {
                // For some reason these are separate permissions, but we need both of them
                addingPermissions.AddRange(new[]
                {
                    "oculus.permission.handtracking",
                    "com.oculus.permission.HAND_TRACKING"
                });
                // Tell Android (and thus Oculus home) that this app supports hand tracking and we can launch the app with it
                addingFeatures.Add(new UsesFeature { name = "oculus.software.handtracking", required = false });
            }

            if (permissions.openXR)
            {
                Logger.Log("Adding OpenXR permission . . .");

                addingPermissions.AddRange(new[]
                {
                    "org.khronos.openxr.permission.OPENXR",
                    "org.khronos.openxr.permission.OPENXR_SYSTEM",
                });

                AxmlElement providerElement = new AxmlElement("provider")
                {
                    Attributes =
                    {
                        new AxmlAttribute("authorities", AndroidNamespaceUri, AuthoritiesAttributeResourceId,
                            "org.khronos.openxr.runtime_broker;org.khronos.openxr.system_runtime_broker")
                    },
                };
                AxmlElement runtimeIntent = new AxmlElement("intent")
                {
                    Children =
                    {
                        new AxmlElement("action")
                        {
                            Attributes =
                            {
                                new AxmlAttribute("name", AndroidNamespaceUri, NameAttributeResourceId,
                                    "org.khronos.openxr.OpenXRRuntimeService")
                            },
                        },
                    },
                };
                AxmlElement layerIntent = new AxmlElement("intent")
                {
                    Children =
                    {
                        new AxmlElement("action")
                        {
                            Attributes =
                            {
                                new AxmlAttribute("name", AndroidNamespaceUri, NameAttributeResourceId,
                                    "org.khronos.openxr.OpenXRApiLayerService")
                            },
                        },
                    },
                };
                manifest.Children.Add(new AxmlElement("queries")
                {
                    Children =
                    {
                        providerElement,
                        runtimeIntent,
                        layerIntent,
                    },
                });
            }

            addingPermissions.AddRange(permissions.otherPermissions);

            // Find which features and permissions already exist to avoid adding existing ones
            ISet<string> existingPermissions = GetExistingChildren(manifest, "uses-permission");
            ISet<string> existingFeatures = GetExistingChildren(manifest, "uses-feature");

            foreach (string permission in addingPermissions)
            {
                if (existingPermissions.Contains(permission))
                {
                    continue;
                } // Do not add existing permissions

                Logger.Log("Adding permission " + permission);
                AxmlElement permElement = new AxmlElement("uses-permission");
                AddNameAttribute(permElement, permission);
                manifest.Children.Add(permElement);
            }

            foreach (UsesFeature feature in addingFeatures)
            {
                if (existingFeatures.Contains(feature.name))
                {
                    continue;
                } // Do not add existing features

                Logger.Log("adding feature " + feature.name);
                AxmlElement featureElement = new AxmlElement("uses-feature");
                AddNameAttribute(featureElement, feature.name);

                featureElement.Attributes.Add(new AxmlAttribute("required", AndroidNamespaceUri,
                    RequiredAttributeResourceId, feature.required));
                manifest.Children.Add(featureElement);
            }

            // Now we need to add the legacyStorageSupport and debuggable flags
            AxmlElement appElement = manifest.Children.Single(element => element.Name == "application");
            if (permissions.debug && !appElement.Attributes.Any(attribute => attribute.Name == "debuggable"))
            {
                Logger.Log("adding debugable flag");
                appElement.Attributes.Add(new AxmlAttribute("debuggable", AndroidNamespaceUri,
                    DebuggableAttributeResourceId, true));
            }

            if (permissions.externalStorage &&
                !appElement.Attributes.Any(attribute => attribute.Name == "requestLegacyExternalStorage"))
            {
                Logger.Log("adding legacy external storage flag");
                appElement.Attributes.Add(new AxmlAttribute("requestLegacyExternalStorage", AndroidNamespaceUri,
                    LegacyStorageAttributeResourceId, true));
            }


            switch (permissions.handTrackingVersion)
            {
                case HandTrackingVersion.Default:
                    Logger.Log("Not specifying hand tracking version. Latest will be used.");
                    break;
                case HandTrackingVersion.V1:
                    Logger.Log("Hand tracking V1 is deprecated");
                    break;
                case HandTrackingVersion.V1HighFrequency:
                    Logger.Log("Adding high-frequency V1 hand-tracking. . .");
                    AxmlElement frequencyElement = new AxmlElement("meta-data");
                    AddNameAttribute(frequencyElement, "com.oculus.handtracking.frequency");
                    frequencyElement.Attributes.Add(new AxmlAttribute("value", AndroidNamespaceUri,
                        ValueAttributeResourceId, "HIGH"));
                    appElement.Children.Add(frequencyElement);
                    break;
                case HandTrackingVersion.V2:
                    Logger.Log("Adding V2 hand-tracking. . .");
                    frequencyElement = new AxmlElement("meta-data");
                    AddNameAttribute(frequencyElement, "com.oculus.handtracking.version");
                    frequencyElement.Attributes.Add(new AxmlAttribute("value", AndroidNamespaceUri,
                        ValueAttributeResourceId, "V2.0"));
                    appElement.Children.Add(frequencyElement);
                    break;
                case HandTrackingVersion.V2_1:
                    Logger.Log("Adding V2.1 hand-tracking. . .");
                    frequencyElement = new AxmlElement("meta-data");
                    AddNameAttribute(frequencyElement, "com.oculus.handtracking.version");
                    frequencyElement.Attributes.Add(new AxmlAttribute("value", AndroidNamespaceUri,
                        ValueAttributeResourceId, "V2.1"));
                    appElement.Children.Add(frequencyElement);
                    break;
            }

            // Add custom package id
            packageId = "";
            string orgPackageId = "";

            for (int i = 0; i < manifest.Attributes.Count; i++)
            {
                if (manifest.Attributes[i].Name == "package")
                {
                    orgPackageId = (string)manifest.Attributes[i].Value;
                    Logger.Log("Original package id is " + orgPackageId);
                    if (permissions.customPackageId != "")
                    {
                        Logger.Log("Patching to " + permissions.customPackageId);
                        manifest.Attributes[i].Value = permissions.customPackageId;
                    }

                    packageId = (string)manifest.Attributes[i].Value;
                }
            }
            
            // Update Splash screen
            if (permissions.splashImageBase64 != "")
            {
                Logger.Log("Updating splash screen");
                string trimmedBase64 = permissions.splashImageBase64;
                if (trimmedBase64.Contains(",")) trimmedBase64 = trimmedBase64.Split(',')[1];
                byte[] data = Convert.FromBase64String(trimmedBase64);
                using (MemoryStream splash = new MemoryStream(data))
                {
                    splash.Position = 0;
                    apkArchive.AddFile("assets/vr_splash.png", splash, CompressionLevel.Optimal);
                }
    
                
                bool exists = false;
                foreach (AxmlElement e in appElement.Children)
                {
                    if (e.Attributes.Any(x => x.Name == "name" && x.Value.ToString() == "com.oculus.ossplash"))
                    {
                        e.Attributes.RemoveAll(x => x.Name == "value");
                        e.Attributes.Add(new AxmlAttribute("value", AndroidNamespaceUri,
                            ValueAttributeResourceId, "true"));
                        exists = true;
                    }
                }
                if (!exists)
                {
                    // Add ossplash meta-data if it's not already present
                    AxmlElement ossplash = new AxmlElement("meta-data");
                    AddNameAttribute(ossplash, "com.oculus.ossplash");
                    ossplash.Attributes.Add(new AxmlAttribute("value", AndroidNamespaceUri,
                        ValueAttributeResourceId, "true"));
                    appElement.Children.Add(ossplash);
                }
                
            }
            
            
            AxmlElement copyOfElement = new AxmlElement("meta-data");
            AddNameAttribute(copyOfElement, "QAVS.copyOf");
            copyOfElement.Attributes.Add(new AxmlAttribute("value", AndroidNamespaceUri,
                ValueAttributeResourceId, orgPackageId));
            appElement.Children.Add(copyOfElement);
            

            QAVSWebserver.patchStatus.package = packageId;


            // Save the manifest using our AXML library
            Logger.Log("Saving manifest as AXML . . .");
            
            ms.SetLength(0);
            ms.Position = 0;
            AxmlSaver.SaveDocument(ms, manifest);
            ms.Position = 0;
            apkArchive.AddFile(ManifestPath, ms, CompressionLevel.Optimal);
            return true;
        }

        public static byte[] GetSplashCover(string apkLocation)
        {
            try
            {
                using ZipArchive a = ZipFile.OpenRead(apkLocation);
                if (a.GetEntry("assets/vr_splash.png") != null)
                {
                    using Stream s = a.GetEntry("assets/vr_splash.png").Open();
                    using MemoryStream ms = new MemoryStream();
                    s.CopyTo(ms);
                    return ms.ToArray();
                }
            }
            catch (Exception e)
            {
                Logger.Log("Error while getting splash cover: " + e.Message, LoggingType.Warning);
            }
            return null;
        }
    }
}