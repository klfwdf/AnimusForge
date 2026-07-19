using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Castle-only prisoner trust scale. Ordinary troops use a persisted troop-type value;
/// captured lords continue to use AF personal trust through the live bridge.
/// </summary>
public static class SiegeCastlePrisonerTrustProfile
{
    public const int MinimumTrust = -100;
    public const int MaximumTrust = 100;
    public const int DefaultDefeatedGarrisonTrust = -5;
    public const int TreatTrustDelta = 10;
    public const int ReceiveArmamentsTrustDelta = -5;
    public const int ReleaseTrustDelta = 8;
    public const int SellTrustDelta = -12;
    public const int ForcedDispositionTrustDelta = -15;
    public const int LordRecruitmentAgreementTrustDelta = 5;
    public const int LordSaleTrustDelta = -12;
    public const int VoluntaryRecruitThreshold = 10;
    public const int VoluntaryLaborThreshold = 5;
    public const int VoluntaryInstructorThreshold = 8;

    public static int Clamp(int value) => Math.Min(MaximumTrust, Math.Max(MinimumTrust, value));

    public static int GetVoluntaryThreshold(SiegeCastleActionKind action)
    {
        return action switch
        {
            SiegeCastleActionKind.RecruitPrisonersVoluntary => VoluntaryRecruitThreshold,
            SiegeCastleActionKind.LaborPrisonersVoluntary => VoluntaryLaborThreshold,
            SiegeCastleActionKind.RepairCastleLaborVoluntary => VoluntaryLaborThreshold,
            SiegeCastleActionKind.InstructorPrisonersVoluntary => VoluntaryInstructorThreshold,
            _ => MinimumTrust
        };
    }

    public static bool MeetsVoluntaryThreshold(SiegeCastleActionKind action, int trust)
    {
        return !SiegeCastleActionKindProfile.IsVoluntary(action)
            || Clamp(trust) >= GetVoluntaryThreshold(action);
    }
}
