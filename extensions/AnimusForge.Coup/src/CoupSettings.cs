using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace AnimusForge.CoupSystem;

public sealed class CoupSettings : AttributeGlobalSettings<CoupSettings>
{
    public override string Id => "AnimusForge_Coup_v1";
    public override string DisplayName => new TextObject("{=AF_COUP_DISPLAY}AnimusForge - Coup").ToString();
    public override string FolderName => "AnimusForge_Coup";
    public override string FormatType => "json";

    [SettingPropertyBool("{=AF_COUP_ENABLED}Enable Coup", Order = 0, RequireRestart = false, HintText = "{=AF_COUP_ENABLED_HINT}Launch an armed coup in the town where your kingdom's ruler resides. Disabling prevents new coups; ongoing battles finish safely. Troop and reinforcement settings apply to the next coup.")]
    [SettingPropertyGroup("{=AF_COUP_GROUP_MAIN}Armed Coup")]
    public bool Enabled { get; set; } = true;

    [SettingPropertyInteger("{=AF_COUP_MIN_CLAN_TIER}Minimum Clan Tier", 0, 6, "{=AF_COUP_UNIT_TIER}0 tier", Order = 0, RequireRestart = false, HintText = "{=AF_COUP_MIN_CLAN_TIER_HINT}Default 4; 0 means unrestricted. Applies only to new coups and is checked again when troops are confirmed.")]
    [SettingPropertyGroup("{=AF_COUP_GROUP_REQUIREMENTS}Entry Requirements")]
    public int MinimumClanTier { get; set; } = 4;

    [SettingPropertyInteger("{=AF_COUP_MIN_INFLUENCE}Minimum Influence", 0, 5000, "0", Order = 1, RequireRestart = false, HintText = "{=AF_COUP_MIN_INFLUENCE_HINT}Default 300; 0 means unrestricted. Checked but not spent. The threshold is fixed when the confirmation window opens and checked again when troops are confirmed.")]
    [SettingPropertyGroup("{=AF_COUP_GROUP_REQUIREMENTS}Entry Requirements")]
    public int MinimumInfluence { get; set; } = 300;

    [SettingPropertyInteger("{=AF_COUP_MIN_TROOPS}Minimum Assault Party Size", 1, 120, "{=AF_COUP_UNIT_TROOPS}0 troops", Order = 2, RequireRestart = false, HintText = "{=AF_COUP_MIN_TROOPS_HINT}Default 60 healthy regular soldiers must be selected, with one additional person left as a reserve. The player, heroes, and wounded do not count. Cannot exceed the street limit; the hall requires at least one survivor regardless of this threshold.")]
    [SettingPropertyGroup("{=AF_COUP_GROUP_REQUIREMENTS}Entry Requirements")]
    public int MinimumTroops { get; set; } = 60;

    [SettingPropertyInteger("{=AF_COUP_STREET_LIMIT}Street Assault Party Limit", 1, 120, "{=AF_COUP_UNIT_TROOPS}0 troops", Order = 0, RequireRestart = false, HintText = "{=AF_COUP_STREET_LIMIT_HINT}Default 60, excluding the player. Healthy regular soldiers only, with at least one reserve. Applies to the next coup; larger parties increase scene load.")]
    [SettingPropertyGroup("{=AF_COUP_GROUP_ASSAULT}Assault Parties")]
    public int StreetAllyLimit { get; set; } = 60;

    [SettingPropertyInteger("{=AF_COUP_HALL_LIMIT}Hall Assault Party Limit", 1, 40, "{=AF_COUP_UNIT_TROOPS}0 troops", Order = 1, RequireRestart = false, HintText = "{=AF_COUP_HALL_LIMIT_HINT}Default 20, excluding the player. Selected only from street survivors. Applies to the next coup; larger parties may crowd the hall.")]
    [SettingPropertyGroup("{=AF_COUP_GROUP_ASSAULT}Assault Parties")]
    public int HallAllyLimit { get; set; } = 20;

    [SettingPropertyInteger("{=AF_COUP_GATE_GUARD_LIMIT}Gate Guard Limit", 1, 30, "{=AF_COUP_UNIT_TROOPS}0 troops", Order = 0, RequireRestart = false, HintText = "{=AF_COUP_GATE_GUARD_LIMIT_HINT}Default 10. Assigned from healthy defenders; fewer are used when unavailable. The hall can be attacked after the gate guards are cleared. Applies to the next coup.")]
    [SettingPropertyGroup("{=AF_COUP_GROUP_GUARDS}Guards and Reinforcements")]
    public int GateGuardLimit { get; set; } = 10;

    [SettingPropertyInteger("{=AF_COUP_HALL_GUARD_LIMIT}Hall Guard Limit", 1, 40, "{=AF_COUP_UNIT_TROOPS}0 troops", Order = 1, RequireRestart = false, HintText = "{=AF_COUP_HALL_GUARD_LIMIT_HINT}Default 20, excluding the ruler. Assigned first from healthy defenders; remaining troops are assigned to gate and street guards. Applies to the next coup.")]
    [SettingPropertyGroup("{=AF_COUP_GROUP_GUARDS}Guards and Reinforcements")]
    public int HallGuardLimit { get; set; } = 20;

    [SettingPropertyInteger("{=AF_COUP_DEFENDER_WAVE_SIZE}Defenders per Wave", 1, 60, "{=AF_COUP_UNIT_TROOPS}0 troops", Order = 2, RequireRestart = false, HintText = "{=AF_COUP_DEFENDER_WAVE_SIZE_HINT}Default 30. Maximum defenders spawned per wave in the street and hall; limited by remaining defenders and never created from nothing. Applies to the next coup.")]
    [SettingPropertyGroup("{=AF_COUP_GROUP_GUARDS}Guards and Reinforcements")]
    public int DefenderWaveSize { get; set; } = 30;

    [SettingPropertyInteger("{=AF_COUP_DEFENDER_INTERVAL}Defender Reinforcement Interval", 5, 120, "{=AF_COUP_UNIT_SECONDS}0 sec", Order = 3, RequireRestart = false, HintText = "{=AF_COUP_DEFENDER_INTERVAL_HINT}Default 30 seconds. The first wave appears when combat starts, then reinforcements arrive at this interval, subject to active-wave and remaining-force limits. Applies to the next coup.")]
    [SettingPropertyGroup("{=AF_COUP_GROUP_GUARDS}Guards and Reinforcements")]
    public int DefenderWaveIntervalSeconds { get; set; } = 30;

    [SettingPropertyInteger("{=AF_COUP_MAX_WAVES}Maximum Active Defender Waves", 1, 4, "{=AF_COUP_UNIT_WAVES}0 waves", Order = 4, RequireRestart = false, HintText = "{=AF_COUP_MAX_WAVES_HINT}Default 4 waves. A wave occupies a slot while any defender remains alive; reinforcements wait for an open slot. Applies to the next coup.")]
    [SettingPropertyGroup("{=AF_COUP_GROUP_GUARDS}Guards and Reinforcements")]
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

