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
        return Describe(role, isEnglish: false);
    }

    public static string Describe(SiegeCastleActionSpeakerRole role, bool isEnglish)
    {
        if (isEnglish)
        {
            return role switch
            {
                SiegeCastleActionSpeakerRole.AlliedSoldier => "Allied soldier brought into the castle by the player",
                SiegeCastleActionSpeakerRole.RegularPrisoner => "Defeated regular garrison prisoner",
                SiegeCastleActionSpeakerRole.CapturedLord => "Captured lord awaiting disposition",
                _ => "Scene character without castle disposition authority"
            };
        }
        return role switch
        {
            SiegeCastleActionSpeakerRole.AlliedSoldier => "Allied soldier brought into the castle by the player",
            SiegeCastleActionSpeakerRole.RegularPrisoner => "Defeated regular garrison prisoner",
            SiegeCastleActionSpeakerRole.CapturedLord => "Captured lord awaiting disposition",
            _ => "Scene character without castle disposition authority"
        };
    }
}
