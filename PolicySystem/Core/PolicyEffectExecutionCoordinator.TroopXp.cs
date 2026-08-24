using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

internal sealed partial class BannerlordPolicyEffectGameBridge
{
	private const int TroopXpPlanVersion = 1;
	private const int MaximumTroopXpPerTroop = 5000;
	private const int MaximumTroopXpPlanParties = 512;
	private const int MaximumTroopXpPlanStacks = 4096;
	private const string LordPartyKind = "l";
	private const string GarrisonPartyKind = "g";

	public bool TryPrepareClanTroopXp(
		string[] clanIds,
		int xpPerTroop,
		out JToken plan,
		out string error)
	{
		plan = CreateTroopXpPlan(xpPerTroop, Array.Empty<TroopXpPartyMutation>());
		error = string.Empty;
		if (xpPerTroop <= 0 || xpPerTroop > MaximumTroopXpPerTroop)
		{
			error = "troop XP per troop must be between 1 and "
				+ MaximumTroopXpPerTroop.ToString(CultureInfo.InvariantCulture);
			return false;
		}

		HashSet<string> seenPartyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<TroopXpPartyMutation> parties = new List<TroopXpPartyMutation>();
		int totalStackCount = 0;
		foreach (string clanId in NormalizeIds(clanIds))
		{
			if (!TryResolveClan(clanId, out Clan clan) || !IsEligibleTroopXpClan(clan))
			{
				continue;
			}

			IReadOnlyList<WarPartyComponent> warPartyComponents;
			IReadOnlyList<Town> fiefs;
			try
			{
				List<WarPartyComponent> warPartySnapshot = new List<WarPartyComponent>();
				if (clan.WarPartyComponents != null)
				{
					foreach (WarPartyComponent component in clan.WarPartyComponents)
					{
						warPartySnapshot.Add(component);
					}
				}
				warPartyComponents = warPartySnapshot;
				List<Town> fiefSnapshot = new List<Town>();
				if (clan.Fiefs != null)
				{
					foreach (Town fief in clan.Fiefs)
					{
						fiefSnapshot.Add(fief);
					}
				}
				fiefs = fiefSnapshot;
			}
			catch (Exception ex)
			{
				error = "troop XP Clan assets could not be enumerated for " + clanId + ": " + ex.Message;
				return false;
			}

			foreach (WarPartyComponent component in warPartyComponents)
			{
				if (component is not LordPartyComponent lordPartyComponent)
				{
					continue;
				}
				MobileParty party = lordPartyComponent.MobileParty;
				Hero leader = party?.LeaderHero;
				if (!IsEligibleLordTroopXpParty(clan, leader, party, lordPartyComponent))
				{
					continue;
				}

				if (!TryAddPreparedParty(
					clan,
					party,
					LordPartyKind,
					leader.StringId,
					xpPerTroop,
					seenPartyIds,
					parties,
					ref totalStackCount,
					out error))
				{
					return false;
				}
			}

			foreach (Town fief in fiefs)
			{
				Settlement settlement = fief?.Settlement;
				MobileParty garrison = fief?.GarrisonParty;
				if (!IsEligibleGarrisonTroopXpParty(clan, settlement, garrison))
				{
					continue;
				}

				if (!TryAddPreparedParty(
					clan,
					garrison,
					GarrisonPartyKind,
					settlement.StringId,
					xpPerTroop,
					seenPartyIds,
					parties,
					ref totalStackCount,
					out error))
				{
					return false;
				}
			}
		}

		parties.Sort((left, right) =>
		{
			int clanComparison = StringComparer.Ordinal.Compare(left.ClanId, right.ClanId);
			return clanComparison != 0
				? clanComparison
				: StringComparer.Ordinal.Compare(left.PartyId, right.PartyId);
		});
		plan = CreateTroopXpPlan(xpPerTroop, parties);
		return true;
	}

