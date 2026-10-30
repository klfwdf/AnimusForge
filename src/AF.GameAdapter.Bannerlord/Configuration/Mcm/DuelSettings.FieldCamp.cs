using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class DuelSettings
{
	private const string FieldCampGroup = "{=AF_MCM_FieldCamp_Group}19. Field camp";

	[SettingPropertyBool("{=AF_MCM_FieldCamp_Enabled}Enable field camp", Order = 0, RequireRestart = false,
		HintText = "{=AF_MCM_FieldCamp_Enabled_Hint}Lets you pitch camp on the campaign map while travelling alone or leading an army. Disabling it ends any active camp.")]
	[SettingPropertyGroup(FieldCampGroup, GroupOrder = 190)]
	public bool FieldCampEnabled { get; set; } = true;

	[SettingPropertyText("{=AF_MCM_FieldCamp_Key}Field camp key (one uppercase letter)", -1, true, "", Order = 1, RequireRestart = false,
		HintText = "{=AF_MCM_FieldCamp_Key_Hint}Press on the campaign map to pitch camp, or to reopen the camp menu while camped. Default: Z.")]
	[SettingPropertyGroup(FieldCampGroup, GroupOrder = 190)]
	public string FieldCampKey { get; set; } = "Z";

	[SettingPropertyFloatingInteger("{=AF_MCM_FieldCamp_PitchHours}Hours to pitch camp", 0f, 6f, "0.0", Order = 2, RequireRestart = false,
		HintText = "{=AF_MCM_FieldCamp_PitchHours_Hint}Campaign hours spent setting up camp. Default: 1.")]
	[SettingPropertyGroup(FieldCampGroup, GroupOrder = 190)]
	public float FieldCampPitchHours { get; set; } = 1f;

	[SettingPropertyFloatingInteger("{=AF_MCM_FieldCamp_StrikeHours}Hours to strike camp", 0f, 6f, "0.0", Order = 3, RequireRestart = false,
		HintText = "{=AF_MCM_FieldCamp_StrikeHours_Hint}Campaign hours spent breaking camp before marching. Not charged when an encounter breaks the camp. Default: 1.")]
	[SettingPropertyGroup(FieldCampGroup, GroupOrder = 190)]
	public float FieldCampStrikeHours { get; set; } = 1f;

	[SettingPropertyFloatingInteger("{=AF_MCM_FieldCamp_HealMultiplier}Wounded recovery multiplier", 1f, 4f, "0.0", Order = 4, RequireRestart = false,
		HintText = "{=AF_MCM_FieldCamp_HealMultiplier_Hint}Wounded soldiers and heroes recover this many times faster than normal while camped. Default: 2.")]
	[SettingPropertyGroup(FieldCampGroup, GroupOrder = 190)]
	public float FieldCampHealMultiplier { get; set; } = 2f;

	[SettingPropertyFloatingInteger("{=AF_MCM_FieldCamp_MoralePerHour}Morale recovered per hour", 0f, 5f, "0.0", Order = 5, RequireRestart = false,
		HintText = "{=AF_MCM_FieldCamp_MoralePerHour_Hint}Morale each camped party regains every hour. Default: 1.")]
	[SettingPropertyGroup(FieldCampGroup, GroupOrder = 190)]
	public float FieldCampMoralePerHour { get; set; } = 1f;

	[SettingPropertyInteger("{=AF_MCM_FieldCamp_MoraleCap}Morale recovery cap", 0, 100, "0", Order = 6, RequireRestart = false,
		HintText = "{=AF_MCM_FieldCamp_MoraleCap_Hint}Resting alone does not raise morale above this value. Default: 70.")]
	[SettingPropertyGroup(FieldCampGroup, GroupOrder = 190)]
	public int FieldCampMoraleCap { get; set; } = 70;

	[SettingPropertyBool("{=AF_MCM_FieldCamp_MapVisuals}Show camps on the campaign map", Order = 7, RequireRestart = false,
		HintText = "{=AF_MCM_FieldCamp_MapVisuals_Hint}Places a tent cluster next to each camped party. Turn off if another mod conflicts with map visuals; camping still works.")]
	[SettingPropertyGroup(FieldCampGroup, GroupOrder = 190)]
	public bool FieldCampMapVisualsEnabled { get; set; } = true;
}

/// <summary>Clamped, allocation-free reads of the field camp settings.</summary>
internal static class FieldCampSettings
{
	internal const int MediumCampMen = 60;
	internal const int LargeCampMen = 200;

	private static DuelSettings S => GlobalSettings<DuelSettings>.Instance;

	internal static bool Enabled => S?.FieldCampEnabled ?? true;

	internal static bool MapVisualsEnabled => S?.FieldCampMapVisualsEnabled ?? true;

	internal static string KeyText => S?.FieldCampKey ?? "Z";

	internal static float PitchHours => MathF.Clamp(S?.FieldCampPitchHours ?? 1f, 0f, 6f);

	internal static float StrikeHours => MathF.Clamp(S?.FieldCampStrikeHours ?? 1f, 0f, 6f);

	internal static float HealMultiplier => MathF.Clamp(S?.FieldCampHealMultiplier ?? 2f, 1f, 4f);

	internal static float MoralePerHour => MathF.Clamp(S?.FieldCampMoralePerHour ?? 1f, 0f, 5f);

	internal static float MoraleCap => MathF.Clamp(S?.FieldCampMoraleCap ?? 70, 0, 100);
}
