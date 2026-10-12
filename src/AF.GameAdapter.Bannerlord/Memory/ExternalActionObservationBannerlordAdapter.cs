using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using NpcActionFacts = AnimusForge.MyBehavior.NpcActionFacts;

namespace AnimusForge;
// External callbacks only: no module rule changes, no duplicate record state.
internal sealed class ExternalActionObservationBannerlordAdapter
{
    private readonly ExternalActionObservationApplication _application;
    private readonly Func<int> _nextSequence;
    private readonly Action<Hero,int> _removeEscape;
    internal ExternalActionObservationBannerlordAdapter(ExternalActionObservationApplication application,Func<int> nextSequence,Action<Hero,int> removeEscape)
    { _application=application;_nextSequence=nextSequence;_removeEscape=removeEscape; }
internal void OnQuestCompletedForActionHistory(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		try
		{
			// DebtPromiseQuest is bookkeeping for an already-recorded debt action, not a separate narrative quest result.
			if (quest is DebtPromiseQuest)
			{
				return;
			}
			Hero questGiver = quest?.QuestGiver;
			string questTitle = (quest?.Title?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(questTitle))
			{
				questTitle = AdapterSceneTexts.Raw("ExternalObservation_000");
			}
			string detailLabel = CampaignCharacterRecordCaptureAdapter.GetQuestCompletionDetailLabel(detail);
			string actionKind = CampaignCharacterRecordCaptureAdapter.GetQuestCompletionActionKind(detail);
			bool? won = detail == QuestBase.QuestCompleteDetails.Success ? true : (detail == QuestBase.QuestCompleteDetails.Cancel ? (bool?)null : false);
			string stableKey = "quest_result:" + ((quest?.StringId ?? questTitle).Trim()) + ":" + detail + ":" + MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
			Settlement settlement = CampaignCharacterRecordCaptureAdapter.ResolveQuestActionSettlement(questGiver);
			if (questGiver != null)
			{
				string npcText = AdapterSceneTexts.Raw("ExternalObservation_001") + questTitle + AdapterSceneTexts.Raw("ExternalObservation_002") + detailLabel + "。";
				_application.RecordExternalNpcAction(questGiver, npcText, stableKey + ":giver", actionKind, isMajor: true, isRecent: true, targetHero: Hero.MainHero, settlement: settlement, locationText: MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement), allowNonLordHero: true, won: won);
			}
			string giverName = questGiver != null ? MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(questGiver) : AdapterSceneTexts.Raw("ExternalObservation_003");
			string playerText = AdapterSceneTexts.Raw("ExternalObservation_004") + questTitle + AdapterSceneTexts.Raw("ExternalObservation_005") + detailLabel + AdapterSceneTexts.Raw("ExternalObservation_006") + giverName + "。";
			_application.RecordExternalPlayerAction(playerText, stableKey + ":player", actionKind, isMajor: true, targetHero: questGiver, settlement: settlement, locationText: MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement), won: won);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnQuestCompletedForActionHistory: " + ex.Message);
		}
	}
internal void OnNewCompanionAddedForActionHistory(Hero newCompanion)
	{
		try
		{
			if (newCompanion == null || !newCompanion.IsPlayerCompanion || newCompanion == Hero.MainHero)
			{
				return;
			}
			Settlement settlement = CampaignCharacterRecordCaptureAdapter.ResolveCurrentActionSettlement(newCompanion);
			string companionName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(newCompanion);
			string stableKey = "companion_join:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(newCompanion) + ":" + MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
			_application.RecordExternalNpcAction(newCompanion, AdapterSceneTexts.Raw("ExternalObservation_007"), stableKey + ":npc", "companion_join", isMajor: true, isRecent: true, targetHero: Hero.MainHero, settlement: settlement, locationText: MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement), allowNonLordHero: true, won: true);
			_application.RecordExternalPlayerAction(AdapterSceneTexts.Raw("ExternalObservation_008") + companionName + AdapterSceneTexts.Raw("ExternalObservation_009"), stableKey + ":player", "companion_join", isMajor: true, targetHero: newCompanion, settlement: settlement, locationText: MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement), won: true);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnNewCompanionAddedForActionHistory: " + ex.Message);
		}
	}
