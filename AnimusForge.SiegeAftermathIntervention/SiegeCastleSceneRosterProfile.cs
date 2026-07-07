using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free scene roster policy for the castle variant of GCCZ aftermath.
/// It plans which military aftermath roles should be represented in-scene without
/// performing Bannerlord spawning or touching town civilian rules.
/// </summary>
public static class SiegeCastleSceneRosterProfile
{
    public const string ScenePlanLogCategory = "CastleSceneRoster";

    public const string MissionAfterStartSource = "castle_scene_after_start";

    public const string ControlTickSource = "castle_scene_control_tick";

    public const int MaxSelectedPlayerSoldiers = 100;

    public const int MaxSelectedPrisoners = 200;

    public const int LordHallFallbackMaxSelectedPlayerSoldiers = 20;

    public const int LordHallFallbackMaxSelectedPrisoners = 40;

    public const int PlayerSoldierFormationClassIndex = 0;

    public const int PrisonerFormationClassIndex = 1;

    public const string PlayerSoldierFormationLabel = "1队";

    public const string PrisonerFormationLabel = "2队";

    public const int MaxVisibleCaptiveLords = 3;

    public const int MinVisibleSurrenderedGarrison = 6;

    public const int MaxVisibleSurrenderedGarrison = 24;

    public const int SurrenderedGarrisonVisualDivisor = 4;

    public const int MinPrisonerGuardAgents = 4;

    public const int MaxPrisonerGuardAgents = 12;

    public const int GuardRatioDivisor = 6;

    public static SiegeCastleSceneRosterPlan BuildPlan(int captiveLordSourceCount, int surrenderedGarrisonSourceCount, int alliedSoldierCount, bool hasArmory)
    {
        int visibleCaptiveLords = ResolveVisibleCaptiveLordCount(captiveLordSourceCount);
        int visibleSurrenderedGarrison = ResolveVisibleSurrenderedGarrisonCount(surrenderedGarrisonSourceCount);
        int prisonerGuards = ResolvePrisonerGuardCount(visibleSurrenderedGarrison, visibleCaptiveLords, alliedSoldierCount);
        return new SiegeCastleSceneRosterPlan(
            visibleCaptiveLords,
            visibleSurrenderedGarrison,
            prisonerGuards,
            Math.Max(0, alliedSoldierCount),
            hasArmory);
    }

    public static int ClampSelectedPlayerSoldierCount(int requestedCount)
    {
        return ClampCount(requestedCount, MaxSelectedPlayerSoldiers);
    }

    public static int ClampSelectedPrisonerCount(int requestedCount)
    {
        return ClampCount(requestedCount, MaxSelectedPrisoners);
    }

    public static int ResolveScenePlayerSoldierSpawnLimit(bool isLordHallFallback)
    {
        return isLordHallFallback ? LordHallFallbackMaxSelectedPlayerSoldiers : MaxSelectedPlayerSoldiers;
    }

    public static int ResolveScenePrisonerSpawnLimit(bool isLordHallFallback)
    {
        return isLordHallFallback ? LordHallFallbackMaxSelectedPrisoners : MaxSelectedPrisoners;
    }

    public static bool IsPlayerSoldierFormationIndex(int formationIndex)
    {
        return formationIndex == PlayerSoldierFormationClassIndex;
    }

    public static bool IsPrisonerFormationIndex(int formationIndex)
    {
        return formationIndex == PrisonerFormationClassIndex;
    }

    public static string BuildSelectionInstructionMessage()
    {
        return "【城堡处置】可从主队带入最多 " + MaxSelectedPlayerSoldiers + " 名健康士兵编入" + PlayerSoldierFormationLabel
            + "，并带入最多 " + MaxSelectedPrisoners + " 名俘虏编入" + PrisonerFormationLabel
            + "；入场后使用原版指挥系统控制。";
    }

    public static string BuildSelectionConfirmedMessage(int soldierCount, int prisonerCount)
    {
        return "【城堡处置】已选择带入士兵 " + Math.Max(0, soldierCount) + " 人（" + PlayerSoldierFormationLabel + "）"
            + "、俘虏 " + Math.Max(0, prisonerCount) + " 人（" + PrisonerFormationLabel + "）。";
    }

    public static int ResolveVisibleCaptiveLordCount(int captiveLordSourceCount)
    {
        if (captiveLordSourceCount <= 0)
        {
            return 0;
        }

        return Math.Min(MaxVisibleCaptiveLords, captiveLordSourceCount);
    }

    public static int ResolveVisibleSurrenderedGarrisonCount(int surrenderedGarrisonSourceCount)
    {
        if (surrenderedGarrisonSourceCount <= 0)
        {
            return 0;
        }

        if (surrenderedGarrisonSourceCount <= MinVisibleSurrenderedGarrison)
        {
            return surrenderedGarrisonSourceCount;
        }

        int scaled = (surrenderedGarrisonSourceCount + SurrenderedGarrisonVisualDivisor - 1) / SurrenderedGarrisonVisualDivisor;
        int visible = Math.Max(MinVisibleSurrenderedGarrison, scaled);
        return Math.Min(MaxVisibleSurrenderedGarrison, visible);
    }

