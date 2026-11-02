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
        // bekko uses mean pooling over every attended token, including <bos>/<eos>.
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

    // Production resolves only the active module's multilingual bekko payload.
    // Retired per-language models are never searched or restored as fallbacks.
    internal static ModelFiles ResolveEmbedding(string moduleRoot)
    {
        string dir = Path.Combine(GetOnnxDirectory(moduleRoot), "bekko");
        RejectRedirectedSubdirectory(dir);
        string model = Path.Combine(dir, "model.onnx");
        string tokenizer = Path.Combine(dir, "tokenizer.json");
        string config = Path.Combine(dir, "config.json");
        RequireReadableFile(model, "bekko embedding model");
        RequireReadableFile(tokenizer, "bekko tokenizer");
        RequireReadableFile(config, "bekko configuration");
        RequireReferencedExternalData(model);
        ValidateConfig(config);
        return new ModelFiles(model, tokenizer, config, "mean_all");
    }

    internal static ModelFiles ResolveReranker(string moduleRoot)
        => throw new InvalidOperationException("The legacy ONNX reranker is retired; retrieval uses bekko embeddings.");

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
