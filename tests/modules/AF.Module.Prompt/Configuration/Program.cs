using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value, string label)
    {
        _checks++;
        if (!value) throw new Exception(label);
    }

    private sealed class MemoryFiles : IPromptConfigurationFiles
    {
        internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        internal readonly Dictionary<string, byte[]> Resources = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        internal readonly List<string> Logs = new List<string>();
        internal string BlockedPath;
        internal ManualResetEventSlim Entered;
        internal ManualResetEventSlim Release;
        public string PathFor(string name) => name;
        public bool Exists(string path) => Files.ContainsKey(path);
        public string ReadText(string path)
        {
            if (path == BlockedPath) { Entered.Set(); Release.Wait(); }
            return new StreamReader(new MemoryStream(Files[path]), Encoding.UTF8, true).ReadToEnd();
        }
        public string ReadText(string path, Encoding encoding) => new StreamReader(new MemoryStream(Files[path]), encoding, true).ReadToEnd();
        public byte[] ReadBytes(string path) => Files[path];
        public Stream OpenResource(string name) => Resources.TryGetValue(name, out byte[] value) ? new MemoryStream(value) : null;
        public void Log(string message) => Logs.Add(message);
        internal void Put(string name, string content) => Files[name] = Encoding.UTF8.GetBytes(content);
        internal void Resource(string name, string content) => Resources[name] = Encoding.UTF8.GetBytes(content);
    }

    private static string ValidPreprocess(int version)
    {
        return new JObject
        {
            ["Version"] = version,
            ["StrictJson"] = new JObject { ["SystemPrompt"] = "strict", ["MentionedEntitiesSchema"] = new JObject { ["entities"] = new JArray() } },
            ["TopicRouting"] = new JObject { ["EmptyValue"] = "none", ["UserPromptTemplate"] = "{topic_list}{routing_guidance}{history}{latest_npc}{latest_player}{top_n}{mentioned_entities_schema}" },
            ["MemorySelection"] = new JObject
            {
                ["ParallelModeInstruction"] = "parallel", ["UnifiedModeInstruction"] = "unified", ["EmptyValue"] = "none",
                ["UserPromptTemplate"] = "{mode_instruction}{final_count}{latest_player_input}{latest_npc_input}{current_scene}{memory_candidates}",
                ["CandidateLineTemplate"] = "{memory_id}{game_date}{age_suffix}{hour_range}{rich_title}",
                ["FallbackGameDateTemplate"] = "{game_day}"
            },
            ["ConnectionTest"] = new JObject { ["ExpectedRuleCode"] = "TEST", ["UserPromptTemplate"] = "{expected_rule_code}{mentioned_entities_schema}" }
        }.ToString();
    }

    private static MemoryFiles FullFiles()
    {
        var io = new MemoryFiles();
        io.Put("AIConfig.json", "{\"Marker\":\"main-user\"}");
        io.Put("RuleBehaviorPrompts.json", "{\"Marker\":\"rule-user\"}");
        io.Put("ActionPostprocessPrompts.json", "{\"Marker\":\"action-user\"}");
        io.Put("PreprocessPrompts.json", ValidPreprocess(2));
        io.Put("ProactiveNpcRequestPrompts.json", "{\"Marker\":\"proactive-user\"}");
        io.Put("RpItemIntroductionPrompts.json", "{\"Version\":1,\"SystemPrompt\":\"disk\",\"UserPromptTemplate\":\"{item_name}{dialogue}\"}");
        io.Resource("AnimusForge.Defaults.PreprocessPrompts.json", ValidPreprocess(2));
        io.Resource("AnimusForge.Defaults.RpItemIntroductionPrompts.json", "{\"Version\":1,\"SystemPrompt\":\"embedded\",\"UserPromptTemplate\":\"{item_name}{dialogue}\"}");
        return io;
    }

    private static void Main()
    {
        var io = FullFiles();
        var loader = new PromptConfigurationLoader(io);
        var normal = loader.Load();
        Check(normal.Snapshot.Main.Marker == "main-user" && normal.Snapshot.Guardrail.Marker == "rule-user", "normal main and rule files");
        Check(normal.Snapshot.ActionPostprocess.Marker == "action-user" && normal.Snapshot.ProactiveRequest.Marker == "proactive-user", "normal action and proactive files");
        Check(normal.Snapshot.Preprocess.Version == 2 && normal.Snapshot.PreprocessLoadError == "", "normal preprocess");
        Check(normal.Snapshot.RpItemIntroduction.SystemPrompt == "disk" && !normal.RpUsedEmbeddedDefaults, "normal RP disk file");

        io.Put("PreprocessPrompts.json", "{\"Version\":1}");
        var old = loader.Load();
        Check(old.Snapshot.Preprocess.Version == 2 && old.Snapshot.PreprocessLoadError == "", "old preprocess schema uses embedded default");
        Check(io.Logs.Any(x => x.Contains("旧版 PreprocessPrompts.json")), "old schema compatibility log");

        io.Files.Remove("PreprocessPrompts.json");
        Check(new PromptConfigurationLoader(io).Load().Snapshot.PreprocessLoadError.Contains("找不到 PreprocessPrompts.json"), "missing preprocess is error, not silent default");
        io.Put("PreprocessPrompts.json", "not-json");
        Check(new PromptConfigurationLoader(io).Load().Snapshot.PreprocessLoadError.Length > 0, "corrupt preprocess is error");
        io.Put("PreprocessPrompts.json", ValidPreprocess(2));
        io.Resource("AnimusForge.Defaults.PreprocessPrompts.json", "{\"Version\":0}");
        Check(new PromptConfigurationLoader(io).Load().Snapshot.PreprocessLoadError.Contains("Version 无效"), "invalid embedded schema fails");
        io.Resource("AnimusForge.Defaults.PreprocessPrompts.json", ValidPreprocess(2));

        io.Files.Remove("RpItemIntroductionPrompts.json");
        var rpMissing = loader.Load();
        Check(rpMissing.RpUsedEmbeddedDefaults && rpMissing.Snapshot.RpItemIntroduction.SystemPrompt == "embedded", "missing RP uses embedded");
        rpMissing.Snapshot.RpItemIntroduction.SystemPrompt = "mutated-old-revision";
        var rpNext = loader.Load();
        Check(rpNext.Snapshot.RpItemIntroduction.SystemPrompt == "embedded"
            && !ReferenceEquals(rpMissing.Snapshot.RpItemIntroduction, rpNext.Snapshot.RpItemIntroduction),
            "embedded RP fallback must be a fresh model per replacement generation");
        io.Files["RpItemIntroductionPrompts.json"] = new byte[] { 0xef, 0xbb, 0xbf }.Concat(Encoding.UTF8.GetBytes("{}" )).ToArray();
        Check(new PromptConfigurationLoader(io).Load().RpUsedEmbeddedDefaults, "RP BOM uses embedded");
        io.Resources.Remove("AnimusForge.Defaults.RpItemIntroductionPrompts.json");
        var rpBothBad = new PromptConfigurationLoader(io).Load();
        Check(rpBothBad.RpBothUnavailable && rpBothBad.Snapshot.RpItemIntroduction.SystemPrompt == "", "invalid disk and embedded disables RP");

        io = FullFiles();
        io.Put("RuleBehaviorPrompts.json", "not-json");
        var outer = new PromptConfigurationLoader(io).Load();
        Check(outer.Snapshot.Main.Marker == "default" && outer.Snapshot.Guardrail.Marker == "default", "outer failure replaces all models");
        Check(outer.Snapshot.PreprocessLoadError.Length > 0, "outer failure records preprocess error");
        io = FullFiles();
        io.Files.Remove("AIConfig.json");
        var missingMain = new PromptConfigurationLoader(io).Load();
        Check(missingMain.Snapshot.Main.Marker == "default" && missingMain.Snapshot.Guardrail.Marker == "rule-user", "missing main alone preserves other configs");
        io = FullFiles();
        io.Put("AIConfig.json", "not-json");
        Check(new PromptConfigurationLoader(io).Load().Snapshot.Guardrail.Marker == "default", "corrupt main triggers original outer all-default fallback");
        io = FullFiles();
        io.Files.Remove("RuleBehaviorPrompts.json");
        var missingRule = new PromptConfigurationLoader(io).Load();
        Check(missingRule.Snapshot.Guardrail.Marker == "default" && missingRule.Snapshot.ActionPostprocess.Marker == "action-user",
            "missing guardrail alone preserves later files");
        io = FullFiles();
        io.Files.Remove("ActionPostprocessPrompts.json");
        var missingAction = new PromptConfigurationLoader(io).Load();
        Check(missingAction.Snapshot.ActionPostprocess.Marker == "default" && missingAction.Snapshot.Preprocess.Version == 2,
            "missing action alone preserves later files");
        io = FullFiles();
        io.Put("ActionPostprocessPrompts.json", "not-json");
        Check(new PromptConfigurationLoader(io).Load().Snapshot.Guardrail.Marker == "default",
            "corrupt action retains outer all-default failure semantics");
        io = FullFiles();
        io.Files.Remove("ProactiveNpcRequestPrompts.json");
        var missingProactive = new PromptConfigurationLoader(io).Load();
        Check(missingProactive.Snapshot.ProactiveRequest.Marker == "default" && missingProactive.Snapshot.Main.Marker == "main-user",
            "missing proactive uses its own fallback without replacing main");
        io = FullFiles();
        io.Put("ProactiveNpcRequestPrompts.json", "not-json");
        Check(new PromptConfigurationLoader(io).Load().Snapshot.ProactiveRequest.Marker == "default",
            "corrupt proactive uses its own fallback");
        io = FullFiles();
        io.Put("RpItemIntroductionPrompts.json", "not-json");
        var corruptRp = new PromptConfigurationLoader(io).Load();
        Check(corruptRp.RpUsedEmbeddedDefaults && corruptRp.Snapshot.RpItemIntroduction.SystemPrompt == "embedded",
            "corrupt RP file uses embedded fallback");
        var rules = new GuardrailConfigModel
        {
            Duel = new DuelConfig { TriggerInstruction = "legacy duel", AcceptKeywords = new List<string> { "  duel  ", "DUEL" } },
            RulePrompts = new List<GuardrailRulePromptConfig>
            {
                new GuardrailRulePromptConfig { Id = "duel", Instruction = "first custom", Code = "FIRST" },
                new GuardrailRulePromptConfig { Id = " DUEL ", Instruction = "last custom", Code = "LAST", PostprocessRules = new List<PostprocessRuleEntry> { new PostprocessRuleEntry { Tag = "" }, new PostprocessRuleEntry { Tag = "[TEST]" } } }
            }
        };
        var registry = PromptRuleRegistry.Build(rules);
        Check(registry.Count == 4 && registry["duel"].Instruction == "last custom", "custom same-ID rules override legacy and earlier custom");
        Check(registry["duel"].Code == "LAST" && registry["duel"].PostprocessRules.Count == 1, "custom normalization retains valid postprocess rule");
        Check(registry["reward"].Code == "TRADE" && registry["loan"].Code == "DEBT", "legacy code fallback preserved");
        io = FullFiles();
        loader = new PromptConfigurationLoader(io);
        var store = new RevisionedPromptConfigurationStore<PromptConfigurationSnapshot>(loader.Load().Snapshot);
        var before = store.Capture();
        io.Put("RuleBehaviorPrompts.json", "{\"Marker\":\"new-rule\"}");
        using (var entered = new ManualResetEventSlim())
        using (var release = new ManualResetEventSlim())
        {
            io.BlockedPath = "RuleBehaviorPrompts.json";
            io.Entered = entered;
            io.Release = release;
            var pending = Task.Run(() => store.Reload(() => loader.Load().Snapshot, _ => new PromptConfigurationSnapshot(new AIConfigModel(), new GuardrailConfigModel(), new ActionPostprocessConfigModel(), new PreprocessPromptsConfigModel(), new ProactiveNpcRequestPromptsConfigModel(), new RpItemIntroductionPromptsConfigModel(), "fallback")));
            Check(entered.Wait(TimeSpan.FromSeconds(5)), "production loader reaches blocked reload");
            Check(ReferenceEquals(before, store.Capture()) && store.Capture().Value.Guardrail.Marker == "rule-user", "in-flight production reload does not leak partial models");
            release.Set();
            Check(pending.GetAwaiter().GetResult().Value.Guardrail.Marker == "new-rule", "production reload publishes complete replacement");
        }
        io.BlockedPath = null;
        var hitStore = new RevisionedPromptConfigurationStore<PromptConfigurationSnapshot>(before.Value);
        using (hitStore.BeginCapture())
        {
            var pinned = hitStore.Read();
            Task.Run(() => hitStore.Reload(() => store.Capture().Value,
                _ => throw new Exception("unexpected"))).GetAwaiter().GetResult();
            Check(hitStore.Capture().Revision != pinned.Revision && hitStore.Read().Revision == pinned.Revision,
                "outer hit scope keeps evaluation and registry on one revision across reload");
            Check(hitStore.Read().Value.Guardrail.Marker == "rule-user"
                && hitStore.Capture().Value.Guardrail.Marker == "new-rule",
                "outer hit scope reads old rule text while next revision is live");
        }
        Check(hitStore.Read().Value.Guardrail.Marker == "new-rule", "outer hit scope releases pinned revision");
        io.Put("RuleBehaviorPrompts.json", "not-json");
        var failure = store.Reload(() => loader.Load().Snapshot, _ => throw new Exception("unexpected"));
        Check(failure.Revision == before.Revision + 2 && failure.Value.Main.Marker == "default", "production outer failure also replaces and advances revision");
        var exceptionFallback = store.Reload(() => throw new IOException("loader failed"),
            _ => new PromptConfigurationSnapshot(new AIConfigModel(), new GuardrailConfigModel(),
                new ActionPostprocessConfigModel(), new PreprocessPromptsConfigModel(),
                new ProactiveNpcRequestPromptsConfigModel(), new RpItemIntroductionPromptsConfigModel(), "fallback"));
        Check(exceptionFallback.Revision == failure.Revision + 1 && exceptionFallback.Value.PreprocessLoadError == "fallback",
            "exceptional default replacement also advances generation");
        Console.WriteLine("PromptJ03 configuration loader checks=" + _checks);
    }
}
