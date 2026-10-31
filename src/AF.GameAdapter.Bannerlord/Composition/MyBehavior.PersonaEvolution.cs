using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using AnimusForge.Refactor.Adapters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class MyBehavior
{
    private readonly Queue<string> _personaEvolutionQueue = new Queue<string>();
    private readonly HashSet<string> _personaEvolutionQueued = new HashSet<string>(StringComparer.Ordinal);
    private Task _personaEvolutionRequest;
    private DateTime _personaEvolutionNextStartUtc;
    private int _personaEvolutionEpoch;
    private bool PersonaEvolutionEnabled => DuelSettings.GetSettings()?.EnableNpcPersonaEventEvolution == true && IsWorldBulletinEnabled();

    internal static void NotifyPersonaEvolutionSettingChanged(DuelSettings settings)
    {
        // Preset/import preview settings are separate instances and must not retire live work.
        if (ReferenceEquals(settings, DuelSettings.GetSettings())) Instance?.ResetPersonaEvolution(clearPending: true);
    }
    private void ResetPersonaEvolution(bool clearPending)
    {
        _personaEvolutionEpoch++;
        // Inspect affected owners only, at toggle/load/reset boundaries.
        if (clearPending)
            foreach (string id in _personaEvolutionQueued)
            {
                var state = _personaProfiles.Get(id, false)?.Evolution;
                state?.Pending?.Clear();
                if (state != null) state.Attempts = 0;
            }
        _personaEvolutionQueue.Clear();
        _personaEvolutionQueued.Clear();
        // Retain an outstanding transport until it really finishes: toggling cannot bypass concurrency.
    }
    private void RestorePersonaEvolutionQueue()
    {
        ResetPersonaEvolution(false);
        foreach (var entry in _personaProfiles.Profiles)
            if (entry.Value?.Evolution?.Pending?.Count > 0) QueuePersonaEvolution(entry.Key);
    }
    private void QueuePersonaEvolution(string id)
    {
        if (_personaEvolutionQueued.Add(id)) _personaEvolutionQueue.Enqueue(id);
    }
    private void OnWorldBulletinEventCaptured(WorldBulletinEvent fact)
    {
        if (!PersonaEvolutionEnabled || fact == null || !PersonaEvolutionPolicy.Accepts(fact.Kind)) return;
        try
        {
            var candidates = new Dictionary<Hero, List<string>>();
            void Add(Hero target, Hero subject, string relation)
            {
                if (target == null || target == Hero.MainHero || !target.IsAlive) return;
                if (!candidates.TryGetValue(target, out var connections)) candidates[target] = connections = new List<string>();
                connections.Add(PersonaPromptTexts.Format("AF_PERSONA_EVOLUTION_" + relation, "{NAME}",
                    ("NAME", subject?.Name?.ToString() ?? ""),
                    ("RELATION", subject == null || subject == target ? "" : target.GetRelation(subject).ToString())));
            }
            foreach (var participant in fact.Participants ?? Enumerable.Empty<WorldBulletinParticipant>())
            {
                var subject = string.IsNullOrEmpty(participant?.HeroId) ? null : Hero.Find(participant.HeroId);
                if (subject == null) continue;
                Add(subject, subject, "Self");
                Add(subject.Spouse, subject, "Spouse");
                Add(subject.Father, subject, "Parent");
                Add(subject.Mother, subject, "Parent");
                foreach (Hero child in subject.Children) Add(child, subject, "Child");
                foreach (Hero sibling in subject.Siblings) Add(sibling, subject, "Sibling");
                Add(subject.Clan?.Leader, subject, "ClanLeader");
            }
            string material = PersonaEvolutionPolicy.Clip(fact.GameDate, 80) + "\n" + fact.Kind + "\n"
                + PersonaEvolutionPolicy.Clip(fact.Sentence, 900) + "\n" + PersonaEvolutionPolicy.Clip(fact.Detail, 300);
            foreach (var entry in candidates)
            {
                var profile = GetNpcPersonaProfile(entry.Key, true);
                if (profile == null) continue;
                var state = profile.Evolution ??= new PersonaEvolutionState();
                if (PersonaEvolutionPolicy.Enqueue(state, new PersonaEvolutionFact
                    { Key = fact.Key, Hour = fact.Hour, Facts = material, Connection = string.Join("\n", entry.Value.Distinct()) }))
                    QueuePersonaEvolution(entry.Key.StringId);
                else if (state.Pending.Count >= PersonaEvolutionPolicy.MaxPending)
                    Logger.Log("NpcPersonaEvolution", "[WARN] pending event limit reached for " + entry.Key.StringId);
            }
        }
        catch (Exception ex) { Logger.Log("NpcPersonaEvolution", "[WARN] event capture skipped: " + ex.Message); }
    }
    private void TryStartPersonaEvolution()
    {
        if (_personaEvolutionQueue.Count == 0) return;
        if (!PersonaEvolutionEnabled) { ResetPersonaEvolution(true); return; }
        if (_personaEvolutionRequest != null && !_personaEvolutionRequest.IsCompleted) return;
        if (DateTime.UtcNow < _personaEvolutionNextStartUtc) return;
        _personaEvolutionNextStartUtc = DateTime.UtcNow.AddSeconds(5);
        // One queue head per check, no all-Hero or all-queue scan.
        string id = _personaEvolutionQueue.Dequeue();
        _personaEvolutionQueued.Remove(id);
        var profile = _personaProfiles.Get(id, false);
        var state = profile?.Evolution;
        if (state?.Pending?.Count > 0)
        {
            var hero = Hero.Find(id);
            if (hero == null || !hero.IsAlive || hero == Hero.MainHero) { state.Pending.Clear(); return; }
            var batch = PersonaEvolutionPolicy.TakeBatch(state, GetCurrentCampaignHourSafe());
            QueuePersonaEvolution(id);
            if (batch.Length == 0) return;
            if (state.Attempts >= 2)
            {
                PersonaEvolutionPolicy.Complete(state, batch);
                Logger.Log("NpcPersonaEvolution", "[SKIP] saved attempt limit reached: " + id);
                return;
            }
            // Share reservations, game facts, lore retrieval, editable requirements, parser and commit guards with manual regeneration.
            _npcPersonaGeneration.GetState(id, out bool busy, out _);
            if (busy) return; // Retry the queued owner later without spending an event attempt or starting another request.
            StartPersonaEvolution(id, hero, profile, state, batch);
        }
    }

    // Keep captured delegates out of the idle Tick method: allocate them only for an eligible batch.
    private void StartPersonaEvolution(string id, Hero hero, NpcPersonaProfile profile, PersonaEvolutionState state, PersonaEvolutionFact[] batch)
    {
        var snapshot = PersonaEvolutionPolicy.Snapshot(profile);
        var input = new JObject
        {
            ["persona"] = PersonaEvolutionPolicy.RequestPersona(snapshot),
            ["events"] = new JArray(batch.Select(e => new JObject { ["facts"] = e.Facts, ["connection"] = e.Connection }))
        };
        int epoch = _personaEvolutionEpoch;
        long generation = SaveRuntimeGuard.CaptureGeneration();
        bool Current()
        {
            var current = _personaProfiles.Get(id, false);
            return epoch == _personaEvolutionEpoch && PersonaEvolutionEnabled && hero.IsAlive
                && ReferenceEquals(Hero.Find(id), hero) && ReferenceEquals(current, profile)
                && ReferenceEquals(current?.Evolution, state) && PersonaEvolutionPolicy.IsCurrent(current, snapshot);
        }
        bool saved = false;
        var trigger = new NpcPersonaEventRegeneration
        {
            Facts = input.ToString(Formatting.None),
            IsCurrent = Current,
            OnSaved = updated =>
            {
                saved = true;
                updated.RecentChangeReason = PersonaEvolutionPolicy.Clip(batch[0].Facts, 240);
                PersonaEvolutionPolicy.Complete(state, batch);
                EncyclopediaHeroPersonaPatch.QueueRefreshForHero(id);
                Logger.Log("NpcPersonaEvolution", "[COMPLETE] full regeneration hero=" + id + " facts=" + batch.Length);
            }
        };
        _personaEvolutionNextStartUtc = DateTime.UtcNow.AddSeconds(30);
        state.Attempts++;
        _personaEvolutionRequest = CompletePersonaEvolutionAsync(id, profile, snapshot, state, batch,
            NpcPersonaGenerationApplication.RegenerateForEventAsync(hero, trigger), () => saved, epoch, generation);
    }
    private async Task CompletePersonaEvolutionAsync(string id, NpcPersonaProfile original, JObject snapshot,
        PersonaEvolutionState state, PersonaEvolutionFact[] batch, Task<string> request, Func<bool> wasSaved, int epoch, long generation)
    {
        try { await request.ConfigureAwait(false); }
        catch (Exception ex) { Logger.Log("NpcPersonaEvolution", "[WARN] regeneration failed: " + ex.Message); }
        await RunMemorySummaryCompletionAsync(generation, () =>
        {
            if (epoch != _personaEvolutionEpoch || !PersonaEvolutionEnabled || wasSaved()) return true;
            var current = _personaProfiles.Get(id, false);
            var hero = Hero.Find(id);
            if (hero == null || !hero.IsAlive || !ReferenceEquals(current?.Evolution, state)) return true;
            if (!ReferenceEquals(current, original) || !PersonaEvolutionPolicy.IsCurrent(current, snapshot))
            {
                // User edits win; do not reapply the same old event to the edited persona.
                PersonaEvolutionPolicy.Complete(state, batch);
                Logger.Log("NpcPersonaEvolution", "[SKIP] profile changed during regeneration: " + id);
                return true;
            }
            Logger.Log("NpcPersonaEvolution", "[WARN] regeneration not saved hero=" + id + " attempt=" + state.Attempts);
            if (state.Attempts >= 2) PersonaEvolutionPolicy.Complete(state, batch);
            _personaEvolutionNextStartUtc = DateTime.UtcNow.AddSeconds(60);
            return true;
        }).ConfigureAwait(false);
    }
}