	public bool TryApplyClanTroopXp(
		JToken plan,
		string reason,
		out JToken journal,
		out int appliedPartyCount,
		out int appliedStackCount,
		out long totalAppliedXp,
		out string error)
	{
		journal = null;
		appliedPartyCount = 0;
		appliedStackCount = 0;
		totalAppliedXp = 0L;
		error = string.Empty;
		_ = reason;
		if (!TryReadTroopXpPlan(plan, out int xpPerTroop, out List<TroopXpPartyMutation> parties, out error))
		{
			return false;
		}

		List<TroopXpPreparedParty> prepared = new List<TroopXpPreparedParty>(parties.Count);
		foreach (TroopXpPartyMutation partyMutation in parties)
		{
			if (!TryResolvePreparedParty(partyMutation, out TroopXpPreparedParty preparedParty, out error))
			{
				return false;
			}
			foreach (TroopXpStackMutation stack in partyMutation.Stacks)
			{
				if (!preparedParty.RosterIndex.TryGetValue(stack.TroopId, out int index))
				{
					error = "troop XP stack disappeared before apply: " + partyMutation.PartyId + "/" + stack.TroopId;
					return false;
				}
				TroopRosterElement current = preparedParty.Roster.GetElementCopyAtIndex(index);
				if (!MatchesTroopXpStack(current, stack, stack.Before))
				{
					error = "troop XP stack changed before apply: " + partyMutation.PartyId + "/" + stack.TroopId;
					return false;
				}
			}
			prepared.Add(preparedParty);
		}

		List<TroopXpPartyMutation> applied = new List<TroopXpPartyMutation>();
		foreach (TroopXpPreparedParty preparedParty in prepared)
		{
			List<TroopXpStackMutation> appliedStacks = new List<TroopXpStackMutation>();
			foreach (TroopXpStackMutation stack in preparedParty.Mutation.Stacks)
			{
				if (!IsPreparedPartyStillValid(preparedParty))
				{
					return FinishFailedTroopXpApply(
						xpPerTroop,
						applied,
						preparedParty,
						appliedStacks,
						"troop XP party anchor changed during apply: " + preparedParty.Mutation.PartyId,
						out journal,
						out appliedPartyCount,
						out error);
				}

				int index = preparedParty.RosterIndex[stack.TroopId];
				TroopRosterElement immediateBefore;
				try
				{
					immediateBefore = preparedParty.Roster.GetElementCopyAtIndex(index);
				}
				catch (Exception ex)
				{
					return FinishFailedTroopXpApply(
						xpPerTroop,
						applied,
						preparedParty,
						appliedStacks,
						"troop XP roster changed during apply for " + preparedParty.Mutation.PartyId
							+ "/" + stack.TroopId + ": " + ex.Message,
						out journal,
						out appliedPartyCount,
						out error);
				}
				if (!MatchesTroopXpStack(immediateBefore, stack, stack.Before))
				{
					return FinishFailedTroopXpApply(
						xpPerTroop,
						applied,
						preparedParty,
						appliedStacks,
						"troop XP stack changed during apply: " + preparedParty.Mutation.PartyId + "/" + stack.TroopId,
						out journal,
						out appliedPartyCount,
						out error);
				}

				int requestedDelta = stack.After - stack.Before;
				Exception callbackException = null;
				try
				{
					preparedParty.Roster.AddXpToTroopAtIndex(index, requestedDelta);
				}
				catch (Exception ex)
				{
					callbackException = ex;
				}

				int actualAfter;
				try
				{
					actualAfter = preparedParty.Roster.GetElementCopyAtIndex(index).Xp;
				}
				catch (Exception ex)
				{
					return FinishFailedTroopXpApply(
						xpPerTroop,
						applied,
						preparedParty,
						appliedStacks,
						"troop XP post-read failed for " + preparedParty.Mutation.PartyId
							+ "/" + stack.TroopId + ": " + ex.Message,
						out journal,
						out appliedPartyCount,
						out error);
				}
				if (actualAfter != stack.Before)
				{
					TroopXpStackMutation observed = new TroopXpStackMutation(
						stack.TroopId,
						stack.Number,
						stack.Before,
						actualAfter);
					appliedStacks.Add(observed);
					appliedStackCount++;
					totalAppliedXp += (long)actualAfter - stack.Before;
				}

				if (actualAfter != stack.After)
				{
					return FinishFailedTroopXpApply(
						xpPerTroop,
						applied,
						preparedParty,
						appliedStacks,
						"troop XP apply was not exact for " + preparedParty.Mutation.PartyId + "/" + stack.TroopId
							+ (callbackException == null ? string.Empty : ": " + callbackException.Message),
						out journal,
						out appliedPartyCount,
						out error);
				}
			}
			if (appliedStacks.Count > 0)
			{
				AddAppliedParty(applied, preparedParty.Mutation, appliedStacks);
				UpdateRosterVersionBestEffort(preparedParty.Roster);
			}
		}

		journal = CreateTroopXpPlan(xpPerTroop, applied);
		appliedPartyCount = applied.Count;
		return true;
	}

