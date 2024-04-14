using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using ComputerUtils.ConsoleUi;
using ComputerUtils.Logging;
using ComputerUtils.Updating;

Logger.displayLogInConsole = true;
Updater updater = new Updater("0.1.0", "https://github.com/ComputerElite/QuestAppVersionSwitcher", "QAVS diff creator", Assembly.GetExecutingAssembly().Location);
if (args.Length == 1 && args[0] == "--update")
{
    Logger.Log("Starting in update mode");
    updater.Update();
    return;
}
updater.UpdateAssistant();

if (!File.Exists("xdelta3.exe"))
{
    DownloadProgressUI d = new DownloadProgressUI();
    d.StartDownload("https://github.com/jmacd/xdelta-gpl/releases/download/v3.0.10/xdelta3-x86_64-3.0.10.exe.zip", "xdelta3.exe.zip");
    foreach(ZipArchiveEntry e in ZipFile.OpenRead("xdelta3.exe.zip").Entries) if(e.Name.ToLower().Contains("xdelta")) e.ExtractToFile("xdelta3.exe", true);
}

Console.WriteLine();
Console.WriteLine();
Console.WriteLine("Expected folder structure:");

Console.WriteLine("<supplied directory>");
Console.WriteLine("  - app.apk");
Console.WriteLine("  - obb/");
Console.WriteLine("     - obb1.obb");
Console.WriteLine("     - obb2.obb");
Console.WriteLine("     - ...");

string source = ConsoleUiController.QuestionString("Source version directory: ");
string target = ConsoleUiController.QuestionString("Target version directory: ");
if(!target.EndsWith(Path.DirectorySeparatorChar)) target += Path.DirectorySeparatorChar;
string output = target + "diffs" + Path.DirectorySeparatorChar + DateTime.Now.Ticks + Path.DirectorySeparatorChar;
if (!Directory.Exists(output)) Directory.CreateDirectory(output);
QuestAppVersionSwitcher.DiffDowngrading.DiffCreator.CreateDiff(source, target, output);
Logger.Log("Diff created in " + output);