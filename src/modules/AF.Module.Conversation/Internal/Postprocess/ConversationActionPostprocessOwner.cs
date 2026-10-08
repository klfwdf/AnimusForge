using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using RichExecutions.Core;
using RichExecutions.Scene;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

using static AnimusForge.ShoutBehavior;

namespace AnimusForge;

internal sealed class PostprocessNetworkRequest
{
 internal string SystemPrompt { get; }
 internal string UserPrompt { get; }
 internal PostprocessNetworkRequest(string system,string user) { SystemPrompt=system ?? ""; UserPrompt=user ?? ""; }
}

internal sealed class PostprocessSummonTarget
{
 internal int PromptId; internal string DisplayName; internal string LocationCode; internal bool HasLocationCharacter;
}
internal sealed class PostprocessGuideTarget
{
 internal int PromptId; internal string DisplayName; internal string LocationCode; internal bool HasLocationCharacter;
}
internal sealed class SceneActionPostprocessWorkItem
	{
		private int _completionStarted;

		public string SystemPrompt { get; }
		public string UserPrompt { get; }
		public string ReplyText { get; }
		public string ImmediateResult { get; }
		public Func<string, string> Normalize { get; }
		public bool RequiresNetwork => Normalize != null;
		internal PostprocessNetworkRequest NetworkRequest { get; }

		public bool TryBeginCompletion()
		{
			return Interlocked.CompareExchange(ref _completionStarted, 1, 0) == 0;
		}

		public SceneActionPostprocessWorkItem(string immediateResult)
		{
			ReplyText = immediateResult;
			ImmediateResult = immediateResult;
		}

		public SceneActionPostprocessWorkItem(string systemPrompt, string userPrompt, string replyText, Func<string, string> normalize)
		{
			SystemPrompt = systemPrompt;
			NetworkRequest = new PostprocessNetworkRequest(systemPrompt, userPrompt);
			UserPrompt = userPrompt;
			ReplyText = replyText;
			Normalize = normalize ?? throw new ArgumentNullException(nameof(normalize));
		}
	}
internal sealed class ConversationCourierPostprocessWorkItem
	{
		private Func<string, string> _completeOnMainThread;

		internal string SystemPrompt { get; }

		internal string UserPrompt { get; }

		internal string FallbackText { get; }
		internal PostprocessNetworkRequest NetworkRequest { get; }

		private readonly string _runtimeTargetKingdomId;

		private readonly string _runtimeTargetHeroId;

		private readonly string _runtimeTargetCharacterId;

		private readonly string _runtimeTargetTroopId;

		private readonly string _runtimeTargetUnnamedRank;

		private readonly int _runtimeTargetAgentIndex;

		internal ConversationCourierPostprocessWorkItem(string systemPrompt, string userPrompt, string fallbackText, string runtimeTargetKingdomId, string runtimeTargetHeroId, string runtimeTargetCharacterId, string runtimeTargetTroopId, string runtimeTargetUnnamedRank, int runtimeTargetAgentIndex, Func<string, string> completeOnMainThread)
		{
			SystemPrompt = systemPrompt ?? "";
			NetworkRequest = new PostprocessNetworkRequest(systemPrompt, userPrompt);
			UserPrompt = userPrompt ?? "";
			FallbackText = fallbackText ?? "";
			_runtimeTargetKingdomId = runtimeTargetKingdomId ?? "";
			_runtimeTargetHeroId = runtimeTargetHeroId ?? "";
			_runtimeTargetCharacterId = runtimeTargetCharacterId ?? "";
			_runtimeTargetTroopId = runtimeTargetTroopId ?? "";
			_runtimeTargetUnnamedRank = runtimeTargetUnnamedRank ?? "";
			_runtimeTargetAgentIndex = runtimeTargetAgentIndex;
			_completeOnMainThread = completeOnMainThread;
		}

		internal string CompleteOnMainThread(string content)
		{
			ConversationActionPostprocessOwner.RequireMainThread();
			Func<string, string> complete = Interlocked.Exchange(ref _completeOnMainThread, null);
			if (complete == null)
			{
				return FallbackText;
			}
			using IDisposable guardrailScopeJ03 = AIConfigHandler.BeginGuardrailRuntimeScope();
			try
			{
				AIConfigHandler.SetGuardrailRuntimeTargetKingdom(_runtimeTargetKingdomId);
				AIConfigHandler.SetGuardrailRuntimeTargetHero(_runtimeTargetHeroId);
				AIConfigHandler.SetGuardrailRuntimeTargetCharacter(_runtimeTargetCharacterId);
				AIConfigHandler.SetGuardrailRuntimeTargetTroop(_runtimeTargetTroopId);
				AIConfigHandler.SetGuardrailRuntimeTargetUnnamedRank(_runtimeTargetUnnamedRank);
				AIConfigHandler.SetGuardrailRuntimeTargetAgentIndex(_runtimeTargetAgentIndex);
				return complete(content ?? "");
			}
			catch (Exception ex)
			{
				Logger.Log("CourierDelivery", "[UnifiedPostprocess] exception: " + ex);
				return FallbackText;
			}
			finally
			{
				AIConfigHandler.ClearGuardrailRuntimeTarget();
			}
		}
	}

internal static class ConversationActionPostprocessOwner
{
	internal static bool HasDeferredDirectGameActionTag(string text)
	{
		if (CustomPolicyAgendaActionTagRegex.IsMatch(text ?? "") || GiveAssetTagCodec.Contains(text))
		{
			return true;
		}
		return Regex.IsMatch(GiveAssetTagCodec.StripTags(text), "\\[(?:ACTION:(?:PUBLIC_EXECUTION_START|GIVE_ASSET|KINGDOM_SERVICE|JOIN_MERCENARY|JOIN_VASSAL|TRADE_TRUST|KING_ABDICATE_TO_PLAYER|VASSALAGE|KINGDOM_ANNEX|AGENDA|WORLDMAP_ORDER|DUEL|ISSUE_|QUEST_TURN_IN|NOBLE_GATHERING|NOBLE_PRISONER_EXECUTE|NOBLE_EXECUTE_ESCORT|NOBLE_EXECUTE_PARTY_PRISONER|TROOP_INSPECTION_SLAUGHTER_PRISONERS|INTIMACY_INTERNAL|MEETING_TAUNT_BATTLE|LET_PLAYER_GO|ENCOUNTER_RELEASE_PLAYER|NPC_SURRENDER|SIEGE_|6|召集)[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*)\\]", RegexOptions.IgnoreCase);
	}
internal static string ExtractDeferredSceneActionTags(string text)
	{
		string text2 = (text ?? "").Replace("\r", "");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (GiveAssetTag giveAssetTag in GiveAssetTagCodec.Extract(text2))
		{
			if (!string.IsNullOrWhiteSpace(giveAssetTag.RawTag) && hashSet.Add(giveAssetTag.RawTag))
			{
				list.Add(giveAssetTag.RawTag);
			}
		}
		text2 = GiveAssetTagCodec.StripTags(text2);
		foreach (Match item in Regex.Matches(text2, "\\[(?:ACTION:[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|ATT:[^\\]]*|ATP:[^\\]]*|FOL|STP|END)\\]", RegexOptions.IgnoreCase))
		{
			string text3 = (item?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add(text3))
			{
				list.Add(text3);
			}
		}
		return string.Join(" ", list).Trim();
	}
internal static bool HasNonMoodDeferredSceneActionTag(string text)
	{
		if (GiveAssetTagCodec.Contains(text))
		{
			return true;
		}
		foreach (Match item in Regex.Matches(GiveAssetTagCodec.StripTags(text), "\\[(?:ACTION:[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|ATT:[^\\]]*|ATP:[^\\]]*|FOL|STP|END)\\]", RegexOptions.IgnoreCase))
		{
			string text2 = (item?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2) && !text2.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
internal static string StripDeferredSceneMoodTags(string text)
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (GiveAssetTag giveAssetTag in GiveAssetTagCodec.Extract(text))
		{
			if (!string.IsNullOrWhiteSpace(giveAssetTag.RawTag) && hashSet.Add(giveAssetTag.RawTag))
			{
				list.Add(giveAssetTag.RawTag);
			}
		}
		foreach (Match item in Regex.Matches(GiveAssetTagCodec.StripTags(text), "\\[(?:ACTION:[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|ATT:[^\\]]*|ATP:[^\\]]*|FOL|STP|END)\\]", RegexOptions.IgnoreCase))
		{
			string text2 = (item?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2) && !text2.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase) && hashSet.Add(text2))
			{
				list.Add(text2);
			}
		}
		return string.Join(" ", list).Trim();
	}

	internal static bool MayContainGeneratedRpItemReward(string responseText)
	{
		return !string.IsNullOrEmpty(responseText)
			&& responseText.IndexOf(GiveAssetTagCodec.Prefix, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	internal static string ResolveNativeConversationPostprocessChainName() => SceneAgentIdentityPromptCaptureAdapter.CaptureEncounterMeetingActive() ? "meeting" : "native_conversation";

	internal static string ResolveScenePostprocessChainName() => SceneAgentIdentityPromptCaptureAdapter.CaptureEncounterMeetingActive() ? "meeting" : "scene";

	internal static bool IsNativeConversationNoSpeechPlaceholder(string text)
	{
		string value = (text ?? "").Replace("\r", "").Trim();
		return string.Equals(value, "（没说话）", StringComparison.Ordinal)
			|| string.Equals(value, "(没说话)", StringComparison.Ordinal)
			|| string.Equals(value, "无", StringComparison.Ordinal)
			|| string.Equals(value, "无回信", StringComparison.Ordinal);
	}

	internal static string StripLordsHallAccessActionTagsForScene(string text)
	{
		string text2 = text ?? "";
		text2 = Regex.Replace(text2, "\\[ACTION:OPEN_LORDS_HALL\\]", "", RegexOptions.IgnoreCase);
		return text2.Trim();
	}

	internal static bool ContainsOpenLordsHallActionTag(string text)
	{
		return !string.IsNullOrWhiteSpace(text) && Regex.IsMatch(text, "\\[ACTION:OPEN_LORDS_HALL\\]", RegexOptions.IgnoreCase);
	}

	internal static string ExtractSceneMechanismActionTagsForScene(string text)
	{
		string text2 = (text ?? "").Replace("\r", "");
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match item in Regex.Matches(text2, "\\[(?:(?:ASS|ACTION:SCENE_SUMMON):[^\\]\\r\\n]+|(?:GUI|ACTION:SCENE_GUIDE):[^\\]\\r\\n]+|ACTION:(?:SETS_REQUEST_MASSACRE|SETS_START_MASSACRE|SETS_STOP_MASSACRE|SETS_CANCEL_MASSACRE_REQUEST)|FOL|ACTION:SCENE_FOLLOW_PLAYER|STP|ACTION:SCENE_STOP_FOLLOW|END)\\]", RegexOptions.IgnoreCase))
		{
			string text3 = (item?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3) && hashSet.Add(text3))
			{
				list.Add(text3);
			}
		}
		return string.Join(" ", list).Trim();
	}

	internal static void ApplyStageQualifications(int targetAgentIndex, ref bool duelRuleInjected, ref bool rewardRuleInjected, ref bool loanRuleInjected, ref bool persistentAdpDebtRuleInjected, ref bool kingdomServiceRuleInjected, ref bool kingdomVassalageRuleInjected, ref bool lordsHallRuleInjected, ref bool meetingReleaseRuleInjected, ref bool vanillaIssueRuleInjected, ref bool heroJoinPartyRuleInjected, ref bool sceneMechanismRuleInjected, ref bool partyTransferRuleInjected, ref bool voteDealRuleInjected, ref bool customPolicyAgendaRuleInjected, ref bool diplomacyRuleInjected, ref bool worldMapPartyCommandRuleInjected, ref bool nobleGatheringRuleInjected, ref bool marriageRuleInjected)
	{
		RequireMainThread();
		duelRuleInjected = duelRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.DuelRuleId, targetAgentIndex);
		rewardRuleInjected = rewardRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.RewardRuleId, targetAgentIndex);
		loanRuleInjected = loanRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.LoanRuleId, targetAgentIndex);
		persistentAdpDebtRuleInjected = persistentAdpDebtRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.PersistentDebtRuleId, targetAgentIndex);
		kingdomServiceRuleInjected = kingdomServiceRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.KingdomServiceRuleId, targetAgentIndex);
		kingdomVassalageRuleInjected = kingdomVassalageRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.KingdomVassalageRuleId, targetAgentIndex);
		lordsHallRuleInjected = lordsHallRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.LordsHallRuleId, targetAgentIndex);
		meetingReleaseRuleInjected = meetingReleaseRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.EncounterReleaseRuleId, targetAgentIndex);
		vanillaIssueRuleInjected = vanillaIssueRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.VanillaIssueRuleId, targetAgentIndex);
		heroJoinPartyRuleInjected = heroJoinPartyRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.KingdomServiceRuleId, targetAgentIndex);
		sceneMechanismRuleInjected = sceneMechanismRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.SceneMechanismRuleId, targetAgentIndex);
		partyTransferRuleInjected = partyTransferRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.PartyTransferRuleId, targetAgentIndex);
		voteDealRuleInjected = voteDealRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.KingdomAgendaRuleId, targetAgentIndex);
		customPolicyAgendaRuleInjected = customPolicyAgendaRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.KingdomAgendaRuleId, targetAgentIndex);
		diplomacyRuleInjected = diplomacyRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.DiplomacyRuleId, targetAgentIndex);
		worldMapPartyCommandRuleInjected = worldMapPartyCommandRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.WorldMapPartyCommandRuleId, targetAgentIndex);
		nobleGatheringRuleInjected = nobleGatheringRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.NobleGatheringRuleId, targetAgentIndex);
		marriageRuleInjected = marriageRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.MarriageRuleId, targetAgentIndex);
	}

 internal static void RequireMainThread()
 {
  if (!TaleWorlds.Library.TWParallel.IsMainThread()) throw new InvalidOperationException("postprocess.requires_main_thread");
 }
