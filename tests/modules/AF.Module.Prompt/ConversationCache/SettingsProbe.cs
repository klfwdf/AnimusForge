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
        var courierArguments = new object[] { courier, duel, "https://relay.test/v1/chat/completions", "plain-model", 12000, false, null, true };
        var completeCourier = (JObject)af.GetType("AnimusForge.ShoutNetwork", true).GetMethod("BuildPrimaryChatPayload", All)
            .Invoke(null, courierArguments);
        string courierJson = completeCourier.ToString();
        Check(completeCourier["messages"].Count() >= courier.Count && courierJson.Contains("AMBER_PASSWORD_NORTH_GATE")
            && courierJson.Contains("dialogue-299"), "actual courier preserves all selected history above final character target");
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
        var boundaryInput = new System.Collections.Generic.List<object> { RequiredMessage("system", new string('规', 50000), 0),
            RequiredMessage("user", "Buy noble prisoner Leosios for 2000.", 1),
            RequiredMessage("assistant", "Agreed. Pay 2000 and take Leosios.", 1),
            RequiredMessage("user", "[AFEF PLAYER_ACTION] Paid 2000.", 0), RequiredMessage("user", "CURRENT_KEEP", 0) };
        duel.GetType().GetProperty("ConversationContextEstimatedTokens").SetValue(duel, 16000);
        var projectedInput = (System.Collections.Generic.List<object>)af.GetType("AnimusForge.ShoutNetwork", true)
            .GetMethod("ApplyPlayerDisplayNameToOutgoingMessages", All).Invoke(null, new object[] { boundaryInput });
        Check((int)projectedInput[1].GetType().GetField("HistoryGroup", All).GetValue(projectedInput[1]) == 1,
            "actual display-name projection retains optional-history provenance");
        var boundaryArguments = new object[] { projectedInput, duel, "https://relay.test/v1/chat/completions", "plain-model", 64000, false, null, true };
        var boundaryPayload = (JObject)af.GetType("AnimusForge.ShoutNetwork", true).GetMethod("BuildPrimaryChatPayload", All).Invoke(null, boundaryArguments);
        Check(boundaryPayload["messages"].Count() == 5 && boundaryPayload["messages"][0]["content"].Value<string>().Length == 50000
            && boundaryPayload["messages"][2]["role"].Value<string>() == "assistant"
            && boundaryPayload["messages"][2]["content"].Value<string>().Contains("take Leosios")
            && boundaryPayload["messages"][3]["content"].Value<string>().Contains("Paid 2000")
            && boundaryPayload["messages"][4]["content"].Value<string>() == "CURRENT_KEEP"
            && boundaryInput.Count == 5 && boundaryPayload["max_tokens"].Value<int>() == 64000,
            "actual primary boundary preserves agreed deal, payment, roles and output above final target");
        var sentFloor = JObject.Parse((string)prepare.Invoke(null, new object[] { "https://relay.test/v1/chat/completions", boundaryPayload }));
        string sentText = sentFloor.ToString();
        Check(sentFloor["messages"][0]["content"].Value<string>().Length == 50000 && sentFloor["max_tokens"].Value<int>() == 64000
            && sentText.Contains("take Leosios") && sentText.Contains("Paid 2000") && sentText.Contains("CURRENT_KEEP"),
            "actual Gemini wire retains agreed transaction above final target after protocol conversion");
        // Exercise the actual product cache adapter, not just the store fixture. Both targets
        // are exceeded; stable system/prefix and dynamic dedup must still work on turn two.
        var cacheType = af.GetType("AnimusForge.ConversationPromptCache", true);
        var beginCache = cacheType.GetMethod("Begin", All);
        long generation = (long)af.GetType("AnimusForge.SaveRuntimeGuard", true).GetProperty("CurrentGeneration", All).GetValue(null);
        foreach (string channel in new[] { "native", "scene", "courier" })
        {
            cacheType.GetMethod("Clear", All).Invoke(null, null);
            var firstInput = new System.Collections.Generic.List<object>(boundaryInput);
            firstInput.Insert(1, RequiredMessage("user", "[State]\nUNCHANGED_CACHE_SENTINEL\nOLD_CACHE_STATE", 0));
            object firstCache = beginCache.Invoke(null, new object[] { firstInput, channel, generation, 123, "Alice" });
            var firstMessages = (System.Collections.Generic.List<object>)firstCache.GetType().GetField("Messages", All).GetValue(firstCache);
            var firstJson = JArray.FromObject(firstMessages);
            Check(firstJson.ToString().Contains("[AF_PROMPT_CACHE_V1]"), channel + " actual over-target request enters prefix cache");
            firstCache.GetType().GetMethod("Accept", All).Invoke(firstCache, new object[] { "CACHE_ACCEPTED_REPLY_SENTINEL", true });
            ((IDisposable)firstCache).Dispose();
            var secondInput = new System.Collections.Generic.List<object>(firstInput);
            secondInput[1] = RequiredMessage("user", "[State]\nUNCHANGED_CACHE_SENTINEL\nNEW_CACHE_STATE", 0);
            secondInput[secondInput.Count - 1] = RequiredMessage("user", "SECOND_TURN_INPUT", 0);
            object secondCache = beginCache.Invoke(null, new object[] { secondInput, channel, generation, 123, "Alice" });
            var secondMessages = (System.Collections.Generic.List<object>)secondCache.GetType().GetField("Messages", All).GetValue(secondCache);
            var secondJson = JArray.FromObject(secondMessages);
            string secondText = secondJson.ToString();
            Check(firstJson.Select((part, i) => JToken.DeepEquals(part, secondJson[i])).All(x => x)
                && secondText.Contains("CACHE_ACCEPTED_REPLY_SENTINEL")
                && secondText.Split(new[] { new string('规', 50000) }, StringSplitOptions.None).Length == 2,
                channel + " actual second turn retains exact prefix, one system copy and accepted reply above both targets");
            string tail = secondJson.Last["content"].Value<string>();
            Check(!tail.Contains("UNCHANGED_CACHE_SENTINEL") && tail.Contains("OLD_CACHE_STATE")
                && tail.Contains("NEW_CACHE_STATE") && tail.Contains("SECOND_TURN_INPUT"),
                channel + " actual dynamic dedup skips unchanged state and preserves additions/removals/current input");
            var cachedArguments = new object[] { secondMessages, duel, "https://relay.test/v1/chat/completions", "gemini-test", 64000, false, null, true };
            var cachedPayload = (JObject)af.GetType("AnimusForge.ShoutNetwork", true).GetMethod("BuildPrimaryChatPayload", All).Invoke(null, cachedArguments);
            string cachedWire = (string)prepare.Invoke(null, new object[] { "https://relay.test/v1/chat/completions", cachedPayload });
            Check(cachedWire.Contains("CACHE_ACCEPTED_REPLY_SENTINEL") && cachedWire.Contains("take Leosios")
                && cachedWire.Contains("Paid 2000") && cachedWire.Contains("SECOND_TURN_INPUT"),
                channel + " actual final Gemini wire retains cached reply, deal, payment and current input");
            ((IDisposable)secondCache).Dispose();
        }
        duel.GetType().GetProperty("ConversationContextEstimatedTokens").SetValue(duel, 128000);
        string json = (string)prepare.Invoke(null, new object[] { "https://api.anthropic.com/v1/messages", Payload(11000) });
        Check(JObject.Parse(json)["system"] is JArray && JObject.Parse(json)["messages"] is JArray,
            "actual final Anthropic protocol passes bounded request");
        duel.GetType().GetProperty("ConversationContextInputChars").SetValue(duel, 100000);
    }
}
