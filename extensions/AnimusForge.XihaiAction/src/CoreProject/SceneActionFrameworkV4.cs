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
                        "发号施令",
                        "实际挥臂、抬手或摆出手势向群体发号施令；口头命令和单纯指向不算该动作。",
                        new[]
                        {
                            "发号施令", "下令手势", "挥臂下令", "command",
                            "command gesture", "issue order gesture"
                        },
                        new[]
                        {
                            "挥臂向众人下令", "抬手向众人下令", "挥手向众人发号施令",
                            "挥臂发号施令", "挥臂向整支队伍发号施令", "抬手发号施令",
                            "作出下令手势",
                            "做出下令手势", "向众人作出命令手势", "朝队伍挥臂下令",
                            "抬手向众人作出明确的下令手势",
                            "挥臂向整队下令",
                            "raised his arm to command the group",
                            "swept her arm in a command gesture",
                            "swept his arm to command the entire group",
                            "swept her arm to command the entire group",
                            "gestured an order to the troops", "面向整队士兵大手一挥", "抬臂作出明确的下令手势", "大手一挥下令", "挥臂下令",
                            "面向士兵挥臂发令", "抬手示意众人听令", "issued a command with a sweeping arm"
                        },
                        new[]
                        {
                            "发号施令", "下令手势", "命令手势",
                            "command", "order", "ordered",
                            "command gesture", "issue order gesture"
                        },
                        new[]
                        {
                            "他挥臂向整支队伍发号施令。",
                            "她抬手向众人作出明确的下令手势。"
                        },
                        new[]
                        {
                            "他只是说：‘前进。’", "他伸手指向门口。",
                            "她命令别人服从，但没有做手势。"
                        }),
                    NewAction(
                        FollowMe,
                        "招手跟上",
                        "实际向同伴或队伍招手示意跟上、跟随或向前；友好问候和对敌勾手叫阵不算该动作。",
                        new[]
                        {
                            "招手示意跟上", "跟上手势", "follow_me",
                            "follow me gesture", "beckon to follow"
                        },
                        new[]
                        {
                            "招手示意同伴跟上", "朝同伴招手示意跟上",
                            "向队伍招手让他们跟上", "回头招手示意队伍跟随",
                            "挥手示意众人跟上", "作出跟上手势", "做出跟上手势",
                            "beckoned his companions to follow",
                            "waved the group forward to follow", "招手示意众人跟上", "向队伍招手示意跟上", "朝同伴连续招手", "示意整支队伍跟上", "回过身朝同伴连续招手", "招手示意队伍跟上",
                            "朝身后招手让大家跟来", "回头挥手示意队伍前进", "waved the group forward"
                        },
                        new[]
                        {
                            "跟上手势", "招手跟上", "跟我来", "跟上",
                            "follow me", "follow me gesture", "beckon to follow"
                        },
                        new[]
                        {
                            "他朝同伴招手示意跟上。",
                            "她挥手示意众人跟上。"
                        },
                        new[]
                        {
                            "他说：‘跟我来。’", "他向朋友挥手问好。",
                            "她朝对手勾手挑衅。"
                        }),
                    NewAction(
                        CutThroat,
                        "割喉手势",
                        "实际用手指横划喉前作出割喉威胁手势；口头威胁和真实持刀割喉或攻击都不算该手势。",
                        new[]
                        {
                            "割喉手势", "抹脖子手势", "cut_throat",
                            "cut throat gesture", "throat cutting gesture"
                        },
                        new[]
                        {
                            "用手指划过喉前", "手指从喉前划过", "抬手在脖子前横划",
                            "做出割喉手势", "作出割喉手势", "比出抹脖子动作",
                            "用拇指划过喉前", "made a throat-cutting gesture",
                            "drew a finger across his throat", "drew one finger across his throat",
                            "drew one finger across her throat", "ran a finger across his throat", "用食指在自己的喉前横划了一下",
                            "用食指在自己的喉前横划一下作割喉手势",
                            "手指在自己的喉前横划一下", "在自己的喉前横划一下",
                            "从喉前横划", "手指在喉前横划", "用食指在喉咙前做了个划过的动作",
                            "抬手横过脖子作割喉威胁"
                        },
                        new[]
                        {
                            "割喉手势", "抹脖子手势", "划喉手势", "割断你的喉咙",
                            "割喉", "抹脖子", "cut throat", "cut throat gesture", "slit your throat"
                        },
                        new[]
                        {
                            "他冷冷地用手指从喉前划过。",
                            "她做出割喉手势。"
                        },
                        new[]
                        {
                            "他说：‘我要割断你的喉咙。’", "他只是挥拳恐吓。",
                            "她拔刀真的砍向对方喉咙。"
                        })
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
            "拔刀割喉", "持刀割喉", "用刀割喉", "刀刃划过喉咙",
            "割开喉咙", "割开对方的喉咙", "割断喉咙", "划开对方的喉咙",
            "拔刀划开喉咙", "拔刀划开对方的喉咙", "刀刃划开喉咙",
            "用刀划过喉咙", "砍向喉咙", "刺向喉咙", "攻击他的喉咙",
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
        {
            HashSet<string> allowed = new HashSet<string>(
                allowedIntentKeys ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            return string.Join("\n", Entries
                .Where(entry => allowed.Contains(entry.IntentKey))
                .Select(entry =>
                    entry.IntentKey + "（" + entry.DisplayNameZhCn + "）：" +
                    entry.SemanticDescriptionZhCn +
                    " 正例：" + string.Join("；", entry.PositiveExamples) +
                    " 反例：" + string.Join("；", entry.NegativeExamples)));
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
