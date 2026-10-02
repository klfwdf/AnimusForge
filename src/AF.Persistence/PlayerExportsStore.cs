using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
		return AnimusForgeDataPaths.GetPlayerExportsDirectory(AnimusForgeDataPaths.GetCurrentRoot());
	}

	internal sealed class ImportFolder
	{
		internal string Name { get; }
		internal string FullPath { get; }
		internal string SourceLabel { get; }
		internal DateTime LastWriteTime { get; }

		internal ImportFolder(DirectoryInfo directory, string sourceLabel)
		{
			Name = directory.Name;
			FullPath = directory.FullName;
			SourceLabel = sourceLabel;
			LastWriteTime = directory.LastWriteTime;
		}
	}

	internal static IReadOnlyList<ImportFolder> GetImportFolders()
	{
		string userRoot = null;
		try { userRoot = GetPlayerExportsRootPath(); }
		catch { /* An unavailable user root must not hide the installed library. */ }
		return GetImportFolders(GetModuleRootPath(), userRoot);
	}

	// Menu-time only: inspect immediate package directories, never their contents.
	internal static IReadOnlyList<ImportFolder> GetImportFolders(string moduleRoot, string userRoot)
	{
		var folders = new List<ImportFolder>();
		var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddImportFolders(folders, roots, Path.Combine(moduleRoot, FolderName), "模组目录");
		AddImportFolders(folders, roots, userRoot, "玩家导出");
		return folders;
	}

	private static void AddImportFolders(List<ImportFolder> folders, HashSet<string> roots, string root, string sourceLabel)
	{
		if (string.IsNullOrWhiteSpace(root)) return;
		try
		{
			string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (!roots.Add(fullRoot) || !Directory.Exists(fullRoot)) return;
			foreach (DirectoryInfo directory in new DirectoryInfo(fullRoot).GetDirectories()
				.Where(d => !d.Name.StartsWith(".", StringComparison.Ordinal))
				.OrderByDescending(d => d.LastWriteTimeUtc))
				folders.Add(new ImportFolder(directory, sourceLabel));
		}
		catch (IOException) { }
		catch (UnauthorizedAccessException) { }
	}

	internal static PlayerExportsPackageExport BeginExportPackage(string root, string folderName)
	{
		return PlayerExportsPackageExport.Begin(root, folderName,
			AnimusForgeDataPaths.GetRecoveryDirectory(AnimusForgeDataPaths.GetCurrentRoot()));
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
	/// Absolute selection stays exact; names prefer the installed library, then user exports.
	/// Blank input still means the latest user export, never a built-in worldbook.
	/// </summary>
	internal static string ResolveImportFolderPath(string folderName)
	{
		return ResolveImportFolderPath(folderName, GetModuleRootPath(), null);
	}

	internal static string ResolveImportFolderPath(string folderName, string moduleRoot, string playerExportsRootPath)
	{
		string text = (folderName ?? "").Trim();
		if (!string.IsNullOrEmpty(text) && Path.IsPathRooted(text))
			return Path.GetFullPath(text);
		if (text == "." || text == "..")
			throw new ArgumentException("Invalid import folder name.", nameof(folderName));
		string name = SanitizeFolderName(text);
		if (!string.IsNullOrEmpty(name))
		{
			string installed = Path.Combine(moduleRoot, FolderName, name);
			if (Directory.Exists(installed)) return Path.GetFullPath(installed);
		}
		string userRoot = playerExportsRootPath ?? GetPlayerExportsRootPath();
		return string.IsNullOrEmpty(name) ? FindLatestExportFolder(userRoot) : Path.Combine(userRoot, name);
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
				where !d.Name.StartsWith(".", StringComparison.Ordinal)
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
		string contents = JsonConvert.SerializeObject(obj, Formatting.Indented);
		JToken.Parse(contents);
		string directory = Path.GetDirectoryName(path);
		PlayerExportsPackageExport.AssertNoReparse(path);
		Directory.CreateDirectory(directory);
		string candidate = Path.Combine(directory, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
		try
		{
			File.WriteAllText(candidate, contents, Encoding.UTF8);
			JToken.Parse(File.ReadAllText(candidate, Encoding.UTF8));
			if (File.Exists(path))
			{
				File.Replace(candidate, path, null);
			}
			else
			{
				File.Move(candidate, path);
			}
		}
		finally
		{
			if (File.Exists(candidate)) File.Delete(candidate);
		}
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

	/// <summary>Only a hidden export candidate may have its old JSON set cleared.</summary>
	internal static void ClearCandidateJsonFiles(string dir)
	{
		if (string.IsNullOrWhiteSpace(dir)) throw new ArgumentException("Candidate directory is required.", nameof(dir));
		string fullPath = Path.GetFullPath(dir);
		if (!fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
			.Any(segment => segment.StartsWith(".af-export-candidate.", StringComparison.Ordinal)))
			throw new InvalidOperationException("Refusing to clear JSON outside an export candidate.");
		PlayerExportsPackageExport.AssertNoReparse(fullPath);
		if (!Directory.Exists(fullPath)) return;
		foreach (string path in Directory.GetFiles(fullPath, "*.json", SearchOption.TopDirectoryOnly))
		{
			PlayerExportsPackageExport.AssertNoReparse(path);
			File.Delete(path);
		}
	}
}
