using Bannerlord.ButterLib.SubModuleWrappers2.Patches;
using Bannerlord.ButterLib.SubSystems;
using HarmonyLib;

namespace Bannerlord.ButterLib.SubModuleWrappers2;

internal sealed class SubModuleWrappers2SubSystem : ISubSystem
{
	private readonly Harmony Harmony = new Harmony("Bannerlord.ButterLib.SubModuleWrappers2");

	public static SubModuleWrappers2SubSystem? Instance { get; private set; }

	public string Id => "SubModuleWrappers2";

	public string Name => "{=NkAAB8EEu2}SubModule Wrappers";

	public string Description => "{=izmKJPjkjN}Mod Developer feature! A wrapper for MBSubModuleBase based on Harmony patches.";

	public bool IsEnabled => true;

	public bool CanBeDisabled => false;

	public bool CanBeSwitchedAtRuntime => false;

	public SubModuleWrappers2SubSystem()
	{
		Instance = this;
		MBSubModuleBasePatch.Enable(Harmony);
	}

	public void Enable()
	{
	}

	public void Disable()
	{
	}
}
