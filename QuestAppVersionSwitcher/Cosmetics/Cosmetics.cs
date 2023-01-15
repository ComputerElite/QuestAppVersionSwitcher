using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Android.Graphics.Path;

namespace QuestAppVersionSwitcher
{
	public class Cosmetics
	{
		public Dictionary<string, CosmeticsGame> games { get; set; } = new Dictionary<string, CosmeticsGame>();

		public CosmeticsGame GetCosmeticsGame(string game)
		{
			if (games.ContainsKey(game)) return games[game];
			return null;
		}
		
		public bool InstallCosmetic(string game, string extension, string path, bool deleteOriginalFile)
		{
			CosmeticsGame CGame = GetCosmeticsGame(game);
			if(CGame == null)
			{
				if (deleteOriginalFile) File.Delete(path);
				return false;
			}
			CGame.InstallCosmetic(extension, path, deleteOriginalFile);
			return true;
		}
		
		public void RemoveCosmetic(string game, string extension, string name)
		{
			CosmeticsGame CGame = GetCosmeticsGame(game);
			if (CGame == null) return;
			CGame.RemoveCosmetic(extension, name);
		}

		public List<string> GetInstalledCosmetics(string game, string extension)
		{
			CosmeticsGame CGame = GetCosmeticsGame(game);
			if (CGame == null) return new List<string>();
			return CGame.GetInstalledCosmetics(extension);
		}

		public static Cosmetics LoadCosmetics()
		{
			Logger.Log("Loading Cosmetics from https://raw.githubusercontent.com/ComputerElite/QuestAppVersionSwitcher/main/Assets/cosmetics.json");
			WebClient c = new WebClient();
			string cosmetics = "{}";
			string jsonLoc = CoreService.coreVars.QAVSDir + "cosmetics.json";
			try
			{
				cosmetics = c.DownloadString("https://raw.githubusercontent.com/ComputerElite/QuestAppVersionSwitcher/main/Assets/cosmetics.json");
				File.WriteAllText(jsonLoc, cosmetics);
				Logger.Log("Caching Cosmetics");
			} catch
			{
				Logger.Log("Request failed, falling back to cache if existing");
				if (File.Exists(jsonLoc)) cosmetics = File.ReadAllText(jsonLoc);
			}
			Cosmetics cos = new Cosmetics();
			Logger.Log("Deserializing");
			try
			{
				cos.games = JsonSerializer.Deserialize<Dictionary<string, CosmeticsGame>>(cosmetics);
				Logger.Log("Deserialized successfully! Got Cosmetics for " + cos.games.Count + " cosmetics");
			} catch(Exception e)
			{
				Logger.Log("Error deserializing:\n" + e.ToString());
			}
			return cos;
		}
	}

	public class CosmeticsGame
	{
		public Dictionary<string, CosmeticType> fileTypes { get; set; } = new Dictionary<string, CosmeticType>();
		
		public CosmeticType GetFileType(string extension)
		{
			if (fileTypes.ContainsKey(extension)) return fileTypes[extension];
			return null;
		}

		public bool InstallCosmetic(string extension, string path, bool deleteOriginalFile = true)
		{
			CosmeticType type = GetFileType(extension);
			if (type == null)
			{
				if (deleteOriginalFile) File.Delete(path);
				return false;
			}
			type.InstallCosmetic(path, deleteOriginalFile);
			return true;
		}

		public List<string> GetInstalledCosmetics(string extension)
		{
			CosmeticType type = GetFileType(extension);
			if (type == null) return new List<string>();
			return type.GetInstalledCosmetics();
		}

		public void RemoveCosmetic(string extension, string name)
		{
			CosmeticType type = GetFileType(extension);
			if (type == null) return;
			type.RemoveCosmetic(name);
		}
	}

	public class CosmeticType
	{
		public string name { get; set; } = "Unknown";
		public string fileType { get; set; } = "Unknown";
		public string directory { get; set; } = "";
		public bool requiresModded { get; set; } = true;

		public void InstallCosmetic(string currentPath, bool deleteOriginalFile = true)
		{
			Logger.Log("Copying Cosmetic " + currentPath + " to " + directory);
			FileManager.CreateDirectoryIfNotExisting(directory);
			File.Copy(currentPath, directory + Path.GetFileName(currentPath), true);
			Logger.Log("Copied");
			if (deleteOriginalFile) File.Delete(currentPath);
		}

		public List<string> GetInstalledCosmetics()
		{
			if (!Directory.Exists(directory)) return new List<string>();
			List<string> cosmetics = new List<string>();
			foreach(string s in Directory.GetFiles(directory))
			{
				cosmetics.Add(Path.GetFileName(s));
			}
			return cosmetics;
		}

		public void RemoveCosmetic(string fileName)
		{
			Logger.Log("Deleting Cosmetic" + fileName + " from " + directory);
			if (File.Exists(directory + fileName)) File.Delete(directory + fileName);
		}
	}
}