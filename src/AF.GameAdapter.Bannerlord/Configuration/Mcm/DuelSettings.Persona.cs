using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyBool("{=AF_TERMINAL_UI_EnableExpandedNpcPersona_Title}Expanded NPC personas", Order = 2, RequireRestart = false,
        HintText = "{=AF_TERMINAL_UI_EnableExpandedNpcPersona_Hint}Default: on. Generates traits, motivations, values, fears, and speech style with new or regenerated personas. Turning off generates only persona prose. Existing expanded data is kept and remains usable; event-driven full persona regeneration is controlled separately.")]
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

    private bool _enableExpandedNpcPersona = true;
}
