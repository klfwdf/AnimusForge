using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free count preservation and player-facing wording for castle prisoner disposition.
/// </summary>
public static class SiegeCastlePrisonerDispositionProfile
{
    public const string RosterUnavailableReason = "castle_roster_unavailable";

    public const string PartyCapacityFullReason = "party_capacity_full";

    public const string NoMatchingRegularPrisonersReason = "no_matching_regular_prisoners";

    public const string NoSellableRegularPrisonersReason = "no_sellable_regular_prisoners";

    public const string UnsupportedDeferredTerminalActionReason = "unsupported_deferred_terminal_action";

    public const string RemovedForReasonPrefix = "removed_for_";

    public const string RecruitedReason = "recruited";

    public const string SlaughteredReason = "slaughtered";

    public const string ExceptionReasonPrefix = "exception:";

    public const uint SuccessMessageColor = 0xFFB6F7A8u;

    public const uint WarningMessageColor = 0xFFFFD27Fu;

    public static int ResolveRecruitCount(int availablePrisoners, int freePartySlots)
    {
        return Math.Min(Math.Max(0, availablePrisoners), Math.Max(0, freePartySlots));
    }

    public static int ResolveRemainingStagedRecruitCapacity(int currentFreePartySlots, int previouslyStagedRecruitCount)
    {
        return Math.Max(0, Math.Max(0, currentFreePartySlots) - Math.Max(0, previouslyStagedRecruitCount));
    }

    public static string BuildStagedRecruitCapacityWarning(
        int currentFreePartySlots,
        int previouslyStagedRecruitCount,
        int currentGroupCount)
    {
        int freeSlots = Math.Max(0, currentFreePartySlots);
        int previousCount = Math.Max(0, previouslyStagedRecruitCount);
        int groupCount = Math.Max(0, currentGroupCount);
        int remainingCapacity = ResolveRemainingStagedRecruitCapacity(freeSlots, previousCount);
        if (groupCount <= remainingCapacity)
        {
            return string.Empty;
        }

        string previousPlan = previousCount > 0 ? "此前收编组已计划 " + previousCount + " 人，" : string.Empty;
        return "【城堡处置】注意：按主队当前 " + freeSlots + " 个空余编制和暂存组执行顺序，"
            + previousPlan + "本组离场时预计最多再收编 "
            + remainingCapacity + " 人；超出部分将继续保持俘虏身份。";
    }

    public static string DescribeDeferredFailureReason(string reasonCode)
    {
        string normalized = (reasonCode ?? string.Empty).Trim();
        switch (normalized)
        {
            case PartyCapacityFullReason:
                return "主队编制已满";
            case RosterUnavailableReason:
                return "俘虏或部队名册不可用";
            case NoMatchingRegularPrisonersReason:
                return "名册中已无该分组可处理的普通战俘";
            case NoSellableRegularPrisonersReason:
                return "名册中已无该分组可出售的普通战俘";
            case UnsupportedDeferredTerminalActionReason:
                return "该暂存处置当前不受离场结算支持";
        }

        if (normalized.StartsWith(RemovedForReasonPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return "名册中已无该分组可处理的普通战俘";
        }
        if (IsExceptionReason(normalized))
        {
            return "结算执行时发生异常，详情已写入日志";
        }
        return "结算未生效，详情已写入日志";
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

    public static string BuildRecruitMessage(int recruited, int remaining, string reasonCode)
    {
        int recruitedCount = Math.Max(0, recruited);
        int remainingCount = Math.Max(0, remaining);
        if (recruitedCount > 0)
        {
            string message = "【城堡处置】已将 " + recruitedCount
                + " 名普通战俘从俘虏名册转入主队；仍有 " + remainingCount + " 名带入的普通战俘待处置。";
            return IsExceptionReason(reasonCode)
                ? message + " 本次仅部分执行成功，异常详情已写入日志。"
                : message;
        }

        switch ((reasonCode ?? string.Empty).Trim())
        {
            case PartyCapacityFullReason:
                return "【城堡处置】主队没有空余编制，未能收编普通战俘。";
            case NoMatchingRegularPrisonersReason:
                return "【城堡处置】本次带入名册中没有仍可收编的普通战俘；被俘领主不会由收编标签处理。";
            case RosterUnavailableReason:
                return "【城堡处置】俘虏或部队名册当前不可用，收编未执行；异常详情已写入日志。";
            default:
                return "【城堡处置】未能收编普通战俘；异常详情已写入日志。";
        }
    }

    public static string BuildSlaughterMessage(int slaughtered, int remaining, string reasonCode)
    {
        int slaughteredCount = Math.Max(0, slaughtered);
        int remainingCount = Math.Max(0, remaining);
        if (slaughteredCount > 0)
        {
            string message = "【城堡处置】屠戮命令已造成 " + slaughteredCount
                + " 名普通战俘在场景内实际死亡，死亡后才从俘虏名册扣除；仍有 " + remainingCount
                + " 名带入的普通战俘待处置。被俘领主未包含在内。";
            return IsExceptionReason(reasonCode)
                ? message + " 本次仅部分执行成功，异常详情已写入日志。"
                : message;
        }

        switch ((reasonCode ?? string.Empty).Trim())
        {
            case NoMatchingRegularPrisonersReason:
                return "【城堡处置】本次带入名册中没有仍可处决的普通战俘；被俘领主未包含在内。";
            case RosterUnavailableReason:
                return "【城堡处置】俘虏名册当前不可用，屠戮未执行；异常详情已写入日志。";
            default:
                return "【城堡处置】未能处决普通战俘；被俘领主未包含在内，异常详情已写入日志。";
        }
    }

    public static string BuildRecruitMemoryText(int recruited, int remaining)
    {
        return "玩家在攻占城堡后的处置现场收编了 " + Math.Max(0, recruited)
            + " 名普通守军战俘，尚余 " + Math.Max(0, remaining) + " 名普通战俘待处置。";
    }

    public static string BuildSlaughterMemoryText(int slaughtered, int remaining)
    {
        return "玩家在攻占城堡后的处置现场下令屠戮；已有 " + Math.Max(0, slaughtered)
            + " 名普通守军战俘在场景内实际死亡，尚余 " + Math.Max(0, remaining)
            + " 名带入的普通战俘待处置；该命令不包含被俘领主。";
    }

    private static bool IsExceptionReason(string reasonCode)
    {
        return (reasonCode ?? string.Empty).StartsWith(ExceptionReasonPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
