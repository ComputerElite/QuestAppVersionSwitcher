using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.OS.Storage;
using Android.Provider;
using Android.Systems;
using Android.Views.TextClassifiers;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.DocumentFile.Provider;
using AndroidX.RecyclerView.Widget;
using ComputerUtils.Android;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using DanTheMan827.OnDeviceADB;
using Google.Android.Material.Dialog;
using Java.IO;
using Java.Lang;
using Java.Nio.FileNio;
using Java.Nio.FileNio.Attributes;
using QuestAppVersionSwitcher.Core;
using QuestAppVersionSwitcher.Mods;
using Xamarin.Forms.Internals;
using File = System.IO.File;
using Process = Java.Lang.Process;

namespace QuestAppVersionSwitcher
{
    public class FolderPermission
    {
        public static ActivityResultLauncher l = null;
        public static void openDirectory(string dirInExtenalStorage)
        {
            if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2)
            {
                if(!Directory.Exists(dirInExtenalStorage)) CreateDirectory(dirInExtenalStorage);
            }
            Logger.Log("Requesting access to " + dirInExtenalStorage);
            CoreService.coreVars.accessFolders.Add(dirInExtenalStorage);
            Intent intent = new Intent(Intent.ActionOpenDocumentTree)
                .PutExtra(
                    DocumentsContract.ExtraInitialUri,
                    Uri.Parse(RemapPathForApi300OrAbove(dirInExtenalStorage)));
            l.Launch(intent);
        }

        public static bool GotAccessTo(string dirInExtenalStorage)
        {
            // ToDo: Check if adb works
            return true;
            Logger.Log("Checking access for " + dirInExtenalStorage + ": " + CoreService.coreVars.accessFolders.Contains(dirInExtenalStorage));
            
            // Temporary hack while I figure out how to get the permission status
            return CoreService.coreVars.accessFolders.Contains(dirInExtenalStorage);
            if (!Directory.Exists(dirInExtenalStorage))
            {
                Logger.Log("QAVS doesn't have access to " + dirInExtenalStorage + "... The folder doesn't exist!");
                return false;
            }
            string uri = RemapPathForApi300OrAbove(dirInExtenalStorage).Replace("com.android.externalstorage.documents/document/", "com.android.externalstorage.documents/tree/");
            List<UriPermission> perms = AndroidCore.context.ContentResolver.PersistedUriPermissions.ToList();
            foreach (UriPermission p in perms)
            {
                Logger.Log("QAVS has permission for " + p.Uri.ToString());
                if (p.Uri.ToString() == uri) return true;
            }
            return false;
        }
        
        public static string RemapPathForApi300OrAbove(string path)
        {
            string suffix = path;
            if (suffix.StartsWith("/sdcard")) suffix = suffix.Substring("/sdcard".Length);
            if (suffix.StartsWith(Environment.ExternalStorageDirectory.AbsolutePath))
            {
                suffix = path.Substring(Environment.ExternalStorageDirectory.AbsolutePath.Length);
            }

            if (suffix.Length < 1) suffix = "/";
            string documentId = "primary:" + suffix.Substring(1);
            return DocumentsContract.BuildDocumentUri(
                "com.android.externalstorage.documents",
                documentId
            ).ToString();
        }

        public static void Copy(string from, string to)
        {
            if (!NeedsSAF(from, to))
            {
                File.Copy(from, to, true);
                return;
            }
            else
            {
                // Use adb
                AdbWrapper.RunAdbCommand("shell cp \"" + from + "\" \"" + to + "\"");
                return;
            }
            try
            {
                Stream file = GetOutputStream(to);
                
                if (file.CanWrite)
                {
                    var readStream = File.OpenRead(from);
                    readStream.CopyTo(file);
                    // Close stuff
                    file.Close();
                    readStream.Close();
                }
                SetFilePermissions(to);
            } catch (Exception e)
            {
                Logger.Log(e.ToString());
            }
        }
        