	public bool TryRestoreClanTroopXp(
		JToken journal,
		string reason,
		out int restoredStackCount,
		out string error)
	{
		restoredStackCount = 0;
		error = string.Empty;
		_ = reason;
		if (!TryReadTroopXpPlan(journal, out _, out List<TroopXpPartyMutation> parties, out error))
		{
			return false;
		}

		List<TroopXpPreparedParty> prepared = new List<TroopXpPreparedParty>(parties.Count);
		foreach (TroopXpPartyMutation partyMutation in parties)
		{
			if (!TryResolvePreparedParty(partyMutation, out TroopXpPreparedParty preparedParty, out error))
			{
				error = "troop XP compensation party unavailable: " + partyMutation.PartyId
					+ (string.IsNullOrWhiteSpace(error) ? string.Empty : ": " + error);
				return false;
			}
			foreach (TroopXpStackMutation stack in partyMutation.Stacks)
			{
				if (!preparedParty.RosterIndex.TryGetValue(stack.TroopId, out int index))
				{
					error = "troop XP compensation stack missing: " + partyMutation.PartyId + "/" + stack.TroopId;
					return false;
				}
				TroopRosterElement currentElement = preparedParty.Roster.GetElementCopyAtIndex(index);
				if (!string.Equals(currentElement.Character?.StringId, stack.TroopId, StringComparison.OrdinalIgnoreCase)
					|| currentElement.Number != stack.Number
					|| (currentElement.Xp != stack.Before && currentElement.Xp != stack.After))
				{
					error = "troop XP compensation CAS mismatch for " + partyMutation.PartyId + "/" + stack.TroopId
						+ ": expectedXp=" + stack.After.ToString(CultureInfo.InvariantCulture)
						+ " actualXp=" + currentElement.Xp.ToString(CultureInfo.InvariantCulture)
						+ " expectedNumber=" + stack.Number.ToString(CultureInfo.InvariantCulture)
						+ " actualNumber=" + currentElement.Number.ToString(CultureInfo.InvariantCulture);
					return false;
				}
			}
			prepared.Add(preparedParty);
		}

		for (int partyIndex = prepared.Count - 1; partyIndex >= 0; partyIndex--)
		{
			TroopXpPreparedParty preparedParty = prepared[partyIndex];
			bool partyChanged = false;
			for (int stackIndex = preparedParty.Mutation.Stacks.Count - 1; stackIndex >= 0; stackIndex--)
			{
				TroopXpStackMutation stack = preparedParty.Mutation.Stacks[stackIndex];
				if (!IsPreparedPartyStillValid(preparedParty))
				{
					if (partyChanged)
					{
						UpdateRosterVersionBestEffort(preparedParty.Roster);
					}
					error = "troop XP compensation party anchor changed during restore: " + preparedParty.Mutation.PartyId;
					return false;
				}

				int index = preparedParty.RosterIndex[stack.TroopId];
				TroopRosterElement currentElement = preparedParty.Roster.GetElementCopyAtIndex(index);
				if (!string.Equals(currentElement.Character?.StringId, stack.TroopId, StringComparison.OrdinalIgnoreCase)
					|| currentElement.Number != stack.Number
					|| (currentElement.Xp != stack.Before && currentElement.Xp != stack.After))
				{
					if (partyChanged)
					{
						UpdateRosterVersionBestEffort(preparedParty.Roster);
					}
					error = "troop XP compensation CAS changed during restore: "
						+ preparedParty.Mutation.PartyId + "/" + stack.TroopId;
					return false;
				}
				if (currentElement.Xp == stack.Before)
				{
					continue;
				}
				try
				{
					preparedParty.Roster.SetElementXp(index, stack.Before);
				}
				catch
				{
					// SetElementXp writes before invoking the owner callback. The exact
					// post-read below determines whether compensation succeeded.
				}
				int restored = preparedParty.Roster.GetElementCopyAtIndex(index).Xp;
				if (restored != stack.Before)
				{
					if (partyChanged)
					{
						UpdateRosterVersionBestEffort(preparedParty.Roster);
					}
					error = "troop XP compensation did not restore " + preparedParty.Mutation.PartyId + "/" + stack.TroopId;
					return false;
				}
				restoredStackCount++;
				partyChanged = true;
			}
			if (partyChanged)
			{
				UpdateRosterVersionBestEffort(preparedParty.Roster);
			}
		}
		return true;
	}

