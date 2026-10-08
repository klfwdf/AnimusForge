using System;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.MountAndBlade;
using DialogueDay = AnimusForge.MyBehavior.DialogueDay;
using System.Collections.Generic;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge.Refactor.Adapters;

internal static class PromptRuleCaptureBannerlordAdapter
{
internal static LegacyDetachedRuleSelector CreateNativeConversationDetachedRuleSelectorForExternal(int topN = 12)
	{
		return new LegacyDetachedRuleSelector(
			(userText, secondaryText, runtimeContext, requestedTopN, excludedRuleIds) =>
			{
				if (AIConfigHandler.TryCallAuxiliaryRuleCodesForExternal(
					userText,
					secondaryText,
					runtimeContext,
					requestedTopN,
					out List<string> ruleIds,
					out string error,
					excludedRuleIds))
				{
					return new DetachedRuleLookupResult(ruleIds, null);
				}
				return new DetachedRuleLookupResult(Array.Empty<string>(), string.IsNullOrWhiteSpace(error) ? "failed" : error);
			},
			 topN);
	}

	internal static string ResolveRuleTargetKey(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
	{
		try
		{
			string text = (targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return "hero:" + text;
			}
			if (targetAgentIndex >= 0)
			{
				return "agent:" + targetAgentIndex;
			}
			string text2 = (targetCharacter?.StringId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				string text3 = (Settlement.CurrentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
				return string.IsNullOrWhiteSpace(text3) ? ("troop:" + text2) : ("troop:" + text2 + "@" + text3);
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

    internal static string CaptureSelectedRuleInstruction(string ruleId,string body,bool hasAnyHero,
        Hero targetHero,CharacterObject targetCharacter,int targetAgentIndex,
        Func<Hero,CharacterObject,int,string> majorInstruction)
    {
        string id=(ruleId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(id)) return "";
        Hero hero=targetHero ?? targetCharacter?.HeroObject;
        string runtime=null, heroJoin=null;
        if (string.Equals(id,"kingdom_service",StringComparison.OrdinalIgnoreCase))
        {
            runtime=AIConfigHandler.BuildRuntimeKingdomServiceInstructionForExternal();
            if (hasAnyHero) heroJoin=AIConfigHandler.BuildRuntimeHeroJoinPartyInstructionForExternal(hero);
        }
        else if (hasAnyHero && string.Equals(id,"kingdom_vassalage",StringComparison.OrdinalIgnoreCase))
            runtime=VassalageBehavior.BuildRuntimeVassalageInstructionForExternal(hero,targetCharacter);
        else if (hasAnyHero && string.Equals(id,"diplomacy",StringComparison.OrdinalIgnoreCase))
            runtime=KingdomAnnexationBehavior.BuildRuntimeAnnexationInstructionForExternal(hero,targetCharacter);
        else if (hasAnyHero && string.Equals(id,"marriage",StringComparison.OrdinalIgnoreCase))
            runtime=RomanceSystemBehavior.Instance?.BuildMarriageRuntimeInstruction(hero) ?? "";
        else if (hasAnyHero && string.Equals(id,"vanilla_issue",StringComparison.OrdinalIgnoreCase))
            runtime=VanillaIssueOfferBridge.BuildRuntimePromptBlockForExternal(hero) ?? "";
        else if (string.Equals(id,"meeting_taunt",StringComparison.OrdinalIgnoreCase))
            runtime=SceneTauntBehavior.BuildUnifiedTauntRuntimeInstructionForExternal(hero,targetCharacter,targetAgentIndex);
        else if (hasAnyHero && string.Equals(id,"npc_major_actions",StringComparison.OrdinalIgnoreCase))
            runtime=majorInstruction(hero,targetCharacter,targetAgentIndex);
        else if (string.Equals(id,"lords_hall_access",StringComparison.OrdinalIgnoreCase))
            runtime=AIConfigHandler.BuildRuntimeLordsHallAccessInstructionForExternal();
        return ExtraRuleInstructionComposer.ResolveSelectedInstruction(id,body,hasAnyHero,runtime,heroJoin);
    }
    internal static List<KeyValuePair<string,string>> CaptureMatchedPreselectedRuleInstructions(
        IEnumerable<string> preselectedRuleIds,int maxRules,bool hasAnyHero,HashSet<string> excludedRuleIdSet,
        Hero targetHero,CharacterObject targetCharacter,int targetAgentIndex,
        Func<Hero,CharacterObject,int,string> majorInstruction)
    {
        try
        {
            var ruleIds=PromptRuleIdPolicy.NormalizePreselectedRuleIds(preselectedRuleIds);
            var result=new List<KeyValuePair<string,string>>();
            if (ruleIds.Count==0) return result;
            int limit=Math.Max(1,maxRules<=0 ? AIConfigHandler.GuardrailRuleReturnCap : maxRules);
            foreach (string ruleId in ruleIds)
            {
                if (result.Count>=limit) break;
                if (PromptRuleIdPolicy.IsBuiltInRuleIdForExtraInjection(ruleId) || PromptRuleIdPolicy.IsExcluded(excludedRuleIdSet,ruleId)) continue;
                if (PromptRuleIdPolicy.IsRuntimeGatedPreprocessRuleId(ruleId) && !AIConfigHandler.CanInjectRuleTopicIntoPreprocessForExternal(ruleId,hasAnyHero)) continue;
                string body=hasAnyHero ? AIConfigHandler.GetGuardrailRuleInstruction(ruleId) : AIConfigHandler.GetGuardrailRuleNonHeroInstruction(ruleId);
                if (string.IsNullOrWhiteSpace(body)) body=AIConfigHandler.GetGuardrailRuleInstruction(ruleId);
                body=CaptureSelectedRuleInstruction(ruleId,body,hasAnyHero,targetHero,targetCharacter,targetAgentIndex,majorInstruction);
                if (!string.IsNullOrWhiteSpace(body)) result.Add(new KeyValuePair<string,string>(ruleId,body));
            }
            return result;
        }
        catch { return new List<KeyValuePair<string,string>>(); }
    }
	internal static void AddPlayerCompanionOrFamilyRuleExclusionsForTarget(HashSet<string> excludedRuleIds, Hero targetHero, CharacterObject targetCharacter = null)
	{
		if (excludedRuleIds == null)
		{
			return;
		}
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		if (AIConfigHandler.IsPlayerPartyTradeLimitedTarget(hero))
		{
			PromptRuleIdPolicy.AddPlayerPartyTradeLimitedExclusions(excludedRuleIds);
		}
	}
	internal static void AddWorldMapCommandRuleExclusionForTarget(HashSet<string> excludedRuleIds, Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
		if (excludedRuleIds == null)
		{
			return;
		}
		if (ShouldExcludeWorldMapCommandRuleForTarget(targetHero, targetCharacter, targetAgentIndex))
		{
			excludedRuleIds.Add("worldmap_party_command");
		}
	}
	internal static void AddSceneMoveRuleExclusionForCurrentMission(HashSet<string> excludedRuleIds)
	{
		if (excludedRuleIds != null && AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission())
		{
			excludedRuleIds.Add("scene_mechanism_actions");
		}
	}
	internal static bool ShouldExcludeWorldMapCommandRuleForTarget(Hero targetHero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null)
			{
				return hero.IsNotable
					|| hero.Occupation == Occupation.Headman
					|| hero.Occupation == Occupation.RuralNotable
					|| hero.Occupation == Occupation.GangLeader
					|| hero.Occupation == Occupation.Merchant
					|| hero.Occupation == Occupation.Artisan
					|| hero.Occupation == Occupation.Preacher;
			}
			if (targetCharacter != null && !targetCharacter.IsHero)
			{
				return !WorldMapPartyCommandBehavior.CanUseNonHeroPartyFallbackForExternal(targetCharacter, targetAgentIndex);
			}
			return false;
		}
		catch
		{
			return false;
		}
	}
	internal static string ResolveTargetKingdomIdForRules(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		try
		{
			string text = (kingdomIdOverride ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.ToLowerInvariant();
			}
			string text2 = (targetHero?.Clan?.Kingdom?.StringId ?? targetHero?.MapFaction?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return text2.ToLowerInvariant();
			}
			Hero heroObject = targetCharacter?.HeroObject;
			string text3 = (heroObject?.Clan?.Kingdom?.StringId ?? heroObject?.MapFaction?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				return text3.ToLowerInvariant();
			}
			return "";
		}
		catch
		{
			return "";
		}
	}
	internal static string NormalizeGuardrailSemanticContextLine(string line)
	{
		string text = (line ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string[] array = text.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		List<string> list = new List<string>();
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = (array[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add(text2);
			}
		}
		return string.Join(" ", list).Trim();
	}
	internal static bool ShouldIncludeGuardrailSemanticContextLine(string line)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (MemoryRecallInputCaptureAdapter.IsActiveSceneSessionHistoryLine(text) || MemoryRecallInputCaptureAdapter.IsLoreInjectionHistoryLine(text) || MemoryRecallInputCaptureAdapter.IsPlayerTurnStartLine(text))
		{
			return false;
		}
		if (text.IndexOf("参与互动让你的脑海里浮现了这些知识", StringComparison.Ordinal) >= 0)
		{
			return false;
		}
		if (text.IndexOf("【以下是关于（", StringComparison.Ordinal) >= 0)
		{
			return false;
		}
		if (text.IndexOf("【触发相关话题/背景】", StringComparison.Ordinal) >= 0)
		{
			return false;
		}
		return true;
	}

	internal static string BuildGuardrailSemanticContext(Func<Hero, List<DialogueDay>> loadHistory, Hero hero, string extraFact)
	{
		try
		{
			List<string> list = new List<string>();
			if (hero != null)
			{
				List<DialogueDay> list2 = loadHistory(hero);
				if (list2 != null && list2.Count > 0)
				{
					List<string> list3 = new List<string>();
					foreach (DialogueDay item in list2)
					{
						if (item == null || item.Lines == null)
						{
							continue;
						}
						for (int i = 0; i < item.Lines.Count; i++)
						{
							string text = (item.Lines[i] ?? "").Trim();
							if (ShouldIncludeGuardrailSemanticContextLine(text))
							{
								list3.Add(text);
							}
						}
					}
					int num = Math.Min(6, list3.Count);
					for (int j = list3.Count - num; j < list3.Count; j++)
					{
						if (j >= 0 && j < list3.Count)
						{
							string text2 = NormalizeGuardrailSemanticContextLine(list3[j]);
							list.Add(text2);
						}
					}
				}
			}
			string text3 = (extraFact ?? "").Trim();
			if (ShouldIncludeGuardrailSemanticContextLine(text3))
			{
				string text5 = NormalizeGuardrailSemanticContextLine(text3);
				if (text5.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) || text5.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
				{
					list.Add(text5);
				}
				else
				{
					list.Add("[AFEF玩家行为补充] " + text5);
				}
			}
			string text6 = VanillaIssueOfferBridge.BuildRagSemanticStateForExternal(hero);
			if (!string.IsNullOrWhiteSpace(text6))
			{
				list.Add(text6);
			}
			if (list.Count <= 0)
			{
				return "";
			}
			return string.Join("\n", list);
		}
		catch
		{
			return "";
		}
	}

internal static string BuildExtraRuleInstructions(Func<Hero,CharacterObject,int,string> majorInstruction, string input, string npcLastUtterance, Hero targetHero, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, IEnumerable<string> excludedRuleIds = null, IEnumerable<string> preselectedRuleIds = null, List<GuardrailRuleHit> fallbackHits = null)
	{
		string text = "";
		string text2 = "";
		string encounterReleaseInstruction = "";
		string restrictedReferralHint = "";
		bool encounterReleaseRuleSelected = false;
		int num = AIConfigHandler.GuardrailRuleReturnCap;
		HashSet<string> excludedRuleIdSet = PromptRuleIdPolicy.BuildRuleIdSet(excludedRuleIds);
		AddPlayerCompanionOrFamilyRuleExclusionsForTarget(excludedRuleIdSet, targetHero, targetCharacter);
		AddWorldMapCommandRuleExclusionForTarget(excludedRuleIdSet, targetHero, targetCharacter, targetAgentIndex);
		AddSceneMoveRuleExclusionForCurrentMission(excludedRuleIdSet);
		string targetKingdomId = ResolveTargetKingdomIdForRules(targetHero, targetCharacter, kingdomIdOverride);
		using IDisposable guardrailScopeJ03 = AIConfigHandler.BeginGuardrailRuntimeScope();
		PromptRuntimeTargetBinding runtimeTargetBinding = SharedPromptCaptureBannerlordAdapter.CreatePromptRuntimeTargetBinding(targetKingdomId, targetHero, targetCharacter, targetAgentIndex);
		AIConfigHandler.ApplyGuardrailRuntimeTarget(runtimeTargetBinding, AIConfigHandler.CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTargetBinding));
		try
		{
			text = preselectedRuleIds != null
				? ExtraRuleInstructionComposer.AssembleMatchedInstructions(CaptureMatchedPreselectedRuleInstructions(preselectedRuleIds, AIConfigHandler.GuardrailRuleReturnCap, hasAnyHero, excludedRuleIdSet, targetHero, targetCharacter, targetAgentIndex, majorInstruction))
				: fallbackHits != null
					? AIConfigHandler.FormatMatchedExtraRuleInstructions(input, npcLastUtterance, hasAnyHero, excludedRuleIdSet, fallbackHits)
					: AIConfigHandler.BuildMatchedExtraRuleInstructions(input, npcLastUtterance, AIConfigHandler.GuardrailRuleReturnCap, hasAnyHero, excludedRuleIdSet);
			encounterReleaseRuleSelected = PromptRuleBlockText.Has(text, "encounter_release_player");
			if (PromptRuleBlockText.Has(text, "party_transfer"))
			{
				string partyTransferRuntimeInstructionForExternal = PartyAssetTransferBannerlordAdapter.BuildPartyTransferRuntimeInstructionForExternal(targetHero, targetCharacter, targetAgentIndex);
				if (!string.IsNullOrWhiteSpace(partyTransferRuntimeInstructionForExternal))
				{
					text = PromptRuleBlockText.ReplaceBody(text, "party_transfer", partyTransferRuntimeInstructionForExternal);
				}
			}
			if (PromptRuleBlockText.Has(text, "vanilla_issue"))
			{
				string vanillaIssueRuntimeInstruction = VanillaIssueOfferBridge.BuildRuntimePromptBlockForExternal(targetHero ?? targetCharacter?.HeroObject);
				if (!string.IsNullOrWhiteSpace(vanillaIssueRuntimeInstruction))
				{
					text = PromptRuleBlockText.ReplaceBody(text, "vanilla_issue", vanillaIssueRuntimeInstruction);
				}
			}
			if (PromptRuleBlockText.Has(text, "npc_major_actions"))
			{
				string npcMajorActionsRuntimeInstruction = majorInstruction(targetHero, targetCharacter, targetAgentIndex);
				if (!string.IsNullOrWhiteSpace(npcMajorActionsRuntimeInstruction))
				{
					text = PromptRuleBlockText.ReplaceBody(text, "npc_major_actions", npcMajorActionsRuntimeInstruction);
				}
			}
			if (!PromptRuleIdPolicy.IsExcluded(excludedRuleIdSet, "lords_hall_access"))
			{
				// Always keep this rule present for the lords-hall gate guard, regardless of semantic hits.
				text2 = (AIConfigHandler.BuildRuntimeLordsHallAccessInstructionForExternal() ?? "").Trim();
			}
			if (!PromptRuleIdPolicy.IsExcluded(excludedRuleIdSet, "encounter_release_player"))
			{
				encounterReleaseInstruction = (LordEncounterBehavior.BuildMeetingPlayerReleaseRuntimeInstructionForExternal(targetHero ?? targetCharacter?.HeroObject, encounterReleaseRuleSelected) ?? "").Trim();
			}
			restrictedReferralHint = BuildRestrictedRuleReferralHint(hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet);
		}
		finally
		{
			AIConfigHandler.ClearGuardrailRuntimeTarget();
		}
		text = PromptRuleBlockText.AppendIfMissing(text, "lords_hall_access", text2, num);
		bool hasEncounterReleaseRuleBlock = PromptRuleBlockText.Has(text, "encounter_release_player");
		if (hasEncounterReleaseRuleBlock)
		{
			if (!string.IsNullOrWhiteSpace(encounterReleaseInstruction))
			{
				text = PromptRuleBlockText.ReplaceBody(text, "encounter_release_player", encounterReleaseInstruction);
			}
			else
			{
				text = PromptRuleBlockText.Remove(text, "encounter_release_player");
			}
		}
		else
		{
			text = PromptRuleBlockText.AppendIfMissing(text, "encounter_release_player", encounterReleaseInstruction, int.MaxValue);
		}
		text = PromptRuleBlockText.AppendIfMissing(text, "noble_deference", BuildNobleDeferenceRuntimeInstruction(hasAnyHero), int.MaxValue);
		if (!string.IsNullOrWhiteSpace(restrictedReferralHint) && (string.IsNullOrWhiteSpace(text) || text.IndexOf("【转介】", StringComparison.OrdinalIgnoreCase) < 0))
		{
			text = string.IsNullOrWhiteSpace(text) ? restrictedReferralHint : (text.TrimEnd() + Environment.NewLine + restrictedReferralHint);
		}
		if (!PromptRuleIdPolicy.IsExcluded(excludedRuleIdSet, "scene_mechanism_actions") && IsSceneFollowingAgentForRules(targetAgentIndex))
		{
			text = ReplaceSceneMechanismRuleForFollowing(text);
		}
		return PromptRuleBlockText.PrependDisclaimer(text);
	}
internal static PromptRuleInstructionSections CaptureRuleInstructionSections(Func<Hero,CharacterObject,int,string> majorInstruction, string input, Hero targetHero, bool useDuelContext, bool isQualified, int playerTier, bool useRewardContext, bool isLoanContext, bool isSurroundingsContext, bool hasAnyHero, CharacterObject targetCharacter, string kingdomIdOverride, int targetAgentIndex, string npcLastUtterance, bool includeDuelStakeContext, bool playerWonLastDuel, bool worldMapPartyCommandContext, IEnumerable<string> excludedRuleIds, IEnumerable<string> preselectedRuleIds, bool suppressForcedMeetingTaunt, List<GuardrailRuleHit> fallbackHits)
	{
		HashSet<string> excludedRuleIdSet = PromptRuleIdPolicy.BuildRuleIdSet(excludedRuleIds);
		AddPlayerCompanionOrFamilyRuleExclusionsForTarget(excludedRuleIdSet, targetHero, targetCharacter);
		AddWorldMapCommandRuleExclusionForTarget(excludedRuleIdSet, targetHero, targetCharacter, targetAgentIndex);
		AddSceneMoveRuleExclusionForCurrentMission(excludedRuleIdSet);
		PromptRuleInstructionSections s = new PromptRuleInstructionSections
		{
			ExcludedRuleIds = excludedRuleIdSet,
			UseDuelContext = useDuelContext,
			IsQualified = isQualified,
			PlayerTier = playerTier,
			IsSurroundingsContext = AIConfigHandler.SurroundingsEnabled && isSurroundingsContext,
			WorldMapPartyCommandContext = worldMapPartyCommandContext
		};
		if (useDuelContext && !PromptRuleIdPolicy.IsExcluded(excludedRuleIdSet, "duel"))
		{
			if (isQualified)
			{
				s.DuelInstruction = BuildDuelRuntimeInstruction(targetHero, targetCharacter, targetAgentIndex);
			}
			else
			{
				s.PlayerDisplayName = PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(targetHero ?? targetCharacter?.HeroObject);
			}
		}
		bool rewardWanted = AIConfigHandler.RewardEnabled && !PromptRuleIdPolicy.IsExcluded(excludedRuleIdSet, "reward");
		bool loanWanted = AIConfigHandler.LoanEnabled && !PromptRuleIdPolicy.IsExcluded(excludedRuleIdSet, "loan");
		s.UseRewardContext = rewardWanted && useRewardContext;
		s.IsLoanContext = loanWanted && isLoanContext;
		if (s.UseRewardContext)
		{
			s.RewardInstruction = ResolveRewardInstructionForPrompt(hasAnyHero, targetHero, targetCharacter);
			s.IncludeDuelStake = AIConfigHandler.DuelStakeEnabled && includeDuelStakeContext;
			s.DuelStakeInstruction = playerWonLastDuel ? AIConfigHandler.DuelStakePlayerWinInstruction : AIConfigHandler.DuelStakeNpcWinInstruction;
		}
		if (s.IsLoanContext)
		{
			s.LoanInstruction = ResolveLoanInstructionForPrompt(hasAnyHero, targetHero, targetCharacter);
		}
		if (s.IsSurroundingsContext && !PromptRuleIdPolicy.IsExcluded(excludedRuleIdSet, "surroundings"))
		{
			s.SurroundingsInstruction = AIConfigHandler.SurroundingsInstruction;
		}
		s.ExtraRuleInstructions = BuildExtraRuleInstructions(majorInstruction, input, npcLastUtterance, targetHero, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, excludedRuleIdSet, preselectedRuleIds, fallbackHits);
		if (worldMapPartyCommandContext && !PromptRuleIdPolicy.IsExcluded(excludedRuleIdSet, "worldmap_party_command"))
		{
			string worldMapInstruction = hasAnyHero ? AIConfigHandler.GetGuardrailRuleInstruction("worldmap_party_command") : AIConfigHandler.GetGuardrailRuleNonHeroInstruction("worldmap_party_command");
			s.WorldMapInstruction = string.IsNullOrWhiteSpace(worldMapInstruction) ? AIConfigHandler.GetGuardrailRuleInstruction("worldmap_party_command") : worldMapInstruction;
		}
		if (PromptRuleBlockText.Has(s.ExtraRuleInstructions, "party_transfer") && PartyAssetTransferBannerlordAdapter.IsPartyTransferRuleEligible(targetHero, targetCharacter, targetAgentIndex))
		{
			s.PartyTransferEligible = true;
			if (rewardWanted && s.RewardInstruction == null)
			{
				s.RewardInstruction = ResolveRewardInstructionForPrompt(hasAnyHero, targetHero, targetCharacter);
			}
			if (loanWanted && s.LoanInstruction == null)
			{
				s.LoanInstruction = ResolveLoanInstructionForPrompt(hasAnyHero, targetHero, targetCharacter);
			}
			if (!rewardWanted) s.RewardInstruction = null;
			if (!loanWanted) s.LoanInstruction = null;
		}
		if (AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.MeetingTauntRuleId) && !suppressForcedMeetingTaunt)
		{
			s.AllowMeetingTaunt = true;
			s.MeetingTauntMarker = AfGcczShoutBridge.MeetingTauntRuleBlockMarker;
			s.MeetingTauntInstruction = SceneTauntBehavior.BuildUnifiedTauntRuntimeInstructionForExternal(targetHero ?? targetCharacter?.HeroObject, targetCharacter, targetAgentIndex);
		}
		return s;
	}
internal static string BuildTriggeredRuleInstructions(Func<Hero,CharacterObject,int,string> majorInstruction, string input, Hero targetHero, bool useDuelContext, bool isQualified, int playerTier, bool useRewardContext, bool isLoanContext, bool isSurroundingsContext, bool hasAnyHero = true, CharacterObject targetCharacter = null, string kingdomIdOverride = null, int targetAgentIndex = -1, string npcLastUtterance = null, bool includeDuelStakeContext = false, bool playerWonLastDuel = false, bool worldMapPartyCommandContext = false, IEnumerable<string> excludedRuleIds = null, IEnumerable<string> preselectedRuleIds = null, bool suppressForcedMeetingTaunt = false, List<GuardrailRuleHit> fallbackHits = null)
	{
		try
		{
			return PromptRuleInstructionComposer.Compose(CaptureRuleInstructionSections(majorInstruction, input, targetHero, useDuelContext, isQualified, playerTier, useRewardContext, isLoanContext, isSurroundingsContext, hasAnyHero, targetCharacter, kingdomIdOverride, targetAgentIndex, npcLastUtterance, includeDuelStakeContext, playerWonLastDuel, worldMapPartyCommandContext, excludedRuleIds, preselectedRuleIds, suppressForcedMeetingTaunt, fallbackHits));
		}
		catch
		{
			return "";
		}
	}
internal static string BuildRestrictedRuleReferralHint(bool hasAnyHero, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, HashSet<string> excludedRuleIdSet)
	{
		List<string> tips = new List<string>();
		if (AnyRestrictedReferralRuleBlocked(hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet, "diplomacy"))
		{
			AddReferralTip(tips, "外交找国王");
		}
		else if (AnyRestrictedReferralRuleBlocked(hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet, "kingdom_vassalage"))
		{
			AddReferralTip(tips, "臣属需双方国王");
		}
		if (AnyRestrictedReferralRuleBlocked(hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet, "kingdom_agenda"))
		{
			AddReferralTip(tips, "投票/提案找领主或国王");
		}
		if (AnyRestrictedReferralRuleBlocked(hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet, "worldmap_party_command"))
		{
			AddReferralTip(tips, "行军/攻击找带队英雄");
		}
		if (AnyRestrictedReferralRuleBlocked(hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet, "party_transfer"))
		{
			AddReferralTip(tips, "部队/俘虏找领主");
		}
		if (AnyRestrictedReferralRuleBlocked(hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet, "marriage"))
		{
			AddReferralTip(tips, "婚事找家主/当事人");
		}
		if (AnyRestrictedReferralRuleBlocked(hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet, "vanilla_issue"))
		{
			AddReferralTip(tips, "任务找委托人");
		}
		if (tips.Count == 0)
		{
			return "";
		}
		if (tips.Count > 4)
		{
			tips = tips.Take(4).ToList();
		}
		return "【转介】无权时简短引导：" + string.Join("；", tips) + "。";
	}
internal static bool AnyRestrictedReferralRuleBlocked(bool hasAnyHero, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, HashSet<string> excludedRuleIdSet, params string[] ruleIds)
	{
		if (ruleIds == null)
		{
			return false;
		}
		for (int i = 0; i < ruleIds.Length; i++)
		{
			if (IsRestrictedReferralRuleBlocked(ruleIds[i], hasAnyHero, targetHero, targetCharacter, targetAgentIndex, excludedRuleIdSet))
			{
				return true;
			}
		}
		return false;
	}
internal static bool IsRestrictedReferralRuleBlocked(string ruleId, bool hasAnyHero, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, HashSet<string> excludedRuleIdSet)
	{
		string id = (ruleId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(id))
		{
			return false;
		}
		if (PromptRuleIdPolicy.IsExcluded(excludedRuleIdSet, id))
		{
			return true;
		}
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		switch (id)
		{
		case "diplomacy":
		case "world_diplomacy_discussion":
		case "kingdom_vassalage":
		case "kingdom_agenda":
		case "marriage":
		case "vanilla_issue":
		case "lords_hall_access":
		case "siege_intervention_aftermath":
			return !AIConfigHandler.CanInjectRuleTopicIntoPreprocessForExternal(id, hasAnyHero);
		case "worldmap_party_command":
			return ShouldExcludeWorldMapCommandRuleForTarget(hero, targetCharacter, targetAgentIndex);
		case "party_transfer":
			return !PartyAssetTransferBannerlordAdapter.IsPartyTransferRuleEligible(hero, targetCharacter, targetAgentIndex);
		default:
			return false;
		}
	}
internal static void AddReferralTip(List<string> tips, string tip)
	{
		string text = (tip ?? "").Trim();
		if (tips != null && !string.IsNullOrWhiteSpace(text) && !tips.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
		{
			tips.Add(text);
		}
	}
internal static string BuildNobleDeferenceRuntimeInstruction(bool hasAnyHero)
	{
		return "";
	}
internal static bool IsSceneFollowingAgentForRules(int targetAgentIndex)
	{
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if (targetAgentIndex < 0 || agents == null || PlayerEncounter.LocationEncounter == null || TaleWorlds.CampaignSystem.Settlements.Locations.LocationComplex.Current == null)
			{
				return false;
			}
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
			if (agent == null || agent == Agent.Main)
			{
				return false;
			}
			TaleWorlds.CampaignSystem.Settlements.Locations.LocationCharacter locationCharacter = TaleWorlds.CampaignSystem.Settlements.Locations.LocationComplex.Current.FindCharacter(agent);
			AccompanyingCharacter accompanyingCharacter = ((locationCharacter != null) ? PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(locationCharacter) : null);
			return accompanyingCharacter != null && accompanyingCharacter.IsFollowingPlayerAtMissionStart;
		}
		catch
		{
			return false;
		}
	}
internal static string ReplaceSceneMechanismRuleForFollowing(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		const string replacement = "【附加规则:scene_mechanism_actions】" + "\r\n" + "【当前正跟随玩家】若此人明确让你停止跟随且你同意，系统会记录停止跟随；若此人改让你去叫【带路与传唤NPC清单】中的人，系统会记录传唤；若此人改让你带路去找【带路与传唤NPC清单】中的目标，系统会记录带路。正文只自然说话，不要自己写标签。";
		int num = text.IndexOf("【附加规则:scene_mechanism_actions】", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return text;
		}
		int num2 = text.IndexOf("【附加规则:", num + 1, StringComparison.Ordinal);
		if (num2 < 0)
		{
			return text.Substring(0, num).TrimEnd() + Environment.NewLine + replacement;
		}
		return text.Substring(0, num).TrimEnd() + Environment.NewLine + replacement + Environment.NewLine + text.Substring(num2).TrimStart();
	}
internal static Agent ResolveDuelRuntimeTargetAgent(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission?.Agents == null)
			{
				return null;
			}
			if (targetAgentIndex >= 0)
			{
				Agent indexedAgent = mission.Agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
				if (indexedAgent != null)
				{
					return indexedAgent;
				}
			}
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero?.CharacterObject != null)
			{
				Agent heroAgent = mission.Agents.FirstOrDefault((Agent a) => a != null && a.Character == hero.CharacterObject);
				if (heroAgent != null)
				{
					return heroAgent;
				}
			}
			if (targetCharacter != null)
			{
				return mission.Agents.FirstOrDefault((Agent a) => a != null && a.Character == targetCharacter);
			}
		}
		catch
		{
		}
		return null;
	}
internal static bool HasDuelRuntimeTarget(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		if (targetHero != null || targetCharacter != null)
		{
			return true;
		}
		Agent agent = ResolveDuelRuntimeTargetAgent(targetHero, targetCharacter, targetAgentIndex);
		if (agent == null)
		{
			return false;
		}
		try
		{
			if (!agent.IsActive() || agent.IsMainAgent || agent == Agent.Main)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		return agent.Character is CharacterObject;
	}
internal static string BuildDuelRuntimeInstruction(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		string baseInstruction = AIConfigHandler.DuelDialogueInstruction;
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		Agent agent = ResolveDuelRuntimeTargetAgent(targetHero, targetCharacter, targetAgentIndex);
		if (hero == null && agent == null)
		{
			return AIConfigHandler.DuelNonHeroInstruction;
		}
		return baseInstruction;
	}
internal static string ResolveRewardInstructionForPrompt(bool hasAnyHero, Hero targetHero, CharacterObject targetCharacter)
	{
		string merchant = (!hasAnyHero && targetCharacter != null && RewardSystemBehavior.Instance != null) ? RewardSystemBehavior.Instance.BuildSettlementMerchantRewardInstruction(targetCharacter) : null;
		string runtimeForMerchant = string.IsNullOrWhiteSpace(merchant) ? null : AIConfigHandler.BuildRuntimeRewardInstructionForExternal(null, targetCharacter);
		string runtime = hasAnyHero ? AIConfigHandler.BuildRuntimeRewardInstructionForExternal(targetHero, targetCharacter) : null;
		return PromptRuleInstructionComposer.ResolveRewardInstruction(hasAnyHero, merchant, runtimeForMerchant, runtime, AIConfigHandler.RewardNonHeroInstruction, AIConfigHandler.RewardInstruction);
	}
internal static string ResolveLoanInstructionForPrompt(bool hasAnyHero, Hero targetHero, CharacterObject targetCharacter)
	{
		bool merchantKind = !hasAnyHero && targetCharacter != null && RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var _);
		string runtime = (hasAnyHero || merchantKind) ? AIConfigHandler.BuildRuntimeLoanInstructionForExternal(targetHero, targetCharacter) : null;
		return PromptRuleInstructionComposer.ResolveLoanInstruction(hasAnyHero, merchantKind, runtime, AIConfigHandler.LoanNonHeroInstruction, AIConfigHandler.LoanInstruction);
	}
internal static string BuildRecentNpcFactContext(Func<Hero,List<DialogueDay>> loadHistory, Hero hero, int maxLines = 4)
	{
		if (hero == null)
		{
			return "";
		}
		try
		{
			List<DialogueDay> list = loadHistory(hero);
			if (list == null || list.Count == 0)
			{
				return "";
			}
			List<string> list2 = new List<string>();
			foreach (DialogueDay item in list)
			{
				if (item?.Lines == null)
				{
					continue;
				}
				foreach (string line in item.Lines)
				{
					string text = (line ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text) && text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
					{
						list2.Add(text);
					}
				}
			}
			if (list2.Count <= 0)
			{
				return "";
			}
			int num = Math.Max(1, maxLines);
			if (list2.Count > num)
			{
				list2 = list2.Skip(list2.Count - num).ToList();
			}
			return string.Join("\n", list2);
		}
		catch
		{
			return "";
		}
	}
}
