using System;
using System.Collections.Generic;
using System.IO;

namespace AnimusForge;

public static class AnimusForgeModulePaths
{
	private const string CurrentModuleFolderName = "AnimusForge";
	private static readonly string[] LegacyModuleFolderNames = new string[2] { "AnimusForge_1_3_x", "AnimusForge_1_4_5" };


    internal static string GetCustomPromptsDirectory()
    {
        string root = AnimusForgeDataPaths.ResolveRoot(GetCurrentModuleRoot());
        string directory = Path.Combine(root, "CustomPrompts");
        ValidateCustomPromptPath(directory);
        return directory;
    }

    internal static void ValidateCustomPromptPath(string path)
        => AnimusForgeDataPaths.ValidatePath(path);

	public static string GetCurrentModuleRoot()
	{
		return AnimusForgeDataPaths.GetCurrentRoot();
	}

	public static string GetModuleDataFilePath(string fileName)
	{
		string safeFileName = Path.GetFileName((fileName ?? "").Trim());
		if (string.IsNullOrWhiteSpace(safeFileName))
		{
			safeFileName = "AIConfig.json";
		}
		string moduleRoot = GetCurrentModuleRoot();
		return Path.Combine(moduleRoot, "ModuleData", safeFileName);
	}

	public static string GetLogsDirectory()
	{
		return Path.Combine(GetCurrentModuleRoot(), "logs");
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

}
