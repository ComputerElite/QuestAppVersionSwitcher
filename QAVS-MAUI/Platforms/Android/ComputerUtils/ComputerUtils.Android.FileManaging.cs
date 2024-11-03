using ComputerUtils.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using ComputerUtils.VarUtils;

namespace ComputerUtils.FileManaging
{
    public class FileManager
    {
        public static long GetDirSize(string dir)
        {
            return GetDirSize(new DirectoryInfo(dir));
        }

        public static long GetDirSize(DirectoryInfo d)
        {
            long size = 0;
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += GetDirSize(di);
            }
            return size;
        }

        public static List<FileEntry> GetAllFilesRecursively(string dir, string start = "")
        {
            List<FileEntry> files = new List<FileEntry>();
            if(dir.EndsWith(Path.DirectorySeparatorChar)) dir = dir.Substring(0, dir.Length - 1);
            foreach (string d in Directory.GetDirectories(dir))
            {
                files.AddRange(GetAllFilesRecursively(d, start + Path.DirectorySeparatorChar + Path.GetFileName(d)));
            }
            foreach (string f in Directory.GetFiles(dir))
            {
                FileEntry entry = new FileEntry();
                
                entry.name = Path.GetFileName(f);
                entry.path = start + Path.DirectorySeparatorChar + entry.name;
                if(entry.path.StartsWith(Path.DirectorySeparatorChar.ToString())) entry.path = entry.path.Substring(1);
                entry.size = new FileInfo(f).Length;
                files.Add(entry);
            }
            return files;
        }

        public class FileEntry
        {
            public string name { get; set; }
            public string path { get; set; }
            public long size { get; set; } = 0;

            public string sizeString
            {
                get
                {
                    return SizeConverter.ByteSizeToString(size);
                }
            }
        }

        public static string GetParentDirIfExisting(string dir)
        {
            try
            {
                DirectoryInfo i = Directory.GetParent(dir);
                if (i == null) return dir;
                return i.FullName;
            }
            catch
            {
                return dir;
            }
        }
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool output = true)
        {
            // Get the subdirectories for the specified directory.
            try
            {
                if (Directory.Exists(destDirName)) Directory.Delete(destDirName, true);
            }
            catch { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("Couldn't delete " + destDirName); Console.ForegroundColor = ConsoleColor.White; }

            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                try
                {
                    if (output) Console.WriteLine("Copying " + file.Name);
                    Logger.Log("Copying " + file.Name);
                    string tempPath = System.IO.Path.Combine(destDirName, file.Name);
                    file.CopyTo(tempPath, true);
                }
                catch (Exception e) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("ERROR copying " + file.Name); Console.ForegroundColor = ConsoleColor.White; Logger.Log("Error copying " + file.Name + ": " + e.ToString(), LoggingType.Error); }
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = System.IO.Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        public static void CreateDirectoryIfNotExisting(string path)
        {
            if (!Directory.Exists(path))
            {
                Logger.Log("Creating " + path);
                Directory.CreateDirectory(path);
            }
            return;
        }

        public static DirectoryInfo RecreateDirectoryIfExisting(string path)
        {

            if (Directory.Exists(path))
            {
                Logger.Log("Deleting " + path);
                SetAttributesNormal(new DirectoryInfo(path));
                Directory.Delete(path, true);
            }
            Logger.Log("Creating " + path);
            Directory.CreateDirectory(path);
            return new DirectoryInfo(path);
        }

        public static void DeleteDirectoryIfExisting(string path)
        {
            if (Directory.Exists(path))
            {
                Logger.Log("Deleting " + path);
                SetAttributesNormal(new DirectoryInfo(path));
                Directory.Delete(path, true);
            }
        }

        public static void SetAttributesNormal(DirectoryInfo dir)
        {
            foreach (DirectoryInfo subDir in dir.GetDirectories()) SetAttributesNormal(subDir);
            foreach (FileInfo file in dir.GetFiles()) file.Attributes = FileAttributes.Normal;
        }

        public static void DeleteFileIfExisting(string file)
        {
            if (File.Exists(file)) File.Delete(file);
        }

        public static void LogTree(string directory, int depth)
        {
            foreach (string dir in Directory.GetDirectories(directory))
            {
                Logger.Log(GetTreePrefix(depth) + Path.GetFileName(dir));
                LogTree(dir, depth + 1);
            }

            foreach (string file in Directory.GetFiles(directory))
            {
                Logger.Log(GetTreePrefix(depth) + Path.GetFileName(file));
            }
        }
        
        public static string GetTreePrefix(int depth)
        {
            string prefix = "";
            for (int i = 0; i < depth - 1; i++)
            {
                prefix += "|  ";
            }
            if(depth > 0)prefix += "├── ";
            return prefix;
        }

        public static string GetLastCharactersOfFile(string file, int length)
        {
            if (!File.Exists(file)) return "";
            if (length > new FileInfo(file).Length) return File.ReadAllText(file);
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(-length, SeekOrigin.End);
                byte[] bytes = new byte[length];
                fs.Read(bytes, 0, length);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
        }
    }
}