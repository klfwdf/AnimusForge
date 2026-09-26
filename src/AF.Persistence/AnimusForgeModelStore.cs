using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class AnimusForgeModelStore
{
    private const string LockResourceName = "AnimusForge.Dependencies.ModelsLock.json";

    internal static string GetReadyGroupDirectory(string group)
    {
        byte[] lockBytes;
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LockResourceName)
            ?? throw new InvalidOperationException("Model dependency lock is unavailable."))
        using (var buffer = new MemoryStream())
        {
            stream.CopyTo(buffer);
            lockBytes = buffer.ToArray();
        }

        string root = AnimusForgeDataPaths.GetCurrentRoot();
        string models = AnimusForgeDataPaths.GetModelsDirectory(root);
        string marker = Path.Combine(models, ".af-models-ready.json");
        if (!File.Exists(marker))
            throw new InvalidOperationException("Model dependency is not ready; run the explicit AF2 model migration.");
        if ((File.GetAttributes(marker) & FileAttributes.ReparsePoint) != 0 || new FileInfo(marker).Length > 1024 * 1024)
            throw new InvalidOperationException("Model dependency readiness record is unsafe.");
        return ValidateReadyGroup(models, group, lockBytes, File.ReadAllText(marker, Encoding.UTF8));
    }

    internal static string ValidateReadyGroup(string models, string group, byte[] lockBytes, string markerJson)
    {
        if (group != "embedding" && group != "reranker")
            throw new ArgumentException("Unknown model group.", nameof(group));
        JObject dependencyLock = JObject.Parse(Encoding.UTF8.GetString(lockBytes));
        JObject marker = JObject.Parse(markerJson);
        string lockHash;
        using (SHA256 sha = SHA256.Create())
            lockHash = BitConverter.ToString(sha.ComputeHash(lockBytes)).Replace("-", "").ToLowerInvariant();
        if ((int?)dependencyLock["schemaVersion"] != 1 || (int?)marker["schema"] != 1
            || !string.Equals((string)marker["lockSha256"], lockHash, StringComparison.Ordinal))
            throw new InvalidOperationException("Model dependency readiness record does not match the lock.");
        string manifestHash = (string)marker["completionManifestSha256"];
        if (manifestHash == null || manifestHash.Length != 64)
            throw new InvalidOperationException("Model migration completion reference is missing.");
        foreach (char digit in manifestHash)
            if (!((digit >= '0' && digit <= '9') || (digit >= 'a' && digit <= 'f')))
                throw new InvalidOperationException("Model migration completion reference is invalid.");
        string recovery = Path.Combine(Path.GetDirectoryName(models) ?? "", "Recovery", "models-" + manifestHash.Substring(0, 24));
        string completed = Path.Combine(recovery, "completed.json");
        if (!Directory.Exists(recovery) || (File.GetAttributes(recovery) & FileAttributes.ReparsePoint) != 0
            || !File.Exists(completed) || (File.GetAttributes(completed) & FileAttributes.ReparsePoint) != 0
            || new FileInfo(completed).Length > 1024 * 1024
            || (string)JObject.Parse(File.ReadAllText(completed, Encoding.UTF8))["manifestSha256"] != manifestHash)
            throw new InvalidOperationException("Model migration completion record is unavailable or invalid.");

        JToken readyGroup = marker["groups"]?[group]
            ?? throw new InvalidOperationException("Model dependency group is not ready: " + group);
        string variant = (string)readyGroup["variant"];
        if (string.IsNullOrWhiteSpace(variant))
            throw new InvalidOperationException("Model dependency variant is missing.");
        JToken expected = dependencyLock["groups"]?[group]?["variants"]?[variant]
            ?? throw new InvalidOperationException("Model dependency variant is not locked: " + group);
        JToken readyFiles = readyGroup["files"]
            ?? throw new InvalidOperationException("Model dependency readiness record is incomplete.");
        if (!(expected is JObject expectedFiles) || !(readyFiles is JObject readyFileMap))
            throw new InvalidOperationException("Model dependency lock or readiness record is malformed.");
        string folder = Path.Combine(models, group);
        if (!Directory.Exists(folder) || (File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Model dependency directory is missing or redirected: " + group);

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (JProperty file in expectedFiles.Properties())
        {
            string name = file.Name;
            if (name.Length == 0 || name == "." || name == ".." || name.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
                throw new InvalidOperationException("Model dependency lock has an invalid file name.");
            string path = Path.Combine(folder, name);
            if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Model dependency file is missing or redirected: " + group);
            long expectedSize = (long?)file.Value["size"] ?? -1;
            string expectedHash = (string)file.Value["sha256"];
            JToken ready = readyFiles[name];
            if (ready == null || expectedSize < 0 || string.IsNullOrEmpty(expectedHash)
                || (long?)ready["size"] != expectedSize
                || !string.Equals((string)ready["sha256"], expectedHash, StringComparison.Ordinal)
                || (long?)ready["mtimeUtcTicks"] != File.GetLastWriteTimeUtc(path).Ticks
                || new FileInfo(path).Length != expectedSize)
                throw new InvalidOperationException("Model dependency differs from its verified migration: " + group);
            names.Add(name);
        }
        if (names.Count != readyFileMap.Count)
            throw new InvalidOperationException("Model dependency readiness record has an unexpected file.");
        foreach (string path in Directory.EnumerateFileSystemEntries(folder))
        {
            if (!names.Contains(Path.GetFileName(path)))
                throw new InvalidOperationException("Model dependency group contains an unknown file.");
        }
        return folder;
    }
}