        public static void Copy(DocumentFile source, string to)
        {
            try
            {
                Stream file = System.IO.File.OpenWrite(to);
                
                if (file.CanWrite)
                {
                    Stream readStream = AndroidCore.context.ContentResolver.OpenInputStream(source.Uri);
                    readStream.CopyTo(file);
                    // Close stuff
                    file.Close();
                    readStream.Close();
                }
                
            } catch (Exception e)
            {
                Logger.Log(e.ToString());
            }
        }
        
        public static void Copy(string source, DocumentFile to)
        {
            try
            {
                Stream file = AndroidCore.context.ContentResolver.OpenOutputStream(to.Uri);

                if (file.CanWrite)
                {
                    Stream readStream = File.OpenRead(source);
                    readStream.CopyTo(file);
                    // Close stuff
                    file.Close();
                    readStream.Close();
                }
                
            } catch (Exception e)
            {
                Logger.Log(e.ToString());
            }
        }

        public static void CreateDirectory(string dir)
        {
            DocumentFile parent = GetAccessToFile(Directory.GetParent(dir).FullName);
            parent.CreateDirectory(Path.GetFileName(dir));
        }

        /// <summary>
        /// ONLY WORKS FOR /sdcard/Android/data/...!!!!!!!
        /// </summary>
        /// <param name="dir">Expected as /sdcard/Android/data/...</param>
        /// <returns></returns>
        public static DocumentFile GetAccessToFile(string dir)
        {
            //Logger.Log("Trying to get access to " + dir);
            string start = "/sdcard/Android/data";
            if(dir.Contains("/Android/obb/")) start = "/sdcard/Android/obb";
            if(dir.Contains(Environment.ExternalStorageDirectory.AbsolutePath))
            {
                dir = dir.Replace(Environment.ExternalStorageDirectory.AbsolutePath, "/sdcard");
            }
            //Logger.Log("Sanitized: " + dir);

            if (Build.VERSION.SdkInt > BuildVersionCodes.SV2)
            {
                // for A13 get specific app folder
                start += "/" + CoreService.coreVars.currentApp;
            }
            string diff = dir.Replace(start, "");
            //Logger.Log("Start: " + start);
            //.Log("Diff: " + diff);
            if (diff.StartsWith("/")) diff = diff.Substring(1);
            string[] dirs = diff.Split('/');
            DocumentFile startDir = DocumentFile.FromTreeUri(AndroidCore.context, Uri.Parse(RemapPathForApi300OrAbove(start).Replace("com.android.externalstorage.documents/document/", "com.android.externalstorage.documents/tree/")));
            DocumentFile currentDir = startDir;
            //Logger.Log("Got access to " + currentDir.Name + ": " + currentDir.CanWrite());

            // Not sure if needed, probably remove
            if (dirs == null)
            {
                return currentDir;
            }
            foreach (string dirName in dirs)
            {
                if(dirName == "") continue;
                //Logger.Log("Got access to " + currentDir.Name + ": " + currentDir.CanWrite());
                if (currentDir.FindFile(dirName) == null) currentDir.CreateDirectory(dirName); // Create directory if it doesn't exist
                //Logger.Log("Got access to " + currentDir.Name + ": " + currentDir.CanWrite());
                currentDir = currentDir.FindFile(dirName);
            }
            //Logger.Log("Final! Got access to " + currentDir.Name + ": " + currentDir.CanWrite());
            return currentDir;
        }

        public static Stream GetOutputStream(string path)
        {
            DocumentFile directory = GetAccessToFile(Directory.GetParent(path).FullName);
            string name = Path.GetFileName(path);
            if (directory.FindFile(name) != null) directory.FindFile(name).Delete();
            return AndroidCore.context.ContentResolver.OpenOutputStream(directory.CreateFile("application/octet-stream", name).Uri);
        }

        public static void Delete(string path)
        {
            if (!NeedsSAF(path))
            {
                File.Delete(path);
                return;
            }
            DocumentFile directory = GetAccessToFile(Directory.GetParent(path).FullName);
            string name = Path.GetFileName(path);
            if (directory.FindFile(name) != null) directory.FindFile(name).Delete();
        }

        public static void CreateDirectoryIfNotExisting(string path)
        {
            if (!NeedsSAF(path) || !path.Contains("sdcard/Android"))
            {
                FileManager.CreateDirectoryIfNotExisting(path);
                SetFilePermissions(path);
                return;
            }
            // Remove trailing slash because it causes problems
            if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
            try
            {
                GetAccessToFile(path);
                SetFilePermissions(path);
            }
            catch (System.Exception e)
            {
                Logger.Log("Error creating directory if it doesn't exist: " + e);
            }
        }

        /// <summary>
        /// Deletes all files in a directory
        /// </summary>
        /// <param name="dir"></param>
        public static void DeleteDirectoryContent(string dir)
        {
            if (!NeedsSAF(dir))
            {
                FileManager.RecreateDirectoryIfExisting(dir);
                return;
            }
            DocumentFile destDir = GetAccessToFile(dir);
            foreach (DocumentFile f in destDir.ListFiles())
            {
                f.Delete();
            }
        }

        public static bool NeedsSAF(string from, string to = "")
        {
            if (Build.VERSION.SdkInt <= BuildVersionCodes.Q) return false;
            return from.Contains("/Android/") || to.Contains("/Android/");
        }
        
        public static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            if (!NeedsSAF(sourceDirName, destDirName))
            {
                FileManager.DirectoryCopy(sourceDirName, destDirName, true);
                return;
            }
            else
            {
                // Do it via adb
                AdbWrapper.RunAdbCommand("shell cp -r \"" + sourceDirName + "\" \"" + destDirName + "\"");
                return;
            }
            // If the destination directory exists, delete it 
            if (Directory.Exists(destDirName)) Delete(destDirName);
            string androidFolder = "Android/data/" + CoreService.coreVars.currentApp;
            string obbFolder = "Android/obb/" + CoreService.coreVars.currentApp;

            if ((sourceDirName.Contains(androidFolder) || sourceDirName.Contains(obbFolder)) && (destDirName.Contains(androidFolder) || destDirName.Contains(obbFolder)))
            {
                // This case should never happen during application use
            } else if (sourceDirName.Contains(androidFolder) || sourceDirName.Contains(obbFolder))
            {
                InternalDirectoryCopy(GetAccessToFile(sourceDirName), destDirName);
            } else if (destDirName.Contains(androidFolder) || destDirName.Contains(obbFolder))
            {
                InternalDirectoryCopy(sourceDirName, GetAccessToFile(destDirName), destDirName);
            }
            else
            {
                FileManager.DirectoryCopy(sourceDirName, destDirName, true);
                SetFolderPermissionRecursive(destDirName);
            }
        }

        public static void SetFolderPermissionRecursive(string sourceDirName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                SetFilePermissions(file.FullName);
            }

            // If copying subdirectories, copy them and their contents to new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                SetFilePermissions(subdir.FullName);
            }
        }

        public static void InternalDirectoryCopy(string source, DocumentFile destDir, string destDirString)
        {
            if (destDir == null) return;
            if (!destDirString.EndsWith(Path.DirectorySeparatorChar)) destDirString += Path.DirectorySeparatorChar;
            
            // Delete all files and directories in destination directory
            foreach (DocumentFile f in destDir.ListFiles())
            {
                f.Delete();
            }
            

            DirectoryInfo dir = new DirectoryInfo(source);

            // Get the files in the directory and copy them to the new location.
            foreach (FileInfo file in dir.GetFiles())
            {
                try
                {
                    Copy(file.FullName, destDir.CreateFile("application/octet-stream", file.Name));
                    SetFilePermissions(destDirString + file.Name);
                }
                catch (Exception e) { Logger.Log("Error copying " + file.Name + ": " + e.ToString(), LoggingType.Error); }
            }
            
            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
                InternalDirectoryCopy(subdir.FullName, destDir.CreateDirectory(subdir.Name), destDirString+ subdir.Name + Path.DirectorySeparatorChar);
                SetFilePermissions(destDirString+ subdir.Name + Path.DirectorySeparatorChar);
            }
        }

        public static void Execute(string cmd)
        {
            Process process = null;

            try {
                Logger.Log("Running " + cmd);
                process = Runtime.GetRuntime().Exec(cmd);
                if (process.WaitFor() == 0)
                {
                    Logger.Log("Success");
                    Logger.Log(new StreamReader(process.InputStream).ReadToEnd(), LoggingType.Debug);
                } else
                {
                    Logger.Log("Error");
                    Logger.Log(new StreamReader(process.ErrorStream).ReadToEnd(), LoggingType.Debug);
                }
            } catch (Exception e) {
                return;
            } finally {
                try {
                    process.Destroy();
                } catch (Exception e) {
                }
            }
        }

        public static void SetFilePermissions(string path)
        {
            return;
            Execute("whoami");
            Execute("chmod 777 " + path);
            /*
            Java.IO.File f = new Java.IO.File(path);
            List<PosixFilePermission> perms = new List<PosixFilePermission>();
            perms.Add(PosixFilePermission.OwnerRead);
            perms.Add(PosixFilePermission.OwnerWrite);
            perms.Add(PosixFilePermission.OwnerExecute);
            perms.Add(PosixFilePermission.GroupExecute);
            perms.Add(PosixFilePermission.GroupRead);
            perms.Add(PosixFilePermission.GroupWrite);
            perms.Add(PosixFilePermission.OthersRead);
            perms.Add(PosixFilePermission.OthersExecute);
            perms.Add(PosixFilePermission.OthersWrite);
            Logger.Log("Applying permissions to " + path);
            // Results in access denied error
            Files.SetPosixFilePermissions(f.ToPath(), perms);
            */
        }

        public static void InternalDirectoryCopy(DocumentFile dir, string destDirName)
        {
            Logger.Log("Starting directory copy: DocumentFile, string");
            if(Directory.Exists(destDirName)) Directory.Delete(destDirName, true);
            Directory.CreateDirectory(destDirName);
            SetFilePermissions(destDirName);
            
            foreach (DocumentFile file in dir.ListFiles())
            {
                try
                {
                    if (file.IsDirectory)
                    {
                        // Handle directory
                        string tempPath = System.IO.Path.Combine(destDirName, file.Name);
                        InternalDirectoryCopy(file, tempPath);
                    }
                    else
                    {
                        // Handle file
                        string tempPath = Path.Combine(destDirName, file.Name);
                        Copy(file, tempPath);
                        SetFilePermissions(tempPath);
                    }
                }
                catch (Exception e) { Logger.Log("Error copying " + file.Name + ": " + e, LoggingType.Error); }
            }
        }

        /// <summary>
        /// Gets all files of a directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<string> GetFiles(string path)
        {
            Logger.Log("Getting files in " + path);
            if (!NeedsSAF(path) || !path.Contains("sdcard/Android"))
            {
                return Directory.GetFiles(path).ToList();
            }
            // Use adb
            List<string> files = new List<string>();
            string[] lines = AdbWrapper.RunAdbCommand("shell ls -1 \"" + path + "\"").Output.Split('\n');
            foreach (string line in lines)
            {
                if (line == "") continue;
                files.Add(line);
            }

            return files;
        }
    }
    
    public class FolderPermissionCallback : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Object result)
        {
            //Logger.Log("Got result 2. ComputerElite should consider marking the folder here instead. Let him know!");
            if (result is ActivityResult activityResult && Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                //Logger.Log("Got result. ComputerElite should consider marking the folder here instead. Let him know!");
                if (activityResult.Data.Data != null)
                {
                    AndroidCore.context.ContentResolver.TakePersistableUriPermission(
                        activityResult.Data.Data,
                        ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                    //Logger.Log(activityResult.Data.DataString);
                }
            }
            QAVSModManager.Update();
            /*
            CoreService.coreVars.accessFolders.AddRange(FolderPermission.queuedDirs);
            CoreService.coreVars.Save();
            */
        }
    }
}
