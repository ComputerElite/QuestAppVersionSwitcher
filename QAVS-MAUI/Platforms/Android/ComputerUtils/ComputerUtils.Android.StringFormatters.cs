namespace ComputerUtils.StringFormatters
{
    public class StringFormatter
    {
        public static string FileNameSafe(string input)
        {
            input = input.Replace("/", "");
            input = input.Replace(":", "");
            input = input.Replace("*", "");
            input = input.Replace("?", "");
            input = input.Replace("\"", "");
            input = input.Replace("<", "");
            input = input.Replace(">", "");
            input = input.Replace("|", "");
            input = input.Replace(@"\", "");
            input.Trim();
            return input;
        }
    }
}