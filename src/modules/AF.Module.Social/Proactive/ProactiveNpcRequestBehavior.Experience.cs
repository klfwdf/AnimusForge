using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace AnimusForge;

public sealed partial class ProactiveNpcRequestBehavior
{
    private PlayerExperienceContactState _experienceContacts = new PlayerExperienceContactState();
    private readonly HashSet<string> _experienceInFlight = new HashSet<string>(StringComparer.Ordinal);

    internal static bool CanOfferExperience(Hero hero, PlayerExperienceMotive motive) => Instance != null && hero != null
        && Instance._experienceContacts.CanOffer(hero.StringId, motive, NowDays());

    internal static PlayerExperienceReservation ReserveExperience(string id, Hero hero, string channel, PlayerExperienceMotive motive, float lifetimeDays = 30f)
    {
        if (motive == null) return null;
        if (!PlayerNotorietyBehavior.CanStillDiscussExperience(hero, motive, channel == "courier", channel == "companion")) return null;
        return Instance?._experienceContacts.Reserve(id, hero.StringId, channel, motive, NowDays(), NowDays() + lifetimeDays);
    }

    internal static void ReleaseExperience(string id)
    {
        if (Instance == null || string.IsNullOrEmpty(id)) return;
        Instance._experienceInFlight.Remove(id);
        Instance._experienceContacts.Release(id);
    }

    internal static PlayerExperienceReservation FindExperienceOpening(Hero hero, string fact)
    {
        if (Instance == null || hero == null || string.IsNullOrEmpty(fact)) return null;
        return Instance._experienceContacts.Pending.FirstOrDefault(x => x.HeroId == hero.StringId
            && !string.IsNullOrEmpty(x.Motive?.Evidence) && fact.IndexOf(x.Motive.Evidence, StringComparison.Ordinal) >= 0);
    }

    internal static void ArmExperienceOpening(Hero hero, string fact)
    {
        var ticket = FindExperienceOpening(hero, fact);
        if (ticket != null) Instance._experienceInFlight.Add(ticket.Id);
    }

    internal static void FinishExperienceOpening(PlayerExperienceReservation ticket, bool presented)
    {
        if (Instance == null || ticket == null) return;
        if (presented && Instance._experienceContacts.Complete(ticket, NowDays()))
            Logger.Log("ProactiveNpcRequest", "experience presented kind=" + ticket.Motive.Kind + " source=" + ticket.Motive.SourceKey + " channel=" + ticket.Channel + " hero=" + ticket.HeroId);
        ReleaseExperience(ticket.Id);
        if (!presented && Instance._activeSession?.Id == ticket.Id)
            Instance.CancelActiveSession("experience_not_presented", true);
    }

    private void ReconcileExperienceContacts()
    {
        _experienceContacts.Prune(NowDays());
        _experienceInFlight.IntersectWith(_experienceContacts.Pending.Select(x => x.Id));
        foreach (var pending in _experienceContacts.Pending.ToArray())
        {
            if (_experienceInFlight.Contains(pending.Id)) continue;
            bool alive = pending.Channel == "map" ? _activeSession?.Id == pending.Id
                : pending.Channel == "companion" ? CompanionProactiveChatBehavior.HasExperienceSession(pending.Id)
                : pending.Channel == "courier" && CourierDeliveryBehavior.HasExperienceSession(pending.Id);
            if (!alive) _experienceContacts.Release(pending.Id);
        }
    }

    private ProactiveCandidate BuildExperienceCandidate(ProactiveCandidate source, DuelSettings settings)
    {
        if (source == null || source.AtWarWithPlayer) return null;
        var motives = PlayerNotorietyBehavior.CaptureExperience(source.Hero, false, false);
        if (motives.Count == 0) return null;
        // Give an older career a chance even while the player is accumulating new events.
        var motive = motives[motives.Count > 1 && MBRandom.RandomFloat < .25f ? 1 : 0];
        var result = TryBuildNeedCandidate(source, settings, motive.Kind, motive.Urgency);
        if (result != null) result.Experience = motive;
        return result;
    }

    internal static string ExperienceFact(PlayerExperienceMotive motive)
    {
        if (motive == null) return "";
        return SocialPromptTexts.Get(motive.Kind == PlayerExperienceContactState.HistoryKind ? "experience_history" : "experience_recent")
            + "\n" + PlayerExperiencePrompt.Begin + "\n" + motive.Evidence + "\n" + PlayerExperiencePrompt.End;
    }

}
