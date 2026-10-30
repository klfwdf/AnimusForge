using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyBool("{=AF_TERMINAL_UI_EnableExpandedNpcPersona_Title}Expanded NPC personas", Order = 2, RequireRestart = false,
        HintText = "{=AF_TERMINAL_UI_EnableExpandedNpcPersona_Hint}Default: on. Generates and uses traits, motivations, values, fears, and speech style. Turning off uses only persona prose and pauses event-driven evolution. Stored expanded data is kept and becomes active again when re-enabled.")]
    [SettingPropertyGroup("{=AF_MCM_Prompt_Group}9. Prompt extensions")]
    public bool EnableExpandedNpcPersona
    {
        get => _enableExpandedNpcPersona;
        set
        {
            if (_enableExpandedNpcPersona == value) return;
            _enableExpandedNpcPersona = value;
            MyBehavior.NotifyPersonaEvolutionSettingChanged(this);
        }
    }

    private bool _enableExpandedNpcPersona = true;
}
