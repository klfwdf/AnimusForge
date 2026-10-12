using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.SceneActions.Core
{
    public sealed class SceneActionContractEntryV3
    {
        internal SceneActionContractEntryV3(
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
    /// V3 freezes the complete twenty-four-intent semantic contract. Local parsing
    /// and the AF classifier prompt consume this same table; raw engine action ids
    /// are deliberately absent from the semantic surface.
    /// </summary>
    public static class SceneActionFrameworkV3
    {
        public const int ContractVersion = 3;

        public const string Kneel = SceneActionFrameworkV2.Kneel;
        public const string StandUp = SceneActionFrameworkV2.StandUp;
        public const string Xihai = SceneActionFrameworkV2.Xihai;
        public const string Cheer = SceneActionFrameworkV2.Cheer;
        public const string Applaud = SceneActionFrameworkV2.Applaud;
        public const string Respect = SceneActionFrameworkV2.Respect;
        public const string Threat = SceneActionFrameworkV2.Threat;
        public const string Surrender = SceneActionFrameworkV2.Surrender;
        public const string Laugh = SceneActionFrameworkV2.Laugh;
        public const string Point = SceneActionFrameworkV2.Point;
        public const string Rage = SceneActionFrameworkV2.Rage;
        public const string Fear = SceneActionFrameworkV2.Fear;
        public const string Disappointed = SceneActionFrameworkV2.Disappointed;
        public const string Challenge = SceneActionFrameworkV2.Challenge;
        public const string Search = SceneActionFrameworkV2.Search;
        public const string Dance = SceneActionFrameworkV2.Dance;
        public const string Greet = "greet";
        public const string Agree = "agree";
        public const string Disagree = "disagree";
        public const string Unsure = "unsure";
        public const string Explain = "explain";
        public const string Promise = "promise";
        public const string CrossArms = "cross_arms";
        public const string DeepBow = "deep_bow";

        private static readonly ReadOnlyCollection<SceneActionContractEntryV3> Entries =
            Array.AsReadOnly(new[]
            {
                Legacy(Kneel, SceneActionCatalogText.Get("kneel", true).Name, SceneActionCatalogText.Get("kneel", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0001"), "kneel" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0001"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0002"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0003"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0004"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0005"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0006"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0007"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0008"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0009"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0010"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0011"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0012"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0013")
                    },
                    SceneActionCatalogText.Get("kneel", true).Positive,
                    SceneActionCatalogText.Get("kneel", true).Negative),
                Legacy(StandUp, SceneActionCatalogText.Get("stand_up", true).Name, SceneActionCatalogText.Get("stand_up", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0014"), "stand up" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0015"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0016"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0017"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0018"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0019"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0020"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0021"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0022"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0023"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0024"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0025"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0026")
                    },
                    SceneActionCatalogText.Get("stand_up", true).Positive,
                    SceneActionCatalogText.Get("stand_up", true).Negative),
                Legacy(Xihai, SceneActionCatalogText.Get("xihai", true).Name, SceneActionCatalogText.Get("xihai", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0027"), "xihai" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0028"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0029"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0030"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0031"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0032"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0033"),
                        "raised his right arm at forty-five degrees in salute"
                    },
                    SceneActionCatalogText.Get("xihai", true).Positive,
                    SceneActionCatalogText.Get("xihai", true).Negative),
                Legacy(Cheer, SceneActionCatalogText.Get("cheer", true).Name, SceneActionCatalogText.Get("cheer", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0034"), "cheer" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0034"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0035"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0036"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0037"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0038"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0039"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0040"),
                        "raised her fist and cheered loudly"
                    },
                    SceneActionCatalogText.Get("cheer", true).Positive,
                    SceneActionCatalogText.Get("cheer", true).Negative),
                Legacy(Applaud, SceneActionCatalogText.Get("applaud", true).Name, SceneActionCatalogText.Get("applaud", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0041"), "applaud" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0041"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0042"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0043"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0044"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0045"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0046"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0047"), "clapped her hands"
                    },
                    SceneActionCatalogText.Get("applaud", true).Positive,
                    SceneActionCatalogText.Get("applaud", true).Negative),
                Legacy(Respect, SceneActionCatalogText.Get("respect", true).Name, SceneActionCatalogText.Get("respect", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0048"), "salute" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0048"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0049"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0050"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0051"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0052"), "bowed politely", "bowed in respect"
                    },
                    SceneActionCatalogText.Get("respect", true).Positive,
                    SceneActionCatalogText.Get("respect", true).Negative),
                Legacy(Threat, SceneActionCatalogText.Get("threat", true).Name, SceneActionCatalogText.Get("threat", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0053"), "threaten" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0053"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0054"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0055"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0056"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0057"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0058"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0059"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0060"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0061"), "clenched his fist in threat",
                        "clenched fist threateningly", "made a threatening fist gesture",
                        "threateningly clenched his fist", "shook a fist threateningly"
                    },
                    SceneActionCatalogText.Get("threat", true).Positive,
                    SceneActionCatalogText.Get("threat", true).Negative),
                Legacy(Surrender, SceneActionCatalogText.Get("surrender", true).Name, SceneActionCatalogText.Get("surrender", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0062"), "surrender" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0062"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0063"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0064"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0065"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0066"),
                        "dropped his weapon and raised both hands in surrender"
                    },
                    SceneActionCatalogText.Get("surrender", true).Positive,
                    SceneActionCatalogText.Get("surrender", true).Negative),
                Legacy(Laugh, SceneActionCatalogText.Get("laugh", true).Name, SceneActionCatalogText.Get("laugh", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0067"), "laugh" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0067"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0068"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0069"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0070"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0071"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0072"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0073"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0074"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0075"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0076"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0077"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0078"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0079"), "burst out laughing",
                        "burst into laughter", "let out a burst of laughter"
                    },
                    SceneActionCatalogText.Get("laugh", true).Positive,
                    SceneActionCatalogText.Get("laugh", true).Negative),
                Legacy(Point, SceneActionCatalogText.Get("point", true).Name, SceneActionCatalogText.Get("point", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0080"), "point" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0080"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0081"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0082"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0083"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0084"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0085"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0086"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0087"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0088"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0089"), "gestured toward the gate"
                    },
                    SceneActionCatalogText.Get("point", true).Positive,
                    SceneActionCatalogText.Get("point", true).Negative),
                Legacy(Rage, SceneActionCatalogText.Get("rage", true).Name, SceneActionCatalogText.Get("rage", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0090"), "rage" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0090"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0091"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0092"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0093"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0094"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0095"), "flew into a rage and roared"
                    },
                    SceneActionCatalogText.Get("rage", true).Positive,
                    SceneActionCatalogText.Get("rage", true).Negative),
                Legacy(Fear, SceneActionCatalogText.Get("fear", true).Name, SceneActionCatalogText.Get("fear", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0096"), "fear" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0096"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0097"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0098"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0099"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0100"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0101"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0102"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0103"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0104"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0105"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0106"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0107"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0108"), "trembled in fear"
                    },
                    SceneActionCatalogText.Get("fear", true).Positive,
                    SceneActionCatalogText.Get("fear", true).Negative),
                Legacy(Disappointed, SceneActionCatalogText.Get("disappointed", true).Name, SceneActionCatalogText.Get("disappointed", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0109"), "disappointed" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0109"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0110"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0111"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0112"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0113"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0114"), "sighed in disappointment"
                    },
                    SceneActionCatalogText.Get("disappointed", true).Positive,
                    SceneActionCatalogText.Get("disappointed", true).Negative),
                Legacy(Challenge, SceneActionCatalogText.Get("challenge", true).Name, SceneActionCatalogText.Get("challenge", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0115"), "challenge" },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0115"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0116"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0117"), "beckoned challengingly" },
                    SceneActionCatalogText.Get("challenge", true).Positive,
                    SceneActionCatalogText.Get("challenge", true).Negative),
                Legacy(Search, SceneActionCatalogText.Get("search", true).Name, SceneActionCatalogText.Get("search", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0118"), "search" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0118"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0119"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0120"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0121"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0122"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0123"), "scanned the room"
                    },
                    SceneActionCatalogText.Get("search", true).Positive,
                    SceneActionCatalogText.Get("search", true).Negative),
                Legacy(Dance, SceneActionCatalogText.Get("dance", true).Name, SceneActionCatalogText.Get("dance", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0124"), "dance" },
                    new[]
                    {
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0124"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0125"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0126"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0127"),
                        SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0128"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0129"), "started dancing"
                    },
                    SceneActionCatalogText.Get("dance", true).Positive,
                    SceneActionCatalogText.Get("dance", true).Negative),
                NewAction(Greet, ActionMode.RandomGroup, true, SceneActionCatalogText.Get("greet", true).Name,
                    SceneActionCatalogText.Get("greet", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0130"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0131"), "greet" },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0132"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0133"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0134"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0135"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0136"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0137"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0138"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0139"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0140"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0141"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0142"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0143"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0144"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0145"), "waved in greeting", "greeted with a wave", "waved hello", SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0146"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0131"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0147"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0148"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0149") },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0130"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0131"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0150"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0151"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0152"), "hello", "greet", "greeting", "waved" },
                    SceneActionCatalogText.Get("greet", true).Positive,
                    SceneActionCatalogText.Get("greet", true).Negative),
                NewAction(Agree, ActionMode.RandomGroup, true, SceneActionCatalogText.Get("agree", true).Name,
                    SceneActionCatalogText.Get("agree", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0153"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0154"), "agree" },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0155"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0156"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0153"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0157"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0158"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0159"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0160"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0161"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0162"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0163"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0164"), "nodded in agreement", "nodded firmly in agreement", "nodded yes", "nodded approvingly", SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0165"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0166"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0167") },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0168"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0169"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0170"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0171"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0172"), "agree", "agreed", "agreement" },
                    SceneActionCatalogText.Get("agree", true).Positive,
                    SceneActionCatalogText.Get("agree", true).Negative),
                NewAction(Disagree, ActionMode.RandomGroup, true, SceneActionCatalogText.Get("disagree", true).Name,
                    SceneActionCatalogText.Get("disagree", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0173"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0174"), "disagree" },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0175"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0176"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0173"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0177"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0178"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0179"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0180"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0181"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0182"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0183"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0184"), "shook his head in disagreement", "shook her head no", "shook his head to disagree", "shook his head to show clear disagreement", SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0185"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0186"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0187") },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0188"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0189"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0190"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0191"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0192"), "disagree", "disagreed", "disagreement" },
                    SceneActionCatalogText.Get("disagree", true).Positive,
                    SceneActionCatalogText.Get("disagree", true).Negative),
                NewAction(Unsure, ActionMode.RandomGroup, true, SceneActionCatalogText.Get("unsure", true).Name,
                    SceneActionCatalogText.Get("unsure", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0193"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0194"), "unsure", "shrug" },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0195"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0196"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0194"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0197"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0198"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0199"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0200"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0201"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0202"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0203"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0204"), "shrugged uncertainly", "shrugged, unsure", "shrugged and opened his hands uncertainly", "spread his hands in uncertainty", SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0205"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0206"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0207") },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0193"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0208"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0209"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0210"), "uncertain", "uncertainty" },
                    SceneActionCatalogText.Get("unsure", true).Positive,
                    SceneActionCatalogText.Get("unsure", true).Negative),
                NewAction(Explain, ActionMode.RandomGroup, true, SceneActionCatalogText.Get("explain", true).Name,
                    SceneActionCatalogText.Get("explain", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0211"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0212"), "explain" },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0213"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0214"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0215"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0216"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0217"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0218"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0219"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0220"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0221"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0222"), "gestured while explaining", "gestured back and forth while explaining", "gestured back and forth while explaining the plan", "explained with his hands", "explained with a hand gesture", SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0223"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0224"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0225"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0226") },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0227"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0228"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0229"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0230"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0231"), "explaining", "explanation" },
                    SceneActionCatalogText.Get("explain", true).Positive,
                    SceneActionCatalogText.Get("explain", true).Negative),
                NewAction(Promise, ActionMode.OneShot, true, SceneActionCatalogText.Get("promise", true).Name,
                    SceneActionCatalogText.Get("promise", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0232"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0233"), "promise" },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0232"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0234"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0235"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0236"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0237"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0238"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0239"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0240"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0241"), "raised his hand to swear", "made an oath gesture", "raised his hand and swore", "raised his right hand and swore", "raised her right hand and swore", SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0242"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0243"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0244") },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0245"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0246"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0247"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0248"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0249"), "promise", "promised", "swore" },
                    SceneActionCatalogText.Get("promise", true).Positive,
                    SceneActionCatalogText.Get("promise", true).Negative),
                NewAction(CrossArms, ActionMode.OneShot, true, SceneActionCatalogText.Get("cross_arms", true).Name,
                    SceneActionCatalogText.Get("cross_arms", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0250"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0251"), "cross arms", "crossarms" },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0252"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0250"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0253"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0254"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0255"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0256"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0257"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0258"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0259"), "crossed his arms", "folded her arms", "folded his arms", SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0260"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0261"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0262") },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0250"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0251"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0263"), "cross arms", "crossed arms" },
                    SceneActionCatalogText.Get("cross_arms", true).Positive,
                    SceneActionCatalogText.Get("cross_arms", true).Negative),
                NewAction(DeepBow, ActionMode.OneShot, false, SceneActionCatalogText.Get("deep_bow", true).Name,
                    SceneActionCatalogText.Get("deep_bow", true).Description,
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0264"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0265"), "deep bow", "deepbow" },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0264"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0265"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0266"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0267"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0268"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0269"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0270"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0271"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0272"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0273"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0274"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0275"), "bowed deeply", "made a deep bow", "made a profound bow", SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0276"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0277"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0278") },
                    new[] { SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0264"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0265"), SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0272"), "deep bow" },
                    SceneActionCatalogText.Get("deep_bow", true).Positive,
                    SceneActionCatalogText.Get("deep_bow", true).Negative)
            });

        private static readonly HashSet<string> NewIntentKeys = new HashSet<string>(
            Entries.Skip(16).Select(entry => entry.IntentKey),
            StringComparer.Ordinal);

        private static readonly HashSet<string> KneelOverlayKeys = new HashSet<string>(
            Entries.Where(entry => entry.CanOverlayKneel).Select(entry => entry.IntentKey),
            StringComparer.Ordinal);

        private static readonly string[] UnsupportedActionCues = SceneActionVocabulary.List("SceneActionFrameworkV3.UnsupportedActionCues");

        public static IReadOnlyList<SceneActionContractEntryV3> LogicalActions => Entries;

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
                SceneActionFrameworkV2.ResolveNaturalActionDescription(normalized));
            foreach (SceneActionContractEntryV3 entry in Entries.Take(16))
            {
                SceneActionContractEntryV2 legacy = SceneActionFrameworkV2.LogicalActions
                    .Single(source => string.Equals(
                        source.IntentKey,
                        entry.IntentKey,
                        StringComparison.Ordinal));
                HashSet<string> v2Cues = new HashSet<string>(
                    legacy.NaturalLanguageAliases.Select(CommandParser.Normalize),
                    StringComparer.Ordinal);
                if (entry.PerformedCues
                    .Select(CommandParser.Normalize)
                    .Where(cue => !v2Cues.Contains(cue))
                    .Any(cue => SceneActionFrameworkV1.ContainsPerformedCue(
                        normalized,
                        cue)))
                {
                    resolved.Add(entry.IntentKey);
                }
            }
            foreach (SceneActionContractEntryV3 entry in Entries.Skip(16))
            {
                if (entry.PerformedCues.Any(cue =>
                        SceneActionFrameworkV1.ContainsPerformedCue(
                            normalized,
                            CommandParser.Normalize(cue))))
                {
                    resolved.Add(entry.IntentKey);
                }
            }

            if (resolved.Contains(DeepBow, StringComparer.Ordinal) &&
                resolved.Contains(Respect, StringComparer.Ordinal) &&
                !ContainsIndependentRespectOutsideDeepBow(normalized))
            {
                resolved.RemoveAll(key => string.Equals(key, Respect, StringComparison.Ordinal));
            }
            if (resolved.Contains(Xihai, StringComparer.Ordinal) &&
                resolved.Contains(Respect, StringComparer.Ordinal) &&
                !ContainsIndependentRespectOutsideXihai(normalized))
            {
                resolved.RemoveAll(key => string.Equals(key, Respect, StringComparison.Ordinal));
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
            resolved.RemoveAll(key =>
                matchedKeys.Contains(key) && !preferredKeys.Contains(key));

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

        private static List<CueSpan> FindCueSpans(
            string normalized,
            Func<SceneActionContractEntryV3, IEnumerable<string>> selectCues)
        {
            List<CueSpan> matches = new List<CueSpan>();
            foreach (SceneActionContractEntryV3 entry in Entries)
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
                !string.Equals(
                    longer.IntentKey,
                    match.IntentKey,
                    StringComparison.Ordinal) &&
                longer.Length > match.Length &&
                longer.Start <= match.Start &&
                longer.End >= match.End));
        }

        public static bool ContainsNaturalActionReference(string text)
        {
            return ResolveNaturalActionReferences(text).Count > 0 ||
                   SceneActionFrameworkV2.ContainsNaturalActionReference(text);
        }

        public static bool ContainsUnsupportedActionReference(string text)
        {
            string normalized = CommandParser.Normalize(text);
            bool danceOwnsTurn = ResolveNaturalActionDescription(normalized)
                .Contains(Dance, StringComparer.Ordinal);
            return UnsupportedActionCues.Any(cue =>
                string.Equals(cue, SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0279"), StringComparison.Ordinal) && danceOwnsTurn
                    ? false
                    : SceneActionFrameworkV1.IndexOfCue(
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
            HashSet<string> legacyCoverage = new HashSet<string>(performed, StringComparer.Ordinal);
            if (performed.Contains(DeepBow))
            {
                legacyCoverage.Add(Respect);
            }
            if (performed.Contains(Cheer))
            {
                // V1/V2 use the broad "挥舞双臂" cue for rage; a longer
                // cheer gesture owns that overlap when it is actually performed.
                legacyCoverage.Add(Rage);
            }
            if (performed.Contains(Agree) || performed.Contains(Greet))
            {
                // Generic "颔首/致意" is a respect reference covered by the
                // more specific performed gesture.
                legacyCoverage.Add(Respect);
            }
            if (SceneActionFrameworkV2.HasSuppressedKnownActionReference(
                text,
                legacyCoverage))
            {
                return true;
            }

            if (performed.Contains(Disappointed))
            {
                legacyCoverage.Add(Disagree);
            }

            return ResolveNaturalActionReferences(text)
                .Where(NewIntentKeys.Contains)
                .Any(key => !performed.Contains(key) &&
                            HasUnsuppressedReference(text, key));
        }

        internal static bool HasUnsuppressedReference(
            string text,
            string intentKey)
        {
            string normalized = CommandParser.Normalize(text);
            SceneActionContractEntryV3 entry = Entries.Single(value =>
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
                    if (IsReferenceCueContextSuppressed(
                        normalized,
                        intentKey,
                        normalizedCue,
                        index) ||
                        SceneActionFrameworkV1.ContainsPerformedCue(
                            normalized,
                            normalizedCue))
                    {
                        if (!IsReferenceCueContextSuppressed(
                            normalized,
                            intentKey,
                            normalizedCue,
                            index))
                        {
                            return true;
                        }
                    }
                    searchFrom = index + Math.Max(1, normalizedCue.Length);
                }
            }
            return false;
        }

        private static bool IsReferenceCueContextSuppressed(
            string normalized,
            string intentKey,
            string cue,
            int cueIndex)
        {
            if (!string.Equals(intentKey, Disagree, StringComparison.Ordinal))
            {
                return false;
            }

            if (string.Equals(cue, SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0190"), StringComparison.Ordinal))
            {
                string after = normalized.Substring(
                    cueIndex + cue.Length,
                    Math.Min(
                        normalized.Length - cueIndex - cue.Length,
                        8));
                return after.StartsWith(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0062"), StringComparison.Ordinal) ||
                       after.StartsWith(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0001"), StringComparison.Ordinal) ||
                       after.StartsWith(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0048"), StringComparison.Ordinal) ||
                       after.StartsWith(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0248"), StringComparison.Ordinal) ||
                       after.StartsWith(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0245"), StringComparison.Ordinal);
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
                    "Catalog does not match the twenty-four-action SceneActionFrameworkV3 contract.");
            }
            foreach (SceneActionContractEntryV3 entry in Entries)
            {
                if (!catalog.TryGetIntent(entry.IntentKey, out IntentDefinition intent) ||
                    intent.Kind != entry.Kind ||
                    !string.Equals(intent.ActionKey, entry.ActionKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Catalog intent drifted from SceneActionFrameworkV3: " + entry.IntentKey);
                }
                if (entry.Kind == IntentKind.PlayAction &&
                    (!catalog.Actions.TryGetValue(entry.ActionKey, out ActionDefinition action) ||
                     action.Mode != entry.PlaybackMode))
                {
                    throw new InvalidOperationException(
                        "Catalog action mode drifted from SceneActionFrameworkV3: " + entry.ActionKey);
                }
                if (string.IsNullOrWhiteSpace(entry.DisplayNameZhCn) ||
                    string.IsNullOrWhiteSpace(entry.SemanticDescriptionZhCn) ||
                    entry.ExactAliases.Count == 0 ||
                    entry.PerformedCues.Count == 0 ||
                    entry.PositiveExamples.Count < 2 ||
                    entry.NegativeExamples.Count < 2)
                {
                    throw new InvalidOperationException(
                        "V3 semantic definition is incomplete: " + entry.IntentKey);
                }
            }
        }

        private static bool ContainsIndependentRespectOutsideDeepBow(string normalized)
        {
            string remainder = normalized;
            SceneActionContractEntryV3 deepBow = Entries.Single(entry =>
                string.Equals(entry.IntentKey, DeepBow, StringComparison.Ordinal));
            foreach (string cue in deepBow.PerformedCues
                         .Select(CommandParser.Normalize)
                         .OrderByDescending(value => value.Length))
            {
                remainder = remainder.Replace(cue, string.Empty);
            }
            return SceneActionFrameworkV2.ResolveNaturalActionDescription(remainder)
                .Contains(Respect, StringComparer.Ordinal);
        }

        private static bool ContainsIndependentRespectOutsideXihai(string normalized)
        {
            SceneActionContractEntryV3 respect = Entries.Single(entry =>
                string.Equals(entry.IntentKey, Respect, StringComparison.Ordinal));
            foreach (string cue in respect.PerformedCues
                         .Select(CommandParser.Normalize)
                         .OrderByDescending(value => value.Length))
            {
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
                    if (!IsXihaiClause(normalized, index) &&
                        !IsNegatedAlternativeClause(normalized, index))
                    {
                        return true;
                    }
                    searchFrom = index + Math.Max(1, cue.Length);
                }
            }
            return false;
        }

        private static bool IsNegatedAlternativeClause(
            string normalized,
            int cueIndex)
        {
            int start = cueIndex;
            while (start > 0 && !IsClauseSeparator(normalized[start - 1]))
            {
                start--;
            }

            string beforeCue = normalized.Substring(start, cueIndex - start);
            return beforeCue.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0280"), StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0281"), StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0282"), StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0283"), StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf("rather than", StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf("ratherthan", StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf("not ordinary", StringComparison.Ordinal) >= 0 ||
                   beforeCue.IndexOf("notordinary", StringComparison.Ordinal) >= 0;
        }

        private static bool IsXihaiClause(string normalized, int cueIndex)
        {
            int start = cueIndex;
            while (start > 0 && !IsClauseSeparator(normalized[start - 1]))
            {
                start--;
            }
            int end = cueIndex;
            while (end < normalized.Length && !IsClauseSeparator(normalized[end]))
            {
                end++;
            }

            string clause = normalized.Substring(start, end - start);
            if (clause.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0027"), StringComparison.Ordinal) >= 0 ||
                clause.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0284"), StringComparison.Ordinal) >= 0 ||
                clause.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0285"), StringComparison.Ordinal) >= 0 ||
                clause.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0286"), StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            bool hasRightArm = clause.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0287"), StringComparison.Ordinal) >= 0 ||
                               clause.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0288"), StringComparison.Ordinal) >= 0 ||
                               clause.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0289"), StringComparison.Ordinal) >= 0;
            bool hasUpwardGeometry = clause.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0290"), StringComparison.Ordinal) >= 0 ||
                                     clause.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0291"), StringComparison.Ordinal) >= 0 ||
                                     clause.IndexOf("45", StringComparison.Ordinal) >= 0 ||
                                     clause.IndexOf(SceneActionVocabulary.Text("SceneActionFrameworkV3.Raw0292"), StringComparison.Ordinal) >= 0;
            return hasRightArm && hasUpwardGeometry;
        }

        private static bool IsClauseSeparator(char character)
        {
            return character == '，' || character == ',' || character == '；' ||
                   character == ';' || character == '。' || character == '.' ||
                   character == '！' || character == '!' || character == '？' ||
                   character == '?';
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

        private static SceneActionContractEntryV3 Legacy(
            string intentKey,
            string displayName,
            string description,
            IEnumerable<string> exactAliases,
            IEnumerable<string> referenceCues,
            IEnumerable<string> positiveExamples,
            IEnumerable<string> negativeExamples)
        {
            SceneActionContractEntryV2 source = SceneActionFrameworkV2.LogicalActions
                .Single(entry => string.Equals(
                    entry.IntentKey,
                    intentKey,
                    StringComparison.Ordinal));
            return new SceneActionContractEntryV3(
                source.IntentKey,
                source.Kind,
                source.ActionKey,
                source.PlaybackMode,
                source.CanOverlayKneel,
                displayName,
                description,
                exactAliases,
                source.NaturalLanguageAliases.Concat(
                    referenceCues ?? Enumerable.Empty<string>()),
                source.NaturalLanguageAliases.Concat(referenceCues ?? Enumerable.Empty<string>()),
                positiveExamples,
                negativeExamples);
        }

        private static SceneActionContractEntryV3 NewAction(
            string key,
            ActionMode mode,
            bool canOverlayKneel,
            string displayName,
            string description,
            IEnumerable<string> exactAliases,
            IEnumerable<string> performedCues,
            IEnumerable<string> referenceCues,
            IEnumerable<string> positiveExamples,
            IEnumerable<string> negativeExamples)
        {
            return new SceneActionContractEntryV3(
                key,
                IntentKind.PlayAction,
                key,
                mode,
                canOverlayKneel,
                displayName,
                description,
                exactAliases,
                performedCues,
                referenceCues,
                positiveExamples,
                negativeExamples);
        }
    }
}
