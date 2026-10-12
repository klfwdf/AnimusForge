using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public static class KingdomAgendaCustomPolicyBehavior
{
	public const string ActionTag = "[ACTION:AGENDA:CUSTOM_POLICY]";

	private const int MaxHistoryChars = 4800;
	private const int ReservationLifetimeMinutes = 12;
	private static readonly Regex ActionTagRegex = new Regex("\\[ACTION:AGENDA:CUSTOM_POLICY\\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
	private static readonly Regex WhitespaceRegex = new Regex("\\s+", RegexOptions.CultureInvariant | RegexOptions.Compiled);
	private static readonly object ReservationLock = new object();
	private static readonly Dictionary<string, ProposalReservation> ActiveReservations = new Dictionary<string, ProposalReservation>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, ProposalReservation> RecentRequests = new Dictionary<string, ProposalReservation>(StringComparer.OrdinalIgnoreCase);

	private sealed class ProposalReservation
	{
		public long RuntimeGeneration;
		public long ExpiresUtcTicks;
		public string RequestKey;
	}

	public static bool IsEligibleTargetForExternal(Hero ruler, out string failureReason)
	{
		failureReason = "";
		try
		{
			if (Campaign.Current == null)
			{
				failureReason = "Policy system has not entered campaign runtime state.";
				return false;
			}
			if (ruler == null)
			{
				failureReason = "Cannot find target ruler for policy proposal.";
				return false;
			}
			if (ruler == Hero.MainHero)
			{
				failureReason = "Player cannot propose policy to themselves via ruler proposal tag.";
				return false;
			}
			if (ruler.IsDead || !ruler.IsAlive)
			{
				failureReason = "Target ruler is dead.";
				return false;
			}

			Clan clan = ruler.Clan;
			Kingdom kingdom = clan?.Kingdom;
			if (clan == null || clan.IsEliminated || kingdom == null || kingdom.IsEliminated)
			{
				failureReason = "Target does not belong to a valid active kingdom.";
				return false;
			}
			if (kingdom.Leader != ruler && kingdom.RulingClan?.Leader != ruler)
			{
				failureReason = "Only the current ruler of the kingdom can accept policy proposals.";
				return false;
			}
			if (!DuelSettings.IsNpcRulerPolicyEnabledForExternal())
			{
				failureReason = "Non-player ruler policy feature is currently disabled.";
				return false;
			}
			if (!PolicyLlmClient.IsConfiguredForNpcPolicy(out _))
			{
				failureReason = "Intelligence service for ruler policy is not configured, check mod settings.";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			failureReason = "Ruler policy proposal eligibility check failed; technical details logged.";
			PolicySystemLog.Failure("Proposal", "proposal-eligibility-failed", ex.Message, ex.ToString());
			return false;
		}
	}

	public static List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero ruler)
	{
		List<PostprocessRuleEntry> result = new List<PostprocessRuleEntry>();
		if (!IsEligibleTargetForExternal(ruler, out string failureReason))
		{
			PolicySystemLog.Lifecycle("Agenda", "postprocess-rule-eligibility", "ineligible", new PolicyLogContext
			{
				TargetHash = PolicySystemLog.HashSensitive(SafeHeroId(ruler)),
				MessageChars = failureReason?.Length ?? 0,
				MessageHash = PolicySystemLog.HashSensitive(failureReason)
			});
			return result;
		}

		Kingdom kingdom = ruler.Clan?.Kingdom;
		foreach (PostprocessRuleEntry rule in AIConfigHandler.GetGuardrailRulePostprocessRules("kingdom_agenda") ?? new List<PostprocessRuleEntry>())
		{
			if (!string.Equals((rule?.Tag ?? "").Trim(), ActionTag, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			result.Add(new PostprocessRuleEntry
			{
				Tag = ActionTag,
				Description = (rule.Description ?? "").Trim()
			});
		}
		PolicySystemLog.Lifecycle("Agenda", "postprocess-rule-eligibility", "eligible", new PolicyLogContext
		{
			TargetHash = PolicySystemLog.HashSensitive(SafeHeroId(ruler) + "|" + SafeKingdomId(kingdom)),
			TargetCount = 1,
			Counts = new Dictionary<string, int>(StringComparer.Ordinal) { ["rules"] = result.Count }
		});
		return result;
	}

	public static bool TryProcessAcceptedAgendaTag(Hero ruler, string chainName, string playerProposalText, string npcReplyText, ref string content, out string failureReason)
	{
		failureReason = "";
		string sourceContent = content ?? "";
		bool hasTag;
		try
		{
			hasTag = ActionTagRegex.IsMatch(sourceContent);
			content = ActionTagRegex.Replace(sourceContent, "").Trim();
		}
		catch (Exception ex)
		{
			// Fall back to a case-insensitive literal cleanup so the internal tag is never shown.
			hasTag = sourceContent.IndexOf(ActionTag, StringComparison.OrdinalIgnoreCase) >= 0;
			content = RemoveLiteralTag(sourceContent).Trim();
			if (!hasTag)
			{
				failureReason = "Failed to parse ruler policy proposal tag: " + ex.Message;
				return false;
			}
		}
		if (!hasTag)
		{
			return false;
		}

		string chain = NormalizeChainName(chainName);
		string proposal = AnimusForgeTextInputSanitizer.SanitizeMultiline(playerProposalText ?? "", AnimusForgeTextInputSanitizer.MaxPolicyContentChars).Trim();
		string reply = AnimusForgeTextInputSanitizer.SanitizeMultiline(npcReplyText ?? "", AnimusForgeTextInputSanitizer.MaxNativeConversationChars).Trim();
		if (string.IsNullOrWhiteSpace(proposal))
		{
			failureReason = "Player did not provide a draftable policy proposal.";
			LogFailure(ruler, chain, "proposal-empty", failureReason);
			return false;
		}
		if (!IsEligibleTargetForExternal(ruler, out failureReason))
		{
			LogFailure(ruler, chain, "proposal-target-ineligible", failureReason);
			return false;
		}

		Kingdom kingdom = ruler.Clan?.Kingdom;
		string scopeKey = BuildScopeKey(ruler, kingdom);
		string requestKey = scopeKey + ":" + ComputeStableHash(NormalizeProposalForKey(proposal));
		if (!TryAcquireReservation(scopeKey, requestKey, out failureReason))
		{
			LogFailure(ruler, chain, "proposal-duplicate-blocked", failureReason);
			return false;
		}

		try
		{
			string historyContext = BuildBoundedHistoryContext(ruler, proposal, reply, chain);
			PolicySystemLog.Write("Proposal", "generation-start",
				"chain=" + chain
				+ " ruler=" + SafeHeroId(ruler)
				+ " kingdom=" + SafeKingdomId(kingdom)
				+ " proposalChars=" + proposal.Length
				+ " request=" + RequestSuffix(requestKey));

			if (!NpcRulerPolicyBehavior.TryStartSuggestedPolicyForExternal(ruler, proposal, reply, historyContext, chain, out failureReason))
			{
				ReleaseReservation(scopeKey, requestKey);
				failureReason = string.IsNullOrWhiteSpace(failureReason) ? PolicyNpcCompatibilityTexts.Get("Npc_Legacy_97dd48c363c291df") : failureReason.Trim();
				LogFailure(ruler, chain, "proposal-queue-failed", failureReason);
				return false;
			}

			try
			{
				MarkRequestAccepted(scopeKey, requestKey);
			}
			catch (Exception ex)
			{
				PolicySystemLog.Failure("Proposal", "proposal-dedup-state-failed",
					"ruler=" + SafeHeroId(ruler) + " kingdom=" + SafeKingdomId(kingdom) + " error=" + ex.Message);
			}
			try
			{
				InformationManager.DisplayMessage(new InformationMessage(
					PolicyUiTexts.Get(
						"AFPOL_UI_AgendaRulerAcceptedDrafting",
						"The ruler accepted the proposal; AnimusForge is drafting it and will submit it directly to the target kingdom's agenda once ready, with no further confirmation required."),
					Colors.Green));
			}
			catch (Exception ex)
			{
				PolicySystemLog.Failure("Proposal", "proposal-notification-failed",
					"ruler=" + SafeHeroId(ruler) + " kingdom=" + SafeKingdomId(kingdom) + " error=" + ex.Message);
			}
			return true;
		}
		catch (Exception ex)
		{
			ReleaseReservation(scopeKey, requestKey);
			failureReason = "Ruler policy proposal failed to enter drafting queue; technical details logged.";
			PolicySystemLog.Failure("Proposal", "proposal-queue-exception", ex.Message, ex.ToString());
			LogFailure(ruler, chain, "proposal-queue-exception", failureReason);
			return false;
		}
	}

	private static string BuildBoundedHistoryContext(Hero ruler, string proposal, string reply, string chain)
	{
		try
		{
			int lineLimit = Math.Max(1, DuelSettings.GetDailyConversationHistoryLineLimitForExternal());
			bool includeActiveScene = !string.Equals(chain, "courier", StringComparison.OrdinalIgnoreCase);
			string history = MyBehavior.BuildHistoryContextForExternal(ruler, lineLimit, proposal, reply, includeActiveScene) ?? "";
			history = history.Trim();
			if (history.Length <= MaxHistoryChars)
			{
				return history;
			}
			return history.Substring(history.Length - MaxHistoryChars, MaxHistoryChars);
		}
		catch
		{
			return "";
		}
	}

	private static bool TryAcquireReservation(string scopeKey, string requestKey, out string failureReason)
	{
		failureReason = "";
		long now = DateTime.UtcNow.Ticks;
		long runtimeGeneration = SaveRuntimeGuard.CurrentGeneration;
		long expires = DateTime.UtcNow.AddMinutes(ReservationLifetimeMinutes).Ticks;
		lock (ReservationLock)
		{
			PruneReservations(now, runtimeGeneration);
			if (RecentRequests.ContainsKey(requestKey))
			{
				failureReason = "An identical policy proposal has just entered the drafting queue.";
				return false;
			}
			if (ActiveReservations.ContainsKey(scopeKey))
			{
				failureReason = "This ruler already has a pending player proposal being drafted.";
				return false;
			}
			ActiveReservations[scopeKey] = new ProposalReservation
			{
				RuntimeGeneration = runtimeGeneration,
				ExpiresUtcTicks = expires,
				RequestKey = requestKey
			};
			return true;
		}
	}

	private static void MarkRequestAccepted(string scopeKey, string requestKey)
	{
		long now = DateTime.UtcNow.Ticks;
		long runtimeGeneration = SaveRuntimeGuard.CurrentGeneration;
		long expires = DateTime.UtcNow.AddMinutes(ReservationLifetimeMinutes).Ticks;
		lock (ReservationLock)
		{
			PruneReservations(now, runtimeGeneration);
			if (ActiveReservations.TryGetValue(scopeKey, out ProposalReservation reservation)
				&& string.Equals(reservation?.RequestKey, requestKey, StringComparison.OrdinalIgnoreCase))
			{
				ActiveReservations.Remove(scopeKey);
			}
			RecentRequests[requestKey] = new ProposalReservation
			{
				RuntimeGeneration = runtimeGeneration,
				ExpiresUtcTicks = expires,
				RequestKey = requestKey
			};
		}
	}

	private static void ReleaseReservation(string scopeKey, string requestKey)
	{
		lock (ReservationLock)
		{
			if (ActiveReservations.TryGetValue(scopeKey, out ProposalReservation reservation)
				&& string.Equals(reservation?.RequestKey, requestKey, StringComparison.OrdinalIgnoreCase))
			{
				ActiveReservations.Remove(scopeKey);
			}
		}
	}

	private static void PruneReservations(long nowUtcTicks, long runtimeGeneration)
	{
		RemoveExpiredEntries(ActiveReservations, nowUtcTicks, runtimeGeneration);
		RemoveExpiredEntries(RecentRequests, nowUtcTicks, runtimeGeneration);
	}

	private static void RemoveExpiredEntries(Dictionary<string, ProposalReservation> entries, long nowUtcTicks, long runtimeGeneration)
	{
		if (entries == null || entries.Count == 0)
		{
			return;
		}
		List<string> expired = null;
		foreach (KeyValuePair<string, ProposalReservation> item in entries)
		{
			ProposalReservation reservation = item.Value;
			if (reservation == null || reservation.RuntimeGeneration != runtimeGeneration || reservation.ExpiresUtcTicks <= nowUtcTicks)
			{
				expired ??= new List<string>();
				expired.Add(item.Key);
			}
		}
		if (expired == null)
		{
			return;
		}
		foreach (string key in expired)
		{
			entries.Remove(key);
		}
	}

	private static string BuildScopeKey(Hero ruler, Kingdom kingdom)
	{
		return SafeKingdomId(kingdom) + ":" + SafeHeroId(ruler);
	}

	private static string NormalizeProposalForKey(string proposal)
	{
		return WhitespaceRegex.Replace((proposal ?? "").Trim(), " ").ToLowerInvariant();
	}

	private static string ComputeStableHash(string value)
	{
		unchecked
		{
			ulong hash = 14695981039346656037UL;
			foreach (char c in value ?? "")
			{
				hash ^= c;
				hash *= 1099511628211UL;
			}
			return hash.ToString("x16");
		}
	}

	private static string RemoveLiteralTag(string value)
	{
		string result = value ?? "";
		int index;
		while ((index = result.IndexOf(ActionTag, StringComparison.OrdinalIgnoreCase)) >= 0)
		{
			result = result.Remove(index, ActionTag.Length);
		}
		return result;
	}

	private static string NormalizeChainName(string chainName)
	{
		string chain = AnimusForgeTextInputSanitizer.SanitizeSingleLine(chainName ?? "", 32).Trim().ToLowerInvariant();
		return string.IsNullOrWhiteSpace(chain) ? "unknown" : chain;
	}

	private static string SafeHeroId(Hero hero)
	{
		return (hero?.StringId ?? "").Trim();
	}

	private static string SafeKingdomId(Kingdom kingdom)
	{
		return (kingdom?.StringId ?? "").Trim();
	}

	private static string RequestSuffix(string requestKey)
	{
		string value = requestKey ?? "";
		int index = value.LastIndexOf(':');
		return index >= 0 && index + 1 < value.Length ? value.Substring(index + 1) : value;
	}

	private static void LogFailure(Hero ruler, string chain, string stage, string failureReason)
	{
		PolicySystemLog.Failure("Proposal", stage,
			"chain=" + (chain ?? "unknown")
			+ " ruler=" + SafeHeroId(ruler)
			+ " kingdom=" + SafeKingdomId(ruler?.Clan?.Kingdom)
			+ " reason=" + (failureReason ?? ""));
	}
}
