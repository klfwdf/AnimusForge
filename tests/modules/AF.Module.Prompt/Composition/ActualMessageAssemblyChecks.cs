using System.Collections;
using System.Reflection;
using System.Linq.Expressions;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

internal static class ActualMessageAssemblyChecks
{
    static int checks;
    static void Check(bool condition, string name) { checks++; if (!condition) throw new Exception("FAIL " + name); }
    static string Field(object value, string name) => (string)value.GetType().GetProperty(name).GetValue(value);
    static object Call(Type type, string name, params object[] values) => type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, values);
    static int Main(string[] args)
    {
        try
        {
            string[] gameDirectories = Environment.GetEnvironmentVariable("AF_TEST_REFERENCES").Split('|');
            using (var sha = SHA256.Create())
                Check(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(args[0]))).Replace("-", "") == args[1], "candidate hash");
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
                string name = new AssemblyName(e.Name).Name;
                if (name == "AnimusForge") return null; // Never resolve an old Stage product.
                foreach (string directory in args.Skip(2)) {
                    if ((name.StartsWith("TaleWorlds.", StringComparison.Ordinal) || name == "SandBox" || name == "SandBoxCore" || name == "StoryMode")
                        && !gameDirectories.Contains(directory, StringComparer.OrdinalIgnoreCase)) continue;
                    string path = Path.Combine(directory, name + ".dll");
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            Assembly product = Assembly.LoadFrom(args[0]);
            Type paths = product.GetType("AnimusForge.AnimusForgeModulePaths", true);
            string resolvedRoot = (string)paths.GetMethod("GetCurrentModuleRoot", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            Check(resolvedRoot == Environment.GetEnvironmentVariable("AF_TEST_MODULE_ROOT"), "real module resolver stays in isolated package fixture");
            JObject corpus = JObject.Parse(File.ReadAllText(Path.Combine(resolvedRoot, "ModuleData", "PromptComposerTexts.json")));
            string Text(string key) => (string)corpus[key];
            Type owner = product.GetType("AnimusForge.MainPromptMessageAssemblyOwner", true);
            JObject retrievalText = JObject.Parse(File.ReadAllText(Path.Combine(resolvedRoot, "ModuleData", "PromptRetrievalTexts.json")));
            Type rerankerText = product.GetType("AnimusForge.PromptRuleTextEvidence", true);
            string rerank = (string)Call(rerankerText, "RerankText", "rule_id", "group_id", "purpose evidence", new[] { "中文输入", "English input" });
            Check(rerank.Contains((string)retrievalText["RuleIdLabel"] + "rule_id"), "actual rerank label uses package and current key");
            Check(rerank.Contains("中文输入 / English input"), "both input languages survive rerank composition");
            Assembly localization = Assembly.Load("TaleWorlds.Localization");
            localization.GetType("TaleWorlds.Localization.LocalizedTextManager", true).GetMethod("LoadLocalizationXmls").Invoke(null, new object[] { new[] { resolvedRoot } });
            Type textManager = localization.GetType("TaleWorlds.Localization.MBTextManager", true);
            Type memoryUi = product.GetType("AnimusForge.MemoryEditorUiText", true);
            Type host = product.GetType("AnimusForge.MyBehavior", true);
            Type coupUi = product.GetType("AnimusForge.CoupSystem.CoupUiText", true);
            JObject coupRaw = JObject.Parse(File.ReadAllText(Path.Combine(resolvedRoot, "ModuleData/CoupSourceTexts.json")));
            Type retry = product.GetType("AnimusForge.LlmRetryPrompt", true);
            Type llmSources = product.GetType("AnimusForge.LlmSourceTexts", true);
            var englishHistory = XDocument.Load(Path.Combine(resolvedRoot, "ModuleData/Languages/afmemory_ui_strings.xml")).Descendants("string").ToDictionary(x => (string)x.Attribute("id"), x => (string)x.Attribute("text"));
            foreach (string uiLanguage in new[] { "English", "简体中文", "English" }) {
                textManager.GetMethod("ChangeLanguage").Invoke(null, new object[] { uiLanguage });
                RuntimeLanguageChecks.Ui(product, resolvedRoot, uiLanguage);
                string xml = Path.Combine(resolvedRoot, "ModuleData/Languages", uiLanguage == "English" ? "afmemory_ui_strings.xml" : "CNs/afmemory_ui_strings-zh-CN.xml");
                string[] historyKeys = { "EditUnavailable", "EditMainThread", "StoreUnavailable", "EditFailed", "EditEmpty", "EditChanged", "EditFormat", "EditUnchanged", "EditDone", "EditDoneCompressed", "DeleteMainThread", "DeleteFailed" };
                var historyRows = XDocument.Load(xml).Descendants("string").Where(x => historyKeys.Any(key => (string)x.Attribute("id") == "AF_MEMORY_UI_History" + key)).ToArray();
                Check(historyRows.Length == 12, "all new history UI keys loaded");
                foreach (var row in historyRows) {
                    // Bannerlord uses the source fallback for English; CN resolves the registered XML.
                    string actualText = (string)Call(memoryUi, "T", (string)row.Attribute("id"), englishHistory[(string)row.Attribute("id")]);
                    Check(actualText == (string)row.Attribute("text"), "actual history UI language " + uiLanguage + " " + (string)row.Attribute("id") + " actual=" + actualText);
                }
                object[] edit = { null, "unchanged player text", null };
                Check(!(bool)host.GetMethod("EditDialogueHistoryLineForExternal", BindingFlags.Public | BindingFlags.Static).Invoke(null, edit), "invalid history remains uneditable");
                Check((string)edit[2] == (string)historyRows.Single(x => (string)x.Attribute("id") == "AF_MEMORY_UI_HistoryEditUnavailable").Attribute("text"), "actual edit status follows UI language");
                Type promptReader = product.GetType("AnimusForge.PromptComposerTexts", true);
                Check((string)Call(promptReader, "Get", "main_courier_reply_system") == Text("main_courier_reply_system"), "UI changes do not switch package prompt");
                string retainedDetail = "X{NAME}$1";
                foreach (string prefix in new[] { "Post-coup rebellion registration failed: ", "Rebellion settlement exception; changes made were retained and founding will not be repeated: ", "Coup fact registration not completed: " }) {
                    string projected = (string)Call(coupUi, "LocalizeReason", prefix + retainedDetail);
                    Check(projected.EndsWith(retainedDetail, StringComparison.Ordinal), "Coup English prefix preserves complete literal detail " + prefix);
                    Check(((string)Call(coupUi, "LocalizeReason", prefix)).Length > 0, "Coup empty detail does not overrun prefix");
                }
                string pending = (string)coupRaw["Prefix_WarRegistrationPending"] + (string)coupRaw["NamingEmpty"];
                string expectedPending = (string)Call(coupUi, "LocalizeReason", pending);
                foreach (string key in new[] { "WarRetryHintCurly", "WarRetryHintCurlySpaced", "WarRetryHintAscii", "WarRetryHintHtml" })
                    Check((string)Call(coupUi, "LocalizeReason", pending + (string)coupRaw[key]) == expectedPending, "Coup historical hint retains nested reason " + key);
                Check((string)Call(coupUi, "LocalizeReason", retainedDetail) == retainedDetail, "Coup unknown detail stays literal");
                string actionError = (string)Call(llmSources, "Get", "RetryActionTagsMissing");
                string modelBody = "PLAYER {NAME} $1\n【模型回复（完整）】\n" + actionError;
                string apiBody = "RAW {DETAIL} $1\n【API原始响应（完整）】";
                string diagnostic = (string)retry.GetMethod("BuildFailureDetail", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { actionError, modelBody, apiBody });
                string originalDiagnostic = diagnostic;
                string display = (string)Call(retry, "ProjectKnownErrorForUi", diagnostic);
                string uiFile = Path.Combine(resolvedRoot, "ModuleData/Languages", uiLanguage == "English" ? "af_ui_strings.xml" : "CNs/af_ui_strings-zh-CN.xml");
                string expectedReason = (string)XDocument.Load(uiFile).Descendants("string").Single(x => (string)x.Attribute("id") == "AF_UI_Retry_ActionTagsMissing").Attribute("text");
                Check(display.StartsWith(expectedReason, StringComparison.Ordinal), "retry structured reason follows UI");
                Check(display.Contains(modelBody) && display.Contains(apiBody), "retry model and raw response preserve all marker-looking content");
                Check(diagnostic == originalDiagnostic && diagnostic.StartsWith(actionError, StringComparison.Ordinal), "retry original diagnostic is unchanged");
                Check((string)Call(retry, "ProjectKnownErrorForUi", modelBody) == modelBody, "unstructured player-like detail stays untouched");
            }
            RuntimeLanguageChecks.Writers(product);
            TerminalSettingsChecks.Run(product, resolvedRoot);
            RuntimeLanguageChecks.EntityMatches(product);
            Type dto = product.GetType("AnimusForge.ConversationMessage", true);
            object Message(string role, string content, string speaker, int agent = -1) {
                object result = Activator.CreateInstance(dto);
                foreach (var entry in new Dictionary<string, object> { ["Role"] = role, ["Content"] = content, ["SpeakerName"] = speaker, ["SpeakerAgentIndex"] = agent, ["TargetAgentIndex"] = 7 })
                    dto.GetProperty(entry.Key).SetValue(result, entry.Value);
                return result;
            }
            var history = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dto));
            history.Add(Message("user", "Player earlier question", "Player"));
            history.Add(Message("assistant", "Alda previous answer", "Alda", 7));
            history.Add(Message("assistant", "Borin overheard answer", "Borin", 8));
            history.Add(Message("system", "[AFEF玩家行为补充] confirmed transfer", "AFEF"));
            history.Add(Message("system", "[AFEF NPC行为补充] confirmed reply", "AFEF"));
            var reply = (IList)Call(owner, "BuildCourierReplyMessages", "Alda", "Player", "current letter", "rules", "delivery fact", "history", history, "persona", "exclude", "recent", "identity", "relationship", "location", "date", "custom");
            var inbound = (IList)Call(owner, "BuildInboundNpcLetterMessages", "Alda", "Player", "intent", "rules", "delivery fact", "history", history, "persona", "exclude", "recent", "identity", "relationship", "location", "date", 220, "private letter", "custom");
            Check(reply.Count == 8 && inbound.Count == 8, "Courier complete history cardinality");
            Type sceneOwner = product.GetType("AnimusForge.SceneHistoryMessageAssemblyOwner", true);
            Type sceneInputType = product.GetType("AnimusForge.SceneHistoryMessageAssemblyInput", true);
            Type sceneContextType = product.GetType("AnimusForge.SceneHistoryMessageContext", true);
            object sceneInput = Activator.CreateInstance(sceneInputType, true);
            sceneInputType.GetField("SystemPrompt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sceneInput, "role and custom rules");
            sceneInputType.GetField("Context", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sceneInput, Activator.CreateInstance(sceneContextType, true));
            var sceneMessages = (IList)Call(sceneOwner, "Build", sceneInput);
            Check(sceneMessages.Count == 1 && Field(sceneMessages[0], "role") == "system", "actual Native/Scene shared builder keeps the system role");
            Check(Field(sceneMessages[0], "content") == "role and custom rules\n\n" + Text("conversation_reply_language"), "actual Native/Scene shared builder appends the package language outside optional reply-format rules");
            Check(Field(reply[0], "role") == "system" && Field(reply[0], "content").StartsWith("custom\n"), "Courier system/custom rule order");
            Check(Field(reply[0], "content") == "custom\n" + Text("main_courier_reply_system").Replace("{NPC}", "Alda").Replace("{PLAYER}", "Player") + "\npersona\nexclude\n\n" + Text("conversation_reply_language"), "reply system retains all rules and adds selected package language");
            Check(Field(inbound[0], "content") == "custom\n" + Text("main_courier_inbound_system").Replace("{NPC}", "Alda").Replace("{PLAYER}", "Player").Replace("{KIND}", "private letter").Replace("{CHARS}", "220") + "\npersona\nexclude\n\n" + Text("conversation_reply_language"), "inbound system retains all rules and adds selected package language");
            foreach (string key in new[] { "main_courier_000", "main_courier_001", "main_courier_002" })
                Check(Field(reply[1], "content").Contains(Text(key)), "reply package heading " + key);
            foreach (string key in new[] { "main_courier_008", "main_courier_009", "main_courier_010" })
                Check(Field(inbound[1], "content").Contains(Text(key)), "inbound package heading " + key);
            string[] roles = { "user", "assistant", "user", "user", "user" };
            for (int i = 0; i < history.Count; i++) {
                Check(Field(reply[i + 2], "role") == roles[i], "Courier role " + i);
                Check(Field(reply[i + 2], "content") == Field(inbound[i + 2], "content"), "Courier direction history " + i);
            }
            Check(Field(reply[5], "content").Contains("[AFEF玩家行为补充]"), "Courier player AFEF preserved");
            Check(Field(reply[6], "content").Contains("[AFEF NPC行为补充]"), "Courier NPC AFEF preserved");
            Check(Field(reply[7], "content").Contains("current letter") && Field(reply[7], "content").Contains("delivery fact"), "Courier current input/fact last");
            Check(Field(reply[7], "content").StartsWith(Text("main_courier_003")), "reply current input package heading");
            Check(Field(inbound[7], "content").StartsWith(Text("main_courier_011")), "inbound current intent package heading");
            string literalName = "领主{PLAYER}$1";
            string literalBody = "保留玩家正文 {NPC} $1 [AFEF玩家行为补充]";
            var literalReply = (IList)Call(owner, "BuildCourierReplyMessages", literalName, "Player", literalBody, "", "", "", null, "", "", "", "", "", "", "", "");
            Check(Field(literalReply[0], "content").Contains(literalName), "dynamic name is not expanded again");
            Check(Field(literalReply[literalReply.Count - 1], "content").Contains(literalBody), "player text and marker remain literal");
            Type equipment = product.GetType("AnimusForge.EquipmentPromptCaptureAdapter", true);
            JObject captureText = JObject.Parse(File.ReadAllText(Path.Combine(resolvedRoot, "ModuleData", "AdapterPromptCapture.json")));
            string unknown = (string)captureText["Project_Unknown"];
            Check((string)Call(equipment, "BuildHeroEquipmentSummaryForPrompt", null, null, 8) == unknown, "null hero equipment uses package");
            Check((string)Call(equipment, "BuildNonHeroEquipmentSummaryForPrompt", null, 8) == unknown, "null NPC equipment uses package");
            Type equipmentPort = product.GetType("AnimusForge.HeroEquipmentPromptLivePort", true);
            object port = Activator.CreateInstance(equipmentPort, true);
            FieldInfo resolveContext = equipmentPort.GetField("ResolveContext", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo getItem = equipmentPort.GetField("GetItem", BindingFlags.Instance | BindingFlags.NonPublic);
            ParameterExpression[] resolveArgs = resolveContext.FieldType.GetMethod("Invoke").GetParameters().Select(p => Expression.Parameter(p.ParameterType)).ToArray();
            ParameterExpression[] itemArgs = getItem.FieldType.GetMethod("Invoke").GetParameters().Select(p => Expression.Parameter(p.ParameterType)).ToArray();
            getItem.SetValue(port, Expression.Lambda(getItem.FieldType, Expression.Throw(Expression.New(typeof(InvalidOperationException)), getItem.FieldType.GetMethod("Invoke").ReturnType), itemArgs).Compile());
            object emptyHero = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(resolveArgs[0].Type);
            foreach (bool civilian in new[] { false, true }) {
                resolveContext.SetValue(port, Expression.Lambda(resolveContext.FieldType, Expression.Block(Expression.Assign(resolveArgs[1], Expression.Constant(civilian)), Expression.Constant(true)), resolveArgs).Compile());
                Check((string)Call(equipment, "BuildHeroEquipmentSummaryForPrompt", emptyHero, port, 8) == Text(civilian ? "equipment_civilian" : "equipment_battle") + Text("equipment_colon") + Text("equipment_naked"), "equipment read failure retains package context " + civilian);
            }
            Type layout = owner.GetNestedType("SceneSingleSpeakerLayout", BindingFlags.NonPublic);
            foreach (string name in Enum.GetNames(layout)) {
                var actual = (string[])Call(owner, "BuildSceneSingleSpeakerPrefixSections", Enum.Parse(layout, name), "private", "persisted", "runtime", "local", "fact", "trust", "misc", "patience", "knowledge", "rules");
                string J(params string[] values) => string.Join("\n\n", values);
                string[] expected = name switch {
                    "GroupFallback" => new[] { "private", "persisted", "runtime", "local", "fact", "trust", "misc", "patience", J("knowledge", "rules") },
                    "Passive" => new[] { "private", "persisted", "runtime", "local", "trust", "misc", "patience", J("knowledge", "rules") },
                    "GroupTurn" => new[] { "private", "persisted", J("runtime", "local", "fact", "trust", "misc", "patience"), J("knowledge", "rules") },
                    "ImmediateReaction" => new[] { "private", "persisted", J("runtime", "knowledge", J(J("local", "trust", "misc"), "fact")) },
                    "CompactArrival" => new[] { "private", "persisted", J("runtime", "knowledge"), J(J("local", "trust", "misc"), "fact") },
                    _ => throw new Exception("unknown product layout")
                };
                Check(actual.SequenceEqual(expected), "actual DLL Scene layout " + name);
            }
            Type shout = product.GetType("AnimusForge.ShoutBehavior", true);
            MethodInfo convert = shout.GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(m => m.Name == "TryConvertSceneMessageToStrictChatMessage" && m.GetParameters().Length == 5);
            // Managed-only Game/Character snapshots expose a null Hero fallback.
            // No live campaign Hero, Mission, save, or native engine is created.
            Type game = Assembly.Load("TaleWorlds.Core").GetType("TaleWorlds.Core.Game", true);
            PropertyInfo currentGame = game.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);
            object previousGame = currentGame.GetValue(null);
            object emptyGame = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(game);
            Type character = Assembly.Load("TaleWorlds.CampaignSystem").GetType("TaleWorlds.CampaignSystem.CharacterObject", true);
            game.GetProperty("PlayerTroop").SetValue(emptyGame, System.Runtime.Serialization.FormatterServices.GetUninitializedObject(character));
            currentGame.SetValue(null, emptyGame);
            try
            {
            foreach (bool distance in new[] { false, true }) {
                for (int i = 0; i < history.Count; i++) {
                    object[] input = { history[i], 7, null, null, distance };
                    Check((bool)convert.Invoke(null, input), "real Native/Scene accepts history " + i);
                    Check(Field(input[2], "role") == roles[i], "real Native/Scene role " + distance + "/" + i);
                    if (i >= 3) Check(Field(input[2], "content").Contains(i == 3 ? "[AFEF玩家行为补充]" : "[AFEF NPC行为补充]"), "real Native/Scene AFEF " + distance + "/" + i);
                }
            }
            }
            finally { currentGame.SetValue(null, previousGame); }
            foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.StartsWith("TaleWorlds.", StringComparison.Ordinal)))
                Check(gameDirectories.Contains(Path.GetDirectoryName(loaded.Location), StringComparer.OrdinalIgnoreCase), "matching game dependency " + loaded.GetName().Name);
            Console.WriteLine("PASS " + checks + " actual candidate DLL message/history checks; LIVE=NOT_RUN");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