internal void OnCompanionRemovedForActionHistory(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		try
		{
			if (companion == null || companion == Hero.MainHero)
			{
				return;
			}
			Settlement settlement = CampaignCharacterRecordCaptureAdapter.ResolveCurrentActionSettlement(companion);
			string detailLabel = CampaignCharacterRecordCaptureAdapter.GetCompanionRemovedDetailLabel(detail);
			string companionName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(companion);
			string stableKey = "companion_leave:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(companion) + ":" + detail + ":" + MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
			string npcText = AdapterSceneTexts.Raw("ExternalObservation_010") + detailLabel + "。";
			string playerText = companionName + AdapterSceneTexts.Raw("ExternalObservation_011") + detailLabel + "。";
			_application.RecordExternalNpcAction(companion, npcText, stableKey + ":npc", "companion_leave", isMajor: true, isRecent: true, targetHero: Hero.MainHero, settlement: settlement, locationText: MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement), allowNonLordHero: true, won: null);
			_application.RecordExternalPlayerAction(playerText, stableKey + ":player", "companion_leave", isMajor: true, targetHero: companion, settlement: settlement, locationText: MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement), won: null);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnCompanionRemovedForActionHistory: " + ex.Message);
		}
	}
internal void OnGovernorChangedForActionHistory(Town fortification, Hero oldGovernor, Hero newGovernor)
	{
		try
		{
			Settlement settlement = fortification?.Settlement;
			if (settlement == null || !CampaignCharacterRecordCaptureAdapter.IsPlayerRelatedGovernorChange(settlement, oldGovernor, newGovernor))
			{
				return;
			}
			string settlementName = MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement);
			string stablePrefix = "governor_changed:" + MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement) + ":" + MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
			if (oldGovernor != null && oldGovernor != newGovernor)
			{
				string oldName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(oldGovernor);
				_application.RecordExternalNpcAction(oldGovernor, AdapterSceneTexts.Raw("ExternalObservation_012") + settlementName + AdapterSceneTexts.Raw("ExternalObservation_013"), stablePrefix + ":removed:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(oldGovernor), "governor_removed", isMajor: true, isRecent: true, targetHero: Hero.MainHero, settlement: settlement, locationText: settlementName, allowNonLordHero: true, won: null);
				_application.RecordExternalPlayerAction(AdapterSceneTexts.Raw("ExternalObservation_014") + oldName + AdapterSceneTexts.Raw("ExternalObservation_015") + settlementName + AdapterSceneTexts.Raw("ExternalObservation_016"), stablePrefix + ":player_removed:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(oldGovernor), "governor_removed", isMajor: true, targetHero: oldGovernor, settlement: settlement, locationText: settlementName, won: null);
			}
			if (newGovernor != null)
			{
				string newName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(newGovernor);
				_application.RecordExternalNpcAction(newGovernor, AdapterSceneTexts.Raw("ExternalObservation_017") + settlementName + AdapterSceneTexts.Raw("ExternalObservation_018"), stablePrefix + ":appointed:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(newGovernor), "governor_appointed", isMajor: true, isRecent: true, targetHero: Hero.MainHero, settlement: settlement, locationText: settlementName, allowNonLordHero: true, won: true);
				_application.RecordExternalPlayerAction(AdapterSceneTexts.Raw("ExternalObservation_019") + newName + AdapterSceneTexts.Raw("ExternalObservation_020") + settlementName + AdapterSceneTexts.Raw("ExternalObservation_021"), stablePrefix + ":player_appointed:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(newGovernor), "governor_appointed", isMajor: true, targetHero: newGovernor, settlement: settlement, locationText: settlementName, won: true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnGovernorChangedForActionHistory: " + ex.Message);
		}
	}
internal void RecordExternalPlayerHighValueRpCraft(
		string batchId,
		string requestedName,
		string finalDisplayName,
		int investedDenars,
		int craftedItemValue,
		string crafterHeroId,
		string crafterDisplayName,
		string outcomeLabel)
	{
		if (investedDenars <= 10000)
		{
			return;
		}
		string normalizedBatchId = (batchId ?? "").Trim();
		string requested = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(
			requestedName,
			80);
		string finalName = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(
			finalDisplayName,
			80);
		if (string.IsNullOrWhiteSpace(normalizedBatchId)
			|| string.IsNullOrWhiteSpace(requested)
			|| string.IsNullOrWhiteSpace(finalName))
		{
			return;
		}
		string crafterName = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(
			crafterDisplayName,
			60);
		string result = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(
			outcomeLabel,
			40);
		Hero player = Hero.MainHero;
		bool craftedByPlayer = string.Equals(
			(crafterHeroId ?? "").Trim(),
			MemoryEntityIdentityBannerlordAdapter.GetHeroId(player),
			StringComparison.OrdinalIgnoreCase);
		string investment = investedDenars.ToString(
			CultureInfo.InvariantCulture);
		string outputValue = Math.Max(1, craftedItemValue).ToString(
			CultureInfo.InvariantCulture);
		bool renamedByOutcome = !string.Equals(
			requested,
			finalName,
			StringComparison.Ordinal);
		string actionText = craftedByPlayer
			? AdapterSceneTexts.Raw("ExternalObservation_022") + investment + AdapterSceneTexts.Raw("ExternalObservation_023")
			: AdapterSceneTexts.Raw("ExternalObservation_024") + investment + AdapterSceneTexts.Raw("ExternalObservation_025")
				+ (string.IsNullOrWhiteSpace(crafterName)
					? AdapterSceneTexts.Raw("ExternalObservation_026")
					: crafterName)
				+ AdapterSceneTexts.Raw("ExternalObservation_027");
		actionText += renamedByOutcome
			? "“" + requested + AdapterSceneTexts.Raw("ExternalObservation_028") + finalName + "”"
			: AdapterSceneTexts.Raw("ExternalObservation_029") + finalName + "”";
		actionText += AdapterSceneTexts.Raw("ExternalObservation_030") + outputValue + AdapterSceneTexts.Raw("ExternalObservation_031")
			+ (string.IsNullOrWhiteSpace(result)
				? "。"
				: AdapterSceneTexts.Raw("ExternalObservation_032") + result + "。");

		string stableKey =
			"player_rp_craft_high_value:" + normalizedBatchId;
		Settlement settlement =
			Settlement.CurrentSettlement
			?? MobileParty.MainParty?.CurrentSettlement;
		string locationText =
			MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement);
		if (string.IsNullOrWhiteSpace(locationText))
		{
			locationText = AdapterSceneTexts.Raw("ExternalObservation_033");
		}
		_application.RecordExternalPlayerAction(
			actionText,
			stableKey + ":action",
			"player_rp_craft",
			isMajor: true,
			targetHero: null,
			settlement: settlement,
			locationText: locationText,
			won: null);

		bool hasKingdom =
			MemoryEntityIdentityBannerlordAdapter.ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(
				out string kingdomId,
				out string settlementId);
		if (string.IsNullOrWhiteSpace(settlementId))
		{
			settlementId = MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement);
		}
		string weeklySnapshot = craftedByPlayer
			? AdapterSceneTexts.Raw("ExternalObservation_034") + investment + AdapterSceneTexts.Raw("ExternalObservation_035")
			: AdapterSceneTexts.Raw("ExternalObservation_036") + investment + AdapterSceneTexts.Raw("ExternalObservation_037")
				+ (string.IsNullOrWhiteSpace(crafterName)
					? AdapterSceneTexts.Raw("ExternalObservation_038")
					: crafterName)
				+ AdapterSceneTexts.Raw("ExternalObservation_039");
		weeklySnapshot += renamedByOutcome
			? "“" + requested + AdapterSceneTexts.Raw("ExternalObservation_040") + finalName + "”"
			: AdapterSceneTexts.Raw("ExternalObservation_041") + finalName + "”";
		weeklySnapshot += AdapterSceneTexts.Raw("ExternalObservation_042") + outputValue + AdapterSceneTexts.Raw("ExternalObservation_043")
			+ (string.IsNullOrWhiteSpace(result)
				? "。"
				: AdapterSceneTexts.Raw("ExternalObservation_044") + result + "。");
		_application.RecordEventSourceMaterial(
			"player_rp_craft",
			AdapterSceneTexts.Raw("ExternalObservation_045") + finalName,
			weeklySnapshot,
			stableKey + ":weekly",
			kingdomId,
			settlementId,
			includeInWorld: true,
			includeInKingdom: hasKingdom,
			actorHeroId: MemoryEntityIdentityBannerlordAdapter.GetHeroId(player),
			actorKingdomId: MemoryEntityIdentityBannerlordAdapter.GetKingdomId(player?.MapFaction));
		Logger.Log(
			"EventWeeklyReport",
			"[PlayerRpCraft] high_value_material_recorded item="
				+ finalName
				+ " invested="
				+ investment
				+ " kingdom="
				+ (kingdomId ?? ""));
	}
