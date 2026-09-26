using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
		string dataRoot = AnimusForgeDataPaths.GetCurrentRoot();
		string exports = AnimusForgeDataPaths.GetPlayerExportsDirectory(dataRoot);
		if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AnimusForgeDataPaths.OverrideEnvironmentVariable)))
			EnsureLegacyMigrationReady(GetModuleRootPath(), dataRoot);
		return exports;
	}

	internal static void EnsureLegacyMigrationReady(string moduleRoot, string dataRoot)
	{
		string legacy = Path.Combine(moduleRoot, FolderName);
		if (!Directory.Exists(legacy)) return;
		for (DirectoryInfo directory = new DirectoryInfo(legacy); directory != null; directory = directory.Parent)
			if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
				throw new InvalidOperationException("Legacy PlayerExports crosses a reparse point; migration requires recovery.");
		if (!Directory.EnumerateFileSystemEntries(legacy).Any()) return;

		string marker = Path.Combine(AnimusForgeDataPaths.GetUserDataDirectory(dataRoot), ".player-exports-ready.json");
		VerifyMigrationMarker(moduleRoot, marker);
	}

	internal static void VerifyMigrationMarker(string moduleRoot, string marker)
	{
		PlayerExportsPackageExport.AssertNoReparse(marker);
		if (!File.Exists(marker))
			throw new InvalidOperationException("Legacy PlayerExports awaits verified migration; the new user-data root is not ready.");
		try
		{
			if ((File.GetAttributes(marker) & FileAttributes.ReparsePoint) != 0)
				throw new InvalidOperationException("Legacy PlayerExports migration record is a reparse point.");
			string key = ComputeModuleRootKey(moduleRoot);
			JObject record = JObject.Parse(File.ReadAllText(marker, Encoding.UTF8));
			string manifestHash = record["sources"]?[key]?.Value<string>();
			if (record.Value<int>("schema") != 1 || string.IsNullOrWhiteSpace(manifestHash)
				|| manifestHash.Length != 64 || !manifestHash.All(Uri.IsHexDigit))
				throw new InvalidOperationException("Legacy PlayerExports migration record does not match this module.");
			string dataRoot = Directory.GetParent(Directory.GetParent(marker).FullName).FullName;
			string completed = Path.Combine(dataRoot, "Recovery", "player-exports-" + manifestHash.Substring(0, 24), "completed.json");
			PlayerExportsPackageExport.AssertNoReparse(completed);
			if (!File.Exists(completed) || (File.GetAttributes(completed) & FileAttributes.ReparsePoint) != 0)
				throw new InvalidOperationException("Legacy PlayerExports migration completion record is missing.");
			JObject completion = JObject.Parse(File.ReadAllText(completed, Encoding.UTF8));
			if (completion.Value<int>("schema") != 1 || completion.Value<string>("manifestSha256") != manifestHash)
				throw new InvalidOperationException("Legacy PlayerExports migration completion record is inconsistent.");
		}
		catch (Exception ex) when (!(ex is InvalidOperationException))
		{
			throw new InvalidOperationException("Legacy PlayerExports migration record is invalid; no old-path fallback is allowed.", ex);
		}
	}

	internal static string ComputeModuleRootKey(string moduleRoot)
	{
		string normalized = Path.GetFullPath(moduleRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();
		using (var sha = SHA256.Create())
			return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(normalized))).Replace("-", "").ToLowerInvariant();
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