	private static bool TryAddPreparedParty(
		Clan clan,
		MobileParty party,
		string partyKind,
		string anchorId,
		int xpPerTroop,
		ISet<string> seenPartyIds,
		ICollection<TroopXpPartyMutation> parties,
		ref int totalStackCount,
		out string error)
	{
		error = string.Empty;
		string partyId = (party?.StringId ?? string.Empty).Trim();
		if (partyId.Length == 0 || !seenPartyIds.Add(partyId))
		{
			return true;
		}

		TroopRoster roster = party.MemberRoster;
		List<TroopXpStackMutation> stacks = new List<TroopXpStackMutation>();
		for (int index = 0; roster != null && index < roster.Count; index++)
		{
			TroopRosterElement element = roster.GetElementCopyAtIndex(index);
			CharacterObject character = element.Character;
			if (!TryCalculateTroopXpAfter(
				party,
				character,
				element.Number,
				element.Xp,
				xpPerTroop,
				out int after))
			{
				continue;
			}

			string troopId = (character.StringId ?? string.Empty).Trim();
			if (troopId.Length == 0)
			{
				continue;
			}
			stacks.Add(new TroopXpStackMutation(troopId, element.Number, element.Xp, after));
			if (totalStackCount > MaximumTroopXpPlanStacks - stacks.Count)
			{
				error = "troop XP plan exceeds the soldier-stack limit";
				return false;
			}
		}

		if (stacks.Count == 0)
		{
			return true;
		}

		if (parties.Count >= MaximumTroopXpPlanParties)
		{
			error = "troop XP plan exceeds the party limit";
			return false;
		}
		stacks.Sort((left, right) => StringComparer.Ordinal.Compare(left.TroopId, right.TroopId));
		parties.Add(new TroopXpPartyMutation(
			(clan.StringId ?? string.Empty).Trim(),
			partyId,
			partyKind,
			(anchorId ?? string.Empty).Trim(),
			stacks));
		totalStackCount += stacks.Count;
		return true;
	}

