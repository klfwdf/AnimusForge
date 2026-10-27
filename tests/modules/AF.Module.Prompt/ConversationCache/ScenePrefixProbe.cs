using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

// Actual compiled channel composers -> shared cache store -> final wire adapter.
// Reflection only crosses internal visibility; no source projection or game state.
internal static class ScenePrefixProbe
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static Assembly af;
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + label); checks++; Console.WriteLine("PASS " + label); }
    static Type T(string name) => af.GetType("AnimusForge." + name, true);
    static object Make(string name) => Activator.CreateInstance(T(name), true);
    static object Call(Type type, string method, params object[] args) => type.GetMethod(method, Flags).Invoke(null, args);
    static object Invoke(object value, string method, params object[] args) => value.GetType().GetMethod(method, Flags).Invoke(value, args);
    static void Set(object value, string field, object text) => value.GetType().GetField(field, Flags).SetValue(value, text);
    static object Get(object value, string field) => value.GetType().GetField(field, Flags).GetValue(value);
    static string Part(object value, string field) => (string)value.GetType().GetProperty(field).GetValue(value);
    static IList List(Type item) => (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(item));
    static object Speech(string role, string text, long sequence)
    {
        object value = Make("ConversationMessage");
        foreach (var pair in new Dictionary<string, object> { { "Role", role }, { "Content", text }, { "EventSequence", sequence },
            { "SpeakerName", role == "assistant" ? "Alda" : "Player" }, { "SpeakerAgentIndex", role == "assistant" ? 7 : -1 },
            { "SpeakerHeroId", role == "assistant" ? "alda" : "player" }, { "TargetAgentIndex", 7 }, { "GameDate", "Day 1" }, { "GameHour", 12 }, { "Scene", "town" } })
            value.GetType().GetProperty(pair.Key).SetValue(value, pair.Value);
        return value;
    }
    static IList Scene(IList history, string state, string current)
    {
        object input = Make("SceneHistoryMessageAssemblyInput"), context = Make("SceneHistoryMessageContext");
        Set(context, "ViewerAgentIndex", 7); Set(context, "ViewerHeroId", "alda"); Set(context, "PlayerName", "Player");
        Set(context, "GameDate", "Day 1"); Set(context, "GameHour", 12); Set(context, "Scene", "town");
        Set(input, "SystemPrompt", "GLOBAL_SYSTEM"); Set(input, "Context", context); Set(input, "PersistentHistory", history);
        Set(input, "PrefixUserSections", new[] { state }); Set(input, "SuffixUserSections", new[] { "CURRENT_RULES" });
        Set(input, "CurrentInputMessage", Speech("user", current, 100)); Set(input, "HistoryLineLimit", 30);
        return (IList)Call(T("SceneHistoryMessageAssemblyOwner"), "Build", input);
    }
    static object Begin(object store, IList selected, string channel, string actor = "Alda")
    {
        IList source = List(T("PromptCacheMessage"));
        foreach (object message in selected)
            source.Add(Activator.CreateInstance(T("PromptCacheMessage"), Flags, null,
                new object[] { Part(message, "role"), Part(message, "content"), 0, "" }, null));
        object policy = Make("PromptCacheText"); Set(policy, "Policy", "fixture-edition");
        return Invoke(store, "Begin", 1L, channel + "|" + actor, actor, source, "GLOBAL_SYSTEM", policy, 150000, 10,
            new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc), channel == "scene");
    }
    static string Wire(object lease)
    {
        var messages = new List<object>();
        foreach (object message in (IEnumerable)Get(lease, "Messages"))
            messages.Add(new { role = (string)Get(message, "Role"), content = (string)Get(message, "Content") });
        var json = Newtonsoft.Json.Linq.JObject.FromObject(new { model = "gemini-test", max_tokens = 12000, messages });
        return (string)Call(T("LlmApiCompat"), "PrepareChatRequestJson", "https://relay.test/v1/chat/completions", json);
    }
    static IList StableScene(string actor, string profile, string action, string gold, string global, IList history)
    {
        object input = Make("SceneHistoryMessageAssemblyInput"), context = Make("SceneHistoryMessageContext");
        Set(context, "ViewerAgentIndex", 7); Set(context, "ViewerHeroId", actor); Set(context, "PlayerName", "Player");
        Set(context, "GameDate", "Day 1"); Set(context, "GameHour", 12); Set(context, "Scene", "town");
        string system = (string)Call(T("MainPromptMessageAssemblyOwner"), "BuildSceneSharedSystemPrompt", global);
        Set(input, "SystemPrompt", system); Set(input, "Context", context); Set(input, "PersistentHistory", history);
        Set(input, "StablePersona", profile); Set(input, "CurrentInstructions", action + " TASK_" + actor);
        Set(input, "PrefixUserSections", new[] { gold + " PRIVATE_" + actor, "KNOWLEDGE_CURRENT" });
        Set(input, "SuffixUserSections", new[] { "RELAY_CURRENT" });
        Set(input, "CurrentInputMessage", Speech("user", "CURRENT_INPUT", 100)); Set(input, "HistoryLineLimit", 30);
        return (IList)Call(T("SceneHistoryMessageAssemblyOwner"), "Build", input);
    }
    static string Text(IList messages, int index) => Part(messages[index], "content");
    static string All(IList messages) => string.Join("\n", messages.Cast<object>().Select(x => Part(x, "content")));
    static void ScenePrefixes()
    {
        var history = List(T("ConversationMessage"));
        history.Add(Speech("user", "QUOTED_OLD_GOLD=1000", 1));
        history.Add(Speech("assistant", "HISTORY_REPLY", 2));
        history.Add(Speech("system", "[AFEF PLAYER_ACTION] HISTORY_FACT paid 2000", 3));
        string global = "GLOBAL_SYSTEM\n" + new string('G', 10000);
        IList alice = StableScene("Alda", "PERSONA_ALDA {TEXT} literal", "OLD_ACTION", "OLD_RUNTIME_GOLD=1000", global, history);
        IList next = StableScene("Alda", "PERSONA_ALDA {TEXT} literal", "NEW_ACTION", "NEW_RUNTIME_GOLD=3000", global, history);
        IList bob = StableScene("Bob", "PERSONA_BOB", "BOB_ACTION", "BOB_RUNTIME", global, List(T("ConversationMessage")));
        Check(Part(alice[0], "role") == "system" && Text(alice, 0) == Text(next, 0) && Text(next, 0) == Text(bob, 0), "public system byte-identical across speakers and runtime changes");
        Check(!Text(alice, 0).Contains("PERSONA_") && !Text(alice, 0).Contains("OLD_ACTION") && !Text(alice, 0).Contains("TASK_"), "public system excludes persona, action and per-turn task");
        Check(Part(alice[1], "role") == "user" && Text(alice, 1) == Text(next, 1) && Text(alice, 1).Contains("{TEXT} literal"), "stable persona precedes history and preserves literal placeholder text");
        Check(Text(alice, 2).Contains("QUOTED_OLD_GOLD") && Part(alice[3], "role") == "assistant", "history follows persona with original user/assistant roles");
        Check(All(next).Contains("HISTORY_FACT") && All(next).Contains("NEW_ACTION") && All(next).Contains("NEW_RUNTIME_GOLD=3000") && !All(next).Contains("OLD_ACTION") && !All(next).Contains("OLD_RUNTIME_GOLD"), "latest-only action/state with original historical facts and quotation");
        Check(!All(bob).Contains("PERSONA_ALDA") && !All(bob).Contains("PRIVATE_Alda") && !All(bob).Contains("HISTORY_FACT"), "speaker request contains no previous actor private profile or history");
        Check(Text(next, next.Count - 1).Contains("CURRENT_INPUT"), "latest input remains last");
        var store = Make("ConversationPromptCacheStore");
        object first = Begin(store, alice, "scene");
        Invoke(first, "Commit", "ACCEPTED", true, new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc));
        object second = Begin(store, next, "scene");
        object[] before = ((IEnumerable)Get(first, "Messages")).Cast<object>().ToArray();
        object[] after = ((IEnumerable)Get(second, "Messages")).Cast<object>().ToArray();
        Check(ReferenceEquals(before[0], after[0]), "store reuses identical frozen system object");
        Check((int)Get(second, "SharedChars") >= Text(alice, 0).Length + Text(alice, 1).Length, "shared prefix includes fixed system and current persona");
        foreach (string model in new[] { "gemini-test", "deepseek-test", "claude-test" })
        {
            Newtonsoft.Json.Linq.JObject Payload(object lease) => new Newtonsoft.Json.Linq.JObject {
                ["model"] = model, ["max_tokens"] = 12000, ["stream"] = true,
                ["messages"] = new Newtonsoft.Json.Linq.JArray(((IEnumerable)Get(lease, "Messages")).Cast<object>().Select(x => new Newtonsoft.Json.Linq.JObject { ["role"] = (string)Get(x, "Role"), ["content"] = (string)Get(x, "Content") })) };
            string Serialize(object lease) => (string)Call(T("LlmApiCompat"), "PrepareChatRequestJson", "https://relay.test/v1/chat/completions", Payload(lease));
            object other = Begin(store, bob, "scene", "Bob");
            var left = Newtonsoft.Json.Linq.JObject.Parse(Serialize(second)); var right = Newtonsoft.Json.Linq.JObject.Parse(Serialize(other));
            Check(Newtonsoft.Json.Linq.JToken.DeepEquals(left["messages"][0], right["messages"][0]), model + " final HTTP system identical across NPCs");
            Check(!left.ToString().Contains("OLD_ACTION") && !left.ToString().Contains("OLD_RUNTIME_GOLD") && left.ToString().Contains("QUOTED_OLD_GOLD"), model + " final wire retains historical quotation but no stale runtime");
            Check(left["stream_options"]?["include_usage"]?.Value<bool>() == true, model + " requests stream usage");
            if (model == "gemini-test") Check(!left.ToString().Contains("cache_control"), "Gemini has no foreign cache controls");
            ((IDisposable)other).Dispose();
        }
        IList edited = StableScene("Alda", "EDITED_PROFILE", "EDITED_ACTION", "EDITED_STATE", global, history);
        object refreshed = Begin(store, edited, "scene");
        string refreshWire = Wire(refreshed);
        Check(refreshWire.Contains("EDITED_PROFILE") && !refreshWire.Contains("PERSONA_ALDA"), "profile edits replace old persona instead of freezing it indefinitely");
        IList newRules = StableScene("Alda", "EDITED_PROFILE", "EDITED_ACTION", "EDITED_STATE", global + " NEW_CUSTOM_RULE", history);
        object ruleRefresh = Begin(store, newRules, "scene");
        Check(Wire(ruleRefresh).Contains("NEW_CUSTOM_RULE"), "custom rule edits invalidate fixed system by content");
        IList removed = StableScene("Alda", "EDITED_PROFILE", "EDITED_ACTION", "EDITED_STATE", global, List(T("ConversationMessage")));
        object pruned = Begin(store, removed, "scene");
        Check(!Wire(pruned).Contains("HISTORY_FACT") && !Wire(pruned).Contains("QUOTED_OLD_GOLD"), "edited or compressed history is never restored from old transport snapshots");
        IList blank = StableScene("Alda", "", "CURRENT_ACTION", "CURRENT_STATE", "", history);
        Check(!string.IsNullOrWhiteSpace(Text(blank, 0)) && All(blank).Contains("CURRENT_ACTION"), "empty custom rules and missing persona retain scene instructions and current state");
    }
    static int Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = new System.Text.UTF8Encoding(false);
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
                string name = new AssemblyName(e.Name).Name;
                if (name == "AnimusForge") return Assembly.LoadFrom(args[0]);
                foreach (string dir in args.Skip(2)) { string path = Path.Combine(dir, name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
                return null;
            };
            af = Assembly.LoadFrom(args[0]);
            ScenePrefixes();
            var history = List(T("ConversationMessage"));
            history.Add(Speech("user", "HISTORY_QUESTION GOLD=1000 is a quotation", 1));
            history.Add(Speech("assistant", "HISTORY_REPLY pay 2000 for prisoner", 2));
            history.Add(Speech("system", "[AFEF PLAYER_ACTION] HISTORY_FACT paid 2000", 3));
            string background = "BACKGROUND_BEGIN_" + new string('B', 5400) + "_END";
            foreach (string channel in new[] { "native", "scene", "courier", "inbound" })
            {
                IList Compose(string state) => channel == "native" || channel == "scene" ? Scene(history, state, "CURRENT_INPUT")
                    : (IList)Call(T("MainPromptMessageAssemblyOwner"), channel == "courier" ? "BuildCourierReplyMessages" : "BuildInboundNpcLetterMessages",
                        channel == "courier"
                        ? new object[] { "Alda", "Player", "CURRENT_INPUT", state, "DELIVERY_FACT", "RECALL", history, "PERSONA", "EXCLUDE", "RECENT_FACTS", "IDENTITY", "RELATION", "LOCATION", "DATE", "GLOBAL_SYSTEM" }
                        : new object[] { "Alda", "Player", "CURRENT_INPUT", state, "DELIVERY_FACT", "RECALL", history, "PERSONA", "EXCLUDE", "RECENT_FACTS", "IDENTITY", "RELATION", "LOCATION", "DATE", 220, "private letter", "GLOBAL_SYSTEM" });
                object store = Make("ConversationPromptCacheStore");
                IList old = Compose("OLD_RUNTIME GOLD=1000\n" + background);
                IList latest = Compose("NEW_RUNTIME GOLD=3000\n" + background);
                Check(Part(latest[0], "role") == "system" && Part(latest[1], "content").Contains("HISTORY_QUESTION")
                    && Part(latest[2], "role") == "assistant" && Part(latest[2], "content").Contains("HISTORY_REPLY"), channel + " actual history first with original roles");
                int fact = Enumerable.Range(0, latest.Count).Single(i => Part(latest[i], "content").Contains("HISTORY_FACT"));
                int state = Enumerable.Range(0, latest.Count).Single(i => Part(latest[i], "content").Contains("NEW_RUNTIME"));
                Check(fact < state && Part(latest[latest.Count - 1], "content").Contains("CURRENT_INPUT"), channel + " historical facts then latest context then current input");
                object first = Begin(store, old, channel);
                Invoke(first, "Commit", "ACCEPTED", true, new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc));
                object next = Begin(store, latest, channel);
                string wire = Wire(next);
                Check(!wire.Contains("OLD_RUNTIME") && wire.Contains("NEW_RUNTIME GOLD=3000")
                    && wire.Split(new[] { background }, StringSplitOptions.None).Length == 2, channel + " real final wire has latest state and one background");
                Check(wire.Contains("HISTORY_QUESTION GOLD=1000") && wire.Contains("HISTORY_REPLY") && wire.Contains("HISTORY_FACT"), channel + " real final wire preserves quoted old value and AFEF history");
                var firstMessages = ((IEnumerable)Get(first, "Messages")).Cast<object>().ToArray();
                var nextMessages = ((IEnumerable)Get(next, "Messages")).Cast<object>().ToArray();
                Check(Enumerable.Range(0, state).All(i => (string)Get(firstMessages[i], "Content") == (string)Get(nextMessages[i], "Content")
                    && (string)Get(firstMessages[i], "Role") == (string)Get(nextMessages[i], "Role")), channel + " system and whole history prefix unchanged by runtime replacement");
                string removed = (string)Call(T("PromptCacheCoreTexts"), "Get", "Removed");
                Check(!wire.Contains(removed.Trim()), channel + " no generated removal manifest");
            }
            Console.WriteLine("Checks=" + checks + "; actual compiled request owners/cache/wire; no game/provider/save"); return 0;
        }
        catch (Exception error) { Console.WriteLine(error); return 1; }
    }
}
