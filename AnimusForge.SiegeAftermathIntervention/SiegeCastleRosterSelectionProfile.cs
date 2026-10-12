using System;



namespace AnimusForge.SiegeAftermathIntervention;



/// <summary>

/// Dependency-free limits and wording for the castle aftermath member/prisoner selector.

/// Bannerlord PartyScreen construction and roster snapshots remain in the fused AF adapter.

/// </summary>

public static class SiegeCastleRosterSelectionProfile

{

    public const int MaxAlliedTroops = 50;



    public const int MaxPrisoners = 200;



    /// <summary>

    /// Native FormationClass.Infantry, displayed as the first command group.

    /// All player-selected castle escort troops share this formation regardless

    /// of their normal troop class.

    /// </summary>

    public const int AlliedFormationClassIndex = 0;



    public const int RegularPrisonerFormationIndex = 6;



    public const int LordPrisonerFormationIndex = 7;



    public const string AlliedSpawnCommandSource = "castle_allied_spawn";



    public const string AlliedCommandUiRefreshSource = "castle_allied_spawn_complete";



    public const int AlliedInitialGridColumns = 8;



    public const float AlliedInitialStartDepth = 4f;



    public const float AlliedInitialRowSpacing = 1.8f;



    public const float AlliedInitialLateralSpacing = 1.6f;



    public const string PrisonerSpawnCommandSource = "castle_prisoner_spawn";



    public const string PrisonerCommandUiRefreshSource = "castle_prisoner_spawn_complete";



    public const string AvailableRosterTitle = "Select Main Party Troops, Army Commanders / Prisoners";
    public const string AvailableRosterTitleEn = "Available party troops, army leaders, and prisoners";



    public const string SelectedRosterTitle = "Castle Aftermath Roster";
    public const string SelectedRosterTitleEn = "Castle aftermath party";



    public const string ScreenHeader = "Select Troops and Prisoners to Enter Castle";
    public const string ScreenHeaderEn = "Select soldiers and prisoners to enter the castle";



    public const string SelectionCanceledSource = "castle_roster_selection_cancel";



    public const string SelectionFailedSource = "castle_roster_selection_failed";



    public const string DecisionPolicyMessageEn = "[Castle aftermath] Enter the scene to negotiate with defeated garrison troops and captive lords, and issue recruitment or slaughter orders; leaving directly without clear orders settles as mercy by default. Town civilian looting, reassurance, massacre, and colonization rules do not apply to castles.";

    public const string DecisionPolicyMessage = "[Castle aftermath] After entering, you may negotiate with defeated defenders and captured lords, and issue recruitment or slaughter orders; leaving directly without a decision defaults to mercy. Town civilian plunder, reassurance, massacre, and colonization do not apply to castles.";



    public static bool IsWithinLimits(int alliedTroopCount, int prisonerCount)

    {

        return alliedTroopCount >= 0

            && prisonerCount >= 0

            && alliedTroopCount <= MaxAlliedTroops

            && prisonerCount <= MaxPrisoners;

    }



    public static int ClampAlliedTroopCount(int count)

    {

        return Clamp(count, MaxAlliedTroops);

    }



    public static int ClampPrisonerCount(int count)

    {

        return Clamp(count, MaxPrisoners);

    }



    public static int ResolveMainStackWounded(

        int originalNumber,

        int originalWounded,

        int mainNumber,

        int currentMainWounded)

    {

        int safeOriginalNumber = Math.Max(0, originalNumber);

        int safeMainNumber = Math.Min(safeOriginalNumber, Math.Max(0, mainNumber));

        int safeHoldingNumber = safeOriginalNumber - safeMainNumber;

        int safeOriginalWounded = Math.Min(safeOriginalNumber, Math.Max(0, originalWounded));

        int minimumMainWounded = Math.Max(0, safeOriginalWounded - safeHoldingNumber);

        int maximumMainWounded = Math.Min(safeMainNumber, safeOriginalWounded);

        return Math.Min(maximumMainWounded, Math.Max(minimumMainWounded, currentMainWounded));

    }



    public static int ResolveMainStackXp(int originalXp, int holdingXp)

    {

        return Math.Max(0, Math.Max(0, originalXp) - Math.Max(0, holdingXp));

    }



    public static bool ShouldIncludePlayerPartyMember(

        bool isPlayer,

        bool isHero,

        bool isAlive,

        bool isPrisoner,

        bool isWounded)

    {

        return !isPlayer

            && !isPrisoner

            && !isWounded

            && (!isHero || isAlive);

    }



    public static bool ShouldIncludeArmyPartyLeader(

        bool isPlayer,

        bool isHero,

        bool isLord,

        bool isPartyLeader,

        bool isFriendly,

        bool isAlive,

        bool isPrisoner,

        bool isWounded)

    {

        return !isPlayer

            && isHero

            && isLord

            && isPartyLeader

            && isFriendly

            && isAlive

            && !isPrisoner

            && !isWounded;

    }



    public static string BuildInstructionMessage()

        => BuildInstructionMessage(false);



    public static string BuildInstructionMessage(bool isEnglish)

