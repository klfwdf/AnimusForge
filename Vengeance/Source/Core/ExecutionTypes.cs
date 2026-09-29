using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace RichExecutions.Core;

// A prisoner the in-scene deployment studio may choose. Carries the hero and the
// custody it came from so the rebuilt request keeps the correct source.
public sealed class ExecutionStudioPrisonerOption
{
    public ExecutionStudioPrisonerOption(Hero hero, PrisonerSource source)
    {
        Hero = hero ?? throw new ArgumentNullException(nameof(hero));
        Source = source;
    }

    public Hero Hero { get; }
    public PrisonerSource Source { get; }
}

public sealed class ExecutionMethodDefinition
{
    private readonly string _nameText;
    private readonly string _descriptionText;

    public ExecutionMethodDefinition(
        string stringId,
        string nameText,
        string descriptionText,
        string sceneActId)
    {
        if (string.IsNullOrWhiteSpace(stringId))
        {
            throw new ArgumentException("A method id is required.", nameof(stringId));
        }

        StringId = stringId;
        _nameText = nameText ?? throw new ArgumentNullException(nameof(nameText));
        _descriptionText = descriptionText ?? throw new ArgumentNullException(nameof(descriptionText));
        SceneActId = sceneActId ?? throw new ArgumentNullException(nameof(sceneActId));
    }

    public string StringId { get; }
    public string SceneActId { get; }
    public TextObject GetName() => new(_nameText);
    public TextObject GetDescription() => new(_descriptionText);
    public override string ToString() => GetName().ToString();
}

public sealed class ExecutionChargeDefinition
{
    private readonly string _nameText;
    private readonly string _descriptionText;

    public ExecutionChargeDefinition(string stringId, string nameText, string descriptionText)
    {
        if (string.IsNullOrWhiteSpace(stringId))
        {
            throw new ArgumentException("A charge id is required.", nameof(stringId));
        }

        StringId = stringId;
        _nameText = nameText ?? throw new ArgumentNullException(nameof(nameText));
        _descriptionText = descriptionText ?? throw new ArgumentNullException(nameof(descriptionText));
    }

    public string StringId { get; }
    public TextObject GetName() => new(_nameText);
    public TextObject GetDescription() => new(_descriptionText);
    public override string ToString() => GetName().ToString();
}

public interface IExecutionSceneAct
{
    string StringId { get; }
    string VictimIdleAction { get; }
    string ExecutionerIdleAction { get; }
    string ExecutionAction { get; }
    string DeathAction { get; }
    float LethalProgress { get; }
    float MaximumActionSeconds { get; }
}

public sealed class ExecutionRequest
{
    public ExecutionRequest(
        Hero victim,
        Hero executor,
        Settlement venue,
        ExecutionMethodDefinition method,
        ExecutionChargeDefinition charge,
        ExecutionTone tone,
        EvidenceStrength evidence,
        LegitimacyTier legitimacyTier,
        int legitimacyScore,
        int influenceCost,
        PrisonerSource prisonerSource,
        VenueAuthority venueAuthority,
        VictimPoliticalStatus victimStatus,
        bool victimIsNoble,
        Guid? sessionId = null)
        : this(
            victim,
            executor,
            venue,
            method,
            charge,
            tone,
            evidence,
            legitimacyTier,
            legitimacyScore,
            influenceCost,
            prisonerSource,
            venueAuthority,
            victimStatus,
            victimIsNoble,
            ExecutionSceneMode.Automatic,
            null,
            sessionId)
    {
    }

