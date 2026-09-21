#nullable disable

using System;
using System.Collections.Generic;

namespace AnimusForge;

/// <summary>
/// The two proposal domains that can acquire a directed, failed-round cooldown.
/// Peace is deliberately excluded because its availability is controlled by war state.
/// </summary>
public enum WorldDiplomacyOfferDomain
{
	None = 0,
	Trade = 1,
	Alliance = 2
}

public enum WorldDiplomacyOfferCooldownAction
{
	ClearCooldown = 0,
	StartCooldown = 1
}

/// <summary>
/// Minimal immutable input used when a closed round is settled. Instances are cheap
/// value types so the campaign behavior can build a short array/list once per round.
/// </summary>
public readonly struct WorldDiplomacyOfferRoundObservation
{
	public WorldDiplomacyOfferRoundObservation(
		string proposerKingdomId,
		string targetKingdomId,
		string proposalIntent,
		string finalStatus)
	{
		ProposerKingdomId = proposerKingdomId ?? "";
		TargetKingdomId = targetKingdomId ?? "";
		ProposalIntent = proposalIntent ?? "";
		FinalStatus = finalStatus ?? "";
	}

	public string ProposerKingdomId { get; }
	public string TargetKingdomId { get; }
	public string ProposalIntent { get; }
	public string FinalStatus { get; }
}

/// <summary>
/// Stable directed key: proposer A to target B is intentionally different from B to A.
/// Equality is case-insensitive because Bannerlord StringIds are treated that way by the
/// diplomacy storage layer.
/// </summary>
public readonly struct WorldDiplomacyOfferCooldownKey : IEquatable<WorldDiplomacyOfferCooldownKey>
{
	private static readonly StringComparer IdComparer = StringComparer.OrdinalIgnoreCase;

	public WorldDiplomacyOfferCooldownKey(
		string proposerKingdomId,
		string targetKingdomId,
		WorldDiplomacyOfferDomain domain)
	{
		ProposerKingdomId = NormalizeId(proposerKingdomId);
		TargetKingdomId = NormalizeId(targetKingdomId);
		Domain = domain;
	}

	public string ProposerKingdomId { get; }
	public string TargetKingdomId { get; }
	public WorldDiplomacyOfferDomain Domain { get; }

	public bool IsValid => Domain != WorldDiplomacyOfferDomain.None
		&& ProposerKingdomId.Length > 0
		&& TargetKingdomId.Length > 0
		&& !IdComparer.Equals(ProposerKingdomId, TargetKingdomId);

	public bool Equals(WorldDiplomacyOfferCooldownKey other)
	{
		return Domain == other.Domain
			&& IdComparer.Equals(ProposerKingdomId, other.ProposerKingdomId)
			&& IdComparer.Equals(TargetKingdomId, other.TargetKingdomId);
	}

	public override bool Equals(object obj)
	{
		return obj is WorldDiplomacyOfferCooldownKey other && Equals(other);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			int hash = IdComparer.GetHashCode(ProposerKingdomId ?? "");
			hash = (hash * 397) ^ IdComparer.GetHashCode(TargetKingdomId ?? "");
			hash = (hash * 397) ^ (int)Domain;
			return hash;
		}
	}

	public static bool operator ==(WorldDiplomacyOfferCooldownKey left, WorldDiplomacyOfferCooldownKey right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(WorldDiplomacyOfferCooldownKey left, WorldDiplomacyOfferCooldownKey right)
	{
		return !left.Equals(right);
	}

	private static string NormalizeId(string value)
	{
		return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
	}
}

public readonly struct WorldDiplomacyOfferCooldownDecision
{
	public WorldDiplomacyOfferCooldownDecision(
		WorldDiplomacyOfferCooldownKey key,
		WorldDiplomacyOfferCooldownAction action)
	{
		Key = key;
		Action = action;
	}

	public WorldDiplomacyOfferCooldownKey Key { get; }
	public WorldDiplomacyOfferCooldownAction Action { get; }
}

/// <summary>
/// Pure rules shared by runtime code and smoke tests. Round aggregation allocates only
/// when a round closes; cooldown boundary checks themselves allocate nothing.
/// </summary>
public static class WorldDiplomacyOfferCooldownRules
{
	private static readonly StringComparer IdComparer = StringComparer.OrdinalIgnoreCase;

