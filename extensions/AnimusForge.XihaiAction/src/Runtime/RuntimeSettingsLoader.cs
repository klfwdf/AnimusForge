using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AnimusForge.SceneActions.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.XihaiAction
{
    internal sealed class SettingsLoadResult
    {
        public SceneActionSettings Settings { get; set; }
        public bool IsValid { get; set; }
        public bool UsedBuiltInDefault { get; set; }
        public bool MigratedFromV1 { get; set; }
        public bool MigratedFromV2 { get; set; }
        public bool MigratedFromV3 { get; set; }
        public int SchemaVersion { get; set; }
        public string SourcePath { get; set; }
        public string Error { get; set; }
    }

    internal static class RuntimeSettingsLoader
    {
        private const string SchemaIdV1 =
            "urn:animusforge:sceneactions:schema:v1:settings";
        private const string SchemaIdV2 =
            "urn:animusforge:sceneactions:schema:v2:settings";
        private const string SchemaIdV3 =
            "urn:animusforge:sceneactions:schema:v3:settings";
        private const string SchemaIdV4 =
            "urn:animusforge:sceneactions:schema:v4:settings";
        private static readonly Regex PackageIdPattern = new Regex(
            "^[a-z0-9]+(?:[._-][a-z0-9]+)*$",
            RegexOptions.CultureInvariant);
        private static readonly Regex LogicalKeyPattern = new Regex(
            "^[a-z0-9][a-z0-9_-]{0,63}$",
            RegexOptions.CultureInvariant);

        public static SettingsLoadResult Load(string path)
        {
            string selectedPath = path;
            bool migratedFromV1 = false;
            bool migratedFromV2 = false;
            bool migratedFromV3 = false;
            if (!File.Exists(selectedPath) &&
                string.Equals(
                    Path.GetFileName(selectedPath),
                    "settings.v4.json",
                    StringComparison.OrdinalIgnoreCase))
            {
                string directory = Path.GetDirectoryName(selectedPath) ?? string.Empty;
                string v3Path = Path.Combine(directory, "settings.v3.json");
                string v2Path = Path.Combine(directory, "settings.v2.json");
                string v1Path = Path.Combine(directory, "settings.v1.json");
                if (File.Exists(v3Path))
                {
                    selectedPath = v3Path;
                    migratedFromV3 = true;
                }
                else if (File.Exists(v2Path))
                {
                    selectedPath = v2Path;
                    migratedFromV2 = true;
                }
                else if (File.Exists(v1Path))
                {
                    selectedPath = v1Path;
                    migratedFromV1 = true;
                }
            }
            else if (!File.Exists(selectedPath) &&
                string.Equals(
                    Path.GetFileName(selectedPath),
                    "settings.v3.json",
                    StringComparison.OrdinalIgnoreCase))
            {
                string v2Path = Path.Combine(
                    Path.GetDirectoryName(selectedPath) ?? string.Empty,
                    "settings.v2.json");
                string v1Path = Path.Combine(
                    Path.GetDirectoryName(selectedPath) ?? string.Empty,
                    "settings.v1.json");
                if (File.Exists(v2Path))
                {
                    selectedPath = v2Path;
                    migratedFromV2 = true;
                }
                else if (File.Exists(v1Path))
                {
                    selectedPath = v1Path;
                    migratedFromV1 = true;
                }
            }
            else if (!File.Exists(selectedPath) &&
                     string.Equals(
                         Path.GetFileName(selectedPath),
                         "settings.v2.json",
                         StringComparison.OrdinalIgnoreCase))
            {
                string v1Path = Path.Combine(
                    Path.GetDirectoryName(selectedPath) ?? string.Empty,
                    "settings.v1.json");
                if (File.Exists(v1Path))
                {
                    selectedPath = v1Path;
                    migratedFromV1 = true;
                }
            }
            if (!File.Exists(selectedPath))
            {
                return new SettingsLoadResult
                {
                    Settings = CreateAuditedDefault(),
                    IsValid = true,
                    UsedBuiltInDefault = true,
                    SchemaVersion = 4,
                    SourcePath = path
                };
            }

            try
            {
                JObject root = LoadStrictObject(selectedPath);
                int schemaVersion = RequireInteger(root, "schemaVersion", 1, 4);
                SceneActionSettings settings = Parse(root, schemaVersion);
                if (schemaVersion == 1)
                {
                    ApplyV2MigrationDefaults(settings);
                }
                if (schemaVersion < 3)
                {
                    ApplyV3MigrationDefaults(settings);
                }
                if (schemaVersion < 4)
                {
                    ApplyV4MigrationDefaults(settings);
                }
                IReadOnlyList<string> errors = settings.Validate();
                if (errors.Count > 0)
                {
                    throw new InvalidDataException(string.Join(" ", errors));
                }
                return new SettingsLoadResult
                {
                    Settings = settings,
                    IsValid = true,
                    UsedBuiltInDefault = false,
                    MigratedFromV1 = migratedFromV1 || schemaVersion == 1,
                    MigratedFromV2 = migratedFromV2 || schemaVersion == 2,
                    MigratedFromV3 = migratedFromV3 || schemaVersion == 3,
                    SchemaVersion = schemaVersion,
                    SourcePath = selectedPath
                };
            }
            catch (Exception ex)
            {
                SceneActionSettings disabled = CreateAuditedDefault();
                disabled.Enabled = false;
                return new SettingsLoadResult
                {
                    Settings = disabled,
                    IsValid = false,
                    UsedBuiltInDefault = false,
                    MigratedFromV1 = migratedFromV1,
                    MigratedFromV2 = migratedFromV2,
                    MigratedFromV3 = migratedFromV3,
                    SchemaVersion = migratedFromV1 ? 1 : migratedFromV2 ? 2 :
                        migratedFromV3 ? 3 : 4,
                    SourcePath = selectedPath,
                    Error = ex.Message
                };
            }
        }

        public static SceneActionSettings CreateAuditedDefault()
        {
            return new SceneActionSettings
            {
                Enabled = true,
                PlayerSceneShoutEnabled = true,
                NpcSceneShoutReplyEnabled = true,
                ForceExactEnabled = true,
                ExactCommandEnabled = true,
                AiClassifierEnabled = true,
                AiClassifierProviderId = "animusforge.main.v130",
                ClassifierTimeoutMs = 6000,
                ClassifierMaxOutputChars = 64,
                RequestTtlMs = 8000,
                ConsentReplyTtlMs = 30000,
                MaxPendingRequests = 64,
                StaggerFromTargetCount = 4,
                StaggerSeconds = 0.1f,
                MaxBatchTargets = 16,
                MaxBatchTailSeconds = 2f,
                MaxQueuedTargets = 128,
                OverflowPolicy = SchedulerOverflowPolicy.Reject,
                ScreenBatchSummary = false,
                FailureRateLimitSeconds = 5f,
                LogLevel = "information",
                DeveloperDiagnosticsEnabled = false,
                AllowRegisteredActionIdProbe = false,
                MaxProgramActions = 4,
                StepTimeoutSeconds = 6f,
                IntermediateKneelHoldSeconds = 1f,
                IntermediateDanceSeconds = 4f,
                DualChannelExperimentalEnabled = true,
                ForceMultiTargetThreshold = 3,
                ForceStaggerMinSeconds = 0.01f,
                ForceStaggerMaxSeconds = 0.02f,
                ActionOverrides = new Dictionary<string, ActionOverride>(StringComparer.Ordinal),
                UserAliases = Array.Empty<AliasDefinition>()
            };
        }

        private static void ApplyV2MigrationDefaults(SceneActionSettings settings)
        {
            Dictionary<string, ActionOverride> migrated =
                new Dictionary<string, ActionOverride>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, ActionOverride> pair in
                     settings.ActionOverrides ??
                     new Dictionary<string, ActionOverride>(StringComparer.Ordinal))
            {
                migrated.Add(pair.Key, pair.Value);
            }
            foreach (string actionKey in SceneActionFrameworkV2.LogicalActions
                         .Where(entry => entry.Kind == IntentKind.PlayAction)
                         .Select(entry => entry.ActionKey)
                         .Distinct(StringComparer.Ordinal))
            {
                if (!migrated.ContainsKey(actionKey))
                {
                    migrated.Add(actionKey, new ActionOverride { Enabled = true });
                }
            }
            settings.ActionOverrides = migrated;
            settings.ClassifierMaxOutputChars = Math.Max(
                settings.ClassifierMaxOutputChars,
                128);
        }

        private static void ApplyV3MigrationDefaults(SceneActionSettings settings)
        {
            Dictionary<string, ActionOverride> migrated =
                new Dictionary<string, ActionOverride>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, ActionOverride> pair in
                     settings.ActionOverrides ??
                     new Dictionary<string, ActionOverride>(StringComparer.Ordinal))
            {
                migrated.Add(pair.Key, pair.Value);
            }
            foreach (string actionKey in SceneActionFrameworkV3.LogicalActions
                         .Skip(16)
                         .Where(entry => entry.Kind == IntentKind.PlayAction)
                         .Select(entry => entry.ActionKey)
                         .Distinct(StringComparer.Ordinal))
            {
                if (!migrated.ContainsKey(actionKey))
                {
                    migrated.Add(actionKey, new ActionOverride { Enabled = false });
                }
            }
            settings.ActionOverrides = migrated;
            settings.ClassifierMaxOutputChars = Math.Max(
                settings.ClassifierMaxOutputChars,
                128);
        }

        private static void ApplyV4MigrationDefaults(SceneActionSettings settings)
        {
            Dictionary<string, ActionOverride> migrated =
                new Dictionary<string, ActionOverride>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, ActionOverride> pair in
                     settings.ActionOverrides ??
                     new Dictionary<string, ActionOverride>(StringComparer.Ordinal))
            {
                migrated.Add(pair.Key, pair.Value);
            }
            foreach (string actionKey in SceneActionFrameworkV4.LogicalActions
                         .Skip(24)
                         .Where(entry => entry.Kind == IntentKind.PlayAction)
                         .Select(entry => entry.ActionKey)
                         .Distinct(StringComparer.Ordinal))
            {
                if (!migrated.ContainsKey(actionKey))
                {
                    migrated.Add(actionKey, new ActionOverride { Enabled = false });
                }
            }
            settings.ActionOverrides = migrated;
            settings.ClassifierMaxOutputChars = Math.Max(
                settings.ClassifierMaxOutputChars,
                128);
        }

        private static JObject LoadStrictObject(string path)
        {
            using (StreamReader commentStream = new StreamReader(path, true))
            using (JsonTextReader commentReader = new JsonTextReader(commentStream))
            {
                commentReader.DateParseHandling = DateParseHandling.None;
                commentReader.MaxDepth = 32;
                while (commentReader.Read())
                {
                    if (commentReader.TokenType == JsonToken.Comment)
                    {
                        throw new JsonException("JSON comments are not allowed.");
                    }
                }
            }

            using (StreamReader stream = new StreamReader(path, true))
            using (JsonTextReader reader = new JsonTextReader(stream))
            {
                reader.DateParseHandling = DateParseHandling.None;
                reader.FloatParseHandling = FloatParseHandling.Double;
                reader.MaxDepth = 32;
                JObject root = JObject.Load(reader, new JsonLoadSettings
                {
                    CommentHandling = CommentHandling.Load,
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    LineInfoHandling = LineInfoHandling.Load
                });
                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.None)
                    {
                        throw new JsonException("Trailing JSON content is not allowed.");
                    }
                }
                return root;
            }
        }

        private static SceneActionSettings Parse(JObject root, int schemaVersion)
        {
            RequireShape(root,
                schemaVersion >= 2
                    ? new[]
                    {
                        "schemaId", "documentType", "schemaVersion", "packageId", "enabled",
                        "inputSources", "resolvers", "requestGate", "scheduler",
                        "programExecution", "actionOverrides", "userAliases", "diagnostics",
                        "developerDiagnostics"
                    }
                    : new[]
                    {
                        "schemaId", "documentType", "schemaVersion", "packageId", "enabled",
                        "inputSources", "resolvers", "requestGate", "scheduler",
                        "actionOverrides", "userAliases", "diagnostics", "developerDiagnostics"
                    });
            RequireConstant(
                root,
                "schemaId",
                schemaVersion == 4 ? SchemaIdV4 : schemaVersion == 3 ? SchemaIdV3 :
                    schemaVersion == 2 ? SchemaIdV2 : SchemaIdV1);
            RequireConstant(root, "documentType", "scene-actions-settings");
            RequireInteger(root, "schemaVersion", schemaVersion, schemaVersion);
            string packageId = RequireString(root, "packageId", 1, 128);
            if (!PackageIdPattern.IsMatch(packageId))
            {
                throw Invalid("packageId", "invalid package id");
            }

            SceneActionSettings settings = CreateAuditedDefault();
            settings.Enabled = RequireBoolean(root, "enabled");

            JObject inputs = RequireObject(root, "inputSources");
            RequireShape(inputs, new[] { "playerSceneShout", "npcSceneShoutReply" });
            settings.PlayerSceneShoutEnabled = RequireBoolean(inputs, "playerSceneShout");
            settings.NpcSceneShoutReplyEnabled = RequireBoolean(inputs, "npcSceneShoutReply");

            JObject resolvers = RequireObject(root, "resolvers");
            RequireShape(resolvers, new[] { "forceExact", "exactCommand", "aiClassifier" });
            settings.ForceExactEnabled = ReadToggle(resolvers, "forceExact");
            settings.ExactCommandEnabled = ReadToggle(resolvers, "exactCommand");
            JObject classifier = RequireObject(resolvers, "aiClassifier");
            bool aiEnabled = RequireBoolean(classifier, "enabled");
            RequireShape(
                classifier,
                aiEnabled
                    ? new[] { "enabled", "providerId", "timeoutMs", "maxOutputChars" }
                    : new[] { "enabled", "timeoutMs", "maxOutputChars" });
            settings.AiClassifierEnabled = aiEnabled;
            settings.AiClassifierProviderId = aiEnabled
                ? RequireProviderId(classifier, "providerId")
                : null;
            settings.ClassifierTimeoutMs = RequireInteger(classifier, "timeoutMs", 100, 30000);
            settings.ClassifierMaxOutputChars =
                RequireInteger(classifier, "maxOutputChars", 4, 128);

            JObject requestGate = RequireObject(root, "requestGate");
            RequireShape(requestGate, new[]
            {
                "requestTtlMs", "consentReplyTtlMs", "maxPendingRequests"
            });
            settings.RequestTtlMs = RequireInteger(requestGate, "requestTtlMs", 500, 60000);
            settings.ConsentReplyTtlMs =
                RequireInteger(requestGate, "consentReplyTtlMs", 1000, 300000);
            settings.MaxPendingRequests =
                RequireInteger(requestGate, "maxPendingRequests", 1, 512);

            JObject scheduler = RequireObject(root, "scheduler");
            RequireShape(
                scheduler,
                schemaVersion >= 2
                    ? new[]
                    {
                        "staggerFromTargetCount", "staggerSeconds", "forceMultiTargetThreshold",
                        "forceStaggerMinSeconds", "forceStaggerMaxSeconds", "maxBatchTargets",
                        "maxBatchTailSeconds", "overflowPolicy", "maxQueuedTargets"
                    }
                    : new[]
                    {
                        "staggerFromTargetCount", "staggerSeconds", "maxBatchTargets",
                        "maxBatchTailSeconds", "overflowPolicy", "maxQueuedTargets"
                    });
            settings.StaggerFromTargetCount =
                RequireInteger(scheduler, "staggerFromTargetCount", 1, 64);
            settings.StaggerSeconds = RequireNumber(scheduler, "staggerSeconds", 0, 2);
            if (schemaVersion >= 2)
            {
                settings.ForceMultiTargetThreshold =
                    RequireInteger(scheduler, "forceMultiTargetThreshold", 3, 64);
                settings.ForceStaggerMinSeconds =
                    RequireNumber(scheduler, "forceStaggerMinSeconds", 0, 0.25);
                settings.ForceStaggerMaxSeconds =
                    RequireNumber(scheduler, "forceStaggerMaxSeconds", 0, 0.25);
            }
            settings.MaxBatchTargets = RequireInteger(scheduler, "maxBatchTargets", 1, 64);
            settings.MaxBatchTailSeconds =
                RequireNumber(scheduler, "maxBatchTailSeconds", 0, 30);
            settings.MaxQueuedTargets =
                RequireInteger(scheduler, "maxQueuedTargets", 1, 512);
            string overflow = RequireString(scheduler, "overflowPolicy", 1, 32);
            if (overflow == "reject")
            {
                settings.OverflowPolicy = SchedulerOverflowPolicy.Reject;
            }
            else if (overflow == "truncate_stable")
            {
                settings.OverflowPolicy = SchedulerOverflowPolicy.TruncateStable;
            }
            else
            {
                throw Invalid("scheduler.overflowPolicy", "unknown policy");
            }

            if (schemaVersion >= 2)
            {
                JObject execution = RequireObject(root, "programExecution");
                RequireShape(execution, new[]
                {
                    "maxActions", "stepTimeoutSeconds", "intermediateKneelHoldSeconds",
                    "intermediateDanceSeconds", "dualChannelExperimental"
                });
                settings.MaxProgramActions = RequireInteger(execution, "maxActions", 4, 4);
                settings.StepTimeoutSeconds =
                    RequireNumber(execution, "stepTimeoutSeconds", 0.5, 30);
                settings.IntermediateKneelHoldSeconds =
                    RequireNumber(execution, "intermediateKneelHoldSeconds", 0, 10);
                settings.IntermediateDanceSeconds =
                    RequireNumber(execution, "intermediateDanceSeconds", 0.5, 30);
                settings.DualChannelExperimentalEnabled =
                    RequireBoolean(execution, "dualChannelExperimental");
            }

            settings.ActionOverrides = ParseActionOverrides(
                RequireObject(root, "actionOverrides"),
                schemaVersion);
            settings.UserAliases = ParseUserAliases(
                RequireArray(root, "userAliases"),
                schemaVersion);

            JObject diagnostics = RequireObject(root, "diagnostics");
            RequireShape(diagnostics,
                new[] { "logLevel", "screenBatchSummary", "failureRateLimitSeconds" });
            string logLevel = RequireString(diagnostics, "logLevel", 1, 32);
            if (!new[] { "trace", "debug", "information", "warning", "error" }
                .Contains(logLevel, StringComparer.Ordinal))
            {
                throw Invalid("diagnostics.logLevel", "unknown level");
            }
            settings.LogLevel = logLevel;
            settings.ScreenBatchSummary = RequireBoolean(diagnostics, "screenBatchSummary");
            settings.FailureRateLimitSeconds =
                RequireNumber(diagnostics, "failureRateLimitSeconds", 0, 300);

            JObject developer = RequireObject(root, "developerDiagnostics");
            RequireShape(developer, new[] { "enabled", "allowRegisteredActionIdProbe" });
            settings.DeveloperDiagnosticsEnabled = RequireBoolean(developer, "enabled");
            settings.AllowRegisteredActionIdProbe =
                RequireBoolean(developer, "allowRegisteredActionIdProbe");
            if (settings.AllowRegisteredActionIdProbe)
            {
                throw Invalid(
                    "developerDiagnostics.allowRegisteredActionIdProbe",
                    "raw action-id probing is not compiled into this production build");
            }

            return settings;
        }

        private static IReadOnlyDictionary<string, ActionOverride> ParseActionOverrides(
            JObject value,
            int schemaVersion)
        {
            Dictionary<string, ActionOverride> result =
                new Dictionary<string, ActionOverride>(StringComparer.Ordinal);
            foreach (JProperty property in value.Properties())
            {
                if (!LogicalKeyPattern.IsMatch(property.Name) ||
                    property.Name.StartsWith("act_", StringComparison.Ordinal))
                {
                    throw Invalid("actionOverrides." + property.Name, "invalid logical key");
                }
                if (!IsKnownActionKey(property.Name, schemaVersion))
                {
                    throw Invalid(
                        "actionOverrides." + property.Name,
                        "action is outside the frozen schema V" + schemaVersion + " contract");
                }
                JObject body = property.Value as JObject ??
                    throw Invalid("actionOverrides." + property.Name, "expected object");
                RequireAllowed(body, new[] { "enabled", "blendInSeconds", "cooldownSeconds" });
                if (!body.Properties().Any())
                {
                    throw Invalid("actionOverrides." + property.Name, "empty override");
                }
                ActionOverride item = new ActionOverride();
                if (body.TryGetValue("enabled", StringComparison.Ordinal, out JToken enabled))
                {
                    item.Enabled = RequireBooleanToken(enabled, property.Name + ".enabled");
                }
                if (body.TryGetValue(
                    "blendInSeconds", StringComparison.Ordinal, out JToken blend))
                {
                    item.BlendInSeconds = RequireNumberToken(
                        blend, property.Name + ".blendInSeconds", 0, 2);
                }
                if (body.TryGetValue(
                    "cooldownSeconds", StringComparison.Ordinal, out JToken cooldown))
                {
                    item.CooldownSeconds = RequireNumberToken(
                        cooldown, property.Name + ".cooldownSeconds", 0, 60);
                }
                result.Add(property.Name, item);
            }
            return result;
        }

        private static IReadOnlyList<AliasDefinition> ParseUserAliases(
            JArray value,
            int schemaVersion)
        {
            List<AliasDefinition> result = new List<AliasDefinition>();
            foreach (JToken token in value)
            {
                JObject alias = token as JObject ??
                    throw Invalid("userAliases", "array item must be an object");
                RequireShape(alias, new[] { "text", "locale", "intentKey", "permissions" });
                string text = RequireString(alias, "text", 1, 32);
                string normalized = CommandParser.Normalize(text);
                if (!string.Equals(text, text.Trim(), StringComparison.Ordinal) ||
                    text.IndexOf('*') >= 0 ||
                    text.StartsWith("act_", StringComparison.OrdinalIgnoreCase) ||
                    text.Any(char.IsControl))
                {
                    throw Invalid("userAliases.text", "unsafe alias text");
                }
                string locale = RequireString(alias, "locale", 1, 16);
                if (locale != "zh-Hans" && locale != "en" && locale != "invariant")
                {
                    throw Invalid("userAliases.locale", "unsupported locale");
                }
                string intentKey = RequireString(alias, "intentKey", 1, 64);
                if (!LogicalKeyPattern.IsMatch(intentKey) ||
                    intentKey.StartsWith("act_", StringComparison.Ordinal))
                {
                    throw Invalid("userAliases.intentKey", "invalid logical key");
                }
                if (!IsKnownIntentKey(intentKey, schemaVersion))
                {
                    throw Invalid(
                        "userAliases.intentKey",
                        "intent is outside the frozen schema V" + schemaVersion + " contract");
                }

                JObject permissions = RequireObject(alias, "permissions");
                RequireShape(permissions, new[] { "inputSources", "resolvers" });
                JArray sources = RequireArray(permissions, "inputSources");
                if (sources.Count != 1 ||
                    sources[0].Type != JTokenType.String ||
                    (string)sources[0] != "player_scene_shout")
                {
                    throw Invalid("userAliases.permissions.inputSources", "player source only");
                }
                JArray resolvers = RequireArray(permissions, "resolvers");
                if (resolvers.Count < 1)
                {
                    throw Invalid("userAliases.permissions.resolvers", "empty resolver list");
                }
                HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
                foreach (JToken resolver in resolvers)
                {
                    if (resolver.Type != JTokenType.String ||
                        !unique.Add((string)resolver) ||
                        ((string)resolver != "force_exact" &&
                         (string)resolver != "exact_command"))
                    {
                        throw Invalid("userAliases.permissions.resolvers", "invalid resolver");
                    }
                }
                result.Add(new AliasDefinition
                {
                    Text = normalized,
                    IntentKey = intentKey,
                    AllowForceExact = unique.Contains("force_exact"),
                    AllowExactCommand = unique.Contains("exact_command"),
                    TargetOverride = null
                });
            }
            return result;
        }

        private static bool IsKnownActionKey(string actionKey, int schemaVersion)
        {
            if (schemaVersion == 1)
            {
                return SceneActionFrameworkV1.LogicalActions.Any(entry =>
                    entry.Kind == IntentKind.PlayAction &&
                    string.Equals(entry.ActionKey, actionKey, StringComparison.Ordinal));
            }
            if (schemaVersion == 2)
            {
                return SceneActionFrameworkV2.LogicalActions.Any(entry =>
                    entry.Kind == IntentKind.PlayAction &&
                    string.Equals(entry.ActionKey, actionKey, StringComparison.Ordinal));
            }
            if (schemaVersion == 3)
            {
                return SceneActionFrameworkV3.LogicalActions.Any(entry =>
                    entry.Kind == IntentKind.PlayAction &&
                    string.Equals(entry.ActionKey, actionKey, StringComparison.Ordinal));
            }
            return SceneActionFrameworkV4.LogicalActions.Any(entry =>
                entry.Kind == IntentKind.PlayAction &&
                string.Equals(entry.ActionKey, actionKey, StringComparison.Ordinal));
        }

        private static bool IsKnownIntentKey(string intentKey, int schemaVersion)
        {
            if (schemaVersion == 1)
            {
                return SceneActionFrameworkV1.IsLogicalIntent(intentKey);
            }
            if (schemaVersion == 2)
            {
                return SceneActionFrameworkV2.IsLogicalIntent(intentKey);
            }
            return schemaVersion == 3
                ? SceneActionFrameworkV3.IsLogicalIntent(intentKey)
                : SceneActionFrameworkV4.IsLogicalIntent(intentKey);
        }

        private static bool ReadToggle(JObject parent, string name)
        {
            JObject value = RequireObject(parent, name);
            RequireShape(value, new[] { "enabled" });
            return RequireBoolean(value, "enabled");
        }

        private static string RequireProviderId(JObject value, string name)
        {
            string result = RequireString(value, name, 1, 128);
            if (!PackageIdPattern.IsMatch(result))
            {
                throw Invalid(name, "invalid provider id");
            }
            return result;
        }

        private static void RequireShape(JObject value, IEnumerable<string> names)
        {
            string[] allowed = names.ToArray();
            RequireAllowed(value, allowed);
            foreach (string name in allowed)
            {
                if (value.Property(name, StringComparison.Ordinal) == null)
                {
                    throw Invalid(name, "required property is missing");
                }
            }
        }

        private static void RequireAllowed(JObject value, IEnumerable<string> names)
        {
            HashSet<string> allowed = new HashSet<string>(names, StringComparer.Ordinal);
            JProperty unknown = value.Properties().FirstOrDefault(p => !allowed.Contains(p.Name));
            if (unknown != null)
            {
                throw Invalid(unknown.Name, "unknown property");
            }
        }

        private static void RequireConstant(JObject value, string name, string expected)
        {
            string actual = RequireString(value, name, 1, 256);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw Invalid(name, "unexpected constant");
            }
        }

        private static JObject RequireObject(JObject value, string name)
        {
            return value[name] as JObject ?? throw Invalid(name, "expected object");
        }

        private static JArray RequireArray(JObject value, string name)
        {
            return value[name] as JArray ?? throw Invalid(name, "expected array");
        }

        private static bool RequireBoolean(JObject value, string name)
        {
            return RequireBooleanToken(value[name], name);
        }

        private static bool RequireBooleanToken(JToken value, string name)
        {
            if (value == null || value.Type != JTokenType.Boolean)
            {
                throw Invalid(name, "expected boolean");
            }
            return (bool)value;
        }

        private static int RequireInteger(JObject value, string name, int min, int max)
        {
            JToken token = value[name];
            if (token == null || token.Type != JTokenType.Integer)
            {
                throw Invalid(name, "expected integer");
            }
            long number = (long)token;
            if (number < min || number > max)
            {
                throw Invalid(name, "integer is outside the allowed range");
            }
            return checked((int)number);
        }

        private static float RequireNumber(JObject value, string name, double min, double max)
        {
            return RequireNumberToken(value[name], name, min, max);
        }

        private static float RequireNumberToken(
            JToken token,
            string name,
            double min,
            double max)
        {
            if (token == null ||
                (token.Type != JTokenType.Integer && token.Type != JTokenType.Float))
            {
                throw Invalid(name, "expected number");
            }
            double number = Convert.ToDouble(((JValue)token).Value, CultureInfo.InvariantCulture);
            if (double.IsNaN(number) || double.IsInfinity(number) || number < min || number > max)
            {
                throw Invalid(name, "number is outside the allowed range");
            }
            return (float)number;
        }

        private static string RequireString(JObject value, string name, int min, int max)
        {
            JToken token = value[name];
            if (token == null || token.Type != JTokenType.String)
            {
                throw Invalid(name, "expected string");
            }
            string text = (string)token;
            if (text.Length < min || text.Length > max)
            {
                throw Invalid(name, "string length is outside the allowed range");
            }
            return text;
        }

        private static InvalidDataException Invalid(string name, string reason)
        {
            return new InvalidDataException(name + ": " + reason + ".");
        }
    }
}
