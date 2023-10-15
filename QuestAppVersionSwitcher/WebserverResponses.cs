using System;
using System.Collections.Generic;
using System.Text.Json;
using QuestAppVersionSwitcher.Core;
using QuestPatcher.QMod;

namespace QuestAppVersionSwitcher
{
    public class ChangeAppRequest
    {
        public string packageName { get; set; } = "";
        public string name { get; set; } = "";
    }

    public class MultiCastContent
    {
        public string QAVSVersion { get { return CoreService.version.ToString(); } }
        public List<string> ips { get; set; }
        public int port { get; set; }
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

    public class ModResponse : GenericResponse
    {
        public int taskId { get; set; } = -1;
        
        public static string GetResponse(string msg, bool success, int taskId)
        {
            ModResponse r = new ModResponse();
            r.msg = msg;
            r.success = success;
            r.taskId = taskId;
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
    
    public class ModLoaderResponse : GenericResponse
    {
        public ModLoader modloader { get; set; } = ModLoader.QuestLoader;
        public static string GetResponse(string msg, ModLoader modloader, bool success)
        {
            ModLoaderResponse r = new ModLoaderResponse();
            r.msg = msg;
            r.modloader = modloader;
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
}