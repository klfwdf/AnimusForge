using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free quantity policy for castle prisoner aftermath actions.
/// It keeps captured lords out of ordinary soldier-prisoner allocation and lets one castle scene
/// split the selected regular prisoner pool across multiple military outcomes.
/// </summary>
public static class SiegeCastlePrisonerAllocationProfile
{
    public const string DiagnosticCategory = "CastlePrisonerAllocation";

    public const string MemoryTitle = "城堡俘虏数量分配";

    public const int SoldierArmoryLootPercent = 50;

    public const int NoExplicitQuantity = -1;

    public const string QuantityInstruction = "士兵俘虏类标签可写数量后缀，例如 [ACTION:战俘劳役:80]、[ACTION:接收军械:40]、[ACTION:屠戮守军:20]、[ACTION:贩卖俘虏:40]；[ACTION:收编战俘] 默认收编剩余全部普通士兵俘虏；数量只消耗普通士兵俘虏池，排除被俘领主/英雄。";

    public static bool IsRegularPrisonerAllocationAction(SiegeCastleAftermathActionKind kind)
    {
        return kind == SiegeCastleAftermathActionKind.HonorCaptives
            || kind == SiegeCastleAftermathActionKind.RecruitGarrison
            || kind == SiegeCastleAftermathActionKind.SeizeArmory
            || kind == SiegeCastleAftermathActionKind.LaborPrisoners
            || kind == SiegeCastleAftermathActionKind.SlaughterGarrison
            || kind == SiegeCastleAftermathActionKind.SellPrisoners;
    }

    public static bool AllowsExplicitQuantity(SiegeCastleAftermathActionKind kind)
    {
        return IsRegularPrisonerAllocationAction(kind);
    }

    public static bool TryFindTagInstance(string rawText, SiegeCastleAftermathRuleDefinition rule, out SiegeCastleActionTagInstance instance)
    {
        instance = default;
        string text = rawText ?? string.Empty;
        string canonical = (rule.CanonicalTag ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(canonical) || !SiegeCastleAftermathProfile.TryParseCanonicalTag(canonical, out SiegeCastleAftermathActionKind kind))
        {
            return false;
        }

        string prefix = canonical.EndsWith("]", StringComparison.Ordinal)
            ? canonical.Substring(0, canonical.Length - 1)
            : canonical;
        string pattern = Regex.Escape(prefix) + @"(?::(?<count>\d{1,6}))?\]";
        Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        int explicitCount = NoExplicitQuantity;
        Group countGroup = match.Groups["count"];
        if (countGroup != null && countGroup.Success && int.TryParse(countGroup.Value, out int parsed))
        {
            explicitCount = ClampExplicitQuantity(parsed);
        }

        string canonicalWithQuantity = explicitCount >= 0 && AllowsExplicitQuantity(kind)
            ? BuildQuantityTag(canonical, explicitCount)
            : canonical;
        instance = new SiegeCastleActionTagInstance(kind, canonical, canonicalWithQuantity, explicitCount);
        return true;
    }

    public static string BuildQuantityTag(string canonicalTag, int count)
    {
        string canonical = (canonicalTag ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(canonical))
        {
            return string.Empty;
        }

        string prefix = canonical.EndsWith("]", StringComparison.Ordinal)
            ? canonical.Substring(0, canonical.Length - 1)
            : canonical;
        return prefix + ":" + ClampExplicitQuantity(count) + "]";
    }

    public static SiegeCastlePrisonerAllocationPlan BuildPlan(int sourceRegularPrisonerCount, IEnumerable<SiegeCastlePrisonerAllocationRequest> requests)
    {
        int source = Math.Max(0, sourceRegularPrisonerCount);
        int remaining = source;
        int requestedTotal = 0;
        int honored = 0;
        int recruited = 0;
        int armory = 0;
        int labor = 0;
        int slaughtered = 0;
        int sold = 0;
        int requestCount = 0;

        if (requests != null)
        {
            foreach (SiegeCastlePrisonerAllocationRequest request in requests)
            {
                if (!IsRegularPrisonerAllocationAction(request.Kind) || request.RequestedCount <= 0)
                {
                    continue;
                }

                requestCount++;
                int desired = ClampExplicitQuantity(request.RequestedCount);
                requestedTotal += desired;
                int take = Math.Min(remaining, desired);
                if (take <= 0)
                {
                    continue;
                }

                switch (request.Kind)
                {
                    case SiegeCastleAftermathActionKind.HonorCaptives:
                        honored += take;
                        break;
                    case SiegeCastleAftermathActionKind.RecruitGarrison:
                        recruited += take;
                        break;
                    case SiegeCastleAftermathActionKind.SeizeArmory:
                        armory += take;
                        break;
                    case SiegeCastleAftermathActionKind.LaborPrisoners:
                        labor += take;
                        break;
                    case SiegeCastleAftermathActionKind.SlaughterGarrison:
                        slaughtered += take;
                        break;
                    case SiegeCastleAftermathActionKind.SellPrisoners:
                        sold += take;
                        break;
                }

                remaining -= take;
            }
        }

        return new SiegeCastlePrisonerAllocationPlan(
            source,
            Math.Max(0, requestCount),
            Math.Max(0, requestedTotal),
            honored,
            recruited,
            armory,
            labor,
            slaughtered,
            sold,
            remaining);
    }

