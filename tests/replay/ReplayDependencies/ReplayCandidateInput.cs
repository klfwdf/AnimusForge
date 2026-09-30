using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

// Offline replay input, independent of build-output directory depth. Never selects Stage.
internal static class ReplayCandidateInput
{
    internal static string RepositoryRoot()
    {
        string root = Environment.GetEnvironmentVariable("AF_REPLAY_REPO_ROOT");
        if (string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root)
            || !File.Exists(Path.Combine(root, "AnimusForge.csproj")))
            throw new InvalidOperationException("AF_REPLAY_REPO_ROOT must name the current repository explicitly.");
        return Path.GetFullPath(root);
    }

    internal static string Read(string[] args)
    {
        if (args.Length != 2 || !Path.IsPathFullyQualified(args[0]))
            throw new InvalidOperationException("Pass the current absolute candidate DLL path and expected SHA256 after --.");
        string root = RepositoryRoot();
        string path = Path.GetFullPath(args[0]);
        string relative = Path.GetRelativePath(root, path);
        if (Path.IsPathFullyQualified(relative) || relative == ".."
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relative.Replace('\\', '/').Contains("single_module_stage", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Replay candidate must be inside the current repository, never Stage.");
        string marker = Path.ChangeExtension(path, ".build.json");
        if (!File.Exists(path) || !File.Exists(marker))
            throw new InvalidOperationException("Current candidate DLL/build marker is missing.");
        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(marker)))
        {
            JsonElement build = document.RootElement;
            if (!string.Equals(hash, args[1], StringComparison.OrdinalIgnoreCase)
                || !string.Equals(hash, build.GetProperty("Sha256").GetString(), StringComparison.OrdinalIgnoreCase)
                || build.GetProperty("Role").GetString() != "Implementation"
                || build.GetProperty("BannerlordApi").GetString() != "1.4"
                || build.GetProperty("BuildFlavor").GetString() != "ANIMUSFORGE_BANNERLORD_API_1_4")
                throw new InvalidOperationException("Replay candidate hash/build identity mismatch.");
        }
        using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "af-replay-dependencies.json"))))
        {
            JsonElement manifest = document.RootElement;
            if (!string.Equals(Path.GetFullPath(manifest.GetProperty("ImplementationPath").GetString()), path, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(manifest.GetProperty("ImplementationSha256").GetString(), hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Replay dependencies were validated for another candidate.");
        }
        return path;
    }
}
