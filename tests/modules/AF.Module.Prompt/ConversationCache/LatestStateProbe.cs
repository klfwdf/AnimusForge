using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

// Actual compiled channel composers -> shared cache store -> final wire adapter.
// Reflection only crosses internal visibility; no source projection or game state.
internal static class LatestStateProbe
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
    static object Begin(object store, IList selected, string channel)
    {
        IList source = List(T("PromptCacheMessage"));
        foreach (object message in selected)
            source.Add(Activator.CreateInstance(T("PromptCacheMessage"), Flags, null,
                new object[] { Part(message, "role"), Part(message, "content"), 0, "" }, null));
        object policy = Make("PromptCacheText"); Set(policy, "Policy", "fixture-edition");
        return Invoke(store, "Begin", 1L, channel, "Alda", source, "GLOBAL_SYSTEM", policy, 150000, 10,
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
