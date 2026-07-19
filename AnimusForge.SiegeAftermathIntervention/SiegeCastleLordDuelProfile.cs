using System.Text;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free contract for the castle captive-lord duel. Bannerlord mission
/// movement, teams, weapons and damage handling remain in the fused runtime adapter.
/// </summary>
public static class SiegeCastleLordDuelProfile
{
    public const int MinimumWeaponTier = 4;

    public const int MaximumWeaponTier = 6;

    public const float WeaponForwardDistance = 20f;

    public const float LoadoutItemSpacing = 0.75f;

    public const float AudienceBaseRadius = 12.5f;

    public const float AudienceRingSpacing = 1.5f;

    public const float AudienceSpacing = 1.25f;

    public const float DuelHealthFloor = 1f;

    public const string RuntimeSource = "castle_captive_lord_duel";

    public static bool IsEligibleWeapon(
        int tier,
        bool isOneHanded,
        bool isTwoHanded,
        bool isMeleeWeapon,
        bool isMerchandise)
    {
        return tier >= MinimumWeaponTier
            && tier <= MaximumWeaponTier
            && (isOneHanded || isTwoHanded)
            && isMeleeWeapon
            && isMerchandise;
    }

    public static bool IsEligibleShield(int tier, bool isShield, bool isMerchandise)
    {
        return tier >= MinimumWeaponTier
            && tier <= MaximumWeaponTier
            && isShield
            && isMerchandise;
    }

    public static string BuildEquipmentFact(
        bool playerIsMounted,
        bool playerCarriesRangedWeapon,
        bool playerWieldsRangedWeapon)
    {
        return "玩家当前骑乘=" + YesNo(playerIsMounted)
            + "；携带远程武器=" + YesNo(playerCarriesRangedWeapon)
            + "；当前手持远程武器=" + YesNo(playerWieldsRangedWeapon) + "。";
    }

    public static string BuildResultFact(
        string castleName,
        string playerName,
        string lordName,
        bool playerWon,
        bool playerWasMountedWhenAccepted,
        bool playerCarriedRangedWeaponWhenAccepted,
        bool playerWieldedRangedWeaponWhenAccepted,
        bool playerMountedDuringDuel,
        bool playerUsedRangedWeapon)
    {
        var sb = new StringBuilder();
        sb.Append("【城堡俘虏领主决斗结果·既成事实】地点=")
            .Append(string.IsNullOrWhiteSpace(castleName) ? SiegeCastleRuntimePromptProfile.DefaultCastleName : castleName.Trim())
            .Append("；玩家=")
            .Append(string.IsNullOrWhiteSpace(playerName) ? SiegeCastleRuntimePromptProfile.DefaultPlayerName : playerName.Trim())
            .Append("；被俘领主=")
            .Append(string.IsNullOrWhiteSpace(lordName) ? "该被俘领主" : lordName.Trim())
            .Append("；胜者=")
            .Append(playerWon ? "玩家" : "被俘领主")
            .Append("。双方都没有死亡，决斗已经结束，被俘领主仍是俘虏；本事实不会自动释放、收编、贩卖或处决任何人。")
            .Append("接受决斗时玩家骑乘=").Append(YesNo(playerWasMountedWhenAccepted))
            .Append("；接受决斗时玩家携带远程武器=").Append(YesNo(playerCarriedRangedWeaponWhenAccepted))
            .Append("；接受决斗时玩家手持远程武器=").Append(YesNo(playerWieldedRangedWeaponWhenAccepted))
            .Append("；决斗中玩家骑乘=").Append(YesNo(playerMountedDuringDuel))
            .Append("；决斗中玩家使用远程武器=").Append(YesNo(playerUsedRangedWeapon)).Append("。");

        if (playerWon)
        {
            sb.Append("请让该领主依据性格、此前谈判、约定以及是否认为规则被破坏来回应败北；不得擅自兑现释放或其他最终处置。");
        }
        else
        {
            sb.Append("该领主已经获胜并放下决斗武器。请让其依据性格、此前谈判和约定回应，可要求玩家信守承诺；不得自行离开场景或解除俘虏状态。");
        }
        return sb.ToString();
    }

    private static string YesNo(bool value) => value ? "是" : "否";
}
