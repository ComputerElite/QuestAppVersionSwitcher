using ComputerUtils.RegexStuff;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;

namespace ComputerUtils.Logging
{
    public class Logger
    {
        public static string logFile { get; set; } = "";
		public static string log { get; set; } = "";
		public static bool removeUsernamesFromLog { get; set; } = true;
        public static bool displayLogInConsole { get; set; } = false;
        public static bool longLogInConsole { get; set; } = true;
        public static Dictionary<string, string> notAllowedStrings { get; set; } = new Dictionary<string, string>();
        public static ReaderWriterLock locker = new ReaderWriterLock();

        public static string CensorString(string input)
        {
            foreach (KeyValuePair<string, string> s in notAllowedStrings) input = input.Replace(s.Key, s.Value);
            return input;
        }

        public static void Log(string text, LoggingType loggingType = LoggingType.Info)
        {
            //Remove username
            //if (removeUsernamesFromLog) text = RegexTemplates.RemoveUserName(text);
            try
            {
                string linePrefix = GetLinePrefix(loggingType);
                text = linePrefix + text.Replace("\n", "\n" + linePrefix);
                text = CensorString(text);
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
                log += "\n" + text;
                if (log.Length > 50000) log = log.Substring(log.Length - 50000);
                if (logFile == "") return;
                LogRaw(text + "\n");
            } catch (Exception e)
            {
                Console.WriteLine("Exception during logging: " + e.Message);
            }
            
        }
        
        public static void Log(string text, string prefix)
        {
            //Remove username
            if (removeUsernamesFromLog) text = RegexTemplates.RemoveUserName(text);
            string linePrefix = GetLinePrefix(prefix);
            text = linePrefix + text.Replace("\n", "\n" + linePrefix);
            if (displayLogInConsole)
            {
                Console.WriteLine(longLogInConsole ? text : text.Replace(linePrefix, ""));
                Console.ForegroundColor = ConsoleColor.White;
            }
            log += "\n" + text;
            if (log.Length > 50000) log = log.Substring(log.Length - 50000);
            if (logFile == "") return;
            LogRaw(text + "\n");
        }
        public static void LogRaw(string text)
        {
            if (logFile == "") return;
            try
            {
                // Aquire a writer lock to make sure no other thread is writing to the file
                locker.AcquireWriterLock(10000); //You might wanna change timeout value 
                File.AppendAllText(logFile, text);
            }
            finally
            {
                locker.ReleaseWriterLock();
            }
        }

        public static string GetLinePrefix(LoggingType loggingType)
        {
            DateTime t = DateTime.Now;
            return "[" + t.Day.ToString("d2") + "." + t.Month.ToString("d2") + "." + t.Year.ToString("d4") + "   " + t.Hour.ToString("d2") + ":" + t.Minute.ToString("d2") + ":" + t.Second.ToString("d2") + "." + t.Millisecond.ToString("d5") + "] " + (Enum.GetName(typeof(LoggingType), loggingType) + ":").PadRight(10);
        }
        
        public static string GetLinePrefix(string loggingType)
        {
            DateTime t = DateTime.Now;
            return "[" + t.Day.ToString("d2") + "." + t.Month.ToString("d2") + "." + t.Year.ToString("d4") + "   " + t.Hour.ToString("d2") + ":" + t.Minute.ToString("d2") + ":" + t.Second.ToString("d2") + "." + t.Millisecond.ToString("d5") + "] " + (loggingType + ":").PadRight(10);
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