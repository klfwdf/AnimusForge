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

using static AnimusForge.SceneMovementController;

using static AnimusForge.ShoutBehavior;
namespace AnimusForge;
internal delegate bool ConversationOpenHallAction(Hero hero,CharacterObject character,int index,ref string tags);
internal sealed class ConversationActionBoundaryBannerlordAdapter
{
    private readonly NativeConversationGameEffectsRuntime _nativeGameEffects;
    private readonly ConversationOpenHallAction _openHall;
    private readonly Action<Action> _post;
    private readonly Func<int> _queueCount;
    private readonly Action<int,string> _queueAgentFact;
    private readonly Action<string,string> _queueNativeFact;
    private readonly ConversationMainThreadActionDrain _drain;
    private readonly Func<Hero,CharacterObject,int,string,string,bool> _scheduleFight;
    internal ConversationActionBoundaryBannerlordAdapter(NativeConversationGameEffectsRuntime effects,ConversationOpenHallAction openHall,Action<Action> post,Func<int> queueCount,ConversationMainThreadActionDrain drain,Func<Hero,CharacterObject,int,string,string,bool> scheduleFight,Action<int,string> queueAgentFact,Action<string,string> queueNativeFact)
    { _nativeGameEffects=effects;_openHall=openHall;_post=post;_queueCount=queueCount;_drain=drain;_scheduleFight=scheduleFight;_queueAgentFact=queueAgentFact;_queueNativeFact=queueNativeFact; }
internal bool TryApplyDeferredScenePostprocessActionTagsDirectly(
		Hero targetHero,
		CharacterObject targetCharacter,
		int targetAgentIndex,
		ref string tags,
		string playerText,
		string npcReplyText,
		string chainName,
		bool replyIsDirectPlayerResponse,
		DetachedDuelDispatchContext duelDispatchContext = null)
	{
		if (_openHall(targetHero, targetCharacter, targetAgentIndex, ref tags))
		{
			return true;
		}
		if (!ConversationActionPostprocessOwner.HasDeferredDirectGameActionTag(tags))
		{
			return false;
		}
		string before = tags ?? "";
		Logger.Log("ShoutBehavior", "[DeferredPostprocess] direct_action_apply start target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agent=" + targetAgentIndex + " tags=" + before.Replace("\r", "\\r").Replace("\n", "\\n"));
		NoblePrisonerExecutionOrderBehavior.TryProcessAcceptedTag(
			targetHero ?? targetCharacter?.HeroObject,
			targetAgentIndex,
			replyIsDirectPlayerResponse,
			ref tags,
			out _);
		PublicExecutionOrderRuntime.Consume(targetAgentIndex, ref tags);
		NoblePrisonerEscortBehavior.TryProcessSceneExecutionTag(targetAgentIndex, replyIsDirectPlayerResponse, ref tags);
		if (string.IsNullOrWhiteSpace(tags))
		{
			return !string.Equals(before, tags ?? "", StringComparison.Ordinal);
		}
		_nativeGameEffects.ApplyNativeConversationActionTags(
			targetHero,
			targetCharacter,
			ref tags,
			targetAgentIndex,
			playerText,
			actionChainName: chainName,
			npcReplyTextOverride: npcReplyText,
			duelDispatchContext: duelDispatchContext);
		bool changed = !string.Equals(before, tags ?? "", StringComparison.Ordinal);
		Logger.Log("ShoutBehavior", "[DeferredPostprocess] direct_action_apply done target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agent=" + targetAgentIndex + " changed=" + changed + " remaining=" + ((tags ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
		return changed;
	}
internal bool TryApplyDeferredSceneMoodTag(NpcDataPacket speaker, string tags)
	{
		try
		{
			MatchCollection matchCollection = Regex.Matches(tags ?? "", "\\[ACTION:MOOD:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase);
			if (matchCollection == null || matchCollection.Count <= 0)
			{
				return false;
			}
			string text = (matchCollection[matchCollection.Count - 1]?.Value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			string text2 = text;
			Hero hero = SceneAgentIdentityPromptCaptureAdapter.ResolveHeroFromAgentIndex(speaker?.AgentIndex ?? (-1));
			if (hero != null)
			{
				MyBehavior.ApplyPostprocessMoodFromSceneHeroResponseExternal(hero, ref text2);
				Logger.Log("ShoutBehavior", "[DeferredPostprocess] mood_applied hero=" + (hero.StringId ?? "") + " mood=" + text);
			}
			else
			{
				MyBehavior.ApplyPostprocessMoodFromSceneUnnamedResponseExternal(speaker?.UnnamedKey, speaker?.Name, ref text2);
				Logger.Log("ShoutBehavior", "[DeferredPostprocess] mood_applied unnamed=" + (speaker?.UnnamedKey ?? speaker?.Name ?? "") + " mood=" + text);
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WARN] DeferredPostprocess mood apply failed: " + ex.Message);
			return false;
		}
	}
internal bool TryProcessSetsOwnedSettlementMassacreActionTags(int targetAgentIndex, ref string content)
	{
		try
		{
			bool requestPermission = !string.IsNullOrWhiteSpace(content) && SetsOwnedSettlementMassacreRequestActionTagRegex.IsMatch(content);
			bool startRequested = !string.IsNullOrWhiteSpace(content) && SetsOwnedSettlementMassacreStartActionTagRegex.IsMatch(content);
			bool stopRequested = !string.IsNullOrWhiteSpace(content) && SetsOwnedSettlementMassacreStopActionTagRegex.IsMatch(content);
			bool cancelRequest = !string.IsNullOrWhiteSpace(content) && SetsOwnedSettlementMassacreCancelRequestActionTagRegex.IsMatch(content);
			if (!requestPermission && !startRequested && !stopRequested && !cancelRequest)
			{
				return false;
			}

			content = ConversationActionPostprocessOwner.StripSetsOwnedSettlementMassacreActionTags(content);
			Mission mission = Mission.Current;
			if (!SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementEntryActiveForExternal(mission))
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] ignored SETS massacre tag outside owned/attached settlement scene. agent=" + targetAgentIndex);
				return false;
			}

			bool massacreActive = SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementMassacreActiveForExternal(mission);
			bool requestPendingForSpeaker = SettlementEntryTroopSelectionBehavior.IsOwnedOrAttachedSettlementMassacreRequestPendingForExternal(mission, targetAgentIndex);
			bool anyRequestPending = SettlementEntryTroopSelectionBehavior.HasOwnedOrAttachedSettlementMassacreRequestForExternal(mission);
			bool handled = false;
			string action = "none";
			if (stopRequested && massacreActive)
			{
				action = "stop";
				handled = SettlementEntryTroopSelectionBehavior.TryStopOwnedOrAttachedSettlementMassacreForExternal(targetAgentIndex, SetsOwnedSettlementMassacreProfile.StopSource);
			}
			else if (cancelRequest && !massacreActive && requestPendingForSpeaker)
			{
				action = "cancel_request";
				handled = SettlementEntryTroopSelectionBehavior.TryCancelOwnedOrAttachedSettlementMassacreRequestForExternal(targetAgentIndex, SetsOwnedSettlementMassacreProfile.CancelRequestSource);
			}
			else if (startRequested && !massacreActive)
			{
				action = "start";
				handled = SettlementEntryTroopSelectionBehavior.TryStartOwnedOrAttachedSettlementMassacreForExternal(targetAgentIndex, SetsOwnedSettlementMassacreProfile.StartSource);
			}
			else if (requestPermission && !massacreActive && !anyRequestPending)
			{
				action = "request_permission";
				handled = SettlementEntryTroopSelectionBehavior.TryRecordOwnedOrAttachedSettlementMassacreRequestForExternal(targetAgentIndex, SetsOwnedSettlementMassacreProfile.RequestSource);
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] SETS owned/attached massacre tag action=" + action
				+ " handled=" + handled
				+ " activeBefore=" + massacreActive
				+ " pendingForSpeaker=" + requestPendingForSpeaker
				+ " anyPending=" + anyRequestPending
				+ " agent=" + targetAgentIndex);
			return handled;
		}
		catch (Exception ex)
		{
			content = ConversationActionPostprocessOwner.StripSetsOwnedSettlementMassacreActionTags(content);
			Logger.Log("ShoutBehavior", "[NativeConversation] SETS owned/attached massacre action failed: " + ex.Message);
			return false;
		}
	}
internal static bool TryProcessCustomPolicyAgendaActionTag(Hero targetHero, string chainName, string playerProposalText, ref string content, string npcReplyTextOverride = null)
	{
		if (string.IsNullOrWhiteSpace(content) || !CustomPolicyAgendaActionTagRegex.IsMatch(content))
		{
			return false;
		}
		string resolvedChainName = string.IsNullOrWhiteSpace(chainName) ? "native" : chainName.Trim();
		string npcReplyText = StripActionTagsForSceneSpeech(string.IsNullOrWhiteSpace(npcReplyTextOverride) ? content : npcReplyTextOverride);
		string failureReason = "";
		bool started = false;
		try
		{
			started = TeamModuleServices.Policy.TryProcessAcceptedAgendaTag(targetHero, resolvedChainName, playerProposalText, npcReplyText, ref content, out failureReason);
			Logger.Log("ShoutBehavior", "[CustomPolicyAgenda] dispatch chain=" + resolvedChainName + " ruler=" + (targetHero?.StringId ?? "null") + " started=" + started + " reason=" + (failureReason ?? ""));
		}
		catch (Exception ex)
		{
			failureReason = ex.GetType().Name + ": " + ex.Message;
			Logger.Log("ShoutBehavior", "[CustomPolicyAgenda] dispatch exception chain=" + resolvedChainName + " ruler=" + (targetHero?.StringId ?? "null") + " error=" + failureReason);
			PolicySystemLog.Failure("CustomPolicyAgenda", "dispatch-failure", "Policy proposal action dispatch failed. chain=" + resolvedChainName + " ruler=" + (targetHero?.StringId ?? "null"), failureReason);
		}
		finally
		{
			// Internal action tags must never leak into scene/native/courier-visible text,
			// including rejected or exceptional paths.
			content = CustomPolicyAgendaActionTagRegex.Replace(content ?? "", "").Trim();
		}
		return started;
	}
	internal const int DeferredPostprocessTargetUnavailableResult = -2;
	internal static readonly Regex CustomPolicyAgendaActionTagRegex = new Regex(Regex.Escape(CustomPolicyAgendaActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
	internal static readonly Regex TroopInspectionPrisonerSlaughterActionTagRegex = new Regex(Regex.Escape(TroopInspectionPrisonerSlaughterProfile.ActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
	internal static readonly Regex SetsOwnedSettlementMassacreCancelRequestActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.CancelRequestActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
	internal static readonly Regex SetsOwnedSettlementMassacreStopActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.StopActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
	internal static readonly Regex SetsOwnedSettlementMassacreStartActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.StartActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
	internal static readonly Regex SetsOwnedSettlementMassacreRequestActionTagRegex = new Regex(Regex.Escape(SetsOwnedSettlementMassacreProfile.RequestActionTag), RegexOptions.IgnoreCase | RegexOptions.Compiled);
	internal const string CustomPolicyAgendaActionTag = KingdomAgendaCustomPolicyBehavior.ActionTag;
	internal const string SiegeSurrenderActionTag = NpcSurrenderActionTag;
	internal const string NpcSurrenderActionTag = "[ACTION:NPC_SURRENDER]";
	internal static readonly Regex MeetingSceneShoutTauntTagRegex = new Regex("\\[ACTION:MEETING_TAUNT_(?:WARN|BATTLE)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
internal static bool TryResolveNativeConversationMeetingTauntParty(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, out PartyBase party)
	{
		party = null;
		try
		{
			Hero hero = targetHero ?? targetCharacter?.HeroObject;
			if (hero != null)
			{
				return false;
			}
			return TryResolveWildernessNonHeroRewardParty(targetHero, targetCharacter, targetAgentIndex, out party);
		}
		catch
		{
			party = null;
			return false;
		}
	}
internal static RewardSystemBehavior.RpItemIntroductionContext CreateRpItemIntroductionContextForReward(
		Hero giverHero,
		NpcDataPacket giverNpc,
		CharacterObject giverCharacter,
		int giverAgentIndex,
		string giverName,
		string currentPlayerText,
		string currentNpcText,
		bool includeNativeConversationSessionHistory = false)
	{
		try
		{
			string nonHeroMemoryId = "";
			string resolvedGiverName = (giverName ?? "").Trim();
			if (giverHero == null
				&& TryResolveWildernessNonHeroMemoryForExternal(
					giverNpc,
					null,
					giverCharacter,
					giverAgentIndex,
					out string memoryId,
					out string memoryName))
			{
				nonHeroMemoryId = memoryId;
				if (string.IsNullOrWhiteSpace(resolvedGiverName))
				{
					resolvedGiverName = memoryName;
				}
			}
			if (string.IsNullOrWhiteSpace(resolvedGiverName))
			{
				resolvedGiverName = giverCharacter?.Name?.ToString() ?? "";
			}
			return RewardSystemBehavior.CreateRpItemIntroductionContextForExternal(
				giverHero,
				nonHeroMemoryId,
				resolvedGiverName,
				currentPlayerText,
				StripActionTagsForSceneSpeech(currentNpcText),
				includeNativeConversationSessionHistory);
		}
		catch
		{
			// Context enrichment is optional; preserve existing reward processing on failure.
			return null;
		}
	}
internal static bool TryConsumeNativeConversationSiegeSurrenderTag(Hero targetHero, CharacterObject targetCharacter, ref string content, out int targetAgentIndex)
	{
		targetAgentIndex = -1;
		string pattern = Regex.Escape(SiegeSurrenderActionTag);
		if (string.IsNullOrWhiteSpace(content) || !Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
		{
			return false;
		}
		targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
		bool validContext = TryResolveNativeConversationSiegeSurrenderContext(targetHero, targetCharacter, targetAgentIndex, out var settlement, out var side, out var sideLabel);
		if (!validContext)
		{
			return false;
		}
		content = Regex.Replace(content, pattern, "", RegexOptions.IgnoreCase).Trim();
		Logger.Log("SiegeSurrender", "Accepted native/direct conversation siege surrender tag. Target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " settlement=" + (settlement?.StringId ?? "") + " side=" + (sideLabel ?? side.ToString()) + " agentIndex=" + targetAgentIndex);
		return true;
	}
internal static bool TryConsumeNativeConversationNpcSurrenderTag(Hero targetHero, CharacterObject targetCharacter, ref string content, out int targetAgentIndex)
	{
		targetAgentIndex = -1;
		if (string.IsNullOrWhiteSpace(content) || !Regex.IsMatch(content, "\\[ACTION:NPC_SURRENDER\\]", RegexOptions.IgnoreCase))
		{
			return false;
		}
		targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
		if (TryResolveNativeConversationSiegeSurrenderContext(targetHero, targetCharacter, targetAgentIndex, out var settlement, out var side, out var sideLabel))
		{
			content = Regex.Replace(content, "\\[ACTION:NPC_SURRENDER\\]", "", RegexOptions.IgnoreCase).Trim();
			Logger.Log("NpcSurrender", "Hidden native/direct conversation NPC surrender tag in siege context. Target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " settlement=" + (settlement?.StringId ?? "") + " side=" + (sideLabel ?? side.ToString()) + " agentIndex=" + targetAgentIndex);
			return false;
		}
		content = Regex.Replace(content, "\\[ACTION:NPC_SURRENDER\\]", "", RegexOptions.IgnoreCase).Trim();
		return true;
	}
internal void QueueNativeConversationNpcSurrender(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string reason)
	{
		string targetId = targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown";
		try
		{
			_post(delegate
			{
				LordEncounterBehavior.TryExecuteNpcSurrenderFromNativeConversation(targetHero, targetCharacter, targetAgentIndex, reason ?? "native_conversation_npc_surrender_tag");
			});
			Logger.Log("NpcSurrender", "Queued native/direct conversation NPC surrender. Target=" + targetId + " agentIndex=" + targetAgentIndex + " reason=" + (reason ?? "N/A"));
			TryDrainNativeConversationQueuedActions("npc_surrender_queued");
		}
		catch (Exception ex)
		{
			Logger.Log("NpcSurrender", "Queue native/direct conversation NPC surrender failed. Target=" + targetId + " error=" + ex.Message);
		}
	}
internal void TryDrainNativeConversationQueuedActions(string reason)
	{
		try
		{
			int pending = _queueCount();
			if (pending <= 0)
			{
				return;
			}
			Logger.Log("ShoutBehavior", "[NativeConversation] draining queued actions. Pending=" + pending + " missionActive=" + (Mission.Current != null) + " reason=" + (reason ?? "N/A"));
			if (!IsBannerlordMainThreadForNativeActions())
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] skip immediate drain from background thread. Pending=" + pending + " reason=" + (reason ?? "N/A") + " thread=" + Thread.CurrentThread.ManagedThreadId);
				return;
			}
			_drain.DrainMainThreadActionsForMissionTick();
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] drain queued actions failed. Reason=" + (reason ?? "N/A") + " error=" + ex.Message);
		}
	}
internal void RecordGeneratedNpcAfefFactsForNativeConversation(Hero targetHero, CharacterObject targetCharacter, IEnumerable<string> factLines)
	{
		if (factLines == null)
		{
			return;
		}
		targetHero ??= targetCharacter?.HeroObject;
		targetCharacter ??= targetHero?.CharacterObject;
		string npcName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
		if (string.IsNullOrWhiteSpace(npcName))
		{
			npcName = "NPC";
		}
		int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
		NpcDataPacket targetNpc = BuildNativeConversationNpcData(targetHero, targetCharacter);
		targetNpc.AgentIndex = targetAgentIndex;
		string nativeKey = BuildNativeConversationHistoryKey(targetHero, targetCharacter, npcName, targetAgentIndex, targetNpc);
		foreach (string rawFact in factLines)
		{
			string fact = NormalizeNativeConversationFactLineForPrompt(rawFact, "NPC");
			if (string.IsNullOrWhiteSpace(fact))
			{
				continue;
			}
			try
			{
				AppendNativeConversationSessionHistory(targetHero, targetCharacter, npcName, "系统", fact, "fact", targetAgentIndex: targetAgentIndex, npc: targetNpc);
				if (targetAgentIndex >= 0)
				{
					_queueAgentFact(targetAgentIndex, fact);
				}
				if (!string.IsNullOrWhiteSpace(nativeKey))
				{
					_queueNativeFact(nativeKey, fact);
				}
				Logger.Log("NativeConversationTrade", "recorded generated NPC AFEF fact target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? npcName) + " fact=" + fact.Replace("\r", "\\r").Replace("\n", "\\n"));
			}
			catch (Exception ex)
			{
				Logger.Log("NativeConversationTrade", "[WARN] Failed to record generated NPC AFEF fact: " + ex.Message);
			}
		}
	}
internal static bool IsNativeConversationWorldMapContext()
	{
		try
		{
			if (Mission.Current != null)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}
internal static bool TryProcessNativeConversationRawMeetingTauntTags(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out bool escalatedToBattle)
	{
		escalatedToBattle = false;
		if (!AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.MeetingTauntRuleId, targetAgentIndex))
		{
			StripBlockedTownTauntTags(ref content);
			return false;
		}
		if (string.IsNullOrWhiteSpace(content) || !IsNativeConversationWorldMapContext())
		{
			return false;
		}
		try
		{
			if (!MeetingSceneShoutTauntTagRegex.IsMatch(content))
			{
				return false;
			}
			PartyBase nonHeroParty = null;
			if (targetHero == null)
			{
				TryResolveNativeConversationMeetingTauntParty(targetHero, targetCharacter, targetAgentIndex, out nonHeroParty);
			}
			if (targetHero == null && nonHeroParty == null)
			{
				return false;
			}
			bool result = LordEncounterBehavior.TryProcessMeetingTauntAction(targetHero, nonHeroParty, ref content, out escalatedToBattle);
			if (result)
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] raw meeting taunt tag consumed before postprocess. target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? nonHeroParty?.Name?.ToString() ?? "") + " escalated=" + escalatedToBattle);
			}
			return result;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] raw meeting taunt handling failed: " + ex.Message);
			return false;
		}
	}
internal static bool TryProcessNativeConversationSceneTauntTags(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out bool escalatedToFight, Func<Hero, CharacterObject, int, string, string, bool> scheduleFight)
	{
		escalatedToFight = false;
		if (!AfGcczShoutBridge.ShouldAllowAfRuleForCurrentStage(TownAfRuleRoutingPolicy.MeetingTauntRuleId, targetAgentIndex))
		{
			StripBlockedTownTauntTags(ref content);
			return false;
		}
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		try
		{
			if (SceneTauntBehavior.HasSceneTauntFightTagForExternal(content))
			{
				if (SceneTauntBehavior.TryConsumeSceneTauntTagsForDelayedFightExternal(targetHero, targetCharacter, targetAgentIndex, ref content, out var hadWarnTag, out var hadFightTag, out var targetKey))
				{
					bool scheduled = hadFightTag && scheduleFight?.Invoke(targetHero, targetCharacter, targetAgentIndex, targetKey, "native_conversation_scene_taunt_fight_tag") == true;
					escalatedToFight = scheduled;
					Logger.Log("ShoutBehavior", "[NativeConversation] scene taunt tag consumed for delayed fight. target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agentIndex=" + targetAgentIndex + " warn=" + hadWarnTag + " fight=" + hadFightTag + " scheduled=" + scheduled);
					return true;
				}
			}
			bool result = SceneTauntBehavior.TryProcessSceneTauntAction(targetHero, targetCharacter, targetAgentIndex, ref content, out escalatedToFight);
			if (result)
			{
				Logger.Log("ShoutBehavior", "[NativeConversation] scene taunt tag consumed. target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "unknown") + " agentIndex=" + targetAgentIndex + " escalated=" + escalatedToFight);
				if (escalatedToFight)
				{
					CloseNativeConversationForSceneMechanism("native_scene_taunt_fight");
				}
			}
			return result;
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[NativeConversation] scene taunt handling failed: " + ex.Message);
			return false;
		}
	}
internal static void StripBlockedTownTauntTags(ref string content)
	{
		if (string.IsNullOrWhiteSpace(content))
		{
			return;
		}

		content = MeetingSceneShoutTauntTagRegex.Replace(content, "");
		content = Regex.Replace(content, "\\[ACTION:SCENE_TAUNT_(?:WARN|FIGHT)\\]", "", RegexOptions.IgnoreCase).Trim();
	}
internal static void SubmitNativeConversationSceneActionObservation(string replyText, int agentIndex)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(replyText) || agentIndex < 0 ||
				!SceneActionsRuntimeHost.IsInitialized ||
				SceneActionsRuntimeHost.Settings?.NpcSceneShoutReplyEnabled != true ||
				Mission.Current == null)
			{
				return;
			}
			Mission mission = Mission.Current;
			Agent speaker = mission.Agents?.FirstOrDefault(agent => agent != null && agent.Index == agentIndex);
			if (speaker == null)
			{
				return;
			}
			bool submitted = SceneActionsRuntimeHost.SubmitNpcReply(
				Guid.NewGuid(),
				mission,
				speaker,
				replyText,
				mission.CurrentTime);
			if (submitted)
			{
				SceneActionsLog.Info(
					"NATIVE_CONVERSATION_ACTION",
					"Submitted native AI conversation reply to the natural-action parser. Agent=" + agentIndex);
			}
		}
		catch (Exception ex)
		{
			// A parser failure must not discard an otherwise valid native conversation.
			SceneActionsLog.Warning(
				"NATIVE_CONVERSATION_ACTION",
				"Natural-action observation failed open: " + ex.Message);
		}
	}
internal bool TryConsumeSceneNpcSurrenderTag(NpcDataPacket speaker, ref string content, out Hero targetHero, out CharacterObject targetCharacter, out int targetAgentIndex)
	{
		targetHero = null;
		targetCharacter = null;
		targetAgentIndex = speaker?.AgentIndex ?? (-1);
		if (string.IsNullOrWhiteSpace(content) || !Regex.IsMatch(content, "\\[ACTION:NPC_SURRENDER\\]", RegexOptions.IgnoreCase))
		{
			return false;
		}
		content = Regex.Replace(content, "\\[ACTION:NPC_SURRENDER\\]", "", RegexOptions.IgnoreCase).Trim();
		try
		{
			int resolvedAgentIndex = targetAgentIndex;
			Agent agent = (resolvedAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == resolvedAgentIndex) : null;
			targetCharacter = agent?.Character as CharacterObject;
			targetHero = targetCharacter?.HeroObject;
			if (targetHero == null && targetAgentIndex >= 0)
			{
				targetHero = SceneAgentIdentityPromptCaptureAdapter.ResolveHeroFromAgentIndex(targetAgentIndex);
			}
			if (targetCharacter == null)
			{
				targetCharacter = targetHero?.CharacterObject;
			}
		}
		catch
		{
		}
		Logger.Log("NpcSurrender", "Consumed scene dialog NPC surrender tag. Target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? speaker?.Name ?? "unknown") + " agentIndex=" + targetAgentIndex);
		return true;
	}
internal static void StripMeetingTauntTagsForSceneConversation(ref string content)
	{
		if (string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		try
		{
			content = MeetingSceneShoutTauntTagRegex.Replace(content, "").Trim();
		}
		catch
		{
		}
	}
}
