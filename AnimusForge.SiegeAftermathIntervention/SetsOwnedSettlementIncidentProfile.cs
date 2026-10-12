namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free wording and relation policy for SETS incidents inside player-owned
/// or ruler-attached settlements. AF adapters own Bannerlord menu registration and relation calls.
/// </summary>
public static class SetsOwnedSettlementIncidentProfile
{
    public const string MenuId = "AnimusForge_sets_owned_town_incident";

    public const string EntryOptionId = "AnimusForge_sets_owned_town_incident_enter";

    public const string LeaveOptionId = "AnimusForge_sets_owned_town_incident_leave";

    public const string EntryOptionText = "{=!}{AF_SETS_OWNED_INCIDENT_ENTRY_OPTION_TEXT}";

    public const string LeaveOptionText = "Withdraw and leave";
    public const string LeaveOptionTextEn = "Withdraw and leave";

    public const uint MessageColor = 0xFFB6F7A8u;

    public const uint WarningColor = 0xFFFF7777u;

    public const int MinorIncidentRelationPenalty = -10;

    public const int NotableKilledRelationPenalty = -30;

    public const int PlunderRelationPenalty = -50;

    public const int MassacreRelationPenalty = -70;

    public const int CulturalRepopulationRelationPenalty = -100;

    public static bool SupportsSceneKind(SetsSettlementSceneKind kind)
    {
        return kind == SetsSettlementSceneKind.Town
            || kind == SetsSettlementSceneKind.Castle
            || kind == SetsSettlementSceneKind.Village;
    }

    public static string BuildEntryOptionText(SetsSettlementSceneKind kind)
    {
        return BuildEntryOptionText(kind, isEnglish: false);
    }

    public static string BuildEntryOptionText(SetsSettlementSceneKind kind, bool isEnglish)
    {
        if (isEnglish)
        {
            switch (kind)
            {
                case SetsSettlementSceneKind.Castle:
                    return "Decide in Person at Castle";
                case SetsSettlementSceneKind.Village:
                    return "Decide in Person at Village";
                default:
                    return "Decide in Person at Town";
            }
        }
        switch (kind)
        {
            case SetsSettlementSceneKind.Castle:
                return SiegeAftermathCompatibilityTexts.Raw("incident_enter_castle");
            case SetsSettlementSceneKind.Village:
                return SiegeAftermathCompatibilityTexts.Raw("incident_enter_village");
            default:
                return SiegeAftermathCompatibilityTexts.Raw("incident_enter_town");
        }
    }

    public static int ResolveOwnerRelationPenalty(
        SiegeAftermathResolutionKind aftermath,
        bool notableKilled,
        bool culturalRepopulation)
    {
        if (aftermath == SiegeAftermathResolutionKind.Pillage)
        {
            return PlunderRelationPenalty;
        }

        if (aftermath == SiegeAftermathResolutionKind.Devastate)
        {
            return culturalRepopulation
                ? CulturalRepopulationRelationPenalty
                : MassacreRelationPenalty;
        }

        return notableKilled
            ? NotableKilledRelationPenalty
            : MinorIncidentRelationPenalty;
    }

    public static int ResolveAdditionalPenaltyAfterNative(int desiredTotalPenalty, int nativePenalty)
    {
        int additionalPenalty = desiredTotalPenalty - nativePenalty;
        return additionalPenalty < 0 ? additionalPenalty : 0;
    }

    public static string BuildMenuText(string playerName, string settlementName)
    {
        return BuildMenuText(SetsSettlementSceneKind.Town, playerName, settlementName, isEnglish: false);
    }

    public static string BuildMenuText(SetsSettlementSceneKind kind, string playerName, string settlementName)
    {
        return BuildMenuText(kind, playerName, settlementName, isEnglish: false);
    }

    public static string BuildMenuText(string playerName, string settlementName, bool isEnglish)
    {
        return BuildMenuText(SetsSettlementSceneKind.Town, playerName, settlementName, isEnglish);
    }

