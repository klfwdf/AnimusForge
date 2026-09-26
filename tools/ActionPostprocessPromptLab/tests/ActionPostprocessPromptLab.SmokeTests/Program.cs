using System.Text.Json;
using ActionPostprocessPromptLab.Core;

var service = new PromptLabService();
var repoRoot = service.FindDefaultRepoRoot(AppContext.BaseDirectory);
if (string.IsNullOrWhiteSpace(repoRoot))
{
    repoRoot = service.FindDefaultRepoRoot(Directory.GetCurrentDirectory());
}

if (string.IsNullOrWhiteSpace(repoRoot))
{
    Console.Error.WriteLine("Could not find repository root with AF.Module.Prompt content files.");
    return 1;
}

var labRoot = service.ResolveLabRoot(repoRoot);
var catalog = service.LoadCatalog(repoRoot);
Console.WriteLine("repo: " + repoRoot);
Console.WriteLine("rules: " + catalog.Rules.Count);
Console.WriteLine("mood-rules: " + catalog.ActionConfig.MoodRules.Count);

if (catalog.Rules.Count == 0)
{
    Console.Error.WriteLine("No postprocess rules were loaded.");
    return 1;
}

if (catalog.ActionConfig.MoodRules.Count == 0)
{
    Console.Error.WriteLine("No mood rules were loaded.");
    return 1;
}

var marriageRule = catalog.Rules.FirstOrDefault(x => x.Id == "marriage");
if (marriageRule == null || !marriageRule.DisplayName.Contains("婚姻", StringComparison.Ordinal))
{
    Console.Error.WriteLine("Rule display names were not localized to Chinese.");
    return 1;
}

var agendaRule = catalog.Rules.FirstOrDefault(x => x.Id == "kingdom_agenda");
var agendaTags = agendaRule?.PostprocessRules.Select(x => x.Tag ?? "").ToList() ?? new List<string>();
if (agendaRule == null ||
    !agendaTags.Contains("[ACTION:AGENDA:议程ID:选项ID:权重]", StringComparer.Ordinal) ||
    !agendaTags.Contains("[ACTION:AGENDA:CUSTOM_POLICY]", StringComparer.Ordinal) ||
    catalog.Rules.Any(x => x.Id is "vote_deal" or "propose_agenda") ||
    catalog.Rules.SelectMany(x => x.PostprocessRules).Any(x =>
        x.Tag.Contains("ACTION:VOTE_DEAL", StringComparison.Ordinal) ||
        x.Tag.Contains("ACTION:PROPOSE", StringComparison.Ordinal)))
{
    Console.Error.WriteLine("Kingdom agenda postprocess rules are missing the unified vote or custom-policy tag.");
    return 1;
}

var assetTransferRule = catalog.Rules.FirstOrDefault(x => x.Id == "reward");
var allConfiguredTags = catalog.Rules.SelectMany(x => x.PostprocessRules).Select(x => x.Tag ?? "").ToList();
if (assetTransferRule == null ||
    assetTransferRule.Code != "Asset Transfer" ||
    assetTransferRule.PostprocessRules.Count(x => x.Tag.StartsWith("[ACTION:GIVE_ASSET:", StringComparison.OrdinalIgnoreCase)) != 1 ||
    catalog.Rules.Any(x => x.Id == "settlement_transfer") ||
    allConfiguredTags.Any(x => x.Contains("GIVE_GOLD", StringComparison.OrdinalIgnoreCase) ||
                               x.Contains("GIVE_ITEM", StringComparison.OrdinalIgnoreCase) ||
                               x.Contains("SETTLEMENT_TRANSFER", StringComparison.OrdinalIgnoreCase)))
{
    Console.Error.WriteLine("Asset Transfer was not migrated to the single GIVE_ASSET tag family.");
    return 1;
}

var caseFile = Path.Combine(labRoot, "cases", "sample_cases.jsonl");
var cases = service.LoadCases(caseFile);
Console.WriteLine("cases: " + cases.Count);
if (cases.Count == 0)
{
    Console.Error.WriteLine("No sample cases were loaded.");
    return 1;
}

var customPolicyAgendaCases = cases.Where(x => x.CaseId.StartsWith("kingdom_agenda_custom_policy_", StringComparison.Ordinal)).ToList();
var customPolicyAgendaAcceptCase = customPolicyAgendaCases.FirstOrDefault(x => x.CaseId == "kingdom_agenda_custom_policy_accept_001");
if (customPolicyAgendaCases.Count < 5 ||
    customPolicyAgendaAcceptCase == null ||
    !customPolicyAgendaAcceptCase.ExpectedTags.Contains("[ACTION:AGENDA:CUSTOM_POLICY]", StringComparer.Ordinal) ||
    customPolicyAgendaCases.Where(x => x.CaseId != "kingdom_agenda_custom_policy_accept_001")
        .Any(x => x.ExpectedTags.Contains("[ACTION:AGENDA:CUSTOM_POLICY]", StringComparer.Ordinal)))
{
    Console.Error.WriteLine("Custom-policy agenda accept/refuse/negotiate/consider/non-ruler samples are missing or incorrectly labeled.");
    return 1;
}

