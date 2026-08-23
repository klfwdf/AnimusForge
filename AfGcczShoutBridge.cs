using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace AnimusForge;

/// <summary>
/// Thin AF-side bridge for the active GCCZ siege-aftermath intervention scene.
/// Keep Bannerlord/AF live types here and GCCZ wording/policy in AnimusForge.SiegeAftermathIntervention.
/// </summary>
internal static class AfGcczShoutBridge
{
	private static readonly object NpcResponseBudgetLock = new object();
	private static readonly SiegeNpcResponseEventBudget NpcResponseBudget = new SiegeNpcResponseEventBudget();

	internal static string RuleId => SiegePostprocessRuleCatalog.RuleId;

	internal static string InjectedRuleBlockMarker => SiegePostprocessRuleCatalog.InjectedRuleBlockMarker;

	internal static string MeetingTauntRuleBlockMarker => "\u3010\u9644\u52a0\u89c4\u5219:meeting_taunt\u3011";

	internal static bool IsActive()
	{
		return IsTownOrCastleAftermathActive() || VillageAftermathBehavior.IsActive();
	}

	private static bool IsTownOrCastleAftermathActive()
	{
		return SiegeAiInterventionBehavior.ShouldRunSiegeInterventionPostprocessForExternal();
	}

	internal static bool ShouldUseExclusivePreprocessRuleRouting()
	{
		return IsTownOrCastleAftermathActive()
			&& GetTownDialoguePhase() != TownAfDialoguePhase.NormalOccupation;
	}

	internal static bool ShouldUseExclusivePostprocessRuleRouting()
	{
		return ShouldUseExclusivePreprocessRuleRouting();
	}

	internal static bool ShouldBypassPreprocessForActiveScene()
	{
		return ShouldUseExclusivePreprocessRuleRouting();
	}

	internal static bool IsExclusivePreprocessRuleId(string ruleId)
	{
		return string.Equals((ruleId ?? string.Empty).Trim(), RuleId, StringComparison.OrdinalIgnoreCase);
	}

