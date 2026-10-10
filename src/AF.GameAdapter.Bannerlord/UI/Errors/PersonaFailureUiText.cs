using TaleWorlds.Localization;

namespace AnimusForge;

internal static class PersonaFailureUiText
{
    internal static string NpcTitle => new TextObject("{=AF_UI_Persona_GenerationFailedTitle}NPC personality and background generation failed").ToString();
    internal static string PromotedPersonaTitle => new TextObject("{=AF_UI_Persona_PromotedGenerationFailedTitle}Promoted companion persona generation failed").ToString();
    internal static string PromotedSkillsTitle => new TextObject("{=AF_UI_Persona_PromotedSkillsFailedTitle}Promoted companion skill generation failed").ToString();
}
