using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using RichExecutions.Diagnostics;

using RichExecutions.Scene;

namespace RichExecutions.Customization;

public sealed class ExecutionSitePresetLoadResult
{
    internal ExecutionSitePresetLoadResult(string path, ExecutionSitePreset? preset, string? error)
    {
        Path = path;
        Preset = preset;
        Error = error;
    }

    public string Path { get; }
    public ExecutionSitePreset? Preset { get; }
    public string? Error { get; }
    public bool IsValid => Preset is not null && string.IsNullOrWhiteSpace(Error);
}

public static class ExecutionSitePresetStore
{
    private const string PresetRootRelativePath =
        @"Mount and Blade II Bannerlord\Configs\RichExecutions\ExecutionSitePresets";

    public static string RootDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        PresetRootRelativePath);

    /// <summary>
    /// Mesh keys that no longer exist in the asset pack. v0.5.5 merged the
    /// impalement bottom Mesh into <c>verjianchi02</c>, so presets saved by
    /// earlier builds still list the removed key. Those pieces are dropped on
    /// load instead of being reported as missing required resources, which
    /// would disable every pre-v0.5.5 impalement preset.
    /// </summary>
    private static readonly string[] ObsoletePieceResourceKeys = { "verjianchi01" };

    public static bool IsObsoletePiece(ExecutionSitePiece? piece) =>
        piece is not null &&
        !string.IsNullOrWhiteSpace(piece.ResourceKey) &&
        ObsoletePieceResourceKeys.Contains(piece.ResourceKey, StringComparer.OrdinalIgnoreCase);

    public static int RemoveObsoletePieces(ExecutionSitePreset? preset)
    {
        if (preset?.Pieces is null)
        {
            return 0;
        }

        return preset.Pieces.RemoveAll(IsObsoletePiece);
    }

    public static IReadOnlyList<ExecutionSitePresetLoadResult> LoadAll(string methodId) =>
        LoadAllFromDirectory(methodId, GetMethodDirectory(methodId));

    private static IReadOnlyList<ExecutionSitePresetLoadResult> LoadAllFromDirectory(string methodId, string directory)
    {
        var results = new List<ExecutionSitePresetLoadResult>();
        if (ExecutionSceneVisualProfiles.TryGet(methodId, out var profile))
        {
            foreach (var preset in ExecutionSitePresetFactory.CreateBuiltInPresets(methodId, profile))
            {
                var validationError = Validate(preset, methodId);
                if (validationError is not null)
                {
                    RexLog.Warning($"Built-in preset '{preset.Name}' was skipped: {validationError}");
                    continue;
                }

                results.Add(new ExecutionSitePresetLoadResult(
                    $"<built-in>\\{methodId}\\{preset.Id:D}.json",
                    preset,
                    null));
            }
        }

        if (!Directory.Exists(directory))
        {
            return results;
        }

        var stored = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(Load);
        foreach (var result in stored)
        {
            if (!result.IsValid)
            {
                // Keep the diagnostic, but malformed user data must neither
                // enter classification nor replace a valid built-in by GUID.
                results.Add(result);
                continue;
            }

            if (ExecutionSitePresetFactory.IsDecorationPreset(result.Preset))
            {
                RexLog.Info(
                    $"Decoration preset '{result.Path}' is hidden from the execution-site preset list; " +
                    "its decoration remains available through Add decoration.");
                continue;
            }

            // A user-edited file with a built-in GUID is authoritative and
            // replaces the in-memory default without creating another entry.
            if (result.Preset is not null)
            {
                results.RemoveAll(existing => existing.Preset?.Id == result.Preset.Id);
            }

            results.Add(result);
        }

        return results;
    }

    public static void EnsureBuiltInPresetsForAllMethods()
    {
        // Built-ins are supplied in memory by LoadAll. This lifecycle hook is
        // retained for binary compatibility, but it must never write preset JSON.
        RexLog.Info("Built-in execution-site presets are memory-only; no preset files were created.");
    }

    public static ExecutionSitePresetLoadResult Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new ExecutionSitePresetLoadResult(path ?? string.Empty, null, "Preset path is empty.");
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var preset = CreateSerializer().ReadObject(stream) as ExecutionSitePreset;
            var migrated = RemoveObsoletePieces(preset);
            if (migrated > 0)
            {
                RexLog.Info(
                    $"Preset '{path}' dropped {migrated} obsolete piece(s) whose Mesh no longer exists " +
                    $"({string.Join(", ", ObsoletePieceResourceKeys)}); the merged apparatus Mesh is used instead.");
            }

            var error = Validate(preset, expectedMethodId: null);
            if (error is not null)
            {
                RexLog.Warning($"Preset '{path}' was disabled: {error}");
                return new ExecutionSitePresetLoadResult(path, preset, error);
            }

            return new ExecutionSitePresetLoadResult(path, preset, null);
        }
        catch (Exception exception)
        {
            RexLog.Error($"Preset '{path}' could not be read and was disabled.", exception);
            return new ExecutionSitePresetLoadResult(path, null, exception.Message);
        }
    }

    public static string Save(ExecutionSitePreset preset)
    {
        var validationError = Validate(preset, preset?.MethodId);
        if (validationError is not null)
        {
            throw new InvalidDataException(validationError);
        }

        var directory = GetMethodDirectory(preset!.MethodId);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, preset.Id.ToString("D") + ".json");
        var temporaryPath = path + ".tmp";
        var backupPath = path + ".bak";

        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None))
            {
                CreateSerializer().WriteObject(stream, preset);
                stream.Flush(true);
            }

            if (File.Exists(path))
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Replace(temporaryPath, path, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, path);
            }

            RexLog.Info($"Saved execution-site preset '{preset.Name}' ({preset.Id:D}) to '{path}'.");
            return path;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public static string GetMethodDirectory(string methodId)
    {
        if (string.IsNullOrWhiteSpace(methodId))
        {
            throw new ArgumentException("A method id is required.", nameof(methodId));
        }

        var safe = new string(methodId
            .Where(character => char.IsLetterOrDigit(character) || character == '_' || character == '-')
            .ToArray());
        if (!string.Equals(safe, methodId, StringComparison.Ordinal) || safe.Length == 0)
        {
            throw new ArgumentException("The method id contains unsafe path characters.", nameof(methodId));
        }

        return Path.Combine(RootDirectory, safe);
    }

    public static string? Validate(ExecutionSitePreset? preset, string? expectedMethodId)
    {
        if (preset is null)
        {
            return "Preset data is empty.";
        }

        if (preset.SchemaVersion != ExecutionSitePreset.CurrentSchemaVersion)
        {
            return $"Unsupported schemaVersion {preset.SchemaVersion}.";
        }

        if (preset.Id == Guid.Empty)
        {
            return "Preset id is empty.";
        }

        if (string.IsNullOrWhiteSpace(preset.Name))
        {
            return "Preset name is empty.";
        }

        if (string.IsNullOrWhiteSpace(preset.MethodId))
        {
            return "Preset methodId is empty.";
        }

        if (!string.IsNullOrWhiteSpace(expectedMethodId) &&
            !string.Equals(preset.MethodId, expectedMethodId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Preset method '{preset.MethodId}' does not match '{expectedMethodId}'.";
        }

        preset.RootAnchor ??= new ExecutionPresetTransform();
        preset.Pieces ??= new List<ExecutionSitePiece>();
        preset.Markers ??= new List<ExecutionSiteMarker>();
        preset.Troops ??= new ExecutionSiteTroopSelection();
        if (preset.Pieces.Any(piece =>
                piece is null ||
                piece.Id == Guid.Empty ||
                string.IsNullOrWhiteSpace(piece.ResourceKey) ||
                piece.Transform is null))
        {
            return "One or more preset pieces are incomplete.";
        }

        if (preset.Markers.Any(marker =>
                marker is null ||
                marker.Id == Guid.Empty ||
                string.IsNullOrWhiteSpace(marker.RoleId) ||
                marker.Transform is null))
        {
            return "One or more preset markers are incomplete.";
        }

        return null;
    }

    private static DataContractJsonSerializer CreateSerializer() => new(
        typeof(ExecutionSitePreset),
        new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true,
            EmitTypeInformation = EmitTypeInformation.Never
        });
}
