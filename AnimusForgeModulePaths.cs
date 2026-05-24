using System;
using System.Collections.Generic;
using System.IO;

namespace AnimusForge;

public static class AnimusForgeModulePaths
{
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

	private static string ResolveModuleRootFromAssemblyDir(string assemblyDir)
	{
		try
		{
			string text = assemblyDir;
			for (int i = 0; i < 6 && !string.IsNullOrWhiteSpace(text); i++)
			{
				if (File.Exists(Path.Combine(text, "SubModule.xml")) && Directory.Exists(Path.Combine(text, "ModuleData")))
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

	private static string GetFallbackModuleRoot()
	{
		List<string> candidates = new List<string>();
		try
		{
			string basePath = TaleWorlds.Engine.Utilities.GetBasePath();
			string modulesDir = Path.Combine(basePath, "Modules");
#if BANNERLORD_1_4_OR_GREATER
			candidates.Add(Path.Combine(modulesDir, "AnimusForge_1_4_5"));
			candidates.Add(Path.Combine(modulesDir, "AnimusForge_1_3_x"));
#else
			candidates.Add(Path.Combine(modulesDir, "AnimusForge_1_3_x"));
			candidates.Add(Path.Combine(modulesDir, "AnimusForge_1_4_5"));
#endif
			candidates.Add(Path.Combine(modulesDir, "AnimusForge"));
		}
		catch
		{
		}
		for (int i = 0; i < candidates.Count; i++)
		{
			string candidate = candidates[i];
			if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate))
			{
				return candidate;
			}
		}
		return candidates.Count > 0 ? candidates[0] : "";
	}
}