internal const string NpcSurrenderActionTag = "[ACTION:NPC_SURRENDER]";
internal const string SiegeSurrenderActionTag = NpcSurrenderActionTag;
internal const string CustomPolicyAgendaPostprocessRuleId = "kingdom_agenda";
internal const string CustomPolicyAgendaActionTag = KingdomAgendaCustomPolicyBehavior.ActionTag;
internal const string AutoGroupRelayRuleId = "scene_auto_group_relay";
internal const string AutoGroupRelayTagTemplate = "[RELAY:接力编号]";
internal static readonly Regex SetsOwnedSettlementMassacreRequestActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.RequestActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
internal static readonly Regex SetsOwnedSettlementMassacreStartActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.StartActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
internal static readonly Regex SetsOwnedSettlementMassacreStopActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.StopActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
internal static readonly Regex SetsOwnedSettlementMassacreCancelRequestActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.CancelRequestActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
internal static readonly Regex TroopInspectionPrisonerSlaughterActionTagRegex = new Regex(Regex.Escape(TroopInspectionPrisonerSlaughterProfile.ActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
internal static readonly Regex CustomPolicyAgendaActionTagRegex = new Regex(Regex.Escape(CustomPolicyAgendaActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
	internal static string TryRunSceneUnifiedActionPostprocess(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string npcName, string playerText, string historyText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool kingdomVassalageRuleInjected, bool kingdomAnnexationRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, bool voteDealRuleInjected, bool diplomacyRuleInjected, bool worldMapPartyCommandRuleInjected, bool marriageRuleInjected, List<RewardSystemBehavior.DuelStakeOption> duelStakeOptions, List<PostprocessRuleEntry> kingdomServiceRules, List<PostprocessRuleEntry> sceneMechanismRules, List<PostprocessSummonTarget> sceneSummonTargets, List<PostprocessGuideTarget> sceneGuideTargets, string entityPostprocessContext = null, bool siegeInterventionRuleInjected = false, bool replyIsDirectPlayerResponse = false, List<string> preprocessRuleHits = null, string chainName = null, bool relayRuleInjected = false, List<NpcDataPacket> relayCandidates = null, int relayPrimaryTargetAgentIndex = -1, bool relaySingleFramedNpc = false, bool customPolicyAgendaRuleInjected = false, DetachedPromptSections detachedMainPromptSections = null)
	{
		SceneActionPostprocessWorkItem workItem = PrepareSceneUnifiedActionPostprocess(targetHero, targetCharacter, targetAgentIndex, npcName, playerText, historyText, replyText, duelRuleInjected, rewardRuleInjected, loanRuleInjected, kingdomServiceRuleInjected, kingdomVassalageRuleInjected, kingdomAnnexationRuleInjected, lordsHallRuleInjected, meetingReleaseRuleInjected, vanillaIssueRuleInjected, heroJoinPartyRuleInjected, sceneMechanismRuleInjected, partyTransferRuleInjected, voteDealRuleInjected, diplomacyRuleInjected, worldMapPartyCommandRuleInjected, marriageRuleInjected, duelStakeOptions, kingdomServiceRules, sceneMechanismRules, sceneSummonTargets, sceneGuideTargets, entityPostprocessContext, siegeInterventionRuleInjected, replyIsDirectPlayerResponse, preprocessRuleHits, chainName, relayRuleInjected, relayCandidates, relayPrimaryTargetAgentIndex, relaySingleFramedNpc, customPolicyAgendaRuleInjected, detachedMainPromptSections);
		if (!workItem.RequiresNetwork)
		{
			return CompleteSceneUnifiedActionPostprocess(workItem, true, null, null);
		}
		bool succeeded = TryRequestSceneUnifiedActionPostprocess(workItem.SystemPrompt, workItem.UserPrompt, out string content, out string error);
		return CompleteSceneUnifiedActionPostprocess(workItem, succeeded, content, error);
	}

	internal static bool TryRequestSceneUnifiedActionPostprocess(string systemPrompt, string userPrompt, out string content, out string error)
	{
		return AIConfigHandler.TryCallAuxiliaryActionPostprocess(systemPrompt, userPrompt, 5000, 0f, out content, out error);
	}

	internal static string CompleteSceneUnifiedActionPostprocess(SceneActionPostprocessWorkItem workItem, bool succeeded, string content, string error)
	{
		RequireMainThread();
		if (workItem == null)
		{
			throw new ArgumentNullException(nameof(workItem));
		}
		if (!workItem.TryBeginCompletion())
		{
			throw new InvalidOperationException("Scene postprocess completion has already started; it must not be retried.");
		}
		if (!workItem.RequiresNetwork)
		{
			return workItem.ImmediateResult;
		}
		if (!succeeded)
		{
			Logger.Log("ShoutBehavior", "[UnifiedPostprocess] 调用失败: " + error);
			return (workItem.ReplyText + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim();
		}
		return workItem.Normalize(content);
	}

	internal static SceneActionPostprocessWorkItem PrepareSceneUnifiedActionPostprocess(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string npcName, string playerText, string historyText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool kingdomVassalageRuleInjected, bool kingdomAnnexationRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, bool voteDealRuleInjected, bool diplomacyRuleInjected, bool worldMapPartyCommandRuleInjected, bool marriageRuleInjected, List<RewardSystemBehavior.DuelStakeOption> duelStakeOptions, List<PostprocessRuleEntry> kingdomServiceRules, List<PostprocessRuleEntry> sceneMechanismRules, List<PostprocessSummonTarget> sceneSummonTargets, List<PostprocessGuideTarget> sceneGuideTargets, string entityPostprocessContext = null, bool siegeInterventionRuleInjected = false, bool replyIsDirectPlayerResponse = false, List<string> preprocessRuleHits = null, string chainName = null, bool relayRuleInjected = false, List<NpcDataPacket> relayCandidates = null, int relayPrimaryTargetAgentIndex = -1, bool relaySingleFramedNpc = false, bool customPolicyAgendaRuleInjected = false, DetachedPromptSections detachedMainPromptSections = null, bool offerSceneActionDirective = false)
	{
		RequireMainThread();
		string text = StripActionTagsForSceneSpeech(replyText ?? "");
		string resolvedChainName = string.IsNullOrWhiteSpace(chainName) ? ResolveScenePostprocessChainName() : chainName.Trim();
		bool executionTopic = HasPreprocessRuleHit(preprocessRuleHits, PublicExecutionOrderPolicy.RuleId);
        bool executionNative = resolvedChainName == "native_conversation" || resolvedChainName == "meeting";
        var executionPermit = executionTopic ? PublicExecutionOrderRuntime.Capture(targetAgentIndex, resolvedChainName,
            executionNative || (relaySingleFramedNpc && relayPrimaryTargetAgentIndex == targetAgentIndex),
            replyIsDirectPlayerResponse, playerText, replyText) : null;
        var executionRules = executionPermit == null ? null : AIConfigHandler.GetGuardrailRulePostprocessRules(PublicExecutionOrderPolicy.RuleId);
		bool kingdomVassalagePreprocessHit = HasPreprocessRuleHit(preprocessRuleHits, "kingdom_vassalage");
		bool nobleGatheringRuleInjected = HasPreprocessRuleHit(preprocessRuleHits, "noble_gathering");
		bool persistentAdpDebtRuleInjected = HasPreprocessRuleHit(preprocessRuleHits, PersistentAdpDebtPostprocessRuleId);
		bool customPolicyAgendaPreprocessHit = HasPreprocessRuleHit(preprocessRuleHits, CustomPolicyAgendaPostprocessRuleId);
		customPolicyAgendaRuleInjected = replyIsDirectPlayerResponse && (customPolicyAgendaRuleInjected || customPolicyAgendaPreprocessHit);
		if (customPolicyAgendaRuleInjected && !TeamModuleServices.Policy.IsEligibleTargetForExternal(targetHero ?? targetCharacter?.HeroObject, out var customPolicyAgendaBlockedReason))
		{
			customPolicyAgendaRuleInjected = false;
			Logger.Log("ShoutBehavior", "[CustomPolicyAgendaPostprocess] blocked chain=" + resolvedChainName + " target=" + (targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "null") + " reason=" + (customPolicyAgendaBlockedReason ?? ""));
		}
		kingdomVassalageRuleInjected = kingdomVassalageRuleInjected || kingdomVassalagePreprocessHit;
		bool royalPostprocessEligible = AIConfigHandler.IsRoyalAbdicationPostprocessTargetForExternal(targetHero ?? targetCharacter?.HeroObject);
		bool royalDiplomacyRequested = diplomacyRuleInjected || kingdomAnnexationRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "diplomacy");
		bool royalDiplomacyRuleInjected = royalDiplomacyRequested && DiplomacyConversationBridge.CanUseDiplomacyActionPostprocessForExternal(targetHero, targetCharacter);
		bool independentClanPeaceResident = replyIsDirectPlayerResponse && DiplomacyConversationBridge.CanUseIndependentClanPeaceForExternal(targetHero, targetCharacter);
		diplomacyRuleInjected = royalDiplomacyRuleInjected || independentClanPeaceResident;
		kingdomAnnexationRuleInjected = false;
		ApplyStageQualifications(targetAgentIndex, ref duelRuleInjected, ref rewardRuleInjected, ref loanRuleInjected, ref persistentAdpDebtRuleInjected, ref kingdomServiceRuleInjected, ref kingdomVassalageRuleInjected, ref lordsHallRuleInjected, ref meetingReleaseRuleInjected, ref vanillaIssueRuleInjected, ref heroJoinPartyRuleInjected, ref sceneMechanismRuleInjected, ref partyTransferRuleInjected, ref voteDealRuleInjected, ref customPolicyAgendaRuleInjected, ref diplomacyRuleInjected, ref worldMapPartyCommandRuleInjected, ref nobleGatheringRuleInjected, ref marriageRuleInjected);
		relayRuleInjected = relayRuleInjected && AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.SceneRelayRuleId, targetAgentIndex);
		Logger.Log("ShoutBehavior", "[UnifiedPostprocess] setup chain=" + resolvedChainName
			+ " preprocessHits=" + ((preprocessRuleHits == null || preprocessRuleHits.Count == 0) ? "(none)" : string.Join(",", preprocessRuleHits))
			+ " kingdom_vassalage_hit=" + kingdomVassalagePreprocessHit
			+ " kingdom_vassalage_injected=" + kingdomVassalageRuleInjected
			+ " kingdom_agenda_injected=" + customPolicyAgendaRuleInjected
			+ " diplomacy_injected=" + diplomacyRuleInjected
			+ " independent_clan_peace_resident=" + independentClanPeaceResident);
		if (kingdomVassalageRuleInjected)
		{
			VassalageDiagnosticLog.Event("postprocess.scene.start", new Dictionary<string, object>
			{
				["targetHero"] = VassalageDiagnosticLog.DescribeHero(targetHero),
				["targetCharacterId"] = targetCharacter?.StringId ?? "",
				["targetAgentIndex"] = targetAgentIndex,
				["npcName"] = npcName ?? "",
				["playerText"] = VassalageDiagnosticLog.Preview(playerText, 1000),
				["historyTextLen"] = (historyText ?? "").Length,
				["replyText"] = VassalageDiagnosticLog.Preview(replyText, 2000),
				["entityPostprocessContextLen"] = (entityPostprocessContext ?? "").Length,
				["preprocessHits"] = preprocessRuleHits
			});
		}
		bool excludeSceneMoveRule = AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission();
		bool inspectionSlaughterRuleAvailable = (sceneMechanismRules ?? new List<PostprocessRuleEntry>())
			.Any(rule => string.Equals(
				(rule?.Tag ?? "").Trim(),
				TroopInspectionPrisonerSlaughterProfile.ActionTag,
				StringComparison.OrdinalIgnoreCase));
		bool noblePrisonerExecutionRuleAvailable = (sceneMechanismRules ?? new List<PostprocessRuleEntry>())
			.Any(rule => string.Equals(
				(rule?.Tag ?? "").Trim(),
				NoblePrisonerEscortBehavior.ExecuteActionTag,
				StringComparison.OrdinalIgnoreCase));
		if (sceneMechanismRuleInjected || excludeSceneMoveRule)
		{
			text = StripSceneMechanismActionTagsForScene(text);
		}
		if (excludeSceneMoveRule && !inspectionSlaughterRuleAvailable && !noblePrisonerExecutionRuleAvailable)
		{
			sceneMechanismRuleInjected = false;
			sceneMechanismRules = null;
			sceneSummonTargets = null;
			sceneGuideTargets = null;
		}
		else if (excludeSceneMoveRule)
		{
			sceneMechanismRuleInjected = true;
			sceneMechanismRules = sceneMechanismRules
				.Where(rule =>
				{
					string tag = (rule?.Tag ?? "").Trim();
					return string.Equals(
						tag,
						TroopInspectionPrisonerSlaughterProfile.ActionTag,
						StringComparison.OrdinalIgnoreCase)
						|| string.Equals(
							tag,
							NoblePrisonerEscortBehavior.ExecuteActionTag,
							StringComparison.OrdinalIgnoreCase);
				})
				.ToList();
			sceneSummonTargets = null;
			sceneGuideTargets = null;
		}
		sceneMechanismRuleInjected = sceneMechanismRuleInjected
			&& AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.SceneMechanismRuleId, targetAgentIndex);
		if (!AIConfigHandler.CanUseAuxiliaryActionPostprocess())
		{
			if (Regex.Matches(text ?? "", "\\[ACTION:MOOD:[^\\]]+\\]", RegexOptions.IgnoreCase).Count <= 0 && !string.IsNullOrWhiteSpace(AIConfigHandler.ActionPostprocessFallbackMoodTag))
			{
				text = (text + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim();
			}
			return new SceneActionPostprocessWorkItem(text.Trim());
		}
		string actionPostprocessSystemPrompt = AIConfigHandler.ActionPostprocessSystemPrompt;
		string actionPostprocessUserPromptTemplate = AIConfigHandler.ActionPostprocessUserPromptTemplate;
		if (string.IsNullOrWhiteSpace(actionPostprocessSystemPrompt) || string.IsNullOrWhiteSpace(actionPostprocessUserPromptTemplate))
		{
			return new SceneActionPostprocessWorkItem((text + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim());
		}
		bool transactionPostprocessEnabled = rewardRuleInjected || loanRuleInjected || persistentAdpDebtRuleInjected;
		bool exposeAllRewardItems = rewardRuleInjected && RequestsAllOrdinaryAssetsForPostprocess(playerText, historyText);
		bool exposeAllFixedAssets = rewardRuleInjected && RequestsAllFixedAssetsForPostprocess(playerText, historyText);
		List<PostprocessRuleEntry> duelRules = duelRuleInjected ? AIConfigHandler.DuelPostprocessRules : null;
		List<PostprocessRuleEntry> persistentAdpDebtRules = persistentAdpDebtRuleInjected ? BuildPersistentAdpDebtPostprocessRules() : null;
		List<PostprocessRuleEntry> transactionRules = transactionPostprocessEnabled ? MergePostprocessRulesForScene(rewardRuleInjected ? AIConfigHandler.RewardPostprocessRules : null, loanRuleInjected ? AIConfigHandler.LoanPostprocessRules : null, persistentAdpDebtRules) : null;
		List<PostprocessRuleEntry> kingdomRules = null;
		if (kingdomServiceRuleInjected)
		{
			kingdomRules = MergePostprocessRulesForScene(kingdomServiceRules, AIConfigHandler.BuildRuntimeKingdomServicePostprocessRules() ?? new List<PostprocessRuleEntry>());
		}
		List<PostprocessRuleEntry> royalRules = AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.RoyalActionRuleId, targetAgentIndex)
			? (AIConfigHandler.BuildRuntimeRoyalPostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) ?? new List<PostprocessRuleEntry>())
			: new List<PostprocessRuleEntry>();
		List<PostprocessRuleEntry> vassalageRules = kingdomVassalageRuleInjected ? (VassalageBehavior.BuildRuntimeVassalagePostprocessRulesForExternal(targetHero, targetCharacter) ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> lordsHallRules = lordsHallRuleInjected ? MergePostprocessRulesForScene(AIConfigHandler.GetGuardrailRulePostprocessRules("lords_hall_access"), AIConfigHandler.BuildRuntimeLordsHallAccessPostprocessRules() ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> meetingReleaseRules = meetingReleaseRuleInjected ? MergePostprocessRulesForScene(AIConfigHandler.GetGuardrailRulePostprocessRules("encounter_release_player"), LordEncounterBehavior.BuildMeetingPlayerReleasePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject)) : null;
		List<PostprocessRuleEntry> vanillaIssueRules = vanillaIssueRuleInjected ? (VanillaIssueOfferBridge.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> heroJoinPartyRules = heroJoinPartyRuleInjected ? (AIConfigHandler.BuildRuntimeHeroJoinPartyPostprocessRules(!ShouldSuppressHeroJoinPartyPostprocessForScene(targetHero ?? targetCharacter?.HeroObject), targetHero ?? targetCharacter?.HeroObject, entityPostprocessContext) ?? new List<PostprocessRuleEntry>()) : null;
		if (heroJoinPartyRuleInjected && (heroJoinPartyRules == null || heroJoinPartyRules.Count == 0))
		{
			heroJoinPartyRuleInjected = false;
		}
		List<PostprocessRuleEntry> mechanismRules = sceneMechanismRuleInjected
			? (excludeSceneMoveRule
				? (sceneMechanismRules ?? new List<PostprocessRuleEntry>())
				: MergePostprocessRulesForScene(
					AIConfigHandler.GetGuardrailRulePostprocessRules("scene_mechanism_actions"),
					sceneMechanismRules ?? new List<PostprocessRuleEntry>()))
			: null;
		List<PostprocessRuleEntry> nobleExecutionOrderRules = replyIsDirectPlayerResponse
			? NoblePrisonerExecutionOrderBehavior.BuildRuntimePostprocessRules(
				targetHero ?? targetCharacter?.HeroObject,
				targetAgentIndex)
			: new List<PostprocessRuleEntry>();
		List<PostprocessRuleEntry> partyTransferRules = partyTransferRuleInjected ? (AIConfigHandler.GetGuardrailRulePostprocessRules("party_transfer") ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> voteDealRules = voteDealRuleInjected ? VoteDealBehavior.BuildAgendaVotePostprocessRulesForExternal() : null;
		List<PostprocessRuleEntry> customPolicyAgendaRules = customPolicyAgendaRuleInjected ? TeamModuleServices.Policy.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) : null;
		if (customPolicyAgendaRuleInjected && customPolicyAgendaRules.Count == 0)
		{
			customPolicyAgendaRuleInjected = false;
		}
		List<PostprocessRuleEntry> diplomacyRules = diplomacyRuleInjected ? BuildRuntimeDiplomacyPostprocessRulesForScene(targetHero, targetCharacter) : null;
		List<PostprocessRuleEntry> proposeAgendaRules = null;
		List<PostprocessRuleEntry> worldMapPartyCommandRules = worldMapPartyCommandRuleInjected ? (WorldMapPartyCommandBehavior.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject, targetCharacter, targetAgentIndex) ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> nobleGatheringRules = nobleGatheringRuleInjected ? (TeamModuleServices.Gathering.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) ?? new List<PostprocessRuleEntry>()) : null;
		Hero marriageSpeaker = targetHero ?? targetCharacter?.HeroObject;
		List<PostprocessRuleEntry> marriageRuntimeRules = marriageRuleInjected ? (RomanceSystemBehavior.Instance?.BuildRuntimeMarriagePostprocessRulesForExternal(marriageSpeaker) ?? new List<PostprocessRuleEntry>()) : null;
		List<PostprocessRuleEntry> marriageRules = marriageRuleInjected ? marriageRuntimeRules : null;
		Settlement siegeSurrenderSettlement = null;
		BattleSideEnum siegeSurrenderSide = BattleSideEnum.None;
		string siegeSurrenderSideLabel = "";
		bool allowEncounterSurrender = AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.EncounterSurrenderRuleId, targetAgentIndex);
		bool siegeSurrenderPostprocessEnabled = allowEncounterSurrender
			&& IsNativeConversationPostprocessChain(resolvedChainName)
			&& TryResolveNativeConversationSiegeSurrenderContext(targetHero, targetCharacter, targetAgentIndex, out siegeSurrenderSettlement, out siegeSurrenderSide, out siegeSurrenderSideLabel);
		List<PostprocessRuleEntry> siegeSurrenderRules = BuildSiegeSurrenderPostprocessRulesForNativeConversation(siegeSurrenderPostprocessEnabled, siegeSurrenderSettlement, siegeSurrenderSide);
		bool npcSurrenderPostprocessEnabled = allowEncounterSurrender
			&& !siegeSurrenderPostprocessEnabled
			&& IsNpcSurrenderPostprocessContext();
		List<PostprocessRuleEntry> npcSurrenderRules = BuildNpcSurrenderPostprocessRulesForScene(npcSurrenderPostprocessEnabled);
		bool siegeInterventionPostprocessEnabled = siegeInterventionRuleInjected && AfGcczShoutBridge.ShouldContinuePostprocess(siegeInterventionRuleInjected, preprocessRuleHits);
		List<PostprocessRuleEntry> siegeInterventionRules = TeamModuleServices.Siege.BuildPostprocessRules(
			siegeInterventionPostprocessEnabled,
			targetAgentIndex,
			replyIsDirectPlayerResponse,
			playerText);
		List<PostprocessRuleEntry> relayRules = BuildAutoGroupRelayPostprocessRulesForScene(relayRuleInjected, relaySingleFramedNpc);
		List<PostprocessRuleEntry> intimacyRules = AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.IntimacyRuleId, targetAgentIndex)
			? SexualConceptionBehavior.BuildRuntimePostprocessRules(targetHero ?? targetCharacter?.HeroObject, resolvedChainName)
			: new List<PostprocessRuleEntry>();
		bool siegeInterventionExclusive = siegeInterventionPostprocessEnabled && AfGcczShoutBridge.ShouldUseExclusivePostprocessRuleRouting(targetAgentIndex);
		// Global fact-detection rule (no topic): only when this request already
		// runs and the reply has stage prose the local parser cannot resolve.
		IReadOnlyList<string> sceneActionOfferedKeys = offerSceneActionDirective && !siegeInterventionExclusive
			? TryOfferSceneActionDirective(targetAgentIndex, replyText)
			: null;
		List<PostprocessRuleEntry> sceneActionRules = BuildSceneActionDirectiveRules(sceneActionOfferedKeys);
		if (sceneActionRules.Count == 0)
		{
			sceneActionOfferedKeys = null;
		}
		List<PostprocessRuleEntry> mergedRules = siegeInterventionExclusive
			? MergePostprocessRulesForScene(siegeInterventionRules, nobleExecutionOrderRules)
			: MergePostprocessRulesForScene(duelRules, transactionRules, kingdomRules, royalRules, vassalageRules, lordsHallRules, meetingReleaseRules, vanillaIssueRules, heroJoinPartyRules, mechanismRules, nobleExecutionOrderRules, partyTransferRules, voteDealRules, customPolicyAgendaRules, diplomacyRules, worldMapPartyCommandRules, nobleGatheringRules, marriageRules, proposeAgendaRules, siegeSurrenderRules, npcSurrenderRules, siegeInterventionRules, relayRules, intimacyRules, sceneActionRules, executionRules);
		bool royalPostprocessRuleInjected = (royalRules ?? new List<PostprocessRuleEntry>()).Any((PostprocessRuleEntry x) => string.Equals((x?.Tag ?? "").Trim(), "[ACTION:KING_ABDICATE_TO_PLAYER]", StringComparison.OrdinalIgnoreCase));
		bool vassalagePostprocessRuleInjected = (vassalageRules ?? new List<PostprocessRuleEntry>()).Any((PostprocessRuleEntry x) => (x?.Tag ?? "").StartsWith("[ACTION:VASSALAGE:", StringComparison.OrdinalIgnoreCase));
		int annexationRuleCount = (mergedRules ?? new List<PostprocessRuleEntry>()).Count((PostprocessRuleEntry x) => (x?.Tag ?? "").StartsWith("[ACTION:KINGDOM_ANNEX:", StringComparison.OrdinalIgnoreCase));
		bool kingdomAnnexPostprocessRuleInjected = annexationRuleCount > 0;
		if (kingdomVassalageRuleInjected)
		{
			VassalageDiagnosticLog.Event("postprocess.scene.rules", new Dictionary<string, object>
			{
				["vassalageRuleCount"] = vassalageRules?.Count ?? 0,
				["vassalageTags"] = (vassalageRules ?? new List<PostprocessRuleEntry>()).Select((PostprocessRuleEntry x) => x?.Tag ?? "").ToList(),
				["mergedRuleCount"] = mergedRules?.Count ?? 0,
				["mergedTags"] = (mergedRules ?? new List<PostprocessRuleEntry>()).Select((PostprocessRuleEntry x) => x?.Tag ?? "").ToList()
			});
		}
		Logger.Log("ShoutBehavior", "[UnifiedPostprocess] requested chain=" + resolvedChainName + " duel=" + duelRuleInjected + " reward=" + rewardRuleInjected + " loan=" + loanRuleInjected + " persistentAdpDebt=" + persistentAdpDebtRuleInjected + " kingdom=" + kingdomServiceRuleInjected + " royalEligible=" + royalPostprocessEligible + " royalRule=" + royalPostprocessRuleInjected + " vassalage=" + kingdomVassalageRuleInjected + " annexation=" + kingdomAnnexPostprocessRuleInjected + " VASSALAGE_rule_injected=" + vassalagePostprocessRuleInjected + " KINGDOM_ANNEX_rule_injected=" + kingdomAnnexPostprocessRuleInjected + " royalRuleCount=" + (royalRules?.Count ?? 0) + " vassalageRuleCount=" + (vassalageRules?.Count ?? 0) + " annexationRuleCount=" + annexationRuleCount + " lordsHall=" + lordsHallRuleInjected + " meetingRelease=" + meetingReleaseRuleInjected + " vanillaIssue=" + vanillaIssueRuleInjected + " heroJoin=" + heroJoinPartyRuleInjected + " sceneMechanism=" + sceneMechanismRuleInjected + " partyTransfer=" + partyTransferRuleInjected + " voteDeal=" + voteDealRuleInjected + " diplomacy=" + diplomacyRuleInjected + " worldMap=" + worldMapPartyCommandRuleInjected + " nobleGathering=" + nobleGatheringRuleInjected + " marriage=" + marriageRuleInjected + " intimacy=" + (intimacyRules?.Count > 0) + " siegeSurrender=" + siegeSurrenderPostprocessEnabled + " siegeSurrenderSide=" + (siegeSurrenderSideLabel ?? "") + " siegeSurrenderSettlement=" + (siegeSurrenderSettlement?.StringId ?? "") + " npcSurrender=" + npcSurrenderPostprocessEnabled + " siegeIntervention=" + siegeInterventionPostprocessEnabled + " relay=" + relayRuleInjected + " siegeSurrenderRule=" + HasSiegeSurrenderPostprocessRule(siegeSurrenderRules) + " mergedHasSiegeSurrender=" + HasSiegeSurrenderPostprocessRule(mergedRules) + " npcSurrenderRule=" + HasNpcSurrenderPostprocessRule(npcSurrenderRules) + " mergedHasNpcSurrender=" + HasNpcSurrenderPostprocessRule(mergedRules) + " preprocessHits=" + ((preprocessRuleHits == null || preprocessRuleHits.Count == 0) ? "(none)" : string.Join(",", preprocessRuleHits)) + " mergedRules=" + ((mergedRules == null || mergedRules.Count == 0) ? "(none; mood postprocess still runs)" : string.Join(",", mergedRules.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
		if (kingdomServiceRuleInjected)
		{
			Logger.Log("ShoutBehavior", "[UnifiedPostprocess] kingdom_rules=" + ((kingdomRules == null || kingdomRules.Count == 0) ? "（无）" : string.Join(",", kingdomRules.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))) + " merged_rules=" + ((mergedRules == null || mergedRules.Count == 0) ? "（无）" : string.Join(",", mergedRules.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
		}
		string text20 = string.IsNullOrWhiteSpace(npcName) ? (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "") : npcName;
		string text2 = NormalizePlayerNameForScenePostprocess(string.IsNullOrWhiteSpace(historyText) ? "（无）" : historyText.Trim(), text20);
		string text3 = BuildPostprocessRuleTextForScene(mergedRules);
		string text4 = BuildPostprocessRuleTextForScene(AIConfigHandler.ActionPostprocessMoodRules);
		string text5 = "（无）";
		string text6 = "（无）";
		string text7 = "（无）";
		string marriagePlayerCandidates = null;
		string marriageTargetCandidates = null;
		string runtimeContext = "（无）";
		List<RewardSystemBehavior.RewardItemInfo> rewardOptions = null;
		List<RewardSystemBehavior.RewardItemInfo> rewardAllOptions = null;
		int rewardAvailableGold = 0;
		List<MyBehavior.PartyTransferPromptEntry> partyTransferTroopOptions = null;
		List<MyBehavior.PartyTransferPromptEntry> partyTransferPrisonerOptions = null;
		List<MyBehavior.PartyTransferPromptEntry> partyTransferAllTroopOptions = null;
		List<MyBehavior.PartyTransferPromptEntry> partyTransferAllPrisonerOptions = null;
		List<MyBehavior.SettlementTransferPromptEntry> settlementTransferNpcOptions = null;
		List<MyBehavior.SettlementTransferPromptEntry> settlementTransferAllNpcOptions = null;
		MentionedWorldEntities promptListMentions = AIConfigHandler.GetLatestAuxiliaryMentionedEntitiesForExternal();
		int promptListMax = PromptListRetrievalService.GetMaxCandidateCount();
		if (transactionPostprocessEnabled)
		{
			if (RewardSystemBehavior.Instance != null)
			{
				try
				{
					text6 = RewardSystemBehavior.Instance.BuildVisibleEquipmentPostprocessListForAI(Hero.MainHero, promptListMentions, promptListMax);
				}
				catch
				{
					text6 = "赤身裸体";
				}
				try
				{
					if (targetHero != null)
					{
						if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope, targetHero, targetCharacter, -1, out rewardAllOptions))
						{
							rewardAllOptions = RewardSystemBehavior.Instance.BuildHeroRewardPostprocessItems(targetHero);
							PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope, targetHero, targetCharacter, -1, rewardAllOptions);
						}
					if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsSnapshotScope, targetHero, targetCharacter, -1, out rewardOptions))
					{
						rewardOptions = PromptListRetrievalService.FilterNpcRewardItemsForAssetTransfer(rewardAllOptions, promptListMentions, promptListMax);
					}
					if (exposeAllRewardItems)
					{
						rewardOptions = (rewardAllOptions ?? new List<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null && x.Count > 0).ToList();
					}
						rewardAvailableGold = RewardSystemBehavior.Instance.GetRewardPostprocessGoldForHero(targetHero);
						text5 = BuildRewardPostprocessItemListForScene(rewardOptions, rewardAvailableGold, rewardAllOptions);
						text7 = NormalizePlayerNameForScenePostprocess(RewardSystemBehavior.Instance.BuildDebtHintForAI(targetHero), text20);
					}
					else if (targetCharacter != null)
					{
						if (TryResolveWildernessNonHeroRewardParty(targetHero, targetCharacter, targetAgentIndex, out var party))
						{
							rewardAllOptions = RewardSystemBehavior.Instance.BuildPartyRewardPostprocessItems(party);
							PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.PartyRewardItemsAllSnapshotScope, null, targetCharacter, -1, rewardAllOptions);
						rewardOptions = PromptListRetrievalService.FilterRewardItems(rewardAllOptions, promptListMentions, promptListMax);
						if (exposeAllRewardItems)
						{
							rewardOptions = (rewardAllOptions ?? new List<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null && x.Count > 0).ToList();
						}
							rewardAvailableGold = RewardSystemBehavior.Instance.GetPartyTradeGoldForExternal(party);
							text5 = BuildRewardPostprocessItemListForScene(rewardOptions, rewardAvailableGold, rewardAllOptions);
							text7 = "（非hero野外部队没有个人赊账账本；如果要归还已经实际收到的物品或第纳尔，请只从上面的部队库存与资金中输出 GIVE 标签。）";
						}
						else
						{
							if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsAllSnapshotScope, null, targetCharacter, -1, out rewardAllOptions))
							{
								rewardAllOptions = RewardSystemBehavior.Instance.BuildSettlementMerchantPostprocessItems(targetCharacter);
								PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsAllSnapshotScope, null, targetCharacter, -1, rewardAllOptions);
							}
						if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsSnapshotScope, null, targetCharacter, -1, out rewardOptions))
						{
							rewardOptions = PromptListRetrievalService.FilterRewardItems(rewardAllOptions, promptListMentions, promptListMax);
						}
						if (exposeAllRewardItems)
						{
							rewardOptions = (rewardAllOptions ?? new List<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null && x.Count > 0).ToList();
						}
							rewardAvailableGold = RewardSystemBehavior.Instance.GetSettlementMarketTradeGold(Settlement.CurrentSettlement);
							text5 = BuildRewardPostprocessItemListForScene(rewardOptions, rewardAvailableGold, rewardAllOptions);
							text7 = NormalizePlayerNameForScenePostprocess(RewardSystemBehavior.Instance.BuildSettlementMerchantDebtHintForAI(targetCharacter), text20);
						}
					}
				}
				catch
				{
					text5 = "（无）";
					text7 = "（无）";
					rewardOptions = null;
					rewardAllOptions = null;
				}
			}
		}
		else if (duelRuleInjected)
		{
			text5 = BuildDuelPostprocessItemListForScene(duelStakeOptions);
			try
			{
				if (RewardSystemBehavior.Instance != null && Hero.MainHero != null)
				{
					text6 = RewardSystemBehavior.Instance.BuildVisibleEquipmentPostprocessListForAI(Hero.MainHero, promptListMentions, promptListMax);
				}
			}
			catch
			{
				text6 = "赤身裸体";
			}
		}
		if (partyTransferRuleInjected)
		{
			try
			{
				List<MyBehavior.PartyTransferPromptEntry> list = MyBehavior.BuildPartyTransferPromptEntriesForExternal(targetHero, targetCharacter, targetAgentIndex);
				int num2 = ResolvePartyTransferRecruitMaxTierForScene(targetHero, targetCharacter);
				bool hasTroopSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferTroopOptions);
				bool hasPrisonerSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferPrisonerOptions);
				bool hasAllTroopSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferAllTroopOptions);
				bool hasAllPrisonerSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferAllPrisonerOptions);
				bool wildernessNonHeroPartyTransfer = MyBehavior.IsWildernessNonHeroPartyTransferEligibleForExternal(targetHero, targetCharacter, targetAgentIndex);
				IEnumerable<MyBehavior.PartyTransferPromptEntry> troopOptions = (num2 > 0) ? list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcTroops && Math.Max(0, x.Character?.Tier ?? 0) > 0 && Math.Max(0, x.Character?.Tier ?? 0) <= num2) : Enumerable.Empty<MyBehavior.PartyTransferPromptEntry>();
				IEnumerable<MyBehavior.PartyTransferPromptEntry> volunteerOptions = list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcVolunteers);
				if (!hasTroopSnapshot || !hasPrisonerSnapshot)
				{
					if (!hasTroopSnapshot)
					{
						partyTransferTroopOptions = BuildDisplayIndexedPartyTransferEntriesForScene(troopOptions.Concat(volunteerOptions));
						partyTransferTroopOptions = PromptListRetrievalService.FilterPartyTransferEntries(partyTransferTroopOptions, promptListMentions, promptListMax, isPrisoner: false);
						PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferTroopOptions);
					}
					if (!hasPrisonerSnapshot)
					{
						partyTransferPrisonerOptions = BuildDisplayIndexedPartyTransferEntriesForScene(list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcPrisoners));
						partyTransferPrisonerOptions = PromptListRetrievalService.FilterPartyTransferEntries(partyTransferPrisonerOptions, promptListMentions, promptListMax, isPrisoner: true);
						PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferPrisonerOptions);
					}
				}
				if (!hasAllTroopSnapshot)
				{
					IEnumerable<MyBehavior.PartyTransferPromptEntry> allTroopOptions = wildernessNonHeroPartyTransfer
						? troopOptions.Concat(volunteerOptions)
						: list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && (x.Section == MyBehavior.PartyTransferEntrySection.NpcTroops || x.Section == MyBehavior.PartyTransferEntrySection.NpcVolunteers));
					partyTransferAllTroopOptions = BuildDisplayIndexedPartyTransferEntriesForScene(allTroopOptions);
					PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferAllTroopOptions);
				}
				if (!hasAllPrisonerSnapshot)
				{
					partyTransferAllPrisonerOptions = BuildDisplayIndexedPartyTransferEntriesForScene(list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcPrisoners));
					PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferAllPrisonerOptions);
				}
				runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildPartyTransferNpcPostprocessListForScene(partyTransferTroopOptions, partyTransferPrisonerOptions, partyTransferAllTroopOptions, partyTransferAllPrisonerOptions));
			}
			catch
			{
				partyTransferTroopOptions = null;
				partyTransferPrisonerOptions = null;
				partyTransferAllTroopOptions = null;
				partyTransferAllPrisonerOptions = null;
			}
		}
		if (rewardRuleInjected)
		{
			try
			{
				List<MyBehavior.SettlementTransferPromptEntry> list2 = MyBehavior.BuildSettlementTransferPromptEntriesForExternal(targetHero, targetCharacter);
				if (!PromptListRetrievalService.TryGetSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferAllNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, out settlementTransferAllNpcOptions))
				{
					settlementTransferAllNpcOptions = BuildDisplayIndexedSettlementTransferEntriesForScene(list2.Where((MyBehavior.SettlementTransferPromptEntry x) => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.NpcFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x)));
					PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferAllNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, settlementTransferAllNpcOptions);
				}
				if (!PromptListRetrievalService.TryGetSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, out settlementTransferNpcOptions))
				{
					settlementTransferNpcOptions = BuildDisplayIndexedSettlementTransferEntriesForScene(list2.Where((MyBehavior.SettlementTransferPromptEntry x) => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.NpcFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x)));
					settlementTransferNpcOptions = PromptListRetrievalService.FilterSettlementTransferEntries(settlementTransferNpcOptions, promptListMentions, promptListMax);
					PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, settlementTransferNpcOptions);
				}
				if (exposeAllFixedAssets)
				{
					settlementTransferNpcOptions = settlementTransferAllNpcOptions;
				}
				runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildSettlementTransferPostprocessListForScene(settlementTransferNpcOptions, settlementTransferAllNpcOptions));
			}
				catch
				{
					settlementTransferNpcOptions = null;
					settlementTransferAllNpcOptions = null;
				}
		}
		if (sceneMechanismRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildSceneMechanismTargetListForPostprocess(sceneSummonTargets, sceneGuideTargets));
		}
		if (voteDealRuleInjected || customPolicyAgendaRuleInjected || worldMapPartyCommandRuleInjected || heroJoinPartyRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, entityPostprocessContext);
		}
		if (worldMapPartyCommandRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, WorldMapPartyCommandBehavior.BuildCurrentNpcCommandTasksPromptForExternal(targetHero, targetCharacter, targetAgentIndex));
		}
		if (diplomacyRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, DiplomacyConversationBridge.BuildDiplomacyPostprocessContext(targetHero ?? targetCharacter?.HeroObject));
		}
		if (marriageRuleInjected && RomanceSystemBehavior.Instance != null)
		{
			marriagePlayerCandidates = RomanceSystemBehavior.Instance.BuildMarriagePostprocessPlayerCandidatesBlockForExternal(marriageSpeaker);
			marriageTargetCandidates = RomanceSystemBehavior.Instance.BuildMarriagePostprocessTargetCandidatesBlockForExternal(marriageSpeaker);
		}
		if (nobleGatheringRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, TeamModuleServices.Gathering.BuildPostprocessContextForExternal(targetHero ?? targetCharacter?.HeroObject));
		}
		if (siegeSurrenderPostprocessEnabled)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildSiegeSurrenderPostprocessContextForNativeConversation(siegeSurrenderSettlement, siegeSurrenderSideLabel));
		}
		if (siegeInterventionPostprocessEnabled)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, TeamModuleServices.Siege.BuildPostprocessContext(
				siegeInterventionPostprocessEnabled,
				targetAgentIndex,
				replyIsDirectPlayerResponse,
				replyIsDirectPlayerResponse ? playerText : string.Empty));
		}
		if (relayRuleInjected)
		{
			runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildSceneRelayTargetListForPostprocess(relayCandidates, targetAgentIndex, relayPrimaryTargetAgentIndex));
		}
		string text8 = AIConfigHandler.BuildActionPostprocessSystemPrompt(text3, text4, text20, text5, text6, text7, marriagePlayerCandidates, marriageTargetCandidates);
		string latestReplyBlock = replyIsDirectPlayerResponse
			? AIConfigHandler.BuildActionPostprocessLatestReplyBlock(playerText, text, text20, text2)
			: AIConfigHandler.BuildActionPostprocessLatestReplyBlock("", text, text20, null);
		string text9 = BuildSceneActionPostprocessUserPrompt(actionPostprocessUserPromptTemplate, text3, text20, text2, latestReplyBlock, text5, text6, text7, marriagePlayerCandidates, marriageTargetCandidates, runtimeContext);
		text9 = AfGcczShoutBridge.AppendTownPostprocessDecisionContract(text9, AfGcczShoutBridge.ShouldUseTownPostprocessDecisionContract(), mergedRules);
		if (NativeConversationDetachedPromptParityLoggingEnabled && detachedMainPromptSections != null)
		{
			try
			{
				LegacyNativePromptParity.LegacyNativePromptParityResult postprocessParity = LegacyNativePromptParity.ComparePostprocessBlocks(text8, text9, 5000, "legacy-native-postprocess");
				DetachedInteractionPromptSections atomicBundle = LegacyNativePromptParity.BuildAtomicBundle(
					detachedMainPromptSections,
					postprocessParity.PostprocessSections);
				Logger.Log("ShoutBehavior", "[NativeDetachedPromptParity] " + postprocessParity.ToDiagnosticString()
					+ " atomicMainSystemSections=" + atomicBundle.Main.SystemSections.Count
					+ " atomicPostSystemSections=" + atomicBundle.Postprocess.SystemSections.Count);
			}
			catch (Exception ex)
			{
				// Keep the old postprocess request as the fail-open fallback.
				Logger.Log("ShoutBehavior", "[NativeDetachedPromptParity] postprocess comparison failed open: " + ex.Message);
			}
		}
		return new SceneActionPostprocessWorkItem(text8, text9, text, content =>
		{
			string text10 = duelRuleInjected ? NormalizeDuelPostprocessTagsForScene(content, duelStakeOptions, targetHero) : "";
			string text11 = transactionPostprocessEnabled ? NormalizeRewardPostprocessTagsForScene(content, rewardOptions, rewardAllOptions, rewardRuleInjected ? settlementTransferNpcOptions : null, rewardRuleInjected, rewardAvailableGold) : "";
			if (persistentAdpDebtRuleInjected && !rewardRuleInjected && !loanRuleInjected)
			{
				text11 = KeepOnlyPersistentAdpDebtTags(text11);
			}
			string text12 = kingdomServiceRuleInjected ? NormalizeKingdomServicePostprocessTagsForScene(content, kingdomRules) : "";
			string royalTags = royalPostprocessRuleInjected ? NormalizeKingdomServicePostprocessTagsForScene(content, royalRules) : "";
			string vassalageTags = kingdomVassalageRuleInjected ? NormalizeVassalagePostprocessTagsForScene(content, vassalageRules) : "";
			string diplomacyTags = (diplomacyRules != null && diplomacyRules.Count > 0) ? NormalizeDiplomacyPostprocessTagsForScene(content, diplomacyRules) : "";
			string annexationTags = diplomacyTags;
			Logger.Log("ShoutBehavior", "[UnifiedPostprocess] result chain=" + resolvedChainName
				+ " rawHasVASSALAGE=" + ContainsVassalageActionTagForLog(content)
				+ " normalizedHasVASSALAGE=" + ContainsVassalageActionTagForLog(vassalageTags)
				+ " rawHasKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTagForLog(content)
				+ " normalizedHasKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTagForLog(diplomacyTags)
				+ " vassalageTags=" + (string.IsNullOrWhiteSpace(vassalageTags) ? "(none)" : vassalageTags.Replace("\r", "").Replace("\n", "\\n"))
				+ " annexationTags=" + (string.IsNullOrWhiteSpace(annexationTags) ? "(none)" : annexationTags.Replace("\r", "").Replace("\n", "\\n")));
			if (kingdomVassalageRuleInjected)
			{
				VassalageDiagnosticLog.Event("postprocess.scene.result", new Dictionary<string, object>
				{
					["rawContent"] = VassalageDiagnosticLog.Preview(content, 4000),
					["normalizedVassalageTags"] = vassalageTags,
					["targetHero"] = VassalageDiagnosticLog.DescribeHero(targetHero),
					["targetCharacterId"] = targetCharacter?.StringId ?? "",
					["targetAgentIndex"] = targetAgentIndex
				});
			}
			string text13 = lordsHallRuleInjected ? NormalizeLordsHallAccessPostprocessTagsForScene(content, lordsHallRules) : "";
			string text14 = meetingReleaseRuleInjected ? NormalizeEncounterReleasePostprocessTagsForScene(content, meetingReleaseRules) : "";
			string text15 = vanillaIssueRuleInjected ? NormalizeVanillaIssuePostprocessTagsForScene(content, vanillaIssueRules) : "";
			string text16 = heroJoinPartyRuleInjected ? NormalizeHeroJoinPartyPostprocessTagsForScene(content, heroJoinPartyRules) : "";
			string text17 = sceneMechanismRuleInjected ? NormalizeSceneMechanismPostprocessTagsForScene(content, mechanismRules, sceneSummonTargets, sceneGuideTargets) : "";
			string nobleExecutionOrderTags = NoblePrisonerExecutionOrderBehavior.NormalizePostprocessTags(content, nobleExecutionOrderRules);
			string text18 = partyTransferRuleInjected ? NormalizePartyTransferPostprocessTagsForScene(content, partyTransferTroopOptions, partyTransferPrisonerOptions, partyTransferAllTroopOptions, partyTransferAllPrisonerOptions) : "";
			string voteDealTags = (voteDealRules != null && voteDealRules.Count > 0) ? NormalizeVoteDealPostprocessTagsForScene(content, voteDealRules) : "";
			string customPolicyAgendaTags = customPolicyAgendaRuleInjected ? NormalizeCustomPolicyAgendaPostprocessTag(content, customPolicyAgendaRules) : "";
			if (customPolicyAgendaRuleInjected || CustomPolicyAgendaActionTagRegex.IsMatch(content ?? ""))
			{
				Logger.Log("ShoutBehavior", "[CustomPolicyAgendaPostprocess] chain=" + resolvedChainName + " RAW_TAG=" + CustomPolicyAgendaActionTagRegex.IsMatch(content ?? "") + " FINAL_TAG=" + !string.IsNullOrWhiteSpace(customPolicyAgendaTags));
			}
			string proposeAgendaTags = "";
			string worldMapPartyCommandTags = worldMapPartyCommandRuleInjected ? NormalizeWorldMapPartyCommandPostprocessTagsForScene(content) : "";
			string nobleGatheringTags = nobleGatheringRuleInjected ? TeamModuleServices.Gathering.NormalizeNobleGatheringPostprocessTagsForExternal(content) : "";
			string marriageTags = (marriageRuleInjected && RomanceSystemBehavior.Instance != null) ? RomanceSystemBehavior.Instance.NormalizeMarriagePostprocessTagsForExternal(content, marriageRules, marriageSpeaker) : "";
			string siegeSurrenderTags = NormalizeSiegeSurrenderPostprocessTagsForScene(content, siegeSurrenderRules, siegeSurrenderPostprocessEnabled);
			string npcSurrenderTags = NormalizeNpcSurrenderPostprocessTagsForScene(content, npcSurrenderRules, npcSurrenderPostprocessEnabled);
			string siegeInterventionTags = siegeInterventionPostprocessEnabled ? TeamModuleServices.Siege.NormalizePostprocessTags(siegeInterventionPostprocessEnabled, content, siegeInterventionRules) : "";
			string relayTags = relayRuleInjected ? NormalizeAutoGroupRelayPostprocessTagsForScene(content, relayCandidates, targetAgentIndex) : "";
			string intimacyTags = SexualConceptionBehavior.NormalizePostprocessTags(content, intimacyRules);
			string executionTags = PublicExecutionOrderRuntime.Normalize(executionPermit, content);
			string sceneActionTags = NormalizeSceneActionDirectiveTag(content, sceneActionOfferedKeys);
			if (sceneActionOfferedKeys != null)
			{
				Logger.Log("ShoutBehavior", "[SceneActionDirective] chain=" + resolvedChainName + " agent=" + targetAgentIndex
					+ " RAW_TAG=" + NpcReplyDirectiveTagV1.ContainsTag(content) + " FINAL_TAG=" + (string.IsNullOrWhiteSpace(sceneActionTags) ? "(none)" : sceneActionTags));
			}
			string text21 = siegeInterventionExclusive
				? MergeNormalizedPostprocessBlocksForScene(siegeInterventionTags, nobleExecutionOrderTags)
				: MergeNormalizedPostprocessBlocksForScene(text10, text11, text12, royalTags, vassalageTags, text13, text14, text15, text16, text17, nobleExecutionOrderTags, text18, voteDealTags, customPolicyAgendaTags, diplomacyTags, worldMapPartyCommandTags, nobleGatheringTags, marriageTags, proposeAgendaTags, siegeSurrenderTags, npcSurrenderTags, siegeInterventionTags, relayTags, intimacyTags, sceneActionTags, executionTags);
			text21 = AfGcczShoutBridge.ValidateTownPostprocessDecision(text21);
			if (string.IsNullOrWhiteSpace(text21))
			{
				text21 = AIConfigHandler.ActionPostprocessFallbackMoodTag;
			}
			MarkWeeklyMemoryMaterialTriggerForScene(targetHero, targetCharacter, text20, StripAutoGroupRelaySignal(text21), targetAgentIndex, rewardAllOptions ?? rewardOptions, partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferAllNpcOptions ?? settlementTransferNpcOptions, partyTransferAllTroopOptions: partyTransferAllTroopOptions, partyTransferAllPrisonerOptions: partyTransferAllPrisonerOptions);
			string text22 = (text + "\n" + text21).Trim();
			Logger.Log("ShoutBehavior", "[UnifiedPostprocess] RAW=\n" + content + "\nFINAL=\n" + text22 + "\n");
			return text22;
		});
	}
