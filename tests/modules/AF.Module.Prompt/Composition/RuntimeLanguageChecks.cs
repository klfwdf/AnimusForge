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
        Type archive = product.GetType("AnimusForge.WeeklyReportArchivePolicy",true);
        string worldTitle = Prompt(product,"Raw","weekly_memory_compat_871fa8d0ce6a");
        Check((string)Call(archive,"ProjectTitleForDisplay","weekly_report:world:0:",worldTitle) == rows["AF_UI_Weekly_OpeningWorldTitle"], "world opening title follows UI");
        Check((string)Call(archive,"ProjectTitleForDisplay","weekly_report:world:0:","edited title") == "edited title", "edited opening title remains literal");
        string heading = (string)Call(archive,"ProjectBulletinHeadingsForDisplay","【大事件】\nText\n【其他消息】\nItem");
        Check(heading == rows["AF_UI_Weekly_MajorHeading"] + "\nText\n" + rows["AF_UI_Weekly_MinorHeading"] + "\nItem", "legacy bulletin headings follow UI");
        string date = (string)Call(product.GetType("AnimusForge.DiplomacyDateProjection",true),"ForUi","1084年秋季2天");
        Check(language != "English" || (!date.Contains("年") && date.Contains("1084") && date.Contains("2")), "known diplomacy date follows English UI");
        // UI language changes must not select a different Prompt package.
        Type profileType = product.GetType("AnimusForge.MyBehavior+NpcPersonaProfile", true);
        object profile = Activator.CreateInstance(profileType, true);
        Type traitType = product.GetType("AnimusForge.NpcPersonaTrait", true);
        object trait = Activator.CreateInstance(traitType, true);
        traitType.GetField("Id", AllInstance).SetValue(trait, "cautious");
        traitType.GetField("Strength", AllInstance).SetValue(trait, 2);
        object traits = profileType.GetField("Traits", AllInstance).GetValue(profile);
        traits.GetType().GetMethod("Add").Invoke(traits, new[] { trait });
        string dynamicValue = "user {NAME} 中文 EN $1";
        foreach (string name in new[] { "Motivations", "Values", "Fears" })
        {
            object list = profileType.GetField(name, AllInstance).GetValue(profile);
            list.GetType().GetMethod("Add").Invoke(list, new object[] { dynamicValue });
        }
        profileType.GetField("SpeechStyle", AllInstance).SetValue(profile, dynamicValue);
        Type personaRules = product.GetType("AnimusForge.NpcPersonaTextRules", true);
        string displayed = (string)Call(personaRules, "BuildPersonaContext", profile, true);
        string prompted = (string)Call(personaRules, "BuildPersonaContext", profile, false);
        JObject personaPackage = JObject.Parse(File.ReadAllText(Path.Combine(root, "ModuleData", "PersonaPromptTexts.json")));
        Check(displayed.Contains(rows["AF_PERSONA_UI_TRAIT_cautious"] + "(2)"), "persona trait follows native UI " + language);
        Check(prompted.Contains((string)personaPackage["AF_PERSONA_TRAIT_cautious"] + "(2)"), "persona trait prompt retains package " + language);
        foreach (string field in new[] { "Traits", "Motivations", "Values", "Fears", "SpeechStyle" })
        {
            Check(displayed.Contains(rows["AF_PERSONA_UI_CONTEXT_" + field]), "persona field follows UI " + language + "/" + field);
            Check(prompted.Contains((string)personaPackage["AF_PERSONA_CONTEXT_" + field]), "persona field retains package " + language + "/" + field);
        }
        Check(displayed.Contains(dynamicValue) && prompted.Contains(dynamicValue), "persona original dynamic text and placeholders preserved " + language);

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
        foreach (string suffix in new[] { "delayed", "withdrawal" }) {
            string key = "memory_diplomacy_oral_" + suffix;
            string raw = Prompt(product, "Raw", key + "_source");
            string uiKey = suffix == "delayed" ? "AF_DIP_UI_OralGeneration_DelayedFact" : "AF_DIP_UI_OralGeneration_WithdrawalFact";
            foreach (string kind in new[] { "afef_player", "afef_npc" })
                Check((string)Call(item, "LocalizeFactBodyForDisplay", raw, kind) == rows[uiKey], "known oral fact body follows UI " + language + "/" + kind);
            Check((string)Call(item, "LocalizeFactBodyForDisplay", raw, "user") == raw, "ordinary user body remains literal");
            Check((string)Call(item, "LocalizeFactBodyForDisplay", raw + literal, "afef_npc") == raw + literal, "unknown or edited fact body remains literal");
        }
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
        Type audit = product.GetType("AnimusForge.PromptLanguageAudit", true);
        var han = (System.Text.RegularExpressions.Regex)audit.GetField("Han", AllStatic).GetValue(null);
        for (int code = 0; code <= char.MaxValue; code++)
        {
            bool expected = (code >= 0x3400 && code <= 0x4DBF) || (code >= 0x4E00 && code <= 0x9FFF) || (code >= 0xF900 && code <= 0xFAFF);
            if (han.IsMatch(((char)code).ToString()) != expected) throw new Exception("Han audit range drift " + code);
        }
        Check(true, "actual audit Unicode blocks preserve all 65536 BMP code-point decisions");
        var protocol = (System.Text.RegularExpressions.Regex)audit.GetField("StableProtocol", AllStatic).GetValue(null);
        Check(protocol.Replace("【AFEF玩家行为补充】 [AFEF NPC行为补充]", "").Contains("行为补充"), "audit no longer exempts Chinese fixed AFEF markers");
        Check(protocol.Replace("[AFEF PLAYER_ACTION] [AFEF NPC_ACTION]", "").Trim() == "", "audit recognizes canonical ASCII fact markers");
        string guard = Prompt(product, "Get", "conversation_reply_language");
        Check(guard == string.Empty, "content edition has no fixed response-language rule");
        void Guard(string value, string name) => Check(!value.Contains("Write all newly generated natural-language content in English")
            && !value.Contains("所有新生成的自然语言内容均使用中文"), "actual writer has no fixed output-language tail " + name);
        string literal = "literal {NAME} $1 ID=lord_S9_m; days=14; gold=-200";
        Check(Prompt(product, "ApplyOutputLanguage", literal) == literal, "composer preserves complete original system without language rule");
        Check(Prompt(product, "ApplyOutputLanguage", literal + "  ") == literal + "  ", "player prompt trailing whitespace and requested language survive unchanged");
        Type memory = product.GetType("AnimusForge.Refactor.Runtime.MemorySummaryRules", true);
        Guard((string)Call(memory, "DailySystemPrompt", 999, 3, literal, literal), "daily memory");
        Guard((string)Call(memory, "MajorSystemPrompt", 280, literal), "major memory");
        Guard((string)Call(memory, "OverviewSystemPrompt", 280, literal), "overview memory");
        Type persona = product.GetType("AnimusForge.NpcPersonaTextRules", true);
        foreach (object value in new[] {
            Call(persona, "BuildNative", literal, literal, true, literal, ""),
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
            string userHeader = (string)Call(product.GetType("AnimusForge.DiplomacyMiscPromptTexts", true), "GetRequired", "PlayerDraft_UserHeader");
            Check(messages.Count == 2 && (string)messages[0]["role"] == "system" && (string)messages[1]["role"] == "user"
                && (string)messages[1]["content"] == userHeader + literal
                && (preference == "" || ((string)messages[0]["content"]).Contains(preference)), "incoming two-role draft keeps preference in system and exact original player material");
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
        Check(guarded.Count == 1 && JToken.DeepEquals(guarded[0], userOnly[0]), "policy does not inject a language system message into user-only requests");
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
        Type directionType = product.GetType("AnimusForge.Illustrator.Core.IllustrationDirection", true);
        foreach (string title in new[] { "灯下裁决", "Lamp Verdict", "Mixed 中文 title" })
        {
            object direction = Call(directionType, "SplitMetadata", "[[AFI_PROTOCOL_TITLE]]" + title
                + "\n[[AFI_PROTOCOL_THEME]]主题 theme\n[[AFI_PROTOCOL_ACTION]]action\n[[AFI_PROTOCOL_SUBJECT]]body");
            Check((string)directionType.GetProperty("Title").GetValue(direction) == title, "director retains original generated title regardless of language");
            Check((string)directionType.GetProperty("Theme").GetValue(direction) == "主题 theme", "director retains original mixed-language theme");
        }
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
        string knownOpening = Prompt(product,"Raw","weekly_memory_compat_871fa8d0ce6a");
        Check(Prompt(product,"ProjectOpeningTextForPrompt",knownOpening) == Prompt(product,"Get","opening_world_title"), "fixed opening title projects at request boundary");
        Check(Prompt(product,"ProjectOpeningTextForPrompt","edited {NAME} $1") == "edited {NAME} $1", "edited opening title is preserved in prompt");
        AfefReaderCompatibility(product);
        FixedWireProjection(product);
        RequestPromptAuditChecks.Run(product);
        StabilityRebellionRetirementChecks.Run(product);
        Console.WriteLine("PASS actual runtime language/UI checks=" + checks);
    }
    private static void AfefReaderCompatibility(Assembly product)
    {
        var failures = new List<string>();
        int total = 0;
        void Audit(bool ok, string label) { total++; if (!ok) failures.Add(label); }
        Type ledger = product.GetType("AnimusForge.DialogueHistoryLedger",true);
        Type recall = product.GetType("AnimusForge.HistoryArchiveRecallOwner",true);
        Type recovery = product.GetType("AnimusForge.MemoryRecoverySeedRules",true);
        Type speech = product.GetType("AnimusForge.ConversationSpeechTextRules",true);
        Type history = product.GetType("AnimusForge.MemoryHistoryCommitBannerlordAdapter",true);
        Type business = product.GetType("AnimusForge.MemoryBusinessStateOwner",true);
        string body = "fact {NAME} $1: 20 gold";
        string firstMeeting = Prompt(product,"Raw","weekly_memory_compat_cdf0b7f2b2ed") + " actor";
        foreach (bool npc in new[] { false, true })
        foreach (string prefix in new[] {
            Prompt(product,"Raw",npc ? "afef_npc_action" : "afef_player_action"),
            Prompt(product,"Raw",npc ? "afef_npc_action_bracket" : "afef_player_action_bracket"),
            npc ? "[AFEF NPC_ACTION]" : "[AFEF PLAYER_ACTION]" })
        {
            string input = prefix + " " + body;
            string canonical = Prompt(product,"Raw",npc ? "afef_npc_action" : "afef_player_action") + " " + body;
            Audit((string)Call(product.GetType("AnimusForge.Refactor.Modules.ScenePromptMessageProjectionComposer",true),"NormalizeSceneExtraFactForHistory",input) == canonical, "scene history keeps actor " + prefix);
            string sceneSingleBody = (string)Call(product.GetType("AnimusForge.ConversationRuntimeTexts",true),"Raw","AF_CONV_Runtime_0300") + "actor" + (string)Call(product.GetType("AnimusForge.ConversationRuntimeTexts",true),"Raw","AF_CONV_Runtime_0301");
            Audit((bool)Call(product.GetType("AnimusForge.SceneHistoryProjectionOwner",true),"IsSingleUseSceneNpcFactText",prefix + " " + sceneSingleBody) == npc, "temporary scene expiry keeps actor " + prefix);
            Audit((string)Call(ledger,"NormalizeAfefFact",input) == canonical, "storage prefix actor " + prefix);
            Audit((string)Call(recovery,"RenderInteractionMemoryFact",input) == canonical, "recovery does not double-wrap " + prefix);
            Audit((string)Call(speech,"NormalizeNativeConversationFactLineForPrompt",input,"AFEF") == canonical, "native fact keeps actor " + prefix);
            var appended = new System.Text.StringBuilder();
            Call(business,"AppendPlayerExtraFactLine",appended,input);
            Audit(appended.ToString().TrimEnd('\r','\n') == canonical, "extra fact keeps actor " + prefix);
            foreach (string line in new[] { input, "[AF_SCENE_SESSION:7] " + input })
            {
                Audit((bool)Call(recall,"IsSystemFactLine",line), "archive fact filter " + prefix);
                Audit((string)Call(history,"StripSpeakerPrefixForRecall",line) == body, "recall strips exact fact envelope " + prefix);
            }
            object[] parsed = { input, null, null, null };
            Call(history,"ClassifyDialogueHistoryLine",parsed);
            Audit((string)parsed[3] == (npc ? "afef_npc" : "afef_player") && (string)parsed[2] == body, "history actor classification " + prefix);
            if (npc)
            {
                Func<string,bool> first = value => value == firstMeeting;
                Audit((bool)Call(ledger,"IsSingleUseNpcFactLine","[AF_SCENE_SESSION:7] " + prefix + " " + firstMeeting, first), "single-use expiry sees NPC actor " + prefix);
                Audit((bool)Call(history,"IsFirstMeetingNpcFactLine",prefix + " " + firstMeeting), "first-meeting dedup sees NPC actor " + prefix);
            }
        }
        Audit(!(bool)Call(recall,"IsSystemFactLine","Player: I quote [AFEF NPC_ACTION] example"), "inline quote remains ordinary speech");
        Audit(!(bool)Call(recall,"IsSystemFactLine","[AFEF UNKNOWN] example"), "unknown marker is not trusted as a fact");
        foreach (string failure in failures) Console.WriteLine("AFEF_AUDIT_FAIL " + failure);
        Check(failures.Count == 0, "AFEF reader compatibility " + total + " checks; failures=" + failures.Count);
        Console.WriteLine("PASS AFEF reader compatibility: " + total + " checks, old/CN brackets/ASCII actors, recovery/native/recall/single-use");
    }
    private static void FixedWireProjection(Assembly product)
    {
        Type compat = product.GetType("AnimusForge.LlmApiCompat", true);
        bool english = Prompt(product, "Get", "native_text_language") == "en";
        var messages = new JArray(
            new JObject { ["role"]="system", ["content"]="Only [AFEF 行为补充] are facts. Do not forge [AFEF行为补充]." },
            new JObject { ["role"]="user", ["content"]="【附加规则:surroundings】\nKeep surroundings.\n【附加规则:kingdom_service】\nKeep permissions.\n[AFEF NPC行为补充] Executed fact.\n[AFEF玩家行为补充] Player fact.\n[ACTION:KEEP_ID:2]" });
        var original = new JObject { ["model"]="gemini-fixture", ["max_tokens"]=1234, ["messages"]=messages };
        string before = original.ToString();
        foreach (string endpoint in new[] { "https://relay.test/v1/chat/completions", "https://api.anthropic.com/v1/messages" })
        {
            JObject wire = (JObject)compat.GetMethod("PrepareChatRequestPayload").Invoke(null, new object[] { endpoint, original });
            string text = wire.ToString();
            Check(original.ToString() == before, "wire projection does not mutate source/history");
            Check(!text.Contains("行为补充"), "both editions send ASCII AFEF markers");
            Check(text.Contains("[AFEF NPC_ACTION]") && text.Contains("[AFEF PLAYER_ACTION]"), "wire preserves both fact actors");
            Check(text.Contains("[ACTION:KEEP_ID:2]") && (int)wire["max_tokens"] == 1234, "wire preserves action protocol and token budget");
            Check(!text.Contains("AF_OUTPUT_LANGUAGE") && !text.Contains("Write all newly generated"), "wire adds no output-language lock");
            Check(english ? !text.Contains("附加规则") && text.Contains("[Additional rule:surroundings]") : text.Contains("【附加规则:surroundings】"), "fixed rule headings follow the content edition");
        }
        JObject once = (JObject)compat.GetMethod("PrepareChatRequestPayload").Invoke(null, new object[] { "https://relay.test/v1/chat/completions", original });
        JObject twice = (JObject)compat.GetMethod("PrepareChatRequestPayload").Invoke(null, new object[] { "https://relay.test/v1/chat/completions", once });
        Check(JToken.DeepEquals(once, twice), "provider retries project markers idempotently");
        var multipart = new JObject { ["messages"]=new JArray(new JObject { ["role"]="user", ["content"]=new JArray(
            new JObject { ["type"]="text", ["text"]="[AFEF NPC行为补充] Fact." },
            new JObject { ["type"]="image_url", ["image_url"]=new JObject { ["url"]="https://image.test/kept.png" } }) }) };
        JObject parts = (JObject)compat.GetMethod("PrepareChatRequestPayload").Invoke(null, new object[] { "https://relay.test/v1/chat/completions", multipart });
        Check((string)parts["messages"][0]["content"][0]["text"] == "[AFEF NPC_ACTION] Fact."
            && JToken.DeepEquals(parts["messages"][0]["content"][1],multipart["messages"][0]["content"][1]), "multipart projection preserves image block");
        Type bulletin = product.GetType("AnimusForge.WorldBulletinPolicy",true);
        string body = (string)Call(bulletin,"BuildBody","Major prose", new[] { "First minor", "Second minor" });
        object[] split = { body, null, null };
        Call(product.GetType("AnimusForge.WorldBulletinStateOwner",true),"SplitWorldBulletinBody",split);
        Check((string)split[1] == "Major prose" && ((IEnumerable)split[2]).Cast<string>().SequenceEqual(new[] { "First minor", "Second minor" }), "published body grammar and archive parser agree in both editions");
        Check(!english || !System.Text.RegularExpressions.Regex.IsMatch(body,@"[\u3400-\u9fff]"), "new EN bulletin body headings contain no Chinese");
        string recordedPath = Environment.GetEnvironmentVariable("AF_TEST_RECORDED_REQUEST");
        if (!string.IsNullOrEmpty(recordedPath) && english)
        {
            JObject recorded = JObject.Parse(File.ReadAllText(recordedPath));
            string saved = recorded.ToString();
            JObject wire = (JObject)compat.GetMethod("PrepareChatRequestPayload").Invoke(null, new object[] { "https://relay.test/v1/chat/completions", recorded });
            var han = new System.Text.RegularExpressions.Regex(@"[\u3400-\u9fff]");
            int beforeHan = han.Matches(string.Join("\n",recorded["messages"].Select(m => (string)m["content"]))).Count;
            int afterHan = han.Matches(string.Join("\n",wire["messages"].Select(m => (string)m["content"]))).Count;
            Check(beforeHan > 0 && afterHan == 0, "recorded first scene request has no fixed Chinese after wire projection");
            Check(recorded.ToString() == saved && recorded["messages"].Count() == wire["messages"].Count(), "recorded request keeps input and message count");
            Check(!wire.ToString().Contains("AF_OUTPUT_LANGUAGE") && wire["messages"].Select(m => (string)m["role"]).SequenceEqual(recorded["messages"].Select(m => (string)m["role"])), "recorded request keeps roles without a language lock");
            Console.WriteLine("PASS recorded first scene request: Chinese characters " + beforeHan + " -> " + afterHan + "; no HTTP request");
        }
        Type ledger = product.GetType("AnimusForge.DialogueHistoryLedger",true);
        foreach (string actor in new[] { "NPC", "PLAYER" })
        {
            string canonical = (string)Call(ledger,"NormalizeAfefFact","[AFEF " + actor + "_ACTION] fact");
            Check(!canonical.Contains("_ACTION]") && canonical.EndsWith(" fact"), "ASCII input retains legacy save identity " + actor);
            object[] parsed = { "[AFEF " + actor + "_ACTION] fact", null, null, null };
            Call(product.GetType("AnimusForge.MemoryHistoryCommitBannerlordAdapter",true),"ClassifyDialogueHistoryLine",parsed);
            Check((string)parsed[3] == (actor == "NPC" ? "afef_npc" : "afef_player") && (string)parsed[2] == "fact", "history reader retains ASCII actor " + actor);
        }
        Type material = product.GetType("AnimusForge.WorldBulletinCampaignMaterialPolicy",true);
        string casualty = (string)Call(material,"BattleCasualties","阵亡21、负伤18");
        Check(english ? casualty == "21 killed, 18 wounded" : casualty == "阵亡21、负伤18", "battle counts and casualty meanings survive projection");
        string snapshot = "Battle at 野外: A defeated B（非领主部队）. Winner: 阵亡21、负伤18";
        string projected = (string)Call(material,"ProjectRecordedBattle",snapshot,"battle");
        Check(!english || !System.Text.RegularExpressions.Regex.IsMatch(projected,@"[\u4e00-\u9fff]"), "recorded battle fixed slots project to English");
        Check((string)Call(material,"ProjectRecordedBattle",snapshot,"player_quote") == snapshot, "unrelated quoted text is not a battle fact");
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
