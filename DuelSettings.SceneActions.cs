using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

/// <summary>
/// SceneActions and battle-speech settings are part of the AF settings page.
/// The XihaiAction runtime reads a cached snapshot of this partial class; it no
/// longer registers a second MCM global-settings type.
/// </summary>
public partial class DuelSettings
{
	private const string SceneActionsRootGroup = "{=SAX_MCM_Group_General}18. 自然语言动作与阵前演讲";
	private const string SceneActionsGeneralGroup = SceneActionsRootGroup;
	private const string SceneActionsSpeechGroup = SceneActionsRootGroup + "/{=SAX_MCM_Group_Speech}阵前演讲";
	private const string SceneActionsStageGroup = SceneActionsSpeechGroup + "/{=SAX_MCM_Group_Stage}演讲者站位";
	private const string SceneActionsAudienceGroup = SceneActionsSpeechGroup + "/{=SAX_MCM_Group_Audience}听众回应";
	private const string SceneActionsAudienceVoiceGroup = SceneActionsAudienceGroup + "/{=SAX_MCM_Group_Voices}原生战吼";
	private const string SceneActionsAudienceReplyGroup = SceneActionsAudienceGroup + "/{=SAX_MCM_Group_Replies}士兵文字回应";
	private const string SceneActionsAdvanceGroup = SceneActionsAudienceGroup + "/{=SAX_MCM_Group_Advance}演讲后发令";
	private const string SceneActionsSafetyGroup = SceneActionsSpeechGroup + "/{=SAX_MCM_Group_Safety}安全与诊断";

	[SettingPropertyBool(
		"{=SAX_MCM_NaturalReplyActions}自然语言回复动作与演讲",
		Order = 0,
		RequireRestart = false,
		HintText = "{=SAX_MCM_NaturalReplyActions_Hint}统一控制自然语言动作和阵前演讲。关闭后隐藏并停用本组下的演讲、听众回应和安全选项；不会改变 AF 的其他对话功能。")]
	[SettingPropertyGroup(SceneActionsGeneralGroup, GroupOrder = 180)]
	public bool NaturalLanguageReplyActionsEnabled { get; set; } = true;

	[SettingPropertyBool(
		"{=SAX_MCM_BattleSpeechEnabled}启用阵前演讲",
		Order = 0,
		RequireRestart = false,
		IsToggle = true,
		HintText = "{=SAX_MCM_BattleSpeechEnabled_Hint}阵前演讲总开关。关闭后隐藏本组的 T 键演讲、正文长度、站位、听众回应和演讲后发令设置。")]
	[SettingPropertyGroup(SceneActionsSpeechGroup, GroupOrder = 181)]
	public bool BattleSpeechEnabled { get; set; } = true;

	[SettingPropertyBool(
		"{=SAX_MCM_TKeyBattleSpeechEnabled}允许 T 键自然语言进入演讲通道",
		Order = 1,
		RequireRestart = false,
		HintText = "{=SAX_MCM_TKeyBattleSpeechEnabled_Hint}T 键只接受玩家演讲；Y 键的演讲菜单仍由阵前演讲开关和当前战斗阶段控制。")]
	[SettingPropertyGroup(SceneActionsSpeechGroup)]
	public bool TKeyBattleSpeechEnabled { get; set; } = true;

	[SettingPropertyInteger(
		"{=SAX_MCM_ReplyMin}演讲正文最少字数",
		6, 160, "0", Order = 2, RequireRestart = false,
		HintText = "{=SAX_MCM_ReplyMin_Hint}限制 AF 生成的阵前演讲正文长度；实际显示与生成使用同一份快照。必须不大于最多字数。")]
	[SettingPropertyGroup(SceneActionsSpeechGroup)]
	public int ReplyMinimumChars { get; set; } = 60;

	[SettingPropertyInteger(
		"{=SAX_MCM_ReplyMax}演讲正文最多字数",
		6, 160, "0", Order = 3, RequireRestart = false,
		HintText = "{=SAX_MCM_ReplyMax_Hint}限制 AF 生成的阵前演讲正文长度；超过此上限的结果会被拒绝并按失败关闭处理。")]
	[SettingPropertyGroup(SceneActionsSpeechGroup)]
	public int ReplyMaximumChars { get; set; } = 160;

