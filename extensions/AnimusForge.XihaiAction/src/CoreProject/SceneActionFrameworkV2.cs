using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.SceneActions.Core
{
    public sealed class SceneActionContractEntryV2
    {
        internal SceneActionContractEntryV2(
            string intentKey,
            IntentKind kind,
            string actionKey,
            ActionMode? playbackMode,
            bool canOverlayKneel,
            params string[] naturalLanguageAliases)
        {
            IntentKey = intentKey;
            Kind = kind;
            ActionKey = actionKey;
            PlaybackMode = playbackMode;
            CanOverlayKneel = canOverlayKneel;
            NaturalLanguageAliases =
                Array.AsReadOnly(naturalLanguageAliases ?? Array.Empty<string>());
        }

        public string IntentKey { get; }
        public IntentKind Kind { get; }
        public string ActionKey { get; }
        public ActionMode? PlaybackMode { get; }
        public bool CanOverlayKneel { get; }
        public IReadOnlyList<string> NaturalLanguageAliases { get; }
    }

    /// <summary>
    /// V2 extends the immutable V1 contract to sixteen closed-set logical intents.
    /// It never accepts raw engine action ids.
    /// </summary>
    public static class SceneActionFrameworkV2
    {
        public const int ContractVersion = 2;

        public const string Kneel = SceneActionFrameworkV1.Kneel;
        public const string StandUp = SceneActionFrameworkV1.StandUp;
        public const string Xihai = SceneActionFrameworkV1.Xihai;
        public const string Cheer = SceneActionFrameworkV1.Cheer;
        public const string Applaud = SceneActionFrameworkV1.Applaud;
        public const string Respect = SceneActionFrameworkV1.Respect;
        public const string Threat = SceneActionFrameworkV1.Threat;
        public const string Surrender = SceneActionFrameworkV1.Surrender;
        public const string Laugh = "laugh";
        public const string Point = "point";
        public const string Rage = "rage";
        public const string Fear = "fear";
        public const string Disappointed = "disappointed";
        public const string Challenge = "challenge";
        public const string Search = "search";
        public const string Dance = "dance";

        private static readonly ReadOnlyCollection<SceneActionContractEntryV2> Entries =
            Array.AsReadOnly(new[]
            {
                EntryFromV1(Kneel, ActionMode.Stateful),
                EntryFromV1(StandUp, null),
                EntryFromV1(Xihai, ActionMode.OneShot),
                EntryFromV1(Cheer, ActionMode.RandomGroup),
                EntryFromV1(Applaud, ActionMode.RandomGroup),
                EntryFromV1(Respect, ActionMode.OneShot),
                EntryFromV1(Threat, ActionMode.OneShot),
                EntryFromV1(Surrender, ActionMode.OneShot),
                Entry(
                    Laugh,
                    ActionMode.OneShot,
                    true,
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0001"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0002"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0003"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0004"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0005"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0006"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0007"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0008"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0009"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0010"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0011"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0012"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0013"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0014"), "laugh", "laughed",
                    "burst into laughter", "let out a burst of laughter",
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0015"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0016"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0017")),
                Entry(
                    Point,
                    ActionMode.OneShot,
                    true,
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0018"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0019"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0020"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0021"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0022"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0023"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0024"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0025"), "point", "pointed", SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0026"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0027"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0028"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0029")),
                Entry(
                    Rage,
                    ActionMode.OneShot,
                    true,
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0030"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0031"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0032"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0033"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0034"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0035"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0036"), "rage", "enraged", SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0037"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0038"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0039")),
                Entry(
                    Fear,
                    ActionMode.RandomGroup,
                    true,
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0040"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0041"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0042"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0043"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0044"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0045"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0046"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0047"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0048"), "fear", "afraid", SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0049"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0050"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0051"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0052"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0053")),
                Entry(
                    Disappointed,
                    ActionMode.RandomGroup,
                    true,
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0054"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0055"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0056"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0057"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0058"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0059"), "disappointed", "dejected", SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0060"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0061"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0062"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0063"),
                    "sighed and lowered his head in disappointment", "lowered his head in disappointment",
                    "sighed in disappointment", "sighed and lowered her head in disappointment", "lowered her head in disappointment"),
                Entry(
                    Challenge,
                    ActionMode.RandomGroup,
                    true,
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0064"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0065"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0066"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0067"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0068"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0069"), "challenge", "challenged", SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0070"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0071"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0072")),
                Entry(
                    Search,
                    ActionMode.RandomGroup,
                    true,
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0073"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0074"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0075"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0076"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0077"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0078"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0079"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0080"), "search", "looked around", SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0081"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0082"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0083"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0084")),
                Entry(
                    Dance,
                    ActionMode.Looping,
                    false,
                    SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0085"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0086"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0087"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0088"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0089"),
                    "dance", "danced", SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0090"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0091"))
            });

        private static readonly HashSet<string> KneelOverlayKeys =
            new HashSet<string>(Entries
                .Where(entry => entry.CanOverlayKneel)
                .Select(entry => entry.IntentKey), StringComparer.Ordinal);

        private static readonly string[] UnsupportedActionCues =
        {
            SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0092"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0093"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0094"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0095"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0096"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0097"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0098"),
            SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0099"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0100"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0101"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0102"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0103"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0104"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0105"),
            SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0106"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0107"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0108"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0109"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0110"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0111"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0112"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0113"),
            SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0114"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0115"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0116"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0117"), SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0118"), "walked over", "walked toward",
            "walked to", "moved toward", "approached", "sat down", "lay down",
            "hugged", "kissed", "drank", "ate", "kicked", "attacked"
        };

        public static IReadOnlyList<SceneActionContractEntryV2> LogicalActions => Entries;

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
                SceneActionFrameworkV1.ResolveNaturalActionDescription(normalized));
            foreach (SceneActionContractEntryV2 entry in Entries.Skip(8))
            {
                bool matched = entry.NaturalLanguageAliases.Any(alias =>
                    SceneActionFrameworkV1.ContainsPerformedCue(
                        normalized,
                        CommandParser.Normalize(alias)));
                if (matched &&
                    RequiresPhysicalActionEvidence(entry.IntentKey) &&
                    !SceneActionFrameworkV1.ContainsPhysicalActionEvidence(
                        entry.IntentKey,
                        normalized))
                {
                    matched = false;
                }
                if (matched)
                {
                    resolved.Add(entry.IntentKey);
                }
            }
            return SelectLongestIntentKeys(
                    normalized,
                    resolved.Distinct(StringComparer.Ordinal).ToArray())
                .ToArray();
        }

        public static IReadOnlyList<string> ResolveNaturalActionReferences(string text)
        {
            string normalized = CommandParser.Normalize(text);
            if (string.IsNullOrEmpty(normalized))
            {
                return Array.Empty<string>();
            }

            List<string> references = new List<string>();
            foreach (SceneActionContractEntryV2 entry in Entries)
            {
                bool matched = false;
                foreach (string alias in entry.NaturalLanguageAliases)
                {
                    string normalizedAlias = CommandParser.Normalize(alias);
                    int searchFrom = 0;
                    while (searchFrom <= normalized.Length - normalizedAlias.Length)
                    {
                        int index = SceneActionFrameworkV1.IndexOfCue(
                            normalized,
                            normalizedAlias,
                            searchFrom);
                        if (index < 0)
                        {
                            break;
                        }
                        if (!string.Equals(entry.IntentKey, Xihai, StringComparison.Ordinal) ||
                            !SceneActionFrameworkV1.IsXihaiEquipmentMention(
                                normalized,
                                index,
                                normalizedAlias.Length))
                        {
                            matched = true;
                            break;
                        }
                        searchFrom = index + Math.Max(1, normalizedAlias.Length);
                    }
                    if (matched)
                    {
                        break;
                    }
                }
                if (matched)
                {
                    references.Add(entry.IntentKey);
                }
            }
            return SelectLongestIntentKeys(
                    normalized,
                    references.Distinct(StringComparer.Ordinal).ToArray())
                .ToArray();
        }

        public static bool ContainsNaturalActionReference(string text)
        {
            return ResolveNaturalActionReferences(text).Count > 0 ||
                   SceneActionFrameworkV1.ContainsNaturalActionReference(text);
        }

        public static bool ContainsUnsupportedActionReference(string text)
        {
            string normalized = CommandParser.Normalize(text);
            // ASCII cues must be matched as whole words. A raw substring check
            // would classify the "ate" part of "threateningly" as the
            // unsupported action "ate", incorrectly closing valid threat
            // gestures. IndexOfCue keeps Chinese phrase matching exact while
            // applying word boundaries to English cues.
            return UnsupportedActionCues.Any(cue => SceneActionFrameworkV1.IndexOfCue(
                normalized,
                CommandParser.Normalize(cue)) >= 0);
        }

        public static bool HasSuppressedKnownActionReference(
            string text,
            IEnumerable<string> performedIntents)
        {
            HashSet<string> performed = new HashSet<string>(
                performedIntents ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            string normalized = CommandParser.Normalize(text);
            List<string> unmatched = ResolveNaturalActionReferences(text)
                .Where(key => !performed.Contains(key))
                .Where(key => !RequiresPhysicalActionEvidence(key) ||
                              SceneActionFrameworkV1.ContainsPhysicalActionEvidence(
                                  key,
                                  normalized) ||
                              performed.Count == 0)
                .Where(key => HasUnsuppressedReference(normalized, key))
                .ToList();
            // A geometrically specific Xihai gesture necessarily contains generic
            // respect words such as “行礼”; V1 intentionally gives the specific cue
            // precedence, so that overlap is not an unperformed second action.
            if (performed.Contains(Xihai))
            {
                unmatched.RemoveAll(key => string.Equals(
                    key,
                    Respect,
                    StringComparison.Ordinal));
            }
            // This explicit alternative is a long-standing V1 safety case: the
            // refused Xihai gesture is not part of the performed respect action.
            if (performed.Contains(Respect) &&
                (normalized.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0119"), StringComparison.Ordinal) >= 0 ||
                 normalized.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV2.Raw0120"), StringComparison.Ordinal) >= 0))
            {
                unmatched.RemoveAll(key => string.Equals(
                    key,
                    Xihai,
                    StringComparison.Ordinal));
            }
            return unmatched.Count > 0;
        }

        private static bool HasUnsuppressedReference(
            string normalized,
            string intentKey)
        {
            SceneActionContractEntryV2 entry = Entries.Single(value =>
                string.Equals(value.IntentKey, intentKey, StringComparison.Ordinal));
            foreach (string alias in entry.NaturalLanguageAliases)
            {
                string cue = CommandParser.Normalize(alias);
                if (string.IsNullOrEmpty(cue))
                {
                    continue;
                }
                int searchFrom = 0;
                while (searchFrom <= normalized.Length - cue.Length)
                {
                    int index = SceneActionFrameworkV1.IndexOfCue(
                        normalized,
                        cue,
                        searchFrom);
                    if (index < 0)
                    {
                        break;
                    }
                    if (SceneActionFrameworkV1.ContainsPerformedCue(normalized, cue))
                    {
                        return true;
                    }
                    searchFrom = index + Math.Max(1, cue.Length);
                }
            }
            return false;
        }

        private static bool RequiresPhysicalActionEvidence(string intentKey)
        {
            return string.Equals(intentKey, Rage, StringComparison.Ordinal) ||
                   string.Equals(intentKey, Fear, StringComparison.Ordinal) ||
                   string.Equals(intentKey, Disappointed, StringComparison.Ordinal);
        }

        private static IReadOnlyList<string> SelectLongestIntentKeys(
            string normalized,
            IEnumerable<string> candidateKeys)
        {
            HashSet<string> candidates = new HashSet<string>(
                candidateKeys ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            if (candidates.Count <= 1)
            {
                return candidates.ToArray();
            }

            List<CueSpan> matches = new List<CueSpan>();
            foreach (SceneActionContractEntryV1 entry in
                     SceneActionFrameworkV1.LogicalActions)
            {
                AddCueSpans(matches, normalized, entry.IntentKey, entry.NpcReplyAliases);
            }
            foreach (SceneActionContractEntryV2 entry in Entries)
            {
                AddCueSpans(matches, normalized, entry.IntentKey, entry.NaturalLanguageAliases);
            }

            HashSet<string> preferred = new HashSet<string>(
                matches.Where(match => candidates.Contains(match.IntentKey))
                    .Where(match => !matches.Any(longer =>
                        candidates.Contains(longer.IntentKey) &&
                        !string.Equals(
                            longer.IntentKey,
                            match.IntentKey,
                            StringComparison.Ordinal) &&
                        longer.Length > match.Length &&
                        longer.Start <= match.Start &&
                        longer.End >= match.End))
                    .Select(match => match.IntentKey),
                StringComparer.Ordinal);
            return candidates.Where(key =>
                    preferred.Contains(key) ||
                    !matches.Any(match => string.Equals(
                        match.IntentKey,
                        key,
                        StringComparison.Ordinal)))
                .ToArray();
        }

        private static void AddCueSpans(
            ICollection<CueSpan> matches,
            string normalized,
            string intentKey,
            IEnumerable<string> cues)
        {
            foreach (string cue in (cues ?? Enumerable.Empty<string>())
                         .Select(CommandParser.Normalize)
                         .Where(value => !string.IsNullOrEmpty(value))
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
                    matches.Add(new CueSpan(intentKey, start, cue.Length));
                    searchFrom = start + Math.Max(1, cue.Length);
                }
            }
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
                    "Catalog does not match the sixteen-action SceneActionFrameworkV2 contract.");
            }

            foreach (SceneActionContractEntryV2 entry in Entries)
            {
                if (!catalog.TryGetIntent(entry.IntentKey, out IntentDefinition intent) ||
                    intent.Kind != entry.Kind ||
                    !string.Equals(intent.ActionKey, entry.ActionKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Catalog intent drifted from SceneActionFrameworkV2: " + entry.IntentKey);
                }
                if (entry.Kind == IntentKind.PlayAction &&
                    (!catalog.Actions.TryGetValue(entry.ActionKey, out ActionDefinition action) ||
                     action.Mode != entry.PlaybackMode))
                {
                    throw new InvalidOperationException(
                        "Catalog action mode drifted from SceneActionFrameworkV2: " + entry.ActionKey);
                }
            }
        }

        private static SceneActionContractEntryV2 EntryFromV1(
            string intentKey,
            ActionMode? mode)
        {
            SceneActionContractEntryV1 source = SceneActionFrameworkV1.LogicalActions
                .Single(entry => string.Equals(
                    entry.IntentKey,
                    intentKey,
                    StringComparison.Ordinal));
            return new SceneActionContractEntryV2(
                source.IntentKey,
                source.Kind,
                source.ActionKey,
                mode,
                false,
                source.NpcReplyAliases.ToArray());
        }

        private static SceneActionContractEntryV2 Entry(
            string key,
            ActionMode mode,
            bool canOverlayKneel,
            params string[] aliases)
        {
            return new SceneActionContractEntryV2(
                key,
                IntentKind.PlayAction,
                key,
                mode,
                canOverlayKneel,
                aliases);
        }
    }
}
