using System;

using System.Linq;

using System.Reflection;

using System.Runtime.CompilerServices;

using HarmonyLib;

using TaleWorlds.CampaignSystem;

using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

using TaleWorlds.Localization;



namespace AnimusForge;



public static class EncyclopediaKingdomStabilityPatch

{

	private static readonly string StabilityMarker = AdapterPromptCaptureTexts.PcapCompat("Marker_KingdomStability");

	private sealed class AppendedBlock { internal string Text = ""; }

	private static readonly ConditionalWeakTable<EncyclopediaFactionPageVM, AppendedBlock> AppendedBlocks = new ConditionalWeakTable<EncyclopediaFactionPageVM, AppendedBlock>();



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

			string standingText = DiplomacyPresentationBridge.Standing(kingdom?.StringId);

			string combinedText = string.Join("\n\n", new[] { stabilityText, standingText }

				.Where(x => !string.IsNullOrWhiteSpace(x)));

			AppendedBlock previous = AppendedBlocks.GetValue(__instance, _ => new AppendedBlock());

			string baseText = RemoveExistingStabilityBlock(__instance.InformationText, previous.Text);

			if (string.IsNullOrWhiteSpace(combinedText))

			{

				__instance.InformationText = baseText;

				previous.Text = "";

				return;

			}

			__instance.InformationText = AppendOrReplaceStabilityBlock(baseText, combinedText);

			previous.Text = combinedText.Trim();

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

		string baseText = (input ?? "").Trim();

		string stabilityBlock = (block ?? "").Trim();

		if (string.IsNullOrWhiteSpace(stabilityBlock))

		{

			return baseText;

		}

		return string.IsNullOrWhiteSpace(baseText) ? stabilityBlock : (baseText + "\n\n" + stabilityBlock);

	}



	private static string RemoveExistingStabilityBlock(string input, string previousBlock = null)

	{

		string text = input ?? "";

		// Exact per-page suffix also covers language changes and standing-only blocks.

		if (!string.IsNullOrEmpty(previousBlock) && text.EndsWith(previousBlock, StringComparison.Ordinal))

		{

			int start = text.Length - previousBlock.Length;

			if (start == 0 || text[start - 1] == '\n') text = text.Substring(0, start).TrimEnd();

		}

		int markerIndex = -1;

		foreach (string marker in new[] { StabilityMarker, "Kingdom stability", new TextObject("{=AF_UI_KingdomStability_EncyclopediaTitle}Kingdom stability").ToString() })

		{

			int start = 0;

			while (!string.IsNullOrEmpty(marker) && start < text.Length)

			{

				int found = text.IndexOf(marker, start, StringComparison.Ordinal);

				if (found < 0) break;

				int end = found + marker.Length;

				if ((found == 0 || text[found - 1] == '\n') && (end == text.Length || text[end] == '\r' || text[end] == '\n'))

				{

					if (markerIndex < 0 || found < markerIndex) markerIndex = found;

					break;

				}

				start = end;

			}

		}

		if (markerIndex < 0)

		{

			return text;

		}

		return text.Substring(0, markerIndex).TrimEnd();

	}

}

