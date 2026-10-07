using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

public enum TownObservedRemoval { Left, Unconscious, Killed }

public sealed class TownPerceivedPerson
{
    public TownPerceivedPerson(string key, string name) { Key = key ?? ""; Name = Clean(name); }
    public string Key { get; }
    public string Name { get; }
    private static string Clean(string value)
    {
        string text = (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        return text.Length == 0 ? "一名附近人物" : text.Substring(0, Math.Min(48, text.Length));
    }
}

/// <summary>Scene-local, observer-specific facts only. No combat, rewards, or persistent memory.</summary>
public sealed class TownScenePerceptionSession
{
    public const float ObservationRadius = 24f;
    public const int MaximumListedPeople = 8;
    public const int MaximumRecentEvents = 6;
    private sealed class Observation
    {
        internal bool Initialized;
        internal Dictionary<string, TownPerceivedPerson> Visible = new Dictionary<string, TownPerceivedPerson>();
        internal readonly Queue<PerceptionEvent> Events = new Queue<PerceptionEvent>();
        internal readonly Dictionary<string, TownObservedRemoval> Removals = new Dictionary<string, TownObservedRemoval>();
        internal int WitnessedDeaths;
    }
    private sealed class PerceptionEvent
    {
        internal string Kind, Name, Cause;
    }
    private readonly object _sync = new object();
    private readonly Dictionary<string, Observation> _observers = new Dictionary<string, Observation>();
    private string _settlementId = "";

    public void Begin(string settlementId)
    {
        lock (_sync) { _observers.Clear(); _settlementId = (settlementId ?? "").Trim(); }
    }
    public void EndScene() { Begin(""); }
    public bool IsActiveFor(string settlementId)
    {
        lock (_sync) return _settlementId.Length > 0 && string.Equals(_settlementId, settlementId, StringComparison.Ordinal);
    }
    public void Observe(string settlementId, string observerKey, IEnumerable<TownPerceivedPerson> visible)
    {
        lock (_sync)
        {
            if (!IsActiveFor(settlementId) || string.IsNullOrEmpty(observerKey)) return;
            Observation state = GetObserver(observerKey);
            var current = new Dictionary<string, TownPerceivedPerson>();
            foreach (TownPerceivedPerson person in visible ?? Array.Empty<TownPerceivedPerson>())
            {
                if (person == null || person.Key.Length == 0 || person.Key == observerKey) continue;
                // A removed identity cannot become a living person again. A new spawn needs a new key.
                if (state.Removals.ContainsKey(person.Key)) continue;
                current[person.Key] = person;
            }
            if (state.Initialized)
            {
                foreach (TownPerceivedPerson old in state.Visible.Values)
                    if (!current.ContainsKey(old.Key)) Add(state, "gone", old.Name);
                foreach (TownPerceivedPerson added in current.Values)
                    if (!state.Visible.ContainsKey(added.Key)) Add(state, "arrived", added.Name);
            }
            state.Visible = current;
            state.Initialized = true;
        }
    }
    public void RecordRemoval(string settlementId, TownPerceivedPerson person, TownObservedRemoval removal,
        string observerKey, string visibleAttackerName)
    {
        lock (_sync)
        {
            if (!IsActiveFor(settlementId) || person == null || person.Key.Length == 0
                || string.IsNullOrEmpty(observerKey) || observerKey == person.Key) return;
            Observation state = GetObserver(observerKey);
            if (state.Removals.TryGetValue(person.Key, out TownObservedRemoval previous)
                && (previous == TownObservedRemoval.Killed || previous == removal
                    || removal == TownObservedRemoval.Left)) return;
            state.Removals[person.Key] = removal;
            state.Visible.Remove(person.Key);
            // Keep the nonliving identity guard, but announce only confirmed deaths, not unconsciousness.
            if (removal == TownObservedRemoval.Unconscious) return;
            if (removal == TownObservedRemoval.Killed) state.WitnessedDeaths++;
            Add(state, removal == TownObservedRemoval.Killed ? "killed" : "left", person.Name,
                string.IsNullOrWhiteSpace(visibleAttackerName) ? "" : new TownPerceivedPerson("", visibleAttackerName).Name);
        }
    }
    public void ForgetObserver(string observerKey) { lock (_sync) _observers.Remove(observerKey ?? ""); }
    public string BuildPrompt(string settlementId, string observerKey, bool ownedIncident, TownPromptTextCatalog catalog)
    {
        lock (_sync)
        {
            if (!IsActiveFor(settlementId) || !_observers.TryGetValue(observerKey ?? "", out Observation state)) return "";
            Dictionary<string, string> text = TownPromptTextCatalog.Resolve(catalog).ScenePerceptionTexts;
            var lines = new List<string> { text["heading"], text[ownedIncident ? "owned" : "occupied"] };
            if (state.Initialized)
                lines.Add(state.Visible.Count == 0 ? text["none"] : text["present"].Replace("{names}",
                    string.Join("、", state.Visible.Values.Take(MaximumListedPeople).Select(x => x.Name))));
            foreach (PerceptionEvent item in state.Events)
            {
                string cause = item.Cause.Length == 0 ? text["unknown_cause"] : text["cause"].Replace("{actor}", item.Cause);
                lines.Add(text[item.Kind].Replace("{name}", item.Name).Replace("{cause}", cause));
            }
            lines.Add(text["count"].Replace("{count}", state.WitnessedDeaths.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            lines.Add(text["instruction"]);
            return string.Join(Environment.NewLine, lines);
        }
    }
    private Observation GetObserver(string key)
    {
        if (!_observers.TryGetValue(key, out Observation state)) _observers[key] = state = new Observation();
        return state;
    }
    private static void Add(Observation state, string kind, string name, string cause = "")
    {
        state.Events.Enqueue(new PerceptionEvent { Kind = kind, Name = name, Cause = cause });
        while (state.Events.Count > MaximumRecentEvents) state.Events.Dequeue();
    }
}
