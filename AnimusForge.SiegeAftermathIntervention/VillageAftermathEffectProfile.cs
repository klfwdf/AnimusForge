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
        => Resolve(action, false);

    public static VillageAftermathEffectProfile Resolve(VillageAftermathActionKind action, bool isEnglish)
    {
        if (isEnglish)
        {
            switch (action)
            {
                case VillageAftermathActionKind.RestrainTroops:
                    return Build(action, 0, 3f, 1f, 2, "Restrain troops");
                case VillageAftermathActionKind.Pacify:
                    return Build(action, 0, 8f, 1f, 3, "Pacify village");
                case VillageAftermathActionKind.Relief:
                    return Build(action, -5000, 30f, 1f, 5, "Relief for villagers");
                case VillageAftermathActionKind.Fine:
                    return Build(action, 1500, -5f, 1f, -3, "Fine");
                case VillageAftermathActionKind.RequisitionFood:
                    return Build(action, 0, -12f, 1f, -5, "Requisition food");
                case VillageAftermathActionKind.RequisitionProduce:
                    return Build(action, 0, -10f, 1f, -5, "Requisition produce");
                case VillageAftermathActionKind.RequisitionLivestock:
                    return Build(action, 0, -15f, 1f, -7, "Requisition livestock");
                case VillageAftermathActionKind.LevyRecruits:
                    return Build(action, 0, -8f, 1f, -5, "Levy recruits");
                case VillageAftermathActionKind.PunishRingleader:
                    return Build(action, 0, -3f, 1f, -10, "Punish ringleader");
                case VillageAftermathActionKind.ConfiscateProperty:
                    return Build(action, 5000, -20f, 1f, -15, "Confiscate property");
                case VillageAftermathActionKind.DestroyLivelihood:
                    return Build(action, 0, 0f, 0.75f, -30, "Destroy livelihoods");
                case VillageAftermathActionKind.Massacre:
                    return Build(action, 8000, 0f, 0.40f, -70, "Massacre");
                default:
                    return Build(action, 0, 0f, 1f, 0, action.ToString());
            }
        }
        switch (action)
        {
            case VillageAftermathActionKind.RestrainTroops:
                return Build(action, 0, 3f, 1f, 2, isEnglish ? "Restrain troops" : SiegeAftermathCompatibilityTexts.Raw("village_action_restrain"));
            case VillageAftermathActionKind.Pacify:
                return Build(action, 0, 8f, 1f, 3, isEnglish ? "Pacify village" : SiegeAftermathCompatibilityTexts.Raw("village_action_pacify"));
            case VillageAftermathActionKind.Relief:
                return Build(action, -5000, 30f, 1f, 5, isEnglish ? "Provide relief" : SiegeAftermathCompatibilityTexts.Raw("village_action_relief"));
            case VillageAftermathActionKind.Fine:
                return Build(action, 1500, -5f, 1f, -3, isEnglish ? "Levy fines" : SiegeAftermathCompatibilityTexts.Raw("village_action_fine"));
            case VillageAftermathActionKind.RequisitionFood:
                return Build(action, 0, -12f, 1f, -5, isEnglish ? "Requisition food" : SiegeAftermathCompatibilityTexts.Raw("village_action_food"));
            case VillageAftermathActionKind.RequisitionProduce:
                return Build(action, 0, -10f, 1f, -5, isEnglish ? "Requisition produce" : SiegeAftermathCompatibilityTexts.Raw("village_action_produce"));
            case VillageAftermathActionKind.RequisitionLivestock:
                return Build(action, 0, -15f, 1f, -7, isEnglish ? "Requisition livestock" : SiegeAftermathCompatibilityTexts.Raw("village_action_livestock"));
            case VillageAftermathActionKind.LevyRecruits:
                return Build(action, 0, -8f, 1f, -5, isEnglish ? "Levy recruits" : SiegeAftermathCompatibilityTexts.Raw("village_action_recruits"));
            case VillageAftermathActionKind.PunishRingleader:
                return Build(action, 0, -3f, 1f, -10, isEnglish ? "Punish ringleader" : SiegeAftermathCompatibilityTexts.Raw("village_action_ringleader"));
            case VillageAftermathActionKind.ConfiscateProperty:
                return Build(action, 5000, -20f, 1f, -15, isEnglish ? "Confiscate property" : SiegeAftermathCompatibilityTexts.Raw("village_action_property"));
            case VillageAftermathActionKind.DestroyLivelihood:
                return Build(action, 0, 0f, 0.75f, -30, isEnglish ? "Destroy livelihoods" : SiegeAftermathCompatibilityTexts.Raw("village_action_livelihood"));
            case VillageAftermathActionKind.Massacre:
                return Build(action, 8000, 0f, 0.40f, -70, isEnglish ? "Massacre" : SiegeAftermathCompatibilityTexts.Raw("village_action_massacre"));
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