internal void RecordVoteDealFulfilled(Hero npc, KingdomDecision decision, DecisionOutcome chosenOutcome, string dealId, string targetDecisionTitle, string targetOptionTitle)
	{
		if (npc == null || npc == Hero.MainHero)
		{
			return;
		}
		string decisionTitle = CampaignCharacterRecordCaptureAdapter.CleanExternalActionTitle(targetDecisionTitle);
		if (string.IsNullOrWhiteSpace(decisionTitle))
		{
			try
			{
				decisionTitle = CampaignCharacterRecordCaptureAdapter.CleanExternalActionTitle(decision?.GetGeneralTitle()?.ToString());
			}
			catch
			{
				decisionTitle = "";
			}
		}
		if (string.IsNullOrWhiteSpace(decisionTitle))
		{
			decisionTitle = AdapterSceneTexts.Raw("ExternalObservation_046");
		}
		string optionTitle = CampaignCharacterRecordCaptureAdapter.CleanExternalActionTitle(targetOptionTitle);
		if (string.IsNullOrWhiteSpace(optionTitle))
		{
			try
			{
				optionTitle = CampaignCharacterRecordCaptureAdapter.CleanExternalActionTitle(chosenOutcome?.GetDecisionTitle()?.ToString());
			}
			catch
			{
				optionTitle = "";
			}
		}
		string optionPhrase = string.IsNullOrWhiteSpace(optionTitle) ? "" : (AdapterSceneTexts.Raw("ExternalObservation_047") + optionTitle + "”");
		string stableKey = "vote_deal_fulfilled:" + ((dealId ?? "").Trim()) + ":" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(npc) + ":" + MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(decisionTitle);
		string npcText = AdapterSceneTexts.Raw("ExternalObservation_048") + decisionTitle + AdapterSceneTexts.Raw("ExternalObservation_049") + optionPhrase + "。";
		string playerText = AdapterSceneTexts.Raw("ExternalObservation_050") + MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(npc) + AdapterSceneTexts.Raw("ExternalObservation_051") + decisionTitle + AdapterSceneTexts.Raw("ExternalObservation_052") + optionPhrase + "。";
		_application.RecordExternalNpcAction(npc, npcText, stableKey + ":npc", "vote_deal_fulfilled", isMajor: true, isRecent: true, targetHero: Hero.MainHero, settlement: null, locationText: MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(decision?.Kingdom, ""), allowNonLordHero: false, won: true);
		_application.RecordExternalPlayerAction(playerText, stableKey + ":player", "vote_deal_fulfilled", isMajor: true, targetHero: npc, settlement: null, locationText: MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(decision?.Kingdom, ""), won: true);
	}
