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
	private static readonly object PostprocessThrottleLock = new object();

	private static int _postprocessThrottleCredit = SiegePostprocessFrequencyProfile.FrequencyWindowSize;

	private static int _postprocessThrottleSkipLogCounter;

	internal static string RuleId => SiegePostprocessRuleCatalog.RuleId;

	internal static string InjectedRuleBlockMarker => SiegePostprocessRuleCatalog.InjectedRuleBlockMarker;

	internal static bool IsActive()
	{
		return SiegeAiInterventionBehavior.ShouldRunSiegeInterventionPostprocessForExternal();
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
			|| ruleInspectionBlock.IndexOf("【附加规则:" + RuleId + "】", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	internal static bool ShouldRunPostprocessFromPreprocessHits(IEnumerable<string> preprocessRuleHits)
	{
		return IsActive() && HasPreprocessRuleHit(preprocessRuleHits);
	}

	internal static bool ShouldRunPostprocessFromPrompt(string ruleInspectionBlock, IEnumerable<string> preprocessRuleHits)
	{
		return IsActive() && (HasInjectedRuleBlock(ruleInspectionBlock) || HasPreprocessRuleHit(preprocessRuleHits));
	}

	internal static bool ShouldContinuePostprocess(bool alreadySelected, IEnumerable<string> preprocessRuleHits)
	{
		return IsActive() && (alreadySelected || HasPreprocessRuleHit(preprocessRuleHits));
	}

	internal static bool ShouldAllowPostprocessByFrequency(bool selected, string playerText, bool replyIsDirectPlayerResponse, string source)
	{
		if (!selected)
		{
			return false;
		}
		if (!IsActive())
		{
			ResetPostprocessThrottle();
			return false;
		}
		if (DuelSettings.IsGcczPostprocessUnlimitedFrequencyEnabled())
		{
			ResetPostprocessThrottle();
			return true;
		}
		if (SiegePostprocessFrequencyProfile.ShouldBypassThrottleForPlayerIntentReview(playerText, replyIsDirectPlayerResponse))
		{
			Logger.Log("Logic", "[GcczShoutBridge] postprocess throttle bypassed for AI review candidate source=" + NormalizeThrottleSource(source) + " reason=" + SiegePostprocessFrequencyProfile.AiReviewCandidateBypassSource);
			return true;
		}

		int limit = DuelSettings.GetGcczPostprocessFrequencyLimit();
		if (limit >= SiegePostprocessFrequencyProfile.FrequencyWindowSize)
		{
			ResetPostprocessThrottle();
			return true;
		}

		bool shouldLogSkip = false;
		lock (PostprocessThrottleLock)
		{
			_postprocessThrottleCredit = Math.Min(
				SiegePostprocessFrequencyProfile.FrequencyWindowSize,
				_postprocessThrottleCredit + limit);
			if (_postprocessThrottleCredit >= SiegePostprocessFrequencyProfile.FrequencyWindowSize)
			{
				_postprocessThrottleCredit -= SiegePostprocessFrequencyProfile.FrequencyWindowSize;
				_postprocessThrottleSkipLogCounter = 0;
				return true;
			}

			_postprocessThrottleSkipLogCounter++;
			shouldLogSkip = _postprocessThrottleSkipLogCounter == 1 || _postprocessThrottleSkipLogCounter % SiegePostprocessFrequencyProfile.FrequencyWindowSize == 0;
		}

		if (shouldLogSkip)
		{
			Logger.Log("Logic", "[GcczShoutBridge] postprocess skipped by frequency limit=" + limit + "/10 source=" + NormalizeThrottleSource(source));
		}
		return false;
	}

	private static void ResetPostprocessThrottle()
	{
		lock (PostprocessThrottleLock)
		{
			_postprocessThrottleCredit = SiegePostprocessFrequencyProfile.FrequencyWindowSize;
			_postprocessThrottleSkipLogCounter = 0;
		}
	}

	internal static void ResetPostprocessFrequencyForMissionBoundary(string source)
	{
		ResetPostprocessThrottle();
		Logger.Log("Logic", "[GcczShoutBridge] postprocess throttle reset source=" + NormalizeThrottleSource(source));
	}

	private static string NormalizeThrottleSource(string source)
	{
		return string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim();
	}

	internal static void AppendRuntimePromptToShoutContext(MyBehavior.ShoutPromptContext shoutPromptContext, Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string cultureIdOverride)
	{
		try
		{
			if (shoutPromptContext == null)
			{
				return;
			}
			string siegePrompt = SiegeAiInterventionBehavior.BuildRuntimePromptForPromptContext(targetHero, targetCharacter, targetAgentIndex, cultureIdOverride);
			if (string.IsNullOrWhiteSpace(siegePrompt))
			{
				return;
			}
			string siegeSection = InjectedRuleBlockMarker + "\n" + siegePrompt.Trim();
			shoutPromptContext.Extras = string.IsNullOrWhiteSpace(shoutPromptContext.Extras)
				? siegeSection
				: (shoutPromptContext.Extras.TrimEnd() + "\n" + siegeSection);
			EnsurePreprocessRuleHit(shoutPromptContext);
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[GcczShoutBridge] prompt append failed: " + ex.Message);
		}
	}

	internal static List<PostprocessRuleEntry> BuildPostprocessRules(bool selected)
	{
		if (!selected)
		{
			return null;
		}
		return SiegeAiInterventionBehavior.BuildRuntimePostprocessRulesForExternal() ?? new List<PostprocessRuleEntry>();
	}

	internal static string BuildPostprocessContext(bool selected, int targetAgentIndex, bool replyIsDirectPlayerResponse)
	{
		return selected
			? SiegeAiInterventionBehavior.BuildRuntimePostprocessContextForExternal(targetAgentIndex, replyIsDirectPlayerResponse)
			: string.Empty;
	}

	internal static string BuildImmediateReactionIdentityOverride(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		return IsActive()
			? SiegeAiInterventionBehavior.BuildImmediateReactionIdentityOverrideForExternal(targetHero, targetCharacter, targetAgentIndex)
			: string.Empty;
	}

	internal static string NormalizePostprocessTags(bool selected, string raw, List<PostprocessRuleEntry> rules)
	{
		return selected ? SiegeAiInterventionBehavior.NormalizeSiegeInterventionPostprocessTagsForExternal(raw, rules) : string.Empty;
	}

	internal static bool TryProcessActionTags(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string text, out bool actionHandled, bool replyIsDirectPlayerResponse = false)
	{
		return SiegeAiInterventionBehavior.TryProcessAiActionTags(targetHero, targetCharacter, targetAgentIndex, ref text, out actionHandled, replyIsDirectPlayerResponse);
	}

	internal static bool ShouldCaptureSharedReliefTransfer(int targetAgentIndex)
	{
		return targetAgentIndex >= 0 && SiegeAiInterventionBehavior.ShouldCapturePlayerGiveForSharedCivilianReliefForExternal();
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

	private static void EnsurePreprocessRuleHit(MyBehavior.ShoutPromptContext shoutPromptContext)
	{
		if (shoutPromptContext.PreprocessRuleIds == null)
		{
			shoutPromptContext.PreprocessRuleIds = new List<string>();
		}
		if (!shoutPromptContext.PreprocessRuleIds.Any(x => string.Equals(x, RuleId, StringComparison.OrdinalIgnoreCase)))
		{
			shoutPromptContext.PreprocessRuleIds.Add(RuleId);
		}
	}
}

