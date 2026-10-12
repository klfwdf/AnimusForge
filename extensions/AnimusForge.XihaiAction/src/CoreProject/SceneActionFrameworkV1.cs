using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnimusForge.SceneActions.Core
{
    public sealed class SceneActionContractEntryV1
    {
        internal SceneActionContractEntryV1(
            string intentKey,
            IntentKind kind,
            string actionKey,
            params string[] npcReplyAliases)
        {
            IntentKey = intentKey;
            Kind = kind;
            ActionKey = actionKey;
            NpcReplyAliases = Array.AsReadOnly(npcReplyAliases ?? Array.Empty<string>());
        }

        public string IntentKey { get; }
        public IntentKind Kind { get; }
        public string ActionKey { get; }
        public IReadOnlyList<string> NpcReplyAliases { get; }
    }

    /// <summary>
    /// The immutable production contract for the first eight logical SceneActions.
    /// Adding a ninth production intent requires a new framework contract version.
    /// </summary>
    public static class SceneActionFrameworkV1
    {
        private static readonly Lazy<char[]> LiExcludedNounsChars = new Lazy<char[]>(() =>
            Array.ConvertAll(SceneActionVocabulary.List("SceneActionFrameworkV1.LiExcludedNouns"), s => s[0]));
        public const int ContractVersion = 1;

        public const string Kneel = "kneel";
        public const string StandUp = "stand_up";
        public const string Xihai = "xihai";
        public const string Cheer = "cheer";
        public const string Applaud = "applaud";
        public const string Respect = "respect";
        public const string Threat = "threat";
        public const string Surrender = "surrender";

        private static readonly ReadOnlyCollection<SceneActionContractEntryV1> Entries =
            Array.AsReadOnly(new[]
            {
                Entry(
                    Kneel,
                    IntentKind.PlayAction,
                    Kneel,
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0001"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0002"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0003"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0004"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0005"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0006"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0007"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0008"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0009"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0010"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0011"),
                    "kneel",
                    "knelt",
                    "knelt down",
                    "fell to his knees",
                    "fell to her knees",
                    "went down on one knee",
                    "dropped to his knees",
                    "dropped to her knees"),
                Entry(
                    StandUp,
                    IntentKind.ExitOwnedState,
                    null,
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0012"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0013"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0014"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0015"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0016"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0017"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0018"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0019"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0020"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0021"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0022"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0023"),
                    "stood up",
                    "stood from his knees",
                    "stood from her knees",
                    "rose from kneeling",
                    "got back on his feet",
                    "got back on her feet"),
                Entry(
                    Xihai,
                    IntentKind.PlayAction,
                    Xihai,
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0024"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0025"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0026"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0027"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0028"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0029"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0030"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0031"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0032"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0033"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0034"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0035"),
                    "nazi salute",
                    "hitler salute",
                    "sieg heil salute",
                    "heil hitler salute",
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0036"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0037"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0038"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0039"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0040"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0041"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0042"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0043"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0044"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0045"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0046"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0047"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0048"),
                    "raised his right arm at forty-five degrees in salute",
                    "raised her right arm at forty-five degrees in salute",
                    "raised his right arm in a salute",
                    "raised her right arm in a salute"),
                Entry(
                    Cheer,
                    IntentKind.PlayAction,
                    Cheer,
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0049"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0050"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0051"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0052"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0053"),
                                         SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0054"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0055"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0056"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0057"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0058"),
                     "cheer",
                     "cheered",
                     "cheered loudly",
                     "raised both arms and cheered loudly",
                     "raised his arms and cheered",
                     "raised her arms and cheered"),
                Entry(
                    Applaud,
                    IntentKind.PlayAction,
                    Applaud,
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0059"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0060"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0061"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0062"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0063"),
                                         SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0064"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0065"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0066"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0067"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0068"),
                     "clapped",
                     "clapped his hands",
                     "clapped her hands",
                     "applauded",
                     "applauded loudly"),
                Entry(
                    Respect,
                    IntentKind.PlayAction,
                    Respect,
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0069"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0070"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0071"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0072"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0073"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0074"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0075"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0076"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0077"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0078"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0079"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0080"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0081"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0082"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0083"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0084"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0085"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0086"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0087"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0088"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0089"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0090"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0091"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0092"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0093"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0094"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0095"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0096"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0097"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0098"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0099"),
                     "bowed politely",
                     "bowed in respect",
                     "made a respectful bow",
                     "gave a respectful bow"),
                Entry(
                    Threat,
                    IntentKind.PlayAction,
                    Threat,
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0100"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0101"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0102"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0103"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0104"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0105"),
                                         SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0106"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0107"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0108"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0109"),
                     "clenched his fist",
                     "clenched her fist",
                     "clenched his fist and shook it threateningly",
                     "clenched her fist and shook it threateningly",
                     "shook his fist threateningly",
                     "shook her fist threateningly",
                     "threateningly clenched his fist"),
                Entry(
                    Surrender,
                    IntentKind.PlayAction,
                    Surrender,
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0110"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0111"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0112"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0113"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0114"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0115"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0116"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0117"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0118"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0119"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0120"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0121"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0122"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0123"),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0124"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0125"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0126"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0127"),
                     SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0128"),
                     "surrendered",
                     "raised both hands in surrender",
                     "dropped his weapon and surrendered",
                     "dropped her weapon and surrendered",
                     "gave up resistance")
            });

        private static readonly string[] StrongCueSuppressors = SceneActionVocabulary.List("SceneActionFrameworkV1.StrongCueSuppressors");

        private static readonly string[] DirectCueSuppressors = SceneActionVocabulary.List("SceneActionFrameworkV1.DirectCueSuppressors");

        private static readonly string[] NonPerformativeContextSuppressors = SceneActionVocabulary.List("SceneActionFrameworkV1.NonPerformativeContextSuppressors");

        private static readonly string[] PerformedDespiteNegationPrefixes = SceneActionVocabulary.List("SceneActionFrameworkV1.PerformedDespiteNegationPrefixes");

        private static readonly string[] PerformanceRecoveryMarkers = SceneActionVocabulary.List("SceneActionFrameworkV1.PerformanceRecoveryMarkers");

        private static readonly string[] HypotheticalCueSuppressors = SceneActionVocabulary.List("SceneActionFrameworkV1.HypotheticalCueSuppressors");

        private static readonly string[] HypotheticalRecoveryMarkers = SceneActionVocabulary.List("SceneActionFrameworkV1.HypotheticalRecoveryMarkers");

        private static readonly string[] CompactEnglishSuppressors =
        {
            "not",
            "never",
            "refuseto",
            "refusedto",
            "didnot",
            "didn't",
            "doesnot",
            "doesn't",
            "donot",
            "don't",
            "willnot",
            "won't",
            "without",
            "plannedto",
            "intendedto",
            "wasgoingto",
            "said",
            "saidthat",
            "onlysaid",
            "merelysaid",
            "justsaid",
            "almost",
            "nearly",
            "tryto",
            "triedto",
            "planto",
            "plansto",
            "wantto",
            "wantsto"
        };

        private static readonly string[] AfterCueSuppressors = SceneActionVocabulary.List("SceneActionFrameworkV1.AfterCueSuppressors");

        private static readonly string[] RagePhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.RagePhysicalCues");

        private static readonly string[] FearPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.FearPhysicalCues");

        private static readonly string[] DisappointedPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.DisappointedPhysicalCues");

        private static readonly string[] CheerPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.CheerPhysicalCues");

        private static readonly string[] ApplaudPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.ApplaudPhysicalCues");

        private static readonly string[] ThreatPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.ThreatPhysicalCues");

        private static readonly string[] SurrenderPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.SurrenderPhysicalCues");

        private static readonly string[] LaughPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.LaughPhysicalCues");

        private static readonly string[] PointPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.PointPhysicalCues");

        private static readonly string[] ChallengePhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.ChallengePhysicalCues");

        private static readonly string[] SearchPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.SearchPhysicalCues");

        private static readonly string[] DancePhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.DancePhysicalCues");

        private static readonly string[] AgreePhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.AgreePhysicalCues");

        private static readonly string[] DisagreePhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.DisagreePhysicalCues");

        private static readonly string[] UnsurePhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.UnsurePhysicalCues");

        private static readonly string[] ExplainPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.ExplainPhysicalCues");

        private static readonly string[] PromisePhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.PromisePhysicalCues");

        private static readonly string[] CrossArmsPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.CrossArmsPhysicalCues");

        private static readonly string[] DeepBowPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.DeepBowPhysicalCues");

        private static readonly string[] GreetPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.GreetPhysicalCues");

        private static readonly string[] CommandPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.CommandPhysicalCues");

        private static readonly string[] FollowMePhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.FollowMePhysicalCues");

        private static readonly string[] CutThroatPhysicalCues = SceneActionVocabulary.List("SceneActionFrameworkV1.CutThroatPhysicalCues");
        private static readonly string[] GenericRespectCues = SceneActionVocabulary.List("SceneActionFrameworkV1.GenericRespectCues");

        private static readonly string RespectMotionAdverbPatternText =
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0129");

        private static readonly Regex RespectLinkedMotionPattern = new Regex(
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0130") + RespectMotionAdverbPatternText +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0131") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0132") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0133") +
            RespectMotionAdverbPatternText +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0134") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0135") + RespectMotionAdverbPatternText +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0136"),
            RegexOptions.CultureInvariant);

        private static readonly string[] RespectRecoveryMarkers = SceneActionVocabulary.List("SceneActionFrameworkV1.RespectRecoveryMarkers");

        private static readonly string RespectCeremonyPatternText =
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0137") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0138") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0139") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0140") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0141");

        private static readonly Regex RespectCeremonyPattern = new Regex(
            RespectCeremonyPatternText,
            RegexOptions.CultureInvariant);

        private static readonly string[] XihaiSpecificLimbs = SceneActionVocabulary.List("SceneActionFrameworkV1.XihaiSpecificLimbs");

        private static readonly string[] XihaiMotions = SceneActionVocabulary.List("SceneActionFrameworkV1.XihaiMotions");

        private static readonly string[] XihaiUpwardDirections = SceneActionVocabulary.List("SceneActionFrameworkV1.XihaiUpwardDirections");

        private static readonly string[] XihaiFingerDetails = SceneActionVocabulary.List("SceneActionFrameworkV1.XihaiFingerDetails");

        private static readonly string[] XihaiPalmDetails = SceneActionVocabulary.List("SceneActionFrameworkV1.XihaiPalmDetails");

        private static readonly string[] XihaiGeometryBlockers = SceneActionVocabulary.List("SceneActionFrameworkV1.XihaiGeometryBlockers");

        // “西海” is also used as the name of the legacy uniform set.  Keep the
        // short action alias for explicit stage descriptions, but do not treat
        // an immediately following clothing/equipment noun as a performed
        // salute (for example, “他穿着西海军装”).
        private static readonly string[] XihaiEquipmentContinuations = SceneActionVocabulary.List("SceneActionFrameworkV1.XihaiEquipmentContinuations");

        private static readonly Regex XihaiDegreeGesturePattern = new Regex(
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0142") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0143") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0144") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0129") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0145") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0146") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0147") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0148") +
            SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0149") +
            RespectCeremonyPatternText + ")",
            RegexOptions.CultureInvariant);

        private static readonly IReadOnlyDictionary<string, string> NpcReplyAliasMap =
            BuildNpcReplyAliasMap();

        public static IReadOnlyList<SceneActionContractEntryV1> LogicalActions => Entries;

        public static bool TryResolveNpcReplyAlias(string text, out string intentKey)
        {
            return NpcReplyAliasMap.TryGetValue(
                CommandParser.Normalize(text),
                out intentKey);
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

            List<string> resolved = new List<string>();
            foreach (SceneActionContractEntryV1 entry in Entries)
            {
                bool matched;
                if (string.Equals(entry.IntentKey, Xihai, StringComparison.Ordinal))
                {
                    matched = entry.NpcReplyAliases.Any(cue =>
                                  ContainsPerformedXihaiCue(
                                      normalized,
                                      CommandParser.Normalize(cue))) ||
                              ContainsPerformedStructuralXihaiCue(normalized);
                }
                else if (string.Equals(entry.IntentKey, Respect, StringComparison.Ordinal))
                {
                    matched = entry.NpcReplyAliases.Any(cue =>
                        ContainsPerformedRespectCue(
                            normalized,
                            CommandParser.Normalize(cue))) ||
                              ContainsPerformedRespectCeremonyCue(normalized);
                }
                else
                {
                    matched = entry.NpcReplyAliases.Any(cue =>
                        ContainsPerformedCue(
                            normalized,
                            CommandParser.Normalize(cue)));
                }

                if (matched)
                {
                    resolved.Add(entry.IntentKey);
                }
            }
            return resolved.ToArray();
        }

        public static bool ContainsNaturalActionReference(string text)
        {
            string normalized = CommandParser.Normalize(text);
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            foreach (SceneActionContractEntryV1 entry in Entries)
            {
                foreach (string rawCue in entry.NpcReplyAliases)
                {
                    string cue = CommandParser.Normalize(rawCue);
                    int searchFrom = 0;
                    while (searchFrom <= normalized.Length - cue.Length)
                    {
                        int index = IndexOfCue(normalized, cue, searchFrom);
                        if (index < 0)
                        {
                            break;
                        }
                        if (string.Equals(entry.IntentKey, Xihai, StringComparison.Ordinal) &&
                            IsXihaiEquipmentMention(normalized, index, cue.Length))
                        {
                            searchFrom = index + Math.Max(1, cue.Length);
                            continue;
                        }
                        if (!string.Equals(
                                entry.IntentKey,
                                Respect,
                                StringComparison.Ordinal) ||
                            !IsRespectNounContinuation(normalized, index, cue.Length))
                        {
                            return true;
                        }
                        searchFrom = index + Math.Max(1, cue.Length);
                    }
                }
            }

            if (RespectCeremonyPattern.IsMatch(normalized) ||
                XihaiDegreeGesturePattern.IsMatch(normalized))
            {
                return true;
            }

            int segmentStart = 0;
            for (int index = 0; index <= normalized.Length; index++)
            {
                if (index < normalized.Length &&
                    !IsSentenceSeparator(normalized[index]))
                {
                    continue;
                }
                if (index > segmentStart && IsStructuralXihaiSegment(
                        normalized.Substring(segmentStart, index - segmentStart)))
                {
                    return true;
                }
                segmentStart = index + 1;
            }
            return false;
        }

        private static bool ContainsPerformedXihaiCue(string text, string cue)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(cue))
            {
                return false;
            }

            int searchFrom = 0;
            while (searchFrom <= text.Length - cue.Length)
            {
                int index = IndexOfCue(text, cue, searchFrom);
                if (index < 0)
                {
                    return false;
                }
                if (!IsXihaiEquipmentMention(text, index, cue.Length) &&
                    !IsCueSuppressed(text, index, cue.Length))
                {
                    return true;
                }
                searchFrom = index + Math.Max(1, cue.Length);
            }
            return false;
        }

        internal static bool IsXihaiEquipmentMention(
            string normalizedText,
            int cueIndex,
            int cueLength)
        {
            if (string.IsNullOrEmpty(normalizedText) ||
                cueIndex < 0 ||
                cueLength != 2 ||
                cueIndex + cueLength > normalizedText.Length ||
                !string.Equals(
                    normalizedText.Substring(cueIndex, cueLength),
                    SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0024"),
                    StringComparison.Ordinal))
            {
                return false;
            }

            int continuationStart = cueIndex + cueLength;
            if (continuationStart < normalizedText.Length &&
                normalizedText[continuationStart] == SceneActionVocabulary.Text("SceneActionFrameworkV1.ContinuationParticle")[0])
            {
                continuationStart++;
            }

            return XihaiEquipmentContinuations.Any(noun =>
                continuationStart + noun.Length <= normalizedText.Length &&
                string.Equals(
                    normalizedText.Substring(continuationStart, noun.Length),
                    noun,
                    StringComparison.Ordinal));
        }

        public static bool IsLogicalIntent(string intentKey)
        {
            return Entries.Any(entry => string.Equals(
                entry.IntentKey,
                intentKey,
                StringComparison.Ordinal));
        }

        public static void ValidateCatalog(SceneActionCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            HashSet<string> expectedIntents = new HashSet<string>(
                Entries.Select(entry => entry.IntentKey),
                StringComparer.Ordinal);
            HashSet<string> expectedActions = new HashSet<string>(
                Entries.Where(entry => entry.Kind == IntentKind.PlayAction)
                    .Select(entry => entry.ActionKey),
                StringComparer.Ordinal);

            if (!expectedIntents.IsSubsetOf(catalog.Intents.Keys) ||
                !expectedActions.IsSubsetOf(catalog.Actions.Keys))
            {
                throw new InvalidOperationException(
                    "Catalog does not contain the complete eight-action SceneActionFrameworkV1 contract.");
            }

            foreach (SceneActionContractEntryV1 entry in Entries)
            {
                if (!catalog.TryGetIntent(entry.IntentKey, out IntentDefinition intent) ||
                    intent.Kind != entry.Kind ||
                    !string.Equals(intent.ActionKey, entry.ActionKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Catalog intent drifted from SceneActionFrameworkV1: " +
                        entry.IntentKey);
                }
            }
        }

        private static SceneActionContractEntryV1 Entry(
            string intentKey,
            IntentKind kind,
            string actionKey,
            params string[] npcReplyAliases)
        {
            return new SceneActionContractEntryV1(
                intentKey,
                kind,
                actionKey,
                npcReplyAliases);
        }

        private static IReadOnlyDictionary<string, string> BuildNpcReplyAliasMap()
        {
            Dictionary<string, string> aliases =
                new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (SceneActionContractEntryV1 entry in Entries)
            {
                foreach (string alias in entry.NpcReplyAliases)
                {
                    string normalized = CommandParser.Normalize(alias);
                    if (string.IsNullOrEmpty(normalized) ||
                        aliases.ContainsKey(normalized))
                    {
                        throw new InvalidOperationException(
                            "Invalid or duplicate NPC reply alias: " + alias);
                    }
                    aliases.Add(normalized, entry.IntentKey);
                }
            }
            return new ReadOnlyDictionary<string, string>(aliases);
        }

        internal static int IndexOfCue(string text, string cue, int searchFrom = 0)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(cue))
            {
                return -1;
            }

            int cursor = Math.Max(0, searchFrom);
            while (cursor <= text.Length - cue.Length)
            {
                int index = text.IndexOf(cue, cursor, StringComparison.Ordinal);
                if (index < 0)
                {
                    return -1;
                }
                if (IsCueBoundaryMatch(text, cue, index))
                {
                    return index;
                }
                cursor = index + Math.Max(1, cue.Length);
            }
            return -1;
        }

        private static bool IsCueBoundaryMatch(string text, string cue, int index)
        {
            if (!IsAsciiWordCue(cue))
            {
                return true;
            }

            int before = index - 1;
            int after = index + cue.Length;
            return (before < 0 || !IsAsciiWordChar(text[before])) &&
                   (after >= text.Length || !IsAsciiWordChar(text[after]));
        }

        private static bool IsAsciiWordCue(string cue)
        {
            bool hasAsciiWordCharacter = false;
            foreach (char character in cue)
            {
                if (IsAsciiWordChar(character))
                {
                    hasAsciiWordCharacter = true;
                    continue;
                }
                if (char.IsWhiteSpace(character) || character == '-' || character == '\'')
                {
                    continue;
                }
                return false;
            }
            return hasAsciiWordCharacter;
        }

        private static bool IsAsciiWordChar(char character)
        {
            return (character >= 'a' && character <= 'z') ||
                   (character >= 'A' && character <= 'Z') ||
                   (character >= '0' && character <= '9') ||
                   character == '_';
        }

        internal static bool ContainsPerformedCue(string text, string cue)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(cue))
            {
                return false;
            }

            int searchFrom = 0;
            while (searchFrom <= text.Length - cue.Length)
            {
                int index = IndexOfCue(text, cue, searchFrom);
                if (index < 0)
                {
                    return false;
                }
                if (!IsCueSuppressed(text, index, cue.Length))
                {
                    return true;
                }
                searchFrom = index + Math.Max(1, cue.Length);
            }
            return false;
        }

        internal static bool ContainsPhysicalActionEvidence(string intentKey, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            string[] cues;
            switch (intentKey)
            {
                case "rage":
                    cues = RagePhysicalCues;
                    break;
                case "fear":
                    cues = FearPhysicalCues;
                    break;
                case "disappointed":
                    cues = DisappointedPhysicalCues;
                    break;
                case Cheer:
                    cues = CheerPhysicalCues;
                    break;
                case Applaud:
                    cues = ApplaudPhysicalCues;
                    break;
                case Threat:
                    cues = ThreatPhysicalCues;
                    break;
                case Surrender:
                    cues = SurrenderPhysicalCues;
                    break;
                case "laugh":
                    cues = LaughPhysicalCues;
                    break;
                case "point":
                    cues = PointPhysicalCues;
                    break;
                case "challenge":
                    cues = ChallengePhysicalCues;
                    break;
                case "search":
                    cues = SearchPhysicalCues;
                    break;
                case "dance":
                    cues = DancePhysicalCues;
                    break;
                case "agree":
                    cues = AgreePhysicalCues;
                    break;
                case "disagree":
                    cues = DisagreePhysicalCues;
                    break;
                case "unsure":
                    cues = UnsurePhysicalCues;
                    break;
                case "explain":
                    cues = ExplainPhysicalCues;
                    break;
                case "promise":
                    cues = PromisePhysicalCues;
                    break;
                case "cross_arms":
                    cues = CrossArmsPhysicalCues;
                    break;
                case "deep_bow":
                    cues = DeepBowPhysicalCues;
                    break;
                case "greet":
                    cues = GreetPhysicalCues;
                    break;
                case "command":
                    cues = CommandPhysicalCues;
                    break;
                case "follow_me":
                    cues = FollowMePhysicalCues;
                    break;
                case "cut_throat":
                    cues = CutThroatPhysicalCues;
                    break;
                default:
                    return true;
            }

            foreach (string cue in cues)
            {
                if (ContainsPerformedCue(text, CommandParser.Normalize(cue)))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool ContainsPerformedRespectCue(string text, string cue)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(cue))
            {
                return false;
            }

            bool genericSalute = GenericRespectCues.Contains(
                cue,
                StringComparer.Ordinal);
            int searchFrom = 0;
            while (searchFrom <= text.Length - cue.Length)
            {
                int index = IndexOfCue(text, cue, searchFrom);
                if (index < 0)
                {
                    return false;
                }
                if (!IsCueSuppressed(text, index, cue.Length) &&
                    !IsRespectNounContinuation(text, index, cue.Length) &&
                    (!genericSalute ||
                     (!ClauseContainsXihaiReference(text, index) &&
                      !IsLinkedRespectMotionSuppressed(text, index))))
                {
                    return true;
                }
                searchFrom = index + Math.Max(1, cue.Length);
            }
            return false;
        }

        private static bool ContainsPerformedRespectCeremonyCue(string text)
        {
            foreach (Match match in RespectCeremonyPattern.Matches(text))
            {
                if (!IsCueSuppressed(text, match.Index, match.Length) &&
                    !IsLinkedRespectMotionSuppressed(text, match.Index) &&
                    !ClauseContainsXihaiReference(text, match.Index))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsLinkedRespectMotionSuppressed(
            string text,
            int cueIndex)
        {
            int clauseStart = cueIndex;
            while (clauseStart > 0 && !IsClauseSeparator(text[clauseStart - 1]))
            {
                clauseStart--;
            }
            if (cueIndex <= clauseStart)
            {
                return false;
            }

            Match nearestMotion = null;
            string precedingText = text.Substring(
                clauseStart,
                cueIndex - clauseStart);
            foreach (Match motion in RespectLinkedMotionPattern.Matches(precedingText))
            {
                if (motion.Success)
                {
                    nearestMotion = motion;
                }
            }
            if (nearestMotion == null)
            {
                return false;
            }

            int nearestIndex = clauseStart + nearestMotion.Index;
            int betweenStart = nearestIndex + nearestMotion.Length;
            string between = text.Substring(
                betweenStart,
                Math.Max(0, cueIndex - betweenStart));
            if (ContainsAny(between, RespectRecoveryMarkers))
            {
                return false;
            }
            return IsCueSuppressed(
                text,
                nearestIndex,
                nearestMotion.Length);
        }

        private static bool IsRespectNounContinuation(
            string text,
            int cueIndex,
            int cueLength)
        {
            if (cueLength <= 0 ||
                cueIndex < 0 ||
                cueIndex + cueLength >= text.Length ||
                text[cueIndex + cueLength - 1] != SceneActionVocabulary.Text("SceneActionFrameworkV1.LiChar")[0])
            {
                return false;
            }

            char nextChar = text[cueIndex + cueLength];
            if (Array.IndexOf(LiExcludedNounsChars.Value, nextChar) >= 0)
            {
                return true;
            }
            switch (nextChar)
            {
                default:
                    return false;
            }
        }

        private static bool ContainsPerformedStructuralXihaiCue(string text)
        {
            foreach (Match match in XihaiDegreeGesturePattern.Matches(text))
            {
                if (!ClauseContainsAny(text, match.Index, XihaiGeometryBlockers) &&
                    !IsCueSuppressed(text, match.Index, match.Length))
                {
                    return true;
                }
            }

            int segmentStart = 0;
            for (int index = 0; index <= text.Length; index++)
            {
                if (index < text.Length && !IsSentenceSeparator(text[index]))
                {
                    continue;
                }

                int length = index - segmentStart;
                if (length > 0)
                {
                    string segment = text.Substring(segmentStart, length);
                    if (IsStructuralXihaiSegment(segment) &&
                        HasUnsuppressedMotion(text, segmentStart, segment))
                    {
                        return true;
                    }
                }
                segmentStart = index + 1;
            }
            return false;
        }

        private static bool ClauseContainsXihaiReference(string text, int cueIndex)
        {
            int start = cueIndex;
            while (start > 0 && !IsClauseSeparator(text[start - 1]))
            {
                start--;
            }

            int end = cueIndex;
            while (end < text.Length && !IsClauseSeparator(text[end]))
            {
                end++;
            }

            string clause = text.Substring(start, end - start);
            SceneActionContractEntryV1 xihai = Entries.First(entry =>
                string.Equals(entry.IntentKey, Xihai, StringComparison.Ordinal));
            return xihai.NpcReplyAliases.Any(cue =>
                       clause.IndexOf(
                           CommandParser.Normalize(cue),
                           StringComparison.Ordinal) >= 0) ||
                   (!ContainsAny(clause, XihaiGeometryBlockers) &&
                    XihaiDegreeGesturePattern.IsMatch(clause)) ||
                   IsStructuralXihaiSegment(clause);
        }

        private static bool IsStructuralXihaiSegment(string segment)
        {
            if (ContainsAny(segment, XihaiGeometryBlockers))
            {
                return false;
            }

            bool hasSpecificLimb = ContainsAny(segment, XihaiSpecificLimbs);
            bool hasMotion = ContainsAny(segment, XihaiMotions);
            bool hasUpwardDirection = ContainsAny(segment, XihaiUpwardDirections);
            bool hasFingerDetail = ContainsAny(segment, XihaiFingerDetails);
            bool hasPalmDetail = ContainsAny(segment, XihaiPalmDetails);
            bool hasStraightArm = segment.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV1.Raw0150"), StringComparison.Ordinal) >= 0;

            return hasSpecificLimb && hasMotion &&
                   (hasUpwardDirection ||
                    (hasStraightArm && hasFingerDetail && hasPalmDetail));
        }

        private static bool HasUnsuppressedMotion(
            string fullText,
            int segmentStart,
            string segment)
        {
            foreach (string motion in XihaiMotions)
            {
                int searchFrom = 0;
                while (searchFrom <= segment.Length - motion.Length)
                {
                    int index = segment.IndexOf(
                        motion,
                        searchFrom,
                        StringComparison.Ordinal);
                    if (index < 0)
                    {
                        break;
                    }
                    if (!IsCueSuppressed(
                        fullText,
                        segmentStart + index,
                        motion.Length))
                    {
                        return true;
                    }
                    searchFrom = index + Math.Max(1, motion.Length);
                }
            }
            return false;
        }

        private static bool ContainsAny(string text, IEnumerable<string> cues)
        {
            return cues.Any(cue =>
                text.IndexOf(cue, StringComparison.Ordinal) >= 0);
        }

        private static bool ClauseContainsAny(
            string text,
            int cueIndex,
            IEnumerable<string> cues)
        {
            int start = cueIndex;
            while (start > 0 && !IsClauseSeparator(text[start - 1]))
            {
                start--;
            }

            int end = cueIndex;
            while (end < text.Length && !IsClauseSeparator(text[end]))
            {
                end++;
            }
            return ContainsAny(text.Substring(start, end - start), cues);
        }

        private static bool IsCueSuppressed(string text, int cueIndex, int cueLength)
        {
            if (string.IsNullOrEmpty(text) ||
                cueIndex < 0 ||
                cueLength <= 0 ||
                cueIndex + cueLength > text.Length)
            {
                return true;
            }

            int clauseStart = cueIndex;
            while (clauseStart > 0 && !IsClauseSeparator(text[clauseStart - 1]))
            {
                clauseStart--;
            }
            string before = CompactWhitespace(text.Substring(
                clauseStart,
                cueIndex - clauseStart));

            if (IsCueInsideQuotation(text, cueIndex, cueLength))
            {
                return true;
            }

            int nonPerformativeEnd = LastMarkerEnd(
                before,
                NonPerformativeContextSuppressors);
            int clauseRecoveryEnd = LastMarkerEnd(
                before,
                PerformanceRecoveryMarkers);
            if (HasUnrecoveredHypothetical(before))
            {
                return true;
            }
            if (nonPerformativeEnd >= 0 && clauseRecoveryEnd < nonPerformativeEnd)
            {
                return true;
            }

            if (HasUnrecoveredSentenceSuppression(text, cueIndex))
            {
                return true;
            }

            int latestSuppressorEnd = Math.Max(
                LastMarkerEnd(before, StrongCueSuppressors),
                Math.Max(
                    LastMarkerEnd(before, DirectCueSuppressors),
                    LastMarkerEnd(before, CompactEnglishSuppressors)));
            int latestPerformedEnd = Math.Max(
                LastMarkerEnd(before, PerformedDespiteNegationPrefixes),
                LastMarkerEnd(before, PerformanceRecoveryMarkers));
            if (latestSuppressorEnd >= 0 && latestPerformedEnd < latestSuppressorEnd)
            {
                return true;
            }

            int afterStart = cueIndex + cueLength;
            int clauseEnd = afterStart;
            while (clauseEnd < text.Length && !IsClauseSeparator(text[clauseEnd]))
            {
                clauseEnd++;
            }
            string after = CompactWhitespace(text.Substring(
                afterStart,
                clauseEnd - afterStart));
            after = after.TrimStart(
                '，', ',', '；', ';', '：', ':', '、', '-', '—');
            if (AfterCueSuppressors.Any(marker =>
                    after.StartsWith(marker, StringComparison.Ordinal)))
            {
                return true;
            }
            return clauseEnd < text.Length &&
                   (text[clauseEnd] == '？' || text[clauseEnd] == '?');
        }

        private static bool HasUnrecoveredSentenceSuppression(
            string text,
            int cueIndex)
        {
            int sentenceStart = cueIndex;
            while (sentenceStart > 0 && !IsSentenceSeparator(text[sentenceStart - 1]))
            {
                sentenceStart--;
            }

            string sentenceBefore = CompactWhitespace(text.Substring(
                sentenceStart,
                cueIndex - sentenceStart));
            if (HasUnrecoveredHypothetical(sentenceBefore))
            {
                return true;
            }
            int latestSuppressorEnd = Math.Max(
                LastMarkerEnd(sentenceBefore, StrongCueSuppressors),
                Math.Max(
                    LastMarkerEnd(sentenceBefore, DirectCueSuppressors),
                    LastMarkerEnd(sentenceBefore, CompactEnglishSuppressors)));
            int latestRecoveryEnd = Math.Max(
                LastMarkerEnd(sentenceBefore, PerformedDespiteNegationPrefixes),
                LastMarkerEnd(sentenceBefore, PerformanceRecoveryMarkers));
            return latestSuppressorEnd >= 0 && latestRecoveryEnd < latestSuppressorEnd;
        }

        private static bool HasUnrecoveredHypothetical(string before)
        {
            int hypotheticalEnd = LastMarkerEnd(
                before,
                HypotheticalCueSuppressors);
            if (hypotheticalEnd < 0)
            {
                return false;
            }

            int recoveryEnd = LastMarkerEnd(
                before,
                HypotheticalRecoveryMarkers);
            return recoveryEnd < hypotheticalEnd;
        }
        private static int LastMarkerEnd(string text, IEnumerable<string> markers)
        {
            int latest = -1;
            foreach (string marker in markers)
            {
                int index = text.LastIndexOf(marker, StringComparison.Ordinal);
                if (index >= 0)
                {
                    latest = Math.Max(latest, index + marker.Length);
                }
            }
            return latest;
        }

        private static bool IsCueInsideQuotation(
            string text,
            int cueIndex,
            int cueLength)
        {
            return IsInsidePairedQuotation(text, cueIndex, cueLength, '“', '”') ||
                   IsInsidePairedQuotation(text, cueIndex, cueLength, '‘', '’') ||
                   IsInsideSymmetricQuotation(text, cueIndex, cueLength, '"') ||
                   IsInsideSymmetricQuotation(text, cueIndex, cueLength, '\'');
        }

        private static bool IsInsidePairedQuotation(
            string text,
            int cueIndex,
            int cueLength,
            char opening,
            char closing)
        {
            int lastOpening = text.LastIndexOf(opening, cueIndex);
            int lastClosing = text.LastIndexOf(closing, cueIndex);
            return lastOpening > lastClosing &&
                   text.IndexOf(closing, cueIndex + cueLength) >= 0;
        }

        private static bool IsInsideSymmetricQuotation(
            string text,
            int cueIndex,
            int cueLength,
            char quote)
        {
            int countBefore = 0;
            for (int index = 0; index < cueIndex; index++)
            {
                if (text[index] == quote)
                {
                    countBefore++;
                }
            }
            return countBefore % 2 == 1 &&
                   text.IndexOf(quote, cueIndex + cueLength) >= 0;
        }

        private static string CompactWhitespace(string text)
        {
            return new string((text ?? string.Empty)
                .Where(character => !char.IsWhiteSpace(character))
                .ToArray());
        }

        private static bool IsClauseSeparator(char character)
        {
            switch (character)
            {
                case '，':
                case '。':
                case '；':
                case '！':
                case '？':
                case ',':
                case ';':
                case '!':
                case '?':
                case '\r':
                case '\n':
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsSentenceSeparator(char character)
        {
            switch (character)
            {
                case '。':
                case '；':
                case '！':
                case '？':
                case ';':
                case '!':
                case '?':
                case '\r':
                case '\n':
                    return true;
                default:
                    return false;
            }
        }
    }
}