    public static string BuildMemoryText(SiegeCastlePrisonerAllocationPlan plan, string castleName)
    {
        string safeCastleName = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        return safeCastleName + "士兵俘虏数量分配：普通士兵俘虏来源 " + Math.Max(0, plan.SourceRegularPrisonerCount)
            + " 人（不含被俘领主/英雄）"
            + "，优待 " + Math.Max(0, plan.HonoredPrisonerCount)
            + "，收编 " + Math.Max(0, plan.RecruitedPrisonerCount)
            + "，缴械/军械接收 " + Math.Max(0, plan.ArmoryPrisonerCount)
            + "，劳役 " + Math.Max(0, plan.LaborPrisonerCount)
            + "，屠戮 " + Math.Max(0, plan.SlaughteredPrisonerCount)
            + "，贩卖 " + Math.Max(0, plan.SoldPrisonerCount)
            + "，未分配 " + Math.Max(0, plan.UnallocatedRegularPrisonerCount)
            + "；士兵军械战利品按同数量战斗战利品的 " + SoldierArmoryLootPercent + "% 估算。";
    }

    public static string BuildDiagnosticText(SiegeCastlePrisonerAllocationPlan plan)
    {
        return "sourceRegularPrisoners=" + Math.Max(0, plan.SourceRegularPrisonerCount)
            + " explicitRequests=" + Math.Max(0, plan.ExplicitRequestCount)
            + " requestedTotal=" + Math.Max(0, plan.RequestedTotalCount)
            + " honor=" + Math.Max(0, plan.HonoredPrisonerCount)
            + " recruit=" + Math.Max(0, plan.RecruitedPrisonerCount)
            + " armory=" + Math.Max(0, plan.ArmoryPrisonerCount)
            + " labor=" + Math.Max(0, plan.LaborPrisonerCount)
            + " slaughter=" + Math.Max(0, plan.SlaughteredPrisonerCount)
            + " sold=" + Math.Max(0, plan.SoldPrisonerCount)
            + " unallocated=" + Math.Max(0, plan.UnallocatedRegularPrisonerCount)
            + " soldierArmoryLootPercent=" + SoldierArmoryLootPercent;
    }

    private static int ClampExplicitQuantity(int value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value > SiegeCastleSceneRosterProfile.MaxSelectedPrisoners
            ? SiegeCastleSceneRosterProfile.MaxSelectedPrisoners
            : value;
    }
}

public readonly struct SiegeCastleActionTagInstance
{
    public SiegeCastleActionTagInstance(SiegeCastleAftermathActionKind kind, string canonicalTag, string canonicalTagWithQuantity, int explicitCount)
    {
        Kind = kind;
        CanonicalTag = canonicalTag ?? string.Empty;
        CanonicalTagWithQuantity = canonicalTagWithQuantity ?? string.Empty;
        ExplicitCount = explicitCount;
    }

    public SiegeCastleAftermathActionKind Kind { get; }

    public string CanonicalTag { get; }

    public string CanonicalTagWithQuantity { get; }

    public int ExplicitCount { get; }

    public bool HasExplicitCount
    {
        get { return ExplicitCount >= 0; }
    }
}

public readonly struct SiegeCastlePrisonerAllocationRequest
{
    public SiegeCastlePrisonerAllocationRequest(SiegeCastleAftermathActionKind kind, int requestedCount)
    {
        Kind = kind;
        RequestedCount = requestedCount;
    }

    public SiegeCastleAftermathActionKind Kind { get; }

    public int RequestedCount { get; }
}

public readonly struct SiegeCastlePrisonerAllocationPlan
{
    public SiegeCastlePrisonerAllocationPlan(
        int sourceRegularPrisonerCount,
        int explicitRequestCount,
        int requestedTotalCount,
        int honoredPrisonerCount,
        int recruitedPrisonerCount,
        int armoryPrisonerCount,
        int laborPrisonerCount,
        int slaughteredPrisonerCount,
        int soldPrisonerCount,
        int unallocatedRegularPrisonerCount)
    {
        SourceRegularPrisonerCount = sourceRegularPrisonerCount;
        ExplicitRequestCount = explicitRequestCount;
        RequestedTotalCount = requestedTotalCount;
        HonoredPrisonerCount = honoredPrisonerCount;
        RecruitedPrisonerCount = recruitedPrisonerCount;
        ArmoryPrisonerCount = armoryPrisonerCount;
        LaborPrisonerCount = laborPrisonerCount;
        SlaughteredPrisonerCount = slaughteredPrisonerCount;
        SoldPrisonerCount = soldPrisonerCount;
        UnallocatedRegularPrisonerCount = unallocatedRegularPrisonerCount;
    }

    public int SourceRegularPrisonerCount { get; }

    public int ExplicitRequestCount { get; }

    public int RequestedTotalCount { get; }

    public int HonoredPrisonerCount { get; }

    public int RecruitedPrisonerCount { get; }

    public int ArmoryPrisonerCount { get; }

    public int LaborPrisonerCount { get; }

    public int SlaughteredPrisonerCount { get; }

    public int SoldPrisonerCount { get; }

    public int UnallocatedRegularPrisonerCount { get; }

    public bool HasExplicitAllocation
    {
        get { return ExplicitRequestCount > 0; }
    }
}
