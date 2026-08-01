using System;

namespace AnimusForge.SiegeAftermathIntervention;

public sealed class VillageAftermathEffectProfile
{
    private VillageAftermathEffectProfile(
        VillageAftermathActionKind action,
        int goldDelta,
        float hearthFlatDelta,
        float hearthMultiplier,
        int ownerRelationDelta,
        string displayName)
    {
        Action = action;
        GoldDelta = goldDelta;
        HearthFlatDelta = hearthFlatDelta;
        HearthMultiplier = hearthMultiplier;
        OwnerRelationDelta = ownerRelationDelta;
        DisplayName = displayName ?? string.Empty;
    }

    public VillageAftermathActionKind Action { get; }

    public int GoldDelta { get; }

    public float HearthFlatDelta { get; }

    public float HearthMultiplier { get; }

    public int OwnerRelationDelta { get; }

    public string DisplayName { get; }

    public bool IsDestructive => Action == VillageAftermathActionKind.DestroyLivelihood
        || Action == VillageAftermathActionKind.Massacre;

    public static VillageAftermathEffectProfile Resolve(VillageAftermathActionKind action)
    {
        switch (action)
        {
            case VillageAftermathActionKind.RestrainTroops:
                return Build(action, 0, 3f, 1f, 2, "约束军纪");
            case VillageAftermathActionKind.Pacify:
                return Build(action, 0, 8f, 1f, 3, "平息村情");
            case VillageAftermathActionKind.Relief:
                return Build(action, -5000, 30f, 1f, 5, "赈济村民");
            case VillageAftermathActionKind.Fine:
                return Build(action, 1500, -5f, 1f, -3, "罚赎");
            case VillageAftermathActionKind.RequisitionFood:
                return Build(action, 0, -12f, 1f, -5, "征粮");
            case VillageAftermathActionKind.RequisitionProduce:
                return Build(action, 0, -10f, 1f, -5, "征收物产");
            case VillageAftermathActionKind.RequisitionLivestock:
                return Build(action, 0, -15f, 1f, -7, "征收牲畜");
            case VillageAftermathActionKind.LevyRecruits:
                return Build(action, 0, -8f, 1f, -5, "征丁");
            case VillageAftermathActionKind.PunishRingleader:
                return Build(action, 0, -3f, 1f, -10, "惩办首恶");
            case VillageAftermathActionKind.ConfiscateProperty:
                return Build(action, 5000, -20f, 1f, -15, "查抄村产");
            case VillageAftermathActionKind.DestroyLivelihood:
                return Build(action, 0, 0f, 0.75f, -30, "毁坏生计");
            case VillageAftermathActionKind.Massacre:
                return Build(action, 8000, 0f, 0.40f, -70, "屠村");
            default:
                return Build(action, 0, 0f, 1f, 0, action.ToString());
        }
    }

    public float ApplyHearth(float currentHearth)
    {
        float safe = float.IsNaN(currentHearth) || float.IsInfinity(currentHearth) ? 0f : Math.Max(0f, currentHearth);
        return Math.Max(0f, safe * Math.Max(0f, HearthMultiplier) + HearthFlatDelta);
    }

    private static VillageAftermathEffectProfile Build(VillageAftermathActionKind action, int goldDelta, float hearthFlatDelta, float hearthMultiplier, int ownerRelationDelta, string displayName)
    {
        return new VillageAftermathEffectProfile(action, goldDelta, hearthFlatDelta, hearthMultiplier, ownerRelationDelta, displayName);
    }
}