	private static bool TryCalculateTroopXpAfter(
		MobileParty party,
		CharacterObject character,
		int number,
		int before,
		int xpPerTroop,
		out int after)
	{
		after = before;
		try
		{
			if (party?.Party == null
				|| character == null
				|| character.IsHero
				|| number <= 0
				|| before < 0
				|| character.UpgradeTargets == null
				|| character.UpgradeTargets.Length == 0)
			{
				return false;
			}

			int maximumCost = 0;
			for (int index = 0; index < character.UpgradeTargets.Length; index++)
			{
				if (character.UpgradeTargets[index] == null)
				{
					return false;
				}
				int cost = character.GetUpgradeXpCost(party.Party, index);
				if (cost > maximumCost)
				{
					maximumCost = cost;
				}
			}
			if (maximumCost <= 0)
			{
				return false;
			}

			long capacity = (long)number * maximumCost;
			long requested = (long)number * xpPerTroop;
			long remaining = Math.Max(0L, capacity - before);
			long intRemaining = int.MaxValue - (long)before;
			long delta = Math.Min(requested, Math.Min(remaining, intRemaining));
			if (delta <= 0L)
			{
				return false;
			}
			after = before + (int)delta;
			return after > before;
		}
		catch
		{
			after = before;
			return false;
		}
	}

	private static bool IsEligibleTroopXpClan(Clan clan)
	{
		return clan != null && !clan.IsEliminated;
	}

	private static bool IsEligibleTroopXpHero(Hero hero)
	{
		return hero != null
			&& hero.IsActive
			&& !hero.IsDead
			&& !hero.IsDisabled
			&& !hero.IsPrisoner;
	}

