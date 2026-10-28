using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Bannerlord.ButterLib.Implementation.MBSubModuleBaseExtended.Patches;
using Bannerlord.ButterLib.Options;
using Bannerlord.ButterLib.SubSystems;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace Bannerlord.ButterLib.Implementation.MBSubModuleBaseExtended;

internal class MBSubModuleBaseExSubSystem : ISubSystem
{
	private readonly Harmony _harmony = new Harmony("Bannerlord.ButterLib.MBSubModuleBaseEx");

	private bool _wasInitialized;

	public static MBSubModuleBaseExSubSystem? Instance { get; private set; }

	public string Id => "MBSubModuleBase extended";

	public string Name => "{=JGylAT3SrB}MBSubModuleBase Extended";

	public string Description => "{=XfveBQYVWH}Mod Developer feature! Introduces a MBSubModuleBase-derived abstract class, that provides new SubModule events.";

	public bool IsEnabled { get; private set; }

	public bool CanBeDisabled => true;

	public bool CanBeSwitchedAtRuntime => false;

	public MBSubModuleBaseExSubSystem()
	{
		Instance = this;
	}

	public void Enable()
	{
		if (!_wasInitialized)
		{
			_wasInitialized = true;
			if (!(SettingsProvider.PopulateSubSystemSettings(this) ?? true))
			{
				return;
			}
		}
		if (!IsEnabled)
		{
			IsEnabled = true;
			ModulePatch.Enable(_harmony);
			MBGameManagerPatch.Enable(_harmony);
		}
	}

	public void Disable()
	{
		if (IsEnabled)
		{
			IsEnabled = false;
			ModulePatch.Disable(_harmony);
			MBGameManagerPatch.Disable(_harmony);
		}
	}

	internal static void LogNoHooksIssue(ILogger logger, int originalCallIndex, int finallyIndex, List<CodeInstruction> codes, MethodBase? currentMethod)
	{
		StringBuilder stringBuilder = new StringBuilder("Indexes: ");
		stringBuilder.Append($"\n\toriginalCallIndex = {originalCallIndex}.\n\tfinallyIndex={finallyIndex}.");
		stringBuilder.Append("\nIL:");
		for (int i = 0; i < codes.Count; i++)
		{
			stringBuilder.Append($"\n\t{i:D4}:\t{codes[i]}");
		}
		HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(currentMethod);
		if (patchInfo != null)
		{
			stringBuilder.Append("\nOther transpilers:");
			foreach (Patch transpiler in patchInfo.Transpilers)
			{
				stringBuilder.Append(transpiler.GetDebugString());
			}
		}
		logger.LogDebug(stringBuilder.ToString());
	}

	internal static bool NotNull<T>(ILogger logger, T obj, string name) where T : class?
	{
		if (obj == null)
		{
			logger.LogError("{Name} is null!", name);
			return false;
		}
		return true;
	}
}