	private readonly struct BilateralDomainKey : IEquatable<BilateralDomainKey>
	{
		internal BilateralDomainKey(WorldDiplomacyOfferCooldownKey directedKey)
		{
			if (IdComparer.Compare(directedKey.ProposerKingdomId, directedKey.TargetKingdomId) <= 0)
			{
				KingdomIdA = directedKey.ProposerKingdomId;
				KingdomIdB = directedKey.TargetKingdomId;
			}
			else
			{
				KingdomIdA = directedKey.TargetKingdomId;
				KingdomIdB = directedKey.ProposerKingdomId;
			}
			Domain = directedKey.Domain;
		}

		internal string KingdomIdA { get; }
		internal string KingdomIdB { get; }
		internal WorldDiplomacyOfferDomain Domain { get; }

		public bool Equals(BilateralDomainKey other)
		{
			return Domain == other.Domain
				&& IdComparer.Equals(KingdomIdA, other.KingdomIdA)
				&& IdComparer.Equals(KingdomIdB, other.KingdomIdB);
		}

		public override bool Equals(object obj)
		{
			return obj is BilateralDomainKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = IdComparer.GetHashCode(KingdomIdA ?? "");
				hash = (hash * 397) ^ IdComparer.GetHashCode(KingdomIdB ?? "");
				hash = (hash * 397) ^ (int)Domain;
				return hash;
			}
		}
	}

	/// <summary>
	/// A successful trade/alliance result belongs to the unordered bilateral domain and
	/// clears both old directed cooldowns. Only when neither direction succeeded do the
	/// actual failed proposer-target directions start/restart their own cooldowns.
	/// </summary>
	public static List<WorldDiplomacyOfferCooldownDecision> EvaluateClosedRound(
		IReadOnlyList<WorldDiplomacyOfferRoundObservation> observations)
	{
		HashSet<BilateralDomainKey> successfulDomains = new HashSet<BilateralDomainKey>();
		HashSet<WorldDiplomacyOfferCooldownKey> failedDirections =
			new HashSet<WorldDiplomacyOfferCooldownKey>();
		int count = observations?.Count ?? 0;
		for (int index = 0; index < count; index++)
		{
			WorldDiplomacyOfferRoundObservation observation = observations[index];
			if (!TryGetProposalDomain(observation.ProposalIntent, out WorldDiplomacyOfferDomain domain)) continue;
			WorldDiplomacyOfferCooldownKey key = new WorldDiplomacyOfferCooldownKey(
				observation.ProposerKingdomId,
				observation.TargetKingdomId,
				domain);
			if (!key.IsValid) continue;

			bool accepted = IsSuccessfulFinalStatus(observation.FinalStatus);
			bool failed = IsFailedFinalStatus(observation.FinalStatus);
			if (!accepted && !failed) continue;
			if (accepted) successfulDomains.Add(new BilateralDomainKey(key));
			else failedDirections.Add(key);
		}

		List<WorldDiplomacyOfferCooldownDecision> decisions =
			new List<WorldDiplomacyOfferCooldownDecision>((successfulDomains.Count * 2) + failedDirections.Count);
		foreach (BilateralDomainKey successful in successfulDomains)
		{
			decisions.Add(new WorldDiplomacyOfferCooldownDecision(
				new WorldDiplomacyOfferCooldownKey(successful.KingdomIdA, successful.KingdomIdB, successful.Domain),
				WorldDiplomacyOfferCooldownAction.ClearCooldown));
			decisions.Add(new WorldDiplomacyOfferCooldownDecision(
				new WorldDiplomacyOfferCooldownKey(successful.KingdomIdB, successful.KingdomIdA, successful.Domain),
				WorldDiplomacyOfferCooldownAction.ClearCooldown));
		}
		foreach (WorldDiplomacyOfferCooldownKey failedDirection in failedDirections)
		{
			if (successfulDomains.Contains(new BilateralDomainKey(failedDirection))) continue;
			decisions.Add(new WorldDiplomacyOfferCooldownDecision(
				failedDirection,
				WorldDiplomacyOfferCooldownAction.StartCooldown));
		}
		decisions.Sort(WorldDiplomacyOfferCooldownDecisionComparer.Instance);
		return decisions;
	}

	/// <summary>
	/// The boundary is intentionally half-open: the cooldown applies while currentDay
	/// is less than failedRoundDay + configuredDays and expires exactly at equality.
	/// A configured value of zero disables the rule without deleting stored history.
	/// </summary>
	public static bool IsCoolingDown(int failedRoundDay, int currentDay, int configuredDays)
	{
		if (configuredDays <= 0 || failedRoundDay < 0) return false;
		return (long)currentDay < (long)failedRoundDay + configuredDays;
	}

	public static bool TryGetProposalDomain(string proposalIntent, out WorldDiplomacyOfferDomain domain)
	{
		if (EqualsToken(proposalIntent, "propose_trade"))
		{
			domain = WorldDiplomacyOfferDomain.Trade;
			return true;
		}
		if (EqualsToken(proposalIntent, "propose_alliance"))
		{
			domain = WorldDiplomacyOfferDomain.Alliance;
			return true;
		}
		domain = WorldDiplomacyOfferDomain.None;
		return false;
	}

	private static bool IsSuccessfulFinalStatus(string finalStatus)
	{
		return EqualsToken(finalStatus, "accepted") || EqualsToken(finalStatus, "partially_executed");
	}

	private static bool IsFailedFinalStatus(string finalStatus)
	{
		string normalized = (finalStatus ?? "").Trim();
		return string.Equals(normalized, "rejected", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "expired", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "countered", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "superseded", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "execution_failed", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "open", StringComparison.OrdinalIgnoreCase);
	}

	private static bool EqualsToken(string value, string expected)
	{
		return string.Equals((value ?? "").Trim(), expected, StringComparison.OrdinalIgnoreCase);
	}

	private sealed class WorldDiplomacyOfferCooldownDecisionComparer : IComparer<WorldDiplomacyOfferCooldownDecision>
	{
		internal static readonly WorldDiplomacyOfferCooldownDecisionComparer Instance =
			new WorldDiplomacyOfferCooldownDecisionComparer();

		public int Compare(WorldDiplomacyOfferCooldownDecision left, WorldDiplomacyOfferCooldownDecision right)
		{
			int proposer = IdComparer.Compare(left.Key.ProposerKingdomId, right.Key.ProposerKingdomId);
			if (proposer != 0) return proposer;
			int target = IdComparer.Compare(left.Key.TargetKingdomId, right.Key.TargetKingdomId);
			if (target != 0) return target;
			return left.Key.Domain.CompareTo(right.Key.Domain);
		}
	}
}
