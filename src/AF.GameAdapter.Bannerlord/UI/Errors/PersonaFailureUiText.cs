using TaleWorlds.Localization;

namespace AnimusForge;

internal static class PersonaFailureUiText
{
    internal static string NpcTitle => new TextObject("{=AF_UI_Persona_GenerationFailedTitle}NPC personality and background generation failed").ToString();
    internal static string PromotedPersonaTitle => new TextObject("{=AF_UI_Persona_PromotedGenerationFailedTitle}Promoted companion persona generation failed").ToString();
    internal static string PromotedSkillsTitle => new TextObject("{=AF_UI_Persona_PromotedSkillsFailedTitle}Promoted companion skill generation failed").ToString();
    internal static string ProjectFailureDetail(string diagnostic)
    {
        if (string.IsNullOrEmpty(diagnostic)) return diagnostic;
        if (diagnostic.StartsWith("NPC personality and background response parsing failed; the persona was not saved.", System.StringComparison.Ordinal))
            return new TextObject("{=AF_UI_PersonaFailure_NpcParse}NPC personality and background response parsing failed; the persona was not saved.").ToString() + diagnostic.Substring(82);
        if (diagnostic.StartsWith("NPC personality and background generation failed.", System.StringComparison.Ordinal))
            return new TextObject("{=AF_UI_PersonaFailure_NpcGenerate}NPC personality and background generation failed.").ToString() + diagnostic.Substring(49);
        if (diagnostic.StartsWith("Promoted companion persona response parsing failed; a local fallback persona will be used.", System.StringComparison.Ordinal))
            return new TextObject("{=AF_UI_PersonaFailure_PromotedParse}Promoted companion persona response parsing failed; a local fallback persona will be used.").ToString() + diagnostic.Substring(90);
        if (diagnostic.StartsWith("Promoted companion persona generation failed; a local fallback persona will be used.", System.StringComparison.Ordinal))
            return new TextObject("{=AF_UI_PersonaFailure_PromotedGenerate}Promoted companion persona generation failed; a local fallback persona will be used.").ToString() + diagnostic.Substring(84);
        if (diagnostic.StartsWith("Promoted companion skill response parsing failed; troop template skills will be kept.", System.StringComparison.Ordinal))
            return new TextObject("{=AF_UI_PersonaFailure_SkillsParse}Promoted companion skill response parsing failed; troop template skills will be kept.").ToString() + diagnostic.Substring(85);
        if (diagnostic.StartsWith("Promoted companion skill generation failed; troop template skills will be kept.", System.StringComparison.Ordinal))
            return new TextObject("{=AF_UI_PersonaFailure_SkillsGenerate}Promoted companion skill generation failed; troop template skills will be kept.").ToString() + diagnostic.Substring(79);
        return diagnostic;
    }
}
