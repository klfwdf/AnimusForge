using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyBool("{=AF_TERMINAL_UI_EnableExpandedNpcPersona_Title}Expanded NPC personas", Order = 2, RequireRestart = false,
        HintText = "{=AF_TERMINAL_UI_EnableExpandedNpcPersona_Hint}Default: on. Generates traits, motivations, values, fears, and speech style with new or regenerated personas. When off, expanded fields are hidden and excluded from model context. Successful generation or regeneration while off clears that character's previous expanded fields; switching off alone keeps stored data. Event-driven persona regeneration is controlled separately.")]
    [SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
    public bool EnableExpandedNpcPersona
    {
        get => _enableExpandedNpcPersona;
        set
        {
            if (_enableExpandedNpcPersona == value) return;
            _enableExpandedNpcPersona = value;
        }
    }

    // Read only the loaded owner; UI and prompt consumers must not initialize settings or prompts.
    internal static bool ExpandedNpcPersonaEnabledForRuntime => LoadedForSceneRuntime()?.EnableExpandedNpcPersona != false;

    private bool _enableExpandedNpcPersona = true;
}
