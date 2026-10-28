using Bannerlord.BUTR.Shared.Helpers;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.CampaignSystem;

namespace Bannerlord.ButterLib.Implementation.SaveSystem.Patches;

internal sealed class BehaviourNamePatch
{
	internal static bool Enable(Harmony harmony)
	{
		return harmony.TryPatch(AccessTools2.Constructor(typeof(CampaignBehaviorBase)), null, SymbolExtensions2.GetMethodInfo((CampaignBehaviorBase x, string y) => CampaignBehaviorBaseCtorPostfix(x, ref y)));
	}

	internal static bool Disable(Harmony harmony)
	{
		harmony.Unpatch(AccessTools2.Constructor(typeof(CampaignBehaviorBase)), SymbolExtensions2.GetMethodInfo((CampaignBehaviorBase x, string y) => CampaignBehaviorBaseCtorPostfix(x, ref y)));
		return true;
	}

	private static void CampaignBehaviorBaseCtorPostfix(CampaignBehaviorBase __instance, ref string? ___StringId)
	{
		ModuleInfoExtendedHelper moduleByType = ModuleInfoHelper.GetModuleByType(__instance.GetType());
		if ((object)moduleByType != null && !moduleByType.IsOfficial)
		{
			___StringId = __instance.GetType().FullName;
		}
	}
}
