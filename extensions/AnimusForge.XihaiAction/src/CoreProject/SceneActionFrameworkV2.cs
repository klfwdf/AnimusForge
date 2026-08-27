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
                    "大笑", "哈哈大笑", "放声大笑", "开怀大笑", "仰头大笑",
                    "笑出声", "笑了起来", "咧嘴大笑", "发出一阵笑声",
                    "爆发出一阵笑声", "爆发出笑声", "骤然笑了出来",
                    "朗声笑了起来", "放声笑了起来", "laugh", "laughed",
                    "burst into laughter", "let out a burst of laughter",
                    "笑得前仰后合", "前仰后合", "捧腹大笑起来"),
                Entry(
                    Point,
                    ActionMode.OneShot,
                    true,
                    "指向", "指了指", "伸手指向", "抬手指向", "用手指向",
                    "朝旁边一指", "指向旁边", "指给", "point", "pointed", "抬起食指越过众人肩头", "明确朝门外点去", "食指点去", "朝门外点去"),
                Entry(
                    Rage,
                    ActionMode.OneShot,
                    true,
                    "愤怒", "发怒", "暴怒", "勃然大怒", "怒吼", "怒气冲冲",
                    "愤怒挥手", "rage", "enraged", "挥舞双臂", "跺脚怒吼", "猛然挥舞双臂并跺脚怒吼"),
                Entry(
                    Fear,
                    ActionMode.RandomGroup,
                    true,
                    "害怕", "恐惧", "惊恐", "惊慌", "慌里慌张", "畏惧",
                    "吓得摆手", "惊慌摆手", "瑟瑟发抖", "fear", "afraid", "吓得连退", "连连摆手", "连退两步", "双手护住脑袋", "不停发抖"),
                Entry(
                    Disappointed,
                    ActionMode.RandomGroup,
                    true,
                    "失望", "沮丧", "失落", "垂头丧气", "无奈叹气",
                    "摇头叹息", "disappointed", "dejected", "长长叹了一口气", "垂下头", "无奈地摇了摇", "长叹一声",
                    "sighed and lowered his head in disappointment", "lowered his head in disappointment",
                    "sighed in disappointment", "sighed and lowered her head in disappointment", "lowered her head in disappointment"),
                Entry(
                    Challenge,
                    ActionMode.RandomGroup,
                    true,
                    "挑衅", "挑战", "叫阵", "勾手挑衅", "招手挑衅",
                    "挑衅地招手", "challenge", "challenged", "朝自己勾了勾手指", "拍胸叫阵", "勾了勾手指叫阵"),
                Entry(
                    Search,
                    ActionMode.RandomGroup,
                    true,
                    "环顾", "环顾四周", "四下张望", "左右张望", "扫视四周",
                    "警惕观察", "搜寻", "寻找", "search", "looked around", "伸长脖子", "缓慢环视大厅每个角落", "环视大厅每个角落", "四处看"),
                Entry(
                    Dance,
                    ActionMode.Looping,
                    false,
                    "跳舞", "舞蹈", "舞动", "翩翩起舞", "随着节奏起舞",
                    "dance", "danced", "扭动身体跳起舞来", "跳起舞来")
            });

        private static readonly HashSet<string> KneelOverlayKeys =
            new HashSet<string>(Entries
                .Where(entry => entry.CanOverlayKneel)
                .Select(entry => entry.IntentKey), StringComparer.Ordinal);

        private static readonly string[] UnsupportedActionCues =
        {
            "挥手", "摆手", "招手", "哭泣", "大哭", "哭了起来", "坐下",
            "躺下", "趴下", "转身", "走路", "走了两步", "走了过去", "走过去",
            "走向", "走到", "走去", "迈步走向", "跑步", "奔跑", "拥抱", "亲吻",
            "喝酒", "吃东西", "踢腿", "踢人", "攻击", "walked over", "walked toward",
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
                (normalized.IndexOf("而是", StringComparison.Ordinal) >= 0 ||
                 normalized.IndexOf("转而", StringComparison.Ordinal) >= 0))
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
