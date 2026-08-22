using System;
using System.Collections.Generic;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

/// <summary>
/// Adds one resource-driven town-memory editor to the existing AF developer menu.
/// </summary>
internal static class GcczTownRuleMemoryDeveloperBridge
{
	private const string DeveloperMenuId = "AnimusForge_dev_root";
	private const string DeveloperOptionId = "AnimusForge_dev_root_gccz_town_memory";
	private const string RegenerateIdentifier = "regenerate_current";

	internal static void Register(CampaignGameStarter starter)
	{
		if (starter == null)
		{
			return;
		}
		TownPromptTextCatalog text = GcczTownPromptResourceProvider.GetCatalog();
		starter.AddGameMenuOption(
			DeveloperMenuId,
			DeveloperOptionId,
			text.SettlementRuleMemoryDeveloperMenuOption,
			OptionCondition,
			OptionConsequence);
	}

	private static bool OptionCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		return MyBehavior.IsDevDataManagementEnabledForExternal()
			&& ResolveCurrentTown() != null;
	}

	private static void OptionConsequence(MenuCallbackArgs args)
	{
		OpenSelection();
	}

	private static void OpenSelection()
	{
		Settlement settlement = ResolveCurrentTown();
		SettlementRuleMemoryRecord record = GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(settlement);
		if (record?.CurrentRule == null)
		{
			return;
		}

		TownPromptTextCatalog text = GcczTownPromptResourceProvider.GetCatalog();
		int currentDay = GetCurrentCampaignDay();
		var options = new List<InquiryElement>
		{
			new InquiryElement(RegenerateIdentifier, text.SettlementRuleMemoryDeveloperRegenerateLabel, null),
		};
		for (int index = 0; index < record.RulerMemories.Count; index++)
		{
			string label = TownPromptComposer.BuildSettlementRuleMemoryDeveloperEntryText(
				record,
				index,
				currentDay,
				text);
			options.Add(new InquiryElement(index, label, null));
		}

		var inquiry = new MultiSelectionInquiryData(
			text.SettlementRuleMemoryDeveloperSelectionTitle,
			text.SettlementRuleMemoryDeveloperSelectionDescription,
			options,
			isExitShown: true,
			0,
			1,
			text.SettlementRuleMemoryDeveloperSaveLabel,
			text.SettlementRuleMemoryDeveloperCancelLabel,
			OnSelection,
			delegate { });
		MBInformationManager.ShowMultiSelectionInquiry(inquiry);
	}

	private static void OnSelection(List<InquiryElement> selected)
	{
		if (selected == null || selected.Count == 0)
		{
			return;
		}
		Settlement settlement = ResolveCurrentTown();
		if (settlement == null)
		{
			return;
		}
		if (string.Equals(selected[0].Identifier as string, RegenerateIdentifier, StringComparison.Ordinal))
		{
			GcczTownRuleMemoryRuntimeBridge.RequestCurrentNarrativeRegeneration(settlement);
			OpenSelection();
			return;
		}
		if (!(selected[0].Identifier is int entryIndex))
		{
			return;
		}

		SettlementRuleMemoryRecord record = GcczTownRuleMemoryRuntimeBridge.GetOrCreateCurrentTownRecord(settlement);
		if (record == null || entryIndex < 0 || entryIndex >= record.RulerMemories.Count)
		{
			return;
		}
		SettlementRuleMemoryEntry entry = record.RulerMemories[entryIndex];
		TownPromptTextCatalog text = GcczTownPromptResourceProvider.GetCatalog();
		string rulerName = string.IsNullOrWhiteSpace(entry.RulerName) ? entry.RulerId : entry.RulerName;
		string title = text.SettlementRuleMemoryDeveloperEditTitleTemplate.Replace("{ruler}", rulerName ?? string.Empty);
		DevHistoryEditPopup.Show(
			title,
			settlement.Name?.ToString() ?? string.Empty,
			entry.Narrative,
			entry.Narrative,
			editedText =>
			{
				GcczTownRuleMemoryRuntimeBridge.TrySetManualNarrative(
					settlement,
					entry.RulerId,
					entry.RuleStartDay,
					editedText);
				OpenSelection();
			},
			OpenSelection,
			text.SettlementRuleMemoryDeveloperEditHint,
			text.SettlementRuleMemoryDeveloperSaveLabel,
			text.SettlementRuleMemoryDeveloperCancelLabel);
	}

	private static Settlement ResolveCurrentTown()
	{
		Settlement settlement = Settlement.CurrentSettlement
			?? Hero.MainHero?.CurrentSettlement
			?? MobileParty.MainParty?.CurrentSettlement;
		return settlement?.IsTown == true ? settlement : null;
	}

	private static int GetCurrentCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}
}