var tagOverrideRunDir = service.CreateRunDirectory(labRoot);
var duelRule = catalog.Rules.FirstOrDefault(x => x.Id == "duel");
var duelEntry = duelRule?.PostprocessRules.FirstOrDefault(x => x.Tag == "[ACTION:DUEL]");
if (duelEntry == null)
{
    Console.Error.WriteLine("Duel postprocess tag was not found.");
    return 1;
}

var duelTag = duelEntry.Tag;
var overrideDescription = "测试用标签提示词覆盖：只有玩家和 NPC 都明确无条件同意正式决斗时才输出。";
var versionConfig = service.CloneActionConfigWithPromptsAndRuleDescriptions(
    catalog.ActionConfig,
    catalog.ActionConfig.SystemPrompt,
    catalog.ActionConfig.UserPromptTemplate,
    catalog.Rules);
versionConfig.RulePostprocessDescriptionOverrides["duel"][duelTag] = overrideDescription;
var versionPath = Path.Combine(tagOverrideRunDir, "tag_prompt_override.json");
service.SavePromptVersion(versionPath, versionConfig);

var versionCatalog = service.LoadCatalog(repoRoot, versionPath);
var versionDuelEntry = versionCatalog.Rules
    .FirstOrDefault(x => x.Id == "duel")?
    .PostprocessRules
    .FirstOrDefault(x => x.Tag == duelTag);
if (versionDuelEntry == null || versionDuelEntry.Tag != duelTag || versionDuelEntry.Description != overrideDescription)
{
    Console.Error.WriteLine("Saved tag prompt description override was not reloaded without changing the tag.");
    return 1;
}

var settings = new PromptLabSettings
{
    ApiUrl = "https://example.invalid/v1/chat/completions",
    ApiKey = "not-written-to-request",
    Model = "prompt-lab-smoke",
    Temperature = 0,
    MaxTokens = 5000,
    ThinkingEnabled = true,
    ReasoningEffort = "max"
};

var rendered = service.RenderPrompt(versionCatalog, cases[0], settings);
if (!rendered.TagRules.Contains(overrideDescription, StringComparison.Ordinal))
{
    Console.Error.WriteLine("Rendered tag rules did not use the edited tag prompt description.");
    return 1;
}

var actionEvidenceCase = new PromptLabCase
{
    CaseId = "action_text_preservation",
    PreprocessHits = new List<string> { "reward" },
    PlayerText = "（将家传剑递到对方掌中）这把剑归你。",
    NpcReplyText = "*郑重收下长剑* 我同意，交易已成。",
    HistoryLines = new List<string>
    {
        "玩家: （从桌上取出银币袋）我已经准备好报酬。",
        "NPC: *将契约推到你面前* 只要签字即可。"
    }
};
var actionEvidenceRendered = service.RenderPrompt(catalog, actionEvidenceCase, settings);
if (!actionEvidenceRendered.HistoryText.Contains("（从桌上取出银币袋）", StringComparison.Ordinal) ||
    !actionEvidenceRendered.HistoryText.Contains("*将契约推到你面前*", StringComparison.Ordinal) ||
    !actionEvidenceRendered.LatestReplyBlock.Contains("（将家传剑递到对方掌中）", StringComparison.Ordinal) ||
    !actionEvidenceRendered.LatestReplyBlock.Contains("*郑重收下长剑*", StringComparison.Ordinal))
{
    Console.Error.WriteLine("Postprocess history or latest reply removed role-play action text.");
    return 1;
}

if (cases.Count > 1)
{
    var secondRendered = service.RenderPrompt(versionCatalog, cases[1], settings);
    if (!secondRendered.TagRules.Contains(overrideDescription, StringComparison.Ordinal))
    {
        Console.Error.WriteLine("Edited tag prompt description was not global across cases.");
        return 1;
    }
}

using var requestDocument = JsonDocument.Parse(rendered.RequestJson);
if (!requestDocument.RootElement.TryGetProperty("messages", out var messages) || messages.GetArrayLength() != 2)
{
    Console.Error.WriteLine("Rendered request did not contain the expected two chat messages.");
    return 1;
}

if (!requestDocument.RootElement.TryGetProperty("thinking", out var thinking) ||
    !thinking.TryGetProperty("type", out var thinkingType) ||
    thinkingType.GetString() != "enabled")
{
    Console.Error.WriteLine("Rendered request did not enable thinking.");
    return 1;
}

