using System;
using RichExecutions.Core;

namespace RichExecutions.Campaign;

public sealed class ExecutionHistoryEntry
{
    internal ExecutionHistoryEntry(
        Guid sessionId,
        string victimId,
        string victimName,
        string settlementId,
        string methodId,
        string chargeId,
        ExecutionTone tone,
        LegitimacyTier legitimacyTier,
        ExecutionActor actor,
        float campaignDay)
    {
        SessionId = sessionId;
        VictimId = victimId;
        VictimName = victimName;
        SettlementId = settlementId;
        MethodId = methodId;
        ChargeId = chargeId;
        Tone = tone;
        LegitimacyTier = legitimacyTier;
        Actor = actor;
        CampaignDay = campaignDay;
    }

    public Guid SessionId { get; }
    public string VictimId { get; }
    public string VictimName { get; }
    public string SettlementId { get; }
    public string MethodId { get; }
    public string ChargeId { get; }
    public ExecutionTone Tone { get; }
    public LegitimacyTier LegitimacyTier { get; }
    public ExecutionActor Actor { get; }
    public float CampaignDay { get; }
}
