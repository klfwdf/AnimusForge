using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free count preservation and player-facing wording for castle prisoner disposition.
/// </summary>
public static class SiegeCastlePrisonerDispositionProfile
{
    public const uint SuccessMessageColor = 0xFFB6F7A8u;

    public const uint WarningMessageColor = 0xFFFFD27Fu;

    public static int ResolveRecruitCount(int availablePrisoners, int freePartySlots)
    {
        return Math.Min(Math.Max(0, availablePrisoners), Math.Max(0, freePartySlots));
    }

    public static int ResolveTransferredWounded(int stackCount, int stackWounded, int transferredCount)
    {
        int count = Math.Max(0, stackCount);
        int wounded = Math.Min(count, Math.Max(0, stackWounded));
        int transferred = Math.Min(count, Math.Max(0, transferredCount));
        return count == 0 ? 0 : Math.Min(transferred, (int)Math.Floor((double)wounded * transferred / count));
    }

    public static int ResolveTransferredXp(int stackCount, int stackXp, int transferredCount)
    {
        int count = Math.Max(0, stackCount);
        int xp = Math.Max(0, stackXp);
        int transferred = Math.Min(count, Math.Max(0, transferredCount));
        return count == 0 ? 0 : Math.Min(xp, (int)Math.Floor((double)xp * transferred / count));
    }

    public static string BuildRecruitMessage(int recruited, int remaining)
    {
        return recruited > 0
            ? "【城堡处置】已收编 " + recruited + " 名普通战俘；仍有 " + Math.Max(0, remaining) + " 名普通战俘待处置。"
            : "【城堡处置】队伍没有空余编制，未能收编普通战俘。";
    }

    public static string BuildSlaughterMessage(int slaughtered)
    {
        return "【城堡处置】已下令处决 " + Math.Max(0, slaughtered) + " 名普通战俘；被俘领主未包含在内。";
    }

    public static string BuildRecruitMemoryText(int recruited, int remaining)
    {
        return "玩家在攻占城堡后的处置现场收编了 " + Math.Max(0, recruited)
            + " 名普通守军战俘，尚余 " + Math.Max(0, remaining) + " 名普通战俘待处置。";
    }

    public static string BuildSlaughterMemoryText(int slaughtered)
    {
        return "玩家在攻占城堡后的处置现场下令处决了 " + Math.Max(0, slaughtered)
            + " 名普通守军战俘；该命令不包含被俘领主。";
    }
}
