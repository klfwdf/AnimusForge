using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using BUTR.DependencyInjection;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using Newtonsoft.Json.Linq;

// Reuses the existing managed MCM provider fixture; all properties, metadata,
// terminal delegates and the JSON formatter belong to the built product DLL.
internal static class PromptCacheSettingsProbe
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static int _checks;
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception("FAIL " + name); _checks++; Console.WriteLine("PASS " + name); }
    private static object Get(object o, string name) => o.GetType().GetProperty(name).GetValue(o);
    private static int Main(string[] args)
    {
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
            {
                string name = new AssemblyName(e.Name).Name;
                if (name == "AnimusForge") return Assembly.LoadFrom(args[0]);
                foreach (string dir in args.Skip(2))
                { string path = Path.Combine(dir, name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
                return null;
            };
            var af = Assembly.LoadFrom(args[0]);
            typeof(GenericServiceProvider).GetField("GlobalServiceProvider", All).SetValue(null, new Discovery());
            var provider = typeof(BaseSettingsProvider);
            provider.GetField("<Instance>k__BackingField", All).SetValue(null,
                typeof(TerminalSettingsChecks).GetMethod("BuildProvider", All).Invoke(null, new object[] { provider }));
            foreach (string name in new[] { "AnimusForge.DuelSettings", "AnimusForge.DialogueUI.DialogueUiSettings",
                "AnimusForge.Illustrator.IllustratorSettings", "AnimusForge.CoupSystem.CoupSettings" })
            {
                var owner = (BaseSettings)Activator.CreateInstance(af.GetType(name, true));
                TerminalProviderProbe.Owners.Add(owner.Id, owner);
            }
            var duel = (BaseSettings)TerminalProviderProbe.Owners["AnimusForge_global_settings"];
            var definitions = ((IEnumerable)af.GetType("AnimusForge.TerminalSettingsRegistry", true)
                .GetProperty("AllDefinitions").GetValue(null)).Cast<object>().ToDictionary(x => (string)Get(x, "Id"));
            var native = duel.GetAllSettingPropertyDefinitions().Select(x => x.Id).ToArray();
            var formatterType = typeof(BaseSettings).Assembly.GetType("MCM.Implementation.JsonSettingsFormat", true);
            var logger = typeof(BaseSettings).Assembly.GetType("BUTR.DependencyInjection.Logger.DefaultBUTRLogger`1", true).MakeGenericType(formatterType);
            var formatter = Activator.CreateInstance(formatterType, Activator.CreateInstance(logger));
            string Save() => (string)formatterType.GetMethod("SaveJson").Invoke(formatter, new object[] { duel });
            string[] retiredIds = { "ConversationPromptCacheEnabled", "ConversationPromptCacheMaxChars", "ConversationPromptCacheTtlMinutes" };
            foreach (string id in retiredIds)
                Check(!native.Contains(id) && !definitions.ContainsKey(id) && duel.GetType().GetProperty(id)==null,
                    "retired cache setting absent from owner, native MCM and terminal " + id);
            var legacy = JObject.Parse(Save());
            legacy[retiredIds[0]]=false; legacy[retiredIds[1]]=20000; legacy[retiredIds[2]]=1;
            string legacyPath = Path.Combine(args[1], "legacy-cache-settings.json");
            File.WriteAllText(legacyPath,legacy.ToString());
            var oldLoaded = (BaseSettings)Activator.CreateInstance(duel.GetType());
            formatterType.GetMethod("LoadFromJson").Invoke(formatter,new object[]{oldLoaded,File.ReadAllText(legacyPath)});
            var resaved=JObject.Parse((string)formatterType.GetMethod("SaveJson").Invoke(formatter,new object[]{oldLoaded}));
            Check(!resaved.Descendants().OfType<JProperty>().Any(p=>retiredIds.Contains(p.Name)),"actual MCM formatter ignores and does not resave retired cache fields");
            Type core=af.GetType("AnimusForge.PromptCacheSettings",true);
            object cache=core.GetField("Instance",All).GetValue(null);
            Check((int)core.GetField("MaxTranscriptCharacters",All).GetRawConstantValue()==150000
                && (int)core.GetProperty("SessionTtlMinutes",All).GetValue(cache)==10,"built-in capacity and TTL ignore old JSON values");
            foreach(string name in new[]{"EnableHierarchicalPromptSplit","EnableSceneCardSplit","EnableDynamicBlockDedup"})
                Check((bool)core.GetProperty(name,All).GetValue(cache),"built-in cache core remains enabled: "+name);
            foreach (var item in new[]
            {
                (Id: "ConversationContextEnabled", Default: (object)true, Changed: (object)false, Min: 0f, Max: 0f),
                (Id: "ConversationContextRecentTurns", Default: (object)12, Changed: (object)7, Min: 2f, Max: 50f),
                (Id: "ConversationContextRecentChars", Default: (object)20000, Changed: (object)9000, Min: 2000f, Max: 100000f),
                (Id: "ConversationContextInputChars", Default: (object)100000, Changed: (object)80000, Min: 20000f, Max: 1000000f),
                (Id: "ConversationContextEstimatedTokens", Default: (object)128000, Changed: (object)64000, Min: 16000f, Max: 1000000f)
            })
            {
                Check(native.Contains(item.Id), "native MCM discovers " + item.Id);
                Check(definitions.ContainsKey(item.Id), "terminal discovers " + item.Id);
                object definition = definitions[item.Id];
                Check(Equals(Get(duel, item.Id), item.Default), "original owner default " + item.Id);
                Check((string)Get(definition, "TabCategory") == (string)Get(definitions["MainApiMaxTokens"], "TabCategory"), "AI terminal category " + item.Id);
                Check(!string.IsNullOrWhiteSpace((string)Get(definition, "DisplayName"))
                    && !string.IsNullOrWhiteSpace((string)Get(definition, "HintText")), "real terminal name and hint " + item.Id);
                if (item.Changed is int)
                    Check((float)Get(definition, "MinValue") == item.Min && (float)Get(definition, "MaxValue") == item.Max, "terminal range " + item.Id);
                ((Delegate)Get(definition, "Setter")).DynamicInvoke(duel, item.Changed);
                Check(Equals(Get(duel, item.Id), item.Changed), "terminal invokes original setter " + item.Id);
                string path = Path.Combine(args[1], item.Id + ".json"); File.WriteAllText(path, Save());
                var loaded = (BaseSettings)Activator.CreateInstance(duel.GetType());
                formatterType.GetMethod("LoadFromJson").Invoke(formatter, new object[] { loaded, File.ReadAllText(path) });
                Check(Equals(Get(loaded, item.Id), item.Changed), "actual MCM JSON disk roundtrip " + item.Id);
                ((Delegate)Get(definition, "Setter")).DynamicInvoke(duel, item.Default);
                Check(Equals(Get(duel, item.Id), item.Default), "terminal restores original owner " + item.Id);
            }
            CheckActualContext(af, duel);
            Console.WriteLine("Checks=" + _checks + "; actual DLL and MCM formatter; no live game/native rendering/HTTP");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }

    private static void CheckActualContext(Assembly af, BaseSettings duel)
    {
        object Make(string name) => Activator.CreateInstance(af.GetType("AnimusForge." + name, true), true);
        void Set(object owner, string field, object value) => owner.GetType().GetField(field, All).SetValue(owner, value);
        var messageType = af.GetType("AnimusForge.ConversationMessage", true);
        var list = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(messageType));
        for (int turn = 0; turn < 300; turn++)
        {
            foreach (string role in new[] { "user", "assistant" })
            {
                object message = Activator.CreateInstance(messageType);
                messageType.GetProperty("Role").SetValue(message, role);
                messageType.GetProperty("Content").SetValue(message, turn == 0 ? "AMBER_PASSWORD_NORTH_GATE" : "dialogue-" + turn + " " + new string('x', 500));
                messageType.GetProperty("SpeakerName").SetValue(message, role == "user" ? "Player" : "Alice");
                messageType.GetProperty("SpeakerAgentIndex").SetValue(message, role == "user" ? -1 : 41);
                messageType.GetProperty("TargetAgentIndex").SetValue(message, 41);
                messageType.GetProperty("GameDayIndex").SetValue(message, 17);
                if (turn == 0) Set(message, "PromptIsRecall", true);
                Set(message, "PromptContextRevision", "intraday-revision-1");
                list.Add(message);
            }
        }
        object context = Make("SceneHistoryMessageContext");
        Set(context, "ViewerAgentIndex", 41); Set(context, "PlayerName", "Player");
        Set(context, "GameDate", "day17"); Set(context, "Scene", "town");
        object options = Make("ConversationContextOptions");
        object input = Make("SceneHistoryMessageAssemblyInput");
        Set(input, "SystemPrompt", "SYSTEM"); Set(input, "Context", context);
        Set(input, "PersistentHistory", list); Set(input, "HistoryLineLimit", 100); Set(input, "WindowOptions", options);
        var built = ((IEnumerable)af.GetType("AnimusForge.SceneHistoryMessageAssemblyOwner", true).GetMethod("Build", All)
            .Invoke(null, new[] { input })).Cast<object>().ToList();
        Check(built.Count <= 32, "actual scene/native shared builder bounds 600 source messages");
        Check(built.Any(x => (string)Get(x, "role") == "assistant" && ((string)Get(x, "content")).Contains("AMBER_PASSWORD_NORTH_GATE")),
            "actual builder preserves recalled early assistant speech beyond the 100-line window");
        Check((string)built[0].GetType().GetField("Revision", All).GetValue(built[0]) == "intraday-revision-1",
            "actual builder forwards summary revision to actor cache");
        Check(JObject.FromObject(built[0]).Properties().Select(x => x.Name).SequenceEqual(new[] { "role", "content" }),
            "request provenance never appears in provider JSON");
        Check(list.Count == 600, "actual builder leaves original history intact");

        var capture = af.GetType("AnimusForge.ConversationContextSettingsCapture", true);
        var prepare = capture.GetMethod("PreparePrimaryJson", All);
        duel.GetType().GetProperty("ConversationContextInputChars").SetValue(duel, 20000);
        var courier = new System.Collections.Generic.List<object>();
        af.GetType("AnimusForge.MainPromptMessageAssemblyOwner", true).GetMethod("AppendCourierPersistentMemoryRoleMessages", All)
            .Invoke(null, new object[] { courier, list, "Alice", "Player" });
        var boundedCourier = (System.Collections.Generic.List<object>)capture.GetMethod("PrepareOptionalHistory", All)
            .Invoke(null, new object[] { courier, 12000 });
        Check(boundedCourier.Count < courier.Count && ((string)Get(boundedCourier.Last(), "content")).Contains("dialogue-299"),
            "actual courier shares whole-turn budget reduction and preserves latest speech");
        const string cacheSystem = "[AF_PROMPT_CACHE_V1]\n";
        JObject Payload(int totalCharacters) => new JObject { ["model"] = "gemini-test", ["max_tokens"] = 1000,
            ["messages"] = new JArray(new JObject { ["role"] = "system", ["content"] = cacheSystem },
                new JObject { ["role"] = "user", ["content"] = new string('a', 9999) },
                new JObject { ["role"] = "user", ["content"] = new string('b', totalCharacters - cacheSystem.Length - 9999) }) };
        string overTargetJson = (string)prepare.Invoke(null, new object[] { "https://relay.test/v1/chat/completions", Payload(20000) });
        var overTargetPayload = JObject.Parse(overTargetJson);
        Check(overTargetPayload["messages"].Count() == 2 && overTargetPayload["messages"][1]["content"].Value<string>().Length == 20000 - cacheSystem.Length + 2,
            "actual Gemini protocol separators continue above management target without cutting required input");
        var requiredOnly = Payload(30000);
        string requiredJson = (string)prepare.Invoke(null, new object[] { "https://api.anthropic.com/v1/messages", requiredOnly });
        Check(JObject.Parse(requiredJson)["messages"][0]["content"].ToString().Contains(new string('b', 1000)),
            "actual required-only oversized Anthropic input continues without local budget exception");

        object RequiredMessage(string role, string content, int group) => Activator.CreateInstance(
            af.GetType("AnimusForge.ConversationPromptMessage", true), All, null, new object[] { role, content, group, "" }, null);
        var boundaryInput = new System.Collections.Generic.List<object> { RequiredMessage("system", new string('s', 30000), 0),
            RequiredMessage("user", "OLD_OMIT", 1), RequiredMessage("assistant", "OLD_REPLY_OMIT", 1), RequiredMessage("user", "CURRENT_KEEP", 0) };
        duel.GetType().GetProperty("ConversationContextEstimatedTokens").SetValue(duel, 16000);
        var projectedInput = (System.Collections.Generic.List<object>)af.GetType("AnimusForge.ShoutNetwork", true)
            .GetMethod("ApplyPlayerDisplayNameToOutgoingMessages", All).Invoke(null, new object[] { boundaryInput });
        Check((int)projectedInput[1].GetType().GetField("HistoryGroup", All).GetValue(projectedInput[1]) == 1,
            "actual display-name projection retains optional-history provenance");
        var boundaryArguments = new object[] { projectedInput, duel, "https://relay.test/v1/chat/completions", "plain-model", 12000, false, null, true };
        var boundaryPayload = (JObject)af.GetType("AnimusForge.ShoutNetwork", true).GetMethod("BuildPrimaryChatPayload", All).Invoke(null, boundaryArguments);
        Check(boundaryPayload["messages"].Count() == 2 && boundaryPayload["messages"][0]["content"].Value<string>().Length == 30000
            && boundaryPayload["messages"][1]["content"].Value<string>() == "CURRENT_KEEP" && boundaryInput.Count == 4 && boundaryPayload["max_tokens"].Value<int>() == 12000,
            "actual primary boundary drops optional turns then preserves required floor and source");
        var sentFloor = JObject.Parse((string)prepare.Invoke(null, new object[] { "https://relay.test/v1/chat/completions", boundaryPayload }));
        Check(sentFloor["messages"][0]["content"].Value<string>().Length == 30000 && sentFloor["max_tokens"].Value<int>() == 12000,
            "actual token target overflow continues with required content and configured output allowance");
        duel.GetType().GetProperty("ConversationContextEstimatedTokens").SetValue(duel, 128000);
        string json = (string)prepare.Invoke(null, new object[] { "https://api.anthropic.com/v1/messages", Payload(11000) });
        Check(JObject.Parse(json)["system"] is JArray && JObject.Parse(json)["messages"] is JArray,
            "actual final Anthropic protocol passes bounded request");
        duel.GetType().GetProperty("ConversationContextInputChars").SetValue(duel, 100000);
    }
}
