using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

public sealed partial class PlayerNotorietyBehavior
{
    // One sorted snapshot per record revision/day, shared by candidates. No per-NPC pruning or sorting.
    private int _experienceCacheDay = -1;
    private int _experienceCacheCount = -1;
    private PlayerActionEntry _experienceLastEntry;
    private List<PlayerExperienceMotive> _experienceRecent = new List<PlayerExperienceMotive>();
    private string _experienceHistorySource;
    private string _experienceHistoryEvidence;
    private string _experienceHistoryVersion;

    internal static List<PlayerExperienceMotive> CaptureExperience(Hero hero, bool courier, bool companion)
    {
        var result = new List<PlayerExperienceMotive>(2);
        var owner = Instance;
        if (owner == null || !IsValidObserver(hero)) return result;
        bool inParty = companion && MobileParty.MainParty != null && hero.PartyBelongedTo == MobileParty.MainParty;
        int day = GetCurrentGameDayIndex();
        if (inParty || owner.CanObserverKnowRecentActions(hero, courier))
        {
            var entries = owner._state.RecentActions;
            var last = entries != null && entries.Count > 0 ? entries[entries.Count - 1] : null;
            if (owner._experienceCacheDay != day || owner._experienceCacheCount != (entries?.Count ?? 0)
                || !ReferenceEquals(owner._experienceLastEntry, last))
            {
                owner._experienceRecent = (entries ?? new List<PlayerActionEntry>()).Where(x => x != null && x.Day >= day - 9 && x.Day <= day)
                    .OrderByDescending(x => x.Day).ThenByDescending(x => x.Sequence).Take(16)
                    .Select(entry => new PlayerExperienceMotive { Kind = PlayerExperienceContactState.RecentKind,
                        SourceKey = "recent:" + ExperienceHash(string.IsNullOrWhiteSpace(entry.StableKey) ? entry.Day + ":" + entry.Sequence + ":" + entry.Text : entry.StableKey),
                        Evidence = PlayerExperienceContactState.BoundedEvidence(entry.Text, 400), SourceVersion = ExperienceHash(entry.Text), Day = entry.Day,
                        Urgency = Math.Max(60f, 90f - (day - entry.Day) * 3f) }).ToList();
                owner._experienceCacheDay = day;
                owner._experienceCacheCount = entries?.Count ?? 0;
                owner._experienceLastEntry = last;
            }
            foreach (var entry in owner._experienceRecent)
            {
                if (!ProactiveNpcRequestBehavior.CanOfferExperience(hero, entry)) continue;
                result.Add(new PlayerExperienceMotive { Kind = entry.Kind, SourceKey = entry.SourceKey, SourceVersion = entry.SourceVersion,
                    Evidence = RenderPlayerActionTextForPrompt(entry.Evidence, BuildPlayerDisplayNameForPrompt(hero)),
                    Day = entry.Day, Urgency = entry.Urgency });
                break;
            }
        }
        if (owner.DoesObserverKnowPlayer(hero))
        {
            string rawHistory = owner._state.MajorSummary ?? "";
            if (!string.Equals(rawHistory, owner._experienceHistorySource, StringComparison.Ordinal))
            {
                owner._experienceHistorySource = rawHistory;
                owner._experienceHistoryEvidence = PlayerExperienceContactState.BoundedEvidence(rawHistory, 800);
                owner._experienceHistoryVersion = ExperienceHash(rawHistory);
            }
            string history = RenderPlayerHistoryTextForPrompt(owner._experienceHistoryEvidence, BuildPlayerHistoryDisplayName());
            if (!string.IsNullOrWhiteSpace(history))
            {
                var motive = new PlayerExperienceMotive { Kind = PlayerExperienceContactState.HistoryKind,
                    SourceKey = "history", Evidence = history, SourceVersion = owner._experienceHistoryVersion, Day = day, Urgency = 65f };
                if (ProactiveNpcRequestBehavior.CanOfferExperience(hero, motive)) result.Add(motive);
            }
        }
        return result;
    }

    internal static bool CanStillDiscussExperience(Hero hero, PlayerExperienceMotive motive, bool courier, bool companion)
    {
        if (Instance == null || !IsValidObserver(hero) || motive == null) return false;
        if (motive.Kind == PlayerExperienceContactState.HistoryKind) return Instance.DoesObserverKnowPlayer(hero)
            && motive.SourceVersion == ExperienceHash(Instance._state.MajorSummary);
        bool inParty = companion && MobileParty.MainParty != null && hero.PartyBelongedTo == MobileParty.MainParty;
        if (motive.Day < GetCurrentGameDayIndex() - 9 || !(inParty || Instance.CanObserverKnowRecentActions(hero, courier))) return false;
        // Only selected contacts recheck their source; candidate scans use the shared cache.
        foreach (var entry in Instance._state.RecentActions ?? new List<PlayerActionEntry>())
        {
            if (entry == null || entry.Day != motive.Day || ExperienceHash(entry.Text) != motive.SourceVersion) continue;
            string key = "recent:" + ExperienceHash(string.IsNullOrWhiteSpace(entry.StableKey) ? entry.Day + ":" + entry.Sequence + ":" + entry.Text : entry.StableKey);
            if (key == motive.SourceKey) return true;
        }
        return false;
    }

    private static string ExperienceHash(string value)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))).Replace("-", "");
    }
}
