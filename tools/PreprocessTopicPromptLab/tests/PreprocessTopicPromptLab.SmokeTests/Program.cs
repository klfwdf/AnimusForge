using System.Text;
using System.Text.Json;
using PreprocessTopicPromptLab.Core;

var service = new PreprocessTopicLabService();
var repoRoot = service.FindDefaultRepoRoot(Directory.GetCurrentDirectory());
Console.WriteLine("repo: " + repoRoot);

var catalog = service.LoadCatalog(repoRoot);
Console.WriteLine("topics: " + catalog.Rules.Count);
if (catalog.Rules.Count == 0 || string.IsNullOrWhiteSpace(catalog.PreprocessPromptsPath) || !File.Exists(catalog.PreprocessPromptsPath))
{
    throw new InvalidOperationException("Topics or PreprocessPrompts.json were not loaded.");
}
var memorySelectionPrompt = string.Join("\n", new[]
{
    catalog.PreprocessPrompts.MemorySelection.ParallelModeInstruction,
    catalog.PreprocessPrompts.MemorySelection.UnifiedModeInstruction,
    catalog.PreprocessPrompts.MemorySelection.UserPromptTemplate
});
if (!memorySelectionPrompt.Contains("\"memory_ids\"", StringComparison.Ordinal) ||
    memorySelectionPrompt.Contains("mentioned_entities", StringComparison.OrdinalIgnoreCase) ||
    memorySelectionPrompt.Contains("noun", StringComparison.OrdinalIgnoreCase) ||
    memorySelectionPrompt.Contains("名词", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Memory preprocessing must select only memory IDs and must not extract nouns.");
}

var duel = catalog.Rules.FirstOrDefault(x => x.Id == "duel");
var reward = catalog.Rules.FirstOrDefault(x => x.Id == "reward");
var sceneMove = catalog.Rules.FirstOrDefault(x => x.Id == "scene_mechanism_actions");
var sceneRelay = catalog.Rules.FirstOrDefault(x => x.Id == "scene_auto_group_relay");
var kingdomAgenda = catalog.Rules.FirstOrDefault(x => x.Id == "kingdom_agenda");
if (duel == null || reward == null)
{
    throw new InvalidOperationException("Expected duel and reward topics.");
}
if (!string.Equals(reward.Code, "Asset Transfer", StringComparison.Ordinal) ||
    catalog.Rules.Any(x => x.Id == "settlement_transfer"))
{
    throw new InvalidOperationException("Fixed assets should route through the unified Asset Transfer reward topic.");
}
if (sceneMove == null || sceneRelay != null)
{
    throw new InvalidOperationException("Preprocess catalog should include scene movement but exclude scene relay.");
}
if (kingdomAgenda == null || !kingdomAgenda.IsEnabled ||
    catalog.Rules.Any(x => x.IsEnabled && x.Id is "vote_deal" or "propose_agenda") ||
    !string.Equals(kingdomAgenda.Code, "KINGDOM_AGENDA", StringComparison.Ordinal) ||
    !kingdomAgenda.PostprocessRules.Any(x => x.Tag == "[ACTION:AGENDA:议程ID:选项ID:权重]") ||
    !kingdomAgenda.PostprocessRules.Any(x => x.Tag == "[ACTION:AGENDA:CUSTOM_POLICY]"))
{
    throw new InvalidOperationException("Preprocess catalog should expose only the unified kingdom agenda topic.");
}

var sampleCasesPath = Path.Combine(service.GetLabRoot(repoRoot), "cases", "sample_cases.jsonl");
var sampleCases = service.LoadCases(sampleCasesPath);
var customPolicySample = sampleCases.FirstOrDefault(x => x.CaseId == "kingdom_agenda_custom_policy_001");
var existingAgendaSample = sampleCases.FirstOrDefault(x => x.CaseId == "kingdom_agenda_existing_vote_001");
if (customPolicySample == null ||
    !customPolicySample.ExpectedTopics.Contains("kingdom_agenda", StringComparer.OrdinalIgnoreCase) ||
    existingAgendaSample == null ||
    !existingAgendaSample.ExpectedTopics.Contains("kingdom_agenda", StringComparer.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Custom-policy and existing-vote samples must share the kingdom agenda topic.");
}

var labCase = new PreprocessLabCase
{
    CaseId = "smoke_duel_reward",
    Title = "决斗带赌注",
    PlayerText = "敢不敢和我单挑？我押五千第纳尔。",
    NpcReplyText = "",
    HistoryLines = new List<string>
    {
        "玩家: 我想用决斗解决这事。",
        "NPC: 你若有赌注，就说清楚。"
    },
    AfefFacts = new List<AfefFact>
    {
        new() { Kind = "player", Text = "玩家展示了5000第纳尔。"}
    },
    RuntimeContext = "目标是有名NPC，允许决斗。",
    ExpectedTopics = new List<string> { "duel" },
    AllowedExtraTopics = new List<string> { "reward", "npc_major_actions", "noble_deference" },
    ForbiddenTopics = new List<string> { "marriage" }
};

var settings = new PreprocessLabSettings
{
    ApiUrl = "https://api.openai.com/v1/chat/completions",
    ApiKey = "",
    Model = "test-model",
    MaxTokens = 512,
    Temperature = 0
};
var promptConfig = service.GetDefaultPromptConfig();
var rendered = service.RenderPrompt(catalog, labCase, settings, promptConfig);
using var requestDoc = JsonDocument.Parse(rendered.RequestJson);
if (!requestDoc.RootElement.TryGetProperty("messages", out var messages))
{
    throw new InvalidOperationException("Request JSON does not contain messages.");
}
if (requestDoc.RootElement.GetProperty("max_tokens").GetInt32() != PreprocessTopicLabService.LabSafeAuxiliaryRouterMaxTokens)
{
    throw new InvalidOperationException("Request JSON does not use the lab-safe auxiliary router max_tokens.");
}
var renderedSystemMessage = messages[0].GetProperty("content").GetString() ?? "";
var renderedUserMessage = messages[1].GetProperty("content").GetString() ?? "";
var mentionedEntitiesSchema = JsonSerializer.Serialize(catalog.PreprocessPrompts.StrictJson.MentionedEntitiesSchema);
if (!string.Equals(rendered.SystemPrompt, PreprocessTopicLabService.DefaultSystemPrompt, StringComparison.Ordinal) ||
    !string.Equals(renderedSystemMessage, PreprocessTopicLabService.DefaultSystemPrompt, StringComparison.Ordinal) ||
    !rendered.SystemPrompt.Contains("Output strict JSON only", StringComparison.Ordinal) ||
    !rendered.SystemPrompt.Contains("Never output CSV", StringComparison.Ordinal) ||
    rendered.SystemPrompt.Contains("comma-separated list of topic numbers", StringComparison.OrdinalIgnoreCase) ||
    rendered.SystemPrompt.Contains("0 if no topic applies", StringComparison.OrdinalIgnoreCase) ||
    !rendered.UserPrompt.Contains("Select exactly 4 closest topic codes in rule_codes", StringComparison.Ordinal) ||
    !rendered.UserPrompt.Contains("Order entities by retrieval importance", StringComparison.Ordinal) ||
    !rendered.UserPrompt.Contains("for equal importance, put the newer mention first", StringComparison.Ordinal) ||
    !rendered.UserPrompt.Contains("Output one strict JSON object only", StringComparison.Ordinal) ||
    rendered.UserPrompt.Contains("comma-separated list of topic numbers", StringComparison.OrdinalIgnoreCase) ||
    rendered.UserPrompt.Contains("0 if no topic applies", StringComparison.OrdinalIgnoreCase) ||
    !rendered.UserPrompt.Contains("\"mentioned_entities\":" + mentionedEntitiesSchema, StringComparison.Ordinal) ||
    !string.Equals(renderedUserMessage, rendered.UserPrompt, StringComparison.Ordinal) ||
    !rendered.UserPrompt.Contains("DUEL: Duel", StringComparison.Ordinal) ||
    !rendered.UserPrompt.Contains("KINGDOM_AGENDA:", StringComparison.Ordinal) ||
    !rendered.UserPrompt.Contains("SCENE_MOVE:", StringComparison.Ordinal) ||
    rendered.UserPrompt.Contains("SCENE_RELAY:", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Rendered prompt does not match the mod auxiliary router structure.");
}
if (messages.GetArrayLength() != 2)
{
    throw new InvalidOperationException("Mod router request should contain exactly two messages.");
}
if (requestDoc.RootElement.GetProperty("thinking").GetProperty("type").GetString() != "disabled")
{
    throw new InvalidOperationException("OpenAI-compatible request should explicitly disable thinking when the setting is off.");
}

var anthropicSettings = new PreprocessLabSettings
{
    ApiUrl = "https://api.deepseek.com/anthropic",
    ApiProtocol = "anthropic",
    ApiKey = "",
    Model = "test-model",
    MaxTokens = 512,
    Temperature = 0
};
var anthropicRendered = service.RenderPrompt(catalog, labCase, anthropicSettings, promptConfig);
using var anthropicRequestDoc = JsonDocument.Parse(anthropicRendered.RequestJson);
if (!anthropicRequestDoc.RootElement.TryGetProperty("system", out var anthropicSystem) ||
    !anthropicRequestDoc.RootElement.TryGetProperty("messages", out var anthropicMessages))
{
    throw new InvalidOperationException("Anthropic-compatible request JSON should contain system and messages.");
}
if (anthropicRequestDoc.RootElement.GetProperty("max_tokens").GetInt32() != PreprocessTopicLabService.LabSafeAuxiliaryRouterMaxTokens)
{
    throw new InvalidOperationException("Anthropic-compatible request JSON does not use the lab-safe auxiliary router max_tokens.");
}
if (anthropicMessages.GetArrayLength() != 1 ||
    !string.Equals(anthropicMessages[0].GetProperty("role").GetString(), "user", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Anthropic-compatible mod router request should contain one user message.");
}
if (!string.Equals(anthropicSystem.GetString(), PreprocessTopicLabService.DefaultSystemPrompt, StringComparison.Ordinal) ||
    !string.Equals(anthropicMessages[0].GetProperty("content").GetString(), anthropicRendered.UserPrompt, StringComparison.Ordinal))
{
    throw new InvalidOperationException("Anthropic-compatible request system prompt does not match the mod router.");
}
if (anthropicRequestDoc.RootElement.GetProperty("thinking").GetProperty("type").GetString() != "disabled")
{
    throw new InvalidOperationException("Anthropic-compatible request should explicitly disable thinking when the setting is off.");
}

var overrideConfig = service.GetDefaultPromptConfig();
overrideConfig.RoutingGuidance = "SCENE_MOVE: local lead/follow scene commands.";
overrideConfig.TopicOverrides["duel"] = new TopicRouteOverride { TopicLabel = "单挑决斗/赌注" };
overrideConfig.TopicOverrides["siege_intervention_aftermath"] = new TopicRouteOverride { Code = "SIEGE_AFTER_SCENE", TopicLabel = "攻城后场景结算/处置" };
var overrideRendered = service.RenderPrompt(catalog, labCase, settings, overrideConfig);
if (!overrideRendered.UserPrompt.Contains("DUEL: 单挑决斗/赌注", StringComparison.Ordinal) ||
    !overrideRendered.UserPrompt.Contains("SIEGE_AFTER_SCENE: 攻城后场景结算/处置", StringComparison.Ordinal) ||
    !overrideRendered.UserPrompt.Contains("SCENE_MOVE: local lead/follow scene commands.", StringComparison.Ordinal))
{
    throw new InvalidOperationException("Topic route overrides or guidance were not rendered into the local lab prompt.");
}

var legacyPromptConfig = service.GetDefaultPromptConfig();
legacyPromptConfig.SystemPrompt = "legacy csv prompt";
var legacyConfigRendered = service.RenderPrompt(catalog, labCase, settings, legacyPromptConfig);
if (!string.Equals(legacyConfigRendered.SystemPrompt, PreprocessTopicLabService.DefaultSystemPrompt, StringComparison.Ordinal))
{
    throw new InvalidOperationException("Legacy prompt configuration must not override the strict preprocessing system contract.");
}

var promptPresetDirectory = Path.Combine(service.GetLabRoot(repoRoot), "prompts");
var promptPresetFiles = Directory.GetFiles(promptPresetDirectory, "topic-route-v*.json");
if (promptPresetFiles.Length == 0)
{
    throw new InvalidOperationException("No preprocessing prompt presets were found.");
}
var strictUtf8 = new UTF8Encoding(false, true);
foreach (var promptPresetFile in promptPresetFiles)
{
    var presetText = strictUtf8.GetString(File.ReadAllBytes(promptPresetFile));
    using var presetDocument = JsonDocument.Parse(presetText);
    var presetSystemPrompt = presetDocument.RootElement.GetProperty("SystemPrompt").GetString() ?? "";
    if (!string.Equals(presetSystemPrompt, PreprocessTopicLabService.DefaultSystemPrompt, StringComparison.Ordinal))
    {
        throw new InvalidOperationException("Prompt preset uses a stale preprocessing system contract: " + Path.GetFileName(promptPresetFile));
    }
}

var validPreprocessResponse = "{\"rule_codes\":[\"DUEL\",\"ASSET_TRANSFER\",\"NPC_MAJOR\",\"NOBLE_PRESSURE\"],\"mentioned_entities\":{\"entities\":[]}}";
if (!service.TryParseTopics(validPreprocessResponse, catalog.Rules, out var parsedTopics, out var validParseError))
{
    throw new InvalidOperationException("Valid preprocessing response was rejected: " + validParseError);
}
var score = service.ScoreTopics(labCase, parsedTopics);
Console.WriteLine("exact: " + score.ExactMatch + " recall=" + score.Recall + " precision=" + score.Precision);
if (!score.ExactMatch)
{
    throw new InvalidOperationException("Score should be exact when reward is allowed extra.");
}

var parsedSceneTopics = service.ParseTopics("{\"rule_codes\":[\"SCENE_RELAY\",\"SCENE_MOVE\"],\"mentioned_entities\":{\"entities\":[]}}", catalog.Rules);
if (parsedSceneTopics.Contains("scene_auto_group_relay", StringComparer.OrdinalIgnoreCase) ||
    !parsedSceneTopics.Contains("scene_mechanism_actions", StringComparer.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Scene relay should be ignored by preprocess parsing while scene movement remains injectable.");
}

var parsedAgendaTopics = service.ParseTopics("{\"rule_codes\":[\"KINGDOM_AGENDA\",\"NPC_RECENT\",\"NOBLE_PRESSURE\"],\"mentioned_entities\":{\"entities\":[]}}", catalog.Rules);
if (!parsedAgendaTopics.Contains("kingdom_agenda", StringComparer.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("KINGDOM_AGENDA did not map back to the unified agenda topic.");
}

var parsedAgendaDiplomacyTopics = service.ParseTopics("{\"rule_codes\":[\"KINGDOM_AGENDA\",\"DIPLOMACY\",\"NPC_RECENT\",\"NOBLE_PRESSURE\"],\"mentioned_entities\":{\"entities\":[]}}", catalog.Rules);
var aiConfigHandlerSource = File.ReadAllText(Path.Combine(repoRoot, "AIConfigHandler.cs"));
if (!parsedAgendaDiplomacyTopics.Contains("kingdom_agenda", StringComparer.OrdinalIgnoreCase) ||
    !parsedAgendaDiplomacyTopics.Contains("diplomacy", StringComparer.OrdinalIgnoreCase) ||
    aiConfigHandlerSource.Contains("PreferDirectDiplomacyTopicOverAgenda(", StringComparison.Ordinal))
{
    throw new InvalidOperationException("KINGDOM_AGENDA and DIPLOMACY must remain independent when both topics are selected.");
}

var invalidPreprocessResponses = new[]
{
    "2,13",
    "DUEL,ASSET_TRANSFER",
    "0",
    "\"DUEL\"",
    "[\"DUEL\",\"ASSET_TRANSFER\"]",
    "{\"rule_codes\":[\"2\",\"13\"],\"mentioned_entities\":{\"entities\":[]}}",
    "{\"rule_codes\":[\"TOPIC_2\",\"T13\"],\"mentioned_entities\":{\"entities\":[]}}",
    "{\"rule_codes\":\"DUEL,ASSET_TRANSFER\",\"mentioned_entities\":{\"entities\":[]}}",
    "{\"rule_codes\":[\"DUEL\"]}",
    "{\"rule_codes\":[\"DUEL\"],\"mentioned_entities\":{\"entities\":\"NPC\"}}",
    "{\"rule_codes\":[\"DUEL\"],\"mentioned_entities\":{\"heroes\":[],\"settlements\":[],\"terms\":[]}}"
};
foreach (var invalidPreprocessResponse in invalidPreprocessResponses)
{
    if (service.TryParseTopics(invalidPreprocessResponse, catalog.Rules, out var _, out var invalidParseError))
    {
        throw new InvalidOperationException("Invalid preprocessing response was accepted: " + invalidPreprocessResponse);
    }
    if (string.IsNullOrWhiteSpace(invalidParseError))
    {
        throw new InvalidOperationException("Invalid preprocessing response did not report a format error: " + invalidPreprocessResponse);
    }
}

var labRoot = service.GetLabRoot(repoRoot);
var runDir = service.CreateRunDirectory(labRoot);
var artifact = service.WriteOfflineArtifacts(runDir, 1, catalog, labCase, settings, promptConfig, "{\"rule_codes\":[\"DUEL\",\"ASSET_TRANSFER\",\"NPC_MAJOR\",\"NOBLE_PRESSURE\"],\"mentioned_entities\":{\"entities\":[]}}");
Console.WriteLine("smoke-run: " + runDir);

var siegeCase = new PreprocessLabCase
{
    CaseId = "smoke_siege_override",
    Title = "本地覆盖 code 解析",
    PlayerText = "攻城结束后，诸位听我处置这座城。",
    ExpectedTopics = new List<string> { "siege_intervention_aftermath" }
};
var overrideArtifact = service.WriteOfflineArtifacts(runDir, 2, catalog, siegeCase, settings, overrideConfig, "{\"rule_codes\":[\"SIEGE_AFTER_SCENE\"],\"mentioned_entities\":{\"entities\":[]}}");
if (!overrideArtifact.Score.ExactMatch)
{
    throw new InvalidOperationException("Topic route override code did not map back to the expected topic id.");
}

JsonDocument.Parse(File.ReadAllText(artifact.RequestPath));
JsonDocument.Parse(File.ReadAllText(artifact.MetaPath));
if (!File.Exists(artifact.InjectedRulesPath))
{
    throw new InvalidOperationException("Injected rules artifact was not written.");
}

Console.WriteLine("ok");
