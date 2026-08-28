using System;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

public enum NobleExecutionActorKind
{
    Invalid = 0,
    Player = 1,
    PlayerClan = 2,
    FriendlyNoble = 3,
}

public enum NobleExecutionHeadDisposition
{
    GiveToPlayer = 0,
    KeepByExecutioner = 1,
}

public enum NobleExecutionRelationAttribution
{
    Player = 0,
    Executioner = 1,
}

public readonly struct NobleExecutionActorFacts
{
    public NobleExecutionActorFacts(
        bool isAlive,
        bool isPrisoner,
        bool isPlayer,
        bool isPlayerClanOrCompanion,
        bool isFriendlyNoble)
    {
        IsAlive = isAlive;
        IsPrisoner = isPrisoner;
        IsPlayer = isPlayer;
        IsPlayerClanOrCompanion = isPlayerClanOrCompanion;
        IsFriendlyNoble = isFriendlyNoble;
    }

    public bool IsAlive { get; }
    public bool IsPrisoner { get; }
    public bool IsPlayer { get; }
    public bool IsPlayerClanOrCompanion { get; }
    public bool IsFriendlyNoble { get; }
}

public readonly struct NobleExecutionActorDecision
{
    public NobleExecutionActorDecision(
        NobleExecutionActorKind actorKind,
        NobleExecutionRelationAttribution relationAttribution,
        bool mustGiveHeadToPlayer,
        string reasonCode)
    {
        ActorKind = actorKind;
        RelationAttribution = relationAttribution;
        MustGiveHeadToPlayer = mustGiveHeadToPlayer;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public NobleExecutionActorKind ActorKind { get; }
    public NobleExecutionRelationAttribution RelationAttribution { get; }
    public bool MustGiveHeadToPlayer { get; }
    public string ReasonCode { get; }
    public bool IsEligible => ActorKind != NobleExecutionActorKind.Invalid;
}

/// <summary>
/// Pure rules shared by the standalone GCCZ contract tests and the fused AF runtime.
/// Consent is represented by an accepted semantic action tag; text keywords are not
/// used as authorization.
/// </summary>
public static class NoblePrisonerExecutionPolicy
{
    public const int MinimumMapDelayHours = 6;
    public const int MaximumMapDelayHours = 18;

    public static NobleExecutionActorDecision EvaluateActor(NobleExecutionActorFacts facts)
    {
        if (!facts.IsAlive)
        {
            return Invalid("actor_dead");
        }
        if (facts.IsPrisoner)
        {
            return Invalid("actor_is_prisoner");
        }
        if (facts.IsPlayer)
        {
            return new NobleExecutionActorDecision(
                NobleExecutionActorKind.Player,
                NobleExecutionRelationAttribution.Player,
                mustGiveHeadToPlayer: true,
                "player");
        }
        if (facts.IsPlayerClanOrCompanion)
        {
            return new NobleExecutionActorDecision(
                NobleExecutionActorKind.PlayerClan,
                NobleExecutionRelationAttribution.Player,
                mustGiveHeadToPlayer: true,
                "player_clan");
        }
        if (facts.IsFriendlyNoble)
        {
            return new NobleExecutionActorDecision(
                NobleExecutionActorKind.FriendlyNoble,
                NobleExecutionRelationAttribution.Executioner,
                mustGiveHeadToPlayer: false,
                "friendly_noble");
        }
        return Invalid("actor_not_authorized");
    }

    public static bool IsHeadDispositionAllowed(
        NobleExecutionActorDecision actor,
        NobleExecutionHeadDisposition disposition)
    {
        return actor.IsEligible
            && (!actor.MustGiveHeadToPlayer
                || disposition == NobleExecutionHeadDisposition.GiveToPlayer);
    }

    public static int ComputeMapDelayHours(string actorHeroId, string prisonerHeroId)
    {
        string key = (actorHeroId ?? string.Empty).Trim() + "|" + (prisonerHeroId ?? string.Empty).Trim();
        uint hash = 2166136261u;
        for (int i = 0; i < key.Length; i++)
        {
            hash ^= key[i];
            hash *= 16777619u;
        }
        int range = MaximumMapDelayHours - MinimumMapDelayHours + 1;
        return MinimumMapDelayHours + (int)(hash % (uint)range);
    }

    public static string BuildHeadItemName(string prisonerName)
    {
        string name = (prisonerName ?? string.Empty).Trim();
        return (name.Length == 0 ? "无名贵族" : name) + "的头颅";
    }

    private static NobleExecutionActorDecision Invalid(string reasonCode)
    {
        return new NobleExecutionActorDecision(
            NobleExecutionActorKind.Invalid,
            NobleExecutionRelationAttribution.Executioner,
            mustGiveHeadToPlayer: false,
            reasonCode);
    }
}

public static class NoblePrisonerExecutionActionTagCatalog
{
    public const string EscortPrefix = "[ACTION:NOBLE_EXECUTE_ESCORT:";
    public const string PartyPrisonerPrefix = "[ACTION:NOBLE_EXECUTE_PARTY_PRISONER:";

    private const string HeroIdPattern = "[A-Za-z0-9_.-]+";
    private static readonly Regex EscortRegex = new Regex(
        @"\[ACTION:NOBLE_EXECUTE_ESCORT:(?<id>" + HeroIdPattern + @"):(?<head>GIVE_HEAD|KEEP_HEAD)\]",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex PartyRegex = new Regex(
        @"\[ACTION:NOBLE_EXECUTE_PARTY_PRISONER:(?<id>" + HeroIdPattern + @")\]",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static string BuildEscortTag(string prisonerHeroId, NobleExecutionHeadDisposition disposition)
    {
        string id = NormalizeHeroId(prisonerHeroId);
        if (id.Length == 0)
        {
            return string.Empty;
        }
        string head = disposition == NobleExecutionHeadDisposition.GiveToPlayer
            ? "GIVE_HEAD"
            : "KEEP_HEAD";
        return EscortPrefix + id + ":" + head + "]";
    }

    public static string BuildPartyPrisonerTag(string prisonerHeroId)
    {
        string id = NormalizeHeroId(prisonerHeroId);
        return id.Length == 0 ? string.Empty : PartyPrisonerPrefix + id + "]";
    }

    public static bool TryExtractEscort(
        string content,
        out string prisonerHeroId,
        out NobleExecutionHeadDisposition disposition)
    {
        prisonerHeroId = string.Empty;
        disposition = NobleExecutionHeadDisposition.GiveToPlayer;
        Match match = EscortRegex.Match(content ?? string.Empty);
        if (!match.Success)
        {
            return false;
        }
        prisonerHeroId = match.Groups["id"].Value;
        disposition = string.Equals(match.Groups["head"].Value, "KEEP_HEAD", StringComparison.OrdinalIgnoreCase)
            ? NobleExecutionHeadDisposition.KeepByExecutioner
            : NobleExecutionHeadDisposition.GiveToPlayer;
        return true;
    }

    public static bool TryExtractPartyPrisoner(string content, out string prisonerHeroId)
    {
        prisonerHeroId = string.Empty;
        Match match = PartyRegex.Match(content ?? string.Empty);
        if (!match.Success)
        {
            return false;
        }
        prisonerHeroId = match.Groups["id"].Value;
        return true;
    }

    public static string StripExecutionTags(string content)
    {
        string stripped = EscortRegex.Replace(content ?? string.Empty, string.Empty);
        return PartyRegex.Replace(stripped, string.Empty).Trim();
    }

    private static string NormalizeHeroId(string value)
    {
        string id = (value ?? string.Empty).Trim();
        return id.Length > 0 && Regex.IsMatch(id, "^" + HeroIdPattern + "$") ? id : string.Empty;
    }
}

public readonly struct NobleExecutionTaskRecord
{
    public NobleExecutionTaskRecord(string operationId, string actorHeroId, string prisonerHeroId, long dueHour)
    {
        OperationId = operationId ?? string.Empty;
        ActorHeroId = actorHeroId ?? string.Empty;
        PrisonerHeroId = prisonerHeroId ?? string.Empty;
        DueHour = dueHour;
    }

    public string OperationId { get; }
    public string ActorHeroId { get; }
    public string PrisonerHeroId { get; }
    public long DueHour { get; }
}

public static class NobleExecutionTaskCodec
{
    public const string Schema = "AFNE1";

    public static string Serialize(NobleExecutionTaskRecord record)
    {
        return IsSafe(record.OperationId)
            && IsSafe(record.ActorHeroId)
            && IsSafe(record.PrisonerHeroId)
            && record.DueHour >= 0
                ? string.Join("|", Schema, record.OperationId, record.ActorHeroId, record.PrisonerHeroId, record.DueHour.ToString())
                : string.Empty;
    }

    public static bool TryDeserialize(string value, out NobleExecutionTaskRecord record)
    {
        record = default;
        string[] fields = (value ?? string.Empty).Split('|');
        if (fields.Length != 5
            || !string.Equals(fields[0], Schema, StringComparison.Ordinal)
            || !IsSafe(fields[1])
            || !IsSafe(fields[2])
            || !IsSafe(fields[3])
            || !long.TryParse(fields[4], out long dueHour)
            || dueHour < 0)
        {
            return false;
        }
        record = new NobleExecutionTaskRecord(fields[1], fields[2], fields[3], dueHour);
        return true;
    }

    private static bool IsSafe(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && Regex.IsMatch(value.Trim(), "^[A-Za-z0-9_.-]+$");
    }
}
