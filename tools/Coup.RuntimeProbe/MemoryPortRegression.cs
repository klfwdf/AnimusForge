using System;
using System.Collections;
using System.Reflection;

// Invoke the actual consumer's cached factory, not a separately fabricated DTO.
internal static class MemoryPortRegression
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    internal static void Run(Assembly af, Assembly coup, Action<string> write)
    {
        int count = 0;
        Action<bool, string> check = (ok, message) => {
            if (!ok) throw new InvalidOperationException("MEMORY_FAIL " + message);
            count++; write("MEMORY_PASS " + message);
        };
        Type bridge = coup.GetType("AnimusForge.CoupSystem.CoupRebellionBridge", true);
        object access = bridge.GetField("_af", All).GetValue(null)
            ?? throw new InvalidOperationException("Memory seam registration failed.");
        check(access.GetType().GetField("MemoryConstructor", All) == null, "consumer has no constructor-signature reflection");
        object memory = access.GetType().GetField("Memory", All).GetValue(access);
        Delegate create = (Delegate)memory.GetType().GetField("CreateCommit", All).GetValue(memory);
        object commit = create.DynamicInvoke("coup:probe", "probe_hero", "confirmed coup fact", 42, "probe_town");
        Func<object, string, object> get = (o, name) => o.GetType().GetProperty(name, All).GetValue(o);
        check((string)get(commit, "CommitId") == "coup:probe", "stable commit id");
        check((string)get(commit, "SessionId") == "coup:probe" && (string)get(commit, "TraceId") == "coup:probe", "session and trace identity preserved");
        check((string)get(commit, "SubjectId") == "probe_hero" && get(commit, "Channel").ToString() == "Domain", "domain and hero preserved");
        check((string)get(commit, "UserText") == "" && (string)get(commit, "AssistantText") == "", "no fabricated speech");
        check((int)get(commit, "GameDay") == 42 && (int)get(commit, "GameHour") == 0, "original coup day/default hour");
        check((string)get(commit, "LocationId") == "probe_town", "original settlement");
        check((long)get(commit, "RuntimeGeneration") == 0L && (long)get(commit, "SaveGeneration") == 0L, "legacy generation defaults");
        check((int)get(commit, "SceneSessionId") == -1 && (int)get(commit, "TargetAgentIndex") == -1 && (string)get(commit, "TargetName") == "", "scene defaults unchanged");
        check((string)get(commit, "CapturedGameDate") == "", "existing recovery date policy unchanged");
        var facts = ((IEnumerable)get(commit, "ConfirmedFacts")).GetEnumerator();
        check(facts.MoveNext(), "confirmed fact exists");
        object fact = facts.Current;
        check((string)get(fact, "FactType") == "coup_outcome" && (string)get(fact, "SubjectId") == "probe_hero" && (string)get(fact, "Text") == "confirmed coup fact", "only authoritative causal coup fact");
        check(!facts.MoveNext(), "no duplicated vanilla political facts");
        object retry = create.DynamicInvoke("coup:probe", "probe_hero", "confirmed coup fact", 42, "probe_town");
        check(!ReferenceEquals(commit, retry) && (string)get(retry, "CommitId") == (string)get(commit, "CommitId"), "retry returns independent payload with stable identity");
        foreach (string invalid in new[] { null, "", " " })
        {
            bool rejected = false;
            try { create.DynamicInvoke(invalid, "probe_hero", "fact", 42, "probe_town"); }
            catch (TargetInvocationException ex) { rejected = ex.InnerException is ArgumentException; }
            check(rejected, "invalid commit key rejected");
        }
        Delegate prepare = (Delegate)memory.GetType().GetField("Prepare", All).GetValue(memory);
        object[] args = { commit, "probe hero", null, null, null };
        check(!(bool)prepare.DynamicInvoke(args) && !string.IsNullOrWhiteSpace((string)args[4]), "missing live campaign fails closed at preparation");
        Delegate apply = (Delegate)memory.GetType().GetField("Commit", All).GetValue(memory);
        object result = apply.DynamicInvoke(commit, "probe hero");
        check(!(bool)get(result, "HistoryWritten"), "missing live campaign cannot report history success");
        Delegate status = (Delegate)memory.GetType().GetField("GetStatus", All).GetValue(memory);
        check((string)status.DynamicInvoke("probe", "probe_hero", "hash") == "Unavailable", "missing live campaign lookup is unavailable");
        write("PASS memory port regression assertions=" + count);
        write("MEMORY_SCOPE actual candidate factory and recovery admission; no Game/Campaign/save or LLM execution.");
    }
}
