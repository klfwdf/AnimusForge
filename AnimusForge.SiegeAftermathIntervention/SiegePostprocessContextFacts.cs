namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Runtime facts collected by the AF adapter for GCCZ postprocess context text.
/// </summary>
public readonly struct SiegePostprocessContextFacts
{
    public SiegePostprocessContextFacts(
        string settlementName,
        string currentOutcome,
        bool destructiveAllowed,
        string speakerName,
        string speakerIdentity,
        TownDialogueRole dialogueRole,
        int targetAgentIndex,
        bool replyIsDirectPlayerResponse,
        string sharedReliefPoolDescription,
        string civilianGatherContext,
        string interventionMemoryContext)
    {
        SettlementName = settlementName ?? string.Empty;
        CurrentOutcome = currentOutcome ?? string.Empty;
        DestructiveAllowed = destructiveAllowed;
        SpeakerName = speakerName ?? string.Empty;
        SpeakerIdentity = speakerIdentity ?? string.Empty;
        DialogueRole = TownDialogueRoleClassifier.NormalizeForRuntime(dialogueRole);
        TargetAgentIndex = targetAgentIndex;
        ReplyIsDirectPlayerResponse = replyIsDirectPlayerResponse;
        SharedReliefPoolDescription = sharedReliefPoolDescription ?? string.Empty;
        CivilianGatherContext = civilianGatherContext ?? string.Empty;
        InterventionMemoryContext = interventionMemoryContext ?? string.Empty;
    }

    public string SettlementName { get; }

    public string CurrentOutcome { get; }

    public bool DestructiveAllowed { get; }

    public string SpeakerName { get; }

    public string SpeakerIdentity { get; }

    public TownDialogueRole DialogueRole { get; }

    public int TargetAgentIndex { get; }

    public bool ReplyIsDirectPlayerResponse { get; }

    public string SharedReliefPoolDescription { get; }

    public string CivilianGatherContext { get; }

    public string InterventionMemoryContext { get; }
}
