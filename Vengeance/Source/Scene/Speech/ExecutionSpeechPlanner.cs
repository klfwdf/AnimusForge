using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Core;

namespace RichExecutions.Scene;

internal sealed class LocalSpeechPlanProvider : ISpeechPlanProvider
{
    private const float MaximumPlanSeconds = 30f;
    private const float PrisonerPhaseBudgetSeconds = 8f;

    public SpeechPlan Build(SpeechContext context, SpeechRecentHistory recentHistory,
        Func<string, string, string> localize)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (recentHistory is null) throw new ArgumentNullException(nameof(recentHistory));
        if (localize is null) throw new ArgumentNullException(nameof(localize));

        // The session, not the global/game RNG, selects the fixed plan. Tone does
        // not select prisoner personality or enter this seed.
        var seed = BitConverter.ToInt32(context.SessionId.ToByteArray(), 0) & int.MaxValue;
        var random = new Random(seed);
        var prisonerRandom = new Random(seed ^ 0x31415926);
        var crowdRandom = new Random(seed ^ 0x27182818);
        var selected = new Dictionary<string, string>(StringComparer.Ordinal);
        SpeechCue Line(string category, SpeechSpeaker speaker, int crowdIndex = -1,
            SpeechReaction reaction = SpeechReaction.None)
        {
            var templates = ExecutionSpeechCatalog.Groups[category];
            var candidates = templates.Select(item => item.Id).Where(id => !selected.ContainsKey(id)).ToArray();
            if (candidates.Length == 0) candidates = templates.Select(item => item.Id).ToArray();
            var speakerRandom = speaker == SpeechSpeaker.Victim ? prisonerRandom :
                speaker == SpeechSpeaker.Crowd ? crowdRandom : random;
            var id = recentHistory.Pick(category, candidates, speakerRandom);
            var template = templates.First(item => item.Id == id);
            var text = localize(template.Id, template.Fallback) ?? string.Empty;
            if (text.Length > ExecutionSpeechLineParser.MaximumLineCharacters)
                text = text.Substring(0, ExecutionSpeechLineParser.MaximumLineCharacters);
            if (string.IsNullOrWhiteSpace(text) || text.Contains("{=") || text.Contains("{VICTIM}") ||
                text.Contains("{EXECUTOR}") || text.Contains("{CHARGE}") || text.Contains("{METHOD}") ||
                text.Contains("{EVIDENCE_PLACE}"))
                throw new InvalidOperationException("Unresolved speech text: " + template.Id);
            selected[id] = category;
            return new SpeechCue(speaker, crowdIndex, text, id,
                ExecutionSpeechTiming.GetLineSeconds(text) + ExecutionSpeechTiming.FadeSeconds, reaction,
                isLastStatement: speaker == SpeechSpeaker.Victim);
        }

        var tone = context.Tone == ExecutionTone.Spectacle ? "Spectacle" :
            context.Tone == ExecutionTone.Terror ? "Terror" : "Judicial";
        // Strong evidence alone cannot invent a site or a ledger entry.
        var hasRecordedPlace = context.Evidence == EvidenceStrength.Strong &&
            !string.IsNullOrWhiteSpace(context.EvidencePlace);
        var declaration = new List<SpeechCue>
        {
            Line(hasRecordedPlace ? "Address_Recorded" : "Address_Alleged", SpeechSpeaker.Executioner),
            Line("Verdict_" + tone, SpeechSpeaker.Executioner)
        };

        var prisoner = new List<SpeechCue>();
        var missingPersonality = !context.Valor.HasValue && !context.Honor.HasValue;
        // The silence draw is made for every personality so tone and missing
        // metadata cannot accidentally reshuffle the primary response RNG.
        var silent = prisonerRandom.Next(2) == 0 && missingPersonality;
        if (!silent)
        {
            prisoner.Add(Line(GetPrisonerCategory(context), SpeechSpeaker.Victim));
            if (!missingPersonality)
            {
                var supplement = context.Relation >= 20 ? "Victim_Friendly" :
                    context.Relation <= -20 ? "Victim_Hostile" :
                    context.Evidence == EvidenceStrength.None ? "Victim_NoEvidence" :
                    context.Evidence == EvidenceStrength.Circumstantial ? "Victim_WeakEvidence" :
                    "Victim_StrongEvidence";
                prisoner.Add(Line(supplement, SpeechSpeaker.Victim));
                // The prisoner's voice must not change when only the ceremony
                // tone changes. Fit its optional second thought against its own
                // fixed budget, never against the executioner's chosen wording.
                if (prisoner.Sum(cue => cue.DurationSeconds) > PrisonerPhaseBudgetSeconds)
                    prisoner.RemoveAt(prisoner.Count - 1);
            }
        }

