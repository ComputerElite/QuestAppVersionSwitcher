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
using AndroidX.RecyclerView.Widget;
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
                if(!Directory.Exists(dirInExtenalStorage)) CreateDirectory(dirInExtenalStorage);
            }
            CoreService.coreVars.accessFolders.Add(dirInExtenalStorage);
            CoreService.coreVars.Save();
            Intent intent = new Intent(Intent.ActionOpenDocumentTree)
                .PutExtra(
                    DocumentsContract.ExtraInitialUri,
                    Uri.Parse(RemapPathForApi300OrAbove(dirInExtenalStorage)));
            l.Launch(intent);
        }

        public static bool GotAccessTo(string dirInExtenalStorage)
        {
            if(!Directory.Exists(dirInExtenalStorage)) return false;
            
            // Remporary hack while I figure out how to get the permission status
            return CoreService.coreVars.accessFolders.Contains(dirInExtenalStorage);
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
            if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
            {
                File.Copy(from, to, true);
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
            string start = "/sdcard/Android/data";
            if(dir.Contains("/Android/obb/")) start = "/sdcard/Android/obb";
            if(dir.Contains(Environment.ExternalStorageDirectory.AbsolutePath))
            {
                dir = dir.Replace(Environment.ExternalStorageDirectory.AbsolutePath, "/sdcard");
            }

            if (Build.VERSION.SdkInt > BuildVersionCodes.SV2)
            {
                // for A13 get specific app folder
                start += "/" + CoreService.coreVars.currentApp;
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
            if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
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
            if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
            {
                FileManager.CreateDirectoryIfNotExisting(path);
                return;
            }
            // Remove trailing slash because it causes problems
            if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
            try
            {
                GetAccessToFile(path);
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
            if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
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
        
        public static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
            {
                FileManager.DirectoryCopy(sourceDirName, destDirName, true);
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
                InternalDirectoryCopy(sourceDirName, GetAccessToFile(destDirName));
            }
            else
            {
                FileManager.DirectoryCopy(sourceDirName, destDirName, true);
            }
        }

        public static void InternalDirectoryCopy(string source, DocumentFile destDir)
        {
            if (destDir == null) return;
            
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
                }
                catch (Exception e) { Logger.Log("Error copying " + file.Name + ": " + e.ToString(), LoggingType.Error); }
            }
            
            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
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

        /// <summary>
        /// Gets all files of a directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<string> GetFiles(string path)
        {
            if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
            {
                return Directory.GetFiles(path).ToList();
            }

            if (path.EndsWith(Path.DirectorySeparatorChar)) path = path.Substring(0, path.Length - 1);
            DocumentFile directory;
            try
            {
                directory = GetAccessToFile(path);
            }
            catch (System.Exception e)
            {
                Logger.Log("Error while getting access to directory " + e, LoggingType.Error);
                return new List<string>();
            }
            List<string> files = new List<string>();
            foreach (DocumentFile f in directory.ListFiles())
            {
                if (!f.IsDirectory) files.Add(f.Name);
            }
            return files;
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
                    Logger.Log(data.DataString);
                    AndroidCore.context.ContentResolver.TakePersistableUriPermission(
                        data.Data,
                        ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                }
            }
            QAVSModManager.Update();
            Logger.Log("Got result. ComputerElite should consider marking the folder here instead. Let him know!");

            /*
            Logger.Log("Got result. Marking folders as accessible: " + String.Join(", ", FolderPermission.queuedDirs.ToArray()));
            CoreService.coreVars.accessFolders.AddRange(FolderPermission.queuedDirs);
            CoreService.coreVars.Save();
            */
        }
        public void OnActivityResult(Object result)
        {
            if (result is ActivityResult activityResult && Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                if (activityResult.Data.Data != null)
                {
                    Logger.Log(activityResult.Data.DataString);
                    AndroidCore.context.ContentResolver.TakePersistableUriPermission(
                        activityResult.Data.Data,
                        ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                }
            }
            QAVSModManager.Update();
            
            Logger.Log("Got result. ComputerElite should consider marking the folder here instead. Let him know!");
            /*
            CoreService.coreVars.accessFolders.AddRange(FolderPermission.queuedDirs);
            CoreService.coreVars.Save();
            */
        }
    }
}