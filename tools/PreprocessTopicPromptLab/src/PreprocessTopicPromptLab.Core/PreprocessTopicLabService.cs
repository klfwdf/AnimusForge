using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace PreprocessTopicPromptLab.Core;

public sealed class PreprocessTopicLabService
{
    public const int ModGuardrailReturnCap = 4;
    public const int ModAuxiliaryRouterMaxTokens = 5000;
    public const int LabSafeAuxiliaryRouterMaxTokens = 512;
    public const float ModAuxiliaryRouterTemperature = 0f;

    public static string DefaultSystemPrompt
    {
        get
        {
            try
            {
                var service = new PreprocessTopicLabService();
                var root = service.FindDefaultRepoRoot(Directory.GetCurrentDirectory());
                return service.LoadModulePreprocessPrompts(Path.Combine(root, "content", "modules", "AF.Module.Prompt", "ModuleData", "PreprocessPrompts.json")).StrictJson.SystemPrompt;
            }
            catch
            {
                return "";
            }
        }
    }

    public const string DefaultUserPromptTemplate =
        "MOD_SOURCE: AIConfigHandler.BuildAuxiliaryGuardrailRoutingPrompt";

    private static readonly string[] ApiProtocolValues = { "auto", "openai", "anthropic" };
    private static readonly string[] ReasoningEffortValues = { "low", "medium", "high", "xhigh", "max" };
    private static readonly Regex PreprocessTemplateVariableRegex = new("\\{([a-z][a-z0-9_]*)\\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> NonPreprocessInjectedTopicIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "scene_auto_group_relay"
    };
    private readonly JsonFileStore _json = new();
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    public string FindDefaultRepoRoot(string startDirectory)
    {
        var current = new DirectoryInfo(string.IsNullOrWhiteSpace(startDirectory) ? Directory.GetCurrentDirectory() : startDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AnimusForge", "ModuleData", "RuleBehaviorPrompts.json")) &&
                File.Exists(Path.Combine(current.FullName, "content", "modules", "AF.Module.Prompt", "ModuleData", "PreprocessPrompts.json")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    public string GetLabRoot(string repoRoot)
    {
        return Path.Combine(repoRoot, "tools", "PreprocessTopicPromptLab");
    }

    public PromptCatalog LoadCatalog(string repoRoot)
    {
        var root = string.IsNullOrWhiteSpace(repoRoot) ? FindDefaultRepoRoot(Directory.GetCurrentDirectory()) : repoRoot;
        var rulePath = Path.Combine(root, "AnimusForge", "ModuleData", "RuleBehaviorPrompts.json");
        var preprocessPath = Path.Combine(root, "content", "modules", "AF.Module.Prompt", "ModuleData", "PreprocessPrompts.json");
        if (!File.Exists(rulePath))
        {
            throw new FileNotFoundException("RuleBehaviorPrompts.json was not found.", rulePath);
        }
        if (!File.Exists(preprocessPath))
        {
            throw new FileNotFoundException("PreprocessPrompts.json was not found.", preprocessPath);
        }

        return new PromptCatalog
        {
            RepoRoot = root,
            RuleBehaviorPath = rulePath,
            PreprocessPromptsPath = preprocessPath,
            PreprocessPrompts = LoadModulePreprocessPrompts(preprocessPath),
            Rules = LoadRules(rulePath).Where(IsPreprocessInjectableRule).ToList()
        };
    }

    public ModulePreprocessPromptsConfig LoadModulePreprocessPrompts(string filePath)
    {
        var config = _json.Deserialize<ModulePreprocessPromptsConfig>(_json.ReadUtf8(filePath)) ?? new ModulePreprocessPromptsConfig();
        ValidateModulePreprocessPrompts(config);
        return config;
    }

    private static void ValidateModulePreprocessPrompts(ModulePreprocessPromptsConfig config)
    {
        RequirePreprocessValue(config?.StrictJson?.SystemPrompt, "StrictJson.SystemPrompt");
        var schema = config?.StrictJson?.MentionedEntitiesSchema;
        foreach (var bucket in new[] { "entities" })
        {
            if (schema == null || !schema.ContainsKey(bucket))
            {
                throw new InvalidDataException("PreprocessPrompts.json schema is missing: StrictJson.MentionedEntitiesSchema." + bucket);
            }
        }

        RequirePreprocessValue(config?.TopicRouting?.EmptyValue, "TopicRouting.EmptyValue");
        ValidateTemplateVariables(config?.TopicRouting?.UserPromptTemplate, "TopicRouting.UserPromptTemplate", "topic_list", "routing_guidance", "history", "latest_npc", "latest_player", "top_n", "mentioned_entities_schema");
        RequirePreprocessValue(config?.MemorySelection?.ParallelModeInstruction, "MemorySelection.ParallelModeInstruction");
        RequirePreprocessValue(config?.MemorySelection?.UnifiedModeInstruction, "MemorySelection.UnifiedModeInstruction");
        RequirePreprocessValue(config?.MemorySelection?.EmptyValue, "MemorySelection.EmptyValue");
        ValidateTemplateVariables(config?.MemorySelection?.UserPromptTemplate, "MemorySelection.UserPromptTemplate", "mode_instruction", "final_count", "latest_player_input", "latest_npc_input", "current_scene", "memory_candidates");
        ValidateTemplateVariables(config?.MemorySelection?.CandidateLineTemplate, "MemorySelection.CandidateLineTemplate", "memory_id", "game_date", "age_suffix", "hour_range", "rich_title");
        ValidateTemplateVariables(config?.MemorySelection?.FallbackGameDateTemplate, "MemorySelection.FallbackGameDateTemplate", "game_day");
        RequirePreprocessValue(config?.ConnectionTest?.ExpectedRuleCode, "ConnectionTest.ExpectedRuleCode");
        ValidateTemplateVariables(config?.ConnectionTest?.UserPromptTemplate, "ConnectionTest.UserPromptTemplate", "expected_rule_code", "mentioned_entities_schema");
    }

    private static string RequirePreprocessValue(string? value, string configPath)
    {
        var text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidDataException("PreprocessPrompts.json is missing: " + configPath);
        }

        return text;
    }

    private static void ValidateTemplateVariables(string? template, string configPath, params string[] requiredVariables)
    {
        var text = RequirePreprocessValue(template, configPath);
        var variables = PreprocessTemplateVariableRegex.Matches(text).Select(x => x.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        foreach (var requiredVariable in requiredVariables ?? Array.Empty<string>())
        {
            if (!variables.Contains(requiredVariable))
            {
                throw new InvalidDataException("PreprocessPrompts.json template is missing: " + configPath + ".{" + requiredVariable + "}");
            }
        }
    }

    private static string RenderPreprocessTemplate(string? template, string configPath, IReadOnlyDictionary<string, string> values)
    {
        var text = RequirePreprocessValue(template, configPath);
        return PreprocessTemplateVariableRegex.Replace(text, match =>
        {
            var key = match.Groups[1].Value;
            if (!values.TryGetValue(key, out var value))
            {
                throw new InvalidDataException("PreprocessPrompts.json template has an unknown or missing value: " + configPath + ".{" + key + "}");
            }

            return value ?? "";
        }).Trim();
    }

    public PreprocessPromptConfig GetDefaultPromptConfig()
    {
        return new PreprocessPromptConfig();
    }

    public PreprocessPromptConfig LoadPromptVersion(string filePath)
    {
        return _json.Deserialize<PreprocessPromptConfig>(_json.ReadUtf8(filePath)) ?? GetDefaultPromptConfig();
    }

    public void SavePromptVersion(string filePath, PreprocessPromptConfig config)
    {
        _json.WriteUtf8(filePath, _json.ToJson(config ?? GetDefaultPromptConfig()));
    }

    private static PromptCatalog CreateEffectiveCatalog(PromptCatalog catalog, PreprocessPromptConfig? promptConfig)
    {
        var result = new PromptCatalog
        {
            RepoRoot = catalog?.RepoRoot ?? "",
            RuleBehaviorPath = catalog?.RuleBehaviorPath ?? "",
            PreprocessPromptsPath = catalog?.PreprocessPromptsPath ?? "",
            PreprocessPrompts = catalog?.PreprocessPrompts ?? new ModulePreprocessPromptsConfig(),
            Rules = (catalog?.Rules ?? new List<TopicRuleInfo>()).Where(IsPreprocessInjectableRule).Select(CloneRule).ToList()
        };

        var overrides = promptConfig?.TopicOverrides;
        if (overrides == null || overrides.Count == 0)
        {
            return result;
        }

        foreach (var pair in overrides)
        {
            var key = (pair.Key ?? "").Trim();
            var value = pair.Value;
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                continue;
            }

            var normalizedKey = NormalizeTopicId(key);
            var normalizedCodeKey = NormalizeRuleCode(key, "", "");
            foreach (var rule in result.Rules.Where(x =>
                         string.Equals(x.Id, normalizedKey, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(NormalizeRuleCode(x.Code, x.Id, x.TopicLabel), normalizedCodeKey, StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrWhiteSpace(value.Code))
                {
                    rule.Code = value.Code.Trim();
                }

                if (!string.IsNullOrWhiteSpace(value.TopicLabel))
                {
                    rule.TopicLabel = value.TopicLabel.Trim();
                }

                if (value.IsEnabled.HasValue)
                {
                    rule.IsEnabled = value.IsEnabled.Value;
                }
            }
        }

        return result;
    }

    private static TopicRuleInfo CloneRule(TopicRuleInfo source)
    {
        return new TopicRuleInfo
        {
            Id = source.Id,
            Source = source.Source,
            TopicNumber = source.TopicNumber,
            TopicLabel = source.TopicLabel,
            Code = source.Code,
            IsEnabled = source.IsEnabled,
            Group = source.Group,
            Priority = source.Priority,
            Instruction = source.Instruction,
            NonHeroInstruction = source.NonHeroInstruction,
            TriggerKeywords = source.TriggerKeywords.ToList(),
            PostprocessRules = source.PostprocessRules
                .Select(x => new PostprocessRuleEntry
                {
                    Tag = x.Tag,
                    Description = x.Description,
                    SingleFramedNpcDescription = x.SingleFramedNpcDescription
                })
                .ToList()
        };
    }

    private static bool IsPreprocessInjectableRule(TopicRuleInfo rule)
    {
        return rule != null && IsPreprocessInjectableTopicId(rule.Id);
    }

    private static bool IsPreprocessInjectableTopicId(string? topicId)
    {
        var normalized = NormalizeTopicId(topicId);
        return string.IsNullOrWhiteSpace(normalized) || !NonPreprocessInjectedTopicIds.Contains(normalized);
    }

    public List<PreprocessLabCase> LoadCases(string filePath)
    {
        var result = new List<PreprocessLabCase>();
        if (!File.Exists(filePath))
        {
            return result;
        }

        foreach (var rawLine in _json.ReadUtf8(filePath).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var item = _json.Deserialize<PreprocessLabCase>(line);
            if (item != null)
            {
                result.Add(item);
            }
        }

        return result;
    }

    public void SaveCases(string filePath, IEnumerable<PreprocessLabCase> cases)
    {
        var lines = new List<string>();
        foreach (var item in cases ?? Enumerable.Empty<PreprocessLabCase>())
        {
            lines.Add(JsonSerializer.Serialize(item, JsonFileStore.JsonOptions));
        }

        _json.WriteUtf8(filePath, string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    public RenderedPreprocessPrompt RenderPrompt(
        PromptCatalog catalog,
        PreprocessLabCase labCase,
        PreprocessLabSettings settings,
        PreprocessPromptConfig? promptConfig = null)
    {
        var effectiveCatalog = CreateEffectiveCatalog(catalog, promptConfig);
        var topicRules = BuildTopicRulesText(effectiveCatalog.Rules, 0);
        var historyText = BuildHistoryText(labCase);
        var afefText = BuildAfefText(labCase.AfefFacts);
        var runtimeGuardrailContext = BuildRuntimeGuardrailContext(labCase, historyText, afefText);
        var rawUserPrompt = BuildAuxiliaryGuardrailRoutingPrompt(
            labCase.PlayerText,
            secondaryText: "",
            runtimeGuardrailContext,
            effectiveCatalog.Rules.ToList(),
            ModGuardrailReturnCap,
            effectiveCatalog.PreprocessPrompts,
            promptConfig?.RoutingGuidance);
        var rendered = new RenderedPreprocessPrompt
        {
            SystemPrompt = RequirePreprocessValue(effectiveCatalog.PreprocessPrompts?.StrictJson?.SystemPrompt, "StrictJson.SystemPrompt"),
            UserPrompt = NormalizeAuxiliaryRoutingRequestText(rawUserPrompt),
            TopicRules = topicRules,
            HistoryText = historyText,
            AfefText = afefText
        };
        rendered.RequestJson = BuildRequestJson(settings, rendered);
        return rendered;
    }

    public string CreateRunDirectory(string labRoot)
    {
        var runDir = Path.Combine(labRoot, "runs", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(runDir);
        return runDir;
    }

    public async Task<RunArtifact> RunCaseAsync(
        string runDir,
        int index,
        PromptCatalog catalog,
        PreprocessLabCase labCase,
        PreprocessLabSettings settings,
        PreprocessPromptConfig promptConfig,
        CancellationToken cancellationToken = default)
    {
        var effectiveCatalog = CreateEffectiveCatalog(catalog, promptConfig);
        var rendered = RenderPrompt(catalog, labCase, settings, promptConfig);
        var safeId = MakeSafeFileName(string.IsNullOrWhiteSpace(labCase.CaseId) ? "case" : labCase.CaseId);
        var prefix = index.ToString("000") + "_" + safeId;
        var promptPath = Path.Combine(runDir, prefix + ".prompt.txt");
        var requestPath = Path.Combine(runDir, prefix + ".request.json");
        var responsePath = Path.Combine(runDir, prefix + ".response.txt");
        var injectedRulesPath = Path.Combine(runDir, prefix + ".injected_rules.txt");
        var metaPath = Path.Combine(runDir, prefix + ".meta.json");

        _json.WriteUtf8(promptPath, FormatRenderedPromptText(rendered));
        _json.WriteUtf8(requestPath, rendered.RequestJson);

        ApiCallResult result;
        try
        {
            result = await CallApiAsync(settings, rendered.RequestJson, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result = new ApiCallResult
            {
                Success = false,
                Error = ex.GetType().Name + ": " + ex.Message
            };
        }

        var actualTopics = ParseTopics(result.AssistantText, effectiveCatalog.Rules);
        var score = ScoreTopics(labCase, actualTopics);
        _json.WriteUtf8(responsePath, string.IsNullOrWhiteSpace(result.AssistantText) ? result.Error : result.AssistantText);
        _json.WriteUtf8(injectedRulesPath, BuildInjectedRulePreview(effectiveCatalog, actualTopics));
        WriteMeta(metaPath, effectiveCatalog, labCase, settings, rendered, result, score, promptPath, requestPath, responsePath, injectedRulesPath);
        return new RunArtifact
        {
            CaseId = labCase.CaseId,
            PromptPath = promptPath,
            RequestPath = requestPath,
            ResponsePath = responsePath,
            InjectedRulesPath = injectedRulesPath,
            MetaPath = metaPath,
            Result = result,
            Score = score
        };
    }

    public RunArtifact WriteOfflineArtifacts(
        string runDir,
        int index,
        PromptCatalog catalog,
        PreprocessLabCase labCase,
        PreprocessLabSettings settings,
        PreprocessPromptConfig promptConfig,
        string responseText)
    {
        var effectiveCatalog = CreateEffectiveCatalog(catalog, promptConfig);
        var rendered = RenderPrompt(catalog, labCase, settings, promptConfig);
        var safeId = MakeSafeFileName(string.IsNullOrWhiteSpace(labCase.CaseId) ? "case" : labCase.CaseId);
        var prefix = index.ToString("000") + "_" + safeId;
        var promptPath = Path.Combine(runDir, prefix + ".prompt.txt");
        var requestPath = Path.Combine(runDir, prefix + ".request.json");
        var responsePath = Path.Combine(runDir, prefix + ".response.txt");
        var injectedRulesPath = Path.Combine(runDir, prefix + ".injected_rules.txt");
        var metaPath = Path.Combine(runDir, prefix + ".meta.json");
        var result = new ApiCallResult
        {
            Success = true,
            AssistantText = responseText ?? "",
            RawResponse = responseText ?? ""
        };
        var actualTopics = ParseTopics(result.AssistantText, effectiveCatalog.Rules);
        var score = ScoreTopics(labCase, actualTopics);

        _json.WriteUtf8(promptPath, FormatRenderedPromptText(rendered));
        _json.WriteUtf8(requestPath, rendered.RequestJson);
        _json.WriteUtf8(responsePath, result.AssistantText);
        _json.WriteUtf8(injectedRulesPath, BuildInjectedRulePreview(effectiveCatalog, actualTopics));
        WriteMeta(metaPath, effectiveCatalog, labCase, settings, rendered, result, score, promptPath, requestPath, responsePath, injectedRulesPath);
        return new RunArtifact
        {
            CaseId = labCase.CaseId,
            PromptPath = promptPath,
            RequestPath = requestPath,
            ResponsePath = responsePath,
            InjectedRulesPath = injectedRulesPath,
            MetaPath = metaPath,
            Result = result,
            Score = score
        };
    }

    public List<string> ParseTopics(string responseText, IEnumerable<TopicRuleInfo> rules)
    {
        TryParseTopics(responseText, rules, out var result, out var _);
        return result;
    }

    public bool TryParseTopics(string responseText, IEnumerable<TopicRuleInfo> rules, out List<string> result, out string error)
    {
        var ruleList = (rules ?? Enumerable.Empty<TopicRuleInfo>()).Where(IsPreprocessInjectableRule).ToList();
        var known = new HashSet<string>(ruleList
            .Select(x => NormalizeTopicId(x.Id))
            .Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
        var codeToId = BuildRuleCodeToIdMap(ruleList);
        result = new List<string>();
        error = "";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var text = (responseText ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "empty_content";
            return false;
        }

        return TryAddJsonTopics(text, known, codeToId, result, seen, out error);
    }

    private static Dictionary<string, string> BuildRuleCodeToIdMap(IEnumerable<TopicRuleInfo> rules)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules ?? Enumerable.Empty<TopicRuleInfo>())
        {
            var id = NormalizeTopicId(rule.Id);
            var code = NormalizeRuleCode(rule.Code, id, rule.TopicLabel);
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(code) && !result.ContainsKey(code))
            {
                result[code] = id;
            }
        }

        return result;
    }

    public TopicScoreResult ScoreTopics(PreprocessLabCase labCase, IEnumerable<string> actualTopics)
    {
        var expected = NormalizeTopicList(labCase.ExpectedTopics, excludeNonPreprocessTopics: true);
        var allowed = NormalizeTopicList(labCase.AllowedExtraTopics, excludeNonPreprocessTopics: true);
        var forbidden = NormalizeTopicList(labCase.ForbiddenTopics, excludeNonPreprocessTopics: true);
        var actual = NormalizeTopicList(actualTopics, excludeNonPreprocessTopics: true);
        var acceptable = new HashSet<string>(expected.Concat(allowed), StringComparer.OrdinalIgnoreCase);
        var missing = expected.Where(x => !actual.Contains(x, StringComparer.OrdinalIgnoreCase)).ToList();
        var unexpected = actual.Where(x => !acceptable.Contains(x)).ToList();
        var forbiddenHits = actual.Where(x => forbidden.Contains(x, StringComparer.OrdinalIgnoreCase)).ToList();
        var hitCount = expected.Count == 0 ? 0 : expected.Count(x => actual.Contains(x, StringComparer.OrdinalIgnoreCase));
        var validActualCount = actual.Count == 0 ? 0 : actual.Count(x => acceptable.Contains(x) && !forbidden.Contains(x, StringComparer.OrdinalIgnoreCase));
        return new TopicScoreResult
        {
            ExpectedTopics = expected,
            AllowedExtraTopics = allowed,
            ForbiddenTopics = forbidden,
            ActualTopics = actual,
            MissingTopics = missing,
            UnexpectedTopics = unexpected,
            ForbiddenHits = forbiddenHits,
            ExactMatch = missing.Count == 0 && unexpected.Count == 0 && forbiddenHits.Count == 0,
            Recall = expected.Count == 0 ? (actual.Count == 0 ? 1 : 0) : Math.Round(hitCount * 1.0 / expected.Count, 4),
            Precision = actual.Count == 0 ? (expected.Count == 0 ? 1 : 0) : Math.Round(validActualCount * 1.0 / actual.Count, 4)
        };
    }

    public string BuildInjectedRulePreview(PromptCatalog catalog, IEnumerable<string> topicIds)
    {
        var ids = NormalizeTopicList(topicIds, excludeNonPreprocessTopics: true);
        if (ids.Count == 0)
        {
            return "（无）";
        }

        var rulesById = (catalog?.Rules ?? new List<TopicRuleInfo>())
            .GroupBy(x => NormalizeTopicId(x.Id), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        foreach (var id in ids)
        {
            if (!rulesById.TryGetValue(id, out var rule))
            {
                sb.AppendLine("【未知话题:" + id + "】");
                continue;
            }

            sb.AppendLine("【话题:" + rule.Id + "】" + rule.DisplayName);
            if (!string.IsNullOrWhiteSpace(rule.Instruction))
            {
                sb.AppendLine(rule.Instruction.Trim());
            }

            if (!string.IsNullOrWhiteSpace(rule.NonHeroInstruction))
            {
                sb.AppendLine("【非英雄版本】" + rule.NonHeroInstruction.Trim());
            }

            if (rule.PostprocessRules.Count > 0)
            {
                sb.AppendLine("【后处理标签】" + string.Join("，", rule.PostprocessRules.Select(x => x.Tag).Where(x => !string.IsNullOrWhiteSpace(x))));
            }

            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    public string FormatRenderedPromptText(RenderedPreprocessPrompt rendered)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SYSTEM");
        sb.AppendLine(rendered.SystemPrompt ?? "");
        sb.AppendLine();
        sb.AppendLine("USER");
        sb.AppendLine(rendered.UserPrompt ?? "");
        return sb.ToString().Trim();
    }

    public static IReadOnlyList<string> GetApiProtocolOptions() => ApiProtocolValues;

    public static IReadOnlyList<string> GetReasoningEffortOptions() => ReasoningEffortValues;

    public static string NormalizeApiProtocolSelection(string? protocol)
    {
        var text = (protocol ?? "").Trim();
        foreach (var value in ApiProtocolValues)
        {
            if (string.Equals(text, value, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return "auto";
    }

    public static string NormalizeReasoningEffortSelection(string? effort)
    {
        var text = (effort ?? "").Trim();
        foreach (var value in ReasoningEffortValues)
        {
            if (string.Equals(text, value, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return "low";
    }

    private static List<TopicRuleInfo> LoadRules(string rulePath)
    {
        var rules = new List<TopicRuleInfo>();
        using var doc = JsonDocument.Parse(File.ReadAllText(rulePath, Encoding.UTF8), new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });
        var root = doc.RootElement;

        AddTopLevelRule(root, rules, "Duel", "duel", "DialogueInstruction", "DUEL");
        AddTopLevelRule(root, rules, "Reward", "reward", "Instruction", "TRADE");
        AddTopLevelRule(root, rules, "Loan", "loan", "Instruction", "LOAN");
        AddTopLevelRule(root, rules, "Surroundings", "surroundings", "Instruction", "NEARBY");

        if (root.TryGetProperty("RulePrompts", out var rulePrompts) && rulePrompts.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in rulePrompts.EnumerateArray())
            {
                var id = GetString(item, "Id").Trim();
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                rules.Add(new TopicRuleInfo
                {
                    Id = NormalizeTopicId(id),
                    Source = "RulePrompts",
                    TopicNumber = GetInt(item, "TopicNumber"),
                    TopicLabel = GetString(item, "TopicLabel"),
                    Code = GetString(item, "Code"),
                    IsEnabled = GetBool(item, "IsEnabled", true),
                    Group = GetString(item, "Group"),
                    Priority = GetInt(item, "Priority"),
                    Instruction = GetString(item, "Instruction"),
                    NonHeroInstruction = GetString(item, "NonHeroInstruction"),
                    TriggerKeywords = GetStringArray(item, "TriggerKeywords"),
                    PostprocessRules = ParsePostprocessRules(item)
                });
            }
        }

        return rules
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.TopicNumber <= 0 ? 999 : x.TopicNumber)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddTopLevelRule(JsonElement root, List<TopicRuleInfo> rules, string propertyName, string id, string instructionProperty, string fallbackCode)
    {
        if (!root.TryGetProperty(propertyName, out var item) || item.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var instruction = GetString(item, instructionProperty);
        if (string.Equals(propertyName, "Duel", StringComparison.OrdinalIgnoreCase))
        {
            var triggerInstruction = GetString(item, "TriggerInstruction");
            if (!string.IsNullOrWhiteSpace(triggerInstruction))
            {
                instruction = string.Join(Environment.NewLine, new[] { instruction, triggerInstruction }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            }
        }

        rules.Add(new TopicRuleInfo
        {
            Id = id,
            Source = propertyName,
            TopicNumber = GetInt(item, "TopicNumber"),
            TopicLabel = GetString(item, "TopicLabel"),
            Code = string.IsNullOrWhiteSpace(GetString(item, "Code")) ? fallbackCode : GetString(item, "Code"),
            IsEnabled = GetBool(item, "IsEnabled", true),
            Instruction = instruction,
            NonHeroInstruction = GetString(item, "NonHeroInstruction"),
            TriggerKeywords = GetStringArray(item, propertyName == "Duel" ? "AcceptKeywords" : "TriggerKeywords"),
            PostprocessRules = ParsePostprocessRules(item)
        });
    }

    private static List<PostprocessRuleEntry> ParsePostprocessRules(JsonElement element)
    {
        var result = new List<PostprocessRuleEntry>();
        if (!element.TryGetProperty("PostprocessRules", out var rules) || rules.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in rules.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var tag = GetString(item, "Tag");
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            result.Add(new PostprocessRuleEntry
            {
                Tag = tag.Trim(),
                Description = GetString(item, "Description").Trim()
            });
        }

        return result;
    }

    private static string BuildTopicRulesText(IEnumerable<TopicRuleInfo> rules, int instructionMaxChars)
    {
        _ = instructionMaxChars;
        var lines = new List<string>();
        foreach (var rule in (rules ?? Enumerable.Empty<TopicRuleInfo>()).Where(x => x.IsEnabled))
        {
            var code = NormalizeRuleCode(rule.Code, rule.Id, rule.TopicLabel);
            var label = (rule.TopicLabel ?? "").Trim();
            if (rule.TopicNumber <= 0 || string.IsNullOrWhiteSpace(rule.Id) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            lines.Add(code + ": " + label);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildRuntimeGuardrailContext(PreprocessLabCase labCase, string historyText, string afefText)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(labCase.RuntimeContext))
        {
            parts.Add(labCase.RuntimeContext.Trim());
        }

        if (!string.IsNullOrWhiteSpace(historyText))
        {
            parts.Add(historyText.Trim());
        }

        if (!string.IsNullOrWhiteSpace(afefText))
        {
            parts.Add(afefText.Trim());
        }

        return NormalizeGuardrailContextText(string.Join(Environment.NewLine, parts));
    }

    private static string BuildAuxiliaryGuardrailRoutingPrompt(
        string userText,
        string secondaryText,
        string runtimeGuardrailContext,
        List<TopicRuleInfo> rules,
        int topN,
        ModulePreprocessPromptsConfig preprocessPrompts,
        string? routingGuidance = null)
    {
        var text = NormalizeSemanticText(userText);
        var userTextIsAfefFact = IsAuxiliaryAfefFactLine(text);
        var routingRuntimeContext = userTextIsAfefFact ? AppendAuxiliaryAfefFactToRoutingContext(runtimeGuardrailContext, text) : runtimeGuardrailContext;
        var routingLatestPlayerText = userTextIsAfefFact ? "" : userText;
        var historyBlock = StripAuxiliaryHistoryInnerThoughts(BuildAuxiliaryGuardrailHistoryBlock(routingRuntimeContext, secondaryText, routingLatestPlayerText, out var latestNpcText));
        var latestNpcLine = StripAuxiliaryHistoryInnerThoughtsFromLine(NormalizeSemanticText(latestNpcText));
        var latestPlayerLine = userTextIsAfefFact ? "" : NormalizeAuxiliaryPlayerRoutingLine(text);
        var topicList = new StringBuilder();
        foreach (var rule in (rules ?? new List<TopicRuleInfo>()).Where(x => x.IsEnabled).OrderBy(x => x.TopicNumber <= 0 ? 999 : x.TopicNumber).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
        {
            var code = NormalizeRuleCode(rule.Code, rule.Id, rule.TopicLabel);
            var label = (rule.TopicLabel ?? "").Trim();
            if (rule.TopicNumber > 0 && !string.IsNullOrWhiteSpace(rule.Id) && !string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(label))
            {
                topicList.AppendLine(code + ": " + label);
            }
        }

        var config = preprocessPrompts?.TopicRouting ?? new ModuleTopicRoutingPreprocessPromptConfig();
        var emptyValue = RequirePreprocessValue(config.EmptyValue, "TopicRouting.EmptyValue");
        var guidance = string.IsNullOrWhiteSpace(routingGuidance) ? config.RoutingGuidance : routingGuidance;
        var mentionedEntitiesSchema = JsonSerializer.Serialize(preprocessPrompts?.StrictJson?.MentionedEntitiesSchema ?? new Dictionary<string, List<string>>());
        return RenderPreprocessTemplate(config.UserPromptTemplate, "TopicRouting.UserPromptTemplate", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["topic_list"] = topicList.ToString().TrimEnd(),
            ["routing_guidance"] = NormalizeAuxiliaryRoutingRequestText(guidance ?? ""),
            ["history"] = string.IsNullOrWhiteSpace(historyBlock) ? emptyValue : NormalizeAuxiliaryRoutingRequestText(historyBlock),
            ["latest_npc"] = string.IsNullOrWhiteSpace(latestNpcLine) ? emptyValue : NormalizeAuxiliaryRoutingRequestText(latestNpcLine),
            ["latest_player"] = string.IsNullOrWhiteSpace(latestPlayerLine) ? emptyValue : NormalizeAuxiliaryRoutingRequestText(latestPlayerLine),
            ["top_n"] = Math.Max(1, topN).ToString(CultureInfo.InvariantCulture),
            ["mentioned_entities_schema"] = mentionedEntitiesSchema
        });
    }

    private static string BuildHistoryText(PreprocessLabCase labCase)
    {
        var lines = new List<string>();
        foreach (var line in labCase.HistoryLines ?? new List<string>())
        {
            var text = (line ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                lines.Add(text);
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildAfefText(IEnumerable<AfefFact>? facts)
    {
        var lines = new List<string>();
        foreach (var fact in facts ?? Enumerable.Empty<AfefFact>())
        {
            var text = (fact.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var prefix = string.Equals((fact.Kind ?? "").Trim(), "npc", StringComparison.OrdinalIgnoreCase)
                ? "[AFEF NPC行为补充] "
                : "[AFEF玩家行为补充] ";
            lines.Add(prefix + text);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildAuxiliaryGuardrailHistoryBlock(string runtimeGuardrailContext, string secondaryText, string latestPlayerText, out string latestNpcText)
    {
        latestNpcText = "";
        var lines = new List<string>();
        try
        {
            AppendAuxiliaryDialogueHistoryLines(lines, runtimeGuardrailContext);
            var secondary = NormalizeSemanticText(secondaryText);
            if (!string.IsNullOrWhiteSpace(secondary) && !IsAuxiliarySceneShoutObserverLine(secondary))
            {
                latestNpcText = secondary;
            }

            if (string.IsNullOrWhiteSpace(latestNpcText))
            {
                latestNpcText = ExtractLatestAuxiliaryNpcUtterance(lines, latestPlayerText);
            }

            TrimAuxiliaryLatestDialogueLines(lines, latestNpcText, latestPlayerText);
        }
        catch
        {
        }

        if (lines.Count <= 0)
        {
            return "(none)";
        }

        var count = Math.Max(3, Math.Min(6, lines.Count));
        if (lines.Count > count)
        {
            lines = lines.Skip(lines.Count - count).ToList();
        }

        return StripAuxiliaryHistoryInnerThoughts(string.Join("\n", lines));
    }

    private static string StripAuxiliaryHistoryInnerThoughts(string historyBlock)
    {
        var text = (historyBlock ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var sb = new StringBuilder(text.Length);
        foreach (var line in text.Split('\n'))
        {
            var cleaned = StripAuxiliaryHistoryInnerThoughtsFromLine(line);
            if (!string.IsNullOrWhiteSpace(cleaned))
            {
                sb.AppendLine(cleaned);
            }
        }

        return Regex.Replace(sb.ToString().Trim(), "[ \\t]{2,}", " ", RegexOptions.CultureInvariant);
    }

    private static string StripAuxiliaryHistoryInnerThoughtsFromLine(string line)
    {
        var text = (line ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        if (IsAuxiliaryAfefFactLine(text))
        {
            return text;
        }

        if (IsAuxiliaryPlayerHistoryLine(text))
        {
            return NormalizeAuxiliaryPlayerRoutingLine(text);
        }

        const string noneAscii = "\u0001AF_NONE_ASCII\u0001";
        const string noneCn = "\u0001AF_NONE_CN\u0001";
        text = text.Replace("(none)", noneAscii).Replace("（无）", noneCn);
        text = RemoveAuxiliaryInnerThoughtSegments(text, '（', '）');
        text = RemoveAuxiliaryInnerThoughtSegments(text, '(', ')');
        text = Regex.Replace(text, "[ \\t]{2,}", " ", RegexOptions.CultureInvariant);
        return text.Replace(noneAscii, "(none)").Replace(noneCn, "（无）").Trim();
    }

    private static string RemoveAuxiliaryInnerThoughtSegments(string text, char open, char close)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        var sb = new StringBuilder(text.Length);
        var depth = 0;
        foreach (var ch in text)
        {
            if (ch == open)
            {
                depth++;
                continue;
            }

            if (ch == close && depth > 0)
            {
                depth--;
                continue;
            }

            if (depth <= 0)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private static void TrimAuxiliaryLatestDialogueLines(List<string> lines, string latestNpcText, string latestPlayerText)
    {
        try
        {
            if (lines == null || lines.Count <= 0)
            {
                return;
            }

            var npc = NormalizeSemanticText(latestNpcText);
            var player = NormalizeSemanticText(latestPlayerText);
            var removed = 0;
            for (var i = lines.Count - 1; i >= 0 && removed < 2; i--)
            {
                var line = lines[i];
                var npcMatch = !string.IsNullOrWhiteSpace(npc) && IsAuxiliaryHistoryUtteranceMatch(line, npc);
                var playerMatch = !string.IsNullOrWhiteSpace(player) && IsAuxiliaryHistoryUtteranceMatch(line, player);
                if (npcMatch || playerMatch)
                {
                    lines.RemoveAt(i);
                    removed++;
                }
            }
        }
        catch
        {
        }
    }

    private static bool IsAuxiliaryHistoryUtteranceMatch(string line, string utterance)
    {
        var text = NormalizeSemanticText(utterance);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var lineText = NormalizeSemanticText(line);
        if (string.IsNullOrWhiteSpace(lineText))
        {
            return false;
        }

        return string.Equals(lineText, text, StringComparison.Ordinal) ||
            string.Equals(ExtractAuxiliaryHistoryUtterance(lineText), text, StringComparison.Ordinal);
    }

    private static string ExtractAuxiliaryHistoryUtterance(string line)
    {
        var text = NormalizeSemanticText(line);
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        const string cnPrefix = "上一句NPC发言：";
        if (text.StartsWith(cnPrefix, StringComparison.Ordinal))
        {
            return NormalizeSemanticText(text.Substring(cnPrefix.Length));
        }

        const string enPrefix = "Previous NPC line:";
        if (text.StartsWith(enPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return NormalizeSemanticText(text.Substring(enPrefix.Length));
        }

        var splitAt = FindAuxiliaryHistorySpeakerDelimiter(text);
        if (splitAt >= 0 && splitAt + 1 < text.Length)
        {
            return NormalizeSemanticText(text.Substring(splitAt + 1));
        }

        return text;
    }

    private static bool IsAuxiliaryPlayerHistoryLine(string line)
    {
        var text = NormalizeSemanticText(line);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var prefix = GetAuxiliaryHistorySpeakerPrefix(text);
        return prefix.Equals("玩家", StringComparison.OrdinalIgnoreCase) ||
            prefix.Equals("你", StringComparison.OrdinalIgnoreCase) ||
            prefix.Equals("Player", StringComparison.OrdinalIgnoreCase) ||
            prefix.Equals("You", StringComparison.OrdinalIgnoreCase) ||
            prefix.EndsWith(" says to you", StringComparison.OrdinalIgnoreCase) ||
            (prefix.Contains("对") && prefix.EndsWith("说", StringComparison.Ordinal));
    }

    private static string GetAuxiliaryHistorySpeakerPrefix(string line)
    {
        var text = NormalizeSemanticText(line);
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var splitAt = FindAuxiliaryHistorySpeakerDelimiter(text);
        if (splitAt >= 0)
        {
            var start = GetAuxiliaryHistorySpeakerSearchStart(text);
            return start < splitAt ? text.Substring(start, splitAt - start).Trim() : text.Substring(0, splitAt).Trim();
        }

        var fallbackStart = GetAuxiliaryHistorySpeakerSearchStart(text);
        return fallbackStart < text.Length ? text.Substring(fallbackStart).Trim() : text.Trim();
    }

    private static int FindAuxiliaryHistorySpeakerDelimiter(string line)
    {
        var text = NormalizeSemanticText(line);
        if (string.IsNullOrWhiteSpace(text))
        {
            return -1;
        }

        var start = GetAuxiliaryHistorySpeakerSearchStart(text);
        var first = text.IndexOfAny(new[] { ':', '：' }, start);
        return first >= 0 ? first : text.IndexOfAny(new[] { ':', '：' });
    }

    private static int GetAuxiliaryHistorySpeakerSearchStart(string line)
    {
        var text = NormalizeSemanticText(line);
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        if (!text.StartsWith("[", StringComparison.Ordinal) &&
            !text.StartsWith("AF_SCENE_SESSION", StringComparison.Ordinal) &&
            !text.StartsWith("_SCENE_SESSION", StringComparison.Ordinal) &&
            !text.StartsWith("SCENE_SESSION", StringComparison.Ordinal))
        {
            return 0;
        }

        var end = text.IndexOf(']');
        return end >= 0 && end + 1 < text.Length && end <= 64 ? end + 1 : 0;
    }

    private static void AppendAuxiliaryDialogueHistoryLines(List<string> lines, string block, bool allowNewDialogueRecords = true)
    {
        if (lines == null)
        {
            return;
        }

        var text = NormalizeGuardrailContextText(block);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        foreach (var record in SplitAuxiliaryDialogueHistoryRecords(text))
        {
            AppendAuxiliaryDialogueHistoryLine(lines, record, allowNewDialogueRecords);
        }
    }

    private static List<string> SplitAuxiliaryDialogueHistoryRecords(string block)
    {
        var result = new List<string>();
        var text = (block ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        var sb = new StringBuilder();
        foreach (var raw in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = NormalizeSemanticText(raw);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (sb.Length <= 0 || IsAuxiliaryDialogueHistoryRecordStart(line))
            {
                if (sb.Length > 0)
                {
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                }

                sb.Append(line);
            }
            else
            {
                sb.Append(' ').Append(line);
            }
        }

        if (sb.Length > 0)
        {
            result.Add(sb.ToString().Trim());
        }

        return result;
    }

    private static bool IsAuxiliaryDialogueHistoryRecordStart(string line)
    {
        var text = NormalizeSemanticText(line);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (IsAuxiliaryAfefFactLine(text) || IsAuxiliarySceneShoutObserverLine(text) ||
            text.StartsWith("上一句NPC发言：", StringComparison.Ordinal) ||
            text.StartsWith("Previous NPC line:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (text.StartsWith("“", StringComparison.Ordinal) || text.StartsWith("\"", StringComparison.Ordinal) || text.StartsWith("'", StringComparison.Ordinal))
        {
            return false;
        }

        var splitAt = FindAuxiliaryHistorySpeakerDelimiter(text);
        if (splitAt <= 0 || splitAt > 96)
        {
            return false;
        }

        var prefix = GetAuxiliaryHistorySpeakerPrefix(text);
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length > 64)
        {
            return false;
        }

        if (IsAuxiliaryPlayerHistoryLine(text))
        {
            return true;
        }

        if (prefix.Equals("NPC", StringComparison.OrdinalIgnoreCase) ||
            prefix.Equals("Assistant", StringComparison.OrdinalIgnoreCase) ||
            prefix.Equals("系统", StringComparison.OrdinalIgnoreCase) ||
            prefix.Equals("System", StringComparison.OrdinalIgnoreCase) ||
            prefix.Equals("旁白", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return prefix.IndexOfAny(new[] { '。', '！', '？', '；', '，', ',', '.', '!', '?', ';', '“', '”', '"', '\'', '（', '(' }) < 0;
    }

    private static void AppendAuxiliaryDialogueHistoryLine(List<string> lines, string line, bool allowNewDialogueRecords = true)
    {
        if (lines == null)
        {
            return;
        }

        var text = NormalizeSemanticText(line);
        if (string.IsNullOrWhiteSpace(text) || !IsAuxiliaryDialogueHistoryLine(text) || lines.Contains(text))
        {
            return;
        }

        if (!allowNewDialogueRecords && IsAuxiliaryHistoryDialogueRecord(text))
        {
            return;
        }

        lines.Add(text);
    }

    private static bool IsAuxiliaryDialogueHistoryLine(string line)
    {
        var text = (line ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (IsAuxiliaryAfefFactLine(text))
        {
            return true;
        }

        return !text.StartsWith("vanilla_issue:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAuxiliaryHistoryDialogueRecord(string line)
    {
        var prefix = GetAuxiliaryHistorySpeakerPrefix(line);
        return IsAuxiliaryPlayerHistoryLine(line) ||
            prefix.Equals("NPC", StringComparison.OrdinalIgnoreCase) ||
            prefix.Equals("Assistant", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractLatestAuxiliaryNpcUtterance(List<string> lines, string latestPlayerText)
    {
        try
        {
            var playerText = NormalizeSemanticText(latestPlayerText);
            if (lines == null || lines.Count <= 0)
            {
                return "";
            }

            for (var i = lines.Count - 1; i >= 0; i--)
            {
                var line = NormalizeSemanticText(lines[i]);
                if (string.IsNullOrWhiteSpace(line) || IsAuxiliaryPlayerHistoryLine(line) || IsAuxiliarySceneShoutObserverLine(line) || IsAuxiliaryAfefFactLine(line))
                {
                    continue;
                }

                var utterance = ExtractAuxiliaryHistoryUtterance(line);
                if (!string.IsNullOrWhiteSpace(playerText) && string.Equals(utterance, playerText, StringComparison.Ordinal))
                {
                    continue;
                }

                return utterance;
            }
        }
        catch
        {
        }

        return "";
    }

    private static bool IsAuxiliarySceneShoutObserverLine(string line)
    {
        var text = NormalizeSemanticText(line);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var start = GetAuxiliaryHistorySpeakerSearchStart(text);
        if (start > 0 && start < text.Length)
        {
            text = text.Substring(start).TrimStart();
        }

        return text.StartsWith("[场景喊话]", StringComparison.Ordinal);
    }

    private static bool IsAuxiliaryAfefFactLine(string line)
    {
        var text = (line ?? "").Trim();
        return text.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal) ||
            text.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal);
    }

    private static string AppendAuxiliaryAfefFactToRoutingContext(string context, string afefLine)
    {
        var text = NormalizeSemanticText(afefLine);
        if (!IsAuxiliaryAfefFactLine(text))
        {
            return NormalizeGuardrailContextText(context);
        }

        var current = NormalizeGuardrailContextText(context);
        if (string.IsNullOrWhiteSpace(current))
        {
            return text;
        }

        foreach (var line in current.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals((line ?? "").Trim(), text, StringComparison.Ordinal))
            {
                return current;
            }
        }

        return current.TrimEnd() + "\n" + text;
    }

    private static string NormalizeAuxiliaryPlayerRoutingLine(string line)
    {
        var text = NormalizeSemanticText(line);
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        text = Regex.Replace(text, "[ \\t]{2,}", " ", RegexOptions.CultureInvariant);
        return text.Trim();
    }

    private static string NormalizeAuxiliaryRoutingRequestText(string text)
    {
        try
        {
            var value = text ?? "";
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            value = value.Replace("（无）", "(none)")
                .Replace("玩家对你说：", "Player says to you: ")
                .Replace("玩家对你说:", "Player says to you: ")
                .Replace("玩家:", "Player:")
                .Replace("玩家：", "Player:")
                .Replace("你:", "Player:")
                .Replace("你：", "Player:")
                .Replace("上一句NPC发言：", "Previous NPC line: ")
                .Replace("[系统事实]", "[System fact]")
                .Replace("某NPC", "NPC");
            return value;
        }
        catch
        {
            return text ?? "";
        }
    }

    private static string NormalizeGuardrailContextText(string text)
    {
        return (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    }

    private static string NormalizeSemanticText(string text)
    {
        return Regex.Replace((text ?? "").Replace("\r\n", "\n").Replace('\r', '\n'), "[ \\t]+", " ", RegexOptions.CultureInvariant).Trim();
    }

    private static string ResolveApiProtocol(PreprocessLabSettings? settings)
    {
        var selected = NormalizeApiProtocolSelection(settings?.ApiProtocol);
        if (!string.Equals(selected, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return selected;
        }

        var apiUrl = (settings?.ApiUrl ?? "").Trim();
        return apiUrl.IndexOf("/anthropic", StringComparison.OrdinalIgnoreCase) >= 0 ? "anthropic" : "openai";
    }

    private static string BuildRequestJson(PreprocessLabSettings settings, RenderedPreprocessPrompt rendered)
    {
        var openAiPayload = BuildOpenAiStyleRouterPayload(settings, rendered);
        return string.Equals(ResolveApiProtocol(settings), "anthropic", StringComparison.OrdinalIgnoreCase)
            ? ConvertOpenAiChatPayloadToAnthropic(openAiPayload).ToJsonString(JsonFileStore.JsonOptions)
            : openAiPayload.ToJsonString(JsonFileStore.JsonOptions);
    }

    private static JsonObject BuildOpenAiStyleRouterPayload(PreprocessLabSettings settings, RenderedPreprocessPrompt rendered)
    {
        var thinkingEnabled = settings?.ThinkingEnabled ?? false;
        var payload = new JsonObject
        {
            ["model"] = settings?.Model ?? "",
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = rendered.SystemPrompt ?? ""
                },
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = rendered.UserPrompt ?? ""
                }
            },
            ["stream"] = false,
            ["max_tokens"] = ResolveAuxiliaryRouterMaxTokens(settings),
            ["temperature"] = (double)ModAuxiliaryRouterTemperature
        };
        payload["thinking"] = new JsonObject
        {
            ["type"] = thinkingEnabled ? "enabled" : "disabled"
        };
        if (thinkingEnabled)
        {
            payload["reasoning_effort"] = NormalizeReasoningEffortSelection(settings?.ReasoningEffort);
        }

        return payload;
    }

    private static int ResolveAuxiliaryRouterMaxTokens(PreprocessLabSettings? settings)
    {
        var configured = settings?.MaxTokens > 0 ? settings.MaxTokens : LabSafeAuxiliaryRouterMaxTokens;
        var upperBound = settings?.ThinkingEnabled ?? false
            ? ModAuxiliaryRouterMaxTokens
            : LabSafeAuxiliaryRouterMaxTokens;
        return Math.Clamp(configured, 64, upperBound);
    }

    private static JsonObject ConvertOpenAiChatPayloadToAnthropic(JsonObject payload)
    {
        var result = new JsonObject
        {
            ["model"] = payload["model"]?.GetValue<string>() ?? "",
            ["max_tokens"] = payload["max_tokens"]?.GetValue<int>() ?? LabSafeAuxiliaryRouterMaxTokens
        };
        if (payload["temperature"] != null)
        {
            result["temperature"] = Math.Clamp(payload["temperature"]?.GetValue<double>() ?? 0d, 0d, 1d);
        }

        var systemBlocks = new List<string>();
        var messages = new JsonArray();
        if (payload["messages"] is JsonArray sourceMessages)
        {
            foreach (var source in sourceMessages.OfType<JsonObject>())
            {
                var role = (source["role"]?.GetValue<string>() ?? "user").Trim().ToLowerInvariant();
                var content = ExtractJsonContentText(source["content"]);
                if (role == "system" || role == "developer")
                {
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        systemBlocks.Add(content.Trim());
                    }

                    continue;
                }

                AddAnthropicMessage(messages, role == "assistant" ? "assistant" : "user", content);
            }
        }

        if (messages.Count == 0)
        {
            AddAnthropicMessage(messages, "user", " ");
        }

        if (systemBlocks.Count > 0)
        {
            result["system"] = string.Join("\n\n", systemBlocks);
        }

        result["messages"] = messages;
        ApplyAnthropicThinking(payload, result);
        return result;
    }

    private static string ExtractJsonContentText(JsonNode? node)
    {
        if (node == null)
        {
            return "";
        }

        if (node is JsonValue)
        {
            return node.GetValue<string>() ?? "";
        }

        if (node is JsonArray array)
        {
            var parts = new List<string>();
            foreach (var item in array)
            {
                if (item is JsonObject obj)
                {
                    var text = obj["text"]?.GetValue<string>() ?? obj["content"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        parts.Add(text);
                    }
                }
                else if (item is JsonValue value)
                {
                    var text = value.GetValue<string>() ?? "";
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        parts.Add(text);
                    }
                }
            }

            return string.Join(Environment.NewLine, parts);
        }

        return node.ToJsonString(JsonFileStore.JsonOptions);
    }

    private static void AddAnthropicMessage(JsonArray messages, string role, string content)
    {
        var normalizedRole = role == "assistant" ? "assistant" : "user";
        var text = string.IsNullOrEmpty(content) ? " " : content;
        if (messages.Count > 0 && messages[messages.Count - 1] is JsonObject last &&
            string.Equals(last["role"]?.GetValue<string>(), normalizedRole, StringComparison.OrdinalIgnoreCase))
        {
            var previous = last["content"]?.GetValue<string>() ?? "";
            last["content"] = string.IsNullOrWhiteSpace(previous) ? text : previous + "\n\n" + text;
            return;
        }

        messages.Add(new JsonObject
        {
            ["role"] = normalizedRole,
            ["content"] = text
        });
    }

    private static void ApplyAnthropicThinking(JsonObject source, JsonObject target)
    {
        var type = source["thinking"]?["type"]?.GetValue<string>() ?? "";
        if (string.Equals(type, "disabled", StringComparison.OrdinalIgnoreCase))
        {
            target["thinking"] = new JsonObject
            {
                ["type"] = "disabled"
            };
            return;
        }

        if (!string.Equals(type, "enabled", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var maxTokens = target["max_tokens"]?.GetValue<int>() ?? ModAuxiliaryRouterMaxTokens;
        if (maxTokens < 2048)
        {
            return;
        }

        var effort = (source["reasoning_effort"]?.GetValue<string>() ?? "").Trim().ToLowerInvariant();
        var preferredBudget = effort == "max" || effort == "xhigh" ? 4096 : 1024;
        var budget = Math.Min(preferredBudget, Math.Max(1024, maxTokens / 2));
        if (budget >= maxTokens)
        {
            budget = Math.Max(1024, maxTokens - 1024);
        }

        if (budget < 1024 || budget >= maxTokens)
        {
            return;
        }

        target["thinking"] = new JsonObject
        {
            ["type"] = "enabled",
            ["budget_tokens"] = budget
        };
    }

    private async Task<ApiCallResult> CallApiAsync(PreprocessLabSettings settings, string requestJson, CancellationToken cancellationToken)
    {
        if (settings == null || string.IsNullOrWhiteSpace(settings.ApiUrl) || string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.Model))
        {
            return new ApiCallResult
            {
                Success = false,
                Error = "api_config_incomplete"
            };
        }

        var protocol = ResolveApiProtocol(settings);
        using var request = new HttpRequestMessage(HttpMethod.Post, NormalizeApiUrl(settings.ApiUrl, protocol));
        if (string.Equals(protocol, "anthropic", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.TryAddWithoutValidation("x-api-key", settings.ApiKey.Trim());
            request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
            if (!IsOfficialAnthropicHost(settings.ApiUrl))
            {
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + settings.ApiKey.Trim());
            }
        }
        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
        }

        request.Content = new StringContent(requestJson ?? "", Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var result = new ApiCallResult
        {
            Success = response.IsSuccessStatusCode,
            StatusCode = (int)response.StatusCode,
            RawResponse = raw
        };
        PopulateUsage(result, raw);
        if (!response.IsSuccessStatusCode)
        {
            result.Error = "http_" + (int)response.StatusCode;
            return result;
        }

        result.AssistantText = string.Equals(protocol, "anthropic", StringComparison.OrdinalIgnoreCase)
            ? ExtractAnthropicAssistantText(raw)
            : ExtractOpenAiAssistantText(raw);
        if (string.IsNullOrWhiteSpace(result.AssistantText))
        {
            result.Success = false;
            result.Error = "empty_content";
        }

        return result;
    }

    private static void PopulateUsage(ApiCallResult result, string raw)
    {
        if (result == null || string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("usage", out var usage) ||
                usage.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            result.InputTokens = ReadUsageInt(usage, "input_tokens");
            result.CacheCreationInputTokens = ReadUsageInt(usage, "cache_creation_input_tokens");
            result.CacheReadInputTokens = ReadUsageInt(usage, "cache_read_input_tokens");
            result.OutputTokens = ReadUsageInt(usage, "output_tokens");
            result.PromptTokens = ReadUsageInt(usage, "prompt_tokens");
            result.CompletionTokens = ReadUsageInt(usage, "completion_tokens");
            result.TotalTokens = ReadUsageInt(usage, "total_tokens");
            if (result.TotalTokens <= 0)
            {
                var anthropicTotal = result.InputTokens + result.CacheCreationInputTokens + result.CacheReadInputTokens + result.OutputTokens;
                var openAiTotal = result.PromptTokens + result.CompletionTokens;
                result.TotalTokens = anthropicTotal > 0 ? anthropicTotal : openAiTotal;
            }
        }
        catch
        {
            return;
        }
    }

    private static int ReadUsageInt(JsonElement usage, string propertyName)
    {
        if (!usage.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static string NormalizeApiUrl(string apiUrl, string protocol)
    {
        var text = (apiUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        if (string.Equals(protocol, "anthropic", StringComparison.OrdinalIgnoreCase))
        {
            if (text.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase) ||
                text.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }

            if (text.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return text + "/messages";
            }

            if (text.EndsWith("/v1/", StringComparison.OrdinalIgnoreCase))
            {
                return text.TrimEnd('/') + "/messages";
            }

            return text.TrimEnd('/') + "/v1/messages";
        }

        if (text.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        if (text.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            return text + "/chat/completions";
        }

        if (text.EndsWith("/v1/", StringComparison.OrdinalIgnoreCase))
        {
            return text.TrimEnd('/') + "/chat/completions";
        }

        return text.TrimEnd('/') + "/v1/chat/completions";
    }

    private static bool IsOfficialAnthropicHost(string apiUrl)
    {
        try
        {
            return Uri.TryCreate((apiUrl ?? "").Trim(), UriKind.Absolute, out var uri) &&
                string.Equals(uri.Host ?? "", "api.anthropic.com", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string ExtractOpenAiAssistantText(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw ?? "");
            if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                foreach (var choice in choices.EnumerateArray())
                {
                    if (choice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content) &&
                        content.ValueKind == JsonValueKind.String)
                    {
                        return content.GetString()?.Trim() ?? "";
                    }
                }
            }
        }
        catch
        {
            return "";
        }

        return "";
    }

    private static string ExtractAnthropicAssistantText(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw ?? "");
            if (doc.RootElement.TryGetProperty("content", out var content))
            {
                if (content.ValueKind == JsonValueKind.String)
                {
                    return content.GetString()?.Trim() ?? "";
                }

                if (content.ValueKind == JsonValueKind.Array)
                {
                    var parts = new List<string>();
                    foreach (var item in content.EnumerateArray())
                    {
                        if (item.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                        {
                            var value = text.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                parts.Add(value.Trim());
                            }
                        }
                    }

                    return string.Join(Environment.NewLine, parts).Trim();
                }
            }
        }
        catch
        {
            return "";
        }

        return "";
    }

    private void WriteMeta(
        string metaPath,
        PromptCatalog catalog,
        PreprocessLabCase labCase,
        PreprocessLabSettings settings,
        RenderedPreprocessPrompt rendered,
        ApiCallResult result,
        TopicScoreResult score,
        string promptPath,
        string requestPath,
        string responsePath,
        string injectedRulesPath)
    {
        var meta = new JsonObject
        {
            ["timestamp"] = DateTimeOffset.Now.ToString("O"),
            ["caseId"] = labCase.CaseId ?? "",
            ["title"] = labCase.Title ?? "",
            ["promptVersionPath"] = settings?.PromptVersionPath ?? "",
            ["model"] = settings?.Model ?? "",
            ["apiUrl"] = settings?.ApiUrl ?? "",
            ["apiProtocol"] = NormalizeApiProtocolSelection(settings?.ApiProtocol),
            ["resolvedApiProtocol"] = ResolveApiProtocol(settings),
            ["resolvedApiUrl"] = NormalizeApiUrl(settings?.ApiUrl ?? "", ResolveApiProtocol(settings)),
            ["thinkingEnabled"] = settings?.ThinkingEnabled ?? false,
            ["reasoningEffort"] = NormalizeReasoningEffortSelection(settings?.ReasoningEffort),
            ["promptPath"] = promptPath,
            ["requestPath"] = requestPath,
            ["responsePath"] = responsePath,
            ["injectedRulesPath"] = injectedRulesPath,
            ["success"] = result.Success,
            ["statusCode"] = result.StatusCode,
            ["error"] = result.Error ?? "",
            ["assistantText"] = result.AssistantText ?? "",
            ["inputTokens"] = result.InputTokens,
            ["cacheCreationInputTokens"] = result.CacheCreationInputTokens,
            ["cacheReadInputTokens"] = result.CacheReadInputTokens,
            ["outputTokens"] = result.OutputTokens,
            ["promptTokens"] = result.PromptTokens,
            ["completionTokens"] = result.CompletionTokens,
            ["totalTokens"] = result.TotalTokens,
            ["rawResponseChars"] = result.RawResponse?.Length ?? 0,
            ["rawResponsePreview"] = result.Success ? "" : Shorten(result.RawResponse ?? "", 4000),
            ["expectedTopics"] = JsonSerializer.SerializeToNode(score.ExpectedTopics, JsonFileStore.JsonOptions),
            ["allowedExtraTopics"] = JsonSerializer.SerializeToNode(score.AllowedExtraTopics, JsonFileStore.JsonOptions),
            ["forbiddenTopics"] = JsonSerializer.SerializeToNode(score.ForbiddenTopics, JsonFileStore.JsonOptions),
            ["actualTopics"] = JsonSerializer.SerializeToNode(score.ActualTopics, JsonFileStore.JsonOptions),
            ["missingTopics"] = JsonSerializer.SerializeToNode(score.MissingTopics, JsonFileStore.JsonOptions),
            ["unexpectedTopics"] = JsonSerializer.SerializeToNode(score.UnexpectedTopics, JsonFileStore.JsonOptions),
            ["forbiddenHits"] = JsonSerializer.SerializeToNode(score.ForbiddenHits, JsonFileStore.JsonOptions),
            ["exactMatch"] = score.ExactMatch,
            ["recall"] = score.Recall,
            ["precision"] = score.Precision,
            ["topicRules"] = rendered.TopicRules ?? "",
            ["ruleBehaviorPath"] = catalog.RuleBehaviorPath ?? "",
            ["preprocessPromptsPath"] = catalog.PreprocessPromptsPath ?? ""
        };
        _json.WriteUtf8(metaPath, meta.ToJsonString(JsonFileStore.JsonOptions));
    }

    private static bool TryAddJsonTopics(string text, HashSet<string> known, Dictionary<string, string> codeToId, List<string> result, HashSet<string> seen, out string error)
    {
        error = "";
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "root_not_object";
                return false;
            }
            if (!root.TryGetProperty("rule_codes", out var ruleCodes))
            {
                error = "missing_rule_codes";
                return false;
            }
            if (ruleCodes.ValueKind != JsonValueKind.Array)
            {
                error = "rule_codes_not_array";
                return false;
            }
            if (!root.TryGetProperty("mentioned_entities", out var mentionedEntities))
            {
                error = "missing_mentioned_entities";
                return false;
            }
            if (mentionedEntities.ValueKind != JsonValueKind.Object)
            {
                error = "mentioned_entities_not_object";
                return false;
            }
            foreach (var property in mentionedEntities.EnumerateObject())
            {
                if (!string.Equals(property.Name, "entities", StringComparison.Ordinal))
                {
                    error = "mentioned_entities_unexpected_field_" + property.Name;
                    return false;
                }
            }
            foreach (var item in ruleCodes.EnumerateArray())
            {
                var code = item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : "";
                if (item.ValueKind != JsonValueKind.String)
                {
                    error = "rule_codes_item_not_string";
                    return false;
                }
                if (Regex.IsMatch(code.Trim(), "^(?:[0-9]+|TOPIC_[0-9]+|T[0-9]+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    error = "numeric_rule_code_not_allowed";
                    return false;
                }
            }
            foreach (var bucket in new[] { "entities" })
            {
                if (!mentionedEntities.TryGetProperty(bucket, out var values))
                {
                    error = "missing_mentioned_entities_" + bucket;
                    return false;
                }
                if (values.ValueKind != JsonValueKind.Array)
                {
                    error = "mentioned_entities_" + bucket + "_not_array";
                    return false;
                }
                foreach (var item in values.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        error = "mentioned_entities_" + bucket + "_item_not_string";
                        return false;
                    }
                }
            }
            AddJsonTopicElement(ruleCodes, known, codeToId, result, seen);
            return true;
        }
        catch (Exception ex)
        {
            error = "invalid_json:" + ex.GetType().Name;
            return false;
        }
    }

    private static void AddJsonTopicElement(JsonElement element, HashSet<string> known, Dictionary<string, string> codeToId, List<string> result, HashSet<string> seen)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    AddTopic(item.GetString(), known, codeToId, result, seen);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            AddTopic(element.GetString(), known, codeToId, result, seen);
        }
    }

    private static void AddTopic(string? raw, HashSet<string> known, Dictionary<string, string> codeToId, List<string> result, HashSet<string> seen)
    {
        var id = NormalizeTopicId(raw ?? "");
        if (known.Contains(id) && seen.Add(id))
        {
            result.Add(id);
            return;
        }

        var code = NormalizeRuleCode(raw ?? "", "", "");
        if (codeToId.TryGetValue(code, out var ruleId) && seen.Add(ruleId))
        {
            result.Add(ruleId);
        }
    }

    private static List<string> NormalizeTopicList(IEnumerable<string>? values, bool excludeNonPreprocessTopics = false)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values ?? Enumerable.Empty<string>())
        {
            var normalized = NormalizeTopicId(value);
            if (excludeNonPreprocessTopics && !IsPreprocessInjectableTopicId(normalized))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(normalized) && !string.Equals(normalized, "none", StringComparison.OrdinalIgnoreCase) && seen.Add(normalized))
            {
                result.Add(normalized);
            }
        }

        return result;
    }

    private static string NormalizeTopicId(string? value)
    {
        return (value ?? "").Trim().Trim('[', ']', '"', '\'', '`').ToLowerInvariant();
    }

    private static string NormalizeRuleCode(string? code, string? id, string? label = null)
    {
        var text = (code ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            var ruleId = (id ?? "").Trim().ToLowerInvariant();
            text = ruleId switch
            {
                "duel" => "DUEL",
				"reward" => "ASSET_TRANSFER",
                "loan" => "DEBT",
                "surroundings" => "NEARBY",
                "kingdom_service" => "KINGDOM",
                "lords_hall_access" => "PASSAGE",
                "marriage" => "MARRIAGE",
                "scene_mechanism_actions" => "SCENE_MOVE",
                "party_transfer" => "PARTY_TRANSFER",
                "vanilla_issue" => "ISSUE",
                "npc_major_actions" => "NPC_MAJOR",
                "encounter_release_player" => "MEETING_RELEASE",
                "hero_join_party" => "HERO_JOIN",
                "noble_deference" => "NOBLE_PRESSURE",
                "kingdom_agenda" => "KINGDOM_AGENDA",
                "diplomacy" => "DIPLOMACY",
                _ => ""
            };
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            text = (label ?? id ?? "RULE").Trim();
        }

        text = Regex.Replace(text.ToUpperInvariant(), "[^A-Z0-9_]+", "_", RegexOptions.CultureInvariant).Trim('_');
        return string.IsNullOrWhiteSpace(text) ? "RULE" : text;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : 0;
    }

    private static bool GetBool(JsonElement element, string propertyName, bool fallback)
    {
        return element.TryGetProperty(propertyName, out var value) && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            ? value.GetBoolean()
            : fallback;
    }

    private static List<string> GetStringArray(JsonElement element, string propertyName)
    {
        var result = new List<string>();
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var text = item.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    result.Add(text.Trim());
                }
            }
        }

        return result;
    }

    private static string NormalizeOneLine(string text)
    {
        return Regex.Replace((text ?? "").Replace("\r\n", "\n").Replace('\r', '\n'), "\\s+", " ", RegexOptions.CultureInvariant).Trim();
    }

    private static string Shorten(string text, int maxChars)
    {
        var value = (text ?? "").Trim();
        if (value.Length <= maxChars)
        {
            return value;
        }

        return value.Substring(0, Math.Max(0, maxChars)).Trim() + "...";
    }

    private static string MakeSafeFileName(string text)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (var ch in text ?? "")
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }

        var value = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(value) ? "case" : value;
    }

    private static string CollapseBlankLines(string text)
    {
        return Regex.Replace((text ?? "").Trim(), "(\\r?\\n){3,}", Environment.NewLine + Environment.NewLine, RegexOptions.CultureInvariant);
    }
}
