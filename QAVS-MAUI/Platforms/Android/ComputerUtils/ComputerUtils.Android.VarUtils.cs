using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ComputerUtils.RandomExtensions;

namespace ComputerUtils.VarUtils
{
    public class Combinator
    {
        public String[] Combinate(String[] arr, int maxLength)
        {
            if (maxLength == 0) return new string[1] { "" };
            List<String> output = new List<String>();
            if (arr.Length == 1)
            {
                return arr;
            }
            else if (arr.Length == 2)
            {
                output.Add(arr[0] + arr[1]);
                output.Add(arr[1] + arr[0]);
            }
            else
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    List<String> tmp = new List<string>(arr);
                    tmp.RemoveAt(i);
                    String[] combined = Combinate(tmp, maxLength - 1);
                    for (int c = 0; c < combined.Length; c++)
                    {
                        output.Add(arr[i] + combined[c]);
                    }
                }
            }
            return output.ToArray();
        }

        public String[] Combinate(List<string> arr, int maxLength)
        {
            return Combinate(arr.ToArray(), maxLength);
        }

        public int[] Combinate(List<int> arr, int maxLength)
        {
            return Array.ConvertAll(Combinate(Array.ConvertAll(arr.ToArray(), s => s.ToString()), maxLength), i => int.Parse(i));
        }
    }

    public class StringUtils
    {
        public static int GetStringLines(string i)
        {
            return i.Split('\n').Length;
        }
        public static int GetStringMaxLineLength(string i)
        {
            int max = 0;
            foreach (string s in i.Split('\n'))
            {
                if (s.Length > max) max = s.Length;
            }
            return max;
        }
    }

    public class SizeConverter
    {
        public static string ByteSizeToString(long input, int decimals = 2)
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

        public static string SecondsToBetterString(long seconds)
        {
            if (seconds < 60) return seconds + " S";
            else if (seconds >= 60) return Math.Floor(seconds / 60.0) + " M  " + (seconds % 60) + " S";
            else if (seconds > 3600) return Math.Floor(seconds / 3600.0) + " H  " + Math.Floor(seconds % 3600 / 60.0) + " M  " + (seconds % 60) + " S";
            else if (seconds > 86400) return Math.Floor(seconds / 86400.0) + " D  " + Math.Floor(seconds % 86400 / 3600.0) + " H  " + Math.Floor(seconds % 311040000 / 60.0) + " M  " + (seconds % 60) + " S";
            return seconds.ToString() + " S";
        }
    }

    public class TimeConverter
    {
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp);
            return dateTime;
        }

        public static DateTime JavaTimeStampToDateTime(long unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(unixTimeStamp);
            return dateTime;
        }

        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        }

        public static long DateTimeToJavaTimestamp(DateTime dateTime)
        {
            return ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
        }
    }
}