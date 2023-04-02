using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.OS.Storage;
using Android.Provider;
using Android.Views.TextClassifiers;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.DocumentFile.Provider;
using ComputerUtils.Android;
using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using Google.Android.Material.Dialog;
using Java.IO;
using Java.Lang;
using QuestAppVersionSwitcher.Core;
using QuestAppVersionSwitcher.Mods;
using Xamarin.Forms.Internals;
using File = System.IO.File;

namespace QuestAppVersionSwitcher
{
    public class FolderPermission
    {
        public static ActivityResultLauncher l = null;
        public static void openDirectory(string dirInExtenalStorage)
        {
            if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2)
            {
                FileManager.CreateDirectoryIfNotExisting(dirInExtenalStorage);
            }
            Intent intent = new Intent(Intent.ActionOpenDocumentTree)
                .PutExtra(
                    DocumentsContract.ExtraInitialUri,
                    Uri.Parse(RemapPathForApi300OrAbove(dirInExtenalStorage)));
            l.Launch(intent);
        }

        public static bool GotAccessTo(string dirInExtenalStorage)
        {
            if(!Directory.Exists(dirInExtenalStorage)) return false;
            string uri = RemapPathForApi300OrAbove(dirInExtenalStorage).Replace("com.android.externalstorage.documents/document/", "com.android.externalstorage.documents/tree/");
            List<UriPermission> perms = AndroidCore.context.ContentResolver.PersistedUriPermissions.ToList();
            foreach (UriPermission p in perms)
            {
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
            try
            {
                Logger.Log("Copying " + from + " to " + to);
                Stream file = GetOutputStream(to);
                
                if (file.CanWrite)
                {
                    var readStream = File.OpenRead(from);
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
        
        public static void Copy(DocumentFile source, string to)
        {
            try
            {
                Logger.Log("Copying " + source.Name + " to " + to);
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
                Logger.Log("Copying " + source + " to " + to);
                Stream file = AndroidCore.context.ContentResolver.OpenInputStream(to.Uri);
                
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
            string start = "/sdcard/Android/data/" + CoreService.coreVars.currentApp;
            if(dir.Contains("/Android/obb/")) start = "/sdcard/Android/obb/" + CoreService.coreVars.currentApp;
            if(dir.Contains(Environment.ExternalStorageDirectory.AbsolutePath))
            {
                dir = dir.Replace(Environment.ExternalStorageDirectory.AbsolutePath, "/sdcard");
            }
            string diff = dir.Replace(start, "");
            if (diff.StartsWith("/")) diff = diff.Substring(1);
            string[] dirs = diff.Split('/');
            DocumentFile startDir = DocumentFile.FromTreeUri(AndroidCore.context, Uri.Parse(RemapPathForApi300OrAbove(start).Replace("com.android.externalstorage.documents/document/", "com.android.externalstorage.documents/tree/")));
            DocumentFile currentDir = startDir;

            // Not sure if needed, probably remove
            if (dirs == null)
            {
                return currentDir;
            }
            foreach (string dirName in dirs)
            {
                if(dirName == "") continue;
                if (currentDir.FindFile(dirName) == null) currentDir.CreateDirectory(dirName); // Create directory if it doesn't exist
                currentDir = currentDir.FindFile(dirName);
            }
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
            DocumentFile directory = GetAccessToFile(Directory.GetParent(path).FullName);
            string name = Path.GetFileName(path);
            if (directory.FindFile(name) != null) directory.FindFile(name).Delete();
        }

        public static void CreateDirectoryIfNotExisting(string path)
        {
            // Remove trailing slash because it causes problems
            if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
            DocumentFile directory = GetAccessToFile(Directory.GetParent(path).FullName);
            string name = Path.GetFileName(path);
            // If name is empty no need to create directory
            if (name == "") return;
            if (directory.FindFile(name) == null) directory.CreateDirectory(name);
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName)
        {
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
                InternalDirectoryCopy(sourceDirName, GetAccessToFile(destDirName));
            }
            else
            {
                FileManager.DirectoryCopy(sourceDirName, destDirName, true);
            }
        }

        public static void InternalDirectoryCopy(string source, DocumentFile destDir)
        {
            Logger.Log("Starting directory copy: string, DocumentFile");
            if (destDir == null) return;
            Logger.Log(source + " -> " + destDir.Uri);
            
            // Delete all files and directories in destination directory
            foreach (DocumentFile f in destDir.ListFiles())
            {
                f.Delete();
            }
            
            Logger.Log("Cleared dest");

            DirectoryInfo dir = new DirectoryInfo(source);
            Logger.Log("Got dir info");

            // Get the files in the directory and copy them to the new location.
            foreach (FileInfo file in dir.GetFiles())
            {
                try
                {
                    Logger.Log("Copying " + file.Name);
                    Copy(file.FullName, destDir.CreateFile("application/octet-stream", file.Name));
                }
                catch (Exception e) { Logger.Log("Error copying " + file.Name + ": " + e.ToString(), LoggingType.Error); }
            }
            
            Logger.Log("Copied all files");
            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
                Logger.Log("Continuing with subdir " + subdir.Name);
                InternalDirectoryCopy(subdir.FullName, destDir.CreateDirectory(subdir.Name));
            }
        }

        public static void InternalDirectoryCopy(DocumentFile dir, string destDirName)
        {
            Logger.Log("Starting directory copy: DocumentFile, string");
            if(Directory.Exists(destDirName)) Directory.Delete(destDirName, true);
            Directory.CreateDirectory(destDirName);
            
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
                    }
                }
                catch (Exception e) { Logger.Log("Error copying " + file.Name + ": " + e, LoggingType.Error); }
            }
        }
    }
    
    public class FolderPermissionCallback : Java.Lang.Object, IActivityResultCallback
    {
        public void OnActivityResult(Result resultCode, Intent data)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                if (data.Data != null)
                {
                    AndroidCore.context.ContentResolver.TakePersistableUriPermission(
                        data.Data,
                        ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                    QAVSModManager.Update();
                }
            }
        }
        public void OnActivityResult(Object result)
        {
            if (result is ActivityResult activityResult && Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                if (activityResult.Data.Data != null)
                {
                    AndroidCore.context.ContentResolver.TakePersistableUriPermission(
                        activityResult.Data.Data,
                        ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                    QAVSModManager.Update();
                }
            }
        }
    }
}