using System;
using System.Collections.Generic;

namespace AnimusForge;

public sealed partial class ProactiveNpcRequestBehavior
{
	// The saved DTO keeps its original identity; all live session transitions have one owner.
	private sealed class ProactiveRequestSessionOwner
	{
		private long _nextEncounterProbeUtcTicks;

		internal ProactiveNpcRequestSession Current { get; private set; }

		internal void Import(ProactiveNpcRequestSession session)
		{
			Current = session;
			_nextEncounterProbeUtcTicks = 0L;
			if (session == null)
			{
				return;
			}
			if (string.IsNullOrWhiteSpace(session.Id))
			{
				session.Id = Guid.NewGuid().ToString("N");
			}
			List<string> normalized = NormalizeSingleNeedType(session.NeedTypes,
				string.IsNullOrWhiteSpace(session.NeedType) ? NeedFoodShortage : session.NeedType);
			session.NeedTypes = normalized;
			session.NeedType = normalized.Count > 0 ? normalized[0] : NeedFoodShortage;
		}

		internal bool TryStart(ProactiveNpcRequestSession session)
		{
			if (Current != null || session == null)
			{
				return false;
			}
			Current = session;
			_nextEncounterProbeUtcTicks = 0L;
			return true;
		}

		internal ProactiveNpcRequestSession Clear()
		{
			ProactiveNpcRequestSession prior = Current;
			Current = null;
			_nextEncounterProbeUtcTicks = 0L;
			return prior;
		}

		internal bool IsChasing => Current != null && string.Equals(Current.Stage, "Chasing", StringComparison.OrdinalIgnoreCase);
		internal bool IsExpired(float nowHours) => Current != null && nowHours > Current.ExpiresAtHours;
		internal bool MatchesHeroId(string heroId) => Current != null
			&& string.Equals(heroId, Current.HeroId, StringComparison.OrdinalIgnoreCase);
		internal bool MatchesPartyId(string partyId) => Current != null && !string.IsNullOrWhiteSpace(partyId)
			&& string.Equals(partyId, Current.PartyId, StringComparison.OrdinalIgnoreCase);

		internal bool TryReserveEncounterProbe(long nowUtcTicks)
		{
			if (!IsChasing || nowUtcTicks < _nextEncounterProbeUtcTicks)
			{
				return false;
			}
			_nextEncounterProbeUtcTicks = DateTime.UtcNow.AddSeconds(ActiveEncounterProbeSeconds).Ticks;
			return true;
		}

		internal void MarkOpeningMenu(float nowHours)
		{
			if (Current == null) return;
			Current.Stage = "OpeningMenu";
			Current.EncounterOpenedAtHours = nowHours;
		}

		internal void ReturnToChasing()
		{
			if (Current != null) Current.Stage = "Chasing";
		}

		internal void MarkEncounterOpened(float nowHours)
		{
			if (Current == null) return;
			Current.Stage = "Menu";
			Current.EncounterOpenedAtHours = nowHours;
		}

		internal void MarkConversationOpening(bool nativeConversation)
		{
			if (Current != null)
			{
				Current.Stage = nativeConversation ? "NativeConversationPending" : "SceneConversationPending";
			}
		}

		internal bool ShouldRecordFatigue => Current != null && !Current.NeedTypeFatigueRecorded;
		internal void MarkFatigueRecorded()
		{
			if (Current != null) Current.NeedTypeFatigueRecorded = true;
		}

		internal static bool ShouldCancelForBusyReason(string reason)
		{
			if (string.IsNullOrWhiteSpace(reason)) return false;
			return reason.IndexOf("siege", StringComparison.OrdinalIgnoreCase) >= 0
				|| reason.IndexOf("besieg", StringComparison.OrdinalIgnoreCase) >= 0
				|| reason.IndexOf("raid", StringComparison.OrdinalIgnoreCase) >= 0
				|| reason.IndexOf("native_activity", StringComparison.OrdinalIgnoreCase) >= 0
				|| reason.IndexOf("map_event", StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}
