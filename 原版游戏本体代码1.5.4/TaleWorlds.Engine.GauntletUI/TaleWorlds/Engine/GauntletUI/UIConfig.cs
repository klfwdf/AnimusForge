using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.Engine.GauntletUI;

public static class UIConfig
{
	public static bool DoNotUseGeneratedPrefabs { get; set; }

	public static bool DebugModeEnabled { get; set; }

	public static bool GetIsUsingGeneratedPrefabs()
	{
		if (!NativeConfig.GetUIDoNotUseGeneratedPrefabs)
		{
			return !DoNotUseGeneratedPrefabs;
		}
		return false;
	}

	public static bool GetIsHotReloadEnabled()
	{
		if (!NativeConfig.GetUIDebugMode)
		{
			return DebugModeEnabled;
		}
		return true;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_debug_mode", "ui")]
	public static string SetDebugMode(List<string> args)
	{
		string result = "Format is \"ui.set_debug_mode [1/0]\".";
		if (args.Count == 1)
		{
			if (int.TryParse(args[0], out var result2) && (result2 == 1 || result2 == 0))
			{
				DebugModeEnabled = result2 == 1;
				return "Success.";
			}
			return result;
		}
		return result;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("use_generated_prefabs", "ui")]
	public static string SetUsingGeneratedPrefabs(List<string> args)
	{
		string result = "Format is \"ui.use_generated_prefabs [1/0].\"";
		if (args.Count == 1)
		{
			if (int.TryParse(args[0], out var result2) && (result2 == 1 || result2 == 0))
			{
				DoNotUseGeneratedPrefabs = result2 == 0;
				return "Success.";
			}
			return result;
		}
		return result;
	}
}
