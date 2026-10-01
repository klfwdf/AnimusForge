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

    [SettingPropertyInteger("最低家族等级", 0, 6, "0 级", Order = 0, RequireRestart = false, HintText = "默认4级，0表示不限制。仅限制新政变，确认选兵时复核；正式发动后不再追检。")]
    [SettingPropertyGroup("发动条件")]
    public int MinimumClanTier { get; set; } = 4;

    [SettingPropertyInteger("最低影响力", 0, 5000, "0", Order = 1, RequireRestart = false, HintText = "默认300，0表示不限制。只检查、不扣除。打开确认窗时固定门槛，确认选兵时复核；正式发动后不再追检。")]
    [SettingPropertyGroup("发动条件")]
    public int MinimumInfluence { get; set; } = 300;

    [SettingPropertyInteger("最低突击队人数", 1, 120, "0 人", Order = 2, RequireRestart = false, HintText = "默认60名健康普通士兵，必须实际选中，并另留1人接应；玩家、英雄及伤兵不计入。不得超过街道上限。大厅从幸存者中至少选1人，不受本门槛限制。")]
    [SettingPropertyGroup("发动条件")]
    public int MinimumTroops { get; set; } = 60;

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
    internal CoupEntryRequirements CaptureEntryRequirements() => CoupEntryRequirements.Normalize(MinimumClanTier, MinimumInfluence, MinimumTroops);
    internal static CoupEntryRequirements CaptureAdmissionForNewCoup() => Instance?.CaptureEntryRequirements() ?? CoupEntryRequirements.Normalize(4, 300, 60);

    internal static bool CheckNewCoupRequirements(int tier, float influence, int healthyRegulars, out string reason)
    {
        var settings = Instance;
        return CoupEntryRequirements.Evaluate(
            CoupEntryRequirements.Clamp(settings?.MinimumClanTier ?? 4, 0, 6),
            CoupEntryRequirements.Clamp(settings?.MinimumInfluence ?? 300, 0, 5000),
            CoupEntryRequirements.Clamp(settings?.MinimumTroops ?? 60, 1, 120),
            tier, influence, healthyRegulars, CoupEntryRequirements.Clamp(settings?.StreetAllyLimit ?? 60, 1, 120), out reason);
    }
}