    {

        if (isEnglish)

        {

            return "[Castle aftermath] Select up to " + MaxAlliedTroops

                + " allied followers to enter the castle: regular troops only from the player party, army parties show only noble captains; also select up to "

                + MaxPrisoners + " prisoners, who may include captive lords.";

        }

        return CastleAftermathCompatibilityTexts.Raw("roster_instruction_prefix") + MaxAlliedTroops

            + CastleAftermathCompatibilityTexts.Raw("roster_instruction_middle")

            + MaxPrisoners + CastleAftermathCompatibilityTexts.Raw("roster_instruction_suffix");

    }



    public static string BuildDecisionPolicyMessage()

        => BuildDecisionPolicyMessage(false);



    public static string BuildDecisionPolicyMessage(bool isEnglish)

        => isEnglish ? DecisionPolicyMessageEn : DecisionPolicyMessage;



    public static string BuildConfirmedMessage(int alliedTroopCount, int prisonerCount)

        => BuildConfirmedMessage( alliedTroopCount, prisonerCount, false);



    public static string BuildConfirmedMessage(int alliedTroopCount, int prisonerCount, bool isEnglish)

    {

        if (isEnglish)

        {

            return "[Castle aftermath] Selected " + ClampAlliedTroopCount(alliedTroopCount)

                + " allied followers and " + ClampPrisonerCount(prisonerCount) + " prisoners.";

        }

        return CastleAftermathCompatibilityTexts.Raw("roster_selected_prefix") + ClampAlliedTroopCount(alliedTroopCount)

            + CastleAftermathCompatibilityTexts.Raw("roster_selected_middle") + ClampPrisonerCount(prisonerCount) + CastleAftermathCompatibilityTexts.Raw("roster_selected_suffix");

    }



    public static string BuildPrisonerSceneReadyMessage(int selectedCount, int activeCount)

        => BuildPrisonerSceneReadyMessage( selectedCount, activeCount, false);



    public static string BuildPrisonerSceneReadyMessage(int selectedCount, int activeCount, bool isEnglish)

    {

        int selected = ClampPrisonerCount(selectedCount);

        int active = ClampPrisonerCount(activeCount);

        if (active >= selected)

        {

            return isEnglish

                ? "[Castle aftermath] Brought " + active + " prisoners; command them through formation 7."

                : CastleAftermathCompatibilityTexts.Raw("roster_spawned_prefix") + active + CastleAftermathCompatibilityTexts.Raw("roster_spawned_prisoner_suffix");

        }

        return isEnglish

            ? "[Castle aftermath] Prisoners entered scene: " + active + "/" + selected + "; details logged."

            : CastleAftermathCompatibilityTexts.Raw("roster_actual_prisoner_prefix") + active + "/" + selected + CastleAftermathCompatibilityTexts.Raw("roster_actual_suffix");

    }



    public static string BuildAlliedSceneReadyMessage(int selectedCount, int activeCount)

        => BuildAlliedSceneReadyMessage( selectedCount, activeCount, false);



    public static string BuildAlliedSceneReadyMessage(int selectedCount, int activeCount, bool isEnglish)

    {

        int selected = ClampAlliedTroopCount(selectedCount);

        int active = ClampAlliedTroopCount(activeCount);

        if (active >= selected)

        {

            return isEnglish

                ? "[Castle aftermath] Brought " + active + " allied escort troops; command them through native orders."

                : CastleAftermathCompatibilityTexts.Raw("roster_spawned_prefix") + active + CastleAftermathCompatibilityTexts.Raw("roster_spawned_allied_suffix");

        }

        return isEnglish

            ? "[Castle aftermath] Allied troops entered scene: " + active + "/" + selected + "; details logged."

            : CastleAftermathCompatibilityTexts.Raw("roster_actual_allied_prefix") + active + "/" + selected + CastleAftermathCompatibilityTexts.Raw("roster_actual_suffix");

    }



    public static string BuildLimitMessage(int alliedTroopCount, int prisonerCount)

        => BuildLimitMessage( alliedTroopCount, prisonerCount, false);



    public static string BuildLimitMessage(int alliedTroopCount, int prisonerCount, bool isEnglish)

    {

        if (alliedTroopCount > MaxAlliedTroops)

        {

            return isEnglish

                ? "Allied escort roster cannot exceed " + MaxAlliedTroops + " troops."

                : CastleAftermathCompatibilityTexts.Raw("roster_allied_limit_prefix") + MaxAlliedTroops + CastleAftermathCompatibilityTexts.Raw("roster_limit_suffix");

        }



        if (prisonerCount > MaxPrisoners)

        {

            return isEnglish

                ? "Brought prisoners cannot exceed " + MaxPrisoners + " captives."

                : CastleAftermathCompatibilityTexts.Raw("roster_prisoner_limit_prefix") + MaxPrisoners + CastleAftermathCompatibilityTexts.Raw("roster_limit_suffix");

        }



        return string.Empty;

    }



    private static int Clamp(int count, int max)

    {

        if (count <= 0)

        {

            return 0;

        }



        return count > max ? max : count;

    }

}

