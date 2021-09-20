using ComputerUtils.RegexStuff;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ComputerUtils.Logging {
    public class Logger
    {
        public static string logFile { get; set; } = "";
        public static bool removeUsernamesFromLog { get; set; } = true;
        public static bool displayLogInConsole { get; set; } = false;
        public static bool longLogInConsole { get; set; } = true;

        public static void Log(string text, LoggingType loggingType = LoggingType.Info)
        {
            //Remove username
            if (removeUsernamesFromLog) text = RegexTemplates.RemoveUserName(text);
            string linePrefix = GetLinePrefix(loggingType);
            text = linePrefix + text.Replace("\n", "\n" + linePrefix);
            if (displayLogInConsole)
            {
                switch (loggingType)
                {
                    case LoggingType.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LoggingType.Info:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LoggingType.Crash:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LoggingType.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LoggingType.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        break;
                }
                Console.WriteLine(longLogInConsole ? text : text.Replace(linePrefix, ""));
                Console.ForegroundColor = ConsoleColor.White;
            }
            if (logFile == "") return;
            File.AppendAllText(logFile, "\n" + text);
        }
        public static void LogRaw(string text)
        {
            if (logFile == "") return;
            File.AppendAllText(logFile, text);
        }

        public static string GetLinePrefix(LoggingType loggingType)
        {
            DateTime t = DateTime.Now;
            return "[" + t.Day.ToString("d2") + "." + t.Month.ToString("d2") + "." + t.Year.ToString("d4") + "   " + t.Hour.ToString("d2") + ":" + t.Minute.ToString("d2") + ":" + t.Second.ToString("d2") + "." + t.Millisecond.ToString("d5") + "] " + (Enum.GetName(typeof(LoggingType), loggingType) + ":").PadRight(10);
        }

        public static void SetLogFile(string file)
        {
            logFile = file;
        }
    }

    public enum LoggingType
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Debug = 3,
        Crash = 4
    }
}