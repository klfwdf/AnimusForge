using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.SceneActions.Core
{
    public sealed class SceneActionContractEntryV4
    {
        internal SceneActionContractEntryV4(
            string intentKey,
            IntentKind kind,
            string actionKey,
            ActionMode? playbackMode,
            bool canOverlayKneel,
            string displayNameZhCn,
            string semanticDescriptionZhCn,
            IEnumerable<string> exactAliases,
            IEnumerable<string> performedCues,
            IEnumerable<string> referenceCues,
            IEnumerable<string> positiveExamples,
            IEnumerable<string> negativeExamples)
        {
            IntentKey = intentKey;
            Kind = kind;
            ActionKey = actionKey;
            PlaybackMode = playbackMode;
            CanOverlayKneel = canOverlayKneel;
            DisplayNameZhCn = displayNameZhCn;
            SemanticDescriptionZhCn = semanticDescriptionZhCn;
            ExactAliases = Freeze(exactAliases);
            PerformedCues = Freeze(performedCues);
            ReferenceCues = Freeze(referenceCues);
            PositiveExamples = Freeze(positiveExamples);
            NegativeExamples = Freeze(negativeExamples);
        }

        public string IntentKey { get; }
        public IntentKind Kind { get; }
        public string ActionKey { get; }
        public ActionMode? PlaybackMode { get; }
        public bool CanOverlayKneel { get; }
        public string DisplayNameZhCn { get; }
        public string SemanticDescriptionZhCn { get; }
        public IReadOnlyList<string> ExactAliases { get; }
        public IReadOnlyList<string> PerformedCues { get; }
        public IReadOnlyList<string> ReferenceCues { get; }
        public IReadOnlyList<string> PositiveExamples { get; }
        public IReadOnlyList<string> NegativeExamples { get; }

        private static IReadOnlyList<string> Freeze(IEnumerable<string> values)
        {
            return Array.AsReadOnly((values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray());
        }
    }

    /// <summary>
    /// V4 freezes twenty-seven logical intents. It preserves the V1/V2/V3
    /// semantic surfaces and adds three gesture-only commands without exposing
    /// native action ids to text input or the classifier.
    /// </summary>
    public static class SceneActionFrameworkV4
    {
        public const int ContractVersion = 4;

        public const string Kneel = SceneActionFrameworkV3.Kneel;
        public const string StandUp = SceneActionFrameworkV3.StandUp;
        public const string Xihai = SceneActionFrameworkV3.Xihai;
        public const string Cheer = SceneActionFrameworkV3.Cheer;
        public const string Applaud = SceneActionFrameworkV3.Applaud;
        public const string Respect = SceneActionFrameworkV3.Respect;
        public const string Threat = SceneActionFrameworkV3.Threat;
        public const string Surrender = SceneActionFrameworkV3.Surrender;
        public const string Laugh = SceneActionFrameworkV3.Laugh;
        public const string Point = SceneActionFrameworkV3.Point;
        public const string Rage = SceneActionFrameworkV3.Rage;
        public const string Fear = SceneActionFrameworkV3.Fear;
        public const string Disappointed = SceneActionFrameworkV3.Disappointed;
        public const string Challenge = SceneActionFrameworkV3.Challenge;
        public const string Search = SceneActionFrameworkV3.Search;
        public const string Dance = SceneActionFrameworkV3.Dance;
        public const string Greet = SceneActionFrameworkV3.Greet;
        public const string Agree = SceneActionFrameworkV3.Agree;
        public const string Disagree = SceneActionFrameworkV3.Disagree;
        public const string Unsure = SceneActionFrameworkV3.Unsure;
        public const string Explain = SceneActionFrameworkV3.Explain;
        public const string Promise = SceneActionFrameworkV3.Promise;
        public const string CrossArms = SceneActionFrameworkV3.CrossArms;
        public const string DeepBow = SceneActionFrameworkV3.DeepBow;
        public const string Command = "command";
        public const string FollowMe = "follow_me";
        public const string CutThroat = "cut_throat";

        private static readonly ReadOnlyCollection<SceneActionContractEntryV4> Entries =
            Array.AsReadOnly(SceneActionFrameworkV3.LogicalActions
                .Select(Legacy)
                .Concat(new[]
                {
                    NewAction(
                        Command,
                        SceneActionCatalogText.Get("command", true).Name,
                        SceneActionCatalogText.Get("command", true).Description,
                        new[]
                        {
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0001"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0002"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0003"), "command",
                            "command gesture", "issue order gesture"
                        },
                        new[]
                        {
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0004"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0005"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0006"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0007"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0008"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0009"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0010"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0011"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0012"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0013"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0014"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0015"),
                            "raised his arm to command the group",
                            "swept her arm in a command gesture",
                            "swept his arm to command the entire group",
                            "swept her arm to command the entire group",
                            "gestured an order to the troops", SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0016"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0017"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0018"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0003"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0019"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0020"), "issued a command with a sweeping arm"
                        },
                        new[]
                        {
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0001"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0002"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0021"),
                            "command", "order", "ordered",
                            "command gesture", "issue order gesture"
                        },
                        SceneActionCatalogText.Get("command", true).Positive,
                        SceneActionCatalogText.Get("command", true).Negative),
                    NewAction(
                        FollowMe,
                        SceneActionCatalogText.Get("follow_me", true).Name,
                        SceneActionCatalogText.Get("follow_me", true).Description,
                        new[]
                        {
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0022"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0023"), "follow_me",
                            "follow me gesture", "beckon to follow"
                        },
                        new[]
                        {
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0022"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0024"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0025"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0026"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0027"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0028"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0029"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0030"),
                            "beckoned his companions to follow",
                            "waved the group forward to follow", SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0031"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0032"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0033"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0034"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0035"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0036"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0037"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0038"), "waved the group forward"
                        },
                        new[]
                        {
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0023"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0039"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0040"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0041"),
                            "follow me", "follow me gesture", "beckon to follow"
                        },
                        SceneActionCatalogText.Get("follow_me", true).Positive,
                        SceneActionCatalogText.Get("follow_me", true).Negative),
                    NewAction(
                        CutThroat,
                        SceneActionCatalogText.Get("cut_throat", true).Name,
                        SceneActionCatalogText.Get("cut_throat", true).Description,
                        new[]
                        {
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0042"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0043"), "cut_throat",
                            "cut throat gesture", "throat cutting gesture"
                        },
                        new[]
                        {
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0044"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0045"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0046"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0047"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0048"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0049"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0050"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0051"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0052"), "made a throat-cutting gesture",
                            "drew a finger across his throat", "drew one finger across his throat",
                            "drew one finger across her throat", "ran a finger across his throat", SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0053"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0054"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0055"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0056"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0057"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0058"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0059"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0060")
                        },
                        new[]
                        {
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0042"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0043"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0061"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0062"),
                            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0063"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0064"), "cut throat", "cut throat gesture", "slit your throat"
                        },
                        SceneActionCatalogText.Get("cut_throat", true).Positive,
                        SceneActionCatalogText.Get("cut_throat", true).Negative)
                })
                .ToArray());

        private static readonly HashSet<string> NewIntentKeys = new HashSet<string>(
            Entries.Skip(24).Select(entry => entry.IntentKey),
            StringComparer.Ordinal);

        private static readonly HashSet<string> KneelOverlayKeys = new HashSet<string>(
            Entries.Where(entry => entry.CanOverlayKneel).Select(entry => entry.IntentKey),
            StringComparer.Ordinal);

        private static readonly string[] ActualViolenceCues =
        {
            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0065"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0066"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0067"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0068"),
            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0069"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0070"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0071"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0072"),
            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0073"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0074"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0075"),
            SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0076"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0077"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0078"), SceneActionVocabulary.Text("SceneActionFrameworkV4.LegacySource0079"),
            "drew a sword", "drew his sword", "drew her sword",
            "attacked the target", "attacked him", "attacked her",
            "slashed his throat", "slashed her throat", "cut his throat",
            "cut her throat", "slit his throat", "slit her throat",
            "stabbed his throat", "stabbed her throat"
        };

        public static IReadOnlyList<SceneActionContractEntryV4> LogicalActions => Entries;

        public static bool IsLogicalIntent(string intentKey)
        {
            return Entries.Any(entry => string.Equals(
                entry.IntentKey,
                intentKey,
                StringComparison.Ordinal));
        }

        public static bool CanOverlayKneel(string intentKey)
        {
            return KneelOverlayKeys.Contains(intentKey ?? string.Empty);
        }

        /// <summary>
        /// Verifies an AI-selected NPC reply program against the actual current
        /// reply text.  The classifier may infer one contextual emotion, but it
        /// must not invent an entity action such as kneel, salute, point, or
        /// command merely because that key is present in the allow-list.
        /// </summary>
        public static bool ValidateNpcClassifierProgramEvidence(
            string currentReplyText,
            ActionProgramV4 program,
            out string reason)
        {
            reason = null;
            if (program == null)
            {
                reason = "NPC classifier returned no action program.";
                return false;
            }

            string evidenceText = CommandParser.Normalize(currentReplyText ?? string.Empty);
            HashSet<string> resolvedSpecialActions = new HashSet<string>(
                ResolveNaturalActionDescription(evidenceText),
                StringComparer.Ordinal);
            foreach (string intentKey in program.Steps.SelectMany(step => step.IntentKeys))
            {
                if (IsContextInferableNpcEmotion(intentKey))
                {
                    continue;
                }
                bool hasEvidence = IsSpecialEvidenceAction(intentKey)
                    ? resolvedSpecialActions.Contains(intentKey)
                    : SceneActionFrameworkV1.ContainsPhysicalActionEvidence(
                        intentKey,
                        evidenceText);
                if (hasEvidence)
                {
                    continue;
                }

                reason = "NPC classifier selected " + intentKey +
                         " without current-reply performed-action evidence.";
                return false;
            }
            return true;
        }

        private static bool IsSpecialEvidenceAction(string intentKey)
        {
            return string.Equals(intentKey, Kneel, StringComparison.Ordinal) ||
                   string.Equals(intentKey, StandUp, StringComparison.Ordinal) ||
                   string.Equals(intentKey, Xihai, StringComparison.Ordinal) ||
                   string.Equals(intentKey, Respect, StringComparison.Ordinal) ||
                   string.Equals(intentKey, DeepBow, StringComparison.Ordinal);
        }

        private static bool IsContextInferableNpcEmotion(string intentKey)
        {
            return string.Equals(intentKey, Fear, StringComparison.Ordinal) ||
                   string.Equals(intentKey, Rage, StringComparison.Ordinal) ||
                   string.Equals(intentKey, Disappointed, StringComparison.Ordinal) ||
                   string.Equals(intentKey, Laugh, StringComparison.Ordinal) ||
                   string.Equals(intentKey, Unsure, StringComparison.Ordinal);
        }

        public static IReadOnlyList<string> ResolveNpcReplyDescription(string text)
        {
            return ResolveNaturalActionDescription(text);
        }

        public static IReadOnlyList<string> ResolveNaturalActionDescription(string text)
        {
            string normalized = CommandParser.Normalize(text);
            if (string.IsNullOrEmpty(normalized))
            {
                return Array.Empty<string>();
            }

            List<string> resolved = new List<string>(
                SceneActionFrameworkV3.ResolveNaturalActionDescription(normalized));
            foreach (SceneActionContractEntryV4 entry in Entries.Skip(24))
            {
                if (entry.PerformedCues.Any(cue =>
                        SceneActionFrameworkV1.ContainsPerformedCue(
                            normalized,
                            CommandParser.Normalize(cue))))
                {
                    resolved.Add(entry.IntentKey);
                }
            }

            List<CueSpan> performedMatches = FindCueSpans(
                normalized,
                entry => entry.PerformedCues);
            HashSet<string> matchedKeys = new HashSet<string>(
                performedMatches.Select(match => match.IntentKey),
                StringComparer.Ordinal);
            HashSet<string> preferredKeys = new HashSet<string>(
                SelectLongestCueSpans(performedMatches)
                    .Select(match => match.IntentKey),
                StringComparer.Ordinal);
            resolved.RemoveAll(key => matchedKeys.Contains(key) && !preferredKeys.Contains(key));
            return resolved.Distinct(StringComparer.Ordinal).ToArray();
        }

        public static IReadOnlyList<string> ResolveNaturalActionReferences(string text)
        {
            string normalized = CommandParser.Normalize(text);
            if (string.IsNullOrEmpty(normalized))
            {
                return Array.Empty<string>();
            }
            return SelectLongestCueSpans(FindCueSpans(
                    normalized,
                    entry => entry.ReferenceCues.Concat(entry.PerformedCues)))
                .OrderBy(match => match.Start)
                .ThenByDescending(match => match.Length)
                .Select(match => match.IntentKey)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public static bool ContainsNaturalActionReference(string text)
        {
            return ResolveNaturalActionReferences(text).Count > 0 ||
                   SceneActionFrameworkV3.ContainsNaturalActionReference(text);
        }

        public static bool ContainsUnsupportedActionReference(string text)
        {
            string normalized = CommandParser.Normalize(text);
            return SceneActionFrameworkV3.ContainsUnsupportedActionReference(normalized) ||
                   ActualViolenceCues.Any(cue => normalized.IndexOf(
                       cue,
                       StringComparison.Ordinal) >= 0);
        }

        public static bool HasSuppressedKnownActionReference(
            string text,
            IEnumerable<string> performedIntents)
        {
            HashSet<string> performed = new HashSet<string>(
                performedIntents ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            HashSet<string> legacyCoverage = new HashSet<string>(performed, StringComparer.Ordinal);
            if (performed.Contains(Command) ||
                performed.Contains(FollowMe) ||
                performed.Contains(CutThroat))
            {
                // V3 deliberately keeps the generic word "手势" as an explain
                // reference cue. A more specific, actually performed V4 gesture
                // must cover that reference without making "手势" executable.
                legacyCoverage.Add(Explain);
            }
            if (performed.Contains(Command))
            {
                legacyCoverage.Add(Point);
                legacyCoverage.Add(Greet);
            }
            if (performed.Contains(FollowMe))
            {
                legacyCoverage.Add(Greet);
                legacyCoverage.Add(Challenge);
            }
            if (performed.Contains(CutThroat))
            {
                legacyCoverage.Add(Threat);
            }
            if (SceneActionFrameworkV3.HasSuppressedKnownActionReference(text, legacyCoverage))
            {
                return true;
            }
            return ResolveNaturalActionReferences(text)
                .Where(NewIntentKeys.Contains)
                .Any(key => !performed.Contains(key) &&
                            !IsCoveredByPerformedGesture(performed, key) &&
                            HasUnsuppressedReference(text, key));
        }

        private static bool HasUnsuppressedReference(
            string text,
            string intentKey)
        {
            if (SceneActionFrameworkV3.IsLogicalIntent(intentKey))
            {
                return SceneActionFrameworkV3.HasUnsuppressedReference(text, intentKey);
            }

            string normalized = CommandParser.Normalize(text);
            SceneActionContractEntryV4 entry = Entries.Single(value =>
                string.Equals(value.IntentKey, intentKey, StringComparison.Ordinal));
            foreach (string cue in entry.ReferenceCues.Concat(entry.PerformedCues))
            {
                string normalizedCue = CommandParser.Normalize(cue);
                if (string.IsNullOrEmpty(normalizedCue))
                {
                    continue;
                }
                int searchFrom = 0;
                while (searchFrom <= normalized.Length - normalizedCue.Length)
                {
                    int index = SceneActionFrameworkV1.IndexOfCue(
                        normalized,
                        normalizedCue,
                        searchFrom);
                    if (index < 0)
                    {
                        break;
                    }
                    if (SceneActionFrameworkV1.ContainsPerformedCue(
                        normalized,
                        normalizedCue))
                    {
                        return true;
                    }
                    searchFrom = index + Math.Max(1, normalizedCue.Length);
                }
            }
            return false;
        }
        private static bool IsCoveredByPerformedGesture(
            HashSet<string> performed,
            string intentKey)
        {
            if (performed.Contains(Command) &&
                (string.Equals(intentKey, Point, StringComparison.Ordinal) ||
                 string.Equals(intentKey, Greet, StringComparison.Ordinal) ||
                 string.Equals(intentKey, Explain, StringComparison.Ordinal)))
            {
                return true;
            }
            if (performed.Contains(FollowMe) &&
                (string.Equals(intentKey, Greet, StringComparison.Ordinal) ||
                 string.Equals(intentKey, Challenge, StringComparison.Ordinal)))
            {
                return true;
            }
            if (performed.Contains(CutThroat) &&
                string.Equals(intentKey, Threat, StringComparison.Ordinal))
            {
                return true;
            }
            return false;
        }
        public static string BuildClassifierDefinitionBlock(IEnumerable<string> allowedIntentKeys)
            => BuildClassifierDefinitionBlock(allowedIntentKeys, true);

        public static string BuildClassifierDefinitionBlock(IEnumerable<string> allowedIntentKeys, bool useChinese)
        {
            HashSet<string> allowed = new HashSet<string>(
                allowedIntentKeys ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            return string.Join("\n", Entries
                .Where(entry => allowed.Contains(entry.IntentKey))
                .Select(entry =>
                    SceneActionCatalogText.FormatEntry(entry.IntentKey, useChinese, true)));
        }

        public static void ValidateCatalog(SceneActionCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }
            SceneActionFrameworkV1.ValidateCatalog(catalog);
            HashSet<string> expectedIntents = new HashSet<string>(
                Entries.Select(entry => entry.IntentKey),
                StringComparer.Ordinal);
            HashSet<string> expectedActions = new HashSet<string>(
                Entries.Where(entry => entry.Kind == IntentKind.PlayAction)
                    .Select(entry => entry.ActionKey),
                StringComparer.Ordinal);
            if (!expectedIntents.SetEquals(catalog.Intents.Keys) ||
                !expectedActions.SetEquals(catalog.Actions.Keys))
            {
                throw new InvalidOperationException(
                    "Catalog does not match the twenty-seven-action SceneActionFrameworkV4 contract.");
            }
            foreach (SceneActionContractEntryV4 entry in Entries)
            {
                if (!catalog.TryGetIntent(entry.IntentKey, out IntentDefinition intent) ||
                    intent.Kind != entry.Kind ||
                    !string.Equals(intent.ActionKey, entry.ActionKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Catalog intent drifted from SceneActionFrameworkV4: " + entry.IntentKey);
                }
                if (entry.Kind == IntentKind.PlayAction &&
                    (!catalog.Actions.TryGetValue(entry.ActionKey, out ActionDefinition action) ||
                     action.Mode != entry.PlaybackMode))
                {
                    throw new InvalidOperationException(
                        "Catalog action mode drifted from SceneActionFrameworkV4: " + entry.ActionKey);
                }
                if (string.IsNullOrWhiteSpace(entry.DisplayNameZhCn) ||
                    string.IsNullOrWhiteSpace(entry.SemanticDescriptionZhCn) ||
                    entry.ExactAliases.Count == 0 ||
                    entry.PerformedCues.Count == 0 ||
                    entry.PositiveExamples.Count < 2 ||
                    entry.NegativeExamples.Count < 2)
                {
                    throw new InvalidOperationException(
                        "V4 semantic definition is incomplete: " + entry.IntentKey);
                }
            }
        }

        private static SceneActionContractEntryV4 Legacy(SceneActionContractEntryV3 source)
        {
            ActionMode? mode = source.PlaybackMode;
            if (source.Kind == IntentKind.PlayAction &&
                (string.Equals(source.IntentKey, Threat, StringComparison.Ordinal) ||
                 string.Equals(source.IntentKey, Surrender, StringComparison.Ordinal) ||
                 string.Equals(source.IntentKey, Point, StringComparison.Ordinal) ||
                 string.Equals(source.IntentKey, Rage, StringComparison.Ordinal) ||
                 string.Equals(source.IntentKey, Cheer, StringComparison.Ordinal)))
            {
                mode = ActionMode.RandomGroup;
            }
            return new SceneActionContractEntryV4(
                source.IntentKey,
                source.Kind,
                source.ActionKey,
                mode,
                source.CanOverlayKneel,
                source.DisplayNameZhCn,
                source.SemanticDescriptionZhCn,
                source.ExactAliases,
                source.PerformedCues,
                source.ReferenceCues,
                source.PositiveExamples,
                source.NegativeExamples);
        }

        private static SceneActionContractEntryV4 NewAction(
            string key,
            string displayName,
            string description,
            IEnumerable<string> exactAliases,
            IEnumerable<string> performedCues,
            IEnumerable<string> referenceCues,
            IEnumerable<string> positiveExamples,
            IEnumerable<string> negativeExamples)
        {
            return new SceneActionContractEntryV4(
                key,
                IntentKind.PlayAction,
                key,
                ActionMode.OneShot,
                true,
                displayName,
                description,
                exactAliases,
                performedCues,
                referenceCues,
                positiveExamples,
                negativeExamples);
        }

        private static List<CueSpan> FindCueSpans(
            string normalized,
            Func<SceneActionContractEntryV4, IEnumerable<string>> selectCues)
        {
            List<CueSpan> matches = new List<CueSpan>();
            foreach (SceneActionContractEntryV4 entry in Entries)
            {
                foreach (string cue in selectCues(entry)
                             .Select(CommandParser.Normalize)
                             .Where(cue => !string.IsNullOrEmpty(cue))
                             .Distinct(StringComparer.Ordinal))
                {
                    int searchFrom = 0;
                    while (searchFrom <= normalized.Length - cue.Length)
                    {
                        int start = SceneActionFrameworkV1.IndexOfCue(
                            normalized,
                            cue,
                            searchFrom);
                        if (start < 0)
                        {
                            break;
                        }
                        if (string.Equals(entry.IntentKey, Xihai, StringComparison.Ordinal) &&
                            SceneActionFrameworkV1.IsXihaiEquipmentMention(
                                normalized,
                                start,
                                cue.Length))
                        {
                            searchFrom = start + Math.Max(1, cue.Length);
                            continue;
                        }
                        matches.Add(new CueSpan(entry.IntentKey, start, cue.Length));
                        searchFrom = start + Math.Max(1, cue.Length);
                    }
                }
            }
            return matches;
        }

        private static IEnumerable<CueSpan> SelectLongestCueSpans(
            IReadOnlyList<CueSpan> matches)
        {
            return matches.Where(match => !matches.Any(longer =>
                !string.Equals(longer.IntentKey, match.IntentKey, StringComparison.Ordinal) &&
                longer.Length > match.Length &&
                longer.Start <= match.Start &&
                longer.End >= match.End));
        }

        private sealed class CueSpan
        {
            public CueSpan(string intentKey, int start, int length)
            {
                IntentKey = intentKey;
                Start = start;
                Length = length;
            }

            public string IntentKey { get; }
            public int Start { get; }
            public int Length { get; }
            public int End => Start + Length;
        }
    }
}
