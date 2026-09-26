using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ActionPostprocessPromptLab.Core;

public sealed class PromptLabService
{
    private const int ActionPostprocessMaxHistoryAndLatestEntries = 8;
    private static readonly string[] ApiProtocolValues = { "auto", "openai", "anthropic" };
    private static readonly string[] ReasoningEffortValues = { "low", "medium", "high", "xhigh", "max" };
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
            if (File.Exists(Path.Combine(current.FullName, "content", "modules", "AF.Module.Prompt", "ModuleData", "ActionPostprocessPrompts.json")) &&
                File.Exists(Path.Combine(current.FullName, "content", "modules", "AF.Module.Prompt", "ModuleData", "RuleBehaviorPrompts.json")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return "";
    }

    public string ResolveLabRoot(string repoRoot)
    {
        return Path.Combine(repoRoot, "tools", "ActionPostprocessPromptLab");
    }

    public PromptCatalog LoadCatalog(string repoRoot, string actionPostprocessPath = "")
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            throw new InvalidOperationException("Repository root is empty.");
        }

        var rulePath = Path.Combine(repoRoot, "content", "modules", "AF.Module.Prompt", "ModuleData", "RuleBehaviorPrompts.json");
        var actionPath = string.IsNullOrWhiteSpace(actionPostprocessPath)
            ? Path.Combine(repoRoot, "content", "modules", "AF.Module.Prompt", "ModuleData", "ActionPostprocessPrompts.json")
            : actionPostprocessPath;

        if (!File.Exists(rulePath))
        {
            throw new FileNotFoundException("RuleBehaviorPrompts.json was not found.", rulePath);
        }

        if (!File.Exists(actionPath))
        {
            throw new FileNotFoundException("ActionPostprocessPrompts.json was not found.", actionPath);
        }

