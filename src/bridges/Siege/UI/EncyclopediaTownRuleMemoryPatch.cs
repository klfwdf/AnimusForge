using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

namespace AnimusForge;

/// <summary>
/// Appends town rule memory below the vanilla settlement encyclopedia description.
/// </summary>
internal static class EncyclopediaTownRuleMemoryPatch
{
	private static readonly object Gate = new object();
	private static readonly List<WeakReference> TrackedViewModels = new List<WeakReference>();
	private static readonly FieldInfo SettlementField = AccessTools.Field(typeof(EncyclopediaSettlementPageVM), "_settlement");
	private static bool _patched;

	internal static void EnsurePatched(Harmony harmony)
	{
		if (_patched || harmony == null)
		{
			return;
		}
		MethodInfo refreshValues = AccessTools.Method(typeof(EncyclopediaSettlementPageVM), nameof(EncyclopediaSettlementPageVM.RefreshValues));
		if (refreshValues == null)
		{
			Logger.Log("GcczTownRuleMemory", "Settlement encyclopedia RefreshValues method was not found.");
			return;
		}
		harmony.Patch(refreshValues, postfix: new HarmonyMethod(typeof(EncyclopediaTownRuleMemoryPatch), nameof(RefreshValuesPostfix)));
		_patched = true;
	}

	public static void RefreshValuesPostfix(EncyclopediaSettlementPageVM __instance)
	{
		if (__instance == null)
		{
			return;
		}
		try
		{
			Settlement settlement = ResolveSettlement(__instance);
			if (settlement?.IsTown != true)
			{
				return;
			}
			Track(__instance);
			string memoryText = GcczTownRuleMemoryRuntimeBridge.BuildEncyclopediaText(settlement, out _).Trim();
			if (string.IsNullOrWhiteSpace(memoryText))
			{
				return;
			}
			string vanillaText = (__instance.InformationText ?? string.Empty).Trim();
			__instance.InformationText = string.IsNullOrWhiteSpace(vanillaText)
				? memoryText
				: vanillaText + Environment.NewLine + Environment.NewLine + memoryText;
		}
		catch (Exception ex)
		{
			Logger.Log("GcczTownRuleMemory", "Settlement encyclopedia memory append failed: " + ex.Message);
		}
	}

	internal static void OnApplicationTick()
	{
		var changedSettlementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		while (GcczTownRuleMemoryRuntimeBridge.TryDequeueChangedSettlementId(out string settlementId))
		{
			if (!string.IsNullOrWhiteSpace(settlementId))
			{
				changedSettlementIds.Add(settlementId);
			}
		}
		if (changedSettlementIds.Count == 0)
		{
			return;
		}

		var refreshTargets = new List<EncyclopediaSettlementPageVM>();
		lock (Gate)
		{
			for (int index = TrackedViewModels.Count - 1; index >= 0; index--)
			{
				var viewModel = TrackedViewModels[index].Target as EncyclopediaSettlementPageVM;
				if (viewModel == null)
				{
					TrackedViewModels.RemoveAt(index);
					continue;
				}
				Settlement settlement = ResolveSettlement(viewModel);
				if (changedSettlementIds.Contains(settlement?.StringId ?? string.Empty))
				{
					refreshTargets.Add(viewModel);
				}
			}
		}

		foreach (EncyclopediaSettlementPageVM viewModel in refreshTargets)
		{
			try
			{
				viewModel.RefreshValues();
			}
			catch (Exception ex)
			{
				Logger.Log("GcczTownRuleMemory", "Settlement encyclopedia refresh failed: " + ex.Message);
			}
		}
	}

	private static Settlement ResolveSettlement(EncyclopediaSettlementPageVM viewModel)
	{
		return SettlementField?.GetValue(viewModel) as Settlement;
	}

	private static void Track(EncyclopediaSettlementPageVM viewModel)
	{
		lock (Gate)
		{
			foreach (WeakReference weakReference in TrackedViewModels)
			{
				if (ReferenceEquals(weakReference.Target, viewModel))
				{
					return;
				}
			}
			TrackedViewModels.Add(new WeakReference(viewModel));
		}
	}
}