internal void RecordPlayerPrisonBreakRescue(Hero rescuedHero)
	{
		try
		{
			Hero player = Hero.MainHero;
			if (player == null || rescuedHero == null || rescuedHero == player || !CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(rescuedHero, allowNonLordHero: true))
			{
				return;
			}
			int day = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
			int hour = AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.GetCurrentHourOfDaySafeForPrompt();
			Settlement settlement = ResolvePrisonBreakRescueSettlement(rescuedHero, player);
			string settlementName = MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement);
			string locationText = string.IsNullOrWhiteSpace(settlementName) ? AdapterSceneTexts.Raw("ExternalObservation_053") : settlementName;
			string locationPhrase = string.IsNullOrWhiteSpace(settlementName) ? "" : (AdapterSceneTexts.Raw("ExternalObservation_054") + settlementName + AdapterSceneTexts.Raw("ExternalObservation_055"));
			string playerName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(player);
			string text = AdapterSceneTexts.Raw("ExternalObservation_056") + locationPhrase + AdapterSceneTexts.Raw("ExternalObservation_057") + playerName + AdapterSceneTexts.Raw("ExternalObservation_058");
			NpcActionFacts facts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("prison_break_rescue", rescuedHero);
			CampaignCharacterRecordCaptureAdapter.ApplyTargetFacts(facts, player);
			if (settlement != null)
			{
				CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(facts, settlement, null, null, locationText);
				CampaignCharacterRecordCaptureAdapter.AddRelatedFactionFacts(facts, settlement.MapFaction);
			}
			else
			{
				facts.LocationText = locationText;
			}
			facts.Won = true;
			string stableKey = BuildPrisonBreakRescueStableKey(player, rescuedHero, settlement, day, hour);
			_removeEscape(rescuedHero, day);
			_application.RecordNpcOutcome(rescuedHero, text, stableKey, facts, allowNonLordHero: true);
			Logger.Log("PrisonBreakRescue", "Recorded player prison break rescue. hero=" + (rescuedHero.StringId ?? "") + " settlement=" + (settlement?.StringId ?? "") + " day=" + day + " hour=" + hour);
		}
		catch (Exception ex)
		{
			Logger.Log("PrisonBreakRescue", "RecordPlayerPrisonBreakRescue failed: " + ex.Message);
		}
	}
internal void RecordDuelResult(Hero targetHero, bool playerWon, string duelContext)
	{
		Hero mainHero = Hero.MainHero;
		if (mainHero == null || !CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(targetHero, allowNonLordHero: true) || targetHero == mainHero)
		{
			Logger.Log("NpcAction", "Skipped duel result action. target=" + (targetHero?.StringId ?? "") + " playerWon=" + playerWon);
			return;
		}
		int day = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
		int hour = AnimusForge.Refactor.Adapters.PersonaIdentityPromptCaptureAdapter.GetCurrentHourOfDaySafeForPrompt();
		string gameDate = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDateTextSafe();
		string locationText = BuildDuelResultLocationText(duelContext);
		bool targetWon = !playerWon;
		string playerName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(mainHero);
		string targetName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(targetHero);
		string text = targetWon ? (AdapterSceneTexts.Raw("ExternalObservation_059") + playerName + "。") : (AdapterSceneTexts.Raw("ExternalObservation_060") + playerName + "。");
		NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("duel_result", targetHero);
		CampaignCharacterRecordCaptureAdapter.ApplyTargetFacts(npcActionFacts, mainHero);
		npcActionFacts.Won = targetWon;
		npcActionFacts.LocationText = locationText;
		string stableKey = "duel_result:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(targetHero) + ":" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(mainHero) + ":" + day + ":" + hour + ":" + (targetWon ? "npc_win" : "npc_loss");
		_application.RecordNpcOutcome(targetHero, text, stableKey, npcActionFacts, allowNonLordHero: true);
		Settlement settlement = Settlement.CurrentSettlement;
		string playerText = playerWon ? (AdapterSceneTexts.Raw("ExternalObservation_061") + targetName + "。") : (AdapterSceneTexts.Raw("ExternalObservation_062") + targetName + "。");
		string playerStableKey = "player_duel_result:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(mainHero) + ":" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(targetHero) + ":" + day + ":" + hour + ":" + (playerWon ? "player_win" : "player_loss");
		PlayerNotorietyBehavior.RecordPlayerActionForExternal(
			playerText,
			playerStableKey,
			"duel_result",
			isMajor: true,
			day,
			gameDate,
			_nextSequence(),
			MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement),
			(settlement?.Name?.ToString() ?? "").Trim(),
			locationText,
			mainHero.Culture?.StringId ?? "",
			targetHero.Culture?.StringId ?? "",
			settlement?.Culture?.StringId ?? "",
			playerWon);
		Logger.Log("NpcAction", "Recorded duel result action. target=" + (targetHero.StringId ?? "") + " playerWon=" + playerWon + " day=" + day + " hour=" + hour);
	}
internal void RecordAnimusForgeSiegeInterventionForExternal(
		MobileParty attackerParty,
		Settlement settlement,
		SiegeAftermathAction.SiegeAftermath aftermath,
		Clan previousOwner,
		string trigger,
		string detail,
		int selectedSoldiers,
		int lootItemTotal,
		int lootStackKinds,
		int lootValue,
		int marketGoldLoot,
		int civilianGoldLoot,
		int civilianTargetsLooted,
		int killedCivilianUnits,
		int killedNotables,
		bool plunderStarted,
		bool massacreStarted)
	{
		try
		{
			if (settlement == null)
			{
				return;
			}
			string settlementName = settlement.Name?.ToString() ?? settlement.StringId ?? AdapterSceneTexts.Raw("ExternalObservation_063");
			string outcomeLabel = AdapterSceneTexts.Raw("ExternalObservation_064");
			if (aftermath == SiegeAftermathAction.SiegeAftermath.Pillage)
			{
				outcomeLabel = AdapterSceneTexts.Raw("ExternalObservation_065");
			}
			else if (aftermath == SiegeAftermathAction.SiegeAftermath.Devastate)
			{
				outcomeLabel = massacreStarted ? AdapterSceneTexts.Raw("ExternalObservation_066") : AdapterSceneTexts.Raw("ExternalObservation_067");
			}
			StringBuilder textBuilder = new StringBuilder();
			textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_068"));
			textBuilder.Append(Hero.MainHero?.Name?.ToString() ?? AdapterSceneTexts.Raw("ExternalObservation_069"));
			textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_070"));
			textBuilder.Append(settlementName);
			textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_071"));
			textBuilder.Append(outcomeLabel);
			textBuilder.Append("。");
			if (!string.IsNullOrWhiteSpace(trigger))
			{
				textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_072"));
				textBuilder.Append(trigger.Trim());
				textBuilder.Append("。");
			}
			if (!string.IsNullOrWhiteSpace(detail))
			{
				textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_073"));
				textBuilder.Append(detail.Trim());
				textBuilder.Append("。");
			}
			textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_074"));
			textBuilder.Append(Math.Max(0, selectedSoldiers));
			textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_075"));
			if (plunderStarted || lootItemTotal > 0 || marketGoldLoot > 0 || civilianGoldLoot > 0)
			{
				textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_076"));
				textBuilder.Append(Math.Max(0, lootItemTotal));
				textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_077"));
				textBuilder.Append(Math.Max(0, lootStackKinds));
				textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_078"));
				textBuilder.Append(Math.Max(0, lootValue));
				textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_079"));
				textBuilder.Append(Math.Max(0, marketGoldLoot));
				textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_080"));
				textBuilder.Append(Math.Max(0, civilianGoldLoot));
				textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_081"));
				textBuilder.Append(Math.Max(0, civilianTargetsLooted));
				textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_082"));
			}
			if (massacreStarted || killedCivilianUnits > 0 || killedNotables > 0)
			{
				textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_083"));
				textBuilder.Append(Math.Max(0, killedCivilianUnits));
				textBuilder.Append(AdapterSceneTexts.Raw("ExternalObservation_084"));
				textBuilder.Append(Math.Max(0, killedNotables));
				textBuilder.Append("。");
			}
			Hero actorHero = attackerParty?.LeaderHero ?? Hero.MainHero;
			Hero currentOwner = settlement.OwnerClan?.Leader;
			Hero previousOwnerHero = previousOwner?.Leader;
			NpcActionFacts facts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts(AfGcczShoutBridge.RuleId, actorHero);
			CampaignCharacterRecordCaptureAdapter.ApplySettlementFacts(facts, settlement, currentOwner, previousOwnerHero, settlementName);
			CampaignCharacterRecordCaptureAdapter.ApplyTargetFacts(facts, previousOwnerHero);
			CampaignCharacterRecordCaptureAdapter.AddRelatedFactionFacts(facts, settlement.MapFaction);
			CampaignCharacterRecordCaptureAdapter.AddRelatedFactionFacts(facts, previousOwner);
			facts.Won = true;
			string stableKey = "siege_intervention:" + (settlement.StringId ?? settlementName) + ":" + aftermath;
			string text = textBuilder.ToString();
			List<Hero> recipients = new List<Hero>
			{
				currentOwner,
				previousOwnerHero,
				attackerParty?.LeaderHero
			};
			foreach (Hero hero in recipients.Where(h => CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(h)).GroupBy(h => h.StringId ?? "", StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
			{
				_application.RecordNpcOutcome(hero, text, stableKey, facts);
			}
			Logger.Log("AnimusForge", "Recorded GCCZ siege intervention aftermath. Settlement=" + settlementName + ", Outcome=" + outcomeLabel + ", Recipients=" + recipients.Count(h => CampaignCharacterRecordCaptureAdapter.ShouldTrackNpcActionHero(h)));
		}
		catch (Exception ex)
		{
			Logger.Log("AnimusForge", "RecordAnimusForgeSiegeInterventionForExternal failed: " + ex.Message);
		}
	}
internal static Settlement ResolvePrisonBreakRescueSettlement(Hero rescuedHero, Hero player)
	{
		try
		{
			return Settlement.CurrentSettlement ?? rescuedHero?.CurrentSettlement ?? rescuedHero?.StayingInSettlement ?? MobileParty.MainParty?.CurrentSettlement ?? player?.CurrentSettlement;
		}
		catch
		{
			return null;
		}
	}
internal static string BuildPrisonBreakRescueStableKey(Hero player, Hero rescuedHero, Settlement settlement, int day, int hour)
	{
		return "prison_break_rescue:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(player) + ":" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(rescuedHero) + ":" + MemoryEntityIdentityBannerlordAdapter.GetSettlementId(settlement) + ":" + day + ":" + hour;
	}
internal static string BuildDuelResultLocationText(string duelContext)
	{
		string text = (Settlement.CurrentSettlement?.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = (duelContext ?? "").Trim().ToLowerInvariant();
		switch (text2)
		{
		case "arena":
			return AdapterSceneTexts.Raw("ExternalObservation_085");
		case "wilderness":
			return AdapterSceneTexts.Raw("ExternalObservation_086");
		case "meeting":
			return AdapterSceneTexts.Raw("ExternalObservation_087");
		}
		string text3 = (Mission.Current?.SceneName ?? "").Trim();
		return string.IsNullOrWhiteSpace(text3) || string.Equals(text3, "Unknown", StringComparison.OrdinalIgnoreCase) ? AdapterSceneTexts.Raw("ExternalObservation_088") : text3;
	}
}
