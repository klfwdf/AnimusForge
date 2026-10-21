using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal sealed class KingdomStabilityLabController
{
 private readonly KingdomStabilityLabPort _port;
 internal KingdomStabilityLabController(KingdomStabilityLabPort port) { _port = port; }
 private static string Ui(string id, string fallback) => new TextObject("{=" + id + "}" + fallback).ToString();
 private static string Ui(string id, string fallback, params (string Name, string Value)[] variables)
 {
  return EventEditorUiText.T(id, fallback, variables);
 }
	internal void OpenDevKingdomStabilityLabMenu()
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		List<Kingdom> devEditableKingdoms = EventEditorProjection.GetDevEditableKingdoms();
		if (devEditableKingdoms.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomStability_NoKingdoms", "No kingdoms are available for testing.")));
			_port.OpenDevEventEditorMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", Ui("AF_UI_KingdomStability_Back", "Back"), null));
		foreach (Kingdom item in devEditableKingdoms)
		{
			list.Add(new InquiryElement(item.StringId ?? "", BuildDevKingdomStabilityLabel(item), null));
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_LabDescription", "Use this lab to inspect and adjust kingdom stability. Uprisings are handled by civil-war factions."));
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DefaultDescription", "All kingdoms start at stability 50 (Average) and do not rebel automatically by default."));
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_WeeklyReportDescription", "STAB_* ratings in weekly reports also change kingdom stability; manual adjustments and weekly ratings affect later weekly resolution."));
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_SelectKingdom", "Select a kingdom to open its details."));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Ui("AF_UI_KingdomStability_LabTitle", "Kingdom Stability and Rebellion Lab"), stringBuilder.ToString().TrimEnd(), list, isExitShown: true, 0, 1, Ui("AF_UI_KingdomStability_Enter", "Enter"), Ui("AF_UI_KingdomStability_Back", "Back"), delegate(List<InquiryElement> selected)
		{
    if (!_port.IsCurrent(generation)) return;
			if (selected == null || selected.Count == 0)
			{
				_port.OpenDevEventEditorMenu();
				return;
			}
			if (selected[0].Identifier is string text && text == "back")
			{
				_port.OpenDevEventEditorMenu();
				return;
			}
			string kingdomId = selected[0].Identifier as string;
			Kingdom kingdom = _port.FindKingdomById(kingdomId);
			if (kingdom == null)
			{
				InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomStability_KingdomNotFound", "The selected kingdom could not be found.")));
				OpenDevKingdomStabilityLabMenu();
			}
			else
			{
				OpenDevKingdomStabilityDetailMenu(kingdom);
			}
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			_port.OpenDevEventEditorMenu();
		}, "", isSeachAvailable: true);
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal string BuildDevKingdomStabilityLabel(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return Ui("AF_UI_KingdomStability_UnknownKingdom", "unknown kingdom");
		}
		int kingdomStabilityValue = _port.GetKingdomStabilityValue(kingdom);
		bool protectedByImmunity = PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom);
		string immunityText = protectedByImmunity ? Ui("AF_UI_KingdomStability_PlayerImmunitySuffix", " (player-kingdom stability rebellion immunity)") : "";
		return Ui("AF_UI_KingdomStability_Label", "{KINGDOM} [{TIER}/{VALUE}]{IMMUNITY}",
			("KINGDOM", _port.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomStability_KingdomFallback", "kingdom"))),
			("TIER", KingdomStabilityUiText.Tier(kingdomStabilityValue)),
			("VALUE", kingdomStabilityValue.ToString()),
			("IMMUNITY", immunityText));
	}

    internal string BuildDevKingdomStabilityDetailText(Kingdom kingdom)
    {
        if (kingdom == null) return Ui("AF_UI_KingdomStability_KingdomNotFound", "The selected kingdom could not be found.");
        int value = _port.GetKingdomStabilityValue(kingdom);
        bool immune = PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom);
        int relation = immune ? 0 : _port.GetKingdomStabilityRelationTargetOffset(value);
        int count = _port.CountActiveKingdomClansForLowClanCountRule(kingdom);
        int loyalty = immune ? 0 : _port.GetLowClanCountRoyalDomainLoyaltyAdjustment(value, count);
        var text = new StringBuilder();
        text.AppendLine(Ui("AF_UI_KingdomStability_DetailKingdom", "Kingdom: {KINGDOM}", ("KINGDOM", _port.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomStability_KingdomFallback", "kingdom")))));
        text.AppendLine(Ui("AF_UI_KingdomStability_DetailStability", "Current stability: {VALUE} ({TIER})", ("VALUE", value.ToString()), ("TIER", KingdomStabilityUiText.Tier(value))));
        text.AppendLine(Ui("AF_UI_KingdomStability_DetailWeeklyBalance", "Dynamic weekly balance modifier: {VALUE}", ("VALUE", _port.FormatKingdomStabilityRelationOffsetText(_port.GetKingdomStabilityWeeklyBalancingDelta(value)))));
        text.AppendLine(Ui("AF_UI_KingdomStability_DetailRelation", "Current non-royal relation modifier: {VALUE} (applies to the king and adult non-royal clan members in this kingdom)", ("VALUE", _port.FormatKingdomStabilityRelationOffsetText(relation))));
        text.AppendLine(Ui("AF_UI_KingdomStability_DetailClanCount", "Effective non-royal clans: {COUNT}; royal-domain daily loyalty modifier: {VALUE}", ("COUNT", count.ToString()), ("VALUE", _port.FormatKingdomStabilityRelationOffsetText(loyalty))));
        text.AppendLine(Ui("AF_UI_KingdomStability_FactionRoute", "Uprisings are handled by civil-war factions through demands, responses and the shared war cap; stability does not trigger a separate random rebellion."));
        if (immune) text.AppendLine(Ui("AF_UI_KingdomStability_FactionImmunity", "Player-kingdom immunity prevents faction uprisings."));
        text.AppendLine(Ui("AF_UI_KingdomStability_DetailKing", "Current king: {HERO}", ("HERO", _port.GetHeroDisplayName(kingdom.Leader))));
        text.AppendLine(Ui("AF_UI_KingdomStability_DetailRulingClan", "Ruling clan: {CLAN}", ("CLAN", _port.GetClanDisplayName(kingdom.RulingClan))));
        return text.ToString().TrimEnd();
    }

	internal void OpenDevKingdomStabilityDetailMenu(Kingdom kingdom)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>
		{
			new InquiryElement("set_value", Ui("AF_UI_KingdomStability_Adjust", "Adjust stability"), null),
			new InquiryElement("back", Ui("AF_UI_KingdomStability_BackToList", "Back to kingdom list"), null)
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Ui("AF_UI_KingdomStability_DetailTitle", "Kingdom stability details - {KINGDOM}", ("KINGDOM", _port.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomStability_KingdomFallback", "kingdom")))), BuildDevKingdomStabilityDetailText(kingdom), list, isExitShown: true, 0, 1, Ui("AF_UI_KingdomStability_Execute", "Execute"), Ui("AF_UI_KingdomStability_Back", "Back"), delegate(List<InquiryElement> selected)
		{
    if (!_port.IsCurrent(generation)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevKingdomStabilityLabMenu();
				return;
			}
			switch (selected[0].Identifier as string)
			{
			case "set_value":
				OpenDevEditKingdomStability(kingdom);
				break;
			default:
				OpenDevKingdomStabilityLabMenu();
				break;
			}
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevKingdomStabilityLabMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OpenDevEditKingdomStability(Kingdom kingdom)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		int kingdomStabilityValue = _port.GetKingdomStabilityValue(kingdom);
		string text = Ui("AF_UI_KingdomStability_EditPrompt", "Enter a stability value from 0 to 100.\n\nCurrent value: {VALUE} ({TIER})\nDefault: 50 (Average)\n\nTiers:\n90-100 Extremely high\n75-89 High\n60-74 Fairly high\n40-59 Average\n25-39 Poor\n10-24 Very poor\n0-9 Extremely poor", ("VALUE", kingdomStabilityValue.ToString()), ("TIER", KingdomStabilityUiText.Tier(kingdomStabilityValue)));
		InformationManager.ShowTextInquiry(new TextInquiryData(Ui("AF_UI_KingdomStability_EditTitle", "Adjust stability - {KINGDOM}", ("KINGDOM", _port.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomStability_KingdomFallback", "kingdom")))), text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, Ui("AF_UI_KingdomStability_Save", "Save"), Ui("AF_UI_KingdomStability_Back", "Back"), delegate(string input)
		{
    if (!_port.IsCurrent(generation)) return;
			if (!int.TryParse((input ?? "").Trim(), out var result))
			{
				InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomStability_InvalidValue", "Enter an integer from 0 to 100.")));
				OpenDevEditKingdomStability(kingdom);
				return;
			}
			result = _port.ClampKingdomStabilityValue(result);
			_port.SetKingdomStabilityValue(kingdom, result);
			InformationManager.DisplayMessage(new InformationMessage(Ui("AF_UI_KingdomStability_Updated", "{KINGDOM} stability updated to {VALUE} ({TIER}).", ("KINGDOM", _port.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomStability_KingdomFallback", "kingdom"))), ("VALUE", result.ToString()), ("TIER", KingdomStabilityUiText.Tier(result)))));
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}, delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}));
	}

    internal void RunDevKingdomRebellionTest(Kingdom kingdom)
        => InformationManager.DisplayMessage(new InformationMessage(KingdomRebellionRuntimeController.LegacyRebellionRetiredMessage));

    internal void ConfirmForceDevKingdomRebellion(Kingdom kingdom)
        => InformationManager.DisplayMessage(new InformationMessage(KingdomRebellionRuntimeController.LegacyRebellionRetiredMessage));
}
