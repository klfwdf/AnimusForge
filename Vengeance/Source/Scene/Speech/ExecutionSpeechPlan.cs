using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Core;

namespace RichExecutions.Scene;

// A main-thread snapshot. Providers never receive live campaign or mission objects.
internal sealed class SpeechContext
{
    internal SpeechContext(Guid sessionId, string victimName, string executorName,
        string chargeName, string methodName, string methodId, string chargeId,
        string venueName, string evidencePlace, EvidenceStrength evidence,
        ExecutionTone tone, LegitimacyTier legitimacy, int? valor, int? honor,
        int? relation, int crowdCount)
    {
        SessionId = sessionId;
        VictimName = victimName ?? string.Empty;
        ExecutorName = executorName ?? string.Empty;
        ChargeName = chargeName ?? string.Empty;
        MethodName = methodName ?? string.Empty;
        MethodId = methodId ?? string.Empty;
        ChargeId = chargeId ?? string.Empty;
        VenueName = venueName ?? string.Empty;
        EvidencePlace = evidencePlace ?? string.Empty;
        Evidence = evidence;
        Tone = tone;
        Legitimacy = legitimacy;
        Valor = valor;
        Honor = honor;
        Relation = relation;
        CrowdCount = Math.Max(0, crowdCount);
    }

    internal Guid SessionId { get; }
    internal string VictimName { get; }
    internal string ExecutorName { get; }
    internal string ChargeName { get; }
    internal string MethodName { get; }
    internal string MethodId { get; }
    internal string ChargeId { get; }
    internal string VenueName { get; }
    // Empty unless a matching ledger entry was resolved to a real settlement.
    internal string EvidencePlace { get; }
    internal EvidenceStrength Evidence { get; }
    internal ExecutionTone Tone { get; }
    internal LegitimacyTier Legitimacy { get; }
    internal int? Valor { get; }
    internal int? Honor { get; }
    internal int? Relation { get; }
    internal int CrowdCount { get; }
}

internal enum SpeechSpeaker { Executioner, Victim, Crowd }
internal enum SpeechReaction { None, Fear }

internal sealed class SpeechCue
{
    internal SpeechCue(SpeechSpeaker speaker, int crowdIndex, string text, string textId,
        float durationSeconds, SpeechReaction reaction = SpeechReaction.None, bool isPause = false)
    {
        if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        Speaker = speaker;
        CrowdIndex = crowdIndex;
        Text = isPause ? string.Empty : text ?? string.Empty;
        TextId = isPause ? string.Empty : textId ?? string.Empty;
        DurationSeconds = durationSeconds;
        Reaction = isPause ? SpeechReaction.None : reaction;
        IsPause = isPause;
    }

    internal SpeechSpeaker Speaker { get; }
    internal int CrowdIndex { get; }
    internal string Text { get; }
    internal string TextId { get; }
    // Includes the fade: a following cue never talks over a readable previous line.
    internal float DurationSeconds { get; }
    internal SpeechReaction Reaction { get; }
    internal bool IsPause { get; }

    internal static SpeechCue Pause(float seconds) =>
        new(SpeechSpeaker.Executioner, -1, string.Empty, string.Empty, seconds, isPause: true);
}

internal sealed class SpeechPlan
{
    internal SpeechPlan(IEnumerable<SpeechCue> cues)
    {
        if (cues is null) throw new ArgumentNullException(nameof(cues));
        var copy = cues.ToArray();
        if (copy.Any(cue => cue is null)) throw new ArgumentException("Null speech cue.", nameof(cues));
        Cues = Array.AsReadOnly(copy);
        TotalSeconds = copy.Sum(cue => cue.DurationSeconds);
    }

    internal IReadOnlyList<SpeechCue> Cues { get; }
    internal float TotalSeconds { get; }
}

internal interface ISpeechPlanProvider
{
    SpeechPlan Build(SpeechContext context, SpeechRecentHistory recentHistory,
        Func<string, string, string> localize);
}

// Owned by one campaign behavior, deliberately neither static nor save data.
internal sealed class SpeechRecentHistory
{
    private const int Capacity = 64;
    private const int RecentPerCategory = 3;
    private readonly Queue<(string Category, string TextId)> _recent = new();

    internal string Pick(string category, IReadOnlyList<string> candidates, Random random)
    {
        if (candidates.Count == 0) throw new ArgumentException("No speech variants.", nameof(candidates));
        var last = _recent.Where(entry => entry.Category == category)
            .Select(entry => entry.TextId).Reverse().Take(RecentPerCategory).ToArray();
        var available = candidates.Where(id => !last.Contains(id)).ToArray();
        return available.Length > 0 ? available[random.Next(available.Length)] : candidates[random.Next(candidates.Count)];
    }

    internal void Remember(string category, string textId)
    {
        _recent.Enqueue((category, textId));
        while (_recent.Count > Capacity) _recent.Dequeue();
    }
}
