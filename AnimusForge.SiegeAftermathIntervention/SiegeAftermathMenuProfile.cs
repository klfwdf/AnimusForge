using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free menu identifiers used by GCCZ aftermath entry and native summary routing.
/// AF adapters still own Bannerlord menu registration, switching, and live menu side effects.
/// </summary>
public static class SiegeAftermathMenuProfile
{
    public const string SettlementTakenPlayerLeaderMenuId = "menu_settlement_taken_player_leader";

    public const string SettlementTakenMenuId = "menu_settlement_taken";

    public const string ContextualSummaryMenuId = "siege_aftermath_contextual_summary";

    public const string ContextualSummarySourceMarker = "contextual_summary";

    public const string EntryMenuOptionId = "AnimusForge_siege_ai_intervention_entry";

    public const int EntryMenuInsertionIndex = 0;

    public static readonly string[] EntryMenuIds =
    {
        SettlementTakenPlayerLeaderMenuId,
        SettlementTakenMenuId
    };

    public static string BuildEntryMenuOptionId(string menuId)
    {
        if (string.IsNullOrWhiteSpace(menuId) || string.Equals(menuId, SettlementTakenPlayerLeaderMenuId, StringComparison.OrdinalIgnoreCase))
        {
            return EntryMenuOptionId;
        }

        return EntryMenuOptionId + "_" + menuId.Trim();
    }

    public static bool IsNativeSettlementTakenMenuId(string menuId)
    {
        return string.Equals(menuId, SettlementTakenMenuId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(menuId, SettlementTakenPlayerLeaderMenuId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsContextualSummaryMenuId(string menuId)
    {
        return string.Equals(menuId, ContextualSummaryMenuId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsNativeOrContextualSummaryMenuId(string menuId)
    {
        return IsNativeSettlementTakenMenuId(menuId) || IsContextualSummaryMenuId(menuId);
    }
}
