using System;

namespace AnimusForge.SiegeAftermathIntervention;

public enum TownCivilianFormationControlPhase
{
    Inactive = 0,
    Pending = 1,
    Complete = 2,
}

/// <summary>
/// Owns side-effect-free GCCZ scene control transitions and retry timing for one mission.
/// </summary>
public sealed class TownSceneControlState
{
    private bool _formationReadyMessageClaimed;

    public bool IsCivilianSpeechRallyActive { get; private set; }

    public bool IsCivilianGatherPropagationActive { get; private set; }

    public TownCivilianFormationControlPhase FormationControlPhase { get; private set; }

    public bool IsCivilianFormationControlPending => FormationControlPhase == TownCivilianFormationControlPhase.Pending;

    public bool IsCivilianFormationControlComplete => FormationControlPhase == TownCivilianFormationControlPhase.Complete;

    public bool IsSoldierDefaultFollowOrderIssued { get; private set; }

    public bool IsPlayerOrderControllerPrimed { get; private set; }

    public bool IsCivilianOrderControllerPrimed { get; private set; }

    public bool IsCivilianAssemblyPointReady { get; private set; }

    public float CivilianGatherStartedAt { get; private set; } = -1f;

    public float NextCivilianGatherTickTime { get; private set; }

    public int CivilianGatherMessengerSpeechCount { get; private set; }

    public float CivilianFormationControlNotBeforeTime { get; private set; } = -1f;

    public float NextCivilianFormationControlBatchTime { get; private set; }

    public float NextPlayerOrderControllerPrimeTime { get; private set; }

    public bool IsCivilianGatherStartAvailable => !IsCivilianSpeechRallyActive
        && !IsCivilianGatherPropagationActive
        && FormationControlPhase == TownCivilianFormationControlPhase.Inactive;

    public bool TryStartCivilianGather(float missionTime)
    {
        if (!IsCivilianGatherStartAvailable)
        {
            return false;
        }

        IsCivilianSpeechRallyActive = true;
        IsCivilianGatherPropagationActive = true;
        FormationControlPhase = TownCivilianFormationControlPhase.Inactive;
        _formationReadyMessageClaimed = false;
        CivilianGatherStartedAt = missionTime;
        NextCivilianGatherTickTime = 0f;
        CivilianGatherMessengerSpeechCount = 0;
        CivilianFormationControlNotBeforeTime = -1f;
        NextCivilianFormationControlBatchTime = 0f;
        return true;
    }

    public bool HasCivilianGatherFallbackElapsed(float missionTime, float fallbackSeconds)
    {
        return IsCivilianGatherPropagationActive
            && CivilianGatherStartedAt >= 0f
            && missionTime - CivilianGatherStartedAt >= Math.Max(0f, fallbackSeconds);
    }

    public bool TryScheduleCivilianGatherTick(float missionTime, float intervalSeconds)
    {
        if (!IsCivilianGatherPropagationActive || missionTime < NextCivilianGatherTickTime)
        {
            return false;
        }

        NextCivilianGatherTickTime = missionTime + Math.Max(0f, intervalSeconds);
        return true;
    }

    public void StopCivilianGatherPropagation()
    {
        IsCivilianGatherPropagationActive = false;
    }

    public void StopCivilianGatherScript()
    {
        IsCivilianSpeechRallyActive = false;
        IsCivilianGatherPropagationActive = false;
    }

    public int RecordCivilianGatherMessengerSpeech()
    {
        CivilianGatherMessengerSpeechCount++;
        return CivilianGatherMessengerSpeechCount;
    }

    public bool TryQueueCivilianFormationControl(float missionTime, float initialDelaySeconds)
    {
        if (FormationControlPhase == TownCivilianFormationControlPhase.Complete)
        {
            return false;
        }

        if (FormationControlPhase == TownCivilianFormationControlPhase.Pending)
        {
            return false;
        }

        FormationControlPhase = TownCivilianFormationControlPhase.Pending;
        CivilianFormationControlNotBeforeTime = missionTime + Math.Max(0f, initialDelaySeconds);
        NextCivilianFormationControlBatchTime = CivilianFormationControlNotBeforeTime;
        return true;
    }

    public bool TryScheduleCivilianFormationControlBatch(float missionTime, float intervalSeconds)
    {
        if (FormationControlPhase != TownCivilianFormationControlPhase.Pending
            || missionTime < CivilianFormationControlNotBeforeTime
            || missionTime < NextCivilianFormationControlBatchTime)
        {
            return false;
        }

        NextCivilianFormationControlBatchTime = missionTime + Math.Max(0f, intervalSeconds);
        return true;
    }

    public bool CompleteCivilianFormationControl()
    {
        if (FormationControlPhase != TownCivilianFormationControlPhase.Pending)
        {
            return false;
        }

        FormationControlPhase = TownCivilianFormationControlPhase.Complete;
        return true;
    }

    public bool TryClaimFormationReadyMessage()
    {
        if (!IsCivilianFormationControlComplete || _formationReadyMessageClaimed)
        {
            return false;
        }

        _formationReadyMessageClaimed = true;
        return true;
    }

    public void RecordSoldierDefaultFollowOrderResult(bool issued)
    {
        IsSoldierDefaultFollowOrderIssued = IsSoldierDefaultFollowOrderIssued || issued;
    }

    public bool CanPrimePlayerOrderController(float missionTime, bool force)
    {
        return force || (!IsPlayerOrderControllerPrimed && missionTime >= NextPlayerOrderControllerPrimeTime);
    }

    public void ScheduleNextPlayerOrderControllerPrime(float missionTime, float intervalSeconds)
    {
        NextPlayerOrderControllerPrimeTime = missionTime + Math.Max(0f, intervalSeconds);
    }

    public void MarkPlayerOrderControllerPrimed()
    {
        IsPlayerOrderControllerPrimed = true;
    }

    public void SetCivilianOrderControllerPrimed(bool primed)
    {
        IsCivilianOrderControllerPrimed = primed;
    }

    public void MarkCivilianAssemblyPointReady()
    {
        IsCivilianAssemblyPointReady = true;
    }

    public void ResetCivilianAssemblyPoint()
    {
        IsCivilianAssemblyPointReady = false;
    }

    public void Reset()
    {
        IsCivilianSpeechRallyActive = false;
        IsCivilianGatherPropagationActive = false;
        FormationControlPhase = TownCivilianFormationControlPhase.Inactive;
        _formationReadyMessageClaimed = false;
        IsSoldierDefaultFollowOrderIssued = false;
        IsPlayerOrderControllerPrimed = false;
        IsCivilianOrderControllerPrimed = false;
        IsCivilianAssemblyPointReady = false;
        CivilianGatherStartedAt = -1f;
        NextCivilianGatherTickTime = 0f;
        CivilianGatherMessengerSpeechCount = 0;
        CivilianFormationControlNotBeforeTime = -1f;
        NextCivilianFormationControlBatchTime = 0f;
        NextPlayerOrderControllerPrimeTime = 0f;
    }

}
