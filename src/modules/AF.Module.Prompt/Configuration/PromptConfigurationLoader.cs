using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal interface IPromptConfigurationFiles
{
    string PathFor(string fileName);
    bool Exists(string path);
    string ReadText(string path);
    string ReadText(string path, Encoding encoding);
    byte[] ReadBytes(string path);
    Stream OpenResource(string resourceName);
    void Log(string message);
}

internal sealed class ProductionPromptConfigurationFiles : IPromptConfigurationFiles
{
    public string PathFor(string fileName) => AnimusForgeModulePaths.GetModuleDataFilePath(fileName);
    public bool Exists(string path) => File.Exists(path);
    public string ReadText(string path) => File.ReadAllText(path);
    public string ReadText(string path, Encoding encoding) => File.ReadAllText(path, encoding);
    public byte[] ReadBytes(string path) => File.ReadAllBytes(path);
    public Stream OpenResource(string resourceName) => typeof(PromptConfigurationLoader).Assembly.GetManifestResourceStream(resourceName);
    public void Log(string message) => Logger.Log("AIConfig", message);
}

internal sealed class PromptConfigurationLoadResult
{
    internal PromptConfigurationSnapshot Snapshot { get; }
    internal bool RpUsedEmbeddedDefaults { get; }
    internal bool RpBothUnavailable { get; }
    internal string RpFallbackReason { get; }

    internal PromptConfigurationLoadResult(PromptConfigurationSnapshot snapshot, bool rpUsedEmbeddedDefaults,
        bool rpBothUnavailable, string rpFallbackReason)
    {
        Snapshot = snapshot;
        RpUsedEmbeddedDefaults = rpUsedEmbeddedDefaults;
        RpBothUnavailable = rpBothUnavailable;
        RpFallbackReason = rpFallbackReason ?? "";
    }
}

