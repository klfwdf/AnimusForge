using Bannerlord.ButterLib.Implementation.ObjectSystem.Patches;
using Bannerlord.ButterLib.SubSystems;
using HarmonyLib;

namespace Bannerlord.ButterLib.Implementation.ObjectSystem;

internal class ObjectSystemSubSystem : ISubSystem
{
	private readonly Harmony _harmony = new Harmony("Bannerlord.ButterLib.ObjectSystem");

	public static ObjectSystemSubSystem? Instance { get; private set; }

	public string Id => "Object System";

	public string Name => "{=IA0mVgHJgo}Object System";

	public string Description => "{=mFZTv1nwOx}Mod Developer feature!";

	public bool IsEnabled { get; private set; }

	public bool CanBeDisabled => true;

	public bool CanBeSwitchedAtRuntime => false;

	public ObjectSystemSubSystem()
	{
		Instance = this;
	}

	public void Enable()
	{
		if (!IsEnabled)
		{
			IsEnabled = true;
			CampaignBehaviorManagerPatch.Enable(_harmony);
		}
	}

	public void Disable()
	{
		if (IsEnabled)
		{
			IsEnabled = false;
			CampaignBehaviorManagerPatch.Disable(_harmony);
		}
	}
}
