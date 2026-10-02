using System;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// Reads only the two named kingdoms and their model values on demand.
internal readonly struct DiplomacyTributePowerSource : IDiplomacyTributePowerSource
{
    private readonly Kingdom _payer;
    private readonly Kingdom _receiver;

    internal DiplomacyTributePowerSource(Kingdom payer, Kingdom receiver)
    {
        _payer = payer;
        _receiver = receiver;
    }

    public bool TryCapture(out DiplomacyTributePowerSnapshot snapshot)
    {
        snapshot = default;
        try
        {
            Kingdom payer = _payer;
            Kingdom receiver = _receiver;
            if (payer == null || receiver == null || payer == receiver) return false;
            var model = Campaign.Current?.Models?.DiplomacyModel;
            if (model == null) return false;
            float scorePayer = model.GetScoreOfDeclaringPeace(payer, receiver);
            float scoreReceiver = model.GetScoreOfDeclaringPeace(receiver, payer);
            float settlementValue = model.GetValueOfSettlementsForFaction(payer);
            float receiverDecisionThreshold = model.GetDecisionMakingThreshold(receiver);
            float payerWarProgress = model.GetWarProgressScore(payer, receiver).ResultNumber;
            float receiverWarProgress = model.GetWarProgressScore(receiver, payer).ResultNumber;
            float payerFiefProsperity = payer.Fiefs.Sum(x => x.Prosperity);
            snapshot = new DiplomacyTributePowerSnapshot(
                scorePayer, scoreReceiver, receiverDecisionThreshold, settlementValue,
                payerWarProgress, receiverWarProgress, payerFiefProsperity);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("DiplomacyBehavior", "[TributePower] context failed: " + ex.Message);
            return false;
        }
    }
}