        var crowd = new List<SpeechCue>();
        var desiredVoices = context.Tone == ExecutionTone.Terror ? crowdRandom.Next(4) / 2 : crowdRandom.Next(3);
        var voiceCount = Math.Min(context.CrowdCount, desiredVoices);
        var fearfulSoundUsed = false;
        for (var i = 0; i < voiceCount; i++)
        {
            var category = GetCrowdCategory(context, i, crowdRandom);
            var reaction = category == "Crowd_Fear" && !fearfulSoundUsed && crowdRandom.Next(3) == 0
                ? SpeechReaction.Fear : SpeechReaction.None;
            fearfulSoundUsed |= reaction == SpeechReaction.Fear;
            crowd.Add(Line(category, SpeechSpeaker.Crowd, i, reaction));
        }

        var closing = new List<SpeechCue> { Line("Close_" + tone, SpeechSpeaker.Executioner) };
        var crowdBeforePrisoner = random.Next(2) == 0;
        var crowdMinimumSeconds = 4f;
        var cues = Compose(declaration, prisoner, crowd, closing, crowdBeforePrisoner, crowdMinimumSeconds);
        // Lengthy translated names retain every character. Remove optional
        // turns before playback rather than cutting any sentence or reveal.
        while (cues.Sum(cue => cue.DurationSeconds) > MaximumPlanSeconds)
        {
            if (crowd.Count > 1) crowd.RemoveAt(crowd.Count - 1);
            else if (crowd.Count > 0) crowd.Clear();
            else if (crowdMinimumSeconds > 3f) crowdMinimumSeconds = 3f;
            else break; // Preserve essential speech, even for exceptionally long names.
            cues = Compose(declaration, prisoner, crowd, closing, crowdBeforePrisoner, crowdMinimumSeconds);
        }

        // Only lines actually retained in this immutable plan enter campaign
        // memory; discarded optional alternatives do not suppress later lines.
        foreach (var cue in cues)
            if (!cue.IsPause) recentHistory.Remember(selected[cue.TextId], cue.TextId);
        return new SpeechPlan(cues);
    }

    private static string GetPrisonerCategory(SpeechContext context)
    {
        if (!context.Valor.HasValue && !context.Honor.HasValue) return "Victim_Neutral";
        var honorable = context.Honor.GetValueOrDefault() > 0;
        if (context.Valor.GetValueOrDefault() > 0)
            return honorable ? "Victim_Bold_Honorable" : "Victim_Bold";
        if (context.Valor.GetValueOrDefault() < 0)
            return honorable ? "Victim_Fearful_Honorable" : "Victim_Fearful";
        return honorable ? "Victim_Steady_Honorable" : "Victim_Steady";
    }

    private static string GetCrowdCategory(SpeechContext context, int voiceIndex, Random random)
    {
        var harsh = context.MethodId == "burning" || context.MethodId == "breaking_wheel" ||
            context.MethodId == "impalement" || context.MethodId == "stoning";
        if ((harsh || context.Tone == ExecutionTone.Terror) && random.Next(2) == 0)
            return "Crowd_Fear";
        if (context.Evidence == EvidenceStrength.None || context.Legitimacy != LegitimacyTier.Legal)
            return voiceIndex == 0 ? "Crowd_Doubt" : "Crowd_Sympathy";
        // Even an accepted verdict need not produce a unanimous crowd.
        return voiceIndex == 0 && context.Evidence == EvidenceStrength.Strong
            ? "Crowd_Support" : random.Next(2) == 0 ? "Crowd_Sympathy" : "Crowd_Doubt";
    }

    private static List<SpeechCue> Compose(List<SpeechCue> declaration, List<SpeechCue> prisoner,
        List<SpeechCue> crowd, List<SpeechCue> closing, bool crowdBeforePrisoner, float crowdMinimumSeconds)
    {
        var result = new List<SpeechCue> { SpeechCue.Pause(2f) };
        AppendPhase(result, declaration, 7f);
        if (crowdBeforePrisoner) AppendPhase(result, crowd, crowdMinimumSeconds);
        AppendPhase(result, prisoner, 6f);
        if (!crowdBeforePrisoner) AppendPhase(result, crowd, crowdMinimumSeconds);
        AppendPhase(result, closing, 3f);
        return result;
    }

    private static void AppendPhase(List<SpeechCue> destination, List<SpeechCue> lines, float minimumSeconds)
    {
        destination.AddRange(lines);
        var remaining = minimumSeconds - lines.Sum(cue => cue.DurationSeconds);
        if (remaining > 0f) destination.Add(SpeechCue.Pause(remaining));
    }
}
