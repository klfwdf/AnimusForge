using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free runtime parameters and source codes for GCCZ civilian-gather interactions.
/// AF adapters still own live mission-agent selection, messenger speech triggering, formation control, and side effects.
/// </summary>
public static class SiegeCivilianGatherInteractionProfile
{
    public const float SpeechRallySettleTolerance = 0.8f;

    public const float TalkMinSeconds = 1.0f;

    public const float TalkMaxSeconds = 3.0f;

    public const float FallbackSeconds = 75.0f;

    public const float ApproachDistance = 3.2f;

    public const float FollowRefreshSeconds = 1.25f;

    public const float FormationSettleDistance = 5.5f;

    public const float SoldierMessengerRatio = 0.20f;

    public const float MessengerMoveSpeedLimit = 1.9f;

    public const float FormationControlInitialDelaySeconds = 0.8f;

    public const float FormationControlBatchIntervalSeconds = 0.12f;

    public const int FormationControlBatchSize = 8;

    // FormationClass.Cavalry, displayed as command group 3.
    public const int NativeCommandFormationClassIndex = 2;

    public const string TargetWaitSource = "gather_target_wait";

    public const string MessengerMoveSource = "gather_messenger_move";

    public const string MessengerSpeechSource = "gather_messenger_speech";

    public const string FollowPrepareSource = "gather_follow_prepare_once";

    public const string InvalidOrAlreadyFollowerReleaseSource = "gather_interaction_invalid_or_target_already_c";

    public const string FakeTalkFollowerSource = "gather_fake_talk";

    public const string InteractionTimeoutReleaseSource = "gather_interaction_timeout";

    public const string FallbackFollowerSource = "gather_120s_fallback";

    public const string FallbackElapsedFormationSource = "gather_120s_elapsed";

    public const string AllGatheredAndSettledFormationSource = "all_civilians_gathered_and_settled";

    public const string TargetBecameFollowerReleaseSource = "gather_target_became_c";

    public const string GatherMarkSourcePrefix = "gather_mark:";

    public const string GatherSeedSourcePrefix = "gather_seed:";

    public const string GatherFallbackSourcePrefix = "gather_fallback:";

    public const string GatherMessengerReturnSourcePrefix = "gather_messenger_return:";

    public const string GatherSoldierReturnSourcePrefix = "gather_soldier_return:";

    public const string CommandControlRepeatSoldierReleaseSource = "command_control_repeat_soldier_gather";

    public const string SoldierSeedMessengerSource = "soldier_seed_20_percent";

    public const string SoldierMessengerSource = "soldier_20_percent";

    public const string FormationQueueSourcePrefix = "queue:";

    public const string FormationControlBeginSource = "civilian_formation_control_begin";

    public const string FormationControlBatchSource = "formation_control_batch";

    public const string FormationReadyFollowSource = "civilian_formation_ready_follow";

    public const string FormationReadyOrderControllerSource = "civilian_formation_ready";

    public const string UnavailableSourceSuffix = "N/A";

    public static string BuildGatherMarkSource(string reason)
    {
        return GatherMarkSourcePrefix + (reason ?? UnavailableSourceSuffix);
    }

    public static string BuildGatherSeedSource(string reason)
    {
        return GatherSeedSourcePrefix + (reason ?? UnavailableSourceSuffix);
    }

    public static string BuildGatherFallbackSource(string reason)
    {
        return GatherFallbackSourcePrefix + (reason ?? UnavailableSourceSuffix);
    }

    public static string BuildGatherMessengerReturnSource(string reason)
    {
        return GatherMessengerReturnSourcePrefix + (reason ?? UnavailableSourceSuffix);
    }

    public static string BuildGatherSoldierReturnSource(string reason)
    {
        return GatherSoldierReturnSourcePrefix + (reason ?? UnavailableSourceSuffix);
    }

    public static string BuildFormationQueueSource(string reason)
    {
        return FormationQueueSourcePrefix + (reason ?? UnavailableSourceSuffix);
    }

    public static bool IsExplicitSemanticGatherSource(string source)
    {
        return string.Equals(source, SiegePostprocessActionEffectProfile.GatherCiviliansSource, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldReleaseSoldiersForCommandControlRepeat(bool formationControlPending, bool formationControlComplete, bool seedIsSoldier, string source)
    {
        if (!seedIsSoldier || (!formationControlPending && !formationControlComplete))
        {
            return false;
        }

        return IsExplicitSemanticGatherSource(source);
    }
}
