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
 private long _confirmationTicket;
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
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_LabDescription", "Use this lab to test kingdom stability and town-holding clan rebellion logic."));
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
		return Ui("AF_UI_KingdomStability_Label", "{KINGDOM} [{TIER}/{VALUE}] weekly rebellion chance {CHANCE}{IMMUNITY}",
			("KINGDOM", _port.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomStability_KingdomFallback", "kingdom"))),
			("TIER", KingdomStabilityUiText.Tier(kingdomStabilityValue)),
			("VALUE", kingdomStabilityValue.ToString()),
			("CHANCE", _port.FormatKingdomRebellionChance(protectedByImmunity ? 0f : _port.GetKingdomRebellionWeeklyChance(kingdomStabilityValue))),
			("IMMUNITY", immunityText));
	}

	internal string BuildDevKingdomStabilityDetailText(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return Ui("AF_UI_KingdomStability_KingdomNotFound", "The selected kingdom could not be found.");
		}
		int kingdomStabilityValue = _port.GetKingdomStabilityValue(kingdom);
		bool protectedByImmunity = PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom);
		float kingdomRebellionWeeklyChance = _port.GetKingdomRebellionWeeklyChance(kingdomStabilityValue);
		int kingdomStabilityRelationTargetOffset = _port.GetKingdomStabilityRelationTargetOffset(kingdomStabilityValue);
		int kingdomStabilityWeeklyBalancingDelta = _port.GetKingdomStabilityWeeklyBalancingDelta(kingdomStabilityValue);
		int lowClanCountRuleClanCount = _port.CountActiveKingdomClansForLowClanCountRule(kingdom);
		int lowClanCountRoyalDomainLoyaltyAdjustment = _port.GetLowClanCountRoyalDomainLoyaltyAdjustment(kingdomStabilityValue, lowClanCountRuleClanCount);
		if (protectedByImmunity)
		{
			kingdomRebellionWeeklyChance = 0f;
			kingdomStabilityRelationTargetOffset = 0;
			lowClanCountRoyalDomainLoyaltyAdjustment = 0;
		}
		List<KingdomRebellionCandidateInfo> list = protectedByImmunity ? new List<KingdomRebellionCandidateInfo>() : _port.EvaluateKingdomRebellionCandidates(kingdom, forceTrigger: false);
		List<KingdomRebellionCandidateInfo> list2 = list.Where((KingdomRebellionCandidateInfo x) => x != null && x.Eligible).Take(5).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailKingdom", "Kingdom: {KINGDOM}", ("KINGDOM", _port.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomStability_KingdomFallback", "kingdom")))));
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailStability", "Current stability: {VALUE} ({TIER})", ("VALUE", kingdomStabilityValue.ToString()), ("TIER", KingdomStabilityUiText.Tier(kingdomStabilityValue))));
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailWeeklyBalance", "Dynamic weekly balance modifier: {VALUE}", ("VALUE", _port.FormatKingdomStabilityRelationOffsetText(kingdomStabilityWeeklyBalancingDelta))));
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailRelation", "Current non-royal relation modifier: {VALUE} (applies to the king and adult non-royal clan members in this kingdom)", ("VALUE", _port.FormatKingdomStabilityRelationOffsetText(kingdomStabilityRelationTargetOffset))));
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailClanCount", "Effective non-royal clans: {COUNT}; royal-domain daily loyalty modifier: {VALUE}", ("COUNT", lowClanCountRuleClanCount.ToString()), ("VALUE", _port.FormatKingdomStabilityRelationOffsetText(lowClanCountRoyalDomainLoyaltyAdjustment))));
		if (protectedByImmunity)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailImmuneChance", "This week's rebellion chance: 0% (player-kingdom stability rebellion immunity is enabled)"));
		}
		else
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailChance", "This week's rebellion chance: {CHANCE}", ("CHANCE", _port.FormatKingdomRebellionChance(kingdomRebellionWeeklyChance))));
		}
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailKing", "Current king: {HERO}", ("HERO", _port.GetHeroDisplayName(kingdom.Leader))));
		stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailRulingClan", "Ruling clan: {CLAN}", ("CLAN", _port.GetClanDisplayName(kingdom.RulingClan))));
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine(" ");
		if (protectedByImmunity)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailImmuneCandidates", "Automatic rebellion candidates: the mod's stability rebellion check is skipped while immunity is active."));
		}
		else if (list2.Count > 0)
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailCandidates", "Automatic rebellion candidates:"));
			foreach (KingdomRebellionCandidateInfo item in list2)
			{
				stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailCandidate", "- {CLAN} | Relation {RELATION} | Towns {TOWNS} | Castles {CASTLES} | Score {SCORE}", ("CLAN", item.ClanName), ("RELATION", item.RelationToKing.ToString()), ("TOWNS", item.TownCount.ToString()), ("CASTLES", item.CastleCount.ToString()), ("SCORE", item.Score.ToString("0.0"))));
			}
		}
		else
		{
			stringBuilder.AppendLine(Ui("AF_UI_KingdomStability_DetailNoCandidates", "No clans currently meet the automatic rebellion requirements."));
		}
		return stringBuilder.ToString().TrimEnd();
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
			new InquiryElement("test_roll", Ui("AF_UI_KingdomStability_TestRoll", "Test this week's rebellion check"), null),
			new InquiryElement("force_rebellion", Ui("AF_UI_KingdomStability_ForceRebellion", "Force rebellion for this kingdom"), null),
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
			case "test_roll":
				RunDevKingdomRebellionTest(kingdom);
				break;
			case "force_rebellion":
				ConfirmForceDevKingdomRebellion(kingdom);
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
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		int num = Math.Max(1, _port.GetCurrentGameDayIndexSafe() / 7);
		KingdomRebellionResolutionResult result = _port.ResolveKingdomRebellion(kingdom, num, executeAction: false, forceTrigger: false);
		InformationManager.ShowInquiry(new InquiryData(Ui("AF_UI_KingdomStability_TestResultTitle", "Rebellion test result - {KINGDOM}", ("KINGDOM", _port.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomStability_KingdomFallback", "kingdom")))), EventEditorProjection.BuildKingdomRebellionResolutionText(_port, result), isAffirmativeOptionShown: true, isNegativeOptionShown: false, Ui("AF_UI_KingdomStability_BackToDetails", "Back to details"), "", delegate
		{
    if (!_port.IsCurrent(generation)) return;
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}, null));
	}

	internal void ConfirmForceDevKingdomRebellion(Kingdom kingdom)
	{
		long generation = _port.CaptureGeneration();
		if (!_port.IsCurrent(generation)) return;
		if (kingdom == null)
		{
			OpenDevKingdomStabilityLabMenu();
			return;
		}
		long ticket = ++_confirmationTicket;
		bool completed = false;
		InformationManager.ShowInquiry(new InquiryData(Ui("AF_UI_KingdomStability_ForceTitle", "Force kingdom rebellion"), Ui("AF_UI_KingdomStability_ForceBody", "This skips the stability probability check but does not relax relation requirements. The system will still choose the best town-holding clan from those that currently meet the automatic rebellion requirements.\n\nIf no clan meets the relation requirements, no rebellion will occur.\n\nThis action will permanently change the kingdom structure in the save. Continue?"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Ui("AF_UI_KingdomStability_ForceExecute", "Force execution"), Ui("AF_UI_KingdomStability_Cancel", "Cancel"), delegate
		{
    if (completed || ticket != _confirmationTicket || !_port.IsCurrent(generation)) return;
    completed = true;
			int num = Math.Max(1, _port.GetCurrentGameDayIndexSafe() / 7);
			KingdomRebellionResolutionResult kingdomRebellionResolutionResult = _port.ResolveKingdomRebellion(kingdom, num, executeAction: false, forceTrigger: true);
			Clan selectedClan = kingdomRebellionResolutionResult?.SelectedClan;
			KingdomRebellionCandidateInfo kingdomRebellionCandidateInfo = kingdomRebellionResolutionResult?.Candidates?.FirstOrDefault((KingdomRebellionCandidateInfo x) => x != null && x.Clan == selectedClan);
			if (selectedClan == null || kingdomRebellionCandidateInfo == null)
			{
				InformationManager.ShowInquiry(new InquiryData(Ui("AF_UI_KingdomStability_ForceResultTitle", "Forced rebellion result - {KINGDOM}", ("KINGDOM", _port.GetKingdomDisplayName(kingdom, Ui("AF_UI_KingdomStability_KingdomFallback", "kingdom")))), EventEditorProjection.BuildKingdomRebellionResolutionText(_port, kingdomRebellionResolutionResult), isAffirmativeOptionShown: true, isNegativeOptionShown: false, Ui("AF_UI_KingdomStability_BackToDetails", "Back to details"), "", delegate
				{
    if (!_port.IsCurrent(generation)) return;
					OpenDevKingdomStabilityDetailMenu(kingdom);
				}, null));
				return;
			}
			_port.StartDevForcedKingdomRebellionAsync(kingdom, selectedClan, num, kingdomRebellionCandidateInfo.RelationToKing, kingdomRebellionCandidateInfo.TownCount, kingdomRebellionCandidateInfo.CastleCount, kingdomRebellionResolutionResult.SelectedFollowerClans);
		}, delegate
		{
    if (completed || ticket != _confirmationTicket || !_port.IsCurrent(generation)) return;
    completed = true;
			OpenDevKingdomStabilityDetailMenu(kingdom);
		}));
	}
}
