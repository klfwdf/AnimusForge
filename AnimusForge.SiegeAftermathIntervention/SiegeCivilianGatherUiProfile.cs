namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free UI and memory wording for the GCCZ civilian-gathering flow.
/// AF adapters still own live mission agents, formation control, and display side effects.
/// </summary>
public static class SiegeCivilianGatherUiProfile
{
    public const uint MessageColor = 0xFFB6F7A8u;

    public const string GatherMemoryTitle = "Gather";
    public const string GatherMemoryTitleEn = "Assemble";

    public const string AssemblyMemoryTitle = "Assembly";
    public const string AssemblyMemoryTitleEn = "Gather";

    public const string MessengerFallbackName = "Messenger";
    public const string MessengerFallbackNameEn = "messenger";

    public const string CivilianFallbackName = "Civilians";
    public const string CivilianFallbackNameEn = "civilians";

    public const string PropagationStartedMessage = "[Siege aftermath] The order was issued; civilians will gather gradually and await further commands.";
    public const string PropagationStartedMessageEn = "[Siege aftermath] The order has been sent; civilians will assemble and await your further commands.";

    public const string MessengerAddedMessage = "[Siege aftermath] Additional messengers joined the gathering.";
    public const string MessengerAddedMessageEn = "[Siege aftermath] A new messenger has joined the assembly order.";

    public const string FormationReadyMessage = "[Siege aftermath] Civilians have gathered to receive orders and joined formation 3; you may command them as a standard unit.";
    public const string FormationReadyMessageEn = "[Siege aftermath] Civilians have assembled into command group 3; you can now direct their formation like troops on the field.";

    public const string FormationCompleteMemory = "Civilians have assembled and joined the player-commanded unit; subsequent NPCs should acknowledge they are present.";
    public const string FormationCompleteMemoryEn = "Civilians have assembled and joined the controllable formation; subsequent NPCs should acknowledge that civilians are present.";

    public static string BuildPropagationStartedMemory(bool seedIsSoldier)
        => BuildPropagationStartedMemory(seedIsSoldier, false);

    public static string BuildPropagationStartedMemory(bool seedIsSoldier, bool isEnglish)
    {
        if (isEnglish)
        {
            return "The player ordered civilians assembled; " + (seedIsSoldier ? "allied soldiers" : "civilians") + " began acting as messengers to notify townspeople to assemble.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("gather_order_prefix") + (seedIsSoldier ? SiegeAftermathCompatibilityTexts.Raw("gather_soldier_noun") : SiegeAftermathCompatibilityTexts.Raw("gather_civilian_noun")) + SiegeAftermathCompatibilityTexts.Raw("gather_order_suffix");
    }

    public static string BuildMessengerAddedMemory(int messengerCount)
        => BuildMessengerAddedMemory(messengerCount, false);

    public static string BuildMessengerAddedMemory(int messengerCount, bool isEnglish)
    {
        int safeMessengerCount = messengerCount < 0 ? 0 : messengerCount;
        if (isEnglish)
        {
            return "The player assigned additional messengers to continue notifying civilians; currently approx " + safeMessengerCount + " messengers.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("gather_additional_prefix") + safeMessengerCount + SiegeAftermathCompatibilityTexts.Raw("gather_additional_suffix");
    }

    public static string BuildMessengerSpeechFactText(string targetName)
        => BuildMessengerSpeechFactText(targetName, false);

    public static string BuildMessengerSpeechFactText(string targetName, bool isEnglish)
    {
        string name = NormalizeTargetName(targetName, isEnglish);
        if (isEnglish)
        {
            return "[Siege aftermath messenger] You are summoning town civilians for the player. The player just ordered all civilians to assemble nearby for orders, announcements, or disposition; you have reached " + name + " and must convey this in one natural line. Do not write internal tags or explain mechanics; make the listener understand the lord/commander summons everyone.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("gather_messenger_prompt_prefix") + name + SiegeAftermathCompatibilityTexts.Raw("gather_messenger_prompt_suffix");
    }

    public static string BuildFormationQueuedMemory(string reason)
        => BuildFormationQueuedMemory(reason, false);

    public static string BuildFormationQueuedMemory(string reason, bool isEnglish)
    {
        if (isEnglish)
        {
            return "Civilian assembly entered final stage; the system is assigning following civilians to the controllable formation; reason=" + NormalizeReason(reason) + ".";
        }
        return SiegeAftermathCompatibilityTexts.Raw("gather_finalize_prefix") + NormalizeReason(reason) + SiegeAftermathCompatibilityTexts.Raw("gather_finalize_suffix");
    }

    private static string NormalizeTargetName(string targetName, bool isEnglish)
    {
        return string.IsNullOrWhiteSpace(targetName) ? (isEnglish ? CivilianFallbackNameEn : CivilianFallbackName) : targetName.Trim();
    }

    private static string NormalizeTargetName(string targetName)
    {
        return string.IsNullOrWhiteSpace(targetName) ? CivilianFallbackName : targetName.Trim();
    }

    private static string NormalizeReason(string reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? "N/A" : reason.Trim();
    }
}
