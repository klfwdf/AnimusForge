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

        internal ModelFiles(string modelPath, string tokenizerPath, string configPath)
        {
            ModelPath = modelPath;
            TokenizerPath = tokenizerPath;
            ConfigPath = configPath;
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
        string nested = Path.Combine(onnx, "onnx");
        string model = FirstReadableFile(
            Path.Combine(nested, "model_quantized.onnx"), Path.Combine(onnx, "model_quantized.onnx"),
            Path.Combine(nested, "model.onnx"), Path.Combine(onnx, "model.onnx"));
        if (model == null)
            throw new InvalidOperationException("Required ONNX embedding model is missing from Modules/AnimusForge/ONNX.");
        if (Path.GetFileName(model) == "model.onnx")
            RequireReadableFile(model + "_data", "embedding external data");
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
        string model = FirstReadableFile(Path.Combine(reranker, "model_quantized.onnx"), Path.Combine(reranker, "model.onnx"));
        string tokenizer = FirstReadableFile(Path.Combine(reranker, "tokenizer.json"));
        if (model == null || tokenizer == null)
            throw new InvalidOperationException("ONNX reranker model or tokenizer.json is missing from Modules/AnimusForge/ONNX/reranker.");
        string config = FirstReadableFile(Path.Combine(reranker, "config.json"));
        if (config != null)
            ValidateConfig(config);
        return new ModelFiles(model, tokenizer, config);
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
