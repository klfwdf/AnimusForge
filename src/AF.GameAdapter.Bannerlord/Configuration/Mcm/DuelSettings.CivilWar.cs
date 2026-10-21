using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

// Civil war faction settings (v2). Kept in its own partial file so DuelSettings.cs stays untouched.
public partial class DuelSettings
{
	private const string CivilWarGroup = "{=AF_MCM_CivilWar_Group}Civil war factions";

	[SettingPropertyBool("{=AF_MCM_CivilWar_Enable}Enable civil war factions", Order = 0, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_Enable_Hint}Executions, fief decisions, imposed policies, wars, peace and raids build discontent. Opposition factions make demands and may rise after rejected demands, subject to cooldowns and the world war cap. This is the sole AF kingdom-uprising route; stability has no separate random rebellion. Kingdom Stability and Rebellion must also be enabled.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public bool EnableCivilWarFactions { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_CivilWar_PlayerKingdom}Allow factions when the player is king", Order = 1, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_PlayerKingdom_Hint}When enabled, kingdoms ruled by the player can also form opposition factions and deliver ultimatums. New ultimatums allow four weeks for a reply. Disabled by default. If player immunity to stability rebellions is enabled, these factions cannot escalate into civil war.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public bool CivilWarAllowPlayerKingdomFactions { get; set; } = false;

	[SettingPropertyFloatingInteger("{=AF_MCM_CivilWar_Randomness}Outcome randomness", 0f, 1f, "0.00", Order = 2, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_Randomness_Hint}1 = roll by probability (each check is clamped to 5%–95%); 0 = decide directly from the expected value (it occurs when probability exceeds 50%). Default: 1.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public float CivilWarRandomness { get; set; } = 1f;

	[SettingPropertyInteger("{=AF_MCM_CivilWar_DiscontentThreshold}Faction discontent threshold", 10, 100, "0", Order = 3, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_DiscontentThreshold_Hint}A clan can lead an opposition faction only after its accumulated discontent reaches this value. Royal-aligned NPC clans that reach it after a negative event immediately withdraw support and become neutral; the king’s clan and the player’s clan are exempt. Default: 35.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarDiscontentThreshold { get; set; } = 35;

	[SettingPropertyInteger("{=AF_MCM_CivilWar_UltimatumDelay}Ultimatum interval (weeks)", 1, 8, "0", Order = 4, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_UltimatumDelay_Hint}How many weeks after a faction forms or a demand is rejected before another ultimatum is sent to the king. Default: 4.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarUltimatumDelayWeeks { get; set; } = 4;

	[SettingPropertyInteger("{=AF_MCM_CivilWar_MaxWarWeeks}Maximum civil war duration (weeks)", 4, 30, "0", Order = 5, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_MaxWarWeeks_Hint}Force an outcome when a civil war reaches this many weeks. Default: 12.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarMaxWarWeeks { get; set; } = 12;

	[SettingPropertyInteger("{=AF_MCM_CivilWar_CooldownWeeks}Faction cooldown (weeks)", 0, 30, "0", Order = 6, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_CooldownWeeks_Hint}How many weeks after a faction is dissolved or a civil war ends before a new faction can form. Default: 8.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarCooldownWeeks { get; set; } = 8;

	[SettingPropertyInteger("{=AF_MCM_CivilWar_MaxFactions}Maximum simultaneous factions", 1, 4, "0", Order = 7, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_MaxFactions_Hint}Maximum number of factions that can exist in one kingdom at the same time. Each has its own demands, discontent, ultimatums, and civil war. When one faction rises, the others enter cooldown and pause ultimatums. Default: 3.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarMaxFactions { get; set; } = 3;

	[SettingPropertyBool("{=AF_MCM_CivilWar_ConcurrentWars}Allow concurrent civil wars", Order = 8, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_ConcurrentWars_Hint}Disabled (default): only one faction can rise at a time; the others pause until the civil war ends. Enabled: each faction can establish its own rebel kingdom and wage war against the royal house simultaneously.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public bool CivilWarAllowConcurrentWars { get; set; } = false;

	[SettingPropertyInteger("{=AF_MCM_CivilWar_PlayerStrengthPercent}Player uprising strength ratio (%)", 0, 100, "0", Order = 9, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_PlayerStrengthPercent_Hint}The player clan’s strength must reach this percentage of the total strength of the kingdom’s established political clans to rise manually or propose a rising to a faction leader. 0 disables the strength gate; default: 20%. A player faction leader does not rise automatically, and refusing a dissolution order still requires this threshold.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarPlayerDetonationStrengthPercent { get; set; } = 20;

	[SettingPropertyInteger("{=AF_MCM_CivilWar_MinRefusals}Minimum refusals before uprising", 1, 4, "0", Order = 10, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_MinRefusals_Hint}A faction’s demands must be rejected by the king at least this many times before it can automatically escalate to civil war, including event-triggered escalation. A faction that has not risen after four refusals is dissolved, so the maximum is 4. Manual player uprisings are exempt. Default: 2.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarMinRefusalsBeforeWar { get; set; } = 2;

	[SettingPropertyInteger("{=AF_MCM_CivilWar_WorldMaxWars}World rebellion cap", 1, 20, "0", Order = 11, RequireRestart = false, HintText = "{=AF_MCM_CivilWar_WorldMaxWars_Hint}When the global cap for civil-war factions currently fighting or waiting to establish a kingdom is reached, NPC factions pause their uprisings and resume when a slot opens. Player uprisings, approved proposals, and defiant uprisings are exempt from the cap but count toward it; registered coup wars also count, while starting a coup is not capped. Lowering the cap does not end existing wars. Native town rebellions are excluded. Default: 3 wars.")]
	[SettingPropertyGroup(CivilWarGroup, GroupOrder = 125)]
	public int CivilWarWorldMaxWars { get; set; } = 3;

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
			tuning.WorldMaxWars = System.Math.Max(1, System.Math.Min(20, settings.CivilWarWorldMaxWars));
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
