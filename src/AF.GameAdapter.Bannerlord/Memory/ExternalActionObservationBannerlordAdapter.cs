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
				questTitle = "一项原版任务";
			}
			string detailLabel = CampaignCharacterRecordCaptureAdapter.GetQuestCompletionDetailLabel(detail);
			string actionKind = CampaignCharacterRecordCaptureAdapter.GetQuestCompletionActionKind(detail);
			bool? won = detail == QuestBase.QuestCompleteDetails.Success ? true : (detail == QuestBase.QuestCompleteDetails.Cancel ? (bool?)null : false);
			string stableKey = "quest_result:" + ((quest?.StringId ?? questTitle).Trim()) + ":" + detail + ":" + MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
			Settlement settlement = CampaignCharacterRecordCaptureAdapter.ResolveQuestActionSettlement(questGiver);
			if (questGiver != null)
			{
				string npcText = "你交给玩家的任务“" + questTitle + "”已有结果：" + detailLabel + "。";
				_application.RecordExternalNpcAction(questGiver, npcText, stableKey + ":giver", actionKind, isMajor: true, isRecent: true, targetHero: Hero.MainHero, settlement: settlement, locationText: MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement), allowNonLordHero: true, won: won);
			}
			string giverName = questGiver != null ? MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(questGiver) : "任务发布人";
			string playerText = "你承接的任务“" + questTitle + "”已有结果：" + detailLabel + "。发布人：" + giverName + "。";
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
			_application.RecordExternalNpcAction(newCompanion, "你加入了玩家队伍，成为玩家的同伴。", stableKey + ":npc", "companion_join", isMajor: true, isRecent: true, targetHero: Hero.MainHero, settlement: settlement, locationText: MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement), allowNonLordHero: true, won: true);
			_application.RecordExternalPlayerAction("你招募了" + companionName + "加入队伍，成为你的同伴。", stableKey + ":player", "companion_join", isMajor: true, targetHero: newCompanion, settlement: settlement, locationText: MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement), won: true);
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
			string npcText = "你离开了玩家队伍。原因：" + detailLabel + "。";
			string playerText = companionName + "离开了你的队伍。原因：" + detailLabel + "。";
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
				_application.RecordExternalNpcAction(oldGovernor, "你卸任了" + settlementName + "总督。", stablePrefix + ":removed:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(oldGovernor), "governor_removed", isMajor: true, isRecent: true, targetHero: Hero.MainHero, settlement: settlement, locationText: settlementName, allowNonLordHero: true, won: null);
				_application.RecordExternalPlayerAction("你解除了" + oldName + "的" + settlementName + "总督职务。", stablePrefix + ":player_removed:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(oldGovernor), "governor_removed", isMajor: true, targetHero: oldGovernor, settlement: settlement, locationText: settlementName, won: null);
			}
			if (newGovernor != null)
			{
				string newName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(newGovernor);
				_application.RecordExternalNpcAction(newGovernor, "你被任命为" + settlementName + "总督。", stablePrefix + ":appointed:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(newGovernor), "governor_appointed", isMajor: true, isRecent: true, targetHero: Hero.MainHero, settlement: settlement, locationText: settlementName, allowNonLordHero: true, won: true);
				_application.RecordExternalPlayerAction("你任命了" + newName + "担任" + settlementName + "总督。", stablePrefix + ":player_appointed:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(newGovernor), "governor_appointed", isMajor: true, targetHero: newGovernor, settlement: settlement, locationText: settlementName, won: true);
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
			? "你投入 " + investment + " 第纳尔，亲手制造"
			: "你投入 " + investment + " 第纳尔，并委托 "
				+ (string.IsNullOrWhiteSpace(crafterName)
					? "一名家族成员或同伴"
					: crafterName)
				+ " 制造";
		actionText += renamedByOutcome
			? "“" + requested + "”，最终得到“" + finalName + "”"
			: "了“" + finalName + "”";
		actionText += "；成品价值 " + outputValue + " 第纳尔"
			+ (string.IsNullOrWhiteSpace(result)
				? "。"
				: "，制造结果为" + result + "。");

		string stableKey =
			"player_rp_craft_high_value:" + normalizedBatchId;
		Settlement settlement =
			Settlement.CurrentSettlement
			?? MobileParty.MainParty?.CurrentSettlement;
		string locationText =
			MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement);
		if (string.IsNullOrWhiteSpace(locationText))
		{
			locationText = "旅途中";
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
			? "玩家投入 " + investment + " 第纳尔亲手制造"
			: "玩家投入 " + investment + " 第纳尔，委托 "
				+ (string.IsNullOrWhiteSpace(crafterName)
					? "一名家族成员或同伴"
					: crafterName)
				+ " 制造";
		weeklySnapshot += renamedByOutcome
			? "“" + requested + "”，最终得到“" + finalName + "”"
			: "了“" + finalName + "”";
		weeklySnapshot += "；成品价值 " + outputValue + " 第纳尔"
			+ (string.IsNullOrWhiteSpace(result)
				? "。"
				: "，制造结果为" + result + "。");
		_application.RecordEventSourceMaterial(
			"player_rp_craft",
			"贵重物品制造 - " + finalName,
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
			decisionTitle = "一项王国决议";
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
		string optionPhrase = string.IsNullOrWhiteSpace(optionTitle) ? "" : ("，支持“" + optionTitle + "”");
		string stableKey = "vote_deal_fulfilled:" + ((dealId ?? "").Trim()) + ":" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(npc) + ":" + MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(decisionTitle);
		string npcText = "你履行了与玩家达成的投票交易，在“" + decisionTitle + "”中按承诺投票" + optionPhrase + "。";
		string playerText = "你促成" + MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(npc) + "履行投票交易，在“" + decisionTitle + "”中按承诺投票" + optionPhrase + "。";
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
			string locationText = string.IsNullOrWhiteSpace(settlementName) ? "越狱现场" : settlementName;
			string locationPhrase = string.IsNullOrWhiteSpace(settlementName) ? "" : ("在" + settlementName + "的地牢中");
			string playerName = MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(player);
			string text = "你" + locationPhrase + "被" + playerName + "越狱营救，成功脱离囚禁。";
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
		string text = targetWon ? ("你在一场正式决斗中击败了" + playerName + "。") : ("你在一场正式决斗中败给了" + playerName + "。");
		NpcActionFacts npcActionFacts = CampaignCharacterRecordCaptureAdapter.CreateNpcActionFacts("duel_result", targetHero);
		CampaignCharacterRecordCaptureAdapter.ApplyTargetFacts(npcActionFacts, mainHero);
		npcActionFacts.Won = targetWon;
		npcActionFacts.LocationText = locationText;
		string stableKey = "duel_result:" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(targetHero) + ":" + MemoryEntityIdentityBannerlordAdapter.GetHeroId(mainHero) + ":" + day + ":" + hour + ":" + (targetWon ? "npc_win" : "npc_loss");
		_application.RecordNpcOutcome(targetHero, text, stableKey, npcActionFacts, allowNonLordHero: true);
		Settlement settlement = Settlement.CurrentSettlement;
		string playerText = playerWon ? ("你在一场正式决斗中击败了" + targetName + "。") : ("你在一场正式决斗中败给了" + targetName + "。");
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
			string settlementName = settlement.Name?.ToString() ?? settlement.StringId ?? "未知定居点";
			string outcomeLabel = "宽恕/安抚";
			if (aftermath == SiegeAftermathAction.SiegeAftermath.Pillage)
			{
				outcomeLabel = "搜掠";
			}
			else if (aftermath == SiegeAftermathAction.SiegeAftermath.Devastate)
			{
				outcomeLabel = massacreStarted ? "血洗/毁坏" : "毁坏";
			}
			StringBuilder textBuilder = new StringBuilder();
			textBuilder.Append("攻城后处置：");
			textBuilder.Append(Hero.MainHero?.Name?.ToString() ?? "玩家");
			textBuilder.Append("刚攻下 ");
			textBuilder.Append(settlementName);
			textBuilder.Append(" 后执行 ");
			textBuilder.Append(outcomeLabel);
			textBuilder.Append("。");
			if (!string.IsNullOrWhiteSpace(trigger))
			{
				textBuilder.Append("触发：");
				textBuilder.Append(trigger.Trim());
				textBuilder.Append("。");
			}
			if (!string.IsNullOrWhiteSpace(detail))
			{
				textBuilder.Append("细节：");
				textBuilder.Append(detail.Trim());
				textBuilder.Append("。");
			}
			textBuilder.Append("参与士兵约 ");
			textBuilder.Append(Math.Max(0, selectedSoldiers));
			textBuilder.Append(" 人。");
			if (plunderStarted || lootItemTotal > 0 || marketGoldLoot > 0 || civilianGoldLoot > 0)
			{
				textBuilder.Append("掠获：物品 ");
				textBuilder.Append(Math.Max(0, lootItemTotal));
				textBuilder.Append(" 件/");
				textBuilder.Append(Math.Max(0, lootStackKinds));
				textBuilder.Append(" 类，估值 ");
				textBuilder.Append(Math.Max(0, lootValue));
				textBuilder.Append("，市场金币 ");
				textBuilder.Append(Math.Max(0, marketGoldLoot));
				textBuilder.Append("，民众金币 ");
				textBuilder.Append(Math.Max(0, civilianGoldLoot));
				textBuilder.Append("，被索取目标 ");
				textBuilder.Append(Math.Max(0, civilianTargetsLooted));
				textBuilder.Append(" 人。");
			}
			if (massacreStarted || killedCivilianUnits > 0 || killedNotables > 0)
			{
				textBuilder.Append("伤亡：平民单位 ");
				textBuilder.Append(Math.Max(0, killedCivilianUnits));
				textBuilder.Append("，要人 ");
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
			return "竞技场";
		case "wilderness":
			return "野外决斗现场";
		case "meeting":
			return "会面现场";
		}
		string text3 = (Mission.Current?.SceneName ?? "").Trim();
		return string.IsNullOrWhiteSpace(text3) || string.Equals(text3, "Unknown", StringComparison.OrdinalIgnoreCase) ? "决斗现场" : text3;
	}
}