    public static int ResolvePrisonerGuardCount(int visibleSurrenderedGarrisonCount, int visibleCaptiveLordCount, int alliedSoldierCount)
    {
        int prisonerWeight = Math.Max(0, visibleSurrenderedGarrisonCount) + Math.Max(0, visibleCaptiveLordCount) * 2;
        if (prisonerWeight <= 0)
        {
            return 0;
        }

        int desired = MinPrisonerGuardAgents + (prisonerWeight + GuardRatioDivisor - 1) / GuardRatioDivisor;
        desired = Math.Min(MaxPrisonerGuardAgents, desired);
        if (alliedSoldierCount > 0)
        {
            desired = Math.Min(desired, alliedSoldierCount);
        }

        return Math.Max(0, desired);
    }

    public static bool ShouldUseCastleSceneRosterBridge(bool isCastle, bool activeStage)
    {
        return isCastle && activeStage;
    }

    public static string BuildSceneSummary(SiegeCastleSceneRosterPlan plan, string castleName)
    {
        string safeCastleName = string.IsNullOrWhiteSpace(castleName) ? "这座城堡" : castleName.Trim();
        return "【城堡军务场景】" + safeCastleName
            + "：计划呈现被俘领主/守将 " + Math.Max(0, plan.VisibleCaptiveLordCount) + " 人"
            + "、战败士兵俘虏代表 " + Math.Max(0, plan.VisibleSurrenderedGarrisonCount) + " 人"
            + "、看押士兵 " + Math.Max(0, plan.PrisonerGuardCount) + " 人"
            + "；玩家带入士兵上限 " + MaxSelectedPlayerSoldiers + " 人，编入" + PlayerSoldierFormationLabel
            + "；玩家带入俘虏上限 " + MaxSelectedPrisoners + " 人，编入" + PrisonerFormationLabel
            + "；军械库" + (plan.HasArmory ? "可登记接收" : "未确认")
            + "。该计划只服务城堡战后军务，不触发城镇平民信任/血洗逻辑。";
    }

    public static string BuildDiagnosticText(SiegeCastleSceneRosterPlan plan, string source)
    {
        return "source=" + (string.IsNullOrWhiteSpace(source) ? "N/A" : source.Trim())
            + " captiveLords=" + Math.Max(0, plan.VisibleCaptiveLordCount)
            + " surrenderedGarrison=" + Math.Max(0, plan.VisibleSurrenderedGarrisonCount)
            + " prisonerGuards=" + Math.Max(0, plan.PrisonerGuardCount)
            + " alliedSoldiers=" + Math.Max(0, plan.AlliedSoldierCount)
            + " soldierFormation=" + PlayerSoldierFormationClassIndex
            + " prisonerFormation=" + PrisonerFormationClassIndex
            + " maxSelectedSoldiers=" + MaxSelectedPlayerSoldiers
            + " maxSelectedPrisoners=" + MaxSelectedPrisoners
            + " lordHallFallbackMaxSoldiers=" + LordHallFallbackMaxSelectedPlayerSoldiers
            + " lordHallFallbackMaxPrisoners=" + LordHallFallbackMaxSelectedPrisoners
            + " armory=" + plan.HasArmory;
    }

    private static int ClampCount(int requestedCount, int maxCount)
    {
        if (requestedCount <= 0 || maxCount <= 0)
        {
            return 0;
        }

        return Math.Min(requestedCount, maxCount);
    }
}

public readonly struct SiegeCastleSceneRosterPlan
{
    public SiegeCastleSceneRosterPlan(
        int visibleCaptiveLordCount,
        int visibleSurrenderedGarrisonCount,
        int prisonerGuardCount,
        int alliedSoldierCount,
        bool hasArmory)
    {
        VisibleCaptiveLordCount = Math.Max(0, visibleCaptiveLordCount);
        VisibleSurrenderedGarrisonCount = Math.Max(0, visibleSurrenderedGarrisonCount);
        PrisonerGuardCount = Math.Max(0, prisonerGuardCount);
        AlliedSoldierCount = Math.Max(0, alliedSoldierCount);
        HasArmory = hasArmory;
    }

    public int VisibleCaptiveLordCount { get; }

    public int VisibleSurrenderedGarrisonCount { get; }

    public int PrisonerGuardCount { get; }

    public int AlliedSoldierCount { get; }

    public bool HasArmory { get; }

    public bool HasAnySceneObject
    {
        get { return VisibleCaptiveLordCount > 0 || VisibleSurrenderedGarrisonCount > 0 || PrisonerGuardCount > 0; }
    }
}
