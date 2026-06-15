namespace AnimusForge.SiegeAftermathIntervention;

public sealed class SiegeCivilianGatherContextFacts
{
    public SiegeCivilianGatherContextFacts(
        bool speechRallyActive,
        bool gatherPropagationActive,
        bool formationControlPending,
        bool formationControlComplete,
        int followerCount,
        int readyFormationCount,
        int messengerCount,
        int totalCivilianCount)
    {
        SpeechRallyActive = speechRallyActive;
        GatherPropagationActive = gatherPropagationActive;
        FormationControlPending = formationControlPending;
        FormationControlComplete = formationControlComplete;
        FollowerCount = followerCount;
        ReadyFormationCount = readyFormationCount;
        MessengerCount = messengerCount;
        TotalCivilianCount = totalCivilianCount;
    }

    public bool SpeechRallyActive { get; }

    public bool GatherPropagationActive { get; }

    public bool FormationControlPending { get; }

    public bool FormationControlComplete { get; }

    public int FollowerCount { get; }

    public int ReadyFormationCount { get; }

    public int MessengerCount { get; }

    public int TotalCivilianCount { get; }
}
