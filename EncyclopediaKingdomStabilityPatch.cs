using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

namespace AnimusForge;

public static class EncyclopediaKingdomStabilityPatch
{
	private const string StabilityMarker = "【王国稳定度】";

	private static readonly FieldInfo FactionField = AccessTools.Field(typeof(EncyclopediaFactionPageVM), "_faction");
	private static readonly object SyncRoot = new object();
	private static bool _patched;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched)
		{
			return;
		}
		lock (SyncRoot)
		{
			if (_patched)
			{
				return;
			}
			MethodInfo refreshValues = AccessTools.Method(typeof(EncyclopediaFactionPageVM), nameof(EncyclopediaFactionPageVM.RefreshValues));
			if (refreshValues == null)
			{
				Logger.Log("EncyclopediaKingdomStability", "[WARN] EncyclopediaFactionPageVM.RefreshValues not found; skip kingdom stability encyclopedia patch.");
				return;
			}
			Harmony activeHarmony = harmony ?? new Harmony("AnimusForge.encyclopedia.kingdom.stability");
			activeHarmony.Patch(refreshValues, postfix: new HarmonyMethod(typeof(EncyclopediaKingdomStabilityPatch), nameof(RefreshValuesPostfix)));
			_patched = true;
			Logger.Log("EncyclopediaKingdomStability", "[INFO] Kingdom encyclopedia stability patch enabled.");
		}
	}

	public static void RefreshValuesPostfix(EncyclopediaFactionPageVM __instance)
	{
		try
		{
			if (__instance == null)
			{
				return;
			}
			Kingdom kingdom = ResolveKingdom(__instance);
			string stabilityText = MyBehavior.BuildKingdomStabilityEncyclopediaTextForExternal(kingdom);
			string standingText = WorldDiplomacyBehavior.BuildKingdomDiplomaticStandingEncyclopediaTextForExternal(kingdom);
			string combinedText = string.Join("\n\n", new[] { stabilityText, standingText }
				.Where(x => !string.IsNullOrWhiteSpace(x)));
			if (string.IsNullOrWhiteSpace(combinedText))
			{
				__instance.InformationText = RemoveExistingStabilityBlock(__instance.InformationText);
				return;
			}
			__instance.InformationText = AppendOrReplaceStabilityBlock(__instance.InformationText, combinedText);
		}
		catch (Exception ex)
		{
			Logger.Log("EncyclopediaKingdomStability", "[WARN] Failed to append kingdom stability text: " + ex.Message);
		}
	}

	private static Kingdom ResolveKingdom(EncyclopediaFactionPageVM vm)
	{
		if (vm == null || FactionField == null)
		{
			return null;
		}
		return FactionField.GetValue(vm) as Kingdom;
	}

	private static string AppendOrReplaceStabilityBlock(string input, string block)
	{
		string baseText = RemoveExistingStabilityBlock(input).Trim();
		string stabilityBlock = (block ?? "").Trim();
		if (string.IsNullOrWhiteSpace(stabilityBlock))
		{
			return baseText;
		}
		return string.IsNullOrWhiteSpace(baseText) ? stabilityBlock : (baseText + "\n\n" + stabilityBlock);
	}

	private static string RemoveExistingStabilityBlock(string input)
	{
		string text = input ?? "";
		int markerIndex = text.IndexOf(StabilityMarker, StringComparison.Ordinal);
		if (markerIndex < 0)
		{
			return text;
		}
		return text.Substring(0, markerIndex).TrimEnd();
	}
}
