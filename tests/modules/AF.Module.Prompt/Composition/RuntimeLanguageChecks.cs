using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

internal static class RuntimeLanguageChecks
{
    static int checks;
    const BindingFlags AllStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool condition, string name) { checks++; if (!condition) throw new Exception("FAIL " + name); }
    static object Call(Type type, string name, params object[] args)
        => type.GetMethods(AllStatic).Single(method => method.Name == name && method.GetParameters().Length == args.Length).Invoke(null, args);
    static string Prompt(Assembly product, string name, params object[] args)
        => (string)Call(product.GetType("AnimusForge.PromptComposerTexts", true), name, args);
    static Dictionary<string, string> UiRows(string root, string language)
    {
        string directory = Path.Combine(root, "ModuleData/Languages", language == "English" ? "" : "CNs");
        return Directory.GetFiles(directory, "*.xml", SearchOption.TopDirectoryOnly)
            .SelectMany(file => XDocument.Load(file).Descendants("string"))
            .GroupBy(row => (string)row.Attribute("id"))
            .ToDictionary(group => group.Key, group => (string)group.First().Attribute("text"));
    }
    internal static void Ui(Assembly product, string root, string language)
    {
        var rows = UiRows(root, language);
        Type dropdown = product.GetType("AnimusForge.McmDropdownRuntimeRefresh", true);
        var options = new[] {
            new { Property="InterfaceStyleDropdown", Index=0, Key="AFD_MCM_Interface_Original" },
            new { Property="InterfaceStyleDropdown", Index=1, Key="AFD_MCM_Interface_New" },
            new { Property="ShoutPanelStyleDropdown", Index=0, Key="AFD_MCM_ShoutStyle_PausedScroll" },
            new { Property="ShoutPanelStyleDropdown", Index=1, Key="AFD_MCM_ShoutStyle_Scroll" },
            new { Property="ShoutPanelStyleDropdown", Index=2, Key="AFD_MCM_ShoutStyle_SideFolio" }
        };
        object original = new object();
        foreach (var option in options)
            Check((string)Call(dropdown, "LocalizeOptionForDisplay", "AnimusForge.DialogueUI.DialogueUiSettings", option.Property, option.Index, original) == rows[option.Key], "actual MCM option follows UI " + language + "/" + option.Key);
        foreach (var scope in new[] {
            new { Type="OtherMod", Property="InterfaceStyleDropdown", Index=0 },
            new { Type="AnimusForge.DialogueUI.DialogueUiSettings", Property="OtherProperty", Index=0 },
            new { Type="AnimusForge.DialogueUI.DialogueUiSettings", Property="ShoutPanelStyleDropdown", Index=3 }
        }) Check(ReferenceEquals(Call(dropdown, "LocalizeOptionForDisplay", scope.Type, scope.Property, scope.Index, original), original), "unknown dropdown object is unchanged");
        var getterScopes = new Dictionary<string, string[]> {
            ["AnimusForge.AnimusForgeConversationHistoryLogVM"] = new[] { "EditButtonText", "DeleteText" },
            ["AnimusForge.TerminalWeeklyReportBrowserPopupVM"] = new[] { "AF_UI_Terminal_Prefab_003Text", "AF_UI_Terminal_Prefab_004Text", "AF_UI_Terminal_Prefab_008Text", "AF_UI_Terminal_Prefab_009Text", "AF_UI_Terminal_Prefab_010Text", "AF_UI_Terminal_Prefab_011Text", "AF_UI_Terminal_Prefab_012Text", "AF_UI_Terminal_Prefab_013Text", "AF_UI_Terminal_Prefab_014Text", "AF_UI_Terminal_Prefab_015Text" }
        };
        foreach (var scope in getterScopes) {
            Type type = product.GetType(scope.Key, true);
            object model = FormatterServices.GetUninitializedObject(type);
            foreach (string name in scope.Value) {
                PropertyInfo property = type.GetProperty(name);
                string key = name == "EditButtonText" ? "AF_UI_ConversationHistory_Edit" : name == "DeleteText" ? "AF_UI_ConversationHistory_Delete" : name.Substring(0, name.Length - 4);
                Check(property != null && property.GetCustomAttributesData().Any(attr => attr.AttributeType.Name.Contains("DataSourceProperty")), "real datasource getter " + scope.Key + "/" + name);
                Check((string)property.GetValue(model) == rows[key], "actual control text " + language + "/" + name);
            }
        }
        Type item = product.GetType("AnimusForge.AnimusForgeConversationHistoryLogItemVM", true);
        foreach (var fact in new[] {
            new { Raw="weekly_memory_compat_d26ab0225ce9", Kind="afef_player", Key="AF_UI_ConversationHistory_PlayerAction" },
            new { Raw="weekly_memory_compat_d0d9d4e1d4c0", Kind="afef_npc", Key="AF_UI_ConversationHistory_NpcAction" }
        }) {
            string raw = Prompt(product, "Raw", fact.Raw);
            Check((string)Call(item, "LocalizeSpeakerForDisplay", raw, fact.Kind) == rows[fact.Key], "AFEF display follows UI " + language);
            Check((string)Call(item, "LocalizeSpeakerForDisplay", raw, "user") == raw, "ordinary speaker is not reclassified");
        }
        string literal = "Vimi {NAME} $1";
        Check((string)Call(item, "LocalizeSpeakerForDisplay", literal, "afef_player") == literal, "dynamic speaker remains literal");
        Type status = product.GetType("AnimusForge.Illustrator.UI.IllustrationStatusUiText", true);
        Type illustrator = product.GetType("AnimusForge.Illustrator.IllustratorText", true);
        string defaultTheme = (string)Call(illustrator, "Raw", "Residual_IllustrationStatusUiText_ade70c013f11");
        Check((string)Call(status, "DisplayTheme", defaultTheme) == rows["AF_ILL_UI_DEFAULT_THEME"], "known gallery theme follows UI " + language);
        Check((string)Call(status, "DisplayTheme", literal) == literal, "generated or edited gallery theme remains literal");
        Type parser = product.GetType("AnimusForge.MemoryHistoryCommitBannerlordAdapter", true);
        string factLine = Prompt(product, "Raw", "weekly_memory_compat_6d7b96ecf184") + " " + literal;
        object[] parsed = { factLine, null, null, null };
        Call(parser, "ClassifyDialogueHistoryLine", parsed);
        Check((string)parsed[3] == "afef_player" && (string)parsed[2] == literal, "actual history parser keeps original AFEF fact body");
        Check((string)Call(item, "LocalizeSpeakerForDisplay", (string)parsed[1], (string)parsed[3]) == rows["AF_UI_ConversationHistory_PlayerAction"], "actual stored AFEF label is localized before entity linking");
    }
    internal static void Writers(Assembly product)
    {
        string guard = Prompt(product, "Get", "conversation_reply_language");
        void Guard(string value, string name) => Check(value.EndsWith(guard, StringComparison.Ordinal), "actual writer output-language tail " + name);
        string literal = "literal {NAME} $1 ID=lord_S9_m; days=14; gold=-200";
        Check(Prompt(product, "ApplyOutputLanguage", literal) == literal + "\n\n" + guard, "language guard preserves complete original system");
        Check(Prompt(product, "ApplyOutputLanguage", literal + "\n\n" + guard) == literal + "\n\n" + guard, "language guard is idempotent");
        Type memory = product.GetType("AnimusForge.Refactor.Runtime.MemorySummaryRules", true);
        Guard((string)Call(memory, "DailySystemPrompt", 999, 3, literal, literal), "daily memory");
        Guard((string)Call(memory, "MajorSystemPrompt", 280, literal), "major memory");
        Guard((string)Call(memory, "OverviewSystemPrompt", 280, literal), "overview memory");
        Type persona = product.GetType("AnimusForge.NpcPersonaTextRules", true);
        foreach (object value in new[] {
            Call(persona, "BuildNative", literal, literal, literal, true, literal),
            Call(persona, "BuildPromoted", literal, literal, literal, "lord_S9_m", literal, literal, literal, literal, literal, literal, literal)
        }) {
            Guard((string)value.GetType().GetProperty("System", AllInstance).GetValue(value), "persona");
            Check(((string)value.GetType().GetProperty("User", AllInstance).GetValue(value)).Contains(literal), "persona preserves literal facts");
        }
        Guard((string)Call(product.GetType("AnimusForge.RebellionNamingRules", true), "BuildSystemPrompt", literal), "rebellion naming");
        Guard((string)Call(product.GetType("AnimusForge.WorldBulletinPolicy", true), "BuildSystemPrompt", 2, 3, literal), "world bulletin");
        Type diplomacy = product.GetType("AnimusForge.Refactor.Domain.WorldDiplomacyPromptContractRules", true);
        Func<string, string, string> englishContract = (_, fallback) => fallback;
        string canonical = (string)Call(diplomacy, "BuildCanonicalHistorySystemPrompt", literal, 100, 400, englishContract);
        Guard(canonical, "diplomacy declaration/history");
        string marker = (string)diplomacy.GetField("DiplomaticDeclarationWritingContractMarker", AllStatic).GetValue(null);
        object[] extract = { canonical, marker, null };
        Check((bool)Call(diplomacy, "TryExtractCommonContractBeforeMarker", extract) && (string)extract[2] == literal, "common diplomacy contract remains byte-identical outside guard");
        Guard((string)Call(diplomacy, "BuildAnalysisSystemPrompt", literal, englishContract), "diplomacy analysis");
        Guard((string)Call(diplomacy, "BuildRoundCompressionSystemPrompt", englishContract), "diplomacy round summary");
        Type draftInput = product.GetType("AnimusForge.WorldDiplomacyPlayerDraftInput", true);
        object input = Activator.CreateInstance(draftInput, AllInstance, null, new object[] { literal, 100, 400, 0L }, null);
        foreach (string preference in new[] { "", literal }) {
            var messages = (JArray)Call(product.GetType("AnimusForge.WorldDiplomacyPlayerDraftRules", true), "BuildMessages", input, preference);
            Guard((string)messages.OfType<JObject>().Last(message => (string)message["role"] == "system")["content"], "player diplomatic draft");
            Check(messages.Count == (preference == "" ? 2 : 3) && ((string)messages.Last["content"]).EndsWith(literal, StringComparison.Ordinal), "draft roles and original player input preserved");
        }
        Type policy = product.GetType("AnimusForge.PolicyLlmClient", true);
        var original = new JArray(new JObject { ["role"]="system", ["content"]=literal }, new JObject { ["role"]="user", ["content"]=literal });
        string before = original.ToString();
        var frozen = (JArray)Call(policy, "FreezePolicyMessages", original);
        Guard((string)frozen[0]["content"], "all policy requests");
        Check(original.ToString() == before && (string)frozen[1]["content"] == literal && frozen.Count == original.Count, "policy freezes without altering original messages or user body");
        Check(JToken.DeepEquals(frozen, (JArray)Call(policy, "FreezePolicyMessages", frozen)), "policy retries do not duplicate guard");
        var userOnly = new JArray(new JObject { ["role"]="user", ["content"]=literal });
        var guarded = (JArray)Call(policy, "FreezePolicyMessages", userOnly);
        Check((string)guarded[0]["role"] == "system" && (string)guarded[0]["content"] == guard && JToken.DeepEquals(guarded[1], userOnly[0]), "policy missing-system fallback preserves player body");
        Type weekly = product.GetType("AnimusForge.WeeklyPromptMaterialOwner", true);
        object profile = Call(weekly, "GetWeeklyReportPromptProfile", 200);
        Guard((string)Call(weekly, "BuildWeeklyReportSystemPrompt", null, profile, literal), "single weekly report");
        Type capture = product.GetType("AnimusForge.Refactor.Adapters.WeeklyPromptCaptureAdapter", true);
        Guard((string)Call(capture, "BuildWeekZeroShortSummarySystemPrompt", "world", "", literal), "week-zero summary");
        Type port = capture.GetNestedType("RequestPromptCapturePorts", BindingFlags.NonPublic);
        if (port == null) port = product.GetType("AnimusForge.Refactor.Adapters.RequestPromptCapturePorts", true);
        object ports = Activator.CreateInstance(port, true);
        foreach (var entry in new Dictionary<string, object> { ["Profile"]=profile, ["WritingRequirements"]=literal }) {
            FieldInfo field = port.GetField(entry.Key, AllInstance);
            field.SetValue(ports, Expression.Lambda(field.FieldType, Expression.Constant(entry.Value, field.FieldType.GetGenericArguments().Last())).Compile());
        }
        Guard((string)Call(capture, "BuildWeeklyReportFullOnDemandSystemPrompt", ports, null), "on-demand full weekly report");
        Guard((string)Call(capture, "BuildWeeklyBatchReportSystemPrompt", ports, null), "batch weekly report");
        Type illustratorText = product.GetType("AnimusForge.Illustrator.IllustratorText", true);
        Type planType = product.GetType("AnimusForge.Illustrator.Core.IllustrationPromptPlan", true);
        Type optionsType = product.GetType("AnimusForge.Illustrator.Core.IllustrationOptions", true);
        object optionsValue = FormatterServices.GetUninitializedObject(optionsType);
        optionsType.GetField("<DirectorApiMaxTokens>k__BackingField", AllInstance).SetValue(optionsValue, 1536);
        optionsType.GetField("<DirectorModelName>k__BackingField", AllInstance).SetValue(optionsValue, "fixture-model");
        Type director = product.GetType("AnimusForge.Illustrator.Core.VisualDirectorEngine", true);
        foreach (string mode in new[] { "fixture", (string)Call(illustratorText,"Raw","Director_933"), (string)Call(illustratorText,"Raw","Director_937"), (string)Call(illustratorText,"Raw","ScreenshotRules_837") }) {
            object plan = Activator.CreateInstance(planType, new object[] { mode, literal, literal });
            var payload = (JObject)Call(director, "BuildDirectorPayload", plan, optionsValue, null, true);
            Guard((string)payload["messages"][0]["content"], "illustration director mode " + mode);
            Check((int)payload["max_tokens"] == 1536 && (string)payload["model"] == "fixture-model" && payload["messages"].Count() == 2, "director model, budget and roles remain intact");
            Check(((string)payload["messages"][1]["content"]).Contains(literal), "director preserves literal captured facts");
        }
        Console.WriteLine("PASS actual runtime language/UI checks=" + checks);
    }
    internal static void EntityMatches(Assembly product)
    {
        Type service = product.GetType("AnimusForge.WorldEntityRetrievalService", true);
        Type record = service.GetNestedType("DetachedEntityCandidate", BindingFlags.NonPublic);
        MethodInfo find = service.GetMethods(AllStatic).Single(method => method.Name == "FindMatches").MakeGenericMethod(record);
        object Candidate(string name, string id, params string[] aliases) {
            object value = Activator.CreateInstance(record, true);
            foreach (var pair in new Dictionary<string, object> { ["Id"]=id, ["Name"]=name, ["Aliases"]=aliases.ToList() }) record.GetField(pair.Key, AllInstance).SetValue(value, pair.Value);
            return value;
        }
        object Selector(int index, string field) {
            Type delegateType = find.GetParameters()[index].ParameterType;
            var argument = Expression.Parameter(record);
            return Expression.Lambda(delegateType, Expression.Convert(Expression.Field(argument, field), delegateType.GetGenericArguments().Last()), argument).Compile();
        }
        var candidates = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(record));
        object dakhila = Candidate("Dakhila", "lord_S9_m", "Dakhila", "Lord Dakhila", "lord_S9_m");
        object nords = Candidate("Nords", "nords", "Nords", "nords");
        candidates.Add(dakhila); candidates.Add(nords);
        IList Match(string mention) => (IList)find.Invoke(null, new object[] { "hero", new[] { mention }, new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase) { [mention]=0 }, candidates, Selector(4,"Aliases"), Selector(5,"Id"), Selector(6,"Name"), 3, null });
        foreach (string mention in new[] { "lord", "lords", "LORDS" }) Check(Match(mention).Count == 0, "actual shared matcher rejects generic title ID/name fuzzy injection " + mention);
        foreach (string mention in new[] { "Dakhila", "Lord Dakhila", "lord_S9_m" }) {
            IList matches = Match(mention);
            Check(matches.Count == 1 && ReferenceEquals(matches[0].GetType().GetField("Value").GetValue(matches[0]), dakhila), "actual matcher retains named or explicit-ID mention " + mention);
        }
        object namedLords = Candidate("Lords", "custom_lords", "Lords", "custom_lords");
        candidates.Add(namedLords);
        IList exact = Match("lords");
        Check(exact.Count == 1 && ReferenceEquals(exact[0].GetType().GetField("Value").GetValue(exact[0]), namedLords), "actual display name Lords remains selectable");
        Console.WriteLine("PASS actual generic-mention matcher; total language/UI/entity checks=" + checks);
    }
}
