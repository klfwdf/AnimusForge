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

	[SettingPropertyInteger("成派不满阈值", 10, 100, "0", Order = 3, RequireRestart = false, HintText = "某家族累计不满达到该值后，才有机会牵头成立反对派。王室阵营的NPC家族在负面事件后达到该值，会立即撤回支持、转为中立；国王家族与玩家家族除外。默认 35。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarDiscontentThreshold { get; set; } = 35;

	[SettingPropertyInteger("最后通牒间隔（周）", 1, 8, "0", Order = 4, RequireRestart = false, HintText = "派系成立或诉求被拒后，隔多少周再次向国王递交最后通牒。默认 4。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarUltimatumDelayWeeks { get; set; } = 4;

	[SettingPropertyInteger("内战最长周数", 4, 30, "0", Order = 5, RequireRestart = false, HintText = "内战持续到该周数时强制结算结局。默认 12。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarMaxWarWeeks { get; set; } = 12;

	[SettingPropertyInteger("派系冷却周数", 0, 30, "0", Order = 6, RequireRestart = false, HintText = "派系解散或内战结束后，多少周内不再形成新派系。默认 8。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarCooldownWeeks { get; set; } = 8;

	[SettingPropertyInteger("同时存在的派系上限", 1, 4, "0", Order = 7, RequireRestart = false, HintText = "一个王国内可同时存在的派系数量。每个派系有各自的诉求、不满、最后通牒与内战。一个派系起兵后，其余派系进入冷却，暂停递交最后通牒。默认 3。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarMaxFactions { get; set; } = 3;

	[SettingPropertyBool("允许多个派系同时内战", Order = 8, RequireRestart = false, HintText = "关闭（默认）：同一时间只允许一个派系起兵，其余派系暂停，等内战结束后再继续。开启：每个派系可各自建立叛军王国，与王室同时开战。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public bool CivilWarAllowConcurrentWars { get; set; } = false;

	[SettingPropertyInteger("玩家手动起兵实力占比（%）", 0, 100, "0", Order = 9, RequireRestart = false, HintText = "玩家家族军力须达到所属王国正式政治家族总军力的此百分比，才能手动起兵或向派系领袖提议起兵。0 表示关闭实力门槛；默认 20%。玩家担任派系领袖时不会自动起兵，拒绝解散令也须满足此门槛。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarPlayerDetonationStrengthPercent { get; set; } = 20;

	[SettingPropertyInteger("起兵所需最少被拒次数", 1, 4, "0", Order = 10, RequireRestart = false, HintText = "派系的诉求至少被国王拒绝这么多次之后，才可能自动升级为内战（含事件触发的升级）。被拒次数达到 4 次仍未起兵的派系会解散，所以上限为 4。玩家手动起兵不受此限。默认 2。")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarMinRefusalsBeforeWar { get; set; } = 2;

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
			tuning.MaxFactions = System.Math.Max(1, System.Math.Min(4, settings.CivilWarMaxFactions));
			tuning.AllowConcurrentWars = settings.CivilWarAllowConcurrentWars;
			tuning.Randomness = CivilWarRules.Clamp(settings.CivilWarRandomness, 0f, 1f);
			tuning.DiscontentThreshold = System.Math.Max(10, settings.CivilWarDiscontentThreshold);
			tuning.PlayerDetonationStrengthPercent = System.Math.Max(0, System.Math.Min(100, settings.CivilWarPlayerDetonationStrengthPercent));
			tuning.UltimatumDelayWeeks = System.Math.Max(1, settings.CivilWarUltimatumDelayWeeks);
			tuning.MaxWarWeeks = System.Math.Min(30, System.Math.Max(tuning.MinWarWeeks + 1, settings.CivilWarMaxWarWeeks));
			tuning.CooldownWeeks = System.Math.Max(0, settings.CivilWarCooldownWeeks);
			tuning.MinRefusalsBeforeWar = System.Math.Max(1, System.Math.Min(tuning.MaxRefusals, settings.CivilWarMinRefusalsBeforeWar));
		}
		catch
		{
		}
		return tuning;
	}
}
