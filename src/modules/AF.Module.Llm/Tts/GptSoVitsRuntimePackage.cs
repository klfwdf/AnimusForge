using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal enum GptSoVitsPackageState { Missing, Installed, Incompatible, Invalid }

// Detached discovery. Never executes a package or loads a model.
internal sealed class GptSoVitsRuntimePackage
{
    internal const int RuntimeApi = 1;
    internal const string PackageId = "AF-GPTSOVITS-L"; // Legacy fixture identity.
    internal string Id { get; private set; }
    internal string Edition { get; private set; }
    internal GptSoVitsPackageState State { get; private set; }
    internal string Reason { get; private set; }
    internal string Root { get; private set; }
    internal string Version { get; private set; }
    internal string PythonPath { get; private set; }
    internal string EntryPath { get; private set; }
    internal string Engine { get; private set; }
    private readonly Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    internal static GptSoVitsRuntimePackage Discover(string moduleRoot, Version afVersion, bool verifyFiles = false)
    {
        GptSoVitsRuntimePackage failure = null;
        foreach (string edition in new[] { "L", "U" })
        {
            var candidate = DiscoverEdition(moduleRoot, afVersion, edition);
            if (candidate.State == GptSoVitsPackageState.Installed && verifyFiles)
            {
                try { candidate.VerifyFiles(); }
                catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
                { candidate.Fail(GptSoVitsPackageState.Invalid, "package_verification_failed"); }
            }
            if (candidate.State == GptSoVitsPackageState.Installed) return candidate;
            if (failure == null || failure.State == GptSoVitsPackageState.Missing) failure = candidate;
        }
        return failure;
    }

    private static GptSoVitsRuntimePackage DiscoverEdition(string moduleRoot, Version afVersion, string edition)
    {
        var result = new GptSoVitsRuntimePackage { State = GptSoVitsPackageState.Missing, Reason = "package_missing", Id = "AF-GPTSOVITS-" + edition, Edition = edition };
        try
        {
            string module = AnimusForgeDataPaths.ResolveRoot(moduleRoot);
            result.Root = ResolvePath(module, result.Id);
            string manifest = ResolvePath(result.Root, "runtime.json");
            if (!File.Exists(manifest)) return result;
            if (new FileInfo(manifest).Length > 256 * 1024) throw new InvalidDataException("manifest_too_large");
            var document = JObject.Parse(File.ReadAllText(manifest));
            if ((int?)document["schema_version"] != 1 || (string)document["package_id"] != result.Id ||
                (int?)document["af_runtime_api"] != RuntimeApi || (string)document["edition"] != edition)
                return result.Fail(GptSoVitsPackageState.Incompatible, "package_protocol_unsupported");
            if (!System.Version.TryParse((string)document["af_min_version"], out var minimum) || afVersion < minimum)
                return result.Fail(GptSoVitsPackageState.Incompatible, "af_version_unsupported");
            if (!System.Version.TryParse((string)document["package_version"], out _)) throw new InvalidDataException("package_version_invalid");
            result.Version = (string)document["package_version"];
            result.Engine = (string)document["engine"];
            if (result.Engine != "gpt-sovits-v2pro") return result.Fail(GptSoVitsPackageState.Incompatible, "engine_unsupported");
            result.PythonPath = ResolvePath(result.Root, Required(document, "python"));
            result.EntryPath = ResolvePath(result.Root, Required(document, "entrypoint"));
            if (!result.PythonPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                !result.EntryPath.EndsWith(".py", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("entrypoint_invalid");
            if (!(document["files"] is JObject files) || files.Count < 2 || files.Count > 64) throw new InvalidDataException("file_manifest_invalid");
            foreach (var item in files.Properties())
            {
                string path = ResolvePath(result.Root, item.Name);
                string hash = item.Value.Type == JTokenType.String ? (string)item.Value : null;
                if (hash == null || hash.Length != 64 || !IsHex(hash) || !File.Exists(path)) throw new InvalidDataException("package_file_missing_or_invalid");
                result._files.Add(path, hash);
            }
            if (!result._files.ContainsKey(result.PythonPath) || !result._files.ContainsKey(result.EntryPath))
                throw new InvalidDataException("entrypoint_not_hashed");
            result.State = GptSoVitsPackageState.Installed;
            result.Reason = "installed_not_started";
            return result;
        }
        catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException ||
            ex is InvalidOperationException || ex is Newtonsoft.Json.JsonException || ex is NotSupportedException)
        {
            return result.Fail(GptSoVitsPackageState.Invalid, ex is InvalidDataException ? ex.Message : "package_invalid");
        }
    }

    // Called on the startup worker, never on a getter or Tick.
    internal void VerifyFiles()
    {
        if (State != GptSoVitsPackageState.Installed) throw new InvalidOperationException(Reason);
        foreach (var file in _files)
        {
            AnimusForgeDataPaths.ValidatePath(file.Key);
            using (var input = File.OpenRead(file.Key))
            using (var sha = SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
                if (!string.Equals(actual, file.Value, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("package_hash_mismatch");
            }
        }
    }

    internal static string ResolvePath(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.IndexOf(':') >= 0)
            throw new InvalidDataException("package_path_invalid");
        foreach (var part in relative.Replace('\\', '/').Split('/'))
            if (part.Length == 0 || part == "." || part == ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException("package_path_invalid");
        string path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("package_path_outside_root");
        AnimusForgeDataPaths.ValidatePath(path);
        return path;
    }

    private GptSoVitsRuntimePackage Fail(GptSoVitsPackageState state, string reason) { State = state; Reason = reason; return this; }
    private static string Required(JObject value, string key) => value[key]?.Type == JTokenType.String ? (string)value[key] : throw new InvalidDataException("package_field_invalid");
    private static bool IsHex(string value)
    {
        foreach (char ch in value) if (!((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))) return false;
        return true;
    }
}
