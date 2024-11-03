using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using Java.Util.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace ComputerUtils.Updating
{
	public class UpdateAvailableResponse
	{
		public string msg { get; set; }
		public string changelog { get; set; }
		public bool isUpdateAvailable { get; set; }
	}

	public class Updater
	{
		public string version = "1.0.0";
		public string AppName = "";
		public string GitHubRepoLink = "";

		public Updater(string currentVersion, string GitHubRepoLink, string AppName)
		{
			this.version = currentVersion;
			this.GitHubRepoLink = GitHubRepoLink;
			this.AppName = AppName;
		}

		public Updater() { }

		public UpdateAvailableResponse CheckUpdate()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Checking for updates");
			GithubRelease latest = GetLatestVersion();
			
			if (latest.comparedToCurrentVersion == 1)
			{
				Logger.Log("Update available: " + version + " -> " + latest.tag_name);
				return new UpdateAvailableResponse { changelog = latest.body, msg = "New update availabel! Current version: " + version + ", latest version: " + latest.tag_name, isUpdateAvailable = true};
			}
			else if (latest.comparedToCurrentVersion == -2)
			{
				Logger.Log("Error while checking for updates", LoggingType.Error);
				return new UpdateAvailableResponse { changelog = latest.body, isUpdateAvailable = false, msg = "An Error occured while checking for updates" };
			}
			else if (latest.comparedToCurrentVersion == -1)
			{
				Logger.Log("User on preview version: " + version + " Latest stable: " + latest.tag_name);
				return new UpdateAvailableResponse { changelog = latest.body, isUpdateAvailable = false, msg = "Have fun on a preview version (" + version + "). You can't downgrade to the latest stable release (" + latest.tag_name + ") as downgrades are not supported on android. If you want to downgrade anyway use SideQuest or adb." };
			}
			else
			{
				Logger.Log("User on newest version");
				return new UpdateAvailableResponse { changelog = latest.body, isUpdateAvailable = false, msg = "You're on the newest version (" + version + ")" };
			}
		}

		public GithubRelease GetLatestVersion()
		{
			try
			{
				Logger.Log("Fetching newest version");
				WebClient c = new WebClient();
				c.Headers.Add("user-agent", AppName + "/" + version);
				string repoApi = "https://api.github.com/repos/" + GitHubRepoLink.Split('/')[3] + "/" + GitHubRepoLink.Split('/')[4] + "/releases";
				string json = c.DownloadString(repoApi);

				List<GithubRelease> updates = JsonSerializer.Deserialize<List<GithubRelease>>(json);

				GithubRelease latest = updates[0];
				latest.comparedToCurrentVersion = latest.GetVersion().CompareTo(new System.Version(version));
				return latest;
			}
			catch
			{
				Logger.Log("Fetching of newest version failed", LoggingType.Error);
				return new GithubRelease();
			}
		}

		public void DownloadLatestAPK(string destination)
		{
			Console.WriteLine(AppName + " started in update mode. Fetching newest version");
			GithubRelease e = GetLatestVersion();
			Console.WriteLine("Updating to version " + e.tag_name + ". Starting download (this may take a few seconds)");
			WebClient c = new WebClient();
			Logger.Log("Downloading update");
			c.DownloadFile(e.GetDownload(), destination);
			Logger.Log("Downloaded");
		}
	}

	public class GithubRelease
	{
		public string url { get; set; } = "";
		public string tag_name { get; set; } = "";
		public string body { get; set; } = "";
		public GithubAuthor author { get; set; } = new GithubAuthor();
		public List<GithubAsset> assets { get; set; } = new List<GithubAsset>();
		public int comparedToCurrentVersion = -2; //0 = same, -1 = earlier, 1 = newer, -2 Error

		public string GetDownload()
		{
			foreach (GithubAsset a in assets)
			{
				if (a.content_type == "application/vnd.android.package-archive") return a.browser_download_url;
			}
			return "";
		}

		public Version GetVersion()
		{
			return new Version(tag_name);
		}
	}

	public class GithubAuthor
	{
		public string login { get; set; } = "";
	}

	public class GithubAsset
	{
		public string browser_download_url { get; set; } = "";
		public string content_type { get; set; } = "";
	}

	public class GithubCommit // stripped
	{
		public GithubCommitCommit commit { get; set; } = new GithubCommitCommit();
		public string html_url { get; set; } = "";
	}

	public class GithubCommitCommit // stripped
	{
		public string message { get; set; } = "";
		public GithubCommitCommiter author { get; set; } = new GithubCommitCommiter();
		public GithubCommitCommiter committer { get; set; } = new GithubCommitCommiter();

	}

	public class GithubCommitCommiter // stripped
	{
		public DateTime date { get; set; } = DateTime.MinValue;
		public string name { get; set; } = "";
		public string email { get; set; } = "";
	}
}