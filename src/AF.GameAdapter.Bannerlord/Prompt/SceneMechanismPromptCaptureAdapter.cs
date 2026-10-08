using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Siege;
using System;
using AnimusForge.Refactor.Modules;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using TaleWorlds.MountAndBlade;
using SceneSummonPromptTarget = AnimusForge.ShoutBehavior.SceneSummonPromptTarget;
using SceneGuidePromptTarget = AnimusForge.ShoutBehavior.SceneGuidePromptTarget;
namespace AnimusForge.Refactor.Adapters;
internal sealed class SceneMechanismPromptCaptureAdapter
{
    private readonly Func<int,bool> _hasSummonSession;
    private readonly Func<Agent,bool> _isFollowing;
    private const string AutoGroupRelayRuleId = "scene_auto_group_relay";
    private readonly Func<List<NpcDataPacket>,string> _summonClosure;
    private readonly Func<NpcDataPacket,string> _followControl;
    internal SceneMechanismPromptCaptureAdapter(Func<int,bool> hasSummonSession, Func<Agent,bool> isFollowing,
        Func<List<NpcDataPacket>,string> summonClosure, Func<NpcDataPacket,string> followControl)
    {
        _hasSummonSession=hasSummonSession ?? throw new ArgumentNullException(nameof(hasSummonSession));
        _isFollowing=isFollowing ?? throw new ArgumentNullException(nameof(isFollowing));
        _summonClosure=summonClosure ?? throw new ArgumentNullException(nameof(summonClosure));
        _followControl=followControl ?? throw new ArgumentNullException(nameof(followControl));
    }
	internal List<PostprocessRuleEntry> BuildRuntimeSceneMechanismPostprocessRulesForScene(NpcDataPacket speaker, List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets, bool includeGenericRules = true)
	{
		List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
		int num = speaker?.AgentIndex ?? (-1);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string inspectionSlaughterRule = TroopInspectionBehavior.BuildPrisonerSlaughterPostprocessRuleForExternal(num);
		if (!string.IsNullOrWhiteSpace(inspectionSlaughterRule))
		{
			AddSceneMechanismPostprocessRule(
				list,
				hashSet,
				TroopInspectionPrisonerSlaughterProfile.ActionTag,
				inspectionSlaughterRule);
		}
		string noblePrisonerExecutionRule = NoblePrisonerEscortBehavior.BuildSceneExecutionPostprocessRule(num);
		if (!string.IsNullOrWhiteSpace(noblePrisonerExecutionRule))
		{
			AddSceneMechanismPostprocessRule(
				list,
				hashSet,
				NoblePrisonerEscortBehavior.ExecuteActionTag,
				noblePrisonerExecutionRule);
		}
		if (!includeGenericRules || AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission())
		{
			return list;
		}
		Agent agent = (num >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == num) : null;
		bool setsSceneActive = SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementEntryActiveForExternal(Mission.Current);
		SetsSettlementSceneKind sceneKind = SettlementEntryTroopSelectionBehavior.GetSetsSettlementSceneKindForExternal(Mission.Current);
		bool setsMassacreActive = setsSceneActive && SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementMassacreActiveForExternal(Mission.Current);
		bool speakerIsSelectedFollower = SettlementEntryTroopSelectionBehavior.IsSetsSelectedFollowerAgentForExternal(agent);
		bool anyMassacreRequestPending = setsSceneActive && SettlementEntryTroopSelectionBehavior.HasOwnedOrAttachedSettlementMassacreRequestForExternal(Mission.Current);
		bool massacreRequestPendingForSpeaker = anyMassacreRequestPending && SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementMassacreRequestPendingForExternal(Mission.Current, num);
		if (SetsOwnedSettlementMassacreProfile.ShouldOfferStopRule(setsSceneActive, setsMassacreActive, speakerIsSelectedFollower))
		{
			AddSceneMechanismPostprocessRule(list, hashSet, SetsOwnedSettlementMassacreProfile.StopActionTag, SetsOwnedSettlementMassacreProfile.BuildStopRuleDescription());
		}
		if (setsMassacreActive)
		{
			return list;
		}
		if (SetsOwnedSettlementMassacreProfile.ShouldOfferRequestRule(setsSceneActive, setsMassacreActive, anyMassacreRequestPending, speakerIsSelectedFollower))
		{
			AddSceneMechanismPostprocessRule(list, hashSet, SetsOwnedSettlementMassacreProfile.RequestActionTag, SetsOwnedSettlementMassacreProfile.BuildRequestRuleDescription(sceneKind));
		}
		if (SetsOwnedSettlementMassacreProfile.ShouldOfferStartRule(setsSceneActive, setsMassacreActive, speakerIsSelectedFollower))
		{
			AddSceneMechanismPostprocessRule(list, hashSet, SetsOwnedSettlementMassacreProfile.StartActionTag, SetsOwnedSettlementMassacreProfile.BuildStartRuleDescription(sceneKind, massacreRequestPendingForSpeaker));
		}
		if (SetsOwnedSettlementMassacreProfile.ShouldOfferCancelRequestRule(setsSceneActive, setsMassacreActive, massacreRequestPendingForSpeaker, speakerIsSelectedFollower))
		{
			AddSceneMechanismPostprocessRule(list, hashSet, SetsOwnedSettlementMassacreProfile.CancelRequestActionTag, SetsOwnedSettlementMassacreProfile.BuildCancelRequestRuleDescription());
		}
		List<PostprocessRuleEntry> guardrailRulePostprocessRules = AIConfigHandler.GetGuardrailRulePostprocessRules("scene_mechanism_actions") ?? new List<PostprocessRuleEntry>();
		if (guardrailRulePostprocessRules.Count == 0)
		{
			return list;
		}
		bool flag = num >= 0 && _hasSummonSession(num);
		bool flag2 = agent != null && _isFollowing(agent);
		if (SceneMovementController.IsPrisonBreakRescueMissionActive())
		{
			if (!SceneMovementController.IsPrisonBreakRescuePrisonerAgent(agent))
			{
				return list;
			}
			foreach (PostprocessRuleEntry guardrailRulePostprocessRule in guardrailRulePostprocessRules)
			{
				string text = (guardrailRulePostprocessRule?.Tag ?? "").Trim();
				string text2 = guardrailRulePostprocessRule?.Description ?? "";
				if (string.Equals(text, "[ACTION:SCENE_FOLLOW_PLAYER]", StringComparison.OrdinalIgnoreCase))
				{
					if (!flag2)
					{
						AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
					}
					continue;
				}
				if (string.Equals(text, "[ACTION:SCENE_STOP_FOLLOW]", StringComparison.OrdinalIgnoreCase) && flag2)
				{
					AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
				}
			}
			return list;
		}
		foreach (PostprocessRuleEntry guardrailRulePostprocessRule in guardrailRulePostprocessRules)
		{
			string text = (guardrailRulePostprocessRule?.Tag ?? "").Trim();
			string text2 = guardrailRulePostprocessRule?.Description ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (string.Equals(text, "[ACTION:SCENE_FOLLOW_PLAYER]", StringComparison.OrdinalIgnoreCase))
			{
				if (!flag2)
				{
					AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
				}
				continue;
			}
			if (string.Equals(text, "[ACTION:SCENE_STOP_FOLLOW]", StringComparison.OrdinalIgnoreCase))
			{
				if (flag2)
				{
					AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
				}
				continue;
			}
			if (string.Equals(text, "[END]", StringComparison.OrdinalIgnoreCase))
			{
				if (flag)
				{
					AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
				}
				continue;
			}
			if (text.StartsWith("[ACTION:SCENE_SUMMON:", StringComparison.OrdinalIgnoreCase))
			{
				if ((summonTargets?.Count).GetValueOrDefault() > 0)
				{
					AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
				}
				continue;
			}
			if (!text.StartsWith("[ACTION:SCENE_GUIDE:", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if ((guideTargets?.Count).GetValueOrDefault() > 0 || (summonTargets?.Count).GetValueOrDefault() > 0)
			{
				AddSceneMechanismPostprocessRule(list, hashSet, text, text2);
			}
		}
		return list;
	}
	internal static void AddSceneMechanismPostprocessRule(List<PostprocessRuleEntry> target, HashSet<string> seenTags, string tag, string description)
	{
		if (target == null || seenTags == null)
		{
			return;
		}
		string text = (tag ?? "").Trim();
		string text2 = description ?? "";
		if (!string.IsNullOrWhiteSpace(text) && seenTags.Add(text))
		{
			target.Add(new PostprocessRuleEntry
			{
				Tag = text,
				Description = text2
			});
		}
	}

internal static void AppendSceneUnifiedTargetPromptSection(StringBuilder prompt, List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets)
	{
		if (prompt == null)
		{
			return;
		}
		List<string> list = new List<string>();
		HashSet<int> hashSet = new HashSet<int>();
		foreach (SceneSummonPromptTarget item in summonTargets ?? new List<SceneSummonPromptTarget>())
		{
			if (item != null && item.PromptId > 0 && !string.IsNullOrWhiteSpace(item.DisplayName) && hashSet.Add(item.PromptId))
			{
				list.Add(item.PromptId + " " + item.DisplayName.Trim() + " " + (item.LocationCode ?? "处"));
			}
		}
		foreach (SceneGuidePromptTarget item2 in guideTargets ?? new List<SceneGuidePromptTarget>())
		{
			if (item2 != null && item2.PromptId > 0 && !string.IsNullOrWhiteSpace(item2.DisplayName) && hashSet.Add(item2.PromptId))
			{
				list.Add(item2.PromptId + " " + item2.DisplayName.Trim() + " " + (item2.LocationCode ?? "处"));
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		prompt.AppendLine("【带路与传唤NPC清单】：");
		foreach (string item3 in list)
		{
			prompt.AppendLine(item3);
		}
	}
internal static string BuildSceneMechanismPromptSection(List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, string sceneSummonClosureInstruction = null, string sceneFollowControlInstruction = null, NpcDataPacket publicActionTarget = null)
	{
		string executionInstruction = publicActionTarget == null
			? string.Empty
			: NoblePrisonerEscortBehavior.BuildSceneExecutionPromptInstruction(publicActionTarget.AgentIndex);
		string inspectionSlaughterInstruction = publicActionTarget == null
			? string.Empty
			: TroopInspectionBehavior.BuildPrisonerSlaughterPromptInstructionForExternal(publicActionTarget.AgentIndex);
		if (AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission())
		{
			return string.Join(
				"\n",
				new[]
				{
					executionInstruction,
					inspectionSlaughterInstruction
				}.Where(text => !string.IsNullOrWhiteSpace(text))).Trim();
		}
		if (SceneMovementController.IsPrisonBreakRescueMissionActive())
		{
			string prisonBreakInstruction = string.IsNullOrWhiteSpace(sceneFollowControlInstruction) ? "" : sceneFollowControlInstruction.Trim();
			return string.IsNullOrWhiteSpace(executionInstruction)
				? prisonBreakInstruction
				: (prisonBreakInstruction + "\n" + executionInstruction).Trim();
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementEntryActiveForExternal(Mission.Current))
		{
			SetsSettlementSceneKind sceneKind = SettlementEntryTroopSelectionBehavior.GetSetsSettlementSceneKindForExternal(Mission.Current);
			bool massacreActive = SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementMassacreActiveForExternal(Mission.Current);
			bool requestPending = SettlementEntryTroopSelectionBehavior.HasOwnedOrAttachedSettlementMassacreRequestForExternal(Mission.Current);
			stringBuilder.AppendLine(SetsOwnedSettlementMassacreProfile.BuildRuntimeInstruction(sceneKind, massacreActive, requestPending));
		}
		AppendSceneUnifiedTargetPromptSection(stringBuilder, sceneSummonTargets, sceneGuideTargets);
		if (!string.IsNullOrWhiteSpace(sceneSummonClosureInstruction))
		{
			stringBuilder.AppendLine(sceneSummonClosureInstruction);
		}
		if (!string.IsNullOrWhiteSpace(sceneFollowControlInstruction))
		{
			stringBuilder.AppendLine(sceneFollowControlInstruction);
		}
		if (!string.IsNullOrWhiteSpace(executionInstruction))
		{
			stringBuilder.AppendLine(executionInstruction);
		}
		if (!string.IsNullOrWhiteSpace(inspectionSlaughterInstruction))
		{
			stringBuilder.AppendLine(inspectionSlaughterInstruction);
		}
		return stringBuilder.ToString().Trim();
	}
internal static bool ShouldExcludeWorldMapCommandTopicForPreprocess(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex = -1)
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
internal bool CanInjectSceneMechanismTopicIntoPreprocess(List<SceneSummonPromptTarget> sceneSummonTargets, List<SceneGuidePromptTarget> sceneGuideTargets, NpcDataPacket sceneSpeakerNpc, List<NpcDataPacket> sceneCandidates)
	{
		try
		{
			if (Mission.Current == null)
			{
				return false;
			}
			string sceneSummonClosureInstruction = "";
			if (sceneCandidates != null && sceneCandidates.Count > 0)
			{
				sceneSummonClosureInstruction = _summonClosure(sceneCandidates);
			}
			else if (sceneSpeakerNpc != null)
			{
				sceneSummonClosureInstruction = _summonClosure(new List<NpcDataPacket> { sceneSpeakerNpc });
			}
			string sceneFollowControlInstruction = _followControl(sceneSpeakerNpc);
			string section = BuildSceneMechanismPromptSection(sceneSummonTargets, sceneGuideTargets, sceneSummonClosureInstruction, sceneFollowControlInstruction, sceneSpeakerNpc);
			return !string.IsNullOrWhiteSpace(section);
		}
		catch
		{
			return false;
		}
	}
internal bool CanInjectRuleTopicIntoPreprocessForCurrentInteraction(string ruleId, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, bool hasAnyHero, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, NpcDataPacket sceneSpeakerNpc = null, List<NpcDataPacket> sceneCandidates = null)
	{
		string id = (ruleId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(id))
		{
			return false;
		}
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		if (!AIConfigHandler.IsGuardrailRuleAvailableToPreprocessForExternal(id, hasAnyHero))
		{
			return false;
		}
		if (NoblePrisonerEscortBehavior.IsEscortedAgent(targetAgentIndex))
		{
			return true;
		}
		if (AIConfigHandler.IsPlayerPartyTradeLimitedTarget(hero) && (id == "loan" || id == "kingdom_agenda" || id == "diplomacy" || id == "party_transfer"))
		{
			return false;
		}
		if (id == AutoGroupRelayRuleId)
		{
			return false;
		}
		switch (id)
		{
		case "scene_mechanism_actions":
			return CanInjectSceneMechanismTopicIntoPreprocess(sceneSummonTargets, sceneGuideTargets, sceneSpeakerNpc, sceneCandidates);
		case "worldmap_party_command":
			return !ShouldExcludeWorldMapCommandTopicForPreprocess(hero, targetCharacter, targetAgentIndex);
		case "party_transfer":
			return (PartyAssetTransferBannerlordAdapter.BuildPartyTransferPromptEntriesForExternal(hero, targetCharacter, targetAgentIndex)?.Count ?? 0) > 0;
		case "encounter_release_player":
			return !string.IsNullOrWhiteSpace(LordEncounterBehavior.BuildMeetingPlayerReleaseRuntimeInstructionForExternal(hero));
		case "meeting_taunt":
			PartyBase meetingTauntParty = null;
			if (hero == null)
			{
				ConversationActionBoundaryBannerlordAdapter.TryResolveNativeConversationMeetingTauntParty(targetHero, targetCharacter, targetAgentIndex, out meetingTauntParty);
			}
			return !string.IsNullOrWhiteSpace(LordEncounterBehavior.BuildMeetingTauntRuntimeInstructionForExternal(hero, targetCharacter, meetingTauntParty))
				|| !string.IsNullOrWhiteSpace(SceneTauntBehavior.BuildUnifiedTauntRuntimeInstructionForExternal(hero, targetCharacter, targetAgentIndex));
		default:
			return true;
		}
	}
internal List<string> BuildPreprocessExcludedRuleIdsForCurrentInteraction(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, bool hasAnyHero, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, NpcDataPacket sceneSpeakerNpc = null, List<NpcDataPacket> sceneCandidates = null, string currentPlayerText = null)
	{
		List<string> allRuleIds = AIConfigHandler.GetConfiguredEnabledGuardrailRuleIdsForExternal();
		if (AfGcczShoutBridge.ShouldUseExclusivePreprocessRuleRouting(targetAgentIndex))
		{
			return AfGcczShoutBridge.BuildRuntimePreprocessRuleExclusions(allRuleIds, targetAgentIndex);
		}
		if (allRuleIds == null || allRuleIds.Count == 0)
		{
			return new List<string>();
		}
		HashSet<string> excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Agent agent = (targetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex) : null;
		string targetKingdomId = SceneAgentIdentityPromptCaptureAdapter.TryGetKingdomIdOverrideFromAgent(agent);
		string targetHeroId = (targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "").Trim();
		string targetCharacterId = (targetCharacter?.StringId ?? "").Trim();
		using IDisposable guardrailScopeJ03 = AIConfigHandler.BeginGuardrailRuntimeScope();
		try
		{
			PromptRuntimeTargetBinding runtimeTargetBinding = SharedPromptCaptureBannerlordAdapter.CreatePromptRuntimeTargetBinding(targetKingdomId, targetHero, targetCharacter, targetAgentIndex);
			AIConfigHandler.ApplyGuardrailRuntimeTarget(runtimeTargetBinding, AIConfigHandler.CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTargetBinding));
			foreach (string ruleId in allRuleIds)
			{
				string id = (ruleId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(id))
				{
					continue;
				}
				if (!CanInjectRuleTopicIntoPreprocessForCurrentInteraction(id, targetHero, targetCharacter, targetAgentIndex, hasAnyHero, sceneSummonTargets, sceneGuideTargets, sceneSpeakerNpc, sceneCandidates))
				{
					excluded.Add(id);
				}
			}
		}
		finally
		{
			AIConfigHandler.ClearGuardrailRuntimeTarget();
		}
		return excluded.ToList();
	}

internal static bool CanInjectDuelPostprocessRule(MyBehavior.ShoutPromptContext ctx, Hero targetHero, int targetAgentIndex, string playerText, out string reason)
	{
		reason = "";
		try
		{
			Agent agent = null;
			var agents = Mission.Current?.Agents;
			if (agents != null)
			{
				if (targetAgentIndex >= 0)
				{
					agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
				}
				if (agent == null)
				{
					agent = agents.FirstOrDefault((Agent a) => a != null && targetHero != null && a.Character == targetHero.CharacterObject);
				}
			}
			if (targetHero == null)
			{
				if (agent == null)
				{
					return true;
				}
				try
				{
					if (!agent.IsActive() || agent.IsMainAgent || agent == Agent.Main || !(agent.Character is CharacterObject))
					{
						reason = "target_agent_invalid";
						return false;
					}
				}
				catch
				{
					reason = "target_agent_invalid";
					return false;
				}
			}
		}
		catch
		{
		}
		return true;
	}
internal static bool IsNpcSurrenderPostprocessContext()
	{
		bool flag = false;
		try
		{
			flag = Campaign.Current?.CurrentConversationContext == ConversationContext.PartyEncounter;
		}
		catch
		{
		}
		try
		{
			flag = flag || LordEncounterBehavior.IsEncounterMeetingMissionActive;
		}
		catch
		{
		}
		try
		{
			flag = flag || MeetingBattleRuntime.IsMeetingActive;
		}
		catch
		{
		}
		if (!flag)
		{
			return false;
		}
		try
		{
			if (MeetingBattleRuntime.IsCombatEscalated)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Settlement.CurrentSettlement != null)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (MobileParty.MainParty?.CurrentSettlement != null)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}
internal static bool IsActiveSiegeSettlementForSurrender(Settlement settlement)
	{
		try
		{
			if (settlement == null || !settlement.IsFortification)
			{
				return false;
			}
			if (settlement.IsUnderSiege || settlement.SiegeEvent != null)
			{
				return true;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			return PlayerSiege.PlayerSiegeEvent?.BesiegedSettlement == settlement;
		}
		catch
		{
			return false;
		}
	}
internal static Settlement ResolveSiegeEventSettlementForSurrender(SiegeEvent siegeEvent)
	{
		if (siegeEvent == null)
		{
			return null;
		}
		try
		{
			Settlement settlement = siegeEvent.BesiegedSettlement;
			if (IsActiveSiegeSettlementForSurrender(settlement))
			{
				return settlement;
			}
		}
		catch
		{
		}
		try
		{
			return Settlement.All?.FirstOrDefault((Settlement x) => x != null && x.SiegeEvent == siegeEvent && IsActiveSiegeSettlementForSurrender(x));
		}
		catch
		{
			return null;
		}
	}
internal static void AddSiegeSettlementCandidate(List<Settlement> settlements, Settlement settlement)
	{
		if (settlements == null || !IsActiveSiegeSettlementForSurrender(settlement))
		{
			return;
		}
		if (!settlements.Any((Settlement x) => x == settlement))
		{
			settlements.Add(settlement);
		}
	}
internal static void AddSiegePartyCandidate(List<PartyBase> parties, PartyBase party)
	{
		if (parties == null || party == null)
		{
			return;
		}
		if (!parties.Any((PartyBase x) => x == party))
		{
			parties.Add(party);
		}
	}
internal static PartyBase ResolveNativeConversationAgentPartyForSiegeSurrender(int targetAgentIndex)
	{
		try
		{
			if (targetAgentIndex < 0)
			{
				return null;
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
			return agent?.Origin?.BattleCombatant as PartyBase;
		}
		catch
		{
			return null;
		}
	}
internal static List<PartyBase> ResolveNativeConversationPartiesForSiegeSurrender(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		List<PartyBase> list = new List<PartyBase>();
		AddSiegePartyCandidate(list, ResolveNativeConversationAgentPartyForSiegeSurrender(targetAgentIndex));
		try
		{
			AddSiegePartyCandidate(list, targetHero?.PartyBelongedTo?.Party);
		}
		catch
		{
		}
		try
		{
			AddSiegePartyCandidate(list, targetCharacter?.HeroObject?.PartyBelongedTo?.Party);
		}
		catch
		{
		}
		try
		{
			AddSiegePartyCandidate(list, MobileParty.ConversationParty?.Party);
		}
		catch
		{
		}
		try
		{
			AddSiegePartyCandidate(list, PlayerEncounter.EncounteredParty);
		}
		catch
		{
		}
		return list;
	}
internal static Settlement ResolveActiveSiegeSettlementForNativeConversation(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		List<Settlement> list = new List<Settlement>();
		try
		{
			AddSiegeSettlementCandidate(list, PlayerSiege.PlayerSiegeEvent?.BesiegedSettlement);
		}
		catch
		{
		}
		try
		{
			AddSiegeSettlementCandidate(list, PlayerEncounter.EncounterSettlement);
		}
		catch
		{
		}
		try
		{
			AddSiegeSettlementCandidate(list, Settlement.CurrentSettlement);
		}
		catch
		{
		}
		try
		{
			AddSiegeSettlementCandidate(list, MobileParty.MainParty?.CurrentSettlement);
			AddSiegeSettlementCandidate(list, MobileParty.MainParty?.BesiegedSettlement);
			AddSiegeSettlementCandidate(list, ResolveSiegeEventSettlementForSurrender(MobileParty.MainParty?.SiegeEvent));
		}
		catch
		{
		}
		targetHero = targetHero ?? targetCharacter?.HeroObject;
		try
		{
			AddSiegeSettlementCandidate(list, targetHero?.CurrentSettlement);
			AddSiegeSettlementCandidate(list, targetHero?.PartyBelongedTo?.CurrentSettlement);
			AddSiegeSettlementCandidate(list, targetHero?.PartyBelongedTo?.BesiegedSettlement);
			AddSiegeSettlementCandidate(list, ResolveSiegeEventSettlementForSurrender(targetHero?.PartyBelongedTo?.SiegeEvent));
		}
		catch
		{
		}
		foreach (PartyBase party in ResolveNativeConversationPartiesForSiegeSurrender(targetHero, targetCharacter, targetAgentIndex))
		{
			try
			{
				AddSiegeSettlementCandidate(list, party.Settlement);
			}
			catch
			{
			}
			try
			{
				AddSiegeSettlementCandidate(list, party.MobileParty?.CurrentSettlement);
				AddSiegeSettlementCandidate(list, party.MobileParty?.BesiegedSettlement);
				AddSiegeSettlementCandidate(list, party.MobileParty?.TargetSettlement);
				AddSiegeSettlementCandidate(list, party.MobileParty?.ShortTermTargetSettlement);
				AddSiegeSettlementCandidate(list, ResolveSiegeEventSettlementForSurrender(party.MobileParty?.SiegeEvent));
			}
			catch
			{
			}
			try
			{
				AddSiegeSettlementCandidate(list, party.MapEvent?.MapEventSettlement);
			}
			catch
			{
			}
			try
			{
				AddSiegeSettlementCandidate(list, ResolveSiegeEventSettlementForSurrender(party.SiegeEvent));
			}
			catch
			{
			}
		}
		return list.FirstOrDefault();
	}
internal static IFaction ResolveSiegeSurrenderTargetFaction(Hero targetHero, CharacterObject targetCharacter, List<PartyBase> parties)
	{
		try
		{
			if (targetHero?.MapFaction != null)
			{
				return targetHero.MapFaction;
			}
		}
		catch
		{
		}
		try
		{
			if (targetCharacter?.HeroObject?.MapFaction != null)
			{
				return targetCharacter.HeroObject.MapFaction;
			}
		}
		catch
		{
		}
		foreach (PartyBase party in parties ?? new List<PartyBase>())
		{
			try
			{
				if (party?.MapFaction != null)
				{
					return party.MapFaction;
				}
			}
			catch
			{
			}
		}
		return null;
	}
internal static IFaction ResolveSiegeAttackerFaction(Settlement settlement)
	{
		try
		{
			if (settlement?.SiegeEvent?.BesiegerCamp?.MapFaction != null)
			{
				return settlement.SiegeEvent.BesiegerCamp.MapFaction;
			}
		}
		catch
		{
		}
		try
		{
			return settlement?.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction;
		}
		catch
		{
			return null;
		}
	}
internal static bool IsSiegeAttackerPartyForSurrender(Settlement settlement, PartyBase party)
	{
		if (!IsActiveSiegeSettlementForSurrender(settlement) || party == null)
		{
			return false;
		}
		try
		{
			if (settlement.SiegeEvent?.BesiegerCamp?.HasInvolvedPartyForEventType(party) == true)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			MobileParty mobileParty = party.MobileParty;
			if (mobileParty != null && settlement.SiegeEvent?.BesiegerCamp?.IsBesiegerSideParty(mobileParty) == true)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			MobileParty mobileParty2 = party.MobileParty;
			if (mobileParty2 != null && (mobileParty2.BesiegedSettlement == settlement || mobileParty2.SiegeEvent == settlement.SiegeEvent))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}
internal static bool IsSiegeDefenderPartyForSurrender(Settlement settlement, PartyBase party)
	{
		if (!IsActiveSiegeSettlementForSurrender(settlement) || party == null)
		{
			return false;
		}
		try
		{
			if (party.IsSettlement && party.Settlement == settlement)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (settlement.HasInvolvedPartyForEventType(party))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (party.MobileParty?.CurrentSettlement == settlement && !IsSiegeAttackerPartyForSurrender(settlement, party))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}
internal static bool TryResolveSiegeSurrenderSide(Settlement settlement, Hero targetHero, CharacterObject targetCharacter, List<PartyBase> parties, out BattleSideEnum side)
	{
		side = BattleSideEnum.None;
		if (!IsActiveSiegeSettlementForSurrender(settlement))
		{
			return false;
		}
		foreach (PartyBase party in parties ?? new List<PartyBase>())
		{
			if (IsSiegeAttackerPartyForSurrender(settlement, party))
			{
				side = BattleSideEnum.Attacker;
				return true;
			}
			if (IsSiegeDefenderPartyForSurrender(settlement, party))
			{
				side = BattleSideEnum.Defender;
				return true;
			}
		}
		targetHero = targetHero ?? targetCharacter?.HeroObject;
		try
		{
			if (targetHero?.CurrentSettlement == settlement)
			{
				side = BattleSideEnum.Defender;
				return true;
			}
		}
		catch
		{
		}
		try
		{
			IFaction faction = ResolveSiegeSurrenderTargetFaction(targetHero, targetCharacter, parties);
			IFaction defenderFaction = settlement.MapFaction;
			IFaction attackerFaction = ResolveSiegeAttackerFaction(settlement);
			if (faction != null && attackerFaction != null && faction == attackerFaction)
			{
				side = BattleSideEnum.Attacker;
				return true;
			}
			if (faction != null && defenderFaction != null && faction == defenderFaction)
			{
				side = BattleSideEnum.Defender;
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (Settlement.CurrentSettlement == settlement || MobileParty.MainParty?.CurrentSettlement == settlement)
			{
				side = BattleSideEnum.Defender;
				return targetHero != null || targetCharacter != null;
			}
		}
		catch
		{
		}
		return false;
	}
internal static bool TryResolveNativeConversationSiegeSurrenderContext(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out Settlement settlement, out BattleSideEnum targetSide, out string sideLabel)
	{
		settlement = null;
		targetSide = BattleSideEnum.None;
		sideLabel = "";
		try
		{
			targetHero = targetHero ?? targetCharacter?.HeroObject;
			settlement = ResolveActiveSiegeSettlementForNativeConversation(targetHero, targetCharacter, targetAgentIndex);
			if (!IsActiveSiegeSettlementForSurrender(settlement))
			{
				return false;
			}
			List<PartyBase> parties = ResolveNativeConversationPartiesForSiegeSurrender(targetHero, targetCharacter, targetAgentIndex);
			if (!TryResolveSiegeSurrenderSide(settlement, targetHero, targetCharacter, parties, out targetSide))
			{
				return false;
			}
			sideLabel = targetSide == BattleSideEnum.Attacker ? "攻城方" : "守城方";
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[SiegeSurrender] context resolve failed: " + ex.Message);
			settlement = null;
			targetSide = BattleSideEnum.None;
			sideLabel = "";
			return false;
		}
	}
internal static int ResolvePartyTransferRecruitMaxTierForScene(Hero targetHero, CharacterObject targetCharacter)
	{
		int num = 6;
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null && RewardSystemBehavior.Instance != null)
			{
				num = RewardSystemBehavior.GetTrustLevelIndex(RewardSystemBehavior.Instance.GetEffectiveTrust(hero));
			}
		}
		catch
		{
		}
		if (num <= 4)
		{
			return 0;
		}
		if (num == 5)
		{
			return 1;
		}
		if (num == 6)
		{
			return 2;
		}
		if (num == 7)
		{
			return 3;
		}
		if (num == 8)
		{
			return 4;
		}
		return int.MaxValue;
	}
internal static string BuildCompactTownAmbientRuleText(
		IEnumerable<PostprocessRuleEntry> entries,
		TownPromptTextCatalog text)
	{
		StringBuilder result = new StringBuilder();
		foreach (PostprocessRuleEntry entry in entries ?? Enumerable.Empty<PostprocessRuleEntry>())
		{
			string tag = (entry?.Tag ?? string.Empty).Trim();
			if (tag.Length == 0)
			{
				continue;
			}

			result.Append(tag);
			IReadOnlyList<TownAmbientReactionActionKind> kinds = TownAmbientReactionTagCatalog.ExtractKinds(tag);
			if (kinds.Count == 1
				&& TownAmbientReactionTagCatalog.TryGetSuggestedAction(
					kinds[0],
					out SiegeInterventionActionKind suggestedAction))
			{
				result.Append(": ").Append(text.GetSuggestionActionLabel(suggestedAction));
			}
			else if (!string.IsNullOrWhiteSpace(entry.Description))
			{
				result.Append(": ").Append(entry.Description.Trim());
			}
			result.AppendLine();
		}
		return result.ToString().TrimEnd();
	}
internal static string EnsureScenePostprocessFallbackMood(string dialogue)
	{
		string normalizedDialogue = (dialogue ?? string.Empty).Trim();
		if (Regex.Matches(normalizedDialogue, "\\[ACTION:MOOD:[^\\]]+\\]", RegexOptions.IgnoreCase).Count > 0
			|| string.IsNullOrWhiteSpace(AIConfigHandler.ActionPostprocessFallbackMoodTag))
		{
			return normalizedDialogue;
		}
		return (normalizedDialogue + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim();
	}
internal static string InsertTaggedLatestReplyAfterScenePublicSection(string historyText, string taggedLatestReplyBlock)
	{
		string text = (historyText ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		string text2 = (taggedLatestReplyBlock ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return string.IsNullOrWhiteSpace(text) ? "（无）" : text;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		if (text.IndexOf("<latest_reply>", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return text;
		}
		string[] array = text.Split('\n');
		int num = -1;
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = (array[i] ?? "").Trim();
			if (text3.StartsWith("【当前场景公共对话与互动", StringComparison.Ordinal) && text3.EndsWith("】", StringComparison.Ordinal))
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			return (text + "\n" + text2).Trim();
		}
		int num2 = array.Length;
		for (int j = num + 1; j < array.Length; j++)
		{
			string text4 = (array[j] ?? "").Trim();
			if (text4.StartsWith("【", StringComparison.Ordinal) && text4.EndsWith("】", StringComparison.Ordinal))
			{
				num2 = j;
				break;
			}
		}
		List<string> list = new List<string>(array.Length + 1);
		for (int k = 0; k < num2; k++)
		{
			list.Add(array[k]);
		}
		list.Add(text2);
		for (int l = num2; l < array.Length; l++)
		{
			list.Add(array[l]);
		}
		return string.Join("\n", list).Trim();
	}
}