internal static bool TryPrepareCourierActionPostprocessForExternal(Hero targetHero, CharacterObject targetCharacter, string npcName, string playerText, string historyText, string replyText, bool duelRuleInjected, bool rewardRuleInjected, bool loanRuleInjected, bool kingdomServiceRuleInjected, bool lordsHallRuleInjected, bool meetingReleaseRuleInjected, bool vanillaIssueRuleInjected, bool heroJoinPartyRuleInjected, bool sceneMechanismRuleInjected, bool partyTransferRuleInjected, out ConversationCourierPostprocessWorkItem workItem, out string immediateResult, bool voteDealRuleInjected = false, bool diplomacyRuleInjected = false, bool worldMapPartyCommandRuleInjected = false, List<string> preprocessRuleHits = null, string entityPostprocessContext = null, int targetAgentIndex = -1, bool latestReplyHasPlayerInput = true, bool forceLooseWeeklyMemoryMaterialSession = false, bool kingdomVassalageRuleInjected = false, bool kingdomAnnexationRuleInjected = false, string chainName = null)
	{
		RequireMainThread();
		workItem = null;
		immediateResult = "";
		string text = StripActionTagsForSceneSpeech(replyText ?? "");
		string resolvedChainName = string.IsNullOrWhiteSpace(chainName) ? "courier" : chainName.Trim();
		string runtimeTargetKingdomId = ResolveCourierRuntimeTargetKingdomId(targetHero, targetCharacter);
		string runtimeTargetHeroId = (targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "").Trim();
		string runtimeTargetCharacterId = (targetCharacter?.StringId ?? "").Trim();
		string runtimeTargetTroopId = runtimeTargetCharacterId.ToLowerInvariant();
		string runtimeTargetUnnamedRank = (targetHero == null && targetCharacter != null) ? (targetCharacter.IsSoldier ? "soldier" : "commoner") : "";
		using IDisposable guardrailScopeJ03 = AIConfigHandler.BeginGuardrailRuntimeScope();
		AIConfigHandler.SetGuardrailRuntimeTargetKingdom(runtimeTargetKingdomId);
		AIConfigHandler.SetGuardrailRuntimeTargetHero(runtimeTargetHeroId);
		AIConfigHandler.SetGuardrailRuntimeTargetCharacter(runtimeTargetCharacterId);
		AIConfigHandler.SetGuardrailRuntimeTargetTroop(runtimeTargetTroopId);
		AIConfigHandler.SetGuardrailRuntimeTargetUnnamedRank(runtimeTargetUnnamedRank);
		AIConfigHandler.SetGuardrailRuntimeTargetAgentIndex(targetAgentIndex);
		try
		{
			bool kingdomVassalagePreprocessHit = HasPreprocessRuleHit(preprocessRuleHits, "kingdom_vassalage");
			bool persistentAdpDebtRuleInjected = HasPreprocessRuleHit(preprocessRuleHits, PersistentAdpDebtPostprocessRuleId);
			bool customPolicyAgendaRuleInjected = latestReplyHasPlayerInput && HasPreprocessRuleHit(preprocessRuleHits, CustomPolicyAgendaPostprocessRuleId);
			if (customPolicyAgendaRuleInjected && !TeamModuleServices.Policy.IsEligibleTargetForExternal(targetHero ?? targetCharacter?.HeroObject, out var customPolicyAgendaBlockedReason))
			{
				customPolicyAgendaRuleInjected = false;
				Logger.Log("CourierDelivery", "[CustomPolicyAgendaPostprocess] blocked chain=" + resolvedChainName + " target=" + (targetHero?.StringId ?? targetCharacter?.HeroObject?.StringId ?? "null") + " reason=" + (customPolicyAgendaBlockedReason ?? ""));
			}
			duelRuleInjected = duelRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "duel");
			rewardRuleInjected = rewardRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "reward");
			loanRuleInjected = loanRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "loan");
			kingdomServiceRuleInjected = kingdomServiceRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "kingdom_service");
			kingdomVassalageRuleInjected = kingdomVassalageRuleInjected || kingdomVassalagePreprocessHit;
			lordsHallRuleInjected = lordsHallRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "lords_hall_access");
			meetingReleaseRuleInjected = meetingReleaseRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "encounter_release_player");
			vanillaIssueRuleInjected = vanillaIssueRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "vanilla_issue");
			heroJoinPartyRuleInjected = heroJoinPartyRuleInjected || kingdomServiceRuleInjected;
			sceneMechanismRuleInjected = sceneMechanismRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "scene_mechanism_actions");
			partyTransferRuleInjected = partyTransferRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "party_transfer");
			voteDealRuleInjected = voteDealRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "kingdom_agenda");
			bool royalPostprocessEligible = AIConfigHandler.IsRoyalAbdicationPostprocessTargetForExternal(targetHero ?? targetCharacter?.HeroObject);
			bool royalDiplomacyRequested = diplomacyRuleInjected || kingdomAnnexationRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "diplomacy");
			bool royalDiplomacyRuleInjected = royalDiplomacyRequested && DiplomacyConversationBridge.CanUseDiplomacyActionPostprocessForExternal(targetHero, targetCharacter);
			bool independentClanPeaceResident = latestReplyHasPlayerInput && DiplomacyConversationBridge.CanUseIndependentClanPeaceForExternal(targetHero, targetCharacter);
			diplomacyRuleInjected = royalDiplomacyRuleInjected || independentClanPeaceResident;
			kingdomAnnexationRuleInjected = false;
			worldMapPartyCommandRuleInjected = worldMapPartyCommandRuleInjected || HasPreprocessRuleHit(preprocessRuleHits, "worldmap_party_command");
			bool nobleGatheringRuleInjected = HasPreprocessRuleHit(preprocessRuleHits, "noble_gathering");
			bool marriageRuleInjected = HasPreprocessRuleHit(preprocessRuleHits, "marriage");
			ApplyStageQualifications(targetAgentIndex, ref duelRuleInjected, ref rewardRuleInjected, ref loanRuleInjected, ref persistentAdpDebtRuleInjected, ref kingdomServiceRuleInjected, ref kingdomVassalageRuleInjected, ref lordsHallRuleInjected, ref meetingReleaseRuleInjected, ref vanillaIssueRuleInjected, ref heroJoinPartyRuleInjected, ref sceneMechanismRuleInjected, ref partyTransferRuleInjected, ref voteDealRuleInjected, ref customPolicyAgendaRuleInjected, ref diplomacyRuleInjected, ref worldMapPartyCommandRuleInjected, ref nobleGatheringRuleInjected, ref marriageRuleInjected);
			bool siegeInterventionRuleInjected = AfGcczShoutBridge.ShouldRunPostprocessForActiveScene();
			if (!CanUseSceneMechanismPostprocessForSpeaker(targetAgentIndex))
			{
				sceneMechanismRuleInjected = false;
			}
			Logger.Log("CourierDelivery", "[UnifiedPostprocess] setup chain=" + resolvedChainName
				+ " preprocessHits=" + ((preprocessRuleHits == null || preprocessRuleHits.Count == 0) ? "(none)" : string.Join(",", preprocessRuleHits))
				+ " kingdom_vassalage_hit=" + kingdomVassalagePreprocessHit
				+ " kingdom_vassalage_injected=" + kingdomVassalageRuleInjected
				+ " kingdom_agenda_injected=" + customPolicyAgendaRuleInjected
				+ " diplomacy_injected=" + diplomacyRuleInjected
				+ " independent_clan_peace_resident=" + independentClanPeaceResident);
			if (kingdomVassalageRuleInjected)
			{
				VassalageDiagnosticLog.Event("postprocess.native.start", new Dictionary<string, object>
				{
					["targetHero"] = VassalageDiagnosticLog.DescribeHero(targetHero),
					["targetCharacterId"] = targetCharacter?.StringId ?? "",
					["npcName"] = npcName ?? "",
					["playerText"] = VassalageDiagnosticLog.Preview(playerText, 1000),
					["historyTextLen"] = (historyText ?? "").Length,
					["replyText"] = VassalageDiagnosticLog.Preview(replyText, 2000),
					["preprocessHits"] = preprocessRuleHits
				});
			}
			if (!AIConfigHandler.CanUseAuxiliaryActionPostprocess())
			{
				if (Regex.Matches(text ?? "", "\\[ACTION:MOOD:[^\\]]+\\]", RegexOptions.IgnoreCase).Count <= 0 && !string.IsNullOrWhiteSpace(AIConfigHandler.ActionPostprocessFallbackMoodTag))
				{
					text = (text + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim();
				}
				immediateResult = text.Trim();
				return false;
			}
			string actionPostprocessSystemPrompt = AIConfigHandler.ActionPostprocessSystemPrompt;
			string actionPostprocessUserPromptTemplate = AIConfigHandler.ActionPostprocessUserPromptTemplate;
			if (string.IsNullOrWhiteSpace(actionPostprocessSystemPrompt) || string.IsNullOrWhiteSpace(actionPostprocessUserPromptTemplate))
			{
				immediateResult = (text + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim();
				return false;
			}
			bool transactionPostprocessEnabled = rewardRuleInjected || loanRuleInjected || persistentAdpDebtRuleInjected;
			bool exposeAllRewardItems = rewardRuleInjected && RequestsAllOrdinaryAssetsForPostprocess(playerText, historyText);
			bool exposeAllFixedAssets = rewardRuleInjected && RequestsAllFixedAssetsForPostprocess(playerText, historyText);
			List<PostprocessRuleEntry> duelRules = duelRuleInjected ? AIConfigHandler.DuelPostprocessRules : null;
			List<PostprocessRuleEntry> persistentAdpDebtRules = persistentAdpDebtRuleInjected ? BuildPersistentAdpDebtPostprocessRules() : null;
			List<PostprocessRuleEntry> transactionRules = transactionPostprocessEnabled ? MergePostprocessRulesForScene(rewardRuleInjected ? AIConfigHandler.RewardPostprocessRules : null, loanRuleInjected ? AIConfigHandler.LoanPostprocessRules : null, persistentAdpDebtRules) : null;
			List<PostprocessRuleEntry> kingdomRules = kingdomServiceRuleInjected ? (AIConfigHandler.BuildRuntimeKingdomServicePostprocessRules() ?? new List<PostprocessRuleEntry>()) : null;
			List<PostprocessRuleEntry> royalRules = AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.RoyalActionRuleId, targetAgentIndex)
				? (AIConfigHandler.BuildRuntimeRoyalPostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) ?? new List<PostprocessRuleEntry>())
				: new List<PostprocessRuleEntry>();
			List<PostprocessRuleEntry> vassalageRules = kingdomVassalageRuleInjected ? (VassalageBehavior.BuildRuntimeVassalagePostprocessRulesForExternal(targetHero, targetCharacter) ?? new List<PostprocessRuleEntry>()) : null;
			List<PostprocessRuleEntry> lordsHallRules = lordsHallRuleInjected ? MergePostprocessRulesForScene(AIConfigHandler.GetGuardrailRulePostprocessRules("lords_hall_access"), AIConfigHandler.BuildRuntimeLordsHallAccessPostprocessRules() ?? new List<PostprocessRuleEntry>()) : null;
			List<PostprocessRuleEntry> meetingReleaseRules = meetingReleaseRuleInjected ? MergePostprocessRulesForScene(AIConfigHandler.GetGuardrailRulePostprocessRules("encounter_release_player"), LordEncounterBehavior.BuildMeetingPlayerReleasePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject)) : null;
			List<PostprocessRuleEntry> vanillaIssueRules = vanillaIssueRuleInjected ? (VanillaIssueOfferBridge.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) ?? new List<PostprocessRuleEntry>()) : null;
			List<PostprocessRuleEntry> heroJoinPartyRules = heroJoinPartyRuleInjected ? (AIConfigHandler.BuildRuntimeHeroJoinPartyPostprocessRules(!ShouldSuppressHeroJoinPartyPostprocessForScene(targetHero ?? targetCharacter?.HeroObject), targetHero ?? targetCharacter?.HeroObject, entityPostprocessContext) ?? new List<PostprocessRuleEntry>()) : null;
			if (heroJoinPartyRuleInjected && (heroJoinPartyRules == null || heroJoinPartyRules.Count == 0))
			{
				heroJoinPartyRuleInjected = false;
			}
			List<PostprocessRuleEntry> mechanismRules = sceneMechanismRuleInjected
				? (AIConfigHandler.GetGuardrailRulePostprocessRules("scene_mechanism_actions") ?? new List<PostprocessRuleEntry>())
				: null;
			if (sceneMechanismRuleInjected)
			{
				string inspectionSlaughterRule = TroopInspectionBehavior.BuildPrisonerSlaughterPostprocessRuleForExternal(targetAgentIndex);
				if (!string.IsNullOrWhiteSpace(inspectionSlaughterRule)
					&& !(mechanismRules ?? new List<PostprocessRuleEntry>()).Any(rule => string.Equals(
						(rule?.Tag ?? "").Trim(),
						TroopInspectionPrisonerSlaughterProfile.ActionTag,
						StringComparison.OrdinalIgnoreCase)))
				{
					mechanismRules.Add(new PostprocessRuleEntry
					{
						Tag = TroopInspectionPrisonerSlaughterProfile.ActionTag,
						Description = inspectionSlaughterRule
					});
				}
				if (AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission())
				{
					mechanismRules = mechanismRules
						.Where(rule => string.Equals(
							(rule?.Tag ?? "").Trim(),
							TroopInspectionPrisonerSlaughterProfile.ActionTag,
							StringComparison.OrdinalIgnoreCase))
						.ToList();
				}
			}
			List<PostprocessRuleEntry> nobleExecutionOrderRules = latestReplyHasPlayerInput
				? NoblePrisonerExecutionOrderBehavior.BuildRuntimePostprocessRules(
					targetHero ?? targetCharacter?.HeroObject,
					targetAgentIndex)
				: new List<PostprocessRuleEntry>();
			List<PostprocessRuleEntry> partyTransferRules = partyTransferRuleInjected ? (AIConfigHandler.GetGuardrailRulePostprocessRules("party_transfer") ?? new List<PostprocessRuleEntry>()) : null;
			List<PostprocessRuleEntry> voteDealRules = voteDealRuleInjected ? VoteDealBehavior.BuildAgendaVotePostprocessRulesForExternal() : null;
			List<PostprocessRuleEntry> customPolicyAgendaRules = customPolicyAgendaRuleInjected ? TeamModuleServices.Policy.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) : null;
			if (customPolicyAgendaRuleInjected && customPolicyAgendaRules.Count == 0)
			{
				customPolicyAgendaRuleInjected = false;
			}
			List<PostprocessRuleEntry> diplomacyRules = diplomacyRuleInjected ? BuildRuntimeDiplomacyPostprocessRulesForScene(targetHero, targetCharacter) : null;
			List<PostprocessRuleEntry> proposeAgendaRules = null;
			List<PostprocessRuleEntry> worldMapPartyCommandRules = worldMapPartyCommandRuleInjected ? (WorldMapPartyCommandBehavior.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject, targetCharacter, targetAgentIndex) ?? new List<PostprocessRuleEntry>()) : null;
			List<PostprocessRuleEntry> nobleGatheringRules = nobleGatheringRuleInjected ? (TeamModuleServices.Gathering.BuildRuntimePostprocessRulesForExternal(targetHero ?? targetCharacter?.HeroObject) ?? new List<PostprocessRuleEntry>()) : null;
			Hero marriageSpeaker = targetHero ?? targetCharacter?.HeroObject;
			List<PostprocessRuleEntry> marriageRuntimeRules = marriageRuleInjected ? (RomanceSystemBehavior.Instance?.BuildRuntimeMarriagePostprocessRulesForExternal(marriageSpeaker) ?? new List<PostprocessRuleEntry>()) : null;
			List<PostprocessRuleEntry> marriageRules = marriageRuleInjected ? marriageRuntimeRules : null;
			List<PostprocessRuleEntry> siegeInterventionRules = TeamModuleServices.Siege.BuildPostprocessRules(
				siegeInterventionRuleInjected,
				targetAgentIndex,
				latestReplyHasPlayerInput,
				playerText);
			List<PostprocessRuleEntry> intimacyRules = AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.IntimacyRuleId, targetAgentIndex)
				? SexualConceptionBehavior.BuildRuntimePostprocessRules(targetHero ?? targetCharacter?.HeroObject, resolvedChainName)
				: new List<PostprocessRuleEntry>();
			bool npcSurrenderPostprocessEnabled = AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.EncounterSurrenderRuleId, targetAgentIndex)
				&& IsNpcSurrenderPostprocessContext();
			List<PostprocessRuleEntry> npcSurrenderRules = BuildNpcSurrenderPostprocessRulesForScene(npcSurrenderPostprocessEnabled);
			bool siegeInterventionExclusive = siegeInterventionRuleInjected && AfGcczShoutBridge.ShouldUseExclusivePostprocessRuleRouting(targetAgentIndex);
			List<PostprocessRuleEntry> mergedRules = siegeInterventionExclusive
				? MergePostprocessRulesForScene(siegeInterventionRules, nobleExecutionOrderRules)
				: MergePostprocessRulesForScene(duelRules, transactionRules, kingdomRules, royalRules, vassalageRules, lordsHallRules, meetingReleaseRules, vanillaIssueRules, heroJoinPartyRules, mechanismRules, nobleExecutionOrderRules, partyTransferRules, voteDealRules, customPolicyAgendaRules, diplomacyRules, worldMapPartyCommandRules, nobleGatheringRules, marriageRules, proposeAgendaRules, npcSurrenderRules, siegeInterventionRules, intimacyRules);
			bool royalPostprocessRuleInjected = (royalRules ?? new List<PostprocessRuleEntry>()).Any((PostprocessRuleEntry x) => string.Equals((x?.Tag ?? "").Trim(), "[ACTION:KING_ABDICATE_TO_PLAYER]", StringComparison.OrdinalIgnoreCase));
			bool vassalagePostprocessRuleInjected = (vassalageRules ?? new List<PostprocessRuleEntry>()).Any((PostprocessRuleEntry x) => (x?.Tag ?? "").StartsWith("[ACTION:VASSALAGE:", StringComparison.OrdinalIgnoreCase));
			int annexationRuleCount = (mergedRules ?? new List<PostprocessRuleEntry>()).Count((PostprocessRuleEntry x) => (x?.Tag ?? "").StartsWith("[ACTION:KINGDOM_ANNEX:", StringComparison.OrdinalIgnoreCase));
			bool kingdomAnnexPostprocessRuleInjected = annexationRuleCount > 0;
			if (kingdomVassalageRuleInjected)
			{
				VassalageDiagnosticLog.Event("postprocess.native.rules", new Dictionary<string, object>
				{
					["vassalageRuleCount"] = vassalageRules?.Count ?? 0,
					["vassalageTags"] = (vassalageRules ?? new List<PostprocessRuleEntry>()).Select((PostprocessRuleEntry x) => x?.Tag ?? "").ToList(),
					["mergedRuleCount"] = mergedRules?.Count ?? 0,
					["mergedTags"] = (mergedRules ?? new List<PostprocessRuleEntry>()).Select((PostprocessRuleEntry x) => x?.Tag ?? "").ToList()
				});
			}
			Logger.Log("CourierDelivery", "[UnifiedPostprocess] requested chain=" + resolvedChainName + " duel=" + duelRuleInjected + " reward=" + rewardRuleInjected + " loan=" + loanRuleInjected + " persistentAdpDebt=" + persistentAdpDebtRuleInjected + " kingdom=" + kingdomServiceRuleInjected + " royalEligible=" + royalPostprocessEligible + " royalRule=" + royalPostprocessRuleInjected + " vassalage=" + kingdomVassalageRuleInjected + " annexation=" + kingdomAnnexPostprocessRuleInjected + " VASSALAGE_rule_injected=" + vassalagePostprocessRuleInjected + " KINGDOM_ANNEX_rule_injected=" + kingdomAnnexPostprocessRuleInjected + " royalRuleCount=" + (royalRules?.Count ?? 0) + " vassalageRuleCount=" + (vassalageRules?.Count ?? 0) + " annexationRuleCount=" + annexationRuleCount + " lordsHall=" + lordsHallRuleInjected + " meetingRelease=" + meetingReleaseRuleInjected + " vanillaIssue=" + vanillaIssueRuleInjected + " heroJoin=" + heroJoinPartyRuleInjected + " sceneMechanism=" + sceneMechanismRuleInjected + " partyTransfer=" + partyTransferRuleInjected + " voteDeal=" + voteDealRuleInjected + " diplomacy=" + diplomacyRuleInjected + " worldMap=" + worldMapPartyCommandRuleInjected + " nobleGathering=" + nobleGatheringRuleInjected + " marriage=" + marriageRuleInjected + " intimacy=" + (intimacyRules?.Count > 0) + " siegeIntervention=" + siegeInterventionRuleInjected + " npcSurrender=" + npcSurrenderPostprocessEnabled + " npcSurrenderRule=" + HasNpcSurrenderPostprocessRule(npcSurrenderRules) + " mergedHasNpcSurrender=" + HasNpcSurrenderPostprocessRule(mergedRules) + " preprocessHits=" + ((preprocessRuleHits == null || preprocessRuleHits.Count == 0) ? "(none)" : string.Join(",", preprocessRuleHits)) + " mergedRules=" + ((mergedRules == null || mergedRules.Count == 0) ? "(none; mood postprocess still runs)" : string.Join(",", mergedRules.Select((PostprocessRuleEntry x) => x?.Tag ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x)))));
			string displayName = string.IsNullOrWhiteSpace(npcName) ? (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC") : npcName.Trim();
			string normalizedHistory = NormalizePlayerNameForScenePostprocess(string.IsNullOrWhiteSpace(historyText) ? "（无）" : historyText.Trim(), displayName);
			string tagRules = BuildPostprocessRuleTextForScene(mergedRules);
			string moodRules = BuildPostprocessRuleTextForScene(AIConfigHandler.ActionPostprocessMoodRules);
			string sharedItemList = "（无）";
			string playerItemList = "（无）";
			string debtHint = "（无）";
			string marriagePlayerCandidates = null;
			string marriageTargetCandidates = null;
			string runtimeContext = "（无）";
			List<RewardSystemBehavior.RewardItemInfo> rewardOptions = null;
			List<RewardSystemBehavior.RewardItemInfo> rewardAllOptions = null;
			int rewardAvailableGold = 0;
			List<MyBehavior.PartyTransferPromptEntry> partyTransferTroopOptions = null;
			List<MyBehavior.PartyTransferPromptEntry> partyTransferPrisonerOptions = null;
			List<MyBehavior.PartyTransferPromptEntry> partyTransferAllTroopOptions = null;
			List<MyBehavior.PartyTransferPromptEntry> partyTransferAllPrisonerOptions = null;
			List<MyBehavior.SettlementTransferPromptEntry> settlementTransferNpcOptions = null;
			List<MyBehavior.SettlementTransferPromptEntry> settlementTransferAllNpcOptions = null;
			MentionedWorldEntities promptListMentions = AIConfigHandler.GetLatestAuxiliaryMentionedEntitiesForExternal();
			int promptListMax = PromptListRetrievalService.GetMaxCandidateCount();
			if (transactionPostprocessEnabled && RewardSystemBehavior.Instance != null)
			{
				try
				{
					playerItemList = RewardSystemBehavior.Instance.BuildVisibleEquipmentPostprocessListForAI(Hero.MainHero, promptListMentions, promptListMax);
				}
				catch
				{
					playerItemList = "赤身裸体";
				}
				try
				{
					if (targetHero != null)
					{
						if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope, targetHero, targetCharacter, -1, out rewardAllOptions))
						{
							rewardAllOptions = RewardSystemBehavior.Instance.BuildHeroRewardPostprocessItems(targetHero);
							PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope, targetHero, targetCharacter, -1, rewardAllOptions);
						}
						if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsSnapshotScope, targetHero, targetCharacter, -1, out rewardOptions))
						{
							rewardOptions = PromptListRetrievalService.FilterNpcRewardItemsForAssetTransfer(rewardAllOptions, promptListMentions, promptListMax);
						}
						if (exposeAllRewardItems)
						{
							rewardOptions = (rewardAllOptions ?? new List<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null && x.Count > 0).ToList();
						}
						rewardAvailableGold = RewardSystemBehavior.Instance.GetRewardPostprocessGoldForHero(targetHero);
						sharedItemList = BuildRewardPostprocessItemListForScene(rewardOptions, rewardAvailableGold, rewardAllOptions);
						debtHint = NormalizePlayerNameForScenePostprocess(RewardSystemBehavior.Instance.BuildDebtHintForAI(targetHero), displayName);
					}
					else if (targetCharacter != null)
					{
						if (TryResolveWildernessNonHeroRewardParty(targetHero, targetCharacter, targetAgentIndex, out var party))
						{
							rewardAllOptions = RewardSystemBehavior.Instance.BuildPartyRewardPostprocessItems(party);
							PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.PartyRewardItemsAllSnapshotScope, null, targetCharacter, -1, rewardAllOptions);
							rewardOptions = PromptListRetrievalService.FilterRewardItems(rewardAllOptions, promptListMentions, promptListMax);
							if (exposeAllRewardItems)
							{
								rewardOptions = (rewardAllOptions ?? new List<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null && x.Count > 0).ToList();
							}
							rewardAvailableGold = RewardSystemBehavior.Instance.GetPartyTradeGoldForExternal(party);
							sharedItemList = BuildRewardPostprocessItemListForScene(rewardOptions, rewardAvailableGold, rewardAllOptions);
							debtHint = "（非hero野外部队没有个人赊账账本；如果要归还已经实际收到的物品或第纳尔，请只从上面的部队库存与资金中输出 GIVE 标签。）";
						}
						else
						{
							if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsAllSnapshotScope, null, targetCharacter, -1, out rewardAllOptions))
							{
								rewardAllOptions = RewardSystemBehavior.Instance.BuildSettlementMerchantPostprocessItems(targetCharacter);
								PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsAllSnapshotScope, null, targetCharacter, -1, rewardAllOptions);
							}
							if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsSnapshotScope, null, targetCharacter, -1, out rewardOptions))
							{
								rewardOptions = PromptListRetrievalService.FilterRewardItems(rewardAllOptions, promptListMentions, promptListMax);
							}
							if (exposeAllRewardItems)
							{
								rewardOptions = (rewardAllOptions ?? new List<RewardSystemBehavior.RewardItemInfo>()).Where((RewardSystemBehavior.RewardItemInfo x) => x != null && x.Count > 0).ToList();
							}
							rewardAvailableGold = RewardSystemBehavior.Instance.GetSettlementMarketTradeGold(Settlement.CurrentSettlement);
							sharedItemList = BuildRewardPostprocessItemListForScene(rewardOptions, rewardAvailableGold, rewardAllOptions);
							debtHint = NormalizePlayerNameForScenePostprocess(RewardSystemBehavior.Instance.BuildSettlementMerchantDebtHintForAI(targetCharacter), displayName);
						}
					}
				}
				catch
				{
					sharedItemList = "（无）";
					debtHint = "（无）";
				rewardOptions = null;
				rewardAllOptions = null;
				}
			}
			else if (duelRuleInjected)
			{
				sharedItemList = BuildDuelPostprocessItemListForScene(null);
				try
				{
					if (RewardSystemBehavior.Instance != null && Hero.MainHero != null)
					{
						playerItemList = RewardSystemBehavior.Instance.BuildVisibleEquipmentPostprocessListForAI(Hero.MainHero, promptListMentions, promptListMax);
					}
				}
				catch
				{
					playerItemList = "赤身裸体";
				}
			}
			if (partyTransferRuleInjected)
			{
				try
				{
					List<MyBehavior.PartyTransferPromptEntry> list = MyBehavior.BuildPartyTransferPromptEntriesForExternal(targetHero, targetCharacter, targetAgentIndex);
					int recruitMaxTier = ResolvePartyTransferRecruitMaxTierForScene(targetHero, targetCharacter);
					bool hasTroopSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferTroopOptions);
					bool hasPrisonerSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferPrisonerOptions);
					bool hasAllTroopSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferAllTroopOptions);
					bool hasAllPrisonerSnapshot = PromptListRetrievalService.TryGetPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, out partyTransferAllPrisonerOptions);
					bool wildernessNonHeroPartyTransfer = MyBehavior.IsWildernessNonHeroPartyTransferEligibleForExternal(targetHero, targetCharacter, targetAgentIndex);
					IEnumerable<MyBehavior.PartyTransferPromptEntry> troopOptions = (recruitMaxTier > 0) ? list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcTroops && Math.Max(0, x.Character?.Tier ?? 0) > 0 && Math.Max(0, x.Character?.Tier ?? 0) <= recruitMaxTier) : Enumerable.Empty<MyBehavior.PartyTransferPromptEntry>();
					IEnumerable<MyBehavior.PartyTransferPromptEntry> volunteerOptions = list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcVolunteers);
					if (!hasTroopSnapshot || !hasPrisonerSnapshot)
					{
						if (!hasTroopSnapshot)
						{
							partyTransferTroopOptions = BuildDisplayIndexedPartyTransferEntriesForScene(troopOptions.Concat(volunteerOptions));
							partyTransferTroopOptions = PromptListRetrievalService.FilterPartyTransferEntries(partyTransferTroopOptions, promptListMentions, promptListMax, isPrisoner: false);
							PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferTroopOptions);
						}
						if (!hasPrisonerSnapshot)
						{
							partyTransferPrisonerOptions = BuildDisplayIndexedPartyTransferEntriesForScene(list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcPrisoners));
							partyTransferPrisonerOptions = PromptListRetrievalService.FilterPartyTransferEntries(partyTransferPrisonerOptions, promptListMentions, promptListMax, isPrisoner: true);
							PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferPrisonerOptions);
						}
					}
					if (!hasAllTroopSnapshot)
					{
						IEnumerable<MyBehavior.PartyTransferPromptEntry> allTroopOptions = wildernessNonHeroPartyTransfer
							? troopOptions.Concat(volunteerOptions)
							: list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && (x.Section == MyBehavior.PartyTransferEntrySection.NpcTroops || x.Section == MyBehavior.PartyTransferEntrySection.NpcVolunteers));
						partyTransferAllTroopOptions = BuildDisplayIndexedPartyTransferEntriesForScene(allTroopOptions);
						PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllTroopsSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferAllTroopOptions);
					}
					if (!hasAllPrisonerSnapshot)
					{
						partyTransferAllPrisonerOptions = BuildDisplayIndexedPartyTransferEntriesForScene(list.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Section == MyBehavior.PartyTransferEntrySection.NpcPrisoners));
						PromptListRetrievalService.PublishPartyTransferSnapshot(PromptListRetrievalService.PartyTransferAllPrisonersSnapshotScope, targetHero, targetCharacter, targetAgentIndex, partyTransferAllPrisonerOptions);
					}
					runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildPartyTransferNpcPostprocessListForScene(partyTransferTroopOptions, partyTransferPrisonerOptions, partyTransferAllTroopOptions, partyTransferAllPrisonerOptions));
				}
				catch
				{
					partyTransferTroopOptions = null;
					partyTransferPrisonerOptions = null;
					partyTransferAllTroopOptions = null;
					partyTransferAllPrisonerOptions = null;
				}
			}
			if (rewardRuleInjected)
			{
				try
				{
					List<MyBehavior.SettlementTransferPromptEntry> list2 = MyBehavior.BuildSettlementTransferPromptEntriesForExternal(targetHero, targetCharacter);
					if (!PromptListRetrievalService.TryGetSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferAllNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, out settlementTransferAllNpcOptions))
					{
						settlementTransferAllNpcOptions = BuildDisplayIndexedSettlementTransferEntriesForScene(list2.Where((MyBehavior.SettlementTransferPromptEntry x) => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.NpcFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x)));
						PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferAllNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, settlementTransferAllNpcOptions);
					}
					if (!PromptListRetrievalService.TryGetSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, out settlementTransferNpcOptions))
					{
						settlementTransferNpcOptions = BuildDisplayIndexedSettlementTransferEntriesForScene(list2.Where((MyBehavior.SettlementTransferPromptEntry x) => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.NpcFiefs && MyBehavior.IsSettlementTransferEntryValidForExternal(x)));
						settlementTransferNpcOptions = PromptListRetrievalService.FilterSettlementTransferEntries(settlementTransferNpcOptions, promptListMentions, promptListMax);
						PromptListRetrievalService.PublishSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferNpcAssetsSnapshotScope, targetHero, targetCharacter, -1, settlementTransferNpcOptions);
					}
					if (exposeAllFixedAssets)
					{
						settlementTransferNpcOptions = settlementTransferAllNpcOptions;
					}
					runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, BuildSettlementTransferPostprocessListForScene(settlementTransferNpcOptions, settlementTransferAllNpcOptions));
				}
				catch
				{
					settlementTransferNpcOptions = null;
					settlementTransferAllNpcOptions = null;
				}
			}
			if (marriageRuleInjected && RomanceSystemBehavior.Instance != null)
			{
				marriagePlayerCandidates = RomanceSystemBehavior.Instance.BuildMarriagePostprocessPlayerCandidatesBlockForExternal(marriageSpeaker);
				marriageTargetCandidates = RomanceSystemBehavior.Instance.BuildMarriagePostprocessTargetCandidatesBlockForExternal(marriageSpeaker);
			}
			if (worldMapPartyCommandRuleInjected || voteDealRuleInjected || customPolicyAgendaRuleInjected || heroJoinPartyRuleInjected)
			{
				runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, entityPostprocessContext);
			}
			if (worldMapPartyCommandRuleInjected)
			{
				runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, WorldMapPartyCommandBehavior.BuildCurrentNpcCommandTasksPromptForExternal(targetHero, targetCharacter, targetAgentIndex));
			}
			if (nobleGatheringRuleInjected)
			{
				runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, TeamModuleServices.Gathering.BuildPostprocessContextForExternal(targetHero ?? targetCharacter?.HeroObject));
			}
			if (diplomacyRuleInjected)
			{
				runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, DiplomacyConversationBridge.BuildDiplomacyPostprocessContext(targetHero ?? targetCharacter?.HeroObject));
			}
			if (siegeInterventionRuleInjected)
			{
				runtimeContext = AppendPostprocessContextBlockForScene(runtimeContext, TeamModuleServices.Siege.BuildPostprocessContext(
					siegeInterventionRuleInjected,
					targetAgentIndex,
					latestReplyHasPlayerInput,
					latestReplyHasPlayerInput ? playerText : string.Empty));
			}
			string systemPrompt = AIConfigHandler.BuildActionPostprocessSystemPrompt(tagRules, moodRules, displayName, sharedItemList, playerItemList, debtHint, marriagePlayerCandidates, marriageTargetCandidates);
			string latestReplyBlock = latestReplyHasPlayerInput ? AIConfigHandler.BuildActionPostprocessLatestReplyBlock(playerText, text, displayName, normalizedHistory) : AIConfigHandler.BuildActionPostprocessLatestReplyBlock("", text, displayName, null);
			string userPrompt = BuildSceneActionPostprocessUserPrompt(actionPostprocessUserPromptTemplate, tagRules, displayName, normalizedHistory, latestReplyBlock, sharedItemList, playerItemList, debtHint, marriagePlayerCandidates, marriageTargetCandidates, runtimeContext);
			userPrompt = AfGcczShoutBridge.AppendTownPostprocessDecisionContract(userPrompt, AfGcczShoutBridge.ShouldUseTownPostprocessDecisionContract(), mergedRules);
			string fallbackText = (text + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim();
			workItem = new ConversationCourierPostprocessWorkItem(systemPrompt, userPrompt, fallbackText, runtimeTargetKingdomId, runtimeTargetHeroId, runtimeTargetCharacterId, runtimeTargetTroopId, runtimeTargetUnnamedRank, targetAgentIndex, delegate(string content)
			{
			string duelTags = duelRuleInjected ? NormalizeDuelPostprocessTagsForScene(content, null, targetHero) : "";
			string rewardTags = transactionPostprocessEnabled ? NormalizeRewardPostprocessTagsForScene(content, rewardOptions, rewardAllOptions, rewardRuleInjected ? settlementTransferNpcOptions : null, rewardRuleInjected, rewardAvailableGold) : "";
			if (persistentAdpDebtRuleInjected && !rewardRuleInjected && !loanRuleInjected)
			{
				rewardTags = KeepOnlyPersistentAdpDebtTags(rewardTags);
			}
			string kingdomTags = kingdomServiceRuleInjected ? NormalizeKingdomServicePostprocessTagsForScene(content, kingdomRules) : "";
			string royalTags = royalPostprocessRuleInjected ? NormalizeKingdomServicePostprocessTagsForScene(content, royalRules) : "";
			string vassalageTags = kingdomVassalageRuleInjected ? NormalizeVassalagePostprocessTagsForScene(content, vassalageRules) : "";
			string diplomacyTags = (diplomacyRules != null && diplomacyRules.Count > 0) ? NormalizeDiplomacyPostprocessTagsForScene(content, diplomacyRules) : "";
			string annexationTags = diplomacyTags;
			Logger.Log("CourierDelivery", "[UnifiedPostprocess] result chain=" + resolvedChainName
				+ " rawHasVASSALAGE=" + ContainsVassalageActionTagForLog(content)
				+ " normalizedHasVASSALAGE=" + ContainsVassalageActionTagForLog(vassalageTags)
				+ " rawHasKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTagForLog(content)
				+ " normalizedHasKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTagForLog(diplomacyTags)
				+ " vassalageTags=" + (string.IsNullOrWhiteSpace(vassalageTags) ? "(none)" : vassalageTags.Replace("\r", "").Replace("\n", "\\n"))
				+ " annexationTags=" + (string.IsNullOrWhiteSpace(annexationTags) ? "(none)" : annexationTags.Replace("\r", "").Replace("\n", "\\n")));
			if (kingdomVassalageRuleInjected)
			{
				VassalageDiagnosticLog.Event("postprocess.native.result", new Dictionary<string, object>
				{
					["rawContent"] = VassalageDiagnosticLog.Preview(content, 4000),
					["normalizedVassalageTags"] = vassalageTags,
					["targetHero"] = VassalageDiagnosticLog.DescribeHero(targetHero),
					["targetCharacterId"] = targetCharacter?.StringId ?? ""
				});
			}
			string lordsHallTags = lordsHallRuleInjected ? NormalizeLordsHallAccessPostprocessTagsForScene(content, lordsHallRules) : "";
			string meetingReleaseTags = meetingReleaseRuleInjected ? NormalizeEncounterReleasePostprocessTagsForScene(content, meetingReleaseRules) : "";
			string vanillaIssueTags = vanillaIssueRuleInjected ? NormalizeVanillaIssuePostprocessTagsForScene(content, vanillaIssueRules) : "";
			string heroJoinTags = heroJoinPartyRuleInjected ? NormalizeHeroJoinPartyPostprocessTagsForScene(content, heroJoinPartyRules) : "";
			string sceneMechanismTags = sceneMechanismRuleInjected ? NormalizeSceneMechanismPostprocessTagsForScene(content, mechanismRules, new List<PostprocessSummonTarget>(), new List<PostprocessGuideTarget>()) : "";
			string nobleExecutionOrderTags = NoblePrisonerExecutionOrderBehavior.NormalizePostprocessTags(content, nobleExecutionOrderRules);
			string partyTransferTags = partyTransferRuleInjected ? NormalizePartyTransferPostprocessTagsForScene(content, partyTransferTroopOptions, partyTransferPrisonerOptions, partyTransferAllTroopOptions, partyTransferAllPrisonerOptions) : "";
			string voteDealTags = (voteDealRules != null && voteDealRules.Count > 0) ? NormalizeVoteDealPostprocessTagsForScene(content, voteDealRules) : "";
			string customPolicyAgendaTags = customPolicyAgendaRuleInjected ? NormalizeCustomPolicyAgendaPostprocessTag(content, customPolicyAgendaRules) : "";
			if (customPolicyAgendaRuleInjected || CustomPolicyAgendaActionTagRegex.IsMatch(content ?? ""))
			{
				Logger.Log("CourierDelivery", "[CustomPolicyAgendaPostprocess] chain=" + resolvedChainName + " RAW_TAG=" + CustomPolicyAgendaActionTagRegex.IsMatch(content ?? "") + " FINAL_TAG=" + !string.IsNullOrWhiteSpace(customPolicyAgendaTags));
			}
			string proposeAgendaTags = "";
			string worldMapPartyCommandTags = worldMapPartyCommandRuleInjected ? NormalizeWorldMapPartyCommandPostprocessTagsForScene(content) : "";
			string nobleGatheringTags = nobleGatheringRuleInjected ? TeamModuleServices.Gathering.NormalizeNobleGatheringPostprocessTagsForExternal(content) : "";
			string marriageTags = (marriageRuleInjected && RomanceSystemBehavior.Instance != null) ? RomanceSystemBehavior.Instance.NormalizeMarriagePostprocessTagsForExternal(content, marriageRules, marriageSpeaker) : "";
			string npcSurrenderTags = NormalizeNpcSurrenderPostprocessTagsForScene(content, npcSurrenderRules, npcSurrenderPostprocessEnabled);
			string siegeInterventionTags = siegeInterventionRuleInjected ? TeamModuleServices.Siege.NormalizePostprocessTags(siegeInterventionRuleInjected, content, siegeInterventionRules) : "";
			string intimacyTags = SexualConceptionBehavior.NormalizePostprocessTags(content, intimacyRules);
			string merged = siegeInterventionExclusive
				? MergeNormalizedPostprocessBlocksForScene(siegeInterventionTags, nobleExecutionOrderTags)
				: MergeNormalizedPostprocessBlocksForScene(duelTags, rewardTags, kingdomTags, royalTags, vassalageTags, lordsHallTags, meetingReleaseTags, vanillaIssueTags, heroJoinTags, sceneMechanismTags, nobleExecutionOrderTags, partyTransferTags, voteDealTags, customPolicyAgendaTags, diplomacyTags, worldMapPartyCommandTags, nobleGatheringTags, marriageTags, proposeAgendaTags, npcSurrenderTags, siegeInterventionTags, intimacyTags);
			merged = AfGcczShoutBridge.ValidateTownPostprocessDecision(merged);
			if (string.IsNullOrWhiteSpace(merged))
			{
				merged = AIConfigHandler.ActionPostprocessFallbackMoodTag;
			}
			MarkWeeklyMemoryMaterialTriggerForScene(targetHero, targetCharacter, displayName, merged, targetAgentIndex, rewardAllOptions ?? rewardOptions, partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferAllNpcOptions ?? settlementTransferNpcOptions, forceLooseWeeklyMemoryMaterialSession, partyTransferAllTroopOptions, partyTransferAllPrisonerOptions);
			string final = (text + "\n" + merged).Trim();
			Logger.Log("CourierDelivery", "[UnifiedPostprocess] RAW=\n" + content + "\nFINAL=\n" + final + "\n");
			return final;
			});
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("CourierDelivery", "[UnifiedPostprocess] exception: " + ex);
			immediateResult = (text + "\n" + AIConfigHandler.ActionPostprocessFallbackMoodTag).Trim();
			return false;
		}
		finally
		{
			AIConfigHandler.ClearGuardrailRuntimeTarget();
		}
	}
