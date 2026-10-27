using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

public partial class DuelSettings
{
    public const int DefaultShoutMaxRelayTurns = 8;
    private int _shoutMaxRelayTurns = DefaultShoutMaxRelayTurns;

    [SettingPropertyInteger("{=AF_MCM_Shout_MaxRelayTurns}Maximum scene-shout relay turns", 1, 8, "0", Order = 16, RequireRestart = false,
        HintText = "{=AF_MCM_Shout_MaxRelayTurns_Hint}Maximum total NPC speaking turns after one player shout, including the first reply. Repeated speakers count again. Set to 1 for only the first reply. Range: 1-8; default: 8. Changes apply to the next shout; characters may end the relay earlier.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public int ShoutMaxRelayTurns
    {
        get => _shoutMaxRelayTurns;
        set => _shoutMaxRelayTurns = System.Math.Max(1, System.Math.Min(8, value));
    }
}