// A complete replacement value is prepared before the revisioned store publishes it.
internal sealed class PromptConfigurationLoader
{
    private const string PreprocessResource = "AnimusForge.Defaults.PreprocessPrompts.json";
    private const string RpIntroductionResource = "AnimusForge.Defaults.RpItemIntroductionPrompts.json";
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private static readonly Regex TemplateVariable = new Regex("\\{([a-z][a-z0-9_]*)\\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IPromptConfigurationFiles _files;
    private readonly Lazy<JObject> _preprocessDefaults;
    private readonly Lazy<RpItemIntroductionPromptsConfigModel> _rpDefaults;

    internal PromptConfigurationLoader(IPromptConfigurationFiles files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _preprocessDefaults = new Lazy<JObject>(LoadEmbeddedPreprocess);
        _rpDefaults = new Lazy<RpItemIntroductionPromptsConfigModel>(LoadEmbeddedRpIntroduction);
    }

    internal PromptConfigurationLoadResult Load()
    {
        AIConfigModel main = null;
        GuardrailConfigModel guardrail = null;
        ActionPostprocessConfigModel action = null;
        PreprocessPromptsConfigModel preprocess = null;
        ProactiveNpcRequestPromptsConfigModel proactive = null;
        RpItemIntroductionPromptsConfigModel rp = null;
        string preprocessError = "";
        string rpFallbackReason = "";
        bool rpUsedEmbedded = false;
        bool rpBothUnavailable = false;
        try
        {
            string mainPath = _files.PathFor("AIConfig.json");
            if (!_files.Exists(mainPath))
            {
                _files.Log("[错误] 找不到 AIConfig.json");
                main = new AIConfigModel();
            }
            else main = JsonConvert.DeserializeObject<AIConfigModel>(_files.ReadText(mainPath)) ?? new AIConfigModel();

            string guardrailPath = _files.PathFor("RuleBehaviorPrompts.json");
            string actionPath = _files.PathFor("ActionPostprocessPrompts.json");
            string preprocessPath = _files.PathFor("PreprocessPrompts.json");
            string proactivePath = _files.PathFor("ProactiveNpcRequestPrompts.json");
            string rpPath = _files.PathFor("RpItemIntroductionPrompts.json");
            if (!_files.Exists(guardrailPath))
            {
                _files.Log("[错误] 找不到 RuleBehaviorPrompts.json");
                guardrail = new GuardrailConfigModel();
            }
            else guardrail = JsonConvert.DeserializeObject<GuardrailConfigModel>(_files.ReadText(guardrailPath)) ?? new GuardrailConfigModel();
            if (!_files.Exists(actionPath))
            {
                _files.Log("[错误] 找不到 ActionPostprocessPrompts.json");
                action = new ActionPostprocessConfigModel();
            }
            else action = JsonConvert.DeserializeObject<ActionPostprocessConfigModel>(_files.ReadText(actionPath)) ?? new ActionPostprocessConfigModel();

            try
            {
                if (!_files.Exists(preprocessPath)) throw new FileNotFoundException("找不到 PreprocessPrompts.json", preprocessPath);
                preprocess = LoadPreprocess(preprocessPath, out bool usedDefaults, out int sourceVersion, out int defaultVersion);
                ValidatePreprocess(preprocess);
                if (usedDefaults)
                    _files.Log(string.Format("[兼容] 检测到旧版 PreprocessPrompts.json (v{0})，其输出 schema 与 v{1} 不兼容；本次运行已采用程序集内置 v{1} 默认提示词，磁盘文件未改写。", sourceVersion, defaultVersion));
            }
            catch (Exception ex)
            {
                preprocess = new PreprocessPromptsConfigModel();
                preprocessError = ex.Message;
                _files.Log("[错误] 前处理提示词配置加载失败: " + ex.Message);
            }
            try
            {
                if (!_files.Exists(proactivePath)) throw new FileNotFoundException("找不到 ProactiveNpcRequestPrompts.json", proactivePath);
                proactive = JsonConvert.DeserializeObject<ProactiveNpcRequestPromptsConfigModel>(_files.ReadText(proactivePath, Encoding.UTF8)) ?? new ProactiveNpcRequestPromptsConfigModel();
            }
            catch (Exception ex)
            {
                _files.Log("[错误] 载入 ProactiveNpcRequestPrompts.json 失败: " + ex.Message);
                proactive = new ProactiveNpcRequestPromptsConfigModel();
            }
            try
            {
                rp = LoadRpIntroduction(rpPath, out rpUsedEmbedded, out rpFallbackReason);
            }
            catch (Exception ex)
            {
                rp = new RpItemIntroductionPromptsConfigModel();
                rpBothUnavailable = true;
                rpFallbackReason = ex.Message;
            }
            _files.Log("配置文件路径：AIConfig=" + mainPath + " RuleBehavior=" + guardrailPath + " ActionPostprocess=" + actionPath + " PreprocessPrompts=" + preprocessPath + " RpItemIntroductionPrompts=" + rpPath);
        }
        catch (Exception ex)
        {
            _files.Log("[错误] 加载失败: " + ex.Message);
            main = new AIConfigModel();
            guardrail = new GuardrailConfigModel();
            action = new ActionPostprocessConfigModel();
            preprocess = new PreprocessPromptsConfigModel();
            preprocessError = ex.Message;
            proactive = new ProactiveNpcRequestPromptsConfigModel();
            rp = new RpItemIntroductionPromptsConfigModel();
        }
        return new PromptConfigurationLoadResult(new PromptConfigurationSnapshot(main, guardrail, action, preprocess,
            proactive, rp, preprocessError), rpUsedEmbedded, rpBothUnavailable, rpFallbackReason);
    }

    private PreprocessPromptsConfigModel LoadPreprocess(string path, out bool usedDefaults, out int sourceVersion, out int defaultVersion)
    {
        JObject source = JObject.Parse(_files.ReadText(path, StrictUtf8));
        JObject defaults = (JObject)_preprocessDefaults.Value.DeepClone();
        defaultVersion = defaults.Value<int?>("Version").GetValueOrDefault();
        if (defaultVersion <= 0) throw new InvalidDataException("程序集内置 PreprocessPrompts.json 的 Version 无效");
        sourceVersion = source.Value<int?>("Version").GetValueOrDefault();
        usedDefaults = sourceVersion < defaultVersion;
        if (usedDefaults) source = defaults;
        return source.ToObject<PreprocessPromptsConfigModel>() ?? new PreprocessPromptsConfigModel();
    }

    private JObject LoadEmbeddedPreprocess()
    {
        using Stream stream = _files.OpenResource(PreprocessResource);
        if (stream == null) throw new MissingManifestResourceException("找不到程序集内置前处理提示词资源: " + PreprocessResource);
        using StreamReader reader = new StreamReader(stream, StrictUtf8, detectEncodingFromByteOrderMarks: true);
        return JObject.Parse(reader.ReadToEnd());
    }

    private RpItemIntroductionPromptsConfigModel LoadRpIntroduction(string path, out bool usedDefaults, out string fallbackReason)
    {
        usedDefaults = false;
        fallbackReason = "";
        try
        {
            if (!_files.Exists(path)) throw new FileNotFoundException("找不到 RpItemIntroductionPrompts.json", path);
            var config = JsonConvert.DeserializeObject<RpItemIntroductionPromptsConfigModel>(ReadStrictUtf8NoBom(path, "RpItemIntroductionPrompts.json"));
            if (config == null) throw new InvalidDataException("RpItemIntroductionPrompts.json 内容为空或不是对象");
            ValidateRpIntroduction(config, "RpItemIntroductionPrompts.json");
            return config;
        }
        catch (Exception ex)
        {
            usedDefaults = true;
            fallbackReason = ex.Message;
            var embedded = _rpDefaults.Value;
            ValidateRpIntroduction(embedded, "程序集内置 RpItemIntroductionPrompts.json");
            return embedded;
        }
    }

    private RpItemIntroductionPromptsConfigModel LoadEmbeddedRpIntroduction()
    {
        using Stream stream = _files.OpenResource(RpIntroductionResource);
        if (stream == null) throw new MissingManifestResourceException("找不到程序集内置 RP物品介绍提示词资源: " + RpIntroductionResource);
        using StreamReader reader = new StreamReader(stream, StrictUtf8, detectEncodingFromByteOrderMarks: false);
        var config = JsonConvert.DeserializeObject<RpItemIntroductionPromptsConfigModel>(reader.ReadToEnd());
        if (config == null) throw new InvalidDataException("程序集内置 RpItemIntroductionPrompts.json 内容为空或不是对象");
        return config;
    }

    private string ReadStrictUtf8NoBom(string path, string displayName)
    {
        byte[] bytes = _files.ReadBytes(path);
        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
            throw new InvalidDataException((displayName ?? "配置文件") + " 必须使用 UTF-8 无 BOM 编码");
        return StrictUtf8.GetString(bytes);
    }

    private static void ValidateRpIntroduction(RpItemIntroductionPromptsConfigModel config, string name)
    {
        if (config == null) throw new InvalidDataException((name ?? "RpItemIntroductionPrompts.json") + " 内容为空");
        if (config.Version != 1)
            throw new InvalidDataException((name ?? "RpItemIntroductionPrompts.json") + " 的 Version 必须为 1，当前为 " + config.Version);
        RequireRpValue(config.SystemPrompt, "SystemPrompt");
        string template = RequireRpValue(config.UserPromptTemplate, "UserPromptTemplate");
        bool item = false, dialogue = false;
        foreach (Match match in Regex.Matches(template, "\\{([^{}]*)\\}", RegexOptions.CultureInvariant))
        {
            string variable = match.Groups[1].Value;
            if (variable != "item_name" && variable != "giver_name" && variable != "dialogue")
                throw new InvalidDataException((name ?? "RpItemIntroductionPrompts.json") + " 的 UserPromptTemplate 包含不支持的占位符: {" + variable + "}。只允许 {item_name}、{giver_name}、{dialogue}");
            item |= variable == "item_name";
            dialogue |= variable == "dialogue";
        }
        if (!item || !dialogue)
            throw new InvalidDataException((name ?? "RpItemIntroductionPrompts.json") + " 的 UserPromptTemplate 必须包含 {item_name} 和 {dialogue}");
    }

    private static string RequireRpValue(string value, string field)
    {
        string text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException("RpItemIntroductionPrompts.json 缺少必填项: " + (field ?? "unknown"));
        return text;
    }

    private static void ValidatePreprocess(PreprocessPromptsConfigModel config)
    {
        RequirePreprocess(config?.StrictJson?.SystemPrompt, "StrictJson.SystemPrompt");
        JObject schema = config?.StrictJson?.MentionedEntitiesSchema;
        if (!(schema?["entities"] is JArray)) throw new InvalidOperationException("PreprocessPrompts.json schema 缺少数组: StrictJson.MentionedEntitiesSchema.entities");
        RequirePreprocess(config?.TopicRouting?.EmptyValue, "TopicRouting.EmptyValue");
        ValidateTemplate(config?.TopicRouting?.UserPromptTemplate, "TopicRouting.UserPromptTemplate", "topic_list", "routing_guidance", "history", "latest_npc", "latest_player", "top_n", "mentioned_entities_schema");
        RequirePreprocess(config?.MemorySelection?.ParallelModeInstruction, "MemorySelection.ParallelModeInstruction");
        RequirePreprocess(config?.MemorySelection?.UnifiedModeInstruction, "MemorySelection.UnifiedModeInstruction");
        RequirePreprocess(config?.MemorySelection?.EmptyValue, "MemorySelection.EmptyValue");
        ValidateTemplate(config?.MemorySelection?.UserPromptTemplate, "MemorySelection.UserPromptTemplate", "mode_instruction", "final_count", "latest_player_input", "latest_npc_input", "current_scene", "memory_candidates");
        ValidateTemplate(config?.MemorySelection?.CandidateLineTemplate, "MemorySelection.CandidateLineTemplate", "memory_id", "game_date", "age_suffix", "hour_range", "rich_title");
        ValidateTemplate(config?.MemorySelection?.FallbackGameDateTemplate, "MemorySelection.FallbackGameDateTemplate", "game_day");
        RequirePreprocess(config?.ConnectionTest?.ExpectedRuleCode, "ConnectionTest.ExpectedRuleCode");
        ValidateTemplate(config?.ConnectionTest?.UserPromptTemplate, "ConnectionTest.UserPromptTemplate", "expected_rule_code", "mentioned_entities_schema");
    }

    private static string RequirePreprocess(string value, string path)
    {
        string text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("PreprocessPrompts.json 缺少必填项: " + (path ?? "unknown"));
        return text;
    }

    private static void ValidateTemplate(string template, string path, params string[] required)
    {
        string text = RequirePreprocess(template, path);
        var variables = new HashSet<string>(TemplateVariable.Matches(text).Cast<Match>().Select(x => x.Groups[1].Value), StringComparer.Ordinal);
        foreach (string variable in required ?? Array.Empty<string>())
            if (!variables.Contains(variable)) throw new InvalidOperationException("PreprocessPrompts.json 模板缺少占位符: " + path + ".{" + variable + "}");
    }
}
