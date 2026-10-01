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

    [SettingPropertyBool("启用宣权篡位", Order = 0, RequireRestart = false, HintText = "在本国国王所在城镇发动武装政变。关闭只禁止新发起，已有战斗安全收尾。人数与增援设置仅在下一次发动时生效。")]
    [SettingPropertyGroup("武装政变")]
    public bool Enabled { get; set; } = true;

    [SettingPropertyInteger("街道突击队人数上限", 1, 120, "0 人", Order = 0, RequireRestart = false, HintText = "默认60人，不含玩家。仅限实际健康普通士兵，至少留一人接应。仅影响下一次政变；人数较多会增加场景负担。")]
    [SettingPropertyGroup("突击队")]
    public int StreetAllyLimit { get; set; } = 60;

    [SettingPropertyInteger("大厅突击队人数上限", 1, 40, "0 人", Order = 1, RequireRestart = false, HintText = "默认20人，不含玩家。只能从街道幸存者中选择。仅影响下一次政变；人数较多可能导致大厅拥堵。")]
    [SettingPropertyGroup("突击队")]
    public int HallAllyLimit { get; set; } = 20;

    [SettingPropertyInteger("门口守卫人数上限", 1, 30, "0 人", Order = 0, RequireRestart = false, HintText = "默认10人。从真实健康守军中分配，不足时按实际人数；清除门卫后才能攻入大厅。仅影响下一次政变。")]
    [SettingPropertyGroup("守卫与增援")]
    public int GateGuardLimit { get; set; } = 10;

    [SettingPropertyInteger("大厅护卫人数上限", 1, 40, "0 人", Order = 1, RequireRestart = false, HintText = "默认20人，不含国王。优先从真实健康守军中分配，剩余兵员再分配门卫和街道守军。仅影响下一次政变。")]
    [SettingPropertyGroup("守卫与增援")]
    public int HallGuardLimit { get; set; } = 20;

    [SettingPropertyInteger("守军每波人数", 1, 60, "0 人", Order = 2, RequireRestart = false, HintText = "默认30人。街道和大厅每波最多生成的人数，实际受剩余守军限制，不凭空补兵。仅影响下一次政变。")]
    [SettingPropertyGroup("守卫与增援")]
    public int DefenderWaveSize { get; set; } = 30;

    [SettingPropertyInteger("守军增援间隔", 5, 120, "0 秒", Order = 3, RequireRestart = false, HintText = "默认30秒。首波随战斗开始生成，之后按间隔增援；仍受存活波数和剩余兵源限制。仅影响下一次政变。")]
    [SettingPropertyGroup("守卫与增援")]
    public int DefenderWaveIntervalSeconds { get; set; } = 30;

    [SettingPropertyInteger("同时存活的守军波数上限", 1, 4, "0 波", Order = 4, RequireRestart = false, HintText = "默认4波。一波仍有存活守军就占用一个名额，达到上限时等待空位再增援。仅影响下一次政变。")]
    [SettingPropertyGroup("守卫与增援")]
    public int MaxActiveDefenderWaves { get; set; } = 4;

    internal CoupBattleOptions CaptureBattleOptions() => CoupBattleOptions.Normalize(
        StreetAllyLimit, HallAllyLimit, GateGuardLimit, HallGuardLimit,
        DefenderWaveSize, DefenderWaveIntervalSeconds, MaxActiveDefenderWaves);

    internal static bool IsEnabled => Instance?.Enabled ?? true;
    internal static CoupBattleOptions CaptureForNewCoup() => Instance?.CaptureBattleOptions() ?? CoupBattleOptions.LegacyDefaults();
}
