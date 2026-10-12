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
    public string PathFor(string fileName)
    {
        if (fileName == "Defaults/PreprocessPrompts.json" || fileName == "Defaults/RpItemIntroductionPrompts.json")
            return Path.Combine(AnimusForgeModulePaths.GetCurrentModuleRoot(), "ModuleData", "Defaults", Path.GetFileName(fileName));
        return AnimusForgeModulePaths.GetModuleDataFilePath(fileName);
    }
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
    private const string PreprocessDefaultsFile = "Defaults/PreprocessPrompts.json";
    private const string RpIntroductionDefaultsFile = "Defaults/RpItemIntroductionPrompts.json";
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private static readonly Regex TemplateVariable = new Regex("\\{([a-z][a-z0-9_]*)\\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IPromptConfigurationFiles _files;
    private readonly Lazy<JObject> _preprocessDefaults;
    private readonly Lazy<string> _rpDefaults;

    internal PromptConfigurationLoader(IPromptConfigurationFiles files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _preprocessDefaults = new Lazy<JObject>(LoadPackagedPreprocess);
        _rpDefaults = new Lazy<string>(LoadPackagedRpIntroduction);
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
                _files.Log("[Error] Missing AIConfig.json");
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
                _files.Log("[Error] Missing RuleBehaviorPrompts.json");
                guardrail = new GuardrailConfigModel();
            }
            else guardrail = JsonConvert.DeserializeObject<GuardrailConfigModel>(_files.ReadText(guardrailPath)) ?? new GuardrailConfigModel();
            if (!_files.Exists(actionPath))
            {
                _files.Log("[Error] Missing ActionPostprocessPrompts.json");
                action = new ActionPostprocessConfigModel();
            }
            else action = JsonConvert.DeserializeObject<ActionPostprocessConfigModel>(_files.ReadText(actionPath)) ?? new ActionPostprocessConfigModel();

            try
            {
                if (!_files.Exists(preprocessPath)) throw new FileNotFoundException("Missing PreprocessPrompts.json", preprocessPath);
                preprocess = LoadPreprocess(preprocessPath, out bool usedDefaults, out int sourceVersion, out int defaultVersion);
                ValidatePreprocess(preprocess);
                if (usedDefaults)
                    _files.Log(string.Format("[Compatibility] Detected legacy PreprocessPrompts.json (v{0}); its output schema is incompatible with v{1}. This run uses packaged v{1} defaults; the disk file was not rewritten.", sourceVersion, defaultVersion));
            }
            catch (Exception ex)
            {
                preprocess = new PreprocessPromptsConfigModel();
                preprocessError = ex.Message;
                _files.Log("[Error] Preprocess prompt configuration load failed: " + ex.Message);
            }
            try
            {
                if (!_files.Exists(proactivePath)) throw new FileNotFoundException("Missing ProactiveNpcRequestPrompts.json", proactivePath);
                proactive = JsonConvert.DeserializeObject<ProactiveNpcRequestPromptsConfigModel>(_files.ReadText(proactivePath, Encoding.UTF8)) ?? new ProactiveNpcRequestPromptsConfigModel();
            }
            catch (Exception ex)
            {
                _files.Log("[Error] ProactiveNpcRequestPrompts.json load failed: " + ex.Message);
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
            _files.Log("Configuration paths: AIConfig=" + mainPath + " RuleBehavior=" + guardrailPath + " ActionPostprocess=" + actionPath + " PreprocessPrompts=" + preprocessPath + " RpItemIntroductionPrompts=" + rpPath);
        }
        catch (Exception ex)
        {
            _files.Log("[Error] Load failed: " + ex.Message);
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
        if (defaultVersion <= 0) throw new InvalidDataException("Invalid Version in packaged default PreprocessPrompts.json");
        sourceVersion = source.Value<int?>("Version").GetValueOrDefault();
        usedDefaults = sourceVersion < defaultVersion;
        if (usedDefaults) source = defaults;
        return source.ToObject<PreprocessPromptsConfigModel>() ?? new PreprocessPromptsConfigModel();
    }

    private JObject LoadPackagedPreprocess()
    {
        string path = _files.PathFor(PreprocessDefaultsFile);
        return JObject.Parse(_files.ReadText(path, StrictUtf8));
    }

    private RpItemIntroductionPromptsConfigModel LoadRpIntroduction(string path, out bool usedDefaults, out string fallbackReason)
    {
        usedDefaults = false;
        fallbackReason = "";
        try
        {
            if (!_files.Exists(path)) throw new FileNotFoundException("Missing RpItemIntroductionPrompts.json", path);
            var config = JsonConvert.DeserializeObject<RpItemIntroductionPromptsConfigModel>(ReadStrictUtf8NoBom(path, "RpItemIntroductionPrompts.json"));
            if (config == null) throw new InvalidDataException("RpItemIntroductionPrompts.json is empty or is not an object");
            ValidateRpIntroduction(config, "RpItemIntroductionPrompts.json");
            return config;
        }
        catch (Exception ex)
        {
            usedDefaults = true;
            fallbackReason = ex.Message;
            var embedded = JsonConvert.DeserializeObject<RpItemIntroductionPromptsConfigModel>(_rpDefaults.Value);
            if (embedded == null) throw new InvalidDataException("Packaged default RpItemIntroductionPrompts.json is empty or is not an object");
            ValidateRpIntroduction(embedded, "Packaged default RpItemIntroductionPrompts.json");
            return embedded;
        }
    }

    private string LoadPackagedRpIntroduction()
    {
        return ReadStrictUtf8NoBom(_files.PathFor(RpIntroductionDefaultsFile), RpIntroductionDefaultsFile);
    }

    private string ReadStrictUtf8NoBom(string path, string displayName)
    {
        byte[] bytes = _files.ReadBytes(path);
        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
            throw new InvalidDataException((displayName ?? "Configuration file") + " must use UTF-8 without BOM");
        return StrictUtf8.GetString(bytes);
    }

    private static void ValidateRpIntroduction(RpItemIntroductionPromptsConfigModel config, string name)
    {
        if (config == null) throw new InvalidDataException((name ?? "RpItemIntroductionPrompts.json") + " is empty");
        if (config.Version != 1)
            throw new InvalidDataException((name ?? "RpItemIntroductionPrompts.json") + " Version must be 1; current value: " + config.Version);
        RequireRpValue(config.SystemPrompt, "SystemPrompt");
        string template = RequireRpValue(config.UserPromptTemplate, "UserPromptTemplate");
        bool item = false, dialogue = false;
        foreach (Match match in Regex.Matches(template, "\\{([^{}]*)\\}", RegexOptions.CultureInvariant))
        {
            string variable = match.Groups[1].Value;
            if (variable != "item_name" && variable != "giver_name" && variable != "dialogue")
                throw new InvalidDataException((name ?? "RpItemIntroductionPrompts.json") + " UserPromptTemplate contains an unsupported placeholder: {" + variable + "}; only {item_name}, {giver_name}, {dialogue} are allowed");
            item |= variable == "item_name";
            dialogue |= variable == "dialogue";
        }
        if (!item || !dialogue)
            throw new InvalidDataException((name ?? "RpItemIntroductionPrompts.json") + " UserPromptTemplate must contain {item_name} and {dialogue}");
    }

    private static string RequireRpValue(string value, string field)
    {
        string text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException("RpItemIntroductionPrompts.json is missing a required field: " + (field ?? "unknown"));
        return text;
    }

    private static void ValidatePreprocess(PreprocessPromptsConfigModel config)
    {
        RequirePreprocess(config?.StrictJson?.SystemPrompt, "StrictJson.SystemPrompt");
        JObject schema = config?.StrictJson?.MentionedEntitiesSchema;
        if (!(schema?["entities"] is JArray)) throw new InvalidOperationException("PreprocessPrompts.json schema is missing an array: StrictJson.MentionedEntitiesSchema.entities");
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
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("PreprocessPrompts.json is missing a required field: " + (path ?? "unknown"));
        return text;
    }

    private static void ValidateTemplate(string template, string path, params string[] required)
    {
        string text = RequirePreprocess(template, path);
        var variables = new HashSet<string>(TemplateVariable.Matches(text).Cast<Match>().Select(x => x.Groups[1].Value), StringComparer.Ordinal);
        foreach (string variable in required ?? Array.Empty<string>())
            if (!variables.Contains(variable)) throw new InvalidOperationException("PreprocessPrompts.json template is missing a placeholder: " + path + ".{" + variable + "}");
    }
}