	private static bool IsEligibleLordTroopXpParty(
		Clan clan,
		Hero leader,
		MobileParty party,
		LordPartyComponent expectedComponent = null)
	{
		if (!IsEligibleTroopXpClan(clan)
			|| !IsEligibleTroopXpHero(leader)
			|| party == null
			|| !party.IsActive
			|| party.IsDisbanding
			|| party.Party == null
			|| !party.IsLordParty
			|| party.IsMilitia
			|| party.IsGarrison
			|| party.LordPartyComponent == null
			|| (expectedComponent != null && !ReferenceEquals(party.LordPartyComponent, expectedComponent))
			|| !ReferenceEquals(party.LeaderHero, leader)
			|| !ReferenceEquals(party.ActualClan, clan)
			|| !ReferenceEquals(leader.Clan, clan))
		{
			return false;
		}
		try
		{
			return ReferenceEquals(leader.PartyBelongedTo, party)
				&& !CourierDeliveryBehavior.IsCourierParty(party);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsEligibleGarrisonTroopXpParty(Clan clan, Settlement settlement, MobileParty party)
	{
		if (!IsEligibleTroopXpClan(clan)
			|| settlement == null
			|| (!settlement.IsTown && !settlement.IsCastle)
			|| !ReferenceEquals(settlement.OwnerClan, clan)
			|| settlement.Town == null
			|| party == null
			|| !ReferenceEquals(settlement.Town.GarrisonParty, party)
			|| !party.IsActive
			|| party.IsDisbanding
			|| party.Party == null
			|| !party.IsGarrison
			|| party.GarrisonPartyComponent == null
			|| !ReferenceEquals(party.ActualClan, clan))
		{
			return false;
		}
		try
		{
			return !CourierDeliveryBehavior.IsCourierParty(party);
		}
		catch
		{
			return false;
		}
	}

	private bool TryResolvePreparedParty(
		TroopXpPartyMutation mutation,
		out TroopXpPreparedParty prepared,
		out string error)
	{
		prepared = null;
		error = string.Empty;
		if (!TryResolveClan(mutation.ClanId, out Clan clan) || !IsEligibleTroopXpClan(clan))
		{
			error = "troop XP Clan is unavailable: " + mutation.ClanId;
			return false;
		}

		MobileParty party = ResolveMobileParty(mutation.PartyId);
		if (string.Equals(mutation.PartyKind, LordPartyKind, StringComparison.Ordinal))
		{
			Hero leader = party?.LeaderHero;
			if (!IsEligibleLordTroopXpParty(clan, leader, party)
				|| !string.Equals(leader?.StringId, mutation.AnchorId, StringComparison.OrdinalIgnoreCase))
			{
				error = "troop XP LordParty is no longer attached to the prepared Clan and leader: " + mutation.PartyId;
				return false;
			}
		}
		else if (string.Equals(mutation.PartyKind, GarrisonPartyKind, StringComparison.Ordinal))
		{
			Settlement settlement = ResolveSettlement(mutation.AnchorId);
			if (!IsEligibleGarrisonTroopXpParty(clan, settlement, party))
			{
				error = "troop XP garrison is no longer the current garrison of the prepared Clan fief: " + mutation.PartyId;
				return false;
			}
		}
		else
		{
			error = "troop XP party kind is invalid: " + mutation.PartyId;
			return false;
		}

		TroopRoster roster = party.MemberRoster;
		if (!TryBuildRosterIndex(roster, out Dictionary<string, int> rosterIndex, out error))
		{
			error = mutation.PartyId + ": " + error;
			return false;
		}
		prepared = new TroopXpPreparedParty(mutation, clan, party, roster, rosterIndex);
		return true;
	}

	private static bool IsPreparedPartyStillValid(TroopXpPreparedParty prepared)
	{
		if (prepared == null || prepared.Mutation == null)
		{
			return false;
		}
		if (string.Equals(prepared.Mutation.PartyKind, LordPartyKind, StringComparison.Ordinal))
		{
			Hero leader = prepared.Party?.LeaderHero;
			return IsEligibleLordTroopXpParty(prepared.Clan, leader, prepared.Party)
				&& string.Equals(leader?.StringId, prepared.Mutation.AnchorId, StringComparison.OrdinalIgnoreCase);
		}
		if (string.Equals(prepared.Mutation.PartyKind, GarrisonPartyKind, StringComparison.Ordinal))
		{
			Settlement settlement = ResolveSettlement(prepared.Mutation.AnchorId);
			return IsEligibleGarrisonTroopXpParty(prepared.Clan, settlement, prepared.Party);
		}
		return false;
	}

	private static MobileParty ResolveMobileParty(string partyId)
	{
		string normalizedId = (partyId ?? string.Empty).Trim();
		if (normalizedId.Length == 0)
		{
			return null;
		}
		try
		{
			return Campaign.Current?.CampaignObjectManager?.Find<MobileParty>(normalizedId);
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveSettlement(string settlementId)
	{
		string normalizedId = (settlementId ?? string.Empty).Trim();
		if (normalizedId.Length == 0)
		{
			return null;
		}
		try
		{
			return Campaign.Current?.CampaignObjectManager?.Find<Settlement>(normalizedId);
		}
		catch
		{
			return null;
		}
	}

	private static bool TryBuildRosterIndex(
		TroopRoster roster,
		out Dictionary<string, int> indexByTroopId,
		out string error)
	{
		indexByTroopId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		error = string.Empty;
		if (roster == null)
		{
			error = "member roster is unavailable";
			return false;
		}
		for (int index = 0; index < roster.Count; index++)
		{
			string troopId = (roster.GetElementCopyAtIndex(index).Character?.StringId ?? string.Empty).Trim();
			if (troopId.Length == 0)
			{
				continue;
			}
			if (indexByTroopId.ContainsKey(troopId))
			{
				error = "member roster contains duplicate troop id: " + troopId;
				return false;
			}
			indexByTroopId.Add(troopId, index);
		}
		return true;
	}

	private static bool MatchesTroopXpStack(TroopRosterElement element, TroopXpStackMutation stack, int expectedXp)
	{
		return string.Equals(element.Character?.StringId, stack.TroopId, StringComparison.OrdinalIgnoreCase)
			&& element.Number == stack.Number
			&& element.Xp == expectedXp;
	}

	private static IEnumerable<string> NormalizeIds(IEnumerable<string> values)
	{
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> normalized = new List<string>();
		foreach (string value in values ?? Array.Empty<string>())
		{
			string id = (value ?? string.Empty).Trim();
			if (id.Length > 0 && seen.Add(id))
			{
				normalized.Add(id);
			}
		}
		normalized.Sort(StringComparer.Ordinal);
		return normalized;
	}

	private static bool FinishFailedTroopXpApply(
		int xpPerTroop,
		ICollection<TroopXpPartyMutation> applied,
		TroopXpPreparedParty preparedParty,
		IReadOnlyList<TroopXpStackMutation> appliedStacks,
		string failure,
		out JToken journal,
		out int appliedPartyCount,
		out string error)
	{
		AddAppliedParty(applied, preparedParty.Mutation, appliedStacks);
		if (appliedStacks != null && appliedStacks.Count > 0)
		{
			UpdateRosterVersionBestEffort(preparedParty.Roster);
		}
		journal = CreateTroopXpPlan(xpPerTroop, applied);
		appliedPartyCount = applied.Count;
		error = failure;
		return false;
	}

	private static void AddAppliedParty(
		ICollection<TroopXpPartyMutation> applied,
		TroopXpPartyMutation source,
		IReadOnlyList<TroopXpStackMutation> stacks)
	{
		if (stacks == null || stacks.Count == 0)
		{
			return;
		}
		applied.Add(new TroopXpPartyMutation(
			source.ClanId,
			source.PartyId,
			source.PartyKind,
			source.AnchorId,
			new List<TroopXpStackMutation>(stacks)));
	}

	private static void UpdateRosterVersionBestEffort(TroopRoster roster)
	{
		try
		{
			roster?.UpdateVersion();
		}
		catch
		{
			// XP is already stored by TroopRosterElement. This call only invalidates
			// cached roster snapshots and cannot make an exact mutation inexact.
		}
	}

	private static JToken CreateTroopXpPlan(
		int xpPerTroop,
		IEnumerable<TroopXpPartyMutation> parties)
	{
		JArray partyTokens = new JArray();
		foreach (TroopXpPartyMutation party in parties ?? Array.Empty<TroopXpPartyMutation>())
		{
			JArray stackTokens = new JArray();
			foreach (TroopXpStackMutation stack in party.Stacks)
			{
				stackTokens.Add(new JObject
				{
					["i"] = stack.TroopId,
					["n"] = stack.Number,
					["b"] = stack.Before,
					["a"] = stack.After
				});
			}
			partyTokens.Add(new JObject
			{
				["c"] = party.ClanId,
				["i"] = party.PartyId,
				["k"] = party.PartyKind,
				["r"] = party.AnchorId,
				["s"] = stackTokens
			});
		}
		return new JObject
		{
			["v"] = TroopXpPlanVersion,
			["x"] = xpPerTroop,
			["p"] = partyTokens
		};
	}

	private static bool TryReadTroopXpPlan(
		JToken token,
		out int xpPerTroop,
		out List<TroopXpPartyMutation> parties,
		out string error)
	{
		xpPerTroop = 0;
		parties = new List<TroopXpPartyMutation>();
		error = string.Empty;
		if (token is not JObject root
			|| !TryReadInt(root["v"], out int version)
			|| version != TroopXpPlanVersion
			|| !TryReadInt(root["x"], out xpPerTroop)
			|| xpPerTroop <= 0
			|| xpPerTroop > MaximumTroopXpPerTroop
			|| root["p"] is not JArray partyTokens
			|| partyTokens.Count > MaximumTroopXpPlanParties)
		{
			error = "troop XP plan envelope is invalid";
			return false;
		}

		HashSet<string> seenPartyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int totalStacks = 0;
		foreach (JToken partyToken in partyTokens)
		{
			if (partyToken is not JObject partyObject || partyObject["s"] is not JArray stackTokens)
			{
				error = "troop XP party plan is invalid";
				return false;
			}
			string clanId = ((string)partyObject["c"] ?? string.Empty).Trim();
			string partyId = ((string)partyObject["i"] ?? string.Empty).Trim();
			string partyKind = ((string)partyObject["k"] ?? string.Empty).Trim();
			string anchorId = ((string)partyObject["r"] ?? string.Empty).Trim();
			if (clanId.Length == 0
				|| partyId.Length == 0
				|| (partyKind != LordPartyKind && partyKind != GarrisonPartyKind)
				|| anchorId.Length == 0
				|| !seenPartyIds.Add(partyId))
			{
				error = "troop XP party identity is missing, invalid, or duplicated";
				return false;
			}

			List<TroopXpStackMutation> stacks = new List<TroopXpStackMutation>(stackTokens.Count);
			HashSet<string> seenTroopIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (JToken stackToken in stackTokens)
			{
				totalStacks++;
				if (totalStacks > MaximumTroopXpPlanStacks || stackToken is not JObject stackObject)
				{
					error = "troop XP plan contains too many or invalid stacks";
					return false;
				}
				string troopId = ((string)stackObject["i"] ?? string.Empty).Trim();
				if (troopId.Length == 0
					|| !seenTroopIds.Add(troopId)
					|| !TryReadInt(stackObject["n"], out int number)
					|| number <= 0
					|| !TryReadInt(stackObject["b"], out int before)
					|| before < 0
					|| !TryReadInt(stackObject["a"], out int after)
					|| after <= before)
				{
					error = "troop XP stack plan is invalid for " + partyId;
					return false;
				}
				stacks.Add(new TroopXpStackMutation(troopId, number, before, after));
			}
			if (stacks.Count == 0)
			{
				error = "troop XP party plan contains no changed stack: " + partyId;
				return false;
			}
			parties.Add(new TroopXpPartyMutation(clanId, partyId, partyKind, anchorId, stacks));
		}
		return true;
	}

	private static bool TryReadInt(JToken token, out int value)
	{
		value = 0;
		if (token?.Type != JTokenType.Integer)
		{
			return false;
		}
		try
		{
			long parsed = token.Value<long>();
			if (parsed < int.MinValue || parsed > int.MaxValue)
			{
				return false;
			}
			value = (int)parsed;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private sealed class TroopXpPreparedParty
	{
		internal TroopXpPreparedParty(
			TroopXpPartyMutation mutation,
			Clan clan,
			MobileParty party,
			TroopRoster roster,
			Dictionary<string, int> rosterIndex)
		{
			Mutation = mutation;
			Clan = clan;
			Party = party;
			Roster = roster;
			RosterIndex = rosterIndex;
		}

		internal TroopXpPartyMutation Mutation { get; }
		internal Clan Clan { get; }
		internal MobileParty Party { get; }
		internal TroopRoster Roster { get; }
		internal Dictionary<string, int> RosterIndex { get; }
	}

	private sealed class TroopXpPartyMutation
	{
		internal TroopXpPartyMutation(
			string clanId,
			string partyId,
			string partyKind,
			string anchorId,
			IReadOnlyList<TroopXpStackMutation> stacks)
		{
			ClanId = clanId;
			PartyId = partyId;
			PartyKind = partyKind;
			AnchorId = anchorId;
			Stacks = stacks ?? Array.Empty<TroopXpStackMutation>();
		}

		internal string ClanId { get; }
		internal string PartyId { get; }
		internal string PartyKind { get; }
		internal string AnchorId { get; }
		internal IReadOnlyList<TroopXpStackMutation> Stacks { get; }
	}

	private sealed class TroopXpStackMutation
	{
		internal TroopXpStackMutation(string troopId, int number, int before, int after)
		{
			TroopId = troopId;
			Number = number;
			Before = before;
			After = after;
		}

		internal string TroopId { get; }
		internal int Number { get; }
		internal int Before { get; }
		internal int After { get; }
	}
}
