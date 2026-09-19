using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>
/// Owner of the PlayerExports folder layout used by the dev import/export flows in MyBehavior,
/// ModOnboardingBehavior and KingdomStrategicProfileBehavior. It only knows paths and JSON files;
/// menus, inquiries and the per-scope export/import bodies stay with their hosts.
/// Module root = nearest ancestor of this assembly that contains SubModule.xml (same DLL as SubModule).
/// </summary>
internal static class PlayerExportsStore
{
	internal const string FolderName = "PlayerExports";

	internal static string GetModuleRootPath()
	{
		try
		{
			string location = typeof(PlayerExportsStore).Assembly.Location;
			string text = (string.IsNullOrEmpty(location) ? "" : Path.GetDirectoryName(Path.GetFullPath(location)));
			DirectoryInfo directoryInfo = (string.IsNullOrEmpty(text) ? null : new DirectoryInfo(text));
			while (directoryInfo != null && directoryInfo.Exists)
			{
				if (File.Exists(Path.Combine(directoryInfo.FullName, "SubModule.xml")))
				{
					return directoryInfo.FullName;
				}
				directoryInfo = directoryInfo.Parent;
			}
		}
		catch
		{
		}
		try
		{
			return Path.GetFullPath(Directory.GetCurrentDirectory());
		}
		catch
		{
			return "";
		}
	}

	internal static string GetPlayerExportsRootPath()
	{
		return Path.Combine(GetModuleRootPath(), FolderName);
	}

	/// <summary>Invalid file-name chars → '_', trimmed, trailing dots removed; empty input stays empty.</summary>
	internal static string SanitizeFolderName(string input)
	{
		string text = (input ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			text = text.Replace(oldChar, '_');
		}
		return text.Trim().TrimEnd('.');
	}

	/// <summary>Sanitized folder name, or a timestamp when the input is blank.</summary>
	internal static string ResolveExportFolderName(string folderName)
	{
		return ResolveExportFolderName(folderName, DateTime.Now);
	}

	internal static string ResolveExportFolderName(string folderName, DateTime now)
	{
		string text = SanitizeFolderName(folderName);
		if (string.IsNullOrEmpty(text))
		{
			text = now.ToString("yyyyMMdd_HHmmss");
		}
		return text;
	}

	/// <summary>
	/// Existing rooted path wins; otherwise PlayerExports/&lt;sanitized&gt;; blank input → the most recently written export folder (or null).
	/// </summary>
	internal static string ResolveImportFolderPath(string folderName)
	{
		string text = (folderName ?? "").Trim();
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				if (Path.IsPathRooted(text))
				{
					string fullPath = Path.GetFullPath(text);
					if (Directory.Exists(fullPath))
					{
						return fullPath;
					}
				}
			}
			catch
			{
			}
		}
		string playerExportsRootPath = GetPlayerExportsRootPath();
		string text2 = SanitizeFolderName(folderName);
		if (string.IsNullOrEmpty(text2))
		{
			return FindLatestExportFolder(playerExportsRootPath);
		}
		return Path.Combine(playerExportsRootPath, text2);
	}

	internal static string FindLatestExportFolder(string root)
	{
		try
		{
			if (!Directory.Exists(root))
			{
				return null;
			}
			DirectoryInfo directoryInfo = new DirectoryInfo(root);
			return (from d in directoryInfo.GetDirectories()
				orderby d.LastWriteTimeUtc descending
				select d).FirstOrDefault()?.FullName;
		}
		catch
		{
			return null;
		}
	}

	internal static void WriteJson(string path, object obj)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
		string contents = JsonConvert.SerializeObject(obj, Formatting.Indented);
		File.WriteAllText(path, contents, Encoding.UTF8);
	}

	/// <summary>Missing, blank or unreadable file → null (legacy tolerant read).</summary>
	internal static T ReadJson<T>(string path) where T : class
	{
		try
		{
			if (!File.Exists(path))
			{
				return null;
			}
			string value = File.ReadAllText(path, Encoding.UTF8);
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}
			return JsonConvert.DeserializeObject<T>(value);
		}
		catch
		{
			return null;
		}
	}

	/// <summary>Deletes top-level *.json in <paramref name="dir"/>; every failure is swallowed (legacy).</summary>
	internal static void ClearJsonFiles(string dir)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
			{
				return;
			}
			string[] files = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
			foreach (string path in files)
			{
				try
				{
					File.Delete(path);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}
}
