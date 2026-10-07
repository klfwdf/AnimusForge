using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyBool("取消繁荣度耗粮", Order = 5, RequireRestart = false,
        HintText = "默认关闭，保留原版繁荣度带来的粮食消耗。开启后，玩家和 NPC 的城市、城堡不再因繁荣度消耗粮食；驻军耗粮、围城、供粮和政策粮食变化仍保留。与数值上限、粮仓容量开关独立，保存后无需重启，再次关闭后恢复原版繁荣耗粮。仅适用于使用原版粮食计算的模型。")]
    [SettingPropertyGroup("16. 政策系统", GroupOrder = 0)]
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