internal static void AppendCompactPartyTransferPostprocessSectionForScene(StringBuilder sb, string header, IEnumerable<MyBehavior.PartyTransferPromptEntry> entries, bool isPrisoner)
	{
		if (sb == null)
		{
			return;
		}
		sb.AppendLine(header);
		List<MyBehavior.PartyTransferPromptEntry> list = (entries ?? Enumerable.Empty<MyBehavior.PartyTransferPromptEntry>()).Where((MyBehavior.PartyTransferPromptEntry x) => x != null).ToList();
		if (list.Count == 0)
		{
			sb.AppendLine("（无）");
			return;
		}
		if (isPrisoner)
		{
			foreach (IGrouping<string, MyBehavior.PartyTransferPromptEntry> item in
				from x in list
				let groupName = x.IsHero ? "贵族/英雄俘虏" : ("T" + GetScenePartyTransferTroopTier(x) + " " + MyBehavior.GetPartyTransferTroopTypeLabelForExternal(x.Character))
				group x by groupName into g
				orderby g.Any((MyBehavior.PartyTransferPromptEntry x) => x.IsHero) descending, g.Max(GetScenePartyTransferTroopTier) descending, g.Key
				select g)
			{
				string text = string.Join("、",
					from x in item
					orderby x.DisplayName
					select BuildCompactPartyTransferEntryTextForScene(x, isPrisoner: true) into x
					where !string.IsNullOrWhiteSpace(x)
					select x);
				if (!string.IsNullOrWhiteSpace(text))
				{
					sb.AppendLine(item.Key + "：" + text);
				}
			}
			return;
		}
		foreach (IGrouping<string, MyBehavior.PartyTransferPromptEntry> item2 in
			from x in list
			let tier = GetScenePartyTransferTroopTier(x)
			let type = MyBehavior.GetPartyTransferTroopTypeLabelForExternal(x.Character)
			group x by "T" + tier + " " + type into g
			orderby g.Max(GetScenePartyTransferTroopTier) descending, g.Key
			select g)
		{
			string text2 = string.Join("、",
				from x in item2
				orderby x.DisplayName
				select BuildCompactPartyTransferEntryTextForScene(x, isPrisoner: false) into x
				where !string.IsNullOrWhiteSpace(x)
				select x);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				sb.AppendLine(item2.Key + "：" + text2);
			}
		}
	}

