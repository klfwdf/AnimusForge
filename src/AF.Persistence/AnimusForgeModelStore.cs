using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class AnimusForgeModelStore
{
    internal sealed class ModelFiles
    {
        internal string ModelPath { get; }
        internal string TokenizerPath { get; }
        internal string ConfigPath { get; }
        // "mean": mean pool over inner tokens (bge-small-zh). "cls": first-token pool (bge-small-en).
        internal string Pooling { get; }

        internal ModelFiles(string modelPath, string tokenizerPath, string configPath, string pooling = "mean")
        {
            ModelPath = modelPath;
            TokenizerPath = tokenizerPath;
            ConfigPath = configPath;
            Pooling = pooling;
        }
    }

    internal static ModelFiles ResolveEmbedding()
        => ResolveEmbedding(AnimusForgeModulePaths.GetCurrentModuleRoot());

    internal static ModelFiles ResolveReranker()
        => ResolveReranker(AnimusForgeModulePaths.GetCurrentModuleRoot());

    // Explicit roots are used by offline contracts; production always resolves the active module.
    internal static ModelFiles ResolveEmbedding(string moduleRoot)
    {
        string onnx = GetOnnxDirectory(moduleRoot);
        // English edition: use ONNX/en (bge-small-en, CLS pooling) when it is installed.
        // A missing/incomplete en directory falls back to the Chinese model below so EN players keep working.
        ModelFiles english = TryResolveEnglishEmbedding(moduleRoot, onnx);
        if (english != null)
            return english;
        string nested = Path.Combine(onnx, "onnx");
        RejectRedirectedSubdirectory(nested);
        string model = FirstReadableFile(
            Path.Combine(nested, "model_quantized.onnx"), Path.Combine(onnx, "model_quantized.onnx"),
            Path.Combine(nested, "model.onnx"), Path.Combine(onnx, "model.onnx"));
        if (model == null)
            throw new InvalidOperationException("Required ONNX embedding model is missing from Modules/AnimusForge/ONNX.");
        if (Path.GetFileName(model) == "model.onnx")
            RequireReadableFile(model + "_data", "embedding external data");
        RequireReferencedExternalData(model);
        string tokenizer = FirstReadableFile(Path.Combine(onnx, "tokenizer.json"), Path.Combine(nested, "tokenizer.json"));
        string config = FirstReadableFile(Path.Combine(onnx, "config.json"), Path.Combine(nested, "config.json"));
        if (tokenizer == null || config == null)
            throw new InvalidOperationException("Required ONNX embedding tokenizer.json or config.json is missing from Modules/AnimusForge/ONNX.");
        ValidateConfig(config);
        return new ModelFiles(model, tokenizer, config);
    }

    internal static ModelFiles ResolveReranker(string moduleRoot)
    {
        string reranker = Path.Combine(GetOnnxDirectory(moduleRoot), "reranker");
        RejectRedirectedSubdirectory(reranker);
        string model = FirstReadableFile(Path.Combine(reranker, "model_quantized.onnx"), Path.Combine(reranker, "model.onnx"));
        string tokenizer = FirstReadableFile(Path.Combine(reranker, "tokenizer.json"));
        if (model == null || tokenizer == null)
            throw new InvalidOperationException("ONNX reranker model or tokenizer.json is missing from Modules/AnimusForge/ONNX/reranker.");
        string config = FirstReadableFile(Path.Combine(reranker, "config.json"));
        if (config != null)
            ValidateConfig(config);
        return new ModelFiles(model, tokenizer, config);
    }

    private static ModelFiles TryResolveEnglishEmbedding(string moduleRoot, string onnx)
    {
        string en = Path.Combine(onnx, "en");
        if (!Directory.Exists(en) || !IsEnglishContent(moduleRoot))
            return null;
        RejectRedirectedSubdirectory(en);
        string model = FirstReadableFile(Path.Combine(en, "model_quantized.onnx"), Path.Combine(en, "model.onnx"));
        string tokenizer = FirstReadableFile(Path.Combine(en, "tokenizer.json"));
        string config = FirstReadableFile(Path.Combine(en, "config.json"));
        if (model == null || tokenizer == null || config == null)
            return null;
        if (Path.GetFileName(model) == "model.onnx")
            RequireReadableFile(model + "_data", "english embedding external data");
        RequireReferencedExternalData(model);
        ValidateConfig(config);
        return new ModelFiles(model, tokenizer, config, "cls");
    }

    // The content edition is a deployment-time property: the module ships either the CN or EN LlmPromptTexts.json.
    // Read the same file LlmPromptTexts uses, once, during engine initialization (never on a hot path).
    private static bool IsEnglishContent(string moduleRoot)
    {
        try
        {
            string path = Path.Combine(moduleRoot, "ModuleData", "LlmPromptTexts.json");
            if (!File.Exists(path))
                return false;
            string value = JObject.Parse(File.ReadAllText(path))["AF_LLM_PROMPT_ContentLanguage"]?.ToString();
            return string.Equals(value, "en", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    // Small ONNX graphs can keep weights in "<model>.onnx_data"; the graph names that file.
    // Require it only when the graph really references it, so single-file models are not rejected.
    private static void RequireReferencedExternalData(string model)
    {
        FileInfo info = new FileInfo(model);
        if (info.Length > 16 * 1024 * 1024)
            return;
        string sidecarName = Path.GetFileName(model) + "_data";
        byte[] needle = System.Text.Encoding.ASCII.GetBytes(sidecarName);
        byte[] bytes = File.ReadAllBytes(model);
        for (int i = 0; i + needle.Length <= bytes.Length; i++)
        {
            if (bytes[i] != needle[0])
                continue;
            int j = 1;
            while (j < needle.Length && bytes[i + j] == needle[j]) j++;
            if (j == needle.Length)
            {
                RequireReadableFile(model + "_data", "embedding external data");
                return;
            }
        }
    }

    private static string GetOnnxDirectory(string moduleRoot)
    {
        if (string.IsNullOrWhiteSpace(moduleRoot))
            throw new InvalidOperationException("Current AnimusForge module root is unavailable.");
        string onnx = Path.Combine(moduleRoot, "ONNX");
        if (!Directory.Exists(onnx) || (File.GetAttributes(onnx) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Required ONNX directory is missing or redirected: Modules/AnimusForge/ONNX.");
        return onnx;
    }

    private static string FirstReadableFile(params string[] candidates)
    {
        foreach (string path in candidates)
        {
            if (File.Exists(path))
            {
                RequireReadableFile(path, "ONNX dependency");
                return path;
            }
        }
        return null;
    }

    private static void RejectRedirectedSubdirectory(string path)
    {
        if (Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("ONNX model directory is redirected: " + path);
    }

    private static void RequireReadableFile(string path, string name)
    {
        if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Required " + name + " is missing or redirected: " + path);
        if (new FileInfo(path).Length == 0)
            throw new InvalidOperationException("Required " + name + " is empty: " + path);
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { }
    }

    private static void ValidateConfig(string path)
    {
        try
        {
            JObject.Parse(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("ONNX config.json is invalid: " + path, ex);
        }
    }
}