	internal static List<string> BuildRuntimePreprocessRuleExclusions(IEnumerable<string> ruleIds)
	{
		if (!IsTownOrCastleAftermathActive())
		{
			return new List<string>();
		}

		TownAfDialoguePhase phase = GetTownDialoguePhase();
		if (phase == TownAfDialoguePhase.NormalOccupation)
		{
			return TownAfRuleRoutingPolicy.BuildExcludedRuleIds(phase, ruleIds).ToList();
		}

		var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string ruleId in ruleIds ?? Enumerable.Empty<string>())
		{
			string id = (ruleId ?? string.Empty).Trim();
			if (!string.IsNullOrWhiteSpace(id) && !IsExclusivePreprocessRuleId(id))
			{
				excluded.Add(id);
			}
		}
		foreach (string blockedRuleId in TownAfRuleRoutingPolicy.GetCollisionBlockedRuleIds())
		{
			excluded.Add(blockedRuleId);
		}
		return excluded.ToList();
	}

	internal static void AddRuntimePreprocessRuleExclusions(HashSet<string> excludedRuleIds)
	{
		if (excludedRuleIds == null || !IsTownOrCastleAftermathActive())
		{
			return;
		}
		excludedRuleIds.Remove(RuleId);
		foreach (string ruleId in BuildRuntimePreprocessRuleExclusions(AIConfigHandler.GetEnabledGuardrailRuleIdsForExternal()))
		{
			string id = (ruleId ?? string.Empty).Trim();
			if (!string.IsNullOrWhiteSpace(id))
			{
				excludedRuleIds.Add(id);
			}
		}
	}

	internal static bool ShouldAllowAfRuleForCurrentStage(string ruleId)
	{
		if (!IsTownOrCastleAftermathActive())
		{
			return true;
		}

		TownAfDialoguePhase phase = GetTownDialoguePhase();
		if (phase == TownAfDialoguePhase.Inactive)
		{
			phase = TownAfDialoguePhase.AtrocityCombat;
		}
		return TownAfRuleRoutingPolicy.IsAllowed(phase, ruleId);
	}

	internal static bool ShouldUseTownPostprocessDecisionContract()
	{
		return GetTownDialoguePhase() != TownAfDialoguePhase.Inactive;
	}

	internal static string ValidateTownPostprocessDecision(string normalizedTags)
	{
		return ShouldUseTownPostprocessDecisionContract()
			? TownPostprocessDecisionValidator.KeepSinglePrimaryAction(normalizedTags)
			: normalizedTags ?? string.Empty;
	}

	internal static bool HasPreprocessRuleHit(IEnumerable<string> preprocessRuleHits)
	{
		if (preprocessRuleHits == null)
		{
			return false;
		}
		foreach (string hit in preprocessRuleHits)
		{
			if (string.Equals((hit ?? string.Empty).Trim(), RuleId, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool HasInjectedRuleBlock(string ruleInspectionBlock)
	{
		if (string.IsNullOrWhiteSpace(ruleInspectionBlock))
		{
			return false;
		}
		return ruleInspectionBlock.IndexOf(InjectedRuleBlockMarker, StringComparison.OrdinalIgnoreCase) >= 0
			|| ruleInspectionBlock.IndexOf(VillageAftermathRuntimePromptProfile.InjectedRuleBlockMarker, StringComparison.OrdinalIgnoreCase) >= 0
			|| ruleInspectionBlock.IndexOf("【附加规则:" + RuleId + "】", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	internal static bool ShouldRunPostprocessForActiveScene()
	{
		return IsActive();
	}

	internal static bool ShouldRunPostprocessFromPrompt(string ruleInspectionBlock, IEnumerable<string> preprocessRuleHits)
	{
		return IsActive() && (ShouldBypassPreprocessForActiveScene() || HasInjectedRuleBlock(ruleInspectionBlock) || HasPreprocessRuleHit(preprocessRuleHits));
	}

	internal static bool ShouldContinuePostprocess(bool alreadySelected, IEnumerable<string> preprocessRuleHits)
	{
		return IsActive() && (ShouldBypassPreprocessForActiveScene() || alreadySelected || HasPreprocessRuleHit(preprocessRuleHits));
	}

	private static string NormalizeThrottleSource(string source)
	{
		return string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim();
	}

	internal static bool ShouldUseTownNpcResponseBudgetForExternal()
	{
		return GetTownDialoguePhase() != TownAfDialoguePhase.Inactive;
	}

	internal static bool TryClaimNpcResponseForExternal(
		string eventId,
		int speakerAgentIndex,
		SiegeNpcResponseEventOrigin origin,
		int availableCount,
		int pendingRequestCount,
		string source,
		out SiegeNpcResponseDecision decision)
	{
		bool activeTownStage = GetTownDialoguePhase() != TownAfDialoguePhase.Inactive;
		bool unlimited = DuelSettings.IsGcczNpcResponseUnlimitedEnabled();
		int configuredLimit = DuelSettings.GetGcczNpcResponseLimit();
		lock (NpcResponseBudgetLock)
		{
			if (activeTownStage && !NpcResponseBudget.IsSceneActive)
			{
				NpcResponseBudget.BeginScene();
			}
			else if (!activeTownStage && NpcResponseBudget.IsSceneActive)
			{
				NpcResponseBudget.EndScene();
			}

			decision = NpcResponseBudget.TryClaim(
				eventId,
				speakerAgentIndex >= 0 ? speakerAgentIndex.ToString() : string.Empty,
				origin,
				unlimited,
				configuredLimit,
				availableCount,
				pendingRequestCount);
		}

		GcczDiagnosticLog.LogVerbose("ResponseBudget", "event=" + NormalizeResponseEventId(eventId)
			+ " source=" + NormalizeThrottleSource(source)
			+ " unlimited=" + unlimited
			+ " configured=" + configuredLimit
			+ " available=" + Math.Max(0, availableCount)
			+ " allowed=" + decision.AllowedCount
			+ " claimed=" + decision.ClaimedCount
			+ " accepted=" + decision.Allowed
			+ " reason=" + decision.Reason);
		return decision.Allowed;
	}

	internal static void ResetNpcResponseBudgetForExternal(string source)
	{
		lock (NpcResponseBudgetLock)
		{
			NpcResponseBudget.EndScene();
		}
		GcczDiagnosticLog.LogVerbose("ResponseBudget", "reset source=" + NormalizeThrottleSource(source));
	}

	private static string NormalizeResponseEventId(string eventId)
	{
		string normalized = (eventId ?? string.Empty).Trim();
		return string.IsNullOrWhiteSpace(normalized) ? "invalid" : normalized;
	}

	internal static void AppendRuntimePromptToShoutContext(MyBehavior.ShoutPromptContext shoutPromptContext, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string cultureIdOverride)
	{
		try
		{
			if (shoutPromptContext == null)
			{
				return;
			}
			bool villageActive = VillageAftermathBehavior.IsActive();
			bool siegeAftermathActive = IsTownOrCastleAftermathActive();
			string siegePrompt = villageActive
				? VillageAftermathBehavior.BuildRuntimePromptForExternal()
				: siegeAftermathActive
					? SiegeAiInterventionBehavior.BuildRuntimePromptForPromptContext(targetHero, targetCharacter, targetAgentIndex, cultureIdOverride)
					: GcczTownRuleMemoryRuntimeBridge.BuildLocalDialoguePromptContext(targetHero, targetCharacter, targetAgentIndex);
			if (string.IsNullOrWhiteSpace(siegePrompt))
			{
				return;
			}
			string marker = villageActive
				? VillageAftermathRuntimePromptProfile.InjectedRuleBlockMarker
				: siegeAftermathActive
					? SiegeAiInterventionBehavior.GetRuntimeInjectedRuleBlockMarkerForExternal()
					: "[GCCZ_TOWN_LOCAL_MEMORY]";
			string siegeSection = (string.IsNullOrWhiteSpace(marker) ? InjectedRuleBlockMarker : marker.Trim()) + "\n" + siegePrompt.Trim();
			shoutPromptContext.Extras = string.IsNullOrWhiteSpace(shoutPromptContext.Extras)
				? siegeSection
				: (shoutPromptContext.Extras.TrimEnd() + "\n" + siegeSection);
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[GcczShoutBridge] prompt append failed: " + ex.Message);
		}
	}

	internal static List<PostprocessRuleEntry> BuildPostprocessRules(
		bool selected,
		int targetAgentIndex,
		bool replyIsDirectPlayerResponse,
		string playerText)
	{
		if (!selected)
		{
			return null;
		}
		if (VillageAftermathBehavior.IsActive())
		{
			return VillageAftermathBehavior.BuildPostprocessRulesForExternal(replyIsDirectPlayerResponse);
		}
		return SiegeAiInterventionBehavior.BuildRuntimePostprocessRulesForExternal(
			targetAgentIndex,
			replyIsDirectPlayerResponse,
			playerText) ?? new List<PostprocessRuleEntry>();
	}

	internal static string BuildPostprocessContext(
		bool selected,
		int targetAgentIndex,
		bool replyIsDirectPlayerResponse,
		string playerText = null)
	{
		if (!selected)
		{
			return string.Empty;
		}
		return VillageAftermathBehavior.IsActive()
			? VillageAftermathBehavior.BuildPostprocessContextForExternal(replyIsDirectPlayerResponse)
			: SiegeAiInterventionBehavior.BuildRuntimePostprocessContextForExternal(targetAgentIndex, replyIsDirectPlayerResponse, playerText);
	}

	internal static string AppendTownPostprocessDecisionContract(
		string userPrompt,
		bool useTownContract,
		IEnumerable<PostprocessRuleEntry> rules)
	{
		if (!useTownContract)
		{
			return userPrompt ?? string.Empty;
		}

		IEnumerable<string> eligibleTags = (rules ?? Enumerable.Empty<PostprocessRuleEntry>())
			.Select(rule => (rule?.Tag ?? string.Empty).Trim())
			.Where(tag => !string.IsNullOrWhiteSpace(tag));
		string contract = TownPromptComposer.BuildPostprocessContract(
			eligibleTags,
			GcczTownPromptResourceProvider.GetCatalog());
		if (string.IsNullOrWhiteSpace(contract))
		{
			return userPrompt ?? string.Empty;
		}

		return string.IsNullOrWhiteSpace(userPrompt)
			? contract
			: userPrompt.TrimEnd() + "\n\n" + contract;
	}

	private static TownAfDialoguePhase GetTownDialoguePhase()
	{
		return SiegeAiInterventionBehavior.GetTownAfDialoguePhaseForExternal();
	}

	internal static string BuildImmediateReactionIdentityOverride(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		return IsTownOrCastleAftermathActive()
			? SiegeAiInterventionBehavior.BuildImmediateReactionIdentityOverrideForExternal(targetHero, targetCharacter, targetAgentIndex)
			: string.Empty;
	}

	internal static bool ShouldUsePersistentPersonalMemory(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		return !IsTownOrCastleAftermathActive()
			|| SiegeAiInterventionBehavior.ShouldUsePersistentPersonalMemoryForExternal(targetHero, targetCharacter, targetAgentIndex);
	}

	internal static string NormalizePostprocessTags(bool selected, string raw, List<PostprocessRuleEntry> rules)
	{
		if (!selected)
		{
			return string.Empty;
		}
		return VillageAftermathBehavior.IsActive()
			? VillageAftermathBehavior.NormalizePostprocessTagsForExternal(raw, rules)
			: SiegeAiInterventionBehavior.NormalizeSiegeInterventionPostprocessTagsForExternal(raw, rules);
	}

	internal static bool TryProcessActionTags(
		Hero targetHero,
		CharacterObject targetCharacter,
		int targetAgentIndex,
		ref string text,
		out bool actionHandled,
		bool replyIsDirectPlayerResponse = false,
		string playerText = null,
		string speakerReplyText = null)
	{
		if (VillageAftermathBehavior.IsActive())
		{
			return VillageAftermathBehavior.TryProcessActionTagsForExternal(
				targetAgentIndex,
				ref text,
				out actionHandled,
				replyIsDirectPlayerResponse);
		}
		return SiegeAiInterventionBehavior.TryProcessAiActionTags(
			targetHero,
			targetCharacter,
			targetAgentIndex,
			ref text,
			out actionHandled,
			replyIsDirectPlayerResponse,
			playerText,
			speakerReplyText);
	}

	internal static bool TryProcessDirectSceneCommand(int targetAgentIndex, string playerText, bool replyIsDirectPlayerResponse, out bool actionHandled)
	{
		return SiegeAiInterventionBehavior.TryProcessDirectSceneCommandForExternal(targetAgentIndex, playerText, replyIsDirectPlayerResponse, out actionHandled);
	}

	internal static bool ShouldCaptureSharedReliefTransfer(int targetAgentIndex)
	{
		return targetAgentIndex >= 0
			&& IsTownOrCastleAftermathActive()
			&& SiegeAiInterventionBehavior.ShouldCapturePlayerGiveForSharedCivilianReliefForExternal();
	}

	internal static bool CaptureSharedReliefGoldTransfer(int targetAgentIndex, int goldAmount)
	{
		return SiegeAiInterventionBehavior.RecordSharedCivilianReliefTransferForExternal(
			targetAgentIndex,
			goldAmount,
			null,
			0,
			null,
			0,
			SiegeSharedReliefBridgeProfile.ShoutGiveGoldSource);
	}

	internal static bool CaptureSharedReliefItemTransfer(int targetAgentIndex, string itemId, int itemAmount, ItemObject item, int unitValue)
	{
		return SiegeAiInterventionBehavior.RecordSharedCivilianReliefTransferForExternal(
			targetAgentIndex,
			0,
			itemId,
			itemAmount,
			item,
			unitValue,
			SiegeSharedReliefBridgeProfile.ShoutGiveItemSource);
	}

}