internal static void AppendCompactSettlementTransferPostprocessSectionForScene(StringBuilder sb, string header, IEnumerable<MyBehavior.SettlementTransferPromptEntry> entries)
	{
		if (sb == null)
		{
			return;
		}
		sb.AppendLine(header);
		List<MyBehavior.SettlementTransferPromptEntry> list = (entries ?? Enumerable.Empty<MyBehavior.SettlementTransferPromptEntry>()).Where(MyBehavior.IsSettlementTransferEntryValidForExternal).ToList();
		if (list.Count == 0)
		{
			sb.AppendLine("（无）");
			return;
		}
		foreach (MyBehavior.SettlementTransferPromptEntry item in list)
		{
			sb.AppendLine(item.PromptIndex + ". " + MyBehavior.GetSettlementTransferAssetDisplayNameForExternal(item) + " | ID " + (string.IsNullOrWhiteSpace(MyBehavior.GetSettlementTransferAssetIdForExternal(item)) ? "未知" : MyBehavior.GetSettlementTransferAssetIdForExternal(item)) + " | 类型 " + (string.IsNullOrWhiteSpace(item.TypeLabel) ? "固定资产" : item.TypeLabel.Trim()) + " | 每日收益 " + Math.Max(0, item.DailyIncomeDenars) + " 第纳尔 | 一次结清指导价 " + Math.Max(0, item.GuidePriceDenars) + " 第纳尔");
		}
	}

internal static string AppendPostprocessContextBlockForScene(string current, string block)
	{
		string text = (current ?? "").Trim();
		string text2 = (block ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2) || text2 == "（无）")
		{
			return string.IsNullOrWhiteSpace(text) ? "（无）" : text;
		}
		if (string.IsNullOrWhiteSpace(text) || text == "（无）")
		{
			return text2;
		}
		if (string.Equals(text, text2, StringComparison.Ordinal)
			|| text.StartsWith(text2 + "\n\n", StringComparison.Ordinal)
			|| text.EndsWith("\n\n" + text2, StringComparison.Ordinal)
			|| text.IndexOf("\n\n" + text2 + "\n\n", StringComparison.Ordinal) >= 0)
		{
			return text;
		}
		return text + "\n\n" + text2;
	}

internal static List<PostprocessRuleEntry> BuildAutoGroupRelayPostprocessRulesForScene(bool enabled, bool useSingleFramedNpcDescription)
	{
		if (!enabled)
		{
			return null;
		}
		List<PostprocessRuleEntry> rules = AIConfigHandler.GetGuardrailRulePostprocessRules(AutoGroupRelayRuleId) ?? new List<PostprocessRuleEntry>();
		rules = rules.Where((PostprocessRuleEntry x) => x != null && string.Equals((x.Tag ?? "").Trim(), AutoGroupRelayTagTemplate, StringComparison.OrdinalIgnoreCase))
			.Select((PostprocessRuleEntry x) => new PostprocessRuleEntry
			{
				Tag = (x.Tag ?? "").Trim(),
				Description = useSingleFramedNpcDescription && !string.IsNullOrWhiteSpace(x.SingleFramedNpcDescription)
					? x.SingleFramedNpcDescription.Trim()
					: (x.Description ?? "").Trim(),
				SingleFramedNpcDescription = (x.SingleFramedNpcDescription ?? "").Trim()
			})
			.ToList();
		if (rules.Count > 0)
		{
			return rules;
		}
		return new List<PostprocessRuleEntry>
		{
			new PostprocessRuleEntry
			{
				Tag = AutoGroupRelayTagTemplate,
				Description = "仅当根据<latest_reply>中NPC本轮发言后需要决定是否继续接话时输出。接力编号必须来自运行时补充事实里的【站在你旁边的人】；选择当前发言者自己的编号表示结束接力，此后不再有人发言。不要编造列表外编号，最多输出一个。"
			}
		};
	}

