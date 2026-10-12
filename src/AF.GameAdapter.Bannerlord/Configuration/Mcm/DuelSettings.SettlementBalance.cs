using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyBool("{=AF_MCM_Settlement_DisableProsperity}Disable prosperity food consumption", Order = 5, RequireRestart = false,
        HintText = "{=AF_MCM_Settlement_DisableProsperity_Hint}When enabled, towns and castles no longer consume food from prosperity. Garrison, siege, supply, and policy food changes remain unchanged.")]
    [SettingPropertyGroup("{=AF_MCM_Group_PolicySystem}Policy system", GroupOrder = 0)]
    public bool DisableProsperityFoodConsumption { get; set; } = false;

    public static bool ShouldDisableProsperityFoodConsumption()
    {
        try
        {
            // Model reads must not initialize prompt files or run settings migrations.
            return GlobalSettings<DuelSettings>.Instance?.DisableProsperityFoodConsumption ?? false;
        }
        catch
        {
            return false;
        }
    }
}
