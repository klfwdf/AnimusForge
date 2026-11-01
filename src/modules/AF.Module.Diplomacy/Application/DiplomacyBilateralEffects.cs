using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

internal static class DiplomacyBilateralEffects
{
    internal static string State(bool applied, string status) => applied ? "applied"
        : status == "UnknownAfterStart" ? "unknown" : status == "PartiallyApplied" ? "partial" : "rejected";

    // Inputs have passed the existing authority and complete-clause admission.
    // Cession is never attempted after failed/unknown/incomplete peace. A failed
    // cession retains confirmed peace without claiming the whole package worked.
    internal static DiplomacyBilateralResult Peace(WorldDiplomacyPeaceTerms requested,
        Func<WorldDiplomacyMakePeaceExecutionReceipt> peace, Func<WorldDiplomacyCessionReceipt> cession)
    {
        var receipt=peace();
        var result=new DiplomacyBilateralResult { State=State(receipt.IsApplied,receipt.Status.ToString()),
            Actor=receipt.PayerKingdomId,Target=receipt.ReceiverKingdomId,Diagnostic=receipt.ErrorCode,
            Terms=new() { TributePayerKingdomId=receipt.PayerKingdomId,TributeReceiverKingdomId=receipt.ReceiverKingdomId,
                DailyTribute=receipt.AppliedDailyTribute,DurationDays=receipt.AppliedDurationDays } };
        if(receipt.IsApplied && !string.IsNullOrEmpty(requested.CessionSettlementId))
        {
            WorldDiplomacyCessionReceipt ceded;
            try { ceded=cession(); }
            catch(Exception ex){ceded=new(true,false,false,ex.GetType().Name);}
            if(ceded.Known && ceded.Applied)
            {
                result.Terms.CessionFromKingdomId=requested.CessionFromKingdomId;
                result.Terms.CessionToKingdomId=requested.CessionToKingdomId;result.Terms.CessionSettlementId=requested.CessionSettlementId;
            }
            else {result.State="partial";result.Diagnostic=ceded.Message;}
        }
        return result;
    }
}