    public ExecutionRequest(
        Hero victim,
        Hero executor,
        Settlement venue,
        ExecutionMethodDefinition method,
        ExecutionChargeDefinition charge,
        ExecutionTone tone,
        EvidenceStrength evidence,
        LegitimacyTier legitimacyTier,
        int legitimacyScore,
        int influenceCost,
        PrisonerSource prisonerSource,
        VenueAuthority venueAuthority,
        VictimPoliticalStatus victimStatus,
        bool victimIsNoble,
        ExecutionSceneMode sceneMode,
        Guid? customPresetId = null,
        Guid? sessionId = null)
    {
        Victim = victim ?? throw new ArgumentNullException(nameof(victim));
        Executor = executor ?? throw new ArgumentNullException(nameof(executor));
        Venue = venue ?? throw new ArgumentNullException(nameof(venue));
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Charge = charge ?? throw new ArgumentNullException(nameof(charge));
        Tone = tone;
        Evidence = evidence;
        LegitimacyTier = legitimacyTier;
        LegitimacyScore = legitimacyScore;
        InfluenceCost = Math.Max(0, influenceCost);
        PrisonerSource = prisonerSource;
        VenueAuthority = venueAuthority;
        VictimStatus = victimStatus;
        VictimIsNoble = victimIsNoble;
        SceneMode = sceneMode;
        CustomPresetId = customPresetId;
        SessionId = sessionId ?? Guid.NewGuid();
    }

    public Guid SessionId { get; }
    public Hero Victim { get; }
    public Hero Executor { get; }
    public Settlement Venue { get; }
    public ExecutionMethodDefinition Method { get; }
    public ExecutionChargeDefinition Charge { get; }
    public ExecutionTone Tone { get; }
    public EvidenceStrength Evidence { get; }
    public LegitimacyTier LegitimacyTier { get; }
    public int LegitimacyScore { get; }
    public int InfluenceCost { get; }
    public PrisonerSource PrisonerSource { get; }
    public VenueAuthority VenueAuthority { get; }
    public VictimPoliticalStatus VictimStatus { get; }
    public bool VictimIsNoble { get; }
    public ExecutionSceneMode SceneMode { get; }
    public Guid? CustomPresetId { get; }
}

public sealed class ExecutionValidationResult
{
    private ExecutionValidationResult(bool isValid, ExecutionFailureReason reason, TextObject message)
    {
        IsValid = isValid;
        Reason = reason;
        Message = message;
    }

    public bool IsValid { get; }
    public ExecutionFailureReason Reason { get; }
    public TextObject Message { get; }

    public static ExecutionValidationResult Valid() =>
        new(true, ExecutionFailureReason.None, TextObject.GetEmpty());

    public static ExecutionValidationResult Invalid(ExecutionFailureReason reason, TextObject message) =>
        new(false, reason, message ?? TextObject.GetEmpty());
}

public sealed class ExecutionOutcome
{
    private ExecutionOutcome(
        ExecutionRequest request,
        ExecutionActor actor,
        bool success,
        bool deathCommitted,
        ExecutionFailureReason failureReason,
        TextObject message,
        ConsequenceDeltas deltas)
    {
        Request = request;
        Actor = actor;
        Success = success;
        DeathCommitted = deathCommitted;
        FailureReason = failureReason;
        Message = message;
        SecurityDelta = deltas.Security;
        LoyaltyDelta = deltas.Loyalty;
        InfluenceDelta = deltas.Influence;
        HonorXpDelta = deltas.HonorXp;
        MercyXpDelta = deltas.MercyXp;
        LocalRelationDelta = deltas.LocalRelation;
    }

    public ExecutionRequest Request { get; }
    public ExecutionActor Actor { get; }
    public bool Success { get; }
    public bool DeathCommitted { get; }
    public ExecutionFailureReason FailureReason { get; }
    public TextObject Message { get; }
    public float SecurityDelta { get; }
    public float LoyaltyDelta { get; }
    public float InfluenceDelta { get; }
    public int HonorXpDelta { get; }
    public int MercyXpDelta { get; }
    public int LocalRelationDelta { get; }

    internal static ExecutionOutcome Succeeded(
        ExecutionRequest request,
        ExecutionActor actor,
        TextObject message,
        ConsequenceDeltas deltas) =>
        new(request, actor, true, true, ExecutionFailureReason.None, message, deltas);

    internal static ExecutionOutcome Failed(
        ExecutionRequest request,
        ExecutionActor actor,
        ExecutionFailureReason reason,
        TextObject message) =>
        new(request, actor, false, false, reason, message, default);
}
