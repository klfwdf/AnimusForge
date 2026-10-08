using System;

namespace AnimusForge;

internal readonly struct PatienceResponseInput
{
    internal readonly string Key;
    internal readonly int Maximum, Relation, PrivateLove;
    internal readonly bool HasHero;
    internal PatienceResponseInput(string key, int maximum, int relation, int privateLove, bool hasHero)
    { Key = key; Maximum = maximum; Relation = relation; PrivateLove = privateLove; HasHero = hasHero; }
}

internal readonly struct PatienceResponseResult
{
    internal readonly PatienceMood Mood;
    internal readonly PatienceChange Change;
    internal readonly int RelationDelta, PrivateLoveDelta;
    internal readonly bool Applied, Correction;
    internal PatienceResponseResult(PatienceMood mood, PatienceChange change, int relationDelta, int loveDelta, bool applied, bool correction)
    { Mood = mood; Change = change; RelationDelta = relationDelta; PrivateLoveDelta = loveDelta; Applied = applied; Correction = correction; }
}

// Response-frequency orchestration only; the original PatienceOwner remains the sole state writer.
internal sealed class PatienceResponseApplication
{
    private readonly Func<string, int, float, PatienceMood, PatienceChange> _apply, _correct;
    internal PatienceResponseApplication(Func<string, int, float, PatienceMood, PatienceChange> apply,
        Func<string, int, float, PatienceMood, PatienceChange> correct)
    { _apply = apply ?? throw new ArgumentNullException(nameof(apply)); _correct = correct ?? throw new ArgumentNullException(nameof(correct)); }

    internal PatienceResponseResult Apply(PatienceResponseInput input, ref string text, float day, bool correction,
        Action<PatienceMood> royalLoyalty, Action<int, bool> relation, Action<int, bool> privateLove)
    {
        PatienceMood mood = PatienceRules.ExtractMoodAndStripTag(ref text);
        if (correction && mood == PatienceMood.Neutral)
            return new PatienceResponseResult(mood, default, 0, 0, false, true);
        // Original order: corrected positive mood can affect loyalty even for an unnamed empty key.
        if (correction) royalLoyalty?.Invoke(mood);
        if (!input.HasHero && string.IsNullOrWhiteSpace(input.Key))
            return new PatienceResponseResult(mood, default, 0, 0, false, correction);
        PatienceChange change = (correction ? _correct : _apply)(input.Key, input.Maximum, day, mood);
        int relationDelta = input.HasHero ? PatienceRules.HeroRelationEffect(mood, input.Relation, change.Before, correction) : 0;
        int loveDelta = input.HasHero ? PatienceRules.ComputePrivateLoveDelta(mood) : 0;
        if (relationDelta != 0) relation?.Invoke(relationDelta, correction);
        // Deliberately not caught: legacy private-love failure must not masquerade as observed success.
        if (loveDelta != 0) privateLove?.Invoke(loveDelta, correction);
        return new PatienceResponseResult(mood, change, relationDelta, loveDelta, true, correction);
    }
}
