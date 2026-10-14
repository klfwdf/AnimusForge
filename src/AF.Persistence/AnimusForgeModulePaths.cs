using System;
using System.Collections.Generic;
using System.IO;

namespace AnimusForge;

public static class AnimusForgeModulePaths
{


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

	/// <summary>Retained API signature; retired module directories are no longer inspected.</summary>
	public static IReadOnlyList<string> GetLegacyModuleRootsForReadOnlyMigration()
		=> Array.Empty<string>();

}
