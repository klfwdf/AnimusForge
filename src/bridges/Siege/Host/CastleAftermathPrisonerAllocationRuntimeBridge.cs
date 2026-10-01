using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Thin Bannerlord roster selector. Count semantics live in the standalone GCCZ profile;
/// this adapter resolves concrete troop types and preserves wounded/xp proportions.
/// </summary>
internal static class CastleAftermathPrisonerAllocationRuntimeBridge
{
	internal static CastleAftermathPrisonerAllocationResult Select(
		TroopRoster availableRoster,
		string playerText)
	{
		int available = CountRegular(availableRoster);
		SiegeCastlePrisonerQuantityDecision quantity = SiegeCastlePrisonerAllocationProfile.Resolve(
			playerText,
			available,
			MBRandom.RandomFloat);
		if (available <= 0 || quantity.RequestedCount <= 0)
		{
			return new CastleAftermathPrisonerAllocationResult(
				TroopRoster.CreateDummyTroopRoster(),
				quantity,
				"none");
		}

		List<TroopRosterElement> candidates = availableRoster.GetTroopRoster()
			.Where(element => element.Character != null && !element.Character.IsHero && element.Number > 0)
			.ToList();
		string normalizedPlayerText = playerText ?? string.Empty;
		List<TroopRosterElement> named = candidates
			.Where(element => MentionsTroop(normalizedPlayerText, element.Character))
			.ToList();
		string selectionMode;
		if (named.Count > 0)
		{
			candidates = named;
			selectionMode = "named_troop";
		}
		else if (SiegeCastlePrisonerAllocationProfile.RequestsEliteTroops(normalizedPlayerText))
		{
			candidates = candidates.OrderByDescending(element => element.Character.Tier).ThenBy(_ => MBRandom.RandomInt(int.MaxValue)).ToList();
			selectionMode = "elite_first";
		}
		else if (SiegeCastlePrisonerAllocationProfile.RequestsLowTierTroops(normalizedPlayerText))
		{
			candidates = candidates.OrderBy(element => element.Character.Tier).ThenBy(_ => MBRandom.RandomInt(int.MaxValue)).ToList();
			selectionMode = "low_tier_first";
		}
		else
		{
			Shuffle(candidates);
			selectionMode = "random_troop";
		}

		TroopRoster selected = TroopRoster.CreateDummyTroopRoster();
		int remaining = quantity.RequestedCount;
		foreach (TroopRosterElement element in candidates)
		{
			if (remaining <= 0)
			{
				break;
			}
			int count = Math.Min(remaining, element.Number);
			if (count <= 0)
			{
				continue;
			}
			int wounded = SiegeCastlePrisonerDispositionProfile.ResolveTransferredWounded(
				element.Number,
				element.WoundedNumber,
				count);
			int xp = SiegeCastlePrisonerDispositionProfile.ResolveTransferredXp(
				element.Number,
				element.Xp,
				count);
			selected.AddToCounts(element.Character, count, false, wounded, xp, true, -1);
			remaining -= count;
		}

		return new CastleAftermathPrisonerAllocationResult(selected, quantity, selectionMode);
	}

	internal static string DescribeRoster(TroopRoster roster)
	{
		if (roster == null || roster.TotalManCount <= 0)
		{
			return "无";
		}
		return string.Join("、", roster.GetTroopRoster()
			.Where(element => element.Character != null && !element.Character.IsHero && element.Number > 0)
			.Select(element => (element.Character.Name?.ToString() ?? element.Character.StringId ?? "未知兵种")
				+ "×" + element.Number));
	}

	private static bool MentionsTroop(string playerText, CharacterObject character)
	{
		if (character == null || string.IsNullOrWhiteSpace(playerText))
		{
			return false;
		}
		string name = character.Name?.ToString();
		return (!string.IsNullOrWhiteSpace(name)
				&& playerText.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
			|| (!string.IsNullOrWhiteSpace(character.StringId)
				&& playerText.IndexOf(character.StringId, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static void Shuffle<T>(IList<T> values)
	{
		for (int i = values.Count - 1; i > 0; i--)
		{
			int j = MBRandom.RandomInt(i + 1);
			(values[i], values[j]) = (values[j], values[i]);
		}
	}

	private static int CountRegular(TroopRoster roster)
		=> roster?.GetTroopRoster()
			.Where(element => element.Character != null && !element.Character.IsHero && element.Number > 0)
			.Sum(element => element.Number) ?? 0;
}

internal sealed class CastleAftermathPrisonerAllocationResult
{
	internal CastleAftermathPrisonerAllocationResult(
		TroopRoster roster,
		SiegeCastlePrisonerQuantityDecision quantity,
		string selectionMode)
	{
		Roster = roster ?? TroopRoster.CreateDummyTroopRoster();
		Quantity = quantity;
		SelectionMode = selectionMode ?? string.Empty;
	}

	internal TroopRoster Roster { get; }

	internal SiegeCastlePrisonerQuantityDecision Quantity { get; }

	internal string SelectionMode { get; }

	internal int Count => Roster.TotalManCount;
}
