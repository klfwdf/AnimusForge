using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace AnimusForge.CoupSystem;

public sealed class CoupSettings : AttributeGlobalSettings<CoupSettings>
{
    public override string Id => "AnimusForge_Coup_v1";
    public override string DisplayName => "AnimusForge - 宣权篡位";
    public override string FolderName => "AnimusForge_Coup";
    public override string FormatType => "json";

    [SettingPropertyBool("启用宣权篡位", Order = 0, RequireRestart = false, HintText = "在本国国王所在城镇发动武装政变。街道最多60人，大厅最多20人。关闭只禁止新发起，已有战斗安全收尾。")]
    [SettingPropertyGroup("武装政变")]
    public bool Enabled { get; set; } = true;

    internal static bool IsEnabled => Instance?.Enabled ?? true;
}