	[SettingPropertyBool(
		"{=SAX_MCM_NpcPositioning}演讲者走到阵前",
		Order = 0,
		RequireRestart = false,
		IsToggle = true,
		HintText = "{=SAX_MCM_NpcPositioning_Hint}NPC 演讲时先走到己方阵线前方并转向士兵；关闭后保留原地演讲，不使用脚本移动。")]
	[SettingPropertyGroup(SceneActionsStageGroup, GroupOrder = 182)]
	public bool NpcPositioningEnabled { get; set; } = true;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_FrontDistance}阵前距离",
		2f, 25f, "0.0 m", Order = 1, RequireRestart = false,
		HintText = "{=SAX_MCM_FrontDistance_Hint}NPC 相对己方阵线中心向前移动的距离；只在“演讲者走到阵前”开启时生效。")]
	[SettingPropertyGroup(SceneActionsStageGroup)]
	public float FrontDistanceMeters { get; set; } = 10f;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_ArrivalRadius}到位判定半径",
		0.5f, 4f, "0.0 m", Order = 2, RequireRestart = false,
		HintText = "{=SAX_MCM_ArrivalRadius_Hint}NPC 与目标站位相距不超过此半径即视为到位；只影响站位移动判定。")]
	[SettingPropertyGroup(SceneActionsStageGroup)]
	public float ArrivalRadiusMeters { get; set; } = 1.5f;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_MoveTimeout}移动超时",
		3f, 45f, "0.0 s", Order = 3, RequireRestart = false,
		HintText = "{=SAX_MCM_MoveTimeout_Hint}NPC 走向阵前的最长等待时间；超时会在当前位置安全开讲，不传送、不重复请求。")]
	[SettingPropertyGroup(SceneActionsStageGroup)]
	public float MovementTimeoutSeconds { get; set; } = 15f;

	// Kept hidden for old integrated profiles. Lateral pacing is intentionally disabled.
	public bool PacingEnabled { get; set; } = false;
	public bool MountedPacingEnabled { get; set; } = false;
	public bool InfantryPacingEnabled { get; set; } = false;
	public float PacingHalfWidthMeters { get; set; } = 2f;
	public float PacingMinimumIntervalSeconds { get; set; } = 2.5f;
	public float PacingMaximumIntervalSeconds { get; set; } = 4.5f;

	[SettingPropertyBool(
		"{=SAX_MCM_AlliedAudience}包含盟军听众",
		Order = 0,
		RequireRestart = false,
		HintText = "{=SAX_MCM_AlliedAudience_Hint}听众快照是否包含同侧盟军；盟军可以回应，但不会接受演讲后的玩家编队 Advance。")]
	[SettingPropertyGroup(SceneActionsAudienceGroup, GroupOrder = 183)]
	public bool IncludeAlliedAudience { get; set; } = true;

	[SettingPropertyInteger(
		"{=SAX_MCM_VisualResponders}最多播放动作的士兵",
		1, 128, "0", Order = 1, RequireRestart = false,
		HintText = "{=SAX_MCM_VisualResponders_Hint}演讲结束后最多抽取多少名士兵播放受控动作；采用确定性抽样和分批提交，避免全军同帧同步。")]
	[SettingPropertyGroup(SceneActionsAudienceGroup)]
	public int MaximumVisualResponders { get; set; } = 60;

	[SettingPropertyInteger(
		"{=SAX_MCM_VisualWave}每波动作人数",
		1, 16, "0", Order = 2, RequireRestart = false,
		HintText = "{=SAX_MCM_VisualWave_Hint}每一批提交的士兵动作人数；只是批次大小，不等于每 Tick 允许的桥接调用上限。")]
	[SettingPropertyGroup(SceneActionsAudienceGroup)]
	public int VisualWaveSize { get; set; } = 6;

	[SettingPropertyInteger(
		"{=SAX_MCM_TickBudget}每 Tick 最多提交动作数",
		1, 16, "0", Order = 3, RequireRestart = false,
		HintText = "{=SAX_MCM_TickBudget_Hint}每个 Mission Tick 最多提交多少个动作桥接调用；用于限制单帧负载，与每波人数独立。")]
	[SettingPropertyGroup(SceneActionsAudienceGroup)]
	public int MaximumVisualSubmissionsPerTick { get; set; } = 6;

	[SettingPropertyBool(
		"{=SAX_MCM_Voices}启用听众原生战吼",
		Order = 0,
		RequireRestart = false,
		IsToggle = true,
		HintText = "{=SAX_MCM_Voices_Hint}启用后在士兵回应阶段分批播放原生战吼；不调用 AF TTS。关闭后隐藏战吼人数、批次和间隔。")]
	[SettingPropertyGroup(SceneActionsAudienceVoiceGroup, GroupOrder = 184)]
	public bool AudienceVoicesEnabled { get; set; } = true;

	[SettingPropertyInteger(
		"{=SAX_MCM_VoiceCount}战吼士兵人数",
		0, 40, "0", Order = 1, RequireRestart = false,
		HintText = "{=SAX_MCM_VoiceCount_Hint}本场最多参与原生战吼的士兵数量；只影响声音规模和性能。")]
	[SettingPropertyGroup(SceneActionsAudienceVoiceGroup)]
	public int AudienceVoiceCount { get; set; } = 22;

	[SettingPropertyInteger(
		"{=SAX_MCM_VoiceWave}每波战吼人数",
		1, 12, "0", Order = 2, RequireRestart = false,
		HintText = "{=SAX_MCM_VoiceWave_Hint}每一波同时触发的原生战吼人数；越小越自然，也越分散性能峰值。")]
	[SettingPropertyGroup(SceneActionsAudienceVoiceGroup)]
	public int AudienceVoiceWaveSize { get; set; } = 3;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_VoiceInterval}战吼波次间隔",
		0.05f, 1f, "0.00 s", Order = 3, RequireRestart = false,
		HintText = "{=SAX_MCM_VoiceInterval_Hint}战吼批次之间的间隔；不影响士兵文字回应的随机间隔。")]
	[SettingPropertyGroup(SceneActionsAudienceVoiceGroup)]
	public float AudienceVoiceWaveIntervalSeconds { get; set; } = 0.18f;

	[SettingPropertyBool(
		"{=SAX_MCM_AudienceReplies}启用士兵文字回应",
		Order = 0,
		RequireRestart = false,
		IsToggle = true,
		HintText = "{=SAX_MCM_AudienceReplies_Hint}启用后让多名士兵用短句回应演讲；关闭后隐藏文字回应人数、批次、字数和间隔。")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup, GroupOrder = 185)]
	public bool AudienceRepliesEnabled { get; set; } = true;

	[SettingPropertyInteger(
		"{=SAX_MCM_AudienceReplyCount}文字回应士兵人数",
		0, 100, "0", Order = 1, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyCount_Hint}本场最多生成文字回应的士兵数量；每个士兵只提交一次请求。")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public int AudienceReplyCount { get; set; } = 24;

	[SettingPropertyInteger(
		"{=SAX_MCM_AudienceReplyWaveSize}每波文字回应人数上限",
		2, 20, "0", Order = 2, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyWaveSize_Hint}同一批最多提交的文字回应人数；是逻辑波次大小，不等于每 Tick 提交预算。")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public int AudienceReplyWaveSize { get; set; } = 5;

	[SettingPropertyInteger("{=SAX_MCM_AudienceReplyTickBudget}每 Tick 最多提交文字回应", 2, 20, "0", Order = 11, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyTickBudget_Hint}独立于每波人数的 Mission Tick 提交预算，用于限制桥接调用和单帧负载。")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public int MaximumAudienceReplySubmissionsPerTick { get; set; } = 8;

	[SettingPropertyInteger(
		"{=SAX_MCM_AudienceReplyMinimumChars}士兵回应最少字数",
		4, 80, "0", Order = 4, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyMinimumChars_Hint}每条士兵文字回应的最少字数；只影响回应生成与显示，不影响演讲正文。")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public int AudienceReplyMinimumChars { get; set; } = 8;

	[SettingPropertyInteger(
		"{=SAX_MCM_AudienceReplyMaximumChars}士兵回应最多字数",
		4, 80, "0", Order = 5, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyMaximumChars_Hint}每条士兵文字回应的最多字数；建议保持简短，避免多人同时显示造成阅读和性能压力。")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public int AudienceReplyMaximumChars { get; set; } = 24;

	public int AudienceReplyWaveDefaultsVersion { get; set; } = 0;
	public int CombatSpeechDefaultsVersion { get; set; } = 0;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_AudienceReplyMinInterval}文字回应最小随机间隔",
		0.1f, 0.5f, "0.00 s", Order = 6, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyMinInterval_Hint}连续文字回应波次的随机间隔下限；与每 Tick 提交预算共同限制请求峰值。")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public float AudienceReplyMinimumIntervalSeconds { get; set; } = 0.2f;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_AudienceReplyMaxInterval}文字回应最大随机间隔",
		0.1f, 0.5f, "0.00 s", Order = 7, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyMaxInterval_Hint}连续文字回应波次的随机间隔上限；必须不小于最小随机间隔。")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public float AudienceReplyMaximumIntervalSeconds { get; set; } = 0.5f;

	public float AudienceReplyIntervalSeconds { get; set; } = 1.1f;

	[SettingPropertyBool(
		"{=SAX_MCM_Advance}演讲后播放发令并 Advance",
		Order = 0,
		RequireRestart = false,
		IsToggle = true,
		HintText = "{=SAX_MCM_Advance_Hint}演讲和听众回应完成后，演讲者播放发令动作，再向玩家直属编队下达 Advance；盟军只回应，不接受该命令。")]
	[SettingPropertyGroup(SceneActionsAdvanceGroup, GroupOrder = 186)]
	public bool TacticalAdvanceEnabled { get; set; } = true;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_AdvanceDelay}发令后 Advance 延迟",
		1.5f, 5f, "0.0 s", Order = 1, RequireRestart = false,
		HintText = "{=SAX_MCM_AdvanceDelay_Hint}发令动作完成后等待多久再向玩家直属编队下达 Advance；只在上方开关开启时生效。")]
	[SettingPropertyGroup(SceneActionsAdvanceGroup)]
	public float TacticalAdvanceDelaySeconds { get; set; } = 1.8f;

	[SettingPropertyFloatingInteger("{=SAX_MCM_EnemyRadius}战斗模式近敌半径", 5f, 75f, "0.0 m", Order = 0, RequireRestart = false,
		HintText = "{=SAX_MCM_EnemyRadius_Hint}敌人进入该半径后演讲继续但留在原地；士兵只可文字回应，抑制演讲者/听众动作、战吼、发令动作和 Advance。")]
	[SettingPropertyGroup(SceneActionsSafetyGroup, GroupOrder = 187)]
	public float EnemyInterruptRadiusMeters { get; set; } = 10f;

	[SettingPropertyBool(
		"{=SAX_MCM_Notifications}屏幕通知",
		Order = 1,
		RequireRestart = false,
		HintText = "{=SAX_MCM_Notifications_Hint}显示演讲开始、取消和结束等状态提示；关闭只影响 UI，不影响演讲执行。")]
	[SettingPropertyGroup(SceneActionsSafetyGroup)]
	public bool ScreenNotifications { get; set; } = true;

	[SettingPropertyBool(
		"{=SAX_MCM_Diagnostics}详细诊断日志",
		Order = 2,
		RequireRestart = false,
		HintText = "{=SAX_MCM_Diagnostics_Hint}写入自然语言动作和阵前演讲的诊断日志；关闭可减少日志 I/O，不改变功能。")]
	[SettingPropertyGroup(SceneActionsSafetyGroup)]
	public bool DiagnosticsEnabled { get; set; }

	// A hidden, monotonic migration marker. It lives in AnimusForge's settings file,
	// so the old XihaiAction json2 file is imported only once.
	public int SceneActionsMcmMigrationVersion { get; set; } = 0;

	// Hidden compatibility switches retained for old callers/configuration names.
	public bool Enabled { get; set; } = true;
	public bool ActionsEnabled { get; set; } = true;
	public bool PlayerInputEnabled { get; set; } = true;
	public bool NpcInputEnabled { get; set; } = true;
	public bool DualChannelEnabled { get; set; } = true;
	public bool AfClassifierEnabled { get; set; } = true;
	public bool NaturalSpeechTriggerEnabled { get; set; } = true;
	public bool SpeechTriggerClassifierEnabled { get; set; } = true;
	public bool SpeechSemanticClassifierEnabled { get; set; } = true;
	public bool Kneel { get; set; } = true;
	public bool StandUp { get; set; } = true;
	public bool Xihai { get; set; } = true;
	public bool Cheer { get; set; } = true;
	public bool Applaud { get; set; } = true;
	public bool Respect { get; set; } = true;
	public bool Threat { get; set; } = true;
	public bool Surrender { get; set; } = true;
	public bool Laugh { get; set; } = true;
	public bool Point { get; set; } = true;
	public bool Rage { get; set; } = true;
	public bool Fear { get; set; } = true;
	public bool Disappointed { get; set; } = true;
	public bool Challenge { get; set; } = true;
	public bool Search { get; set; } = true;
	public bool Dance { get; set; } = true;
	public bool Greet { get; set; } = true;
	public bool Agree { get; set; } = true;
	public bool Disagree { get; set; } = true;
	public bool Unsure { get; set; } = true;
	public bool Explain { get; set; } = true;
	public bool Promise { get; set; } = true;
	public bool CrossArms { get; set; } = true;
	public bool DeepBow { get; set; } = true;
	public bool Command { get; set; } = true;
	public bool FollowMe { get; set; } = true;
	public bool CutThroat { get; set; } = true;
}
