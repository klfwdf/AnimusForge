using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using Newtonsoft.Json;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
	private CourierSession GetSessionById(string sessionId)
	{
		if (string.IsNullOrWhiteSpace(sessionId))
		{
			return null;
		}
		lock (_sessionLock)
		{
			_sessions.TryGetValue(sessionId, out var session);
			return session;
		}
	}

	public static bool IsCourierParty(MobileParty party)
	{
		try
		{
			if (party == null)
			{
				return false;
			}
			string partyId = NormalizeCourierPartyIdForLookup(party.StringId);
			if (partyId.Length == 0)
			{
				return false;
			}
			if (IsCourierPartyIdNormalized(partyId))
			{
				return true;
			}
			CourierDeliveryBehavior instance = Instance;
			if (instance == null)
			{
				return false;
			}
			HashSet<string> snapshot = instance._activeCourierPartyIdsSnapshot;
			if (snapshot != null && snapshot.Count == 0 && instance._courierRuntimeIndexesReady)
			{
				return false;
			}
			if (snapshot != null && snapshot.Contains(partyId))
			{
				return true;
			}
			// The immutable snapshot is maintained whenever a courier is created, removed,
			// or restored from a save. Do not lock and scan every session for ordinary parties.
			if (instance._courierRuntimeIndexesReady)
			{
				return false;
			}
			lock (instance._sessionLock)
			{
				return instance._sessions.Values.Any(x => x != null && string.Equals((x.CourierPartyId ?? "").Trim(), partyId, StringComparison.OrdinalIgnoreCase) && !IsTerminalStage(x));
			}
		}
		catch
		{
			return false;
		}
	}

	public static bool IsNpcIssuedCourierParty(MobileParty party)
	{
		try
		{
			if (party == null || !IsCourierParty(party))
			{
				return false;
			}
			CourierSession session = TryFindCourierSessionByPartyId(party.StringId);
			return session != null && IsInboundToPlayer(session);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsCourierPartyId(string partyId)
	{
		return IsCourierPartyIdNormalized(NormalizeCourierPartyIdForLookup(partyId));
	}

	public static bool HasPotentialActiveCourierPartiesForAi()
	{
		return Volatile.Read(ref _hasPotentialActiveCourierPartiesForAi) != 0;
	}

	public static bool HasCourierPartyIdPrefix(MobileParty party)
	{
		return IsCourierPartyId(party?.StringId);
	}

	private static bool IsCourierPartyIdNormalized(string partyId)
	{
		return !string.IsNullOrEmpty(partyId)
			&& partyId.StartsWith(CourierPartyPrefix, StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizeCourierPartyIdForLookup(string partyId)
	{
		if (string.IsNullOrEmpty(partyId))
		{
			return "";
		}
		int lastIndex = partyId.Length - 1;
		return char.IsWhiteSpace(partyId[0]) || char.IsWhiteSpace(partyId[lastIndex])
			? partyId.Trim()
			: partyId;
	}

	private static void UpdateAiCourierPresenceFlag(HashSet<string> snapshot, bool indexesReady)
	{
		Volatile.Write(ref _hasPotentialActiveCourierPartiesForAi, !indexesReady || (snapshot?.Count ?? 0) > 0 ? 1 : 0);
	}

	public static bool HasActiveCourierForHeroForExternal(Hero hero)
	{
		try
		{
			return Instance != null && Instance.HasActiveCourierForHero(hero);
		}
		catch
		{
			return false;
		}
	}

	private void OnGameLoadFinished()
	{
		try
		{
			lock (_sessionLock)
			{
				foreach (CourierSession session in _sessions.Values)
				{
					NormalizeSession(session);
					ResetReplyGenerationAfterLoad(session, "game_load_finished");
					MobileParty courier = ResolveCourierParty(session);
					MarkExistingCourierTemporaryShips(session, courier);
					ApplyCourierAiOverrides(courier, "load_restore");
				}
			}
			RebuildCourierRuntimeIndexes();
			DiscoverCourierLetterInventoryRecordsFromGeneratedRewardManifest("game_load_manifest_merge");
			PrimeCourierLetterInventoryRecords("game_load_finished");
			RestoreCourierLetterInventoryItems("game_load_finished");
			ScheduleCourierLetterInventoryRestoreRetries("game_load_finished", 8);
			Log("game_load_finished active=" + GetActiveSessionCount());
		}
		catch (Exception ex)
		{
			Log("game_load_finished failed: " + ex);
		}
	}

	private void RebuildCourierRuntimeIndexes()
	{
		try
		{
			HashSet<string> partyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			lock (_sessionLock)
			{
				foreach (CourierSession session in _sessions.Values)
				{
					string partyId = (session?.CourierPartyId ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(partyId) && !IsTerminalStage(session))
					{
						partyIds.Add(partyId);
					}
				}
				foreach (string cachedId in _courierPartyCache.Keys.ToList())
				{
					if (!partyIds.Contains(cachedId))
					{
						_courierPartyCache.Remove(cachedId);
					}
				}
			}
			_activeCourierPartyIdsSnapshot = partyIds;
			_courierRuntimeIndexesReady = true;
			UpdateAiCourierPresenceFlag(partyIds, _courierRuntimeIndexesReady);
		}
		catch (Exception ex)
		{
			Log("runtime index rebuild failed: " + ex.Message);
		}
	}

	private void AddCourierRuntimeIndex(CourierSession session, MobileParty courier = null)
	{
		try
		{
			string partyId = (session?.CourierPartyId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(partyId) || IsTerminalStage(session))
			{
				return;
			}
			HashSet<string> partyIds = new HashSet<string>(_activeCourierPartyIdsSnapshot ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase)
			{
				partyId
			};
			_activeCourierPartyIdsSnapshot = partyIds;
			_courierRuntimeIndexesReady = true;
			UpdateAiCourierPresenceFlag(partyIds, _courierRuntimeIndexesReady);
			if (courier != null)
			{
				lock (_sessionLock)
				{
					_courierPartyCache[partyId] = courier;
				}
			}
		}
		catch
		{
		}
	}

	private void RemoveCourierRuntimeIndex(CourierSession session)
	{
		try
		{
			string partyId = (session?.CourierPartyId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(partyId))
			{
				return;
			}
			HashSet<string> partyIds = new HashSet<string>(_activeCourierPartyIdsSnapshot ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
			partyIds.Remove(partyId);
			_activeCourierPartyIdsSnapshot = partyIds;
			_courierRuntimeIndexesReady = true;
			UpdateAiCourierPresenceFlag(partyIds, _courierRuntimeIndexesReady);
			lock (_sessionLock)
			{
				_courierPartyCache.Remove(partyId);
			}
		}
		catch
		{
		}
	}

	private bool HasActiveCourierForHero(Hero hero)
	{
		string heroId = SafeHeroId(hero);
		if (string.IsNullOrWhiteSpace(heroId))
		{
			return false;
		}
		lock (_sessionLock)
		{
			return _sessions.Values.Any(x => x != null && string.Equals(x.RecipientHeroId ?? "", heroId, StringComparison.OrdinalIgnoreCase) && !IsTerminalStage(x));
		}
	}

	private MobileParty ResolveCourierParty(CourierSession session)
	{
		string id = (session?.CourierPartyId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			lock (_sessionLock)
			{
				if (_courierPartyCache.TryGetValue(id, out var cached) && cached != null && cached.IsActive && string.Equals((cached.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase))
				{
					return cached;
				}
			}
			MobileParty resolved = MobileParty.All?.FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
			lock (_sessionLock)
			{
				if (resolved != null)
				{
					_courierPartyCache[id] = resolved;
				}
				else
				{
					_courierPartyCache.Remove(id);
				}
			}
			return resolved;
		}
		catch
		{
			return null;
		}
	}

	private Hero ResolveRecipient(CourierSession session)
	{
		return ResolveHeroByIdForCourier(session?.RecipientHeroId);
	}

	private Hero ResolveSender(CourierSession session)
	{
		return ResolveHeroByIdForCourier(session?.SenderHeroId);
	}

	private static Hero ResolveHeroByIdForCourier(string heroId)
	{
		string id = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			return Hero.Find(id) ?? Hero.FindFirst(x => x != null && string.Equals((x.StringId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private bool HasAnyActiveNpcInitiatedInboundCourier()
	{
		lock (_sessionLock)
		{
			return _sessions.Values.Any(x => x != null
				&& IsInboundToPlayer(x)
				&& x.IsNpcInitiated
				&& !IsTerminalStage(x));
		}
	}

	private bool IsInboundNeedTypeReserved(string needType)
	{
		string normalized = (needType ?? "").Trim();
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return false;
		}
		lock (_sessionLock)
		{
			return _sessions.Values.Any(x => x != null
				&& IsInboundToPlayer(x)
				&& x.IsNpcInitiated
				&& !IsTerminalStage(x)
				&& string.Equals((x.InboundNeedType ?? "").Trim(), normalized, StringComparison.OrdinalIgnoreCase));
		}
	}

	public static bool IsInboundNeedTypeReservedForExternal(string needType)
	{
		try
		{
			return Instance?.IsInboundNeedTypeReserved(needType) == true;
		}
		catch
		{
			return false;
		}
	}

	private static CourierSession TryFindCourierSessionByPartyId(string partyId, bool includeTerminal = false)
	{
		try
		{
			CourierDeliveryBehavior instance = Instance;
			string id = (partyId ?? "").Trim();
			if (instance == null || string.IsNullOrWhiteSpace(id))
			{
				return null;
			}
			lock (instance._sessionLock)
			{
				return instance._sessions.Values.FirstOrDefault(x => x != null
					&& string.Equals((x.CourierPartyId ?? "").Trim(), id, StringComparison.OrdinalIgnoreCase)
					&& (includeTerminal || !IsTerminalStage(x)));
			}
		}
		catch
		{
			return null;
		}
	}

	private static CourierStage ParseStage(string value)
	{
		if (Enum.TryParse(value ?? "", true, out CourierStage stage))
		{
			return stage;
		}
		return CourierStage.Outbound;
	}

	private static CourierPayloadMode ParsePayloadMode(string value)
	{
		if (Enum.TryParse(value ?? "", true, out CourierPayloadMode mode))
		{
			return mode;
		}
		return CourierPayloadMode.Normal;
	}

	private static bool IsTerminalStage(CourierSession session)
	{
		CourierStage stage = ParseStage(session?.Stage);
		return stage == CourierStage.Completed || stage == CourierStage.Destroyed;
	}

	private static void NormalizeSession(CourierSession session)
	{
		if (session == null)
		{
			return;
		}
		session.Id = (session.Id ?? "").Trim();
		session.Direction = NormalizeCourierDirection(session.Direction);
		session.SenderHeroId = (session.SenderHeroId ?? "").Trim();
		session.SenderName = session.SenderName ?? "";
		session.RecipientHeroId = (session.RecipientHeroId ?? "").Trim();
		session.RecipientName = session.RecipientName ?? "";
		session.CourierPartyId = (session.CourierPartyId ?? "").Trim();
		session.Stage = ParseStage(session.Stage).ToString();
		session.PayloadMode = ParsePayloadMode(session.PayloadMode).ToString();
		session.InboundLetterKind = string.IsNullOrWhiteSpace(session.InboundLetterKind)
			? (IsInboundToPlayer(session) ? InboundLetterKindDiplomacy : "")
			: session.InboundLetterKind.Trim();
		session.InboundMotiveType = string.IsNullOrWhiteSpace(session.InboundMotiveType)
			? (IsInboundToPlayer(session) ? LetterMotiveDiplomacy : "")
			: session.InboundMotiveType.Trim();
		session.InboundNeedType = (session.InboundNeedType ?? "").Trim();
		session.InboundIntentFact = session.InboundIntentFact ?? "";
		session.InboundIntentText = session.InboundIntentText ?? "";
		session.InboundFallbackLetter = session.InboundFallbackLetter ?? session.LetterText ?? "";
		session.InboundEventKey = (session.InboundEventKey ?? "").Trim();
		session.InboundCompletionReceipt = (session.InboundCompletionReceipt ?? "").Trim();
		if (IsInboundToPlayer(session))
		{
			session.IsNpcInitiated = true;
		}
		session.LastRouteKey = session.LastRouteKey ?? "";
		session.TemporaryShipHullId = (session.TemporaryShipHullId ?? "").Trim();
		session.LastProgressRouteKey = session.LastProgressRouteKey ?? "";
		session.Entries = session.Entries ?? new List<CourierCargoEntry>();
		session.CrewEntries = session.CrewEntries ?? new List<CourierCargoEntry>();
		foreach (CourierCargoEntry entry in session.Entries.Concat(session.CrewEntries).Where((CourierCargoEntry x) => x != null))
		{
			entry.SourceSettlementId = (entry.SourceSettlementId ?? "").Trim();
		}
	}

	private static string NormalizeCourierDirection(string direction)
	{
		string value = (direction ?? "").Trim();
		if (string.Equals(value, CourierDirectionInboundToPlayer, StringComparison.OrdinalIgnoreCase))
		{
			return CourierDirectionInboundToPlayer;
		}
		return CourierDirectionOutbound;
	}

	private static bool IsInboundToPlayer(CourierSession session)
	{
		return string.Equals(NormalizeCourierDirection(session?.Direction), CourierDirectionInboundToPlayer, StringComparison.OrdinalIgnoreCase);
	}

	private static void ResetReplyGenerationAfterLoad(CourierSession session, string reason)
	{
		if (session == null)
		{
			return;
		}
		session.ReplyWaitPopupShown = false;
		if (IsTerminalStage(session))
		{
			session.ReplyGenerationStarted = false;
			return;
		}
		if (IsInboundToPlayer(session)
			&& !string.IsNullOrWhiteSpace(session.InboundCompletionReceipt))
		{
			// A persisted receipt is either waiting for durable memory completion,
			// ready to apply, or quarantined. All three must block a second LLM run.
			session.ReplyGenerationStarted = !session.ReplyGenerated;
			return;
		}
		if (session.ReplyGenerated)
		{
			session.ReplyGenerationStarted = false;
			return;
		}
		CourierStage stage = ParseStage(session.Stage);
		if (!session.ReplyGenerationStarted && stage != CourierStage.GeneratingReply)
		{
			return;
		}
		if (session.ReplyGenerationStarted)
		{
			Log("reply generation restart armed session=" + session.Id + " reason=" + (reason ?? ""));
		}
		session.ReplyGenerationStarted = false;
	}

	private static List<CourierCargoEntry> CloneEntries(List<CourierCargoEntry> entries)
	{
		return (entries ?? new List<CourierCargoEntry>()).Where(x => x != null).Select(x => new CourierCargoEntry
		{
			Kind = x.Kind,
			Id = x.Id,
			Name = x.Name,
			Amount = x.Amount,
			GuidePriceDenars = x.GuidePriceDenars,
			IsHero = x.IsHero,
			SourceSettlementId = x.SourceSettlementId,
			Delivered = x.Delivered
		}).ToList();
	}

	private int GetActiveSessionCount()
	{
		lock (_sessionLock)
		{
			return _sessions.Values.Count(x => x != null && !IsTerminalStage(x));
		}
	}

	private static string NewSessionId()
	{
		return CourierPartyPrefix + DateTime.UtcNow.Ticks + "_" + MBRandom.RandomInt(1000000);
	}

	private static string SafeHeroId(Hero hero)
	{
		return (hero?.StringId ?? "").Trim();
	}
}
