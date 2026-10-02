using System;

namespace AnimusForge;

internal readonly struct DiplomacyTributePowerSnapshot
{
    internal DiplomacyTributePowerSnapshot(float scorePayer, float scoreReceiver,
        float receiverDecisionThreshold, float settlementValue, float payerWarProgress,
        float receiverWarProgress, float payerFiefProsperity)
    {
        ScorePayer = scorePayer;
        ScoreReceiver = scoreReceiver;
        ReceiverDecisionThreshold = receiverDecisionThreshold;
        SettlementValue = settlementValue;
        PayerWarProgress = payerWarProgress;
        ReceiverWarProgress = receiverWarProgress;
        PayerFiefProsperity = payerFiefProsperity;
    }
    internal float ScorePayer { get; }
    internal float ScoreReceiver { get; }
    internal float ReceiverDecisionThreshold { get; }
    internal float SettlementValue { get; }
    internal float PayerWarProgress { get; }
    internal float ReceiverWarProgress { get; }
    internal float PayerFiefProsperity { get; }
}

internal interface IDiplomacyTributePowerSource
{
    bool TryCapture(out DiplomacyTributePowerSnapshot snapshot);
}

internal static class DiplomacyTributePowerApplication
{
    internal static bool TryBuild<TSource>(ref TSource source, out AfTributePowerContext context)
        where TSource : struct, IDiplomacyTributePowerSource
    {
        context = default;
        try
        {
            if (!source.TryCapture(out DiplomacyTributePowerSnapshot snapshot)) return false;
            context = Calculate(snapshot);
            return true;
        }
        catch { return false; }
    }

    internal static AfTributePowerContext Calculate(DiplomacyTributePowerSnapshot value)
    {
        float num = value.ScoreReceiver > 0f
            ? value.ScoreReceiver - value.ScorePayer
            : value.ReceiverDecisionThreshold - value.ScoreReceiver;
        float warDiff = Math.Abs(value.PayerWarProgress - value.ReceiverWarProgress);
        float rawRatio = num / (value.SettlementValue + 1f);
        float ratio = rawRatio;
        if (warDiff < 75f)
        {
            ratio = 0.05f;
        }
        else
        {
            ratio /= 2f;
            if (ratio < 0.05f) ratio = 0f;
            else if (ratio < 0.10f) ratio = 0.05f;
            else if (ratio < 0.15f) ratio = 0.10f;
            else ratio = 0.15f;
        }
        int calculatedTribute = (int)(ratio * value.PayerFiefProsperity * 0.35f) / 10 * 10;
        return new AfTributePowerContext(
            value.ScorePayer, value.ScoreReceiver, value.ReceiverDecisionThreshold,
            value.SettlementValue, value.PayerWarProgress, value.ReceiverWarProgress,
            warDiff, rawRatio, ratio, value.PayerFiefProsperity, calculatedTribute);
    }
}
