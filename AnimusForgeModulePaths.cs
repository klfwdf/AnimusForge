using System;
using System.Collections.Generic;
using System.IO;

namespace AnimusForge;

public static class AnimusForgeModulePaths
{
	private const string CurrentModuleFolderName = "AnimusForge";
	private static readonly string[] LegacyModuleFolderNames = new string[2] { "AnimusForge_1_3_x", "AnimusForge_1_4_5" };

	public static string GetCurrentModuleRoot()
	{
		try
		{
			string assemblyLocation = typeof(AnimusForgeModulePaths).Assembly.Location;
			string assemblyDir = string.IsNullOrWhiteSpace(assemblyLocation) ? "" : Path.GetDirectoryName(assemblyLocation);
			string moduleRoot = ResolveModuleRootFromAssemblyDir(assemblyDir);
			if (!string.IsNullOrWhiteSpace(moduleRoot))
			{
				return moduleRoot;
			}
		}
		catch
		{
		}
		return GetFallbackModuleRoot();
	}

	public static string GetModuleDataFilePath(string fileName)
	{
		string safeFileName = Path.GetFileName((fileName ?? "").Trim());
		if (string.IsNullOrWhiteSpace(safeFileName))
		{
			safeFileName = "AIConfig.json";
		}
		string moduleRoot = GetCurrentModuleRoot();
		if (string.IsNullOrWhiteSpace(moduleRoot))
		{
			return safeFileName;
		}
		return Path.Combine(moduleRoot, "ModuleData", safeFileName);
	}

	public static string GetLogsDirectory()
	{
		string moduleRoot = GetCurrentModuleRoot();
		if (string.IsNullOrWhiteSpace(moduleRoot))
		{
			return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnimusForgeLogs");
		}
		return Path.Combine(moduleRoot, "Logs");
	}

	public static string GetLogFilePath(string fileName)
	{
		string safeFileName = Path.GetFileName((fileName ?? "").Trim());
		if (string.IsNullOrWhiteSpace(safeFileName))
		{
			safeFileName = "AnimusForge.log";
		}
		return Path.Combine(GetLogsDirectory(), safeFileName);
	}

	/// <summary>
	/// Returns existing legacy module roots for one-time, read-only data migration.
	/// These paths must never be used as the active module root or as an output target.
	/// </summary>
	public static IReadOnlyList<string> GetLegacyModuleRootsForReadOnlyMigration()
	{
		List<string> roots = new List<string>();
		try
		{
			string modulesDir = Path.Combine(TaleWorlds.Engine.Utilities.GetBasePath(), "Modules");
			for (int i = 0; i < LegacyModuleFolderNames.Length; i++)
			{
				string candidate = Path.Combine(modulesDir, LegacyModuleFolderNames[i]);
				if (Directory.Exists(candidate))
				{
					roots.Add(candidate);
				}
			}
		}
		catch
		{
		}
		return roots;
	}

	private static string ResolveModuleRootFromAssemblyDir(string assemblyDir)
	{
		try
		{
			string text = assemblyDir;
			for (int i = 0; i < 6 && !string.IsNullOrWhiteSpace(text); i++)
			{
				if (IsCurrentModuleRoot(text))
				{
					return text;
				}
				DirectoryInfo parent = Directory.GetParent(text);
				text = parent?.FullName;
			}
		}
		catch
		{
		}
		return "";
	}

	private static bool IsCurrentModuleRoot(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		string folderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		return string.Equals(folderName, CurrentModuleFolderName, StringComparison.OrdinalIgnoreCase)
			&& File.Exists(Path.Combine(path, "SubModule.xml"))
			&& Directory.Exists(Path.Combine(path, "ModuleData"));
	}

	private static string GetFallbackModuleRoot()
	{
		try
		{
			string basePath = TaleWorlds.Engine.Utilities.GetBasePath();
			string modulesDir = Path.Combine(basePath, "Modules");
			return Path.Combine(modulesDir, CurrentModuleFolderName);
		}
		catch
		{
		}
		return "";
	}
}