        var actionConfig = _json.Deserialize<ActionPostprocessConfigModel>(_json.ReadUtf8(actionPath)) ?? new ActionPostprocessConfigModel();
        var rules = LoadRules(rulePath, actionConfig);
        ApplyPostprocessDescriptionOverrides(rules, actionConfig.RulePostprocessDescriptionOverrides);
        return new PromptCatalog
        {
            RepoRoot = repoRoot,
            RuleBehaviorPath = rulePath,
            ActionPostprocessPath = actionPath,
            ActionConfig = actionConfig,
            Rules = rules
        };
    }

    public List<PromptLabCase> LoadCases(string caseFile)
    {
        var result = new List<PromptLabCase>();
        if (!File.Exists(caseFile))
        {
            return result;
        }

        foreach (var rawLine in File.ReadLines(caseFile, new UTF8Encoding(false, true)))
        {
            var line = (rawLine ?? "").Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var item = JsonSerializer.Deserialize<PromptLabCase>(line, JsonFileStore.JsonOptions);
            if (item != null)
            {
                NormalizeCase(item);
                result.Add(item);
            }
        }

        return result;
    }

    public void SaveCases(string caseFile, IEnumerable<PromptLabCase> cases)
    {
        var sb = new StringBuilder();
        foreach (var item in cases ?? Array.Empty<PromptLabCase>())
        {
            NormalizeCase(item);
            sb.AppendLine(JsonSerializer.Serialize(item, JsonFileStore.JsonOptions));
        }

        _json.WriteUtf8(caseFile, sb.ToString());
    }

    public PromptLabSettings LoadSettings(string labRoot)
    {
        var path = Path.Combine(labRoot, "local.settings.json");
        if (!File.Exists(path))
        {
            return new PromptLabSettings();
        }

        return _json.Deserialize<PromptLabSettings>(_json.ReadUtf8(path)) ?? new PromptLabSettings();
    }

    public void SaveSettings(string labRoot, PromptLabSettings settings)
    {
        _json.WriteUtf8(Path.Combine(labRoot, "local.settings.json"), _json.ToJson(settings ?? new PromptLabSettings()));
    }

    public RenderedPrompt RenderPrompt(PromptCatalog catalog, PromptLabCase labCase, PromptLabSettings settings, ActionPostprocessConfigModel? overrideConfig = null)
    {
        if (catalog == null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (labCase == null)
        {
            throw new ArgumentNullException(nameof(labCase));
        }

        var config = overrideConfig ?? catalog.ActionConfig ?? new ActionPostprocessConfigModel();
        var tagRules = BuildTagRules(catalog, labCase.PreprocessHits);
        var moodRules = BuildRuleText(config.MoodRules);
        var latestReply = BuildLatestReplyBlock(labCase.PlayerText, labCase.NpcReplyText);
        var historyText = PrepareHistoryText(BuildHistoryText(labCase), latestReply);
        var systemPrompt = BuildSystemPrompt(config.SystemPrompt, tagRules, moodRules);
        var userPrompt = BuildUserPrompt(
            config.UserPromptTemplate,
            tagRules,
            historyText,
            latestReply,
            labCase.RuntimeContext,
            labCase.PreprocessHits);

        var rendered = new RenderedPrompt
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            LatestReplyBlock = latestReply,
            HistoryText = historyText,
            TagRules = tagRules,
            MoodRules = moodRules
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
        PromptLabCase labCase,
        PromptLabSettings settings,
        ActionPostprocessConfigModel? overrideConfig = null,
        CancellationToken cancellationToken = default)
    {
        var rendered = RenderPrompt(catalog, labCase, settings, overrideConfig);
        var safeId = MakeSafeFileName(string.IsNullOrWhiteSpace(labCase.CaseId) ? "case" : labCase.CaseId);
        var prefix = index.ToString("000") + "_" + safeId;
        var promptPath = Path.Combine(runDir, prefix + ".prompt.txt");
        var requestPath = Path.Combine(runDir, prefix + ".request.json");
        var responsePath = Path.Combine(runDir, prefix + ".response.txt");
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

        _json.WriteUtf8(responsePath, string.IsNullOrWhiteSpace(result.AssistantText) ? result.Error : result.AssistantText);
        WriteMeta(metaPath, catalog, labCase, settings, rendered, result, promptPath, requestPath, responsePath);
        return new RunArtifact
        {
            CaseId = labCase.CaseId,
            PromptPath = promptPath,
            RequestPath = requestPath,
            ResponsePath = responsePath,
            MetaPath = metaPath,
            Result = result
        };
    }

    public void WriteOfflineArtifacts(
        string runDir,
        int index,
        PromptCatalog catalog,
        PromptLabCase labCase,
        PromptLabSettings settings,
        string responseText,
        ActionPostprocessConfigModel? overrideConfig = null)
    {
        var rendered = RenderPrompt(catalog, labCase, settings, overrideConfig);
        var safeId = MakeSafeFileName(string.IsNullOrWhiteSpace(labCase.CaseId) ? "case" : labCase.CaseId);
        var prefix = index.ToString("000") + "_" + safeId;
        var promptPath = Path.Combine(runDir, prefix + ".prompt.txt");
        var requestPath = Path.Combine(runDir, prefix + ".request.json");
        var responsePath = Path.Combine(runDir, prefix + ".response.txt");
        var metaPath = Path.Combine(runDir, prefix + ".meta.json");
        var result = new ApiCallResult
        {
            Success = true,
            AssistantText = responseText ?? "",
            RawResponse = responseText ?? ""
        };

        _json.WriteUtf8(promptPath, FormatRenderedPromptText(rendered));
        _json.WriteUtf8(requestPath, rendered.RequestJson);
        _json.WriteUtf8(responsePath, result.AssistantText);
        WriteMeta(metaPath, catalog, labCase, settings, rendered, result, promptPath, requestPath, responsePath);
    }

    public ActionPostprocessConfigModel CloneActionConfigWithPrompts(ActionPostprocessConfigModel source, string systemPrompt, string userPromptTemplate)
    {
        var json = _json.ToJson(source ?? new ActionPostprocessConfigModel());
        var clone = _json.Deserialize<ActionPostprocessConfigModel>(json) ?? new ActionPostprocessConfigModel();
        clone.SystemPrompt = systemPrompt ?? "";
        clone.UserPromptTemplate = userPromptTemplate ?? "";
        return clone;
    }

    public ActionPostprocessConfigModel CloneActionConfigWithPromptsAndRuleDescriptions(
        ActionPostprocessConfigModel source,
        string systemPrompt,
        string userPromptTemplate,
        IEnumerable<PromptRuleInfo> rules)
    {
        var clone = CloneActionConfigWithPrompts(source, systemPrompt, userPromptTemplate);
        clone.RulePostprocessDescriptionOverrides = BuildPostprocessDescriptionOverrides(rules);
        return clone;
    }

    public void SavePromptVersion(string filePath, ActionPostprocessConfigModel config)
    {
        _json.WriteUtf8(filePath, _json.ToJson(config ?? new ActionPostprocessConfigModel()));
    }

    public ActionPostprocessConfigModel LoadPromptVersion(string filePath)
    {
        return _json.Deserialize<ActionPostprocessConfigModel>(_json.ReadUtf8(filePath)) ?? new ActionPostprocessConfigModel();
    }

    public string FormatJson(string json)
    {
        var node = _json.ParseNode(json);
        return node?.ToJsonString(JsonFileStore.JsonOptions) ?? "";
    }

    public static string FormatRenderedPromptText(RenderedPrompt rendered)
    {
        if (rendered == null)
        {
            return "";
        }

        var sb = new StringBuilder();
        AppendSection(sb, "SYSTEM PROMPT", rendered.SystemPrompt);
        AppendSection(sb, "USER PROMPT", rendered.UserPrompt);
        AppendSection(sb, "TAG RULES", rendered.TagRules);
        AppendSection(sb, "MOOD RULES", rendered.MoodRules);
        AppendSection(sb, "HISTORY", rendered.HistoryText);
        AppendSection(sb, "LATEST REPLY", rendered.LatestReplyBlock);
        return sb.ToString().TrimEnd();
    }

    public static IReadOnlyList<string> GetReasoningEffortOptions()
    {
        return ReasoningEffortValues;
    }

    public static string NormalizeReasoningEffortSelection(string? effort)
    {
        var text = (effort ?? "").Trim().ToLowerInvariant();
        foreach (var value in ReasoningEffortValues)
        {
            if (string.Equals(value, text, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return "max";
    }

    public static string NormalizeReasoningEffortForRequest(string? effort)
    {
        return NormalizeReasoningEffortSelection(effort);
    }

    private static List<PromptRuleInfo> LoadRules(string rulePath, ActionPostprocessConfigModel actionConfig)
    {
        var rules = new List<PromptRuleInfo>();
        using var doc = JsonDocument.Parse(File.ReadAllText(rulePath, Encoding.UTF8), new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return rules;
        }

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (property.NameEquals("DuelStake") || property.NameEquals("KnowledgeRetrieval"))
            {
                continue;
            }

            var postRules = ParsePostprocessRules(property.Value);
            if (postRules.Count == 0)
            {
                continue;
            }

            var id = NormalizeTopLevelRuleId(property.Name);
            rules.Add(new PromptRuleInfo
            {
                Id = id,
                Source = property.Name,
                IsEnabled = GetBool(property.Value, "IsEnabled", true),
                TopicNumber = GetInt(property.Value, "TopicNumber"),
                TopicLabel = GetString(property.Value, "TopicLabel"),
                Code = GetString(property.Value, "Code"),
                PostprocessRules = postRules
            });
        }

        if (doc.RootElement.TryGetProperty("RulePrompts", out var rulePrompts) && rulePrompts.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in rulePrompts.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var postRules = ParsePostprocessRules(item);
                if (postRules.Count == 0)
                {
                    continue;
                }

                var id = GetString(item, "Id");
                if (string.IsNullOrWhiteSpace(id))
                {
                    id = GetString(item, "TopicLabel");
                }

                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                rules.Add(new PromptRuleInfo
                {
                    Id = id.Trim(),
                    Source = "RulePrompts",
                    IsEnabled = GetBool(item, "IsEnabled", true),
                    TopicNumber = GetInt(item, "TopicNumber"),
                    TopicLabel = GetString(item, "TopicLabel"),
                    Code = GetString(item, "Code"),
                    PostprocessRules = postRules
                });
            }
        }

        if ((actionConfig.WildernessPostprocessRules?.Count ?? 0) > 0)
        {
            rules.Add(new PromptRuleInfo
            {
                Id = "action:wilderness",
                Source = "ActionPostprocessPrompts",
                TopicLabel = "Wilderness postprocess rules",
                PostprocessRules = actionConfig.WildernessPostprocessRules ?? new List<PostprocessRuleEntry>()
            });
        }

        if ((actionConfig.RoyalPostprocessRules?.Count ?? 0) > 0)
        {
            rules.Add(new PromptRuleInfo
            {
                Id = "action:royal",
                Source = "ActionPostprocessPrompts",
                TopicLabel = "Royal postprocess rules",
                PostprocessRules = actionConfig.RoyalPostprocessRules ?? new List<PostprocessRuleEntry>()
            });
        }

        return rules
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.TopicNumber == 0 ? int.MaxValue : x.TopicNumber)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private static Dictionary<string, Dictionary<string, string>> BuildPostprocessDescriptionOverrides(IEnumerable<PromptRuleInfo> rules)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules ?? Array.Empty<PromptRuleInfo>())
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Id))
            {
                continue;
            }

            var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in rule.PostprocessRules ?? new List<PostprocessRuleEntry>())
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Tag))
                {
                    continue;
                }

                descriptions[entry.Tag.Trim()] = entry.Description ?? "";
            }

            if (descriptions.Count > 0)
            {
                result[rule.Id.Trim()] = descriptions;
            }
        }

        return result;
    }

    private static void AppendSection(StringBuilder sb, string title, string? content)
    {
        if (sb.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine();
        }

        sb.AppendLine("===== " + title + " =====");
        sb.Append(content ?? "");
    }

    private static void ApplyPostprocessDescriptionOverrides(
        IEnumerable<PromptRuleInfo> rules,
        Dictionary<string, Dictionary<string, string>>? overrides)
    {
        if (overrides == null || overrides.Count == 0)
        {
            return;
        }

        foreach (var rule in rules ?? Array.Empty<PromptRuleInfo>())
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Id))
            {
                continue;
            }

            if (!overrides.TryGetValue(rule.Id.Trim(), out var descriptions) || descriptions == null || descriptions.Count == 0)
            {
                continue;
            }

            foreach (var entry in rule.PostprocessRules ?? new List<PostprocessRuleEntry>())
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Tag))
                {
                    continue;
                }

                if (descriptions.TryGetValue(entry.Tag.Trim(), out var description))
                {
                    entry.Description = description ?? "";
                }
            }
        }
    }

    private string BuildTagRules(PromptCatalog catalog, IEnumerable<string> preprocessHits)
    {
        var selected = new HashSet<string>((preprocessHits ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
        var merged = new List<PostprocessRuleEntry>();
        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in catalog.Rules)
        {
            if (selected.Contains(rule.Id) || selected.Contains(rule.Source) || selected.Contains(rule.TopicLabel))
            {
                foreach (var entry in rule.PostprocessRules)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Tag))
                    {
                        continue;
                    }

                    if (seenTags.Add(entry.Tag.Trim()))
                    {
                        merged.Add(entry);
                    }
                }
            }
        }

        return BuildRuleText(merged);
    }

    private static string BuildRuleText(IEnumerable<PostprocessRuleEntry>? entries)
    {
        if (entries == null)
        {
            return "";
        }

        var sb = new StringBuilder();
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Tag))
            {
                continue;
            }

            sb.Append(entry.Tag.Trim());
            if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                sb.Append('：').Append(entry.Description.Trim());
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildSystemPrompt(string template, string tagRules, string moodRules)
    {
        var text = template ?? "";
        text = text.Replace("{tag_rules}", string.IsNullOrWhiteSpace(tagRules) ? "（无）" : tagRules.Trim())
            .Replace("{mood_rules}", string.IsNullOrWhiteSpace(moodRules) ? "（无）" : moodRules.Trim())
            .Replace("{npc_name}", "NPC");
        return CollapseBlankLines(text.Trim());
    }

    private static string BuildUserPrompt(
        string template,
        string tagRules,
        string historyText,
        string latestReplyBlock,
        string runtimeContext,
        IEnumerable<string>? preprocessHits)
    {
        var text = template ?? "";
        var hitText = string.Join(", ", (preprocessHits ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim()));
        text = ReplaceOptionalSection(text, "玩家可见装备：", "player_item_list", null);
        text = ReplaceOptionalSection(text, "{npc_name}的物品清单：", "shared_item_list", null);
        text = ReplaceOptionalSection(text, "玩家家族可婚配未婚成员（事实清单）：", "marriage_player_candidates", null);
        text = ReplaceOptionalSection(text, "对方家族可婚配未婚成员（事实清单）：", "marriage_target_candidates", null);
        text = ReplaceOptionalSection(text, "债务提示：", "debt_hint", null);
        text = ReplaceOptionalSection(text, "运行时补充事实：", "runtime_context", runtimeContext);
        text = text.Replace("{tag_rules}", string.IsNullOrWhiteSpace(tagRules) ? "（无）" : tagRules.Trim())
            .Replace("{history}", string.IsNullOrWhiteSpace(historyText) ? "（无）" : historyText.Trim())
            .Replace("{reply}", string.IsNullOrWhiteSpace(latestReplyBlock) ? "玩家: （无）\nNPC: （无）" : latestReplyBlock.Trim())
            .Replace("{preprocess_hits}", string.IsNullOrWhiteSpace(hitText) ? "（无）" : hitText)
            .Replace("{npc_name}", "NPC");
        return CollapseBlankLines(text.Trim());
    }

    private static string ReplaceOptionalSection(string template, string titleLine, string tokenName, string? value)
    {
        var text = template ?? "";
        var token = "{" + tokenName + "}";
        var normalized = NormalizeOptionalValue(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (!string.IsNullOrWhiteSpace(titleLine))
            {
                var pattern = @"(?:\r?\n){0,2}" + Regex.Escape(titleLine) + @"\r?\n" + Regex.Escape(token) + @"(?:\r?\n)?";
                text = Regex.Replace(text, pattern, "", RegexOptions.CultureInvariant);
            }

            return text.Replace(token, "");
        }

        return text.Replace(token, normalized);
    }

    private static string NormalizeOptionalValue(string? value)
    {
        var text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text) || text == "（无）" || text == "(none)" || text == "none")
        {
            return "";
        }

        return text;
    }

    private static string BuildHistoryText(PromptLabCase labCase)
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

        foreach (var fact in labCase.AfefFacts ?? new List<AfefFact>())
        {
            var text = (fact?.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var prefix = string.Equals((fact?.Kind ?? "").Trim(), "npc", StringComparison.OrdinalIgnoreCase)
                ? "[AFEF NPC行为补充]"
                : "[AFEF玩家行为补充]";
            lines.Add(prefix + " " + text);
        }

        return string.Join("\n", lines);
    }

    private static string PrepareHistoryText(string historyText, string latestReplyBlock)
    {
        var text = (historyText ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var latestEntries = CountLatestReplyEntries(latestReplyBlock);
        var maxEntries = Math.Max(0, ActionPostprocessMaxHistoryAndLatestEntries - latestEntries);
        if (maxEntries <= 0)
        {
            return "";
        }

        var latestKeys = BuildEntryKeys(latestReplyBlock);
        var entries = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in text.Split('\n'))
        {
            var normalized = NormalizeHistoryLine(line);
            if (string.IsNullOrWhiteSpace(normalized) || !IsHistoryEntryStart(normalized))
            {
                continue;
            }

            var key = BuildEntryKey(normalized);
            if (string.IsNullOrWhiteSpace(key) || latestKeys.Contains(key))
            {
                continue;
            }

            if (seen.Add(key))
            {
                entries.Add(normalized);
            }
        }

        if (entries.Count > maxEntries)
        {
            entries = entries.Skip(entries.Count - maxEntries).ToList();
        }

        return Regex.Replace(string.Join("\n", entries).Trim(), "[ \\t]{2,}", " ", RegexOptions.CultureInvariant);
    }

    private static string BuildLatestReplyBlock(string playerText, string npcReplyText)
    {
        return ("玩家: " + (string.IsNullOrWhiteSpace(playerText) ? "（无）" : NormalizeDialogueText(playerText)) + "\n" +
                "NPC: " + (string.IsNullOrWhiteSpace(npcReplyText) ? "（无）" : NormalizeDialogueText(npcReplyText))).Trim();
    }

    private static string NormalizeDialogueText(string text)
    {
        return Regex.Replace((text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim(), "\\s+", " ", RegexOptions.CultureInvariant).Trim();
    }

    private static string NormalizeHistoryLine(string line)
    {
        var text = NormalizeHistoryContent((line ?? "").Trim());
        text = Regex.Replace(text, "^【[^】]*对(?:你|NPC|[^】]+)说】\\s*", "玩家: ", RegexOptions.CultureInvariant);
        text = Regex.Replace(text, "\\s+", " ", RegexOptions.CultureInvariant).Trim();
        if (text.StartsWith("【AFEF玩家行为补充】", StringComparison.Ordinal))
        {
            text = "[AFEF玩家行为补充] " + text.Substring("【AFEF玩家行为补充】".Length).Trim();
        }

        if (text.StartsWith("【AFEF NPC行为补充】", StringComparison.Ordinal))
        {
            text = "[AFEF NPC行为补充] " + text.Substring("【AFEF NPC行为补充】".Length).Trim();
        }

        return text;
    }

    private static string NormalizeHistoryContent(string line)
    {
        var text = line ?? "";
        if (text.StartsWith("【", StringComparison.Ordinal) && text.EndsWith("】", StringComparison.Ordinal))
        {
            return text;
        }

        // Role-play action prose is evidence for postprocess tag selection.
        // Preserve it in both history and latest-reply rendering.
        return text.Trim().TrimStart('，', '。', '、', '；', '：', ',', ';', ':').Trim();
    }

    private static bool IsHistoryEntryStart(string line)
    {
        var text = (line ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (text.StartsWith("[AFEF", StringComparison.Ordinal))
        {
            return true;
        }

        var idx = text.IndexOfAny(new[] { ':', '：' });
        if (idx <= 0)
        {
            return false;
        }

        var speaker = text.Substring(0, idx).Trim();
        return speaker.Equals("玩家", StringComparison.OrdinalIgnoreCase) ||
               speaker.Equals("NPC", StringComparison.OrdinalIgnoreCase) ||
               speaker.Equals("你", StringComparison.OrdinalIgnoreCase) ||
               speaker.Contains("对", StringComparison.Ordinal);
    }

    private static int CountLatestReplyEntries(string latestReplyBlock)
    {
        var count = 0;
        foreach (var line in (latestReplyBlock ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var text = (line ?? "").Trim();
            var idx = text.IndexOfAny(new[] { ':', '：' });
            if (idx <= 0 || idx + 1 >= text.Length)
            {
                continue;
            }

            var speaker = text.Substring(0, idx).Trim();
            var body = text.Substring(idx + 1).Trim();
            if (!string.IsNullOrWhiteSpace(body) && body != "（无）" &&
                (speaker.Equals("玩家", StringComparison.OrdinalIgnoreCase) || speaker.Equals("NPC", StringComparison.OrdinalIgnoreCase)))
            {
                count++;
            }
        }

        return count;
    }

    private static HashSet<string> BuildEntryKeys(string latestReplyBlock)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in (latestReplyBlock ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var key = BuildEntryKey(line);
            if (!string.IsNullOrWhiteSpace(key))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private static string BuildEntryKey(string line)
    {
        var text = NormalizeHistoryLine(line);
        var idx = text.IndexOfAny(new[] { ':', '：' });
        if (idx >= 0 && idx + 1 < text.Length)
        {
            text = text.Substring(idx + 1).Trim();
        }

        return Regex.Replace(text, "\\s+", " ", RegexOptions.CultureInvariant).Trim();
    }

    public static IReadOnlyList<string> GetApiProtocolOptions()
    {
        return ApiProtocolValues;
    }

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

    private static string ResolveApiProtocol(PromptLabSettings? settings)
    {
        var selected = NormalizeApiProtocolSelection(settings?.ApiProtocol);
        if (!string.Equals(selected, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return selected;
        }

        var apiUrl = (settings?.ApiUrl ?? "").Trim();
        return apiUrl.IndexOf("/anthropic", StringComparison.OrdinalIgnoreCase) >= 0
            ? "anthropic"
            : "openai";
    }

    private static string BuildRequestJson(PromptLabSettings settings, RenderedPrompt rendered)
    {
        return string.Equals(ResolveApiProtocol(settings), "anthropic", StringComparison.OrdinalIgnoreCase)
            ? BuildAnthropicRequestJson(settings, rendered)
            : BuildOpenAiRequestJson(settings, rendered);
    }

    private static string BuildOpenAiRequestJson(PromptLabSettings settings, RenderedPrompt rendered)
    {
        var thinkingEnabled = settings?.ThinkingEnabled ?? true;
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
            ["max_tokens"] = Math.Max(16, settings?.MaxTokens ?? 5000),
            ["temperature"] = settings?.Temperature ?? 0f
        };
        payload["thinking"] = new JsonObject
        {
            ["type"] = thinkingEnabled ? "enabled" : "disabled"
        };
        if (thinkingEnabled)
        {
            payload["reasoning_effort"] = NormalizeReasoningEffortForRequest(settings?.ReasoningEffort);
        }

        return payload.ToJsonString(JsonFileStore.JsonOptions);
    }

    private static string BuildAnthropicRequestJson(PromptLabSettings settings, RenderedPrompt rendered)
    {
        var payload = new JsonObject
        {
            ["model"] = settings?.Model ?? "",
            ["system"] = rendered.SystemPrompt ?? "",
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = rendered.UserPrompt ?? ""
                }
            },
            ["max_tokens"] = Math.Max(16, settings?.MaxTokens ?? 5000),
            ["temperature"] = settings?.Temperature ?? 0f,
            ["stream"] = false
        };

        if (settings?.ThinkingEnabled ?? true)
        {
            var maxTokens = Math.Max(16, settings?.MaxTokens ?? 5000);
            var budget = GetAnthropicThinkingBudget(maxTokens, settings?.ReasoningEffort);
            if (budget > 0)
            {
                payload["thinking"] = new JsonObject
                {
                    ["type"] = "enabled",
                    ["budget_tokens"] = budget
                };
            }
        }
        else
        {
            payload["thinking"] = new JsonObject
            {
                ["type"] = "disabled"
            };
        }

        return payload.ToJsonString(JsonFileStore.JsonOptions);
    }

    private static int GetAnthropicThinkingBudget(int maxTokens, string? effort)
    {
        var normalized = NormalizeReasoningEffortSelection(effort);
        var requested = normalized switch
        {
            "low" => 1024,
            "medium" => 4096,
            "high" => 8192,
            "xhigh" => 12000,
            "max" => 16000,
            _ => 4096
        };
        var upperBound = Math.Max(0, maxTokens - 1024);
        return Math.Min(requested, upperBound);
    }

    private async Task<ApiCallResult> CallApiAsync(PromptLabSettings settings, string requestJson, CancellationToken cancellationToken)
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
        PromptLabCase labCase,
        PromptLabSettings settings,
        RenderedPrompt rendered,
        ApiCallResult result,
        string promptPath,
        string requestPath,
        string responsePath)
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
            ["thinkingEnabled"] = settings?.ThinkingEnabled ?? true,
            ["reasoningEffort"] = NormalizeReasoningEffortSelection(settings?.ReasoningEffort),
            ["reasoningEffortSent"] = (settings?.ThinkingEnabled ?? true) ? NormalizeReasoningEffortForRequest(settings?.ReasoningEffort) : "",
            ["promptPath"] = promptPath,
            ["requestPath"] = requestPath,
            ["responsePath"] = responsePath,
            ["success"] = result.Success,
            ["statusCode"] = result.StatusCode,
            ["error"] = result.Error ?? "",
            ["assistantText"] = result.AssistantText ?? "",
            ["rawResponse"] = result.RawResponse ?? "",
            ["expectedTags"] = JsonSerializer.SerializeToNode(labCase.ExpectedTags ?? new List<string>(), JsonFileStore.JsonOptions),
            ["preprocessHits"] = JsonSerializer.SerializeToNode(labCase.PreprocessHits ?? new List<string>(), JsonFileStore.JsonOptions),
            ["tagRules"] = rendered.TagRules ?? "",
            ["moodRules"] = rendered.MoodRules ?? "",
            ["ruleBehaviorPath"] = catalog.RuleBehaviorPath ?? "",
            ["actionPostprocessPath"] = catalog.ActionPostprocessPath ?? ""
        };
        _json.WriteUtf8(metaPath, meta.ToJsonString(JsonFileStore.JsonOptions));
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

    private static string NormalizeTopLevelRuleId(string name)
    {
        return (name ?? "").Trim() switch
        {
            "Duel" => "duel",
            "Reward" => "reward",
            "Loan" => "loan",
            _ => (name ?? "").Trim()
        };
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? "";
        }

        return "";
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value) &&
            value.TryGetInt32(out var number))
        {
            return number;
        }

        return 0;
    }

    private static bool GetBool(JsonElement element, string propertyName, bool fallback)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }

        return fallback;
    }

    private static void NormalizeCase(PromptLabCase item)
    {
        item.CaseId = (item.CaseId ?? "").Trim();
        item.Title = (item.Title ?? "").Trim();
        item.PreprocessHits ??= new List<string>();
        item.HistoryLines ??= new List<string>();
        item.AfefFacts ??= new List<AfefFact>();
        item.ExpectedTags ??= new List<string>();
        item.RuntimeContext ??= "";
        item.Notes ??= "";
    }
}
