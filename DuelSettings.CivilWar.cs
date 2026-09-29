using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

// Civil war faction settings (v2). Kept in its own partial file so DuelSettings.cs stays untouched.
public partial class DuelSettings
{
	private const string CivilWarGroup = "12b. 内战派系";

	[SettingPropertyBool("启用内战派系", Order = 0, RequireRestart = false, HintText = "开启后，王国内部会因处决、封地落选、强推政策/战争/和约、领地遭劫等事件积累不满，形成反对派并向国王提出诉求；诉求被拒可能升级为内战（临时叛军王国 + 真实战争）。需要同时开启“启用王国稳定度与叛乱”。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public bool EnableCivilWarFactions { get; set; } = true;

	[SettingPropertyBool("玩家为国王时也形成派系", Order = 1, RequireRestart = false, HintText = "开启后，玩家统治的王国同样会形成反对派并向玩家递交最后通牒。默认关闭。若“玩家为国王时免疫稳定度叛乱”开启，派系不会升级为内战。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public bool CivilWarAllowPlayerKingdomFactions { get; set; } = false;

	[SettingPropertyFloatingInteger("结果随机度", 0f, 1f, "0.00", Order = 2, RequireRestart = false, HintText = "1 = 按概率掷骰（每个判定保底 5%~95%）；0 = 按期望值直接决定（概率过半即发生）。默认 1。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public float CivilWarRandomness { get; set; } = 1f;

	[SettingPropertyInteger("成派不满阈值", 10, 100, "0", Order = 3, RequireRestart = false, HintText = "某家族累计不满达到该值后，才有机会牵头成立反对派。默认 35。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarDiscontentThreshold { get; set; } = 35;

	[SettingPropertyInteger("最后通牒间隔（周）", 1, 8, "0", Order = 4, RequireRestart = false, HintText = "派系成立或诉求被拒后，隔多少周再次向国王递交最后通牒。默认 2。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarUltimatumDelayWeeks { get; set; } = 2;

	[SettingPropertyInteger("内战最长周数", 4, 30, "0", Order = 5, RequireRestart = false, HintText = "内战持续到该周数时强制结算结局。默认 12。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarMaxWarWeeks { get; set; } = 12;

	[SettingPropertyInteger("派系冷却周数", 0, 30, "0", Order = 6, RequireRestart = false, HintText = "派系解散或内战结束后，多少周内不再形成新派系。默认 8。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarCooldownWeeks { get; set; } = 8;

	public static bool IsCivilWarFactionsEnabled()
	{
		try { return IsKingdomStabilityAndRebellionEnabled() && (GetSettings()?.EnableCivilWarFactions ?? true); }
		catch { return false; }
	}

	public static bool IsCivilWarPlayerKingdomFactionsAllowed()
	{
		try { return GetSettings()?.CivilWarAllowPlayerKingdomFactions ?? false; }
		catch { return false; }
	}

	// Built once per weekly advance; never read from hot paths.
	internal static CivilWarTuning BuildCivilWarTuning()
	{
		CivilWarTuning tuning = new CivilWarTuning();
		try
		{
			DuelSettings settings = GetSettings();
			if (settings == null) return tuning;
			tuning.Randomness = CivilWarRules.Clamp(settings.CivilWarRandomness, 0f, 1f);
			tuning.DiscontentThreshold = System.Math.Max(10, settings.CivilWarDiscontentThreshold);
			tuning.UltimatumDelayWeeks = System.Math.Max(1, settings.CivilWarUltimatumDelayWeeks);
			tuning.MaxWarWeeks = System.Math.Max(tuning.MinWarWeeks + 1, settings.CivilWarMaxWarWeeks);
			tuning.CooldownWeeks = System.Math.Max(0, settings.CivilWarCooldownWeeks);
		}
		catch
		{
		}
		return tuning;
	}
}