if (!requestDocument.RootElement.TryGetProperty("reasoning_effort", out var reasoningEffort) ||
    reasoningEffort.GetString() != "max")
{
    Console.Error.WriteLine("Rendered request did not contain the expected reasoning_effort.");
    return 1;
}

if (rendered.RequestJson.Contains(settings.ApiKey, StringComparison.Ordinal))
{
    Console.Error.WriteLine("Request JSON leaked the API key.");
    return 1;
}

var anthropicSettings = new PromptLabSettings
{
    ApiUrl = "https://api.deepseek.com/anthropic",
    ApiKey = "not-written-to-request",
    Model = "deepseek-chat",
    Temperature = 0,
    MaxTokens = 20000,
    ThinkingEnabled = true,
    ReasoningEffort = "max"
};

var anthropicRendered = service.RenderPrompt(versionCatalog, cases[0], anthropicSettings);
using var anthropicRequestDocument = JsonDocument.Parse(anthropicRendered.RequestJson);
if (!anthropicRequestDocument.RootElement.TryGetProperty("system", out var anthropicSystem) ||
    anthropicSystem.GetString() != anthropicRendered.SystemPrompt)
{
    Console.Error.WriteLine("Anthropic request did not put the system prompt in the top-level system field.");
    return 1;
}

if (!anthropicRequestDocument.RootElement.TryGetProperty("messages", out var anthropicMessages) ||
    anthropicMessages.GetArrayLength() != 1 ||
    anthropicMessages[0].GetProperty("role").GetString() != "user")
{
    Console.Error.WriteLine("Anthropic request did not contain exactly one user message.");
    return 1;
}

if (!anthropicRequestDocument.RootElement.TryGetProperty("thinking", out var anthropicThinking) ||
    !anthropicThinking.TryGetProperty("budget_tokens", out var anthropicBudget) ||
    anthropicBudget.GetInt32() <= 0)
{
    Console.Error.WriteLine("Anthropic request did not contain a positive thinking budget.");
    return 1;
}

if (anthropicRendered.RequestJson.Contains(anthropicSettings.ApiKey, StringComparison.Ordinal))
{
    Console.Error.WriteLine("Anthropic request JSON leaked the API key.");
    return 1;
}

var runDir = tagOverrideRunDir;
service.WriteOfflineArtifacts(runDir, 1, catalog, cases[0], settings, "[ACTION:MOOD:NEUTRAL]");
var promptPath = Path.Combine(runDir, "001_" + cases[0].CaseId + ".prompt.txt");
var requestPath = Path.Combine(runDir, "001_" + cases[0].CaseId + ".request.json");
var responsePath = Path.Combine(runDir, "001_" + cases[0].CaseId + ".response.txt");
var metaPath = Path.Combine(runDir, "001_" + cases[0].CaseId + ".meta.json");
if (!File.Exists(promptPath) || !File.Exists(requestPath) || !File.Exists(responsePath) || !File.Exists(metaPath))
{
    Console.Error.WriteLine("Smoke run did not create prompt/request/response/meta files.");
    return 1;
}

var savedPromptText = File.ReadAllText(promptPath);
if (!savedPromptText.Contains("===== SYSTEM PROMPT =====", StringComparison.Ordinal) ||
    !savedPromptText.Contains("===== USER PROMPT =====", StringComparison.Ordinal) ||
    !savedPromptText.Contains("===== LATEST REPLY =====", StringComparison.Ordinal))
{
    Console.Error.WriteLine("Saved prompt text did not contain the expected rendered prompt sections.");
    return 1;
}

using var savedRequest = JsonDocument.Parse(File.ReadAllText(requestPath));
using var savedMeta = JsonDocument.Parse(File.ReadAllText(metaPath));
if (!savedMeta.RootElement.TryGetProperty("caseId", out var caseId) || caseId.GetString() != cases[0].CaseId)
{
    Console.Error.WriteLine("Saved meta did not contain the expected caseId.");
    return 1;
}

if (!savedMeta.RootElement.TryGetProperty("promptPath", out var savedPromptPath) ||
    savedPromptPath.GetString() != promptPath)
{
    Console.Error.WriteLine("Saved meta did not contain the expected promptPath.");
    return 1;
}

if (!savedRequest.RootElement.TryGetProperty("thinking", out var savedThinking) ||
    !savedThinking.TryGetProperty("type", out var savedThinkingType) ||
    savedThinkingType.GetString() != "enabled")
{
    Console.Error.WriteLine("Saved request did not keep thinking enabled.");
    return 1;
}

if (!savedMeta.RootElement.TryGetProperty("reasoningEffortSent", out var metaReasoningEffort) ||
    metaReasoningEffort.GetString() != "max")
{
    Console.Error.WriteLine("Saved meta did not contain the expected reasoning effort.");
    return 1;
}

Console.WriteLine("smoke-run: " + runDir);
return 0;
