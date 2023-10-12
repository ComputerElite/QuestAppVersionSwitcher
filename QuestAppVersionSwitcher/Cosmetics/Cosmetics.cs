using ComputerUtils.Android.FileManaging;
using ComputerUtils.Android.Logging;
using QuestAppVersionSwitcher.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuestAppVersionSwitcher.Mods;
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
		
		public bool InstallCosmeticByExtension(string game, string extension, string path, bool deleteOriginalFile)
		{
			CosmeticsGame CGame = GetCosmeticsGame(game);
			if(CGame == null)
			{
				if (deleteOriginalFile) File.Delete(path);
				return false;
			}
			CGame.InstallCosmeticByExtension(extension, path, deleteOriginalFile);
			return true;
		}
		
		public bool InstallCosmeticById(string game, string id, string path, bool deleteOriginalFile)
		{
			CosmeticsGame CGame = GetCosmeticsGame(game);
			if(CGame == null)
			{
				if (deleteOriginalFile) File.Delete(path);
				return false;
			}
			CGame.InstallCosmeticById(id, path, deleteOriginalFile);
			return true;
		}
		
		public void RemoveCosmetic(string game, string id, string name)
		{
			CosmeticsGame CGame = GetCosmeticsGame(game);
			if (CGame == null) return;
			CGame.RemoveCosmetic(id, name);
		}

		public List<string> GetInstalledCosmetics(string game, string id)
		{
			CosmeticsGame CGame = GetCosmeticsGame(game);
			if (CGame == null) return new List<string>();
			return CGame.GetInstalledCosmetics(id);
		}

		public static Cosmetics LoadCosmetics()
		{
			Logger.Log("Loading Cosmetics from https://raw.githubusercontent.com/ComputerElite/QuestAppVersionSwitcher/main/Assets/cosmetics-new-new.json");
			string cosmetics = "{}";
			string jsonLoc = CoreService.coreVars.QAVSDir + "cosmetics.json";
			try
			{
				cosmetics = ExternalFilesDownloader.DownloadStringWithTimeout("https://raw.githubusercontent.com/ComputerElite/QuestAppVersionSwitcher/main/Assets/cosmetics-new-new.json", 5000);
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
				Logger.Log("Deserialized successfully! Got Cosmetics for " + cos.games.Count + " games");
			} catch(Exception e)
			{
				Logger.Log("Error deserializing:\n" + e.ToString());
			}
			return cos;
		}

		public void AddCopyType(string packageId, FileCopyType type)
		{
			if(!games.ContainsKey(packageId)) games.Add(packageId, new CosmeticsGame());
			games[packageId].fileTypes.Add(new CosmeticType()
			{
				directory = type.Path,
				fileTypes = type.SupportedExtensions,
				name = type.NamePlural,
				requiresModded = true
			});
		}
		
		public void RemoveCopyType(string packageId, FileCopyType type)
		{
			if (!games.ContainsKey(packageId)) return;
			foreach (string extension in type.SupportedExtensions)
			{
				games[packageId].fileTypes.Remove(games[packageId].fileTypes.FirstOrDefault(x => x.fileType == extension));
			}
		}
	}

	public class CosmeticsGame
	{
		public List<CosmeticType> fileTypes { get; set; } = new List<CosmeticType>();
		
		public CosmeticType GetFileTypeByExtension(string extension)
		{
			return fileTypes.FirstOrDefault(x => x.fileTypes.Contains(extension));
			return null;
		}public CosmeticType GetFileTypeById(string id)
		{
			return fileTypes.FirstOrDefault(x => x.id == id);
		}

		public bool InstallCosmeticByExtension(string extension, string path, bool deleteOriginalFile = true)
		{
			CosmeticType type = GetFileTypeByExtension(extension);
			if (type == null)
			{
				if (deleteOriginalFile) File.Delete(path);
				return false;
			}
			type.InstallCosmetic(path, deleteOriginalFile);
			return true;
		}
		
		public bool InstallCosmeticById(string id, string path, bool deleteOriginalFile = true)
		{
			CosmeticType type = GetFileTypeById(id);
			if (type == null)
			{
				if (deleteOriginalFile) File.Delete(path);
				return false;
			}
			type.InstallCosmetic(path, deleteOriginalFile);
			return true;
		}

		public List<string> GetInstalledCosmetics(string id)
		{
			CosmeticType type = GetFileTypeById(id);
			if (type == null) return new List<string>();
			return type.GetInstalledCosmetics();
		}

		public void RemoveCosmetic(string id, string name)
		{
			CosmeticType type = GetFileTypeById(id);
			if (type == null) return;
			type.RemoveCosmetic(name);
		}
	}

	public class CosmeticType
	{
		public string name { get; set; } = "Unknown";
		public string fileType { get; set; } = "Unknown";
		public List<string> fileTypes { get; set; } = new List<string>();
		public string directory { get; set; } = "";

		public string id
		{
			get
			{
				return name + "-" + String.Join("-", fileTypes);
			}
		}

		public bool requiresModded { get; set; } = true;
		public bool unzip { get; set; } = false;

		public void InstallCosmetic(string currentPath, bool deleteOriginalFile = true)
		{
			if (unzip)
			{
				
				Logger.Log("Extracting zip " + currentPath + " to " + directory);
				FileManager.CreateDirectoryIfNotExisting(directory);
				ZipFile.ExtractToDirectory(currentPath, directory + Path.GetFileNameWithoutExtension(currentPath), true);
				Logger.Log("Extracted");
				FolderPermission.SetFolderPermissionRecursive(directory + Path.GetFileNameWithoutExtension(currentPath));
				if (deleteOriginalFile) File.Delete(currentPath);
			}
			else
			{
				Logger.Log("Copying Cosmetic " + currentPath + " to " + directory);
				FileManager.CreateDirectoryIfNotExisting(directory);
				File.Copy(currentPath, directory + Path.GetFileName(currentPath), true);
				FolderPermission.SetFilePermissions(directory + Path.GetFileName(currentPath));
				Logger.Log("Copied");
				if (deleteOriginalFile) File.Delete(currentPath);
			}
		}

		public List<string> GetInstalledCosmetics()
		{
			if (!Directory.Exists(directory)) return new List<string>();
			List<string> cosmetics = new List<string>();
			if (unzip)
			{
				foreach(string s in Directory.GetDirectories(directory))
				{
					cosmetics.Add(Path.GetFileName(s));
				}
			}
			else
			{
				foreach(string s in Directory.GetFiles(directory))
				{
					cosmetics.Add(Path.GetFileName(s));
				}
			}
			return cosmetics;
		}

		public void RemoveCosmetic(string fileName)
		{
			Logger.Log("Deleting Cosmetic" + fileName + " from " + directory);
			if (unzip)
			{
				if (Directory.Exists(directory + fileName)) Directory.Delete(directory + fileName, true);
			}
			else
			{
				if (File.Exists(directory + fileName)) File.Delete(directory + fileName);
			}
		}
	}
}