namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Role gate used by castle action postprocessing.
/// </summary>
public enum SiegeCastleActionSpeakerRole
{
    Unknown = 0,
    AlliedSoldier = 1,
    RegularPrisoner = 2,
    CapturedLord = 3
}

public static class SiegeCastleActionSpeakerRoleProfile
{
    public static SiegeCastleActionSpeakerRole Resolve(bool alliedSoldier, bool prisoner, bool lord)
    {
        if (prisoner && lord)
        {
            return SiegeCastleActionSpeakerRole.CapturedLord;
        }

        if (prisoner)
        {
            return SiegeCastleActionSpeakerRole.RegularPrisoner;
        }

        return alliedSoldier
            ? SiegeCastleActionSpeakerRole.AlliedSoldier
            : SiegeCastleActionSpeakerRole.Unknown;
    }

    public static string Describe(SiegeCastleActionSpeakerRole role)
    {
        return role switch
        {
            SiegeCastleActionSpeakerRole.AlliedSoldier => "玩家带入城堡的己方士兵",
            SiegeCastleActionSpeakerRole.RegularPrisoner => "守城战败的普通战俘士兵",
            SiegeCastleActionSpeakerRole.CapturedLord => "等待处置的被俘领主",
            _ => "无城堡处置权限的现场角色"
        };
    }
}
