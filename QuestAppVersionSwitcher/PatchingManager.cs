using ComputerUtils.Android.AndroidTools;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.IO.Pem;
using Org.BouncyCastle.X509;
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
using Android.Webkit;
using Org.BouncyCastle.Bcpg.Sig;
using QuestPatcher.QMod;

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
        public const string TagPermission = "qavs.modded";
        public static readonly Uri AndroidNamespaceUri = new Uri("http://schemas.android.com/apk/res/android");

        public static readonly string mainScotlandLoaderVersion = "v0.1.0-alpha";
        public static readonly string scotland2Version = "v0.1.2";
        public static readonly string questLoaderVersion = "v1.2.3";

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

        public static bool IsAPKModded(ZipArchive apkArchive)
        {
            return apkArchive.GetEntry(QAVSTagName) != null || OtherTagNames.Any(tagName => apkArchive.GetEntry(tagName) != null);
        }

        /// <summary>
        /// Gets the modded json of the currently selected app
        /// </summary>
        /// <returns></returns>
        public static ModdedJson GetModdedJson()
        {
            ZipArchive apk = ZipFile.OpenRead(AndroidService.FindAPKLocation(CoreService.coreVars.currentApp));
            ModdedJson json = GetModdedJson(apk);
            apk.Dispose();
            return json;
        }

        public static ModdedJson GetModdedJson(ZipArchive apkArchive)
        {
            if (apkArchive.GetEntry(QAVSTagName) == null) return null;
            Stream stream = apkArchive.GetEntry(QAVSTagName).Open();
            string json = "";
            using (var sr = new StreamReader(stream, Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    json += line + "\n";
                }
            }
            return JsonSerializer.Deserialize<ModdedJson>(json);
        }


        public static async void PatchAPK(ZipArchive apkArchive, string appLocation, bool forcePatch)
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
            Dictionary<string, ApkSigner.PrePatchHash>? prePatchHashes = AddLibsAndPatchGame(apkArchive);
            apkArchive.Dispose();
            QAVSWebserver.patchStatus.doneOperations = 5;
            QAVSWebserver.patchStatus.progress = .55;
            QAVSWebserver.BroadcastPatchingStatus();

            if (!await ApkSigner.SignApkWithPatchingCertificate(appLocation, prePatchHashes))
            {
                return;
            }
            QAVSWebserver.patchStatus.doneOperations = 8;
            QAVSWebserver.patchStatus.progress = .95;
            QAVSWebserver.patchStatus.currentOperation = "Almost done. Hang tight";
            QAVSWebserver.BroadcastPatchingStatus();
            ZipArchive a = ZipFile.OpenRead(appLocation);
            PatchingStatus status = GetPatchingStatus(a);
            a.Dispose();
            string backupName = QAVSWebserver.MakeFileNameSafe(status.version) + "_patched";
            string backupDir = CoreService.coreVars.QAVSBackupDir + packageId + "/" + backupName + "/";
            FileManager.RecreateDirectoryIfExisting(backupDir);
            File.Move(appLocation, backupDir + "app.apk");
            Logger.Log("Moved apk");

            QAVSWebserver.patchStatus.doneOperations = 9;
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
            WebClient c = new WebClient();
            string libUnityIndexString = c.DownloadString("https://raw.githubusercontent.com/Lauriethefish/QuestUnstrippedUnity/main/index.json");
            Dictionary<string, Dictionary<string, string>> index = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(libUnityIndexString);
            string appId = CoreService.coreVars.currentApp;
            if (index.ContainsKey(appId))
            {
                if (index[appId].ContainsKey(version))
                {
                    c.DownloadFile("https://raw.githubusercontent.com/Lauriethefish/QuestUnstrippedUnity/main/versions/" + index[appId][version] + ".so", CoreService.coreVars.QAVSTmpPatchingDir + "libunity.so");
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
            return false;
        }

        public static void PatchUnityIl2CppApp(ZipArchive apkArchive, ref ModdedJson moddedJson)
        {
            bool isApk64Bit = apkArchive.GetEntry("lib/arm64-v8a/libil2cpp.so") != null;

            QAVSWebserver.patchStatus.currentOperation = "Adding unstripped libunity to APK if available";
            QAVSWebserver.BroadcastPatchingStatus();
            string libpath = isApk64Bit ? "lib/arm64-v8a/" : "lib/armeabi-v7a/";
            string versionName = GetPatchingStatus(apkArchive).version;
            
            if (apkArchive.GetEntry(libpath + "libunity.so") != null && AttemptDownloadUnstrippedUnity(versionName))
            {
                Logger.Log("Adding libunity.so to " + (apkArchive.GetEntry("lib/arm64-v8a/libil2cpp.so") != null ? "lib/arm64-v8a/libunity.so" : "lib/armeabi-v7a/libunity.so"));

                ZipArchiveEntry unity = apkArchive.GetEntry(libpath + "libunity.so");
                if (unity != null) unity.Delete();
                apkArchive.CreateEntryFromFile(CoreService.coreVars.QAVSTmpPatchingDir + "libunity.so", libpath + "libunity.so");
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
                ZipArchiveEntry main = apkArchive.GetEntry(libpath + "libmain.so");
                if (main != null) main.Delete();
                moddedJson.modifiedFiles.Add(libpath + "libmain.so");
                apkArchive.CreateEntryFromFile(libMainScotlandPath, libpath + "libmain.so");

                moddedJson.modloaderName = "Scotland2";
                moddedJson.modloaderVersion = scotland2Version;
            } else if (CoreService.coreVars.patchingPermissions.modloader == ModLoader.QuestLoader)
            {
                QAVSWebserver.patchStatus.progress = .35;
                QAVSWebserver.patchStatus.currentOperation = "Adding modloader";
                QAVSWebserver.BroadcastPatchingStatus();
                apkArchive.CreateEntryFromFile(isApk64Bit ? libModloader64Path : libModloader32Path, libpath + "libmodloader.so");
                moddedJson.modifiedFiles.Add(libpath + "libmodloader.so");

                QAVSWebserver.patchStatus.progress = .45;
                QAVSWebserver.patchStatus.currentOperation = "Adding libmain";
                QAVSWebserver.BroadcastPatchingStatus();
                ZipArchiveEntry main = apkArchive.GetEntry(libpath + "libmain.so");
                if (main != null) main.Delete();
                moddedJson.modifiedFiles.Add(libpath + "libmain.so");
                apkArchive.CreateEntryFromFile(isApk64Bit ? libMain64Path : libMain32Path, libpath + "libmain.so");

                moddedJson.modloaderName = "QuestLoader";
                moddedJson.modloaderVersion = questLoaderVersion;
            }
        }

        public static Dictionary<string, ApkSigner.PrePatchHash>? AddLibsAndPatchGame(ZipArchive apkArchive)
        {
            Dictionary<string, ApkSigner.PrePatchHash>? prePatchHashes;
            QAVSWebserver.patchStatus.currentOperation = "Preparing pre patch hashes";
            QAVSWebserver.BroadcastPatchingStatus();
            prePatchHashes = ApkSigner.CollectPrePatchHashes(apkArchive).Result;
            QAVSWebserver.patchStatus.progress = .25;
            QAVSWebserver.BroadcastPatchingStatus();

            ModdedJson moddedJson = new ModdedJson();

            if (!CoreService.coreVars.patchingPermissions.resignOnly)
            {
                /// Diffrent patching apps
                if(apkArchive.GetEntry("lib/arm64-v8a/libil2cpp.so") != null || apkArchive.GetEntry("lib/armeabi-v7a/libil2cpp.so") != null)
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
            apkArchive.CreateEntry(QAVSTagName);
            apkArchive.CreateEntry(LegacyTagName);
            moddedJson.patcherVersion = CoreService.version.ToString();
            moddedJson.patcherName = "QuestAppVersionSwitcher";
            
            using (StreamWriter writer = new StreamWriter(apkArchive.GetEntry(QAVSTagName).Open()))
            {
                writer.WriteLine(JsonSerializer.Serialize(moddedJson, typeof(ModdedJson), new JsonSerializerOptions
                {
                    WriteIndented = true,
                }));
                writer.Close();
            }
            return prePatchHashes;
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
            ZipArchive apk = ZipFile.OpenRead(AndroidService.FindAPKLocation(app));
            PatchingStatus s =  GetPatchingStatus(apk);
            apk.Dispose();
            return s;
        }
        
        public static PatchingStatus GetPatchingStatusOfBackup(string package, string backupName)
        {
            string backupDir = CoreService.coreVars.QAVSBackupDir + package + "/" + backupName + "/";
            ZipArchive apk = ZipFile.OpenRead(backupDir + "app.apk");
            PatchingStatus s =  GetPatchingStatus(apk);
            apk.Dispose();
            return s;
        }

        public static PatchingStatus GetPatchingStatus(ZipArchive apk)
        {
            PatchingStatus status = new PatchingStatus();
            MemoryStream manifestStream = new MemoryStream();
            apk.GetEntry(ManifestPath).Open().CopyTo(manifestStream);
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
            status.isPatched = PatchingManager.IsAPKModded(apk);
            status.moddedJson = PatchingManager.GetModdedJson(apk);
            manifestStream.Close();
            manifestStream.Dispose();
            return status;
        }

        public static void AddNameAttribute(AxmlElement element, string name)
        {
            element.Attributes.Add(new AxmlAttribute("name", AndroidNamespaceUri, NameAttributeResourceId, name));
        }

        public static bool PatchManifest(ZipArchive apkArchive)
        {
            QAVSWebserver.patchStatus.currentOperation = "Patching manifest";
            QAVSWebserver.BroadcastPatchingStatus();
            ZipArchiveEntry? manifestEntry = apkArchive.GetEntry(ManifestPath);
            if (manifestEntry == null)
            {
                QAVSWebserver.patchStatus.error = true;
                QAVSWebserver.patchStatus.errorText = "Android Manifest doesn't exist. Cannot mod game";
                QAVSWebserver.BroadcastPatchingStatus();
                return false;
            }

            // The AMXL loader requires a seekable stream
            MemoryStream ms = new MemoryStream();
            using (Stream stream = manifestEntry.Open())
            {
                stream.CopyTo(ms);
                stream.Close();
                stream.Dispose();
            }

            ms.Position = 0;
            AxmlElement manifest = AxmlLoader.LoadDocument(ms);

            // First we add permissions and features to the APK for modding
            List<string> addingPermissions = new List<string>();
            List<UsesFeature> addingFeatures = new List<UsesFeature>();
            PatchingPermissions permissions = CoreService.coreVars.patchingPermissions;
            if (permissions.externalStorage)
            {
                // Technically, we only need READ_EXTERNAL_STORAGE and WRITE_EXTERNAL_STORAGE, but we also add MANAGE_EXTERNAL_STORAGE as this is what Android 11 needs instead
                addingPermissions.AddRange(new[]
                {
                    "android.permission.READ_EXTERNAL_STORAGE",
                    "android.permission.WRITE_EXTERNAL_STORAGE",
                    "android.permission.MANAGE_EXTERNAL_STORAGE",
                    TagPermission
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

            for (int i = 0; i < manifest.Attributes.Count; i++)
            {
                if (manifest.Attributes[i].Name == "package")
                {
                    if (permissions.customPackageId != "")
                    {
                        manifest.Attributes[i].Value = permissions.customPackageId;
                    }

                    packageId = (string)manifest.Attributes[i].Value;
                }
            }

            QAVSWebserver.patchStatus.package = packageId;


            // Save the manifest using our AXML library
            Logger.Log("Saving manifest as AXML . . .");
            ms.Close();
            ms.Dispose();
            manifestEntry.Delete(); // Remove old manifest

            manifestEntry = apkArchive.CreateEntry(ManifestPath);
            using (Stream saveStream = manifestEntry.Open())
            {
                AxmlSaver.SaveDocument(saveStream, manifest);
                saveStream.Close();
                saveStream.Dispose();
            }
            return true;
        }

    }
}