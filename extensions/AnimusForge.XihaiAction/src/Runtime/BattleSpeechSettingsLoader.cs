using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimusForge.SceneActions.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.XihaiAction
{
    internal static class BattleSpeechSettingsLoader
    {
        private const string SchemaId = "urn:animusforge:sceneactions:battle-speech:v1";
        private const string DocumentType = "battle-speech-settings";
        private static readonly HashSet<string> AllowedProperties = new HashSet<string>(
            new[]
            {
                "schemaId", "documentType", "schemaVersion", "enabled",
                "allowDeployment", "allowPreEngagement", "playerCaptureSeconds",
                "npcReplySeconds", "maxSpeechChars", "audienceRadiusMeters",
                "enemyInterruptRadiusMeters", "enemyScanIntervalSeconds",
                "minimumAudience", "maximumAudience", "minimumDurationSeconds",
                "maximumDurationSeconds", "charactersPerSecond", "screenNotifications"
            },
            StringComparer.Ordinal);

        public static BattleSpeechSettingsV1 Load(
            string moduleRoot,
            out bool valid,
            out string reason)
        {
            valid = false;
            reason = null;
            BattleSpeechSettingsV1 defaults = new BattleSpeechSettingsV1();
            if (string.IsNullOrWhiteSpace(moduleRoot))
            {
                reason = "Module root is empty.";
                return defaults;
            }

            string path = Path.Combine(
                moduleRoot,
                "ModuleData",
                "SceneActions",
                "battle-speech.v1.json");
            if (!File.Exists(path))
            {
                reason = "battle-speech.v1.json is missing; audited defaults selected.";
                valid = true;
                return defaults;
            }

            try
            {
                JObject document = LoadStrictObject(path);
                RequireShape(document);
                RequireString(document, "schemaId", SchemaId);
                RequireString(document, "documentType", DocumentType);
                RequireInt(document, "schemaVersion", 1, 1);

                BattleSpeechSettingsV1 settings = new BattleSpeechSettingsV1
                {
                    Enabled = RequireBool(document, "enabled"),
                    AllowDeployment = RequireBool(document, "allowDeployment"),
                    AllowPreEngagement = RequireBool(document, "allowPreEngagement"),
                    PlayerCaptureSeconds = RequireFloat(document, "playerCaptureSeconds"),
                    NpcReplySeconds = RequireFloat(document, "npcReplySeconds"),
                    MaxSpeechChars = RequireInt(document, "maxSpeechChars", int.MinValue, int.MaxValue),
                    AudienceRadiusMeters = RequireFloat(document, "audienceRadiusMeters"),
                    EnemyInterruptRadiusMeters = RequireFloat(document, "enemyInterruptRadiusMeters"),
                    EnemyScanIntervalSeconds = RequireFloat(document, "enemyScanIntervalSeconds"),
                    MinimumAudience = RequireInt(document, "minimumAudience", int.MinValue, int.MaxValue),
                    MaximumAudience = RequireInt(document, "maximumAudience", int.MinValue, int.MaxValue),
                    MinimumDurationSeconds = RequireFloat(document, "minimumDurationSeconds"),
                    MaximumDurationSeconds = RequireFloat(document, "maximumDurationSeconds"),
                    CharactersPerSecond = RequireFloat(document, "charactersPerSecond"),
                    ScreenNotifications = RequireBool(document, "screenNotifications")
                };
                var errors = settings.Validate();
                if (errors.Count > 0)
                {
                    reason = string.Join("; ", errors);
                    return defaults;
                }
                valid = true;
                reason = "Strict battle speech settings loaded: " + path;
                return settings;
            }
            catch (Exception ex)
            {
                reason = ex.GetType().Name + ": " + ex.Message;
                return defaults;
            }
        }

        private static JObject LoadStrictObject(string path)
        {
            using (StreamReader commentStream = new StreamReader(path, true))
            using (JsonTextReader commentReader = new JsonTextReader(commentStream))
            {
                commentReader.DateParseHandling = DateParseHandling.None;
                commentReader.MaxDepth = 16;
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
                reader.MaxDepth = 16;
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

        private static void RequireShape(JObject document)
        {
            JProperty unknown = document.Properties().FirstOrDefault(property =>
                !AllowedProperties.Contains(property.Name));
            if (unknown != null)
            {
                throw new JsonException("Unknown battle speech property: " + unknown.Name);
            }
            string missing = AllowedProperties.FirstOrDefault(name => document[name] == null);
            if (missing != null)
            {
                throw new JsonException("Missing battle speech property: " + missing);
            }
        }

        private static void RequireString(JObject document, string name, string expected)
        {
            JToken token = document[name];
            if (token?.Type != JTokenType.String ||
                !string.Equals(token.Value<string>(), expected, StringComparison.Ordinal))
            {
                throw new JsonException(name + " must equal " + expected + ".");
            }
        }

        private static bool RequireBool(JObject document, string name)
        {
            JToken token = document[name];
            if (token?.Type != JTokenType.Boolean)
            {
                throw new JsonException(name + " must be a boolean.");
            }
            return token.Value<bool>();
        }

        private static int RequireInt(JObject document, string name, int minimum, int maximum)
        {
            JToken token = document[name];
            if (token?.Type != JTokenType.Integer)
            {
                throw new JsonException(name + " must be an integer.");
            }
            int value = token.Value<int>();
            if (value < minimum || value > maximum)
            {
                throw new JsonException(name + " is outside the allowed range.");
            }
            return value;
        }

        private static float RequireFloat(JObject document, string name)
        {
            JToken token = document[name];
            if (token == null ||
                (token.Type != JTokenType.Float && token.Type != JTokenType.Integer))
            {
                throw new JsonException(name + " must be numeric.");
            }
            return token.Value<float>();
        }
    }
}
