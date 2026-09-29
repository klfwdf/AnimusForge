using RichExecutions.Core;

namespace RichExecutions.Campaign;

public sealed class EvidenceRecord
{
    internal EvidenceRecord(
        string victimId,
        string chargeId,
        EvidenceStrength strength,
        string settlementId,
        float campaignDay)
    {
        VictimId = victimId;
        ChargeId = chargeId;
        Strength = strength;
        SettlementId = settlementId;
        CampaignDay = campaignDay;
    }

    public string VictimId { get; }
    public string ChargeId { get; }
    public EvidenceStrength Strength { get; }
    public string SettlementId { get; }
    public float CampaignDay { get; }
}
