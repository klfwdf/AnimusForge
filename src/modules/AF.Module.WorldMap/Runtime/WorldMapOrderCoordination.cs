using System;

namespace AnimusForge;

internal sealed class WorldMapOrderToken
{
	internal string Kind { get; set; } = "";
	internal string[] Parts { get; set; } = Array.Empty<string>();
	internal bool IsStop { get; set; }
}

internal enum WorldMapOrderSequenceDecision
{
	AcceptCommand = 0,
	AcceptLeadingStop = 1,
	RejectNonLeadingStop = 2
}

internal sealed class WorldMapOrderSequenceCoordinator
{
	internal bool HasParsedTag { get; private set; }
	internal bool LeadingStop { get; private set; }

	internal WorldMapOrderSequenceDecision Observe(bool isStop)
	{
		if (!HasParsedTag)
		{
			HasParsedTag = true;
			LeadingStop = isStop;
			return isStop
				? WorldMapOrderSequenceDecision.AcceptLeadingStop
				: WorldMapOrderSequenceDecision.AcceptCommand;
		}
		return isStop
			? WorldMapOrderSequenceDecision.RejectNonLeadingStop
			: WorldMapOrderSequenceDecision.AcceptCommand;
	}
}

internal enum WorldMapOrderAdmissionRoute
{
	NonHeroParty = 0,
	CompanionPartyCreation = 1,
	GovernorExpedition = 2,
	ExistingHeroParty = 3
}

internal enum WorldMapCommandRoute
{
	Unknown = 0,
	GoToSettlement = 1,
	PatrolSettlement = 2,
	FollowHero = 3,
	FollowParty = 4,
	AttackHero = 5,
	AttackParty = 6,
	MergeToPlayer = 7
}

/// <summary>
/// Detached WorldMap protocol, admission and command-routing rules. Runtime IDs
/// and game-object qualification remain in the Campaign host immediately before
/// enqueue/mutation; this owner never reads TaleWorlds objects.
/// </summary>
internal static class WorldMapOrderCoordinator
{
	private const string Prefix = "[ACTION:WORLDMAP_ORDER:";

	internal static bool TryParseToken(string tag, out WorldMapOrderToken token)
	{
		token = null;
		string text = (tag ?? "").Trim();
		if (!text.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
			|| !text.EndsWith("]", StringComparison.Ordinal))
		{
			return false;
		}
		string inner = text.Substring(Prefix.Length, text.Length - Prefix.Length - 1);
		string[] parts = inner.Split(new[] { ':' }, StringSplitOptions.None);
		for (int i = 0; i < parts.Length; i++)
		{
			parts[i] = (parts[i] ?? "").Trim();
		}
		if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
		{
			return false;
		}
		string kind = parts[0].ToUpperInvariant();
		token = new WorldMapOrderToken
		{
			Kind = kind,
			Parts = parts,
			IsStop = string.Equals(kind, "STOP", StringComparison.Ordinal)
		};
		return true;
	}

	internal static bool IsSafeIdentifier(string value)
	{
		return !string.IsNullOrWhiteSpace(value)
			&& value.IndexOfAny(new[] { '[', ']', '\r', '\n' }) < 0;
	}

	internal static WorldMapOrderAdmissionRoute SelectAdmissionRoute(
		bool hasHero,
		bool heroIsInPlayerMainParty,
		bool hasImplicitPartyTask,
		bool heroIsGovernor)
	{
		if (!hasHero)
		{
			return WorldMapOrderAdmissionRoute.NonHeroParty;
		}
		if (heroIsInPlayerMainParty && hasImplicitPartyTask)
		{
			return WorldMapOrderAdmissionRoute.CompanionPartyCreation;
		}
		return heroIsGovernor
			? WorldMapOrderAdmissionRoute.GovernorExpedition
			: WorldMapOrderAdmissionRoute.ExistingHeroParty;
	}

	internal static WorldMapCommandRoute ClassifyCommand(string kind)
	{
		switch ((kind ?? "").Trim().ToUpperInvariant())
		{
			case "GOTOSETTLEMENT": return WorldMapCommandRoute.GoToSettlement;
			case "PATROLSETTLEMENT": return WorldMapCommandRoute.PatrolSettlement;
			case "FOLLOWHERO": return WorldMapCommandRoute.FollowHero;
			case "FOLLOWPARTY": return WorldMapCommandRoute.FollowParty;
			case "ATTACKHERO": return WorldMapCommandRoute.AttackHero;
			case "ATTACKPARTY": return WorldMapCommandRoute.AttackParty;
			case "MERGETOPLAYER": return WorldMapCommandRoute.MergeToPlayer;
			default: return WorldMapCommandRoute.Unknown;
		}
	}
}
