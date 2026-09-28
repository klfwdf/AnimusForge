using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

/// <summary>
/// A dev-menu export is built beside the active package, then published only after
/// validation and a verified private copy of the old package. No campaign Tick use.
/// </summary>
internal sealed class PlayerExportsPackageExport
{
	private readonly string _root;
	private readonly string _name;
	private readonly string _recoveryRoot;
	private readonly string _operationId;
	private readonly bool _hadOriginal;
	private readonly Dictionary<string, string> _originalFiles;
	private bool _published;

	internal string CandidatePath { get; }
	internal string FinalPath => Path.Combine(_root, _name);

	private PlayerExportsPackageExport(string root, string name, string recoveryRoot, string operationId,
		bool hadOriginal, Dictionary<string, string> originalFiles, string candidatePath)
	{
		_root = root;
		_name = name;
		_recoveryRoot = recoveryRoot;
		_operationId = operationId;
		_hadOriginal = hadOriginal;
		_originalFiles = originalFiles;
		CandidatePath = candidatePath;
	}

	internal static PlayerExportsPackageExport Begin(string root, string name, string recoveryRoot)
	{
		if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(recoveryRoot)
			|| !Path.IsPathRooted(root) || !Path.IsPathRooted(recoveryRoot))
			throw new ArgumentException("Export and recovery roots must be absolute.");
		if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." || name.StartsWith(".", StringComparison.Ordinal)
			|| name.EndsWith(".", StringComparison.Ordinal)
			|| name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
			|| name.IndexOf(Path.DirectorySeparatorChar) >= 0 || name.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
			throw new ArgumentException("Invalid export package name.", nameof(name));
		root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		recoveryRoot = Path.GetFullPath(recoveryRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		if (recoveryRoot.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
			|| root.StartsWith(recoveryRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(root, recoveryRoot, StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("Recovery must be separate from PlayerExports.");
		AssertNoReparse(root);
		AssertNoReparse(recoveryRoot);
		Directory.CreateDirectory(root);
		string original = Path.Combine(root, name);
		AssertNoReparse(original);
		if (File.Exists(original)) throw new IOException("Export package path is a file.");
		bool hadOriginal = Directory.Exists(original);
		Dictionary<string, string> originalFiles = hadOriginal ? Snapshot(original) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		string id = Guid.NewGuid().ToString("N");
		string candidate = Path.Combine(root, ".af-export-candidate." + id);
		Directory.CreateDirectory(candidate);
		if (hadOriginal)
		{
			CopyTree(original, candidate);
			if (!EqualSnapshots(originalFiles, Snapshot(candidate)) || !EqualSnapshots(originalFiles, Snapshot(original)))
				throw new IOException("Existing export changed while preparing a candidate; old package retained.");
		}
		return new PlayerExportsPackageExport(root, name, recoveryRoot, id, hadOriginal, originalFiles, candidate);
	}

	internal void Publish()
	{
		if (_published) throw new InvalidOperationException("Export candidate was already published.");
		AssertNoReparse(CandidatePath);
		if (!Directory.Exists(CandidatePath)) throw new IOException("Export candidate is missing.");
		Dictionary<string, string> candidateFiles = Snapshot(CandidatePath);
		foreach (KeyValuePair<string, string> file in candidateFiles)
		{
			if (!file.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
			JToken.Parse(File.ReadAllText(Path.Combine(CandidatePath, file.Key), Encoding.UTF8));
		}
		AssertOriginalUnchanged();
		if (_hadOriginal)
		{
			string backup = Path.Combine(_recoveryRoot, "PlayerExports", _operationId, _name);
			AssertNoReparse(backup);
			Directory.CreateDirectory(backup);
			CopyTree(FinalPath, backup);
			if (!EqualSnapshots(_originalFiles, Snapshot(backup)))
				throw new IOException("Export backup verification failed; old package retained.");
		}
		AssertOriginalUnchanged();
		string retired = Path.Combine(_root, ".af-export-retired." + _operationId);
		if (_hadOriginal)
		{
			AssertNoReparse(retired);
			Directory.Move(FinalPath, retired);
		}
		try
		{
			if (_hadOriginal && !EqualSnapshots(_originalFiles, Snapshot(retired)))
				throw new InvalidOperationException("Existing export changed during publication; no new package activated.");
			Directory.Move(CandidatePath, FinalPath);
			_published = true;
		}
		catch
		{
			if (_hadOriginal && !Directory.Exists(FinalPath) && Directory.Exists(retired))
				Directory.Move(retired, FinalPath);
			throw;
		}
		if (_hadOriginal)
		{
			try { DeleteVerifiedRetired(retired, _originalFiles); }
			catch (Exception ex)
			{
				throw new IOException("New export was published, but a retained old copy requires recovery.", ex);
			}
		}
	}

	internal void RestoreSubdirectory(string name)
	{
		if (_published) throw new InvalidOperationException("Published export cannot be edited.");
		if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".."
			|| name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
			|| name.IndexOf(Path.DirectorySeparatorChar) >= 0 || name.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
			throw new ArgumentException("Invalid export subdirectory.", nameof(name));
		AssertOriginalUnchanged();
		string candidate = Path.Combine(CandidatePath, name);
		if (Directory.Exists(candidate)) DeleteVerifiedRetired(candidate, Snapshot(candidate));
		string original = Path.Combine(FinalPath, name);
		if (Directory.Exists(original))
		{
			Directory.CreateDirectory(candidate);
			CopyTree(original, candidate);
			if (!EqualSnapshots(Snapshot(original), Snapshot(candidate)))
				throw new IOException("Failed to restore an unchanged export section.");
		}
	}

	private void AssertOriginalUnchanged()
	{
		AssertNoReparse(FinalPath);
		if (_hadOriginal != Directory.Exists(FinalPath)
			|| (_hadOriginal && !EqualSnapshots(_originalFiles, Snapshot(FinalPath))))
			throw new InvalidOperationException("Existing export changed during candidate preparation; no publication performed.");
	}

	private static Dictionary<string, string> Snapshot(string directory)
	{
		AssertNoReparse(directory);
		var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		EnumerateTree(directory, out List<string> directories, out List<string> filePaths);
		foreach (string path in filePaths)
		{
			string relative = path.Substring(directory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (files.ContainsKey(relative)) throw new IOException("Case-insensitive export path collision.");
			files.Add(relative, Hash(path));
		}
		return files;
	}

	private static bool EqualSnapshots(Dictionary<string, string> first, Dictionary<string, string> second)
	{
		return first.Count == second.Count && first.All(file => second.TryGetValue(file.Key, out string hash) && hash == file.Value);
	}

	private static void CopyTree(string source, string target)
	{
		AssertNoReparse(source);
		AssertNoReparse(target);
		EnumerateTree(source, out List<string> directories, out List<string> files);
		foreach (string dir in directories.OrderBy(path => path.Length))
		{
			Directory.CreateDirectory(Path.Combine(target, dir.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
		}
		foreach (string file in files)
		{
			string destination = Path.Combine(target, file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			Directory.CreateDirectory(Path.GetDirectoryName(destination));
			File.Copy(file, destination, overwrite: false);
		}
	}

	private static void DeleteVerifiedRetired(string retired, Dictionary<string, string> expected)
	{
		if (!EqualSnapshots(expected, Snapshot(retired))) throw new IOException("Retired export changed; retained copy not removed.");
		EnumerateTree(retired, out List<string> directories, out List<string> files);
		foreach (string file in files)
		{
			File.Delete(file);
		}
		foreach (string dir in directories.OrderByDescending(path => path.Length))
			Directory.Delete(dir);
		Directory.Delete(retired);
	}

	private static void EnumerateTree(string root, out List<string> directories, out List<string> files)
	{
		directories = new List<string>();
		files = new List<string>();
		var pending = new Stack<string>();
		pending.Push(root);
		while (pending.Count > 0)
		{
			string current = pending.Pop();
			AssertNoReparse(current);
			foreach (string entry in Directory.GetFileSystemEntries(current, "*", SearchOption.TopDirectoryOnly))
			{
				AssertNoReparse(entry);
				if (Directory.Exists(entry))
				{
					directories.Add(entry);
					pending.Push(entry);
				}
				else if (File.Exists(entry)) files.Add(entry);
				else throw new IOException("Export contains an unsupported filesystem entry.");
			}
		}
	}

	private static string Hash(string path)
	{
		using (var sha = SHA256.Create())
		using (var stream = File.OpenRead(path))
			return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
	}

	internal static void AssertNoReparse(string path)
	{
		for (DirectoryInfo dir = new DirectoryInfo(Path.GetFullPath(path)); dir != null; dir = dir.Parent)
		{
			if (dir.Exists && (dir.Attributes & FileAttributes.ReparsePoint) != 0)
				throw new IOException("Export path crosses a reparse point.");
		}
		if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
			throw new IOException("Export file is a reparse point.");
	}
}