    public static string BuildMenuText(SetsSettlementSceneKind kind, string playerName, string settlementName, bool isEnglish)
    {
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind, isEnglish);
        if (isEnglish)
        {
            return NormalizeName(playerName, "The player") + " went on a slaughter in " + NormalizeName(settlementName, "this " + noun) + ". The fate of the remaining residents is yours to decide.";
        }
        return NormalizeName(playerName, SiegeAftermathCompatibilityTexts.Raw("player_fallback")) + SiegeAftermathCompatibilityTexts.Raw("incident_menu_in") + NormalizeName(settlementName, SiegeAftermathCompatibilityTexts.Raw("incident_this") + noun) + SiegeAftermathCompatibilityTexts.Raw("incident_menu_suffix");
    }

    public static string BuildEntryInstruction()
    {
        return BuildEntryInstruction(SetsSettlementSceneKind.Town, isEnglish: false);
    }

    public static string BuildEntryInstruction(SetsSettlementSceneKind kind)
    {
        return BuildEntryInstruction(kind, isEnglish: false);
    }

    public static string BuildEntryInstruction(bool isEnglish)
    {
        return BuildEntryInstruction(SetsSettlementSceneKind.Town, isEnglish);
    }

    public static string BuildEntryInstruction(SetsSettlementSceneKind kind, bool isEnglish)
    {
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind, isEnglish);
        if (isEnglish)
        {
            if (kind == SetsSettlementSceneKind.Village)
            {
                return "[GCCZ village] This is an incident in an owned or governed village: you can enter in person to continue disposition, or withdraw and leave.";
            }
            return "[SETS] This is an incident in an owned/attached " + noun + ": you can enter in person to continue disposition, or withdraw and leave. Native devastation, pillage, and mercy options are hidden.";
        }

        if (kind == SetsSettlementSceneKind.Village)
        {
            return SiegeAftermathCompatibilityTexts.Raw("incident_instruction_village");
        }
        return SiegeAftermathCompatibilityTexts.Raw("incident_instruction_prefix") + noun + SiegeAftermathCompatibilityTexts.Raw("incident_instruction_suffix");
    }

    public static string BuildLeaveMessage()
    {
        return BuildLeaveMessage(SetsSettlementSceneKind.Town, isEnglish: false);
    }

    public static string BuildLeaveMessage(SetsSettlementSceneKind kind)
    {
        return BuildLeaveMessage(kind, isEnglish: false);
    }

    public static string BuildLeaveMessage(bool isEnglish)
    {
        return BuildLeaveMessage(SetsSettlementSceneKind.Town, isEnglish);
    }

    public static string BuildLeaveMessage(SetsSettlementSceneKind kind, bool isEnglish)
    {
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind, isEnglish);
        if (isEnglish)
        {
            return "[SETS] You withdrew and left, taking no further action against the remaining residents of the " + noun + ".";
        }
        return SiegeAftermathCompatibilityTexts.Raw("incident_leave_prefix") + noun + SiegeAftermathCompatibilityTexts.Raw("incident_leave_suffix");
    }

    public static string BuildRelationPenaltyMessage(int penalty)
    {
        return BuildRelationPenaltyMessage(penalty, isEnglish: false);
    }

    public static string BuildRelationPenaltyMessage(int penalty, bool isEnglish)
    {
        if (isEnglish)
        {
            return "[SETS] The relation of the attached domain owner with the player changed by " + penalty + " due to your disposition.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("incident_penalty_prefix") + penalty + SiegeAftermathCompatibilityTexts.Raw("incident_penalty_suffix");
    }

    public static string BuildRuntimeContext(string playerName, string settlementName, bool alliedSoldier, bool civilian)
    {
        return BuildRuntimeContext(SetsSettlementSceneKind.Town, playerName, settlementName, alliedSoldier, civilian, isEnglish: false);
    }

    public static string BuildRuntimeContext(string playerName, string settlementName, bool alliedSoldier, bool civilian, bool isEnglish)
    {
        return BuildRuntimeContext(SetsSettlementSceneKind.Town, playerName, settlementName, alliedSoldier, civilian, isEnglish);
    }

    public static string BuildRuntimeContext(SetsSettlementSceneKind kind, string playerName, string settlementName, bool alliedSoldier, bool civilian)
    {
        return BuildRuntimeContext(kind, playerName, settlementName, alliedSoldier, civilian, isEnglish: false);
    }

    public static string BuildRuntimeContext(SetsSettlementSceneKind kind, string playerName, string settlementName, bool alliedSoldier, bool civilian, bool isEnglish)
    {
        string noun = SetsSettlementEntryProfile.GetSettlementNoun(kind, isEnglish);
        if (isEnglish)
        {
            string player = NormalizeName(playerName, "The player");
            string settlement = NormalizeName(settlementName, "this " + noun);
            string context = "[SETS OWNED/ATTACHED " + noun.ToUpperInvariant() + " INCIDENT] This is not an ordinary post-siege disposition of a foreign enemy settlement, but an internal incident within " + settlement + " already owned or ruled by " + player + " as monarch. "
                + player + " remains the highest command authority on-site, but the residents are their subjects; if this is an attached vassal's domain, the owner will hate the player for harm, looting, or massacre.";
            if (alliedSoldier)
            {
                context += " [ALLIED SOLDIER SPECIAL AWARENESS] You are a soldier/companion brought into the " + noun + " by the player, not the local garrison. If the player orders looting or massacre of their own subjects, you will still obey direct orders, but should show shock, hesitation, lowered voice, concern for reputation and the vassal lord's reaction; do not treat this as standard enemy loot rights, and do not escalate disposition on your own.";
            }
            if (civilian)
            {
                context += " [SUBJECT AWARENESS] You know the player is the master or king of this " + noun + ", not passing bandits; you may show fear, plead, or accuse, but do not deny the player's authority to rule.";
            }
            return context;
        }

        string playerCn = NormalizeName(playerName, SiegeAftermathCompatibilityTexts.Raw("player_fallback"));
        string settlementCn = NormalizeName(settlementName, SiegeAftermathCompatibilityTexts.Raw("incident_this") + noun);
        string contextCn = SiegeAftermathCompatibilityTexts.Raw("incident_ctx_prefix") + noun + SiegeAftermathCompatibilityTexts.Raw("incident_ctx_internal") + playerCn + SiegeAftermathCompatibilityTexts.Raw("incident_ctx_owned") + settlementCn + SiegeAftermathCompatibilityTexts.Raw("incident_ctx_incident") + playerCn + SiegeAftermathCompatibilityTexts.Raw("incident_ctx_authority");
        if (alliedSoldier)
        {
            contextCn += SiegeAftermathCompatibilityTexts.Raw("incident_soldier_awareness_prefix") + noun + SiegeAftermathCompatibilityTexts.Raw("incident_soldier_awareness_suffix");
        }
        if (civilian)
        {
            contextCn += SiegeAftermathCompatibilityTexts.Raw("incident_civilian_awareness_prefix") + noun + SiegeAftermathCompatibilityTexts.Raw("incident_civilian_awareness_suffix");
        }
        return contextCn;
    }

    private static string NormalizeName(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
