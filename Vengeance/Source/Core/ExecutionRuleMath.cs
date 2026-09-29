using System;

namespace RichExecutions.Core;

public readonly struct LegitimacyInput
{
    public LegitimacyInput(
        VenueAuthority venueAuthority,
        VictimPoliticalStatus victimStatus,
        EvidenceStrength evidence,
        ExecutionTone tone,
        string methodId,
        bool victimIsNoble)
    {
        VenueAuthority = venueAuthority;
        VictimStatus = victimStatus;
        Evidence = evidence;
        Tone = tone;
        MethodId = methodId ?? string.Empty;
        VictimIsNoble = victimIsNoble;
    }

    public VenueAuthority VenueAuthority { get; }
    public VictimPoliticalStatus VictimStatus { get; }
    public EvidenceStrength Evidence { get; }
    public ExecutionTone Tone { get; }
    public string MethodId { get; }
    public bool VictimIsNoble { get; }
}

public readonly struct ConsequenceDeltas
{
    public ConsequenceDeltas(
        float security,
        float loyalty,
        float influence,
        int honorXp,
        int mercyXp,
        int localRelation)
    {
        Security = security;
        Loyalty = loyalty;
        Influence = influence;
        HonorXp = honorXp;
        MercyXp = mercyXp;
        LocalRelation = localRelation;
    }

    public float Security { get; }
    public float Loyalty { get; }
    public float Influence { get; }
    public int HonorXp { get; }
    public int MercyXp { get; }
    public int LocalRelation { get; }
}

public readonly struct EvidenceMatch
{
    public EvidenceMatch(string victimId, string chargeId, EvidenceStrength strength)
    {
        VictimId = victimId ?? string.Empty;
        ChargeId = chargeId ?? string.Empty;
        Strength = strength;
    }

    public string VictimId { get; }
    public string ChargeId { get; }
    public EvidenceStrength Strength { get; }
}

public static class ExecutionRuleMath
{
    public static int CalculateLegitimacyScore(in LegitimacyInput input)
    {
        var score = input.VenueAuthority switch
        {
            VenueAuthority.OwnTown => 2,
            VenueAuthority.AlliedTown => 1,
            _ => 0
        };

        score += input.VictimStatus switch
        {
            VictimPoliticalStatus.BanditOrRebel => 2,
            VictimPoliticalStatus.WarEnemy => 1,
            VictimPoliticalStatus.Neutral => -2,
            VictimPoliticalStatus.SameKingdom => -4,
            _ => 0
        };

        score += input.Evidence switch
        {
            EvidenceStrength.Strong => 3,
            EvidenceStrength.Circumstantial => 1,
            EvidenceStrength.None => -3,
            _ => -3
        };

        score += input.Tone switch
        {
            ExecutionTone.Judicial => 1,
            ExecutionTone.Spectacle => 0,
            ExecutionTone.Terror => -2,
            _ => 0
        };

        score += CalculateMethodLegitimacy(input.MethodId, input.VictimIsNoble);
        return score;
    }

    public static int CalculateMethodLegitimacy(string methodId, bool victimIsNoble)
    {
        return ExecutionMethodRules.TryGet(methodId, out var rule)
            ? victimIsNoble ? rule.NobleLegitimacy : rule.CommonerLegitimacy
            : 0;
    }

    public static LegitimacyTier GetLegitimacyTier(int score)
    {
        if (score >= 5)
        {
            return LegitimacyTier.Legal;
        }

        return score >= 1 ? LegitimacyTier.Disputed : LegitimacyTier.Illegal;
    }

    public static int GetInfluenceCost(VenueAuthority authority, bool playerIsRuler)
    {
        if (authority == VenueAuthority.OwnTown)
        {
            return 0;
        }

        if (authority == VenueAuthority.AlliedTown)
        {
            return playerIsRuler ? 10 : 30;
        }

        return int.MaxValue;
    }

    public static ConsequenceDeltas CalculateConsequences(
        LegitimacyTier tier,
        ExecutionTone tone,
        string methodId,
        int influenceCost)
    {
        float security;
        float loyalty;
        float influence;
        int honorXp;
        int localRelation;

        switch (tier)
        {
            case LegitimacyTier.Legal:
                security = 3f;
                loyalty = 2f;
                influence = 10f;
                honorXp = 100;
                localRelation = 1;
                break;
            case LegitimacyTier.Disputed:
                security = 2f;
                loyalty = -1f;
                influence = 0f;
                honorXp = -50;
                // Negative execution relations are owned exclusively by the
                // kingdom-scoped vanilla relation model during campaign commit.
                // Do not stack a second local penalty on top of the requested
                // half-strength vanilla values.
                localRelation = 0;
                break;
            default:
                security = 4f;
                loyalty = -5f;
                influence = -15f;
                honorXp = -150;
                localRelation = 0;
                break;
        }

        var mercyXp = 0;
        switch (tone)
        {
            case ExecutionTone.Judicial:
                loyalty += 1f;
                honorXp += 50;
                break;
            case ExecutionTone.Spectacle:
                security += 1f;
                loyalty -= 1f;
                influence += 5f;
                break;
            case ExecutionTone.Terror:
                security += 2f;
                loyalty -= 3f;
                influence -= 10f;
                honorXp -= 100;
                mercyXp -= 100;
                localRelation = Math.Max(0, localRelation - 1);
                break;
        }

        if (ExecutionMethodRules.TryGet(methodId, out var methodRule))
        {
            security += methodRule.SecurityDelta;
            loyalty += methodRule.LoyaltyDelta;
            mercyXp += methodRule.MercyXpDelta;
        }

        influence -= Math.Max(0, influenceCost);
        return new ConsequenceDeltas(security, loyalty, influence, honorXp, mercyXp, localRelation);
    }

    public static EvidenceStrength FindStrongestEvidence(
        System.Collections.Generic.IEnumerable<EvidenceMatch> matches,
        string victimId,
        string chargeId)
    {
        var strongest = EvidenceStrength.None;
        foreach (var match in matches)
        {
            if (!string.Equals(match.VictimId, victimId, StringComparison.Ordinal) ||
                !string.Equals(match.ChargeId, chargeId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (match.Strength > strongest)
            {
                strongest = match.Strength;
            }
        }

        return strongest;
    }

    public static CrowdReactionType GetCrowdReaction(
        LegitimacyTier tier,
        ExecutionTone tone,
        string methodId)
    {
        if (tier == LegitimacyTier.Illegal || tone == ExecutionTone.Terror ||
            (ExecutionMethodRules.TryGet(methodId, out var rule) && rule.ForcesPanic))
        {
            return CrowdReactionType.Panic;
        }

        return tier == LegitimacyTier.Legal ? CrowdReactionType.Cheer : CrowdReactionType.Divided;
    }
}
