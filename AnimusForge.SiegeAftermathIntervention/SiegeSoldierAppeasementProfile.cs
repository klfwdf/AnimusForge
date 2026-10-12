namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free profile for GCCZ soldier appeasement and its fallback morale penalty.
/// AF adapters apply Bannerlord party morale, UI, and memory side effects.
/// </summary>
public sealed class SiegeSoldierAppeasementProfile
{
    public const uint ValidationMessageColor = 0xFFFFD27Fu;

    public const string TargetValidationMessage = "[Siege aftermath] Appeasing soldier morale must be directed to an allied soldier.";
    public const string TargetValidationMessageEn = "[Siege aftermath] Appeasing soldier morale must target allied soldiers who entered the town.";

    public int MoralePenalty { get; } = 20;

    public string NeedMemoryTitle { get; } = SiegeAftermathCompatibilityTexts.Raw("appease_morale_title");
    public string NeedMemoryTitleEn { get; } = "Morale";

    public string NeedMessageText { get; } = SiegeAftermathCompatibilityTexts.Raw("appease_need_msg");
    public string NeedMessageTextEn { get; } = "[Siege aftermath] Some soldiers are discontented over missed loot; appease allied soldiers before leaving to avoid party morale -20.";

    public uint NeedMessageColor { get; } = 0xFFFFD27Fu;

    public string AppeasementMemoryTitle { get; } = SiegeAftermathCompatibilityTexts.Raw("appease_done_title");
    public string AppeasementMemoryTitleEn { get; } = "Soldier Appeasement";

    public string AppeasementMemoryText { get; } = SiegeAftermathCompatibilityTexts.Raw("appease_done_memory");
    public string AppeasementMemoryTextEn { get; } = "The player appeased allied soldiers with discipline, compensation, or future loot arrangements, avoiding morale deductions from mercy/relief routes.";

    public string AppeasementMessageText { get; } = SiegeAftermathCompatibilityTexts.Raw("appease_done_msg");
    public string AppeasementMessageTextEn { get; } = "[Siege aftermath] You appeased the soldiers; discontent over foregoing pillage was calmed, no morale deducted.";

    public uint AppeasementMessageColor { get; } = 0xFFB6F7A8u;

    public string PenaltyMemoryTitle { get; } = SiegeAftermathCompatibilityTexts.Raw("appease_morale_title");
    public string PenaltyMemoryTitleEn { get; } = "Morale";

    public string PenaltyMemoryText { get; } = SiegeAftermathCompatibilityTexts.Raw("appease_penalty_memory");
    public string PenaltyMemoryTextEn { get; } = "The player did not appease soldiers discontented over missed loot before departure; party morale deducted by 20.";

    public string PenaltyMessageText { get; } = SiegeAftermathCompatibilityTexts.Raw("appease_penalty_msg");
    public string PenaltyMessageTextEn { get; } = "[Siege aftermath] Soldiers remain resentful over missed loot; party morale -20.";

    public uint PenaltyMessageColor { get; } = 0xFFFF7777u;

    public string BuildNeedMemoryText(string outcomeName)
        => BuildNeedMemoryText(outcomeName, false);

    public string BuildNeedMemoryText(string outcomeName, bool isEnglish)
    {
        string label = string.IsNullOrWhiteSpace(outcomeName)
            ? (isEnglish ? "mercy disposition" : SiegeAftermathCompatibilityTexts.Raw("appease_mercy_disposition"))
            : outcomeName.Trim();
        if (isEnglish)
        {
            return "The player\u0027s choice of " + label + " triggered subtle discontent over missed loot; appeasing allied soldiers before departure avoids morale penalty.";
        }
        return SiegeAftermathCompatibilityTexts.Raw("appease_trigger_prefix") + label + SiegeAftermathCompatibilityTexts.Raw("appease_trigger_suffix");
    }
}
