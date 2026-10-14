using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// Detached selection evidence. Never holds Hero/Party references or translates saved text.
internal sealed class PlayerExperienceMotive
{
    public string Kind { get; set; }
    public string SourceKey { get; set; }
    public string Evidence { get; set; }
    public string SourceVersion { get; set; }
    public int Day { get; set; }
    public float Urgency { get; set; }
}

internal sealed class PlayerExperienceReservation
{
    public string Id { get; set; }
    public string HeroId { get; set; }
    public string Channel { get; set; }
    public PlayerExperienceMotive Motive { get; set; }
    public float ExpiresAtDays { get; set; }
}

// Main-thread owner, persisted by the existing proactive behavior's chunked save.
internal sealed class PlayerExperienceContactState
{
    internal const string HistoryKind = "PlayerHistoryDiscussion";
    internal const string RecentKind = "PlayerRecentEvent";
    internal const int MaxReceipts = 1024;
    public Dictionary<string, float> DiscussedUntil { get; set; } = new Dictionary<string, float>(StringComparer.Ordinal);
    public Dictionary<string, float> SourceFatigueUntil { get; set; } = new Dictionary<string, float>(StringComparer.Ordinal);
    public List<PlayerExperienceReservation> Pending { get; set; } = new List<PlayerExperienceReservation>();

    internal static bool IsExperience(string kind) => kind == HistoryKind || kind == RecentKind;
    private static string Key(string heroId, PlayerExperienceMotive motive) => heroId + "|" + (motive.Kind == HistoryKind ? HistoryKind : motive.SourceKey);

    internal void Normalize()
    {
        DiscussedUntil ??= new Dictionary<string, float>(StringComparer.Ordinal);
        SourceFatigueUntil ??= new Dictionary<string, float>(StringComparer.Ordinal);
        Pending ??= new List<PlayerExperienceReservation>();
        Pending.RemoveAll(x => x == null || x.Motive == null || !IsExperience(x.Motive.Kind) || string.IsNullOrEmpty(x.Id));
    }

    internal void Prune(float now)
    {
        foreach (string key in DiscussedUntil.Where(x => x.Value <= now).Select(x => x.Key).ToArray()) DiscussedUntil.Remove(key);
        foreach (string key in SourceFatigueUntil.Where(x => x.Value <= now).Select(x => x.Key).ToArray()) SourceFatigueUntil.Remove(key);
        Pending.RemoveAll(x => x.ExpiresAtDays <= now);
    }

    internal bool CanOffer(string heroId, PlayerExperienceMotive motive, float now)
    {
        if (string.IsNullOrEmpty(heroId) || motive == null || !IsExperience(motive.Kind)
            || string.IsNullOrWhiteSpace(motive.SourceKey) || string.IsNullOrWhiteSpace(motive.Evidence)) return false;
        if (motive.Kind == RecentKind && (motive.Day > now || motive.Day < (int)Math.Floor(now) - 9)) return false;
        if (DiscussedUntil.TryGetValue(Key(heroId, motive), out float until) && until > now) return false;
        if (SourceFatigueUntil.TryGetValue(motive.SourceKey, out until) && until > now) return false;
        if (DiscussedUntil.Count + Pending.Count >= MaxReceipts) return false;
        return !Pending.Any(x => x.HeroId == heroId && x.ExpiresAtDays > now);
    }

    internal PlayerExperienceReservation Reserve(string id, string heroId, string channel, PlayerExperienceMotive motive, float now, float expires)
    {
        Prune(now);
        if (string.IsNullOrEmpty(id) || !CanOffer(heroId, motive, now)) return null;
        var reservation = new PlayerExperienceReservation { Id = id, HeroId = heroId, Channel = channel, Motive = motive, ExpiresAtDays = expires };
        Pending.Add(reservation);
        return reservation;
    }

    internal void Release(string id) => Pending.RemoveAll(x => x.Id == id);

    internal bool Complete(PlayerExperienceReservation reservation, float now)
    {
        if (reservation == null || !Pending.Any(x => ReferenceEquals(x, reservation))) return false;
        Release(reservation.Id);
        DiscussedUntil[Key(reservation.HeroId, reservation.Motive)] = reservation.Motive.Kind == HistoryKind
            ? now + 30f : Math.Max(now + 1f, reservation.Motive.Day + 10f);
        // One day's source fatigue prevents an immediate chorus, without discarding other events.
        if (reservation.Motive.Kind == RecentKind) SourceFatigueUntil[reservation.Motive.SourceKey] = now + 1f;
        return true;
    }

    // Keep complete source records; do not invent a new summary or cut a sentence/negation.
    internal static string BoundedEvidence(string text, int limit)
    {
        text = (text ?? "").Trim();
        if (text.Length <= limit) return text;
        int boundary = text.LastIndexOf('\n', limit - 1, limit);
        if (boundary < 0)
        {
            for (int i = limit - 1; i >= 0; i--)
                if (text[i] == '。' || text[i] == '！' || text[i] == '？' || (text[i] == '.' && i + 1 < text.Length && char.IsWhiteSpace(text[i + 1])))
                { boundary = i + 1; break; }
        }
        return boundary > 0 ? text.Substring(0, boundary).Trim() : "";
    }
}
