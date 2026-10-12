using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.SceneActions.Core
{
    public sealed class ImplicitEmotionDecisionV1
    {
        internal ImplicitEmotionDecisionV1(
            string intentKey,
            int score,
            IEnumerable<string> evidence)
        {
            IntentKey = intentKey ?? string.Empty;
            Score = score;
            Evidence = Array.AsReadOnly((evidence ?? Enumerable.Empty<string>()).ToArray());
        }

        public string IntentKey { get; }
        public int Score { get; }
        public IReadOnlyList<string> Evidence { get; }
    }

    public static class ImplicitEmotionInferenceV1
    {
        private const int MinimumScore = 5;
        private const int MinimumWinningMargin = 2;

        private static readonly ReadOnlyCollection<string> SupportedKeys =
            Array.AsReadOnly(new[]
            {
                SceneActionFrameworkV4.Fear,
                SceneActionFrameworkV4.Rage,
                SceneActionFrameworkV4.Disappointed,
                SceneActionFrameworkV4.Laugh,
                SceneActionFrameworkV4.Unsure
            });

        private static readonly string[] FearPallorCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0001"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0002"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0003"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0004"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0005"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0006"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0007")
        };
        private static readonly string[] FearFreezeCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0008"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0009"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0010"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0011"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0012"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0013"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0014"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0015")
        };
        private static readonly string[] FearTremorCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0016"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0017"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0018"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0019"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0020"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0021"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0022")
        };
        private static readonly string[] FearMaskingCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0023"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0024"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0025"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0026"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0027"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0028"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0029")
        };
        private static readonly string[] FearThreatCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0030"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0031"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0032"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0033"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0034"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0035"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0036"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0037"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0038"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0039"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0040"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0041")
        };
        private static readonly string[] FearVulnerabilityCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0042"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0043"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0044"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0045"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0046"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0047"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0048"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0049"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0050")
        };
        private static readonly string[] FearNegativeCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0051"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0052"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0053"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0054"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0055"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0056"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0057"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0058"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0059")
        };

        private static readonly string[] RageBodyCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0060"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0061"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0062"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0063"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0064"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0065"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0066"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0067"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0068"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0069"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0070")
        };
        private static readonly string[] RageMaskingCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0071"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0072"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0073"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0074"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0075"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0076")
        };
        private static readonly string[] RageProvocationCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0077"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0078"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0079"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0080"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0081"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0082"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0083"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0084")
        };
        private static readonly string[] RageDialogueCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0085"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0086"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0087"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0088"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0089"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0090")
        };
        private static readonly string[] RageNegativeCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0091"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0092"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0093"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0094"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0095"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0096")
        };

        private static readonly string[] DisappointedBodyCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0097"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0098"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0099"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0100"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0101"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0102"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0103"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0104"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0105"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0106"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0107")
        };
        private static readonly string[] DisappointedContextCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0108"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0109"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0110"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0111"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0112"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0113"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0114"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0115")
        };
        private static readonly string[] DisappointedDialogueCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0116"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0117"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0118"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0119"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0120"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0121"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0122")
        };
        private static readonly string[] DisappointedNegativeCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0123"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0124"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0125"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0126"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0127")
        };

        private static readonly string[] LaughBodyCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0128"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0129"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0130"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0131"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0132"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0133"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0134"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0135"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0136"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0137")
        };
        private static readonly string[] LaughContextCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0138"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0139"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0140"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0141"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0142"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0143")
        };
        private static readonly string[] LaughDialogueCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0144"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0145"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0146"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0147"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0148"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0149")
        };
        private static readonly string[] LaughNegativeCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0150"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0151"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0152"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0153"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0154")
        };

        private static readonly string[] UnsureBodyCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0155"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0156"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0157"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0158"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0159"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0160"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0161"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0162"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0163"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0164")
        };
        private static readonly string[] UnsureContextCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0165"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0166"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0167"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0168"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0169"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0170"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0171")
        };
        private static readonly string[] UnsureDialogueCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0172"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0173"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0174"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0175"),
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0176"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0177")
        };
        private static readonly string[] UnsureNegativeCues =
        {
            SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0178"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0179"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0180"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0181"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0182")
        };

        public static IReadOnlyList<string> SupportedIntentKeys => SupportedKeys;

        public static bool TryInfer(
            string previousPlayerText,
            string stageDirectionText,
            string fullNpcReplyText,
            IEnumerable<string> allowedIntentKeys,
            out ImplicitEmotionDecisionV1 decision)
        {
            decision = null;
            HashSet<string> allowed = new HashSet<string>(
                allowedIntentKeys ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            if (allowed.Count == 0)
            {
                return false;
            }

            string previous = CommandParser.Normalize(previousPlayerText);
            string stage = CommandParser.Normalize(stageDirectionText);
            string full = CommandParser.Normalize(fullNpcReplyText);
            if (string.IsNullOrEmpty(stage) && string.IsNullOrEmpty(full))
            {
                return false;
            }

            List<Candidate> candidates = new List<Candidate>();
            AddCandidate(candidates, allowed, ScoreFear(previous, stage, full));
            AddCandidate(candidates, allowed, ScoreRage(previous, stage, full));
            AddCandidate(candidates, allowed, ScoreDisappointed(previous, stage, full));
            AddCandidate(candidates, allowed, ScoreLaugh(previous, stage, full));
            AddCandidate(candidates, allowed, ScoreUnsure(previous, stage, full));

            Candidate[] ordered = candidates
                .Where(candidate => candidate.Score >= MinimumScore)
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.IntentKey, StringComparer.Ordinal)
                .ToArray();
            if (ordered.Length == 0)
            {
                return false;
            }
            if (ordered.Length > 1 &&
                ordered[0].Score - ordered[1].Score < MinimumWinningMargin)
            {
                return false;
            }

            decision = new ImplicitEmotionDecisionV1(
                ordered[0].IntentKey,
                ordered[0].Score,
                ordered[0].Evidence);
            return true;
        }

        private static Candidate ScoreFear(string previous, string stage, string full)
        {
            Candidate candidate = new Candidate(SceneActionFrameworkV4.Fear);
            string all = Join(previous, stage, full);
            if (ContainsAny(all, FearNegativeCues))
            {
                return candidate;
            }
            candidate.AddIf(
                ContainsAny(stage, FearPallorCues) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0183"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0184"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0185")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0186"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0187")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0188"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0187")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0189"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0187")),
                3,
                "visible-pallor");
            candidate.AddIf(
                ContainsAny(stage, FearFreezeCues) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0190"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0191")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0192"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0191")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0193"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0194")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0193"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0195")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0196"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0197")),
                2,
                "involuntary-freeze");
            candidate.AddIf(
                ContainsAny(stage, FearTremorCues) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0198"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0199")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0200"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0199")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0201"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0202")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0203"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0199")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0204"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0205")),
                3,
                "loss-of-control");
            candidate.Add(stage, FearMaskingCues, 2, "forced-composure");
            candidate.Add(previous, FearThreatCues, 3, "lethal-threat-context");
            candidate.Add(full, FearVulnerabilityCues, 2, "vulnerable-dialogue");
            return candidate;
        }

        private static Candidate ScoreRage(string previous, string stage, string full)
        {
            Candidate candidate = new Candidate(SceneActionFrameworkV4.Rage);
            string all = Join(previous, stage, full);
            if (ContainsAny(all, RageNegativeCues))
            {
                return candidate;
            }
            candidate.AddIf(
                ContainsAny(stage, RageBodyCues) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0206"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0207")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0208"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0207")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0209"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0210")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0211"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0212")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0213"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0212")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0214"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0215")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0214"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0216")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0217"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0218"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0219")),
                3,
                "hostile-body-tension");
            candidate.Add(stage, RageMaskingCues, 3, "suppressed-escalation");
            candidate.Add(previous, RageProvocationCues, 2, "provocation-context");
            candidate.AddIf(
                ContainsAny(full, RageDialogueCues) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0220"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0221")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0222"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0223")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0224"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0225")),
                2,
                "hostile-dialogue");
            return candidate;
        }

        private static Candidate ScoreDisappointed(string previous, string stage, string full)
        {
            Candidate candidate = new Candidate(SceneActionFrameworkV4.Disappointed);
            string all = Join(previous, stage, full);
            if (ContainsAny(all, DisappointedNegativeCues))
            {
                return candidate;
            }
            candidate.AddIf(
                ContainsAny(stage, DisappointedBodyCues) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0226"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0227")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0228"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0229"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0230")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0231"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0232")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0233"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0234")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0235"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0236")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0237"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0225")),
                3,
                "withdrawn-body-language");
            candidate.AddIf(
                ContainsAny(previous, DisappointedContextCues) ||
                ContainsOrdered(previous, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0238"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0239")) ||
                ContainsOrdered(previous, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0240"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0241")) ||
                ContainsOrdered(previous, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0242"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0243")) ||
                ContainsOrdered(previous, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0187"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0244")),
                2,
                "broken-expectation-context");
            candidate.AddIf(
                ContainsAny(full, DisappointedDialogueCues) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0245"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0246")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0247"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0248")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0249")),
                2,
                "resigned-dialogue");
            return candidate;
        }

        private static Candidate ScoreLaugh(string previous, string stage, string full)
        {
            Candidate candidate = new Candidate(SceneActionFrameworkV4.Laugh);
            string all = Join(previous, stage, full);
            if (ContainsAny(all, LaughNegativeCues))
            {
                return candidate;
            }
            candidate.AddIf(
                ContainsAny(stage, LaughBodyCues) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0250"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0251")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0252"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0253")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0250"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0254")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0231"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0255")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0256"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0257")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0258"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0257")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0237"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0259"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0250")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0260"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0261")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0262"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0263")),
                4,
                "suppressed-amusement-body-language");
            candidate.AddIf(
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0250"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0264"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0251")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0252"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0253")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0231"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0255")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0256"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0257")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0258"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0257")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0237"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0259"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0250")),
                1,
                "unmistakable-amusement");
            candidate.AddIf(
                ContainsAny(previous, LaughContextCues) ||
                ContainsOrdered(previous, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0265"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0266")) ||
                ContainsOrdered(previous, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0267"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0266")) ||
                ContainsOrdered(previous, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0268"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0269")),
                1,
                "absurd-context");
            candidate.AddIf(
                ContainsAny(full, LaughDialogueCues) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0270"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0271")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0272"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0273")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0274"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0270")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0238"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0275")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0276"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0277")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0260"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0278")),
                2,
                "amused-dialogue");
            return candidate;
        }

        private static Candidate ScoreUnsure(string previous, string stage, string full)
        {
            Candidate candidate = new Candidate(SceneActionFrameworkV4.Unsure);
            string all = Join(previous, stage, full);
            if (ContainsAny(all, UnsureNegativeCues))
            {
                return candidate;
            }
            candidate.AddIf(
                ContainsAny(stage, UnsureBodyCues) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0279"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0194")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0224"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0280")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0198"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0281")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0237"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0249")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0282"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0225")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0226"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0283")) ||
                ContainsOrdered(stage, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0284"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0285")),
                3,
                "indecisive-body-language");
            candidate.AddIf(
                ContainsAny(previous, UnsureContextCues) ||
                ContainsOrdered(previous, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0286"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0287")) ||
                ContainsOrdered(previous, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0288"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0289")) ||
                ContainsOrdered(previous, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0290"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0291")),
                2,
                "choice-conflict-context");
            candidate.AddIf(
                ContainsAny(full, UnsureDialogueCues) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0292"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0293")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0294"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0295")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0296"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0297")) ||
                ContainsOrdered(full, SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0294"), SceneActionVocabulary.Text("ImplicitEmotionInferenceV1.Raw0298")),
                2,
                "deliberative-dialogue");
            return candidate;
        }

        private static void AddCandidate(
            ICollection<Candidate> candidates,
            ISet<string> allowed,
            Candidate candidate)
        {
            if (candidate != null && allowed.Contains(candidate.IntentKey))
            {
                candidates.Add(candidate);
            }
        }

        private static string Join(params string[] values)
        {
            return string.Join(" ", values.Where(value => !string.IsNullOrEmpty(value)));
        }

        private static bool ContainsOrdered(string text, params string[] tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null || tokens.Length == 0)
            {
                return false;
            }
            int cursor = 0;
            foreach (string rawToken in tokens)
            {
                string token = CommandParser.Normalize(rawToken);
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }
                int index = text.IndexOf(token, cursor, StringComparison.Ordinal);
                if (index < 0)
                {
                    return false;
                }
                cursor = index + token.Length;
            }
            return true;
        }
        private static bool ContainsAny(string text, IEnumerable<string> cues)
        {
            return !string.IsNullOrEmpty(text) &&
                   (cues ?? Enumerable.Empty<string>()).Any(cue =>
                       !string.IsNullOrEmpty(cue) &&
                       text.IndexOf(CommandParser.Normalize(cue), StringComparison.Ordinal) >= 0);
        }

        private sealed class Candidate
        {
            public Candidate(string intentKey)
            {
                IntentKey = intentKey;
            }

            public string IntentKey { get; }
            public int Score { get; private set; }
            public List<string> Evidence { get; } = new List<string>();

            public void Add(
                string text,
                IEnumerable<string> cues,
                int points,
                string evidence)
            {
                AddIf(ContainsAny(text, cues), points, evidence);
            }

            public void AddIf(bool condition, int points, string evidence)
            {
                if (!condition)
                {
                    return;
                }
                Score += points;
                Evidence.Add(evidence);
            }
        }
    }
}
