using RichExecutions.Core;
using TaleWorlds.CampaignSystem;

namespace RichExecutions.UI;

internal sealed class ExecutionPrisonerOption
{
    public ExecutionPrisonerOption(Hero hero, PrisonerSource source)
    {
        Hero = hero;
        Source = source;
    }

    public Hero Hero { get; }
    public PrisonerSource Source { get; }
}

internal sealed class ExecutionEvidencePresentation
{
    public ExecutionEvidencePresentation(
        EvidenceStrength strength,
        string recordText,
        string detailText)
    {
        Strength = strength;
        RecordText = recordText;
        DetailText = detailText;
    }

    public EvidenceStrength Strength { get; }
    public string RecordText { get; }
    public string DetailText { get; }
}

internal sealed class ExecutionCrimeIncidentPresentation
{
    public ExecutionCrimeIncidentPresentation(
        string chargeId,
        string chargeText,
        string recordText,
        string detailText,
        EvidenceStrength strength,
        float campaignDay)
    {
        ChargeId = chargeId;
        ChargeText = chargeText;
        RecordText = recordText;
        DetailText = detailText;
        Strength = strength;
        CampaignDay = campaignDay;
    }

    public string ChargeId { get; }
    public string ChargeText { get; }
    public string RecordText { get; }
    public string DetailText { get; }
    public EvidenceStrength Strength { get; }
    public float CampaignDay { get; }
}

internal sealed class ExecutionJudgementSelection
{
    public ExecutionJudgementSelection(
        Hero victim,
        PrisonerSource source,
        ExecutionMethodDefinition method,
        ExecutionChargeDefinition charge,
        ExecutionTone tone)
    {
        Victim = victim;
        Source = source;
        Method = method;
        Charge = charge;
        Tone = tone;
    }

    public Hero Victim { get; }
    public PrisonerSource Source { get; }
    public ExecutionMethodDefinition Method { get; }
    public ExecutionChargeDefinition Charge { get; }
    public ExecutionTone Tone { get; }
}