internal static string BuildCompactPartyTransferEntryTextForScene(MyBehavior.PartyTransferPromptEntry entry, bool isPrisoner)
	{
		if (entry == null)
		{
			return "";
		}
		string text = (entry.DisplayName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int num = Math.Max(0, entry.Count);
		if (num <= 0)
		{
			return "";
		}
		if (isPrisoner && entry.IsHero)
		{
			string sourceLabel = MyBehavior.GetPartyTransferPrisonerSourceLabelForExternal(entry);
			return entry.PromptIndex + ". " + text + "x1" + (string.IsNullOrWhiteSpace(sourceLabel) ? "" : ("（来源：" + sourceLabel + "）"));
		}
		string text2 = entry.Section == MyBehavior.PartyTransferEntrySection.NpcVolunteers ? "（原版要人募兵）" : "";
		return entry.PromptIndex + ". " + text + "x" + num + text2;
	}

internal static List<MyBehavior.PartyTransferPromptEntry> BuildDisplayIndexedPartyTransferEntriesForScene(IEnumerable<MyBehavior.PartyTransferPromptEntry> entries)
	{
		List<MyBehavior.PartyTransferPromptEntry> list = new List<MyBehavior.PartyTransferPromptEntry>();
		int num = 1;
		foreach (MyBehavior.PartyTransferPromptEntry entry in entries ?? Enumerable.Empty<MyBehavior.PartyTransferPromptEntry>())
		{
			if (entry == null)
			{
				continue;
			}
			list.Add(new MyBehavior.PartyTransferPromptEntry
			{
				PromptIndex = num++,
				Section = entry.Section,
				Character = entry.Character,
				DisplayName = entry.DisplayName,
				Count = entry.Count,
				WoundedCount = entry.WoundedCount,
				WageDenarsPerDay = entry.WageDenarsPerDay,
				HirePriceDenarsPerUnit = entry.HirePriceDenarsPerUnit,
				BuyPriceDenarsPerUnit = entry.BuyPriceDenarsPerUnit,
				IsHero = entry.IsHero,
				OwnerParty = entry.OwnerParty,
				SourceSettlement = entry.SourceSettlement,
				VolunteerOwner = entry.VolunteerOwner,
				VolunteerSlotIndices = entry.VolunteerSlotIndices == null ? null : new List<int>(entry.VolunteerSlotIndices)
			});
		}
		return list;
	}

internal static List<MyBehavior.SettlementTransferPromptEntry> BuildDisplayIndexedSettlementTransferEntriesForScene(IEnumerable<MyBehavior.SettlementTransferPromptEntry> entries)
	{
		List<MyBehavior.SettlementTransferPromptEntry> list = new List<MyBehavior.SettlementTransferPromptEntry>();
		int num = 1;
		foreach (MyBehavior.SettlementTransferPromptEntry entry in entries ?? Enumerable.Empty<MyBehavior.SettlementTransferPromptEntry>())
		{
			if (!MyBehavior.IsSettlementTransferEntryValidForExternal(entry))
			{
				continue;
			}
			list.Add(new MyBehavior.SettlementTransferPromptEntry
			{
				PromptIndex = num++,
				Section = entry.Section,
				AssetKind = entry.AssetKind,
				Settlement = entry.Settlement,
				Workshop = entry.Workshop,
				CaravanParty = entry.CaravanParty,
				OwnerHero = entry.OwnerHero,
				SettlementId = entry.SettlementId,
				AssetId = entry.AssetId,
				DisplayName = entry.DisplayName,
				TypeLabel = entry.TypeLabel,
				DailyIncomeDenars = entry.DailyIncomeDenars,
				GuidePriceDenars = entry.GuidePriceDenars,
				OwnerClan = entry.OwnerClan
			});
		}
		return list;
	}

internal static string BuildDuelPostprocessItemListForScene(List<RewardSystemBehavior.DuelStakeOption> options)
	{
		if (options == null || options.Count == 0)
		{
			return "（无）";
		}
		StringBuilder stringBuilder = new StringBuilder();
		List<RewardSystemBehavior.DuelStakeOption> list = options.Where((RewardSystemBehavior.DuelStakeOption x) => x != null && !x.IsPrivateEquipment).ToList();
		List<RewardSystemBehavior.DuelStakeOption> list2 = options.Where((RewardSystemBehavior.DuelStakeOption x) => x != null && x.IsPrivateEquipment).ToList();
		if (list.Count > 0)
		{
			stringBuilder.AppendLine("库存物品：");
			foreach (RewardSystemBehavior.DuelStakeOption duelStakeOption in list)
			{
				stringBuilder.Append(duelStakeOption.Name ?? duelStakeOption.ItemId ?? "未知物品")
					.Append(" x")
					.Append(Math.Max(1, duelStakeOption.Count))
					.Append(" | guidePrice=")
					.Append(Math.Max(1, duelStakeOption.GuidePrice))
					.AppendLine();
			}
		}
		if (list2.Count > 0)
		{
			stringBuilder.AppendLine("私人装备：");
			foreach (RewardSystemBehavior.DuelStakeOption duelStakeOption2 in list2)
			{
				stringBuilder.Append(duelStakeOption2.Name ?? duelStakeOption2.ItemId ?? "未知物品")
					.Append(" x")
					.Append(Math.Max(1, duelStakeOption2.Count))
					.Append(" | guidePrice=")
					.Append(Math.Max(1, duelStakeOption2.GuidePrice))
					.AppendLine();
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

internal static List<PostprocessRuleEntry> BuildNpcSurrenderPostprocessRulesForScene(bool enabled)
	{
		if (!enabled)
		{
			return null;
		}
		return (AIConfigHandler.WildernessPostprocessRules ?? new List<PostprocessRuleEntry>())
			.Where((PostprocessRuleEntry x) => string.Equals((x?.Tag ?? "").Trim(), NpcSurrenderActionTag, StringComparison.OrdinalIgnoreCase))
			.ToList();
	}

internal static string BuildPartyTransferNpcPostprocessListForScene(List<MyBehavior.PartyTransferPromptEntry> troopOptions, List<MyBehavior.PartyTransferPromptEntry> prisonerOptions, List<MyBehavior.PartyTransferPromptEntry> allTroopOptions = null, List<MyBehavior.PartyTransferPromptEntry> allPrisonerOptions = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendCompactPartyTransferPostprocessSectionForScene(stringBuilder, "【你当前可转移或可招募部队】：", troopOptions, isPrisoner: false);
		List<MyBehavior.PartyTransferPromptEntry> allTroops = (allTroopOptions ?? troopOptions ?? new List<MyBehavior.PartyTransferPromptEntry>()).Where((MyBehavior.PartyTransferPromptEntry x) => x != null).ToList();
		stringBuilder.Append("全部非Hero士兵: ").Append(allTroops.Sum((MyBehavior.PartyTransferPromptEntry x) => Math.Max(0, x.Count))).Append(" 人 | 雇佣指导总值: ").Append(MyBehavior.CalculatePartyTransferTotalValueForExternal(allTroops, isPrisoner: false)).AppendLine(" 第纳尔");
		AppendCompactPartyTransferPostprocessSectionForScene(stringBuilder, "【你当前可转移俘虏】：", prisonerOptions, isPrisoner: true);
		List<MyBehavior.PartyTransferPromptEntry> allPrisoners = (allPrisonerOptions ?? prisonerOptions ?? new List<MyBehavior.PartyTransferPromptEntry>()).Where((MyBehavior.PartyTransferPromptEntry x) => x != null).ToList();
		stringBuilder.Append("全部俘虏: ").Append(allPrisoners.Sum((MyBehavior.PartyTransferPromptEntry x) => Math.Max(0, x.Count))).Append(" 人 | 购买指导总值: ").Append(MyBehavior.CalculatePartyTransferTotalValueForExternal(allPrisoners, isPrisoner: true)).AppendLine(" 第纳尔");
		return stringBuilder.ToString().TrimEnd();
	}

internal static List<PostprocessRuleEntry> BuildPersistentAdpDebtPostprocessRules()
	{
		List<PostprocessRuleEntry> result = new List<PostprocessRuleEntry>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PostprocessRuleEntry rule in MergePostprocessRulesForScene(AIConfigHandler.LoanPostprocessRules, AIConfigHandler.RewardPostprocessRules))
		{
			string tag = (rule?.Tag ?? "").Trim();
			if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith("[ADP:", StringComparison.OrdinalIgnoreCase) || !seen.Add(tag))
			{
				continue;
			}
			string description = (rule.Description ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(description))
			{
				description += "；";
			}
			description += "仅限当前债务提示中列出的、玩家欠当前聊天NPC对象的债务ID；只有本轮系统事实或NPC明确表示对应债务已偿还、收到、豁免或免除时输出；禁止用于创建新债务。";
			result.Add(new PostprocessRuleEntry
			{
				Tag = tag,
				Description = description
			});
		}
		if (result.Count == 0)
		{
			result.Add(new PostprocessRuleEntry
			{
				Tag = "[ADP:债务ID]",
				Description = "仅限当前债务提示中列出的、玩家欠当前聊天NPC对象的债务ID；只有本轮系统事实或NPC明确表示对应债务已偿还、收到、豁免或免除时输出；禁止用于创建新债务。"
			});
		}
		return result;
	}

internal static string BuildPostprocessRuleTextForScene(IEnumerable<PostprocessRuleEntry> entries)
	{
		if (entries == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (PostprocessRuleEntry entry in entries)
		{
			if (entry == null || string.IsNullOrWhiteSpace(entry.Tag))
			{
				continue;
			}
			stringBuilder.Append(entry.Tag.Trim());
			if (!string.IsNullOrWhiteSpace(entry.Description))
			{
				stringBuilder.Append("：").Append(entry.Description.Trim());
			}
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString().TrimEnd();
	}

internal static string BuildRewardPostprocessItemListForScene(List<RewardSystemBehavior.RewardItemInfo> options, int gold, List<RewardSystemBehavior.RewardItemInfo> allOptions = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("第纳尔: ").Append(Math.Max(0, gold)).AppendLine();
		if (options == null || options.Count == 0)
		{
			stringBuilder.Append("全部可转物品总值: ").Append(RewardSystemBehavior.CalculateRewardItemsTotalValueForExternal(allOptions)).AppendLine(" 第纳尔（不含第纳尔）");
			return stringBuilder.ToString().TrimEnd();
		}
		List<RewardSystemBehavior.RewardItemInfo> list = options.Where((RewardSystemBehavior.RewardItemInfo x) => x != null && !x.IsPrivateEquipment).ToList();
		List<RewardSystemBehavior.RewardItemInfo> list2 = options.Where((RewardSystemBehavior.RewardItemInfo x) => x != null && x.IsPrivateEquipment).ToList();
		if (list.Count > 0)
		{
			stringBuilder.AppendLine("库存物品：");
			foreach (RewardSystemBehavior.RewardItemInfo item in list)
			{
				stringBuilder.Append(item.Name ?? item.PromptStringId ?? item.StringId ?? "未知物品")
					.Append(string.IsNullOrWhiteSpace(item.PromptStringId) || string.Equals(item.PromptStringId, item.StringId, StringComparison.OrdinalIgnoreCase) ? "" : (" | id=" + item.PromptStringId))
					.Append(" x")
					.Append(Math.Max(1, item.Count))
					.Append(" | guidePrice=")
					.Append(Math.Max(1, item.GuidePrice))
					.AppendLine();
			}
		}
		if (list2.Count > 0)
		{
			stringBuilder.AppendLine("私人战斗装备：");
			foreach (RewardSystemBehavior.RewardItemInfo item2 in list2)
			{
				stringBuilder.Append(item2.Name ?? item2.PromptStringId ?? item2.StringId ?? "未知物品")
					.Append(" x")
					.Append(Math.Max(1, item2.Count))
					.Append(" | guidePrice=")
					.Append(Math.Max(1, item2.GuidePrice))
					.AppendLine();
			}
		}
		stringBuilder.Append("全部可转物品总值: ").Append(RewardSystemBehavior.CalculateRewardItemsTotalValueForExternal(allOptions ?? options)).AppendLine(" 第纳尔（不含第纳尔）");
		return stringBuilder.ToString().TrimEnd();
	}

internal static List<PostprocessRuleEntry> BuildRuntimeDiplomacyPostprocessRulesForScene(Hero targetHero, CharacterObject targetCharacter)
	{
		bool allowRoyalDiplomacy = DiplomacyConversationBridge.CanUseFullDiplomacyActionPostprocessForExternal(targetHero, targetCharacter);
		bool allowNpcDeclareWar = DiplomacyConversationBridge.CanUseNpcSovereignDeclareWarPostprocessForExternal(targetHero, targetCharacter);
		bool allowIndependentClanPeace = DiplomacyConversationBridge.CanUseIndependentClanPeaceForExternal(targetHero, targetCharacter);
		List<PostprocessRuleEntry> diplomacyRules = (AIConfigHandler.GetGuardrailRulePostprocessRules("diplomacy") ?? new List<PostprocessRuleEntry>())
			.Where((PostprocessRuleEntry rule) =>
			{
				string tag = (rule?.Tag ?? "").Trim();
				if (tag.StartsWith("[ACTION:KINGDOM_ANNEX:", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				if (tag.StartsWith("[ACTION:DIPLOMACY:DECLARE_WAR:", StringComparison.OrdinalIgnoreCase))
				{
					return allowRoyalDiplomacy || allowNpcDeclareWar;
				}
				return DiplomacyConversationBridge.IsIndependentClanPeacePostprocessTag(tag)
					? allowIndependentClanPeace
					: allowRoyalDiplomacy;
			})
			.Select((PostprocessRuleEntry rule) =>
			{
				string tag = (rule?.Tag ?? "").Trim();
				if (!allowRoyalDiplomacy
					&& allowNpcDeclareWar
					&& tag.StartsWith("[ACTION:DIPLOMACY:DECLARE_WAR:", StringComparison.OrdinalIgnoreCase))
				{
					return new PostprocessRuleEntry
					{
						Tag = rule.Tag,
						Description = "仅当NPC国王在<latest_reply>中明确同意让自己的王国立即向第三方王国宣战、开战、讨伐或出兵时输出。玩家不是国王，不能以自己的名义或代表所属王国宣战。id1必须是NPC王国ID，id2必须是目标第三方王国ID；两者都从运行时事实复制。",
						SingleFramedNpcDescription = rule.SingleFramedNpcDescription,
						RuntimeAllowedParameterValues = rule.RuntimeAllowedParameterValues
					};
				}
				return rule;
			})
			.ToList();
		List<PostprocessRuleEntry> annexationRules = allowRoyalDiplomacy
			? (KingdomAnnexationBehavior.BuildRuntimeAnnexationPostprocessRulesForExternal(targetHero, targetCharacter) ?? new List<PostprocessRuleEntry>())
			: new List<PostprocessRuleEntry>();
		return MergePostprocessRulesForScene(diplomacyRules, annexationRules);
	}

internal static string BuildSceneActionPostprocessUserPrompt(string userPromptTemplate, string tagRules, string npcName, string historyText, string latestReplyBlock, string sharedItemList = null, string playerItemList = null, string debtHint = null, string marriagePlayerCandidates = null, string marriageTargetCandidates = null, string runtimeContext = null)
	{
		return AIConfigHandler.BuildActionPostprocessUserPrompt(userPromptTemplate, tagRules, npcName, string.IsNullOrWhiteSpace(historyText) ? "（无）" : historyText.Trim(), string.IsNullOrWhiteSpace(latestReplyBlock) ? "玩家: （无）\nNPC: （无）" : latestReplyBlock.Trim(), sharedItemList, playerItemList, debtHint, marriagePlayerCandidates, marriageTargetCandidates, runtimeContext);
	}

internal static string BuildSceneMechanismTargetListForPostprocess(List<PostprocessSummonTarget> summonTargets, List<PostprocessGuideTarget> guideTargets)
	{
		StringBuilder stringBuilder = new StringBuilder();
		List<string> list = new List<string>();
		HashSet<int> hashSet = new HashSet<int>();
		foreach (PostprocessSummonTarget item in summonTargets ?? new List<PostprocessSummonTarget>())
		{
			if (item != null && item.PromptId > 0 && !string.IsNullOrWhiteSpace(item.DisplayName) && hashSet.Add(item.PromptId))
			{
				list.Add(item.PromptId + " " + item.DisplayName.Trim() + " " + ((item.LocationCode ?? "处").Trim()));
			}
		}
		foreach (PostprocessGuideTarget item2 in guideTargets ?? new List<PostprocessGuideTarget>())
		{
			if (item2 != null && item2.PromptId > 0 && !string.IsNullOrWhiteSpace(item2.DisplayName) && hashSet.Add(item2.PromptId))
			{
				list.Add(item2.PromptId + " " + item2.DisplayName.Trim() + " " + ((item2.LocationCode ?? "处").Trim()));
			}
		}
		if (list.Count == 0)
		{
			return "（无）";
		}
		stringBuilder.AppendLine("【带路与传唤NPC清单】：");
		foreach (string item3 in list)
		{
			stringBuilder.AppendLine(item3);
		}
		return stringBuilder.ToString().Trim();
	}

internal static string BuildSceneRelayNpcListLineForPostprocess(NpcDataPacket npc, bool isPrimaryTarget = false, bool isCurrentSpeaker = false)
	{
		if (npc == null || npc.AgentIndex < 0)
		{
			return "";
		}
		string name = npc.IsHero ? GetSceneNpcIdentityNameForPrompt(npc) : GetSceneNpcGivenNameForPrompt(npc);
		if (string.IsNullOrWhiteSpace(name))
		{
			name = (npc.Name ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			name = "未知NPC";
		}
		string identity = GetSceneNpcListIdentityForPrompt(npc);
		if (string.IsNullOrWhiteSpace(identity))
		{
			identity = npc.IsHero ? "有名人物" : "场景人物";
		}
		string position = isPrimaryTarget ? "主对话对象" : (isCurrentSpeaker ? "当前发言者" : "在场人物");
		return "- 名字: " + SanitizeSceneRelayPostprocessField(name) + " | 身份: " + SanitizeSceneRelayPostprocessField(identity) + " | 对话位置: " + position + " | 接力编号: " + npc.AgentIndex;
	}

internal static string BuildSceneRelayTargetListForPostprocess(IEnumerable<NpcDataPacket> presentNpcs, int currentSpeakerAgentIndex, int primaryTargetAgentIndex = -1)
	{
		try
		{
			List<NpcDataPacket> relayNpcs = new List<NpcDataPacket>();
			HashSet<int> seen = new HashSet<int>();
			foreach (NpcDataPacket npc in presentNpcs ?? Enumerable.Empty<NpcDataPacket>())
			{
				if (npc == null || npc.AgentIndex < 0 || !seen.Add(npc.AgentIndex))
				{
					continue;
				}
				relayNpcs.Add(npc);
			}
			if (relayNpcs.Count == 0)
			{
				return "";
			}
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("【站在你旁边的人】");
			foreach (NpcDataPacket npc in relayNpcs)
			{
				string line = BuildSceneRelayNpcListLineForPostprocess(npc, npc.AgentIndex == primaryTargetAgentIndex, npc.AgentIndex == currentSpeakerAgentIndex);
				if (!string.IsNullOrWhiteSpace(line))
				{
					sb.AppendLine(line);
				}
			}
			if (currentSpeakerAgentIndex >= 0)
			{
				sb.AppendLine("【接力终止】选择当前发言者自己的接力编号（" + currentSpeakerAgentIndex + "）表示结束接力，此后不会再有NPC发言。");
			}
			return sb.ToString().TrimEnd();
		}
		catch
		{
			return "";
		}
	}

internal static string BuildSettlementTransferPostprocessListForScene(List<MyBehavior.SettlementTransferPromptEntry> npcOptions, List<MyBehavior.SettlementTransferPromptEntry> allNpcOptions = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendCompactSettlementTransferPostprocessSectionForScene(stringBuilder, "【你当前可转移固定资产】：", npcOptions);
		List<MyBehavior.SettlementTransferPromptEntry> all = (allNpcOptions ?? npcOptions ?? new List<MyBehavior.SettlementTransferPromptEntry>()).Where(MyBehavior.IsSettlementTransferEntryValidForExternal).ToList();
		stringBuilder.Append("你全部可转固定资产: ").Append(all.Count).Append(" 项 | 一次结清指导总值: ").Append(MyBehavior.CalculateSettlementTransferTotalValueForExternal(all)).AppendLine(" 第纳尔");
		return stringBuilder.ToString().TrimEnd();
	}

internal static string BuildSiegeSurrenderPostprocessContextForNativeConversation(Settlement settlement, string sideLabel)
	{
		string settlementName = settlement?.Name?.ToString() ?? "当前围城";
		string side = string.IsNullOrWhiteSpace(sideLabel) ? "当前NPC本方" : sideLabel.Trim();
		return "【当前围城投降上下文】\n围城目标：" + settlementName + "\n当前NPC归属：" + side + "\n标签：" + SiegeSurrenderActionTag;
	}

internal static List<PostprocessRuleEntry> BuildSiegeSurrenderPostprocessRulesForNativeConversation(bool enabled, Settlement settlement, BattleSideEnum targetSide)
	{
		if (!enabled)
		{
			return null;
		}
		string settlementName = settlement?.Name?.ToString() ?? "当前围城";
		string description;
		if (targetSide == BattleSideEnum.Defender)
		{
			description = "仅当NPC明确代表守方同意" + settlementName + "开城投降、交出城池或停止守城时输出；讨论、求饶、无授权禁止。";
		}
		else if (targetSide == BattleSideEnum.Attacker)
		{
			description = "仅当NPC明确代表攻城方同意在" + settlementName + "投降、撤围并接受战败处置时输出；讨论、威胁、临时撤退、无授权禁止。";
		}
		else
		{
			description = "仅当NPC明确代表当前围城本方同意投降或撤围时输出；模糊表态或无授权禁止。";
		}
		return new List<PostprocessRuleEntry>
		{
			new PostprocessRuleEntry
			{
				Tag = SiegeSurrenderActionTag,
				Description = description
			}
		};
	}

internal static bool CanUseSceneMechanismPostprocessForSpeaker(int speakerAgentIndex)
	{
		return !AIConfigHandler.ShouldExcludeSceneMoveRuleForCurrentMission()
			|| NoblePrisonerEscortBehavior.IsEscortedAgent(speakerAgentIndex)
			|| TroopInspectionBehavior.CanOfferPrisonerSlaughterActionForExternal(
				speakerAgentIndex,
				out _,
				out _);
	}

internal static bool ContainsKingdomAnnexActionTagForLog(string text)
	{
		return (text ?? "").IndexOf("[ACTION:KINGDOM_ANNEX:", StringComparison.OrdinalIgnoreCase) >= 0;
	}

internal static bool ContainsVassalageActionTagForLog(string text)
	{
		return (text ?? "").IndexOf("[ACTION:VASSALAGE:", StringComparison.OrdinalIgnoreCase) >= 0;
	}

internal static string ExtractDiplomacyPostprocessActionName(string tag)
	{
		const string prefix = "[ACTION:DIPLOMACY:";
		string value = (tag ?? "").Trim();
		if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		int start = prefix.Length;
		int colon = value.IndexOf(':', start);
		int bracket = value.IndexOf(']', start);
		int end = colon >= 0 ? colon : bracket;
		return end > start ? value.Substring(start, end - start).Trim() : "";
	}

internal static RewardSystemBehavior.DuelStakeOption FindDuelStakeOptionByTokenForScene(List<RewardSystemBehavior.DuelStakeOption> options, string token)
	{
		if (options == null || options.Count == 0 || string.IsNullOrWhiteSpace(token))
		{
			return null;
		}
		string text = token.Trim();
		RewardSystemBehavior.DuelStakeOption duelStakeOption = options.FirstOrDefault((RewardSystemBehavior.DuelStakeOption x) => x != null && string.Equals((x.Name ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (duelStakeOption != null)
		{
			return duelStakeOption;
		}
		duelStakeOption = options.FirstOrDefault((RewardSystemBehavior.DuelStakeOption x) => x != null && string.Equals((x.ItemId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (duelStakeOption != null)
		{
			return duelStakeOption;
		}
		List<RewardSystemBehavior.DuelStakeOption> list3 = options.Where((RewardSystemBehavior.DuelStakeOption x) => x != null && !string.IsNullOrWhiteSpace(x.Name) && x.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list3.Count == 1)
		{
			return list3[0];
		}
		list3 = options.Where((RewardSystemBehavior.DuelStakeOption x) => x != null && text.IndexOf(x.Name ?? "", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list3.Count == 1)
		{
			return list3[0];
		}
		return null;
	}

internal static MyBehavior.PartyTransferPromptEntry FindPartyTransferEntryByTokenForScene(List<MyBehavior.PartyTransferPromptEntry> options, string token)
	{
		if (options == null || options.Count == 0 || string.IsNullOrWhiteSpace(token))
		{
			return null;
		}
		string text = token.Trim();
		MyBehavior.PartyTransferPromptEntry partyTransferPromptEntry = options.FirstOrDefault((MyBehavior.PartyTransferPromptEntry x) => x != null && string.Equals((x.DisplayName ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (partyTransferPromptEntry != null)
		{
			return partyTransferPromptEntry;
		}
		List<MyBehavior.PartyTransferPromptEntry> list = options.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && !string.IsNullOrWhiteSpace(x.DisplayName) && x.DisplayName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list.Count == 1)
		{
			return list[0];
		}
		list = options.Where((MyBehavior.PartyTransferPromptEntry x) => x != null && !string.IsNullOrWhiteSpace(text) && text.IndexOf(x.DisplayName ?? "", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list.Count == 1)
		{
			return list[0];
		}
		return null;
	}

internal static RewardSystemBehavior.RewardItemInfo FindRewardItemByTokenForScene(List<RewardSystemBehavior.RewardItemInfo> options, string token)
	{
		if (options == null || options.Count == 0 || string.IsNullOrWhiteSpace(token))
		{
			return null;
		}
		string text = token.Trim();
		static string normalizeLooseName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return "";
			}
			return Regex.Replace(value.Trim(), "[\\s\\u3000]+", "").Replace("的", "");
		}
		RewardSystemBehavior.RewardItemInfo rewardItemInfo = options.FirstOrDefault((RewardSystemBehavior.RewardItemInfo x) => x != null && string.Equals((x.Name ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (rewardItemInfo != null)
		{
			return rewardItemInfo;
		}
		rewardItemInfo = options.FirstOrDefault((RewardSystemBehavior.RewardItemInfo x) => x != null && string.Equals((x.PromptStringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (rewardItemInfo != null)
		{
			return rewardItemInfo;
		}
		rewardItemInfo = options.FirstOrDefault((RewardSystemBehavior.RewardItemInfo x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (rewardItemInfo != null)
		{
			return rewardItemInfo;
		}
		string looseText = normalizeLooseName(text);
		if (!string.IsNullOrWhiteSpace(looseText))
		{
			List<RewardSystemBehavior.RewardItemInfo> looseMatches = options.Where((RewardSystemBehavior.RewardItemInfo x) => x != null && string.Equals(normalizeLooseName(x.Name ?? ""), looseText, StringComparison.OrdinalIgnoreCase)).ToList();
			if (looseMatches.Count == 1)
			{
				return looseMatches[0];
			}
		}
		List<RewardSystemBehavior.RewardItemInfo> list3 = options.Where((RewardSystemBehavior.RewardItemInfo x) => x != null && !string.IsNullOrWhiteSpace(x.Name) && x.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list3.Count == 1)
		{
			return list3[0];
		}
		list3 = options.Where((RewardSystemBehavior.RewardItemInfo x) => x != null && !string.IsNullOrWhiteSpace(text) && text.IndexOf(x.Name ?? "", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list3.Count == 1)
		{
			return list3[0];
		}
		return null;
	}

internal static MyBehavior.SettlementTransferPromptEntry FindSettlementTransferEntryByTokenForScene(List<MyBehavior.SettlementTransferPromptEntry> options, string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		MyBehavior.SettlementTransferPromptEntry settlementTransferPromptEntry = options?.FirstOrDefault((MyBehavior.SettlementTransferPromptEntry x) => x != null && x.PromptIndex > 0 && string.Equals(x.PromptIndex.ToString(), text, StringComparison.OrdinalIgnoreCase));
		if (settlementTransferPromptEntry != null)
		{
			return settlementTransferPromptEntry;
		}
		settlementTransferPromptEntry = options?.FirstOrDefault((MyBehavior.SettlementTransferPromptEntry x) => x != null && string.Equals(MyBehavior.GetSettlementTransferAssetIdForExternal(x), text, StringComparison.OrdinalIgnoreCase));
		if (settlementTransferPromptEntry != null)
		{
			return settlementTransferPromptEntry;
		}
		settlementTransferPromptEntry = options?.FirstOrDefault((MyBehavior.SettlementTransferPromptEntry x) => x != null && string.Equals((x.SettlementId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (settlementTransferPromptEntry != null)
		{
			return settlementTransferPromptEntry;
		}
		settlementTransferPromptEntry = options?.FirstOrDefault((MyBehavior.SettlementTransferPromptEntry x) => x != null && string.Equals((x.DisplayName ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (settlementTransferPromptEntry != null)
		{
			return settlementTransferPromptEntry;
		}
		List<MyBehavior.SettlementTransferPromptEntry> list = (options ?? new List<MyBehavior.SettlementTransferPromptEntry>()).Where((MyBehavior.SettlementTransferPromptEntry x) => x != null && !string.IsNullOrWhiteSpace(x.DisplayName) && x.DisplayName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list.Count == 1)
		{
			return list[0];
		}
		list = (options ?? new List<MyBehavior.SettlementTransferPromptEntry>()).Where((MyBehavior.SettlementTransferPromptEntry x) => x != null && !string.IsNullOrWhiteSpace(MyBehavior.GetSettlementTransferAssetIdForExternal(x)) && MyBehavior.GetSettlementTransferAssetIdForExternal(x).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list.Count == 1)
		{
			return list[0];
		}
		return null;
	}

internal static string GetSceneNpcGivenNameForPrompt(NpcDataPacket npc)
	{
		string text = (ShoutUtils.GetPromptListName(npc) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? GetSceneNpcIdentityNameForPrompt(npc) : text;
	}

internal static string GetSceneNpcIdentityNameForPrompt(NpcDataPacket npc)
	{
		string text = (ShoutUtils.GetPromptIdentityName(npc) ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "未命名NPC" : text;
	}

internal static string GetSceneNpcListIdentityForPrompt(NpcDataPacket npc)
	{
		if (npc != null && !npc.IsHero)
		{
			return GetSceneNpcIdentityNameForPrompt(npc);
		}
		string text = (npc?.RoleDesc ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? GetSceneNpcIdentityNameForPrompt(npc) : text;
	}

internal static int GetScenePartyTransferTroopTier(MyBehavior.PartyTransferPromptEntry entry)
	{
		return Math.Max(0, entry?.Character?.Tier ?? 0);
	}

internal static bool HasNpcSurrenderPostprocessRule(List<PostprocessRuleEntry> rules)
	{
		return (rules ?? new List<PostprocessRuleEntry>()).Any((PostprocessRuleEntry x) => string.Equals((x?.Tag ?? "").Trim(), NpcSurrenderActionTag, StringComparison.OrdinalIgnoreCase));
	}

internal static bool HasPreprocessRuleHit(IEnumerable<string> ruleIds, string ruleId)
	{
		string text = (ruleId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || ruleIds == null)
		{
			return false;
		}
		return ruleIds.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
	}

internal static bool HasSiegeSurrenderPostprocessRule(List<PostprocessRuleEntry> rules)
	{
		return (rules ?? new List<PostprocessRuleEntry>()).Any((PostprocessRuleEntry x) => string.Equals((x?.Tag ?? "").Trim(), SiegeSurrenderActionTag, StringComparison.OrdinalIgnoreCase));
	}

internal static bool IsNativeConversationPostprocessChain(string chainName)
	{
		return string.Equals((chainName ?? "").Trim(), "native_conversation", StringComparison.OrdinalIgnoreCase);
	}

internal static bool IsValidVoteDealPostprocessTag(string tag, HashSet<string> validTags)
	{
		if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith("[ACTION:AGENDA:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string payload = tag.Substring("[ACTION:AGENDA:".Length).TrimEnd(']');
		if (string.IsNullOrWhiteSpace(payload))
		{
			return false;
		}
		string[] parts = payload.Split(':');
		return parts.Length == 3
			&& Regex.IsMatch(parts[0], "^A[1-9][0-9]*$", RegexOptions.IgnoreCase)
			&& Regex.IsMatch(parts[1], "^O[1-9][0-9]*$", RegexOptions.IgnoreCase)
			&& Regex.IsMatch(parts[2], "^(?:SLIGHTLY_FAVOR|STRONGLY_FAVOR|FULLY_PUSH)$", RegexOptions.IgnoreCase);
	}

internal static string KeepOnlyPersistentAdpDebtTags(string tags)
	{
		if (string.IsNullOrWhiteSpace(tags))
		{
			return "";
		}
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match match in Regex.Matches(tags, "\\[(?:ADP:[^\\]\\r\\n]+|ACTION:MOOD:[^\\]\\r\\n]+)\\]", RegexOptions.IgnoreCase))
		{
			string value = (match?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
			{
				result.Add(value);
			}
		}
		return string.Join("\n", result);
	}

internal static string MergeNormalizedPostprocessBlocksForScene(params string[] blocks)
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = "";
		foreach (string block in blocks ?? Array.Empty<string>())
		{
			if (string.IsNullOrWhiteSpace(block))
			{
				continue;
			}
			string[] array = block.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text2 in array)
			{
				string text3 = (text2 ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text3))
				{
					continue;
				}
				if (text3.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
				{
					if (string.IsNullOrWhiteSpace(text))
					{
						text = text3;
					}
					continue;
				}
				if (hashSet.Add(text3))
				{
					list.Add(text3);
				}
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", list.Concat(new string[1] { text }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string MergePostprocessRuleDescription(string existingDescription, string incomingDescription)
	{
		string text = (existingDescription ?? "").Trim();
		string text2 = (incomingDescription ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		if (string.IsNullOrWhiteSpace(text2) || text.IndexOf(text2, StringComparison.Ordinal) >= 0)
		{
			return text;
		}
		return text + Environment.NewLine + text2;
	}

internal static List<PostprocessRuleEntry> MergePostprocessRulesForScene(params List<PostprocessRuleEntry>[] ruleSets)
	{
		List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		if (ruleSets == null)
		{
			return list;
		}
		foreach (List<PostprocessRuleEntry> ruleSet in ruleSets)
		{
			if (ruleSet == null)
			{
				continue;
			}
			foreach (PostprocessRuleEntry item in ruleSet)
			{
				string text = (item?.Tag ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				if (!dictionary.TryGetValue(text, out var value))
				{
					dictionary.Add(text, list.Count);
					list.Add(item);
					continue;
				}
				if (string.Equals(text, "[AD:价值:天数:N/P:备注内容]", StringComparison.OrdinalIgnoreCase))
				{
					list[value] = MergeSharedDeferredDebtPostprocessRule(list[value], item);
				}
			}
		}
		return list;
	}

internal static PostprocessRuleEntry MergeSharedDeferredDebtPostprocessRule(PostprocessRuleEntry existing, PostprocessRuleEntry incoming)
	{
		if (existing == null || incoming == null)
		{
			return existing ?? incoming;
		}
		string text = MergePostprocessRuleDescription(existing.Description, incoming.Description);
		string text2 = MergePostprocessRuleDescription(existing.SingleFramedNpcDescription, incoming.SingleFramedNpcDescription);
		if (string.Equals(text, existing.Description ?? "", StringComparison.Ordinal) && string.Equals(text2, existing.SingleFramedNpcDescription ?? "", StringComparison.Ordinal))
		{
			return existing;
		}
		return new PostprocessRuleEntry
		{
			Tag = existing.Tag,
			Description = text,
			SingleFramedNpcDescription = text2,
			RuntimeAllowedParameterValues = existing.RuntimeAllowedParameterValues
		};
	}

internal static string NormalizeAutoGroupRelayPostprocessTagsForScene(string raw, List<NpcDataPacket> relayCandidates, int currentSpeakerAgentIndex)
	{
		if (string.IsNullOrWhiteSpace(raw) || relayCandidates == null || relayCandidates.Count == 0)
		{
			return "";
		}
		HashSet<int> validIds = new HashSet<int>();
		foreach (NpcDataPacket npc in relayCandidates)
		{
			if (npc != null && npc.AgentIndex >= 0)
			{
				validIds.Add(npc.AgentIndex);
			}
		}
		if (validIds.Count == 0)
		{
			return "";
		}
		foreach (Match item in Regex.Matches(raw ?? "", "\\[RELAY\\s*:\\s*(\\d+)\\]", RegexOptions.IgnoreCase))
		{
			// The current speaker is the prompt's stop signal, not a next-turn candidate.
			if (int.TryParse(item.Groups[1].Value, out int relayTargetAgentIndex)
				&& (relayTargetAgentIndex == currentSpeakerAgentIndex || validIds.Contains(relayTargetAgentIndex)))
			{
				return "[RELAY:" + relayTargetAgentIndex + "]";
			}
		}
		return "";
	}

internal static string NormalizeCustomPolicyAgendaPostprocessTag(string raw, List<PostprocessRuleEntry> rules)
	{
		bool ruleAllowsTag = rules != null && rules.Any((PostprocessRuleEntry rule) => string.Equals((rule?.Tag ?? "").Trim(), CustomPolicyAgendaActionTag, StringComparison.OrdinalIgnoreCase));
		return ruleAllowsTag && CustomPolicyAgendaActionTagRegex.IsMatch(raw ?? "") ? CustomPolicyAgendaActionTag : "";
	}

internal static string NormalizeDiplomacyPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		HashSet<string> allowedDiplomacyActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> exactTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PostprocessRuleEntry rule in rules ?? new List<PostprocessRuleEntry>())
		{
			string ruleTag = (rule?.Tag ?? "").Trim();
			if (string.IsNullOrWhiteSpace(ruleTag))
			{
				continue;
			}
			if (ruleTag.StartsWith("[ACTION:DIPLOMACY:", StringComparison.OrdinalIgnoreCase))
			{
				string action = ExtractDiplomacyPostprocessActionName(ruleTag);
				int payloadSeparator = ruleTag.IndexOf(':', "[ACTION:DIPLOMACY:".Length);
				if (payloadSeparator < 0)
				{
					exactTags.Add(ruleTag);
				}
				else if (!string.IsNullOrWhiteSpace(action))
				{
					allowedDiplomacyActions.Add(action);
				}
				continue;
			}
			exactTags.Add(ruleTag);
		}
		List<string> list = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match item in Regex.Matches(raw ?? "", "\\[ACTION:(?:DIPLOMACY|KINGDOM_ANNEX):[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase))
		{
			string tag = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(tag))
			{
				continue;
			}
			bool accepted = tag.StartsWith("[ACTION:DIPLOMACY:", StringComparison.OrdinalIgnoreCase)
				? exactTags.Contains(tag) || allowedDiplomacyActions.Contains(ExtractDiplomacyPostprocessActionName(tag))
				: exactTags.Contains(tag);
			if (accepted && seen.Add(tag))
			{
				list.Add(tag);
			}
		}
		return string.Join("\n", list).Trim();
	}

internal static string NormalizeDuelPostprocessTagsForScene(string raw, List<RewardSystemBehavior.DuelStakeOption> options, Hero targetHero = null)
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = "";
		foreach (Match item in Regex.Matches(raw ?? "", "\\[(?:ACTION:[^\\]\\r\\n]*|AD:[^\\]\\r\\n]*|ADP:[^\\]\\r\\n]*)\\]", RegexOptions.IgnoreCase))
		{
			string text2 = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			if (text2.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				text = text2;
				continue;
			}
			if (hashSet.Add(text2))
			{
				list.Add(text2);
			}
		}
		if (list.Any((string x) => string.Equals((x ?? "").Trim(), "[ACTION:DUEL]", StringComparison.OrdinalIgnoreCase)))
		{
			try
			{
				DuelBehavior.ClearPendingDuelDebtTag(targetHero);
				string deferredAdTag = list.LastOrDefault((string x) => (x ?? "").Trim().StartsWith("[AD:", StringComparison.OrdinalIgnoreCase));
				if (!string.IsNullOrWhiteSpace(deferredAdTag))
				{
					Match match = Regex.Match(deferredAdTag, "\\[AD:(\\d+):(\\d+):P:([^\\]]*)\\]", RegexOptions.IgnoreCase);
					if (match.Success && int.TryParse(match.Groups[1].Value, out var result) && int.TryParse(match.Groups[2].Value, out var result2) && result > 0)
					{
						DuelBehavior.CachePendingDuelDebtTag(targetHero, result, result2, (match.Groups[3].Value ?? "").Trim());
					}
				}
			}
			catch
			{
			}
			list = list.Where((string x) => !((x ?? "").Trim().StartsWith("[AD:", StringComparison.OrdinalIgnoreCase) || (x ?? "").Trim().StartsWith("[ADP:", StringComparison.OrdinalIgnoreCase))).ToList();
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		string text3 = string.Join("\n", list.Concat(new string[1] { text }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
		return TranslateDuelStakeItemIndexesForScene(text3, options).Trim();
	}

internal static string NormalizeEncounterReleasePostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PostprocessRuleEntry rule in rules ?? new List<PostprocessRuleEntry>())
		{
			string text2 = (rule?.Tag ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				hashSet.Add(text2);
			}
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = "";
		foreach (Match item in Regex.Matches(raw ?? "", "\\[ACTION:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase))
		{
			string text3 = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				continue;
			}
			if (text3.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				text = text3;
				continue;
			}
			if (!hashSet.Contains(text3))
			{
				continue;
			}
			if (hashSet2.Add(text3))
			{
				list.Add(text3);
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", list.Concat(new string[1] { text }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string NormalizeHeroJoinPartyPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool allowsPersonalJoinVariants = false;
		bool allowsCompanionFallbackTemplate = false;
		foreach (PostprocessRuleEntry rule in rules ?? new List<PostprocessRuleEntry>())
		{
			string text2 = (rule?.Tag ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				hashSet.Add(text2);
				if (string.Equals(text2, "[A:H_J_P_P_C/L]", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(text2, "[A:H_J_P_P_C&L]", StringComparison.OrdinalIgnoreCase))
				{
					allowsPersonalJoinVariants = true;
				}
				if (string.Equals(text2, "[A:H_J_P_P_C&L]", StringComparison.OrdinalIgnoreCase))
				{
					allowsCompanionFallbackTemplate = true;
				}
			}
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = "";
		foreach (Match item in Regex.Matches(raw ?? "", "\\[(?:ACTION:[^\\]\\r\\n]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]\\r\\n]+))\\]", RegexOptions.IgnoreCase))
		{
			string text3 = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				continue;
			}
			if (text3.StartsWith("[ACTION:C_J_K:", StringComparison.OrdinalIgnoreCase))
			{
				// Accept only this provider-side envelope alias, then reuse the existing ID whitelist below.
				text3 = "[A:C_J_K:" + text3.Substring("[ACTION:C_J_K:".Length);
			}
			if (string.Equals(text3, "[A:H_J_P_P_C&L]", StringComparison.OrdinalIgnoreCase))
			{
				if (!allowsCompanionFallbackTemplate)
				{
					continue;
				}
				// The model copied the rule template instead of selecting a concrete variant.
				text3 = "[A:H_J_P_P_C]";
			}
			if (text3.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				text = text3;
				continue;
			}
			bool allowed = hashSet.Contains(text3);
			if (!allowed)
			{
				allowed = allowsPersonalJoinVariants
					&& (string.Equals(text3, "[A:H_J_P_P_C]", StringComparison.OrdinalIgnoreCase)
						|| string.Equals(text3, "[A:H_J_P_P_L]", StringComparison.OrdinalIgnoreCase));
			}
			if (!allowed)
			{
				Match clanJoinMatch = Regex.Match(text3, "^\\[A:C_J_K:([a-zA-Z0-9_.\\-]+)\\]$", RegexOptions.IgnoreCase);
				if (clanJoinMatch.Success)
				{
					string kingdomId = (clanJoinMatch.Groups[1].Value ?? "").Trim();
					allowed = (rules ?? new List<PostprocessRuleEntry>()).Any((PostprocessRuleEntry rule) =>
						string.Equals((rule?.Tag ?? "").Trim(), "[A:C_J_K:{targetKingdomId}]", StringComparison.OrdinalIgnoreCase)
						&& rule.RuntimeAllowedParameterValues != null
						&& rule.RuntimeAllowedParameterValues.Contains(kingdomId));
				}
			}
			if (!allowed)
			{
				continue;
			}
			if (hashSet2.Add(text3))
			{
				list.Add(text3);
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", list.Concat(new string[1] { text }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string NormalizeKingdomServicePostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PostprocessRuleEntry rule in rules ?? new List<PostprocessRuleEntry>())
		{
			string text2 = (rule?.Tag ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				hashSet.Add(text2);
			}
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = "";
		foreach (Match item in Regex.Matches(raw ?? "", "\\[(?:ACTION:[^\\]\\r\\n]*|A:(?:P_J_K_[MV]|P_L_K|CIVIL_FACTION:[^\\]\\r\\n]+))\\]", RegexOptions.IgnoreCase))
		{
			string text3 = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				continue;
			}
			if (text3.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				text = text3;
				continue;
			}
			bool allowed = hashSet.Contains(text3);
			if (!allowed)
			{
				Match legacyJoinMatch = Regex.Match(text3, "^\\[ACTION:KINGDOM_SERVICE:(MERCENARY|VASSAL|LEAVE):[^\\]]+\\]$", RegexOptions.IgnoreCase);
				if (legacyJoinMatch.Success)
				{
					string serviceType = (legacyJoinMatch.Groups[1].Value ?? "").Trim();
					allowed = (serviceType.Equals("MERCENARY", StringComparison.OrdinalIgnoreCase) && hashSet.Contains("[A:P_J_K_M]"))
						|| (serviceType.Equals("VASSAL", StringComparison.OrdinalIgnoreCase) && hashSet.Contains("[A:P_J_K_V]"))
						|| (serviceType.Equals("LEAVE", StringComparison.OrdinalIgnoreCase) && hashSet.Contains("[A:P_L_K]"));
				}
			}
			if (!allowed)
			{
				continue;
			}
			if (hashSet2.Add(text3))
			{
				list.Add(text3);
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", list.Concat(new string[1] { text }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string NormalizeLordsHallAccessPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PostprocessRuleEntry rule in rules ?? new List<PostprocessRuleEntry>())
		{
			string text2 = (rule?.Tag ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				hashSet.Add(text2);
			}
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = "";
		foreach (Match item in Regex.Matches(raw ?? "", "\\[ACTION:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase))
		{
			string text3 = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				continue;
			}
			if (text3.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				text = text3;
				continue;
			}
			if (!hashSet.Contains(text3))
			{
				continue;
			}
			if (hashSet2.Add(text3))
			{
				list.Add(text3);
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", list.Concat(new string[1] { text }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string NormalizeNpcSurrenderPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules, bool enabled)
	{
		if (!enabled || !HasNpcSurrenderPostprocessRule(rules))
		{
			return "";
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = "";
		foreach (Match item in Regex.Matches(raw ?? "", "\\[ACTION:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase))
		{
			string text2 = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			if (text2.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				text = text2;
				continue;
			}
			if (!string.Equals(text2, NpcSurrenderActionTag, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (hashSet.Add(NpcSurrenderActionTag))
			{
				list.Add(NpcSurrenderActionTag);
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", list.Concat(new string[1] { text }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string NormalizePartyTransferPostprocessTagsForScene(string raw, List<MyBehavior.PartyTransferPromptEntry> troopOptions, List<MyBehavior.PartyTransferPromptEntry> prisonerOptions, List<MyBehavior.PartyTransferPromptEntry> allTroopOptions = null, List<MyBehavior.PartyTransferPromptEntry> allPrisonerOptions = null)
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = "";
		bool troopAll = false;
		bool prisonerAll = false;
		foreach (Match item in Regex.Matches(raw ?? "", "\\[(?:ACTION:MOOD:[^\\]\\r\\n]*|ATT:[^\\]\\r\\n]*|ATP:[^\\]\\r\\n]*)\\]", RegexOptions.IgnoreCase))
		{
			string text2 = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			if (text2.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				text = text2;
				continue;
			}
			Match match = Regex.Match(text2, "^\\[ATT:([^\\]\\r\\n:]+):(ALL|\\d+)\\]$", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				string text3 = match.Groups[1].Value.Trim();
			bool targetAll = TransferQuantitySpec.IsAllValue(text3);
			bool amountAll = TransferQuantitySpec.IsAllValue(match.Groups[2].Value);
			if (targetAll || amountAll)
			{
				if (targetAll && !amountAll && (!int.TryParse(match.Groups[2].Value, out var allAmount) || allAmount <= 0))
				{
					continue;
				}
				troopAll = true;
					continue;
				}
				if (!int.TryParse(match.Groups[2].Value, out var result) || result <= 0)
				{
					continue;
				}
				MyBehavior.PartyTransferPromptEntry partyTransferPromptEntry = null;
				if (int.TryParse(text3, out var result2) && result2 > 0)
				{
					partyTransferPromptEntry = troopOptions?.FirstOrDefault((MyBehavior.PartyTransferPromptEntry x) => x != null && x.PromptIndex == result2);
				}
				if (partyTransferPromptEntry == null)
				{
					partyTransferPromptEntry = FindPartyTransferEntryByTokenForScene(troopOptions, text3);
				}
				if (partyTransferPromptEntry == null || partyTransferPromptEntry.Count <= 0)
				{
					continue;
				}
				result = Math.Min(Math.Max(1, result), Math.Max(1, partyTransferPromptEntry.Count));
				string item2 = "[ATT:" + partyTransferPromptEntry.PromptIndex + ":" + result + "]";
				if (hashSet.Add(item2))
				{
					list.Add(item2);
				}
				continue;
			}
			match = Regex.Match(text2, "^\\[ATP:([^\\]\\r\\n:]+):(ALL|\\d+)\\]$", RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				continue;
			}
			string text4 = match.Groups[1].Value.Trim();
			bool targetAll2 = TransferQuantitySpec.IsAllValue(text4);
			bool amountAll2 = TransferQuantitySpec.IsAllValue(match.Groups[2].Value);
			if (targetAll2 || amountAll2)
			{
				if (targetAll2 && !amountAll2 && (!int.TryParse(match.Groups[2].Value, out var allAmount2) || allAmount2 <= 0))
				{
					continue;
				}
				prisonerAll = true;
				continue;
			}
			if (!int.TryParse(match.Groups[2].Value, out var result3) || result3 <= 0)
			{
				continue;
			}
			MyBehavior.PartyTransferPromptEntry partyTransferPromptEntry2 = null;
			if (int.TryParse(text4, out var result4) && result4 > 0)
			{
				partyTransferPromptEntry2 = prisonerOptions?.FirstOrDefault((MyBehavior.PartyTransferPromptEntry x) => x != null && x.PromptIndex == result4);
			}
			if (partyTransferPromptEntry2 == null)
			{
				partyTransferPromptEntry2 = FindPartyTransferEntryByTokenForScene(prisonerOptions, text4);
			}
			if (partyTransferPromptEntry2 == null || partyTransferPromptEntry2.Count <= 0)
			{
				continue;
			}
			result3 = Math.Min(Math.Max(1, result3), Math.Max(1, partyTransferPromptEntry2.Count));
			string item3 = "[ATP:" + partyTransferPromptEntry2.PromptIndex + ":" + result3 + "]";
			if (hashSet.Add(item3))
			{
				list.Add(item3);
			}
		}
		if (troopAll)
		{
			list.RemoveAll((string x) => (x ?? "").StartsWith("[ATT:", StringComparison.OrdinalIgnoreCase));
			if ((allTroopOptions ?? new List<MyBehavior.PartyTransferPromptEntry>()).Any((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Count > 0))
			{
				list.Add("[ATT:ALL:ALL]");
			}
		}
		if (prisonerAll)
		{
			list.RemoveAll((string x) => (x ?? "").StartsWith("[ATP:", StringComparison.OrdinalIgnoreCase));
			if ((allPrisonerOptions ?? new List<MyBehavior.PartyTransferPromptEntry>()).Any((MyBehavior.PartyTransferPromptEntry x) => x != null && x.Count > 0))
			{
				list.Add("[ATP:ALL:ALL]");
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", list.Concat(new string[1] { text }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string NormalizePlayerNameForScenePostprocess(string text, string npcName = null)
	{
		try
		{
			string text2 = (text ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				return text2;
			}
			return AIConfigHandler.NormalizeActionPostprocessNameReferences(text2, npcName);
		}
		catch
		{
			return text ?? "";
		}
	}

internal static string NormalizeRewardPostprocessTagsForScene(string raw, List<RewardSystemBehavior.RewardItemInfo> options, List<RewardSystemBehavior.RewardItemInfo> allOptions, List<MyBehavior.SettlementTransferPromptEntry> fixedAssetOptions, bool allowAssetTransfer, int availableGold)
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = "";
		List<string> extractedTags = GiveAssetTagCodec.Extract(raw).Select((GiveAssetTag x) => x.RawTag).ToList();
		string rawWithoutGiveAssetTags = GiveAssetTagCodec.StripTags(raw);
		foreach (Match item in Regex.Matches(rawWithoutGiveAssetTags, "\\[(?:ACTION:[^\\]\\r\\n]*|AD:[^\\]\\r\\n]*|ADP:[^\\]\\r\\n]*)\\]", RegexOptions.IgnoreCase))
		{
			extractedTags.Add(item?.Value ?? "");
		}
		foreach (string extractedTag in extractedTags)
		{
			string text2 = (extractedTag ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			if (text2.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				text = text2;
				continue;
			}
			if (text2.StartsWith("[ACTION:GIVE_ASSET:", StringComparison.OrdinalIgnoreCase))
			{
				if (!allowAssetTransfer)
				{
					continue;
				}
				if (!GiveAssetTagCodec.TryParseWhole(text2, out GiveAssetTag giveAssetTag))
				{
					continue;
				}
				string assetToken = (giveAssetTag.AssetToken ?? "").Trim();
				string quantityToken = (giveAssetTag.QuantityToken ?? "").Trim();
				if (TransferQuantitySpec.IsAllValue(assetToken))
				{
					continue;
				}
				if (RewardSystemBehavior.IsGoldAssetTokenForExternal(assetToken))
				{
					if (!int.TryParse(quantityToken, out var goldAmount) || goldAmount <= 0)
					{
						continue;
					}
					text2 = "[ACTION:GIVE_ASSET:GOLD:" + goldAmount + "]";
				}
				else
				{
					MyBehavior.SettlementTransferPromptEntry fixedAsset = FindSettlementTransferEntryByTokenForScene(fixedAssetOptions, assetToken);
					if (fixedAsset != null)
					{
						if (!string.Equals(quantityToken, "1", StringComparison.Ordinal))
						{
							continue;
						}
						string fixedAssetId = MyBehavior.GetSettlementTransferAssetIdForExternal(fixedAsset);
						if (string.IsNullOrWhiteSpace(fixedAssetId))
						{
							continue;
						}
						text2 = "[ACTION:GIVE_ASSET:" + fixedAssetId.Trim() + ":1]";
					}
					else
					{
						RewardSystemBehavior.RewardItemInfo rewardItem = null;
						if (int.TryParse(assetToken, out var itemIndex) && itemIndex > 0 && itemIndex <= (options?.Count ?? 0))
						{
							rewardItem = options[itemIndex - 1];
						}
						rewardItem ??= FindRewardItemByTokenForScene(options, assetToken);
						if (rewardItem == null && !ReferenceEquals(allOptions, options))
						{
							rewardItem = FindRewardItemByTokenForScene(allOptions, assetToken);
						}
						bool isItemIndex = int.TryParse(assetToken, out var itemIndex2) && itemIndex2 > 0 && itemIndex2 <= (options?.Count ?? 0);
						if (TransferQuantitySpec.IsAllValue(quantityToken))
						{
							if (!isItemIndex && (rewardItem == null || rewardItem.Item == null || rewardItem.Count <= 0))
							{
								continue;
							}
							text2 = "[ACTION:GIVE_ASSET:" + assetToken + ":ALL]";
						}
					else if (int.TryParse(quantityToken, out var rpItemAmount)
						&& rpItemAmount > 0
						&& (isItemIndex || RewardSystemBehavior.IsValidGeneratedRpAssetNameForExternal(assetToken)))
					{
						text2 = "[ACTION:GIVE_ASSET:" + assetToken + ":" + rpItemAmount + "]";
						Logger.Log("ShoutBehavior", "[RewardPostprocess] GIVE_ASSET literal accepted source=scene asset=" + assetToken + " amount=" + rpItemAmount + " indexed=" + isItemIndex);
					}
					else
					{
						Logger.Log("ShoutBehavior", "[RewardPostprocess] GIVE_ASSET literal rejected source=scene asset=" + assetToken + " quantity=" + quantityToken + " indexed=" + isItemIndex);
						continue;
					}
					}
				}
			}
			else if (!RewardSystemBehavior.IsCanonicalDebtActionTagForExternal(text2))
			{
				continue;
			}
			if (hashSet.Add(text2))
			{
				list.Add(text2);
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		string text3 = string.Join("\n", list.Concat(new string[1] { text }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
		text3 = TranslateRewardItemIndexesForScene(text3, options);
		return text3.Trim();
	}

internal static string NormalizeSceneMechanismPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules, List<PostprocessSummonTarget> summonTargets, List<PostprocessGuideTarget> guideTargets)
	{
		List<PostprocessSummonTarget> summonCandidates = (summonTargets ?? new List<PostprocessSummonTarget>()).Where((PostprocessSummonTarget x) => x != null && x.PromptId > 0 && !string.IsNullOrWhiteSpace(x.DisplayName)).ToList();
		List<PostprocessGuideTarget> guideCandidates = (guideTargets ?? new List<PostprocessGuideTarget>()).Where((PostprocessGuideTarget x) => x != null && x.PromptId > 0 && !string.IsNullOrWhiteSpace(x.DisplayName)).ToList();
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool setsOwnedMassacreRequestRuleEnabled = false;
		bool setsOwnedMassacreStartRuleEnabled = false;
		bool setsOwnedMassacreStopRuleEnabled = false;
		bool setsOwnedMassacreCancelRequestRuleEnabled = false;
		bool inspectionPrisonerSlaughterRuleEnabled = false;
		bool noblePrisonerExecutionRuleEnabled = false;
		foreach (PostprocessRuleEntry rule in rules ?? new List<PostprocessRuleEntry>())
		{
			string text = (rule?.Tag ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (string.Equals(text, "[ACTION:SCENE_FOLLOW_PLAYER]", StringComparison.OrdinalIgnoreCase))
			{
				flag = true;
				continue;
			}
			if (string.Equals(text, "[ACTION:SCENE_STOP_FOLLOW]", StringComparison.OrdinalIgnoreCase))
			{
				flag2 = true;
				continue;
			}
			if (string.Equals(text, "[END]", StringComparison.OrdinalIgnoreCase))
			{
				flag3 = true;
				continue;
			}
			if (string.Equals(text, SetsOwnedSettlementMassacreProfile.RequestActionTag, StringComparison.OrdinalIgnoreCase))
			{
				setsOwnedMassacreRequestRuleEnabled = true;
				continue;
			}
			if (string.Equals(text, SetsOwnedSettlementMassacreProfile.StartActionTag, StringComparison.OrdinalIgnoreCase))
			{
				setsOwnedMassacreStartRuleEnabled = true;
				continue;
			}
			if (string.Equals(text, SetsOwnedSettlementMassacreProfile.StopActionTag, StringComparison.OrdinalIgnoreCase))
			{
				setsOwnedMassacreStopRuleEnabled = true;
				continue;
			}
			if (string.Equals(text, SetsOwnedSettlementMassacreProfile.CancelRequestActionTag, StringComparison.OrdinalIgnoreCase))
			{
				setsOwnedMassacreCancelRequestRuleEnabled = true;
				continue;
			}
			if (string.Equals(text, TroopInspectionPrisonerSlaughterProfile.ActionTag, StringComparison.OrdinalIgnoreCase))
			{
				inspectionPrisonerSlaughterRuleEnabled = true;
				continue;
			}
			if (string.Equals(text, NoblePrisonerEscortBehavior.ExecuteActionTag, StringComparison.OrdinalIgnoreCase))
			{
				noblePrisonerExecutionRuleEnabled = true;
				continue;
			}
			if (text.StartsWith("[ACTION:SCENE_SUMMON:", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (text.StartsWith("[ACTION:SCENE_GUIDE:", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
		}
		List<string> actionTags = new List<string>();
		HashSet<string> hashSet3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text2 = "";
		foreach (Match item2 in Regex.Matches(raw ?? "", "\\[ACTION:MOOD:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase))
		{
			string text3 = (item2?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				text2 = text3;
			}
		}
		if (flag && Regex.IsMatch(raw ?? "", "\\[(?:FOL|ACTION:SCENE_FOLLOW_PLAYER)\\]", RegexOptions.IgnoreCase) && hashSet3.Add("[ACTION:SCENE_FOLLOW_PLAYER]"))
		{
			actionTags.Add("[ACTION:SCENE_FOLLOW_PLAYER]");
		}
		if (flag2 && Regex.IsMatch(raw ?? "", "\\[(?:STP|ACTION:SCENE_STOP_FOLLOW)\\]", RegexOptions.IgnoreCase) && hashSet3.Add("[ACTION:SCENE_STOP_FOLLOW]"))
		{
			actionTags.Add("[ACTION:SCENE_STOP_FOLLOW]");
		}
		if (flag3 && Regex.IsMatch(raw ?? "", "\\[END\\]", RegexOptions.IgnoreCase) && hashSet3.Add("[END]"))
		{
			actionTags.Add("[END]");
		}
		if (setsOwnedMassacreStopRuleEnabled && SetsOwnedSettlementMassacreStopActionTagRegex.IsMatch(raw ?? "") && hashSet3.Add(SetsOwnedSettlementMassacreProfile.StopActionTag))
		{
			actionTags.Add(SetsOwnedSettlementMassacreProfile.StopActionTag);
		}
		if (setsOwnedMassacreCancelRequestRuleEnabled && SetsOwnedSettlementMassacreCancelRequestActionTagRegex.IsMatch(raw ?? "") && hashSet3.Add(SetsOwnedSettlementMassacreProfile.CancelRequestActionTag))
		{
			actionTags.Add(SetsOwnedSettlementMassacreProfile.CancelRequestActionTag);
		}
		if (setsOwnedMassacreStartRuleEnabled && SetsOwnedSettlementMassacreStartActionTagRegex.IsMatch(raw ?? "") && hashSet3.Add(SetsOwnedSettlementMassacreProfile.StartActionTag))
		{
			actionTags.Add(SetsOwnedSettlementMassacreProfile.StartActionTag);
		}
		if (setsOwnedMassacreRequestRuleEnabled && SetsOwnedSettlementMassacreRequestActionTagRegex.IsMatch(raw ?? "") && hashSet3.Add(SetsOwnedSettlementMassacreProfile.RequestActionTag))
		{
			actionTags.Add(SetsOwnedSettlementMassacreProfile.RequestActionTag);
		}
		if (inspectionPrisonerSlaughterRuleEnabled
			&& TroopInspectionPrisonerSlaughterActionTagRegex.IsMatch(raw ?? "")
			&& hashSet3.Add(TroopInspectionPrisonerSlaughterProfile.ActionTag))
		{
			actionTags.Add(TroopInspectionPrisonerSlaughterProfile.ActionTag);
		}
		if (noblePrisonerExecutionRuleEnabled
			&& (raw ?? string.Empty).IndexOf(NoblePrisonerEscortBehavior.ExecuteActionTag, StringComparison.OrdinalIgnoreCase) >= 0
			&& hashSet3.Add(NoblePrisonerEscortBehavior.ExecuteActionTag))
		{
			actionTags.Add(NoblePrisonerEscortBehavior.ExecuteActionTag);
		}
		List<string> list3 = new List<string>();
		HashSet<string> hashSet4 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match item3 in Regex.Matches(raw ?? "", "\\[(?:ASS|ACTION:SCENE_SUMMON):([^\\]\\r\\n]+)\\]", RegexOptions.IgnoreCase))
		{
			List<string> list4 = ParseSceneMechanismTargetTokens(item3.Groups[1].Value);
			if (list4 == null || list4.Count == 0)
			{
				continue;
			}
			foreach (string item in list4)
			{
				PostprocessSummonTarget sceneSummonPromptTarget = ResolvePostprocessSummonTargetByToken(summonCandidates, guideCandidates, item);
				if (sceneSummonPromptTarget == null)
				{
					continue;
				}
				string text4 = (sceneSummonPromptTarget.DisplayName ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text4) && hashSet4.Add(text4))
				{
					list3.Add(text4);
				}
			}
		}
		if (list3.Count > 0)
		{
			string text4 = "[ACTION:SCENE_SUMMON:" + string.Join(",", list3) + "]";
			if (hashSet3.Add(text4))
			{
				actionTags.Add(text4);
			}
		}
		foreach (Match item4 in Regex.Matches(raw ?? "", "\\[(?:GUI|ACTION:SCENE_GUIDE):([^\\]\\r\\n]+)\\]", RegexOptions.IgnoreCase))
		{
			string text5 = NormalizeSceneMechanismTargetToken(item4.Groups[1].Value);
			if (string.IsNullOrWhiteSpace(text5))
			{
				continue;
			}
			PostprocessGuideTarget sceneGuidePromptTarget = ResolveSceneMechanismTargetByToken(guideCandidates, text5, (PostprocessGuideTarget x) => x.PromptId, (PostprocessGuideTarget x) => x.DisplayName);
			string text6 = (sceneGuidePromptTarget?.DisplayName ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text6))
			{
				PostprocessSummonTarget sceneSummonPromptTarget2 = ResolveSceneMechanismTargetByToken(summonCandidates, text5, (PostprocessSummonTarget x) => x.PromptId, (PostprocessSummonTarget x) => x.DisplayName);
				text6 = (sceneSummonPromptTarget2?.DisplayName ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(text6))
			{
				continue;
			}
			string text7 = "[ACTION:SCENE_GUIDE:" + text6 + "]";
			if (hashSet3.Add(text7))
			{
				actionTags.Add(text7);
			}
		}
		actionTags = SelectPrimarySceneMechanismPostprocessActions(raw, actionTags);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", actionTags.Concat(new string[1] { text2 }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string NormalizeSceneMechanismTargetKey(string text)
	{
		string text2 = NormalizeSceneMechanismTargetToken(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		return text2.Replace("\"", "").Replace("'", "").Replace("“", "").Replace("”", "").Replace("‘", "").Replace("’", "").Replace("「", "").Replace("」", "").Replace("『", "").Replace("』", "").Replace(" ", "").Trim();
	}

internal static string NormalizeSceneMechanismTargetToken(string text)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		text2 = text2.Trim('"', '\'', '“', '”', '‘', '’', '「', '」', '『', '』', '[', ']');
		return Regex.Replace(text2, "\\s+", " ").Trim();
	}

internal static string NormalizeSiegeSurrenderPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules, bool enabled)
	{
		if (!enabled || !HasSiegeSurrenderPostprocessRule(rules))
		{
			return "";
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = "";
		foreach (Match item in Regex.Matches(raw ?? "", "\\[ACTION:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase))
		{
			string text2 = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			if (text2.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				text = text2;
				continue;
			}
			if (!string.Equals(text2, SiegeSurrenderActionTag, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (hashSet.Add(SiegeSurrenderActionTag))
			{
				list.Add(SiegeSurrenderActionTag);
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", list.Concat(new string[1] { text }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string NormalizeVanillaIssuePostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PostprocessRuleEntry rule in rules ?? new List<PostprocessRuleEntry>())
		{
			string text2 = (rule?.Tag ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				hashSet.Add(text2);
			}
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = "";
		foreach (Match item in Regex.Matches(raw ?? "", "\\[ACTION:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase))
		{
			string text3 = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				continue;
			}
			if (text3.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				text = text3;
				continue;
			}
			if (!hashSet.Contains(text3))
			{
				continue;
			}
			if (hashSet2.Add(text3))
			{
				list.Add(text3);
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", list.Concat(new string[1] { text }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string NormalizeVassalagePostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		HashSet<string> allowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PostprocessRuleEntry rule in rules ?? new List<PostprocessRuleEntry>())
		{
			string tag = (rule?.Tag ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(tag))
			{
				allowedTags.Add(tag);
			}
		}
		List<string> accepted = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string moodTag = "";
		foreach (Match match in Regex.Matches(raw ?? "", "\\[ACTION:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase))
		{
			string tag = (match?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(tag))
			{
				continue;
			}
			if (tag.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				moodTag = tag;
				continue;
			}
			if (!allowedTags.Contains(tag))
			{
				continue;
			}
			if (seen.Add(tag))
			{
				accepted.Add(tag);
			}
		}
		if (string.IsNullOrWhiteSpace(moodTag))
		{
			moodTag = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", accepted.Concat(new string[1] { moodTag }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string NormalizeVoteDealPostprocessTagsForScene(string raw, List<PostprocessRuleEntry> rules)
	{
		HashSet<string> validTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PostprocessRuleEntry rule in rules ?? new List<PostprocessRuleEntry>())
		{
			string tag = (rule?.Tag ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(tag))
			{
				validTags.Add(tag);
			}
		}
		List<string> list = new List<string>();
		string moodTag = "";
		foreach (Match item in Regex.Matches(raw ?? "", "\\[(?:ACTION:MOOD:[^\\]\\r\\n]*|ACTION:AGENDA:[^\\]\\r\\n]*)\\]", RegexOptions.IgnoreCase))
		{
			string tag = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(tag))
			{
				continue;
			}
			if (tag.StartsWith("[ACTION:MOOD:", StringComparison.OrdinalIgnoreCase))
			{
				moodTag = tag;
				continue;
			}
			if (IsValidVoteDealPostprocessTag(tag, validTags))
			{
				list.Add(tag);
			}
		}
		if (string.IsNullOrWhiteSpace(moodTag))
		{
			moodTag = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", list.Concat(new string[1] { moodTag }).Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static string NormalizeWorldMapPartyCommandPostprocessTagsForScene(string raw)
	{
		string normalized = WorldMapPartyCommandBehavior.NormalizeWorldMapOrderTagsForExternal(raw);
		string moodTag = "";
		foreach (Match item in Regex.Matches(raw ?? "", "\\[ACTION:MOOD:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase))
		{
			string tag = (item?.Value ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(tag))
			{
				moodTag = tag;
			}
		}
		if (string.IsNullOrWhiteSpace(moodTag))
		{
			moodTag = AIConfigHandler.ActionPostprocessFallbackMoodTag;
		}
		return string.Join("\n", new string[2] { normalized, moodTag }.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
	}

internal static List<string> ParseSceneMechanismTargetTokens(string rawValue)
	{
		if (string.IsNullOrWhiteSpace(rawValue))
		{
			return null;
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string[] array = Regex.Split(rawValue, "\\s*[,，、；;]\\s*");
		foreach (string text in array)
		{
			string text2 = NormalizeSceneMechanismTargetToken(text);
			if (!string.IsNullOrWhiteSpace(text2) && hashSet.Add(text2))
			{
				list.Add(text2);
			}
		}
		return list;
	}

internal static bool RequestsAllFixedAssetsForPostprocess(string playerText, string historyText)
	{
		string text = ((playerText ?? "") + "\n" + (historyText ?? "")).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return Regex.IsMatch(text, "(?:全部|所有(?:的)?|全都|一切)\\s*(?:资产|财产|固定资产|领地|城市|城堡|工坊|商队|贸易车队)|(?:固定资产|领地|城市|城堡|工坊|商队|贸易车队)\\s*(?:全部|全都|都)|\\ball\\s+(?:assets?|property|fixed\\s+assets?|fiefs?|towns?|castles?|workshops?|caravans?)\\b", RegexOptions.IgnoreCase);
	}

internal static bool RequestsAllOrdinaryAssetsForPostprocess(string playerText, string historyText)
	{
		string text = ((playerText ?? "") + "\n" + (historyText ?? "")).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return Regex.IsMatch(text, "(?:全部|所有(?:的)?|全都|一切)\\s*(?:资产|财产|物品|东西|货物|装备|库存|行囊)|(?:资产|财产|物品|东西|货物|装备|库存|行囊)\\s*(?:全部|全都|都)|\\ball\\s+(?:assets?|property|items?|goods|equipment|inventory)\\b", RegexOptions.IgnoreCase);
	}

internal static string ResolveCourierRuntimeTargetKingdomId(Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			string text = (targetHero?.Clan?.Kingdom?.StringId ?? targetHero?.MapFaction?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.ToLowerInvariant();
			}
			Hero heroObject = targetCharacter?.HeroObject;
			text = (heroObject?.Clan?.Kingdom?.StringId ?? heroObject?.MapFaction?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.ToLowerInvariant();
			}
		}
		catch
		{
		}
		return "";
	}

internal static T ResolveSceneMechanismTargetByToken<T>(IEnumerable<T> targets, string token, Func<T, int> getId, Func<T, string> getName) where T : class
	{
		if (targets == null)
		{
			return null;
		}
		string text = NormalizeSceneMechanismTargetToken(token);
		string text2 = NormalizeSceneMechanismTargetKey(token);
		if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(text2))
		{
			return null;
		}
		List<T> list = targets.Where((T x) => x != null).ToList();
		if (int.TryParse(text, out var result))
		{
			T val = list.FirstOrDefault((T x) => getId(x) == result);
			if (val != null)
			{
				return val;
			}
		}
		T val2 = list.FirstOrDefault((T x) => string.Equals((getName(x) ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (val2 != null)
		{
			return val2;
		}
		val2 = list.FirstOrDefault((T x) => string.Equals(NormalizeSceneMechanismTargetKey(getName(x)), text2, StringComparison.OrdinalIgnoreCase));
		if (val2 != null)
		{
			return val2;
		}
		List<T> list2 = list.Where((T x) => !string.IsNullOrWhiteSpace(getName(x)) && NormalizeSceneMechanismTargetKey(getName(x)).IndexOf(text2, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list2.Count == 1)
		{
			return list2[0];
		}
		list2 = list.Where((T x) => !string.IsNullOrWhiteSpace(text2) && text2.IndexOf(NormalizeSceneMechanismTargetKey(getName(x)), StringComparison.OrdinalIgnoreCase) >= 0).ToList();
		if (list2.Count == 1)
		{
			return list2[0];
		}
		return null;
	}

internal static PostprocessSummonTarget ResolvePostprocessSummonTargetByToken(
		IEnumerable<PostprocessSummonTarget> summonTargets,
		IEnumerable<PostprocessGuideTarget> guideTargets,
		string token)
	{
		PostprocessSummonTarget direct = ResolveSceneMechanismTargetByToken(
			summonTargets,
			token,
			(PostprocessSummonTarget x) => x.PromptId,
			(PostprocessSummonTarget x) => x.DisplayName);
		if (direct != null)
		{
			return direct;
		}

		PostprocessGuideTarget guide = ResolveSceneMechanismTargetByToken(
			guideTargets,
			token,
			(PostprocessGuideTarget x) => x.PromptId,
			(PostprocessGuideTarget x) => x.DisplayName);
		if (guide == null || !guide.HasLocationCharacter)
		{
			return null;
		}

		// The guide list intentionally contains off-location role targets (for example
		// shop workers). Reuse that same LocationCharacter for summon execution instead
		// of dropping a valid model tag simply because it was not a visible NPC.
		return new PostprocessSummonTarget
		{
			PromptId = guide.PromptId,
			DisplayName = guide.DisplayName,
			LocationCode = guide.LocationCode,
			HasLocationCharacter = guide.HasLocationCharacter,

		};
	}

internal static string SanitizeSceneRelayPostprocessField(string value)
	{
		return Regex.Replace((value ?? "").Replace("\r", " ").Replace("\n", " ").Replace("|", " "), "[ \\t]{2,}", " ").Trim();
	}

internal static List<string> SelectPrimarySceneMechanismPostprocessActions(string raw, List<string> actionTags)
	{
		List<string> list = (actionTags ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		if (list.Count <= 1)
		{
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
		if ((raw ?? string.Empty).IndexOf(NoblePrisonerEscortBehavior.ExecuteActionTag, StringComparison.OrdinalIgnoreCase) >= 0
			&& hashSet.Contains(NoblePrisonerEscortBehavior.ExecuteActionTag))
		{
			return new List<string>
			{
				NoblePrisonerEscortBehavior.ExecuteActionTag
			};
		}
		if (TroopInspectionPrisonerSlaughterActionTagRegex.IsMatch(raw ?? "")
			&& hashSet.Contains(TroopInspectionPrisonerSlaughterProfile.ActionTag))
		{
			return new List<string>
			{
				TroopInspectionPrisonerSlaughterProfile.ActionTag
			};
		}
		if (SetsOwnedSettlementMassacreStopActionTagRegex.IsMatch(raw ?? "") && hashSet.Contains(SetsOwnedSettlementMassacreProfile.StopActionTag))
		{
			return new List<string> { SetsOwnedSettlementMassacreProfile.StopActionTag };
		}
		if (SetsOwnedSettlementMassacreCancelRequestActionTagRegex.IsMatch(raw ?? "") && hashSet.Contains(SetsOwnedSettlementMassacreProfile.CancelRequestActionTag))
		{
			return new List<string> { SetsOwnedSettlementMassacreProfile.CancelRequestActionTag };
		}
		if (SetsOwnedSettlementMassacreStartActionTagRegex.IsMatch(raw ?? "") && hashSet.Contains(SetsOwnedSettlementMassacreProfile.StartActionTag))
		{
			return new List<string> { SetsOwnedSettlementMassacreProfile.StartActionTag };
		}
		if (SetsOwnedSettlementMassacreRequestActionTagRegex.IsMatch(raw ?? "") && hashSet.Contains(SetsOwnedSettlementMassacreProfile.RequestActionTag))
		{
			return new List<string> { SetsOwnedSettlementMassacreProfile.RequestActionTag };
		}
		foreach (Match item in Regex.Matches(raw ?? "", "\\[(?:FOL|ACTION:SCENE_FOLLOW_PLAYER|STP|ACTION:SCENE_STOP_FOLLOW|END|(?:ASS|ACTION:SCENE_SUMMON):[^\\]\\r\\n]+|(?:GUI|ACTION:SCENE_GUIDE):[^\\]\\r\\n]+)\\]", RegexOptions.IgnoreCase))
		{
			string text = (item?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (Regex.IsMatch(text, "\\[(?:STP|ACTION:SCENE_STOP_FOLLOW)\\]", RegexOptions.IgnoreCase) && hashSet.Contains("[ACTION:SCENE_STOP_FOLLOW]"))
			{
				return new List<string> { "[ACTION:SCENE_STOP_FOLLOW]" };
			}
			if (Regex.IsMatch(text, "\\[END\\]", RegexOptions.IgnoreCase) && hashSet.Contains("[END]"))
			{
				return new List<string> { "[END]" };
			}
			if (Regex.IsMatch(text, "\\[(?:GUI|ACTION:SCENE_GUIDE):", RegexOptions.IgnoreCase))
			{
				string text2 = list.FirstOrDefault((string x) => x.StartsWith("[ACTION:SCENE_GUIDE:", StringComparison.OrdinalIgnoreCase));
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return new List<string> { text2 };
				}
			}
			if (Regex.IsMatch(text, "\\[(?:ASS|ACTION:SCENE_SUMMON):", RegexOptions.IgnoreCase))
			{
				List<string> list2 = list.Where((string x) => x.StartsWith("[ACTION:SCENE_SUMMON:", StringComparison.OrdinalIgnoreCase)).ToList();
				if (list2.Count > 0)
				{
					return list2;
				}
			}
			if (Regex.IsMatch(text, "\\[(?:FOL|ACTION:SCENE_FOLLOW_PLAYER)\\]", RegexOptions.IgnoreCase) && hashSet.Contains("[ACTION:SCENE_FOLLOW_PLAYER]"))
			{
				return new List<string> { "[ACTION:SCENE_FOLLOW_PLAYER]" };
			}
		}
		return list;
	}

internal static string StripActionTagsForSceneSpeech(string text)
	{
		string text2 = GiveAssetTagCodec.StripTags((text ?? "").Replace("\r", ""));
		return Regex.Replace(text2, "\\[(?:ACTION:[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|ATT:[^\\]]*|ATP:[^\\]]*|FOL|STP)\\]", "", RegexOptions.IgnoreCase).Trim();
	}

internal static string StripSceneMechanismActionTagsForScene(string text)
	{
		string text2 = text ?? "";
		text2 = Regex.Replace(text2, "\\[(?:ASS|ACTION:SCENE_SUMMON):[^\\]]*\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:GUI|ACTION:SCENE_GUIDE):[^\\]]*\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:FOL|ACTION:SCENE_FOLLOW_PLAYER)\\]", "", RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[(?:STP|ACTION:SCENE_STOP_FOLLOW)\\]", "", RegexOptions.IgnoreCase);
		text2 = StripSetsOwnedSettlementMassacreActionTags(text2);
		text2 = Regex.Replace(text2, "\\[END\\]", "", RegexOptions.IgnoreCase);
		return text2.Trim();
	}

internal static string StripSetsOwnedSettlementMassacreActionTags(string content)
	{
		string result = SetsOwnedSettlementMassacreRequestActionTagRegex.Replace(content ?? "", "");
		result = SetsOwnedSettlementMassacreStartActionTagRegex.Replace(result, "");
		result = SetsOwnedSettlementMassacreStopActionTagRegex.Replace(result, "");
		result = SetsOwnedSettlementMassacreCancelRequestActionTagRegex.Replace(result, "");
		return result.Trim();
	}

internal static string TranslateDuelStakeItemIndexesForScene(string text, List<RewardSystemBehavior.DuelStakeOption> options)
	{
		if (string.IsNullOrWhiteSpace(text) || options == null || options.Count == 0)
		{
			return text ?? "";
		}
		string text2 = Regex.Replace(text, "\\[ACTION:(DUEL_STAKE(?:_(?:NPC|PLAYER))?_ITEM):(\\d+):(\\d+)\\]", delegate(Match m)
		{
			string value = m.Groups[1].Value;
			if (!int.TryParse(m.Groups[2].Value, out var result) || !int.TryParse(m.Groups[3].Value, out var result2) || result <= 0 || result > options.Count || result2 <= 0)
			{
				return "";
			}
			RewardSystemBehavior.DuelStakeOption duelStakeOption = options[result - 1];
			if (duelStakeOption == null || string.IsNullOrWhiteSpace(duelStakeOption.ItemId))
			{
				return "";
			}
			result2 = Math.Min(Math.Max(1, result2), Math.Max(1, duelStakeOption.Count));
			return "[ACTION:" + value + ":" + duelStakeOption.ItemId.Trim() + ":" + result2 + "]";
		}, RegexOptions.IgnoreCase);
		return Regex.Replace(text2, "\\[ACTION:(DUEL_STAKE(?:_(?:NPC|PLAYER))?_ITEM):([^\\]\\r\\n:]+):(\\d+)\\]", delegate(Match m)
		{
			string value = m.Groups[1].Value;
			string token = m.Groups[2].Value;
			if (!int.TryParse(m.Groups[3].Value, out var result3) || result3 <= 0)
			{
				return "";
			}
			RewardSystemBehavior.DuelStakeOption duelStakeOption2 = FindDuelStakeOptionByTokenForScene(options, token);
			if (duelStakeOption2 == null || string.IsNullOrWhiteSpace(duelStakeOption2.ItemId))
			{
				return "[ACTION:" + value + ":" + token.Trim() + ":" + result3 + "]";
			}
			result3 = Math.Min(Math.Max(1, result3), Math.Max(1, duelStakeOption2.Count));
			return "[ACTION:" + value + ":" + duelStakeOption2.ItemId.Trim() + ":" + result3 + "]";
		}, RegexOptions.IgnoreCase);
	}

internal static string TranslateRewardItemIndexesForScene(string text, List<RewardSystemBehavior.RewardItemInfo> options)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return text ?? "";
		}
		static string getItemDisplayName(RewardSystemBehavior.RewardItemInfo item)
		{
			string text4 = item?.Name;
			if (string.IsNullOrWhiteSpace(text4))
			{
				text4 = item?.Item?.Name?.ToString();
			}
			return text4 ?? "";
		}
		static void logRewardItemTranslation(string source, string token, RewardSystemBehavior.RewardItemInfo item, string actionKey)
		{
			try
			{
				Logger.Log("ShoutBehavior", "[RewardPostprocess] item_translate source=" + (source ?? "") + " token=" + (token ?? "") + " matchedName=" + (item?.Name ?? "") + " stringId=" + (item?.StringId ?? "") + " promptId=" + (item?.PromptStringId ?? "") + " modifierId=" + (item?.ModifierStringId ?? "") + " actionKey=" + (actionKey ?? ""));
			}
			catch
			{
			}
		}
		return GiveAssetTagCodec.ReplaceTags(text, delegate(GiveAssetTag tag)
		{
			string token = (tag.AssetToken ?? "").Trim();
			bool isAll = TransferQuantitySpec.IsAllValue(tag.QuantityToken);
			int result3 = 0;
			if (TransferQuantitySpec.IsAllValue(token) && !isAll)
			{
				return "";
			}
			if (!isAll && (!int.TryParse(tag.QuantityToken, out result3) || result3 <= 0))
			{
				return "";
			}
			if (options != null && int.TryParse(token, out var itemIndex) && itemIndex > 0 && itemIndex <= options.Count)
			{
				RewardSystemBehavior.RewardItemInfo rewardItemInfo = options[itemIndex - 1];
				string text3 = getItemDisplayName(rewardItemInfo);
				logRewardItemTranslation("index", token, rewardItemInfo, text3);
				if (string.IsNullOrWhiteSpace(text3))
				{
					return "";
				}
				token = text3.Trim();
			}
			return isAll ? ("[ACTION:GIVE_ASSET:" + token + ":ALL]") : ("[ACTION:GIVE_ASSET:" + token + ":" + result3 + "]");
		});
	}
}
