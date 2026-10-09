using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

/// <summary>
/// SceneActions and battle-speech settings are part of the AF settings page.
/// The XihaiAction runtime reads this single MCM object through its cached
/// reflection bridge; it no longer registers a second MCM global-settings type.
/// </summary>
public partial class DuelSettings
{
	private const string SceneActionsRootGroup = "{=SAX_MCM_Group_General}18. Natural Actions and Battle Speech";
	private const string SceneActionsGeneralGroup = SceneActionsRootGroup;
	private const string SceneActionsSpeechGroup = SceneActionsRootGroup + "/{=SAX_MCM_Group_Speech}Battle speech";
	private const string SceneActionsStageGroup = SceneActionsSpeechGroup + "/{=SAX_MCM_Group_Stage}Speaker staging";
	private const string SceneActionsAudienceGroup = SceneActionsSpeechGroup + "/{=SAX_MCM_Group_Audience}Audience response";
	private const string SceneActionsAudienceVoiceGroup = SceneActionsAudienceGroup + "/{=SAX_MCM_Group_Voices}Native battle cries";
	private const string SceneActionsAudienceReplyGroup = SceneActionsAudienceGroup + "/{=SAX_MCM_Group_Replies}Soldier spoken replies";
	private const string SceneActionsAdvanceGroup = SceneActionsAudienceGroup + "/{=SAX_MCM_Group_Advance}Post-speech order";
	private const string SceneActionsSafetyGroup = SceneActionsSpeechGroup + "/{=SAX_MCM_Group_Safety}Safety and diagnostics";

	[SettingPropertyBool(
		"{=SAX_MCM_NaturalReplyActions}Natural-language reply actions",
		Order = 0,
		RequireRestart = false,
		HintText = "{=SAX_MCM_NaturalReplyActions_Hint}Controls natural-language action parsing and playback in ordinary AF dialogue and scene-shout replies. Disabling it keeps ordinary text replies and does not affect the independent battle-speech subsystem.")]
	[SettingPropertyGroup(SceneActionsGeneralGroup, GroupOrder = 180)]
	public bool NaturalLanguageReplyActionsEnabled { get; set; } = true;

	[SettingPropertyBool(
		"{=SAX_MCM_BattleSpeechEnabled}Enable battle speech",
		Order = 0,
		RequireRestart = false,
		IsToggle = true,
		HintText = "{=SAX_MCM_BattleSpeechEnabled_Hint}Master switch for battle speech. Disabling it hides T-key routing, speech length, staging, audience response, and post-speech order options.")]
	[SettingPropertyGroup(SceneActionsSpeechGroup, GroupOrder = 181)]
	public bool BattleSpeechEnabled { get; set; } = true;

	[SettingPropertyBool(
		"{=SAX_MCM_TKeyBattleSpeechEnabled}Enable battle-speech recognition from T-key scene shout",
		Order = 1,
		RequireRestart = false,
		HintText = "{=SAX_MCM_TKeyBattleSpeechEnabled_Hint}When enabled, T-key scene-shout text can enter the battle-speech channel. Y-key speech entries still require the main battle-speech switch and an eligible battle phase.")]
	[SettingPropertyGroup(SceneActionsSpeechGroup)]
	public bool TKeyBattleSpeechEnabled { get; set; } = true;

	[SettingPropertyInteger(
		"{=SAX_MCM_ReplyMin}Suggested battle speech minimum characters (6-160)",
		6, 160, "0", Order = 2, RequireRestart = false,
		HintText = "{=SAX_MCM_ReplyMin_Hint}Suggested lower bound sent to AF. A valid shorter speech is still used unchanged and does not trigger fallback. Must not exceed the suggested maximum.")]
	[SettingPropertyGroup(SceneActionsSpeechGroup)]
	public int ReplyMinimumChars { get; set; } = 60;

	[SettingPropertyInteger(
		"{=SAX_MCM_ReplyMax}Suggested battle speech maximum characters (6-160)",
		6, 160, "0", Order = 3, RequireRestart = false,
		HintText = "{=SAX_MCM_ReplyMax_Hint}Suggested upper bound sent to AF. A valid longer speech is still used unchanged, without truncation or fallback.")]
	[SettingPropertyGroup(SceneActionsSpeechGroup)]
	public int ReplyMaximumChars { get; set; } = 160;

	[SettingPropertyBool(
		"{=SAX_MCM_NpcPositioning}Move NPC to the front line",
		Order = 0,
		RequireRestart = false,
		IsToggle = true,
		HintText = "{=SAX_MCM_NpcPositioning_Hint}Moves an NPC speaker in front of the friendly line without forcing the natural facing of an infantry speaker, rider, or mount. Disabling keeps the speaker in place and takes no scripted movement control.")]
	[SettingPropertyGroup(SceneActionsStageGroup, GroupOrder = 182)]
	public bool NpcPositioningEnabled { get; set; } = true;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_FrontDistance}Distance in front of troops",
		2f, 25f, "0.0 m", Order = 1, RequireRestart = false,
		HintText = "{=SAX_MCM_FrontDistance_Hint}Distance the NPC moves forward from the friendly line center. Used only while speaker staging is enabled.")]
	[SettingPropertyGroup(SceneActionsStageGroup)]
	public float FrontDistanceMeters { get; set; } = 10f;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_ArrivalRadius}Arrival radius",
		0.5f, 4f, "0.0 m", Order = 2, RequireRestart = false,
		HintText = "{=SAX_MCM_ArrivalRadius_Hint}The NPC is considered in position inside this radius. This affects only staging arrival detection.")]
	[SettingPropertyGroup(SceneActionsStageGroup)]
	public float ArrivalRadiusMeters { get; set; } = 1.5f;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_MoveTimeout}Movement timeout",
		3f, 45f, "0.0 s", Order = 3, RequireRestart = false,
		HintText = "{=SAX_MCM_MoveTimeout_Hint}Maximum staging wait. On timeout the NPC safely speaks from the current position; no teleport and no duplicate request.")]
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
		"{=SAX_MCM_AlliedAudience}Include allied troops",
		Order = 0,
		RequireRestart = false,
		HintText = "{=SAX_MCM_AlliedAudience_Hint}Includes same-side allies in the frozen audience. Allies may respond but never receive the player's post-speech Advance order.")]
	[SettingPropertyGroup(SceneActionsAudienceGroup, GroupOrder = 183)]
	public bool IncludeAlliedAudience { get; set; } = true;

	[SettingPropertyInteger(
		"{=SAX_MCM_VisualResponders}Soldiers responding with actions",
		1, 128, "0", Order = 1, RequireRestart = false,
		HintText = "{=SAX_MCM_VisualResponders_Hint}Maximum soldiers sampled for controlled response actions. Sampling is deterministic and submissions are batched to avoid full-army frame synchronization.")]
	[SettingPropertyGroup(SceneActionsAudienceGroup)]
	public int MaximumVisualResponders { get; set; } = 60;

	[SettingPropertyInteger(
		"{=SAX_MCM_VisualWave}Action responders per wave",
		1, 16, "0", Order = 2, RequireRestart = false,
		HintText = "{=SAX_MCM_VisualWave_Hint}Soldier actions in one logical wave. This is a wave size, not the per-Mission-tick bridge-call budget.")]
	[SettingPropertyGroup(SceneActionsAudienceGroup)]
	public int VisualWaveSize { get; set; } = 6;

	[SettingPropertyInteger(
		"{=SAX_MCM_TickBudget}Maximum actions submitted per tick",
		1, 16, "0", Order = 3, RequireRestart = false,
		HintText = "{=SAX_MCM_TickBudget_Hint}Maximum action bridge calls submitted per Mission tick. Limits frame load independently from the logical wave size.")]
	[SettingPropertyGroup(SceneActionsAudienceGroup)]
	public int MaximumVisualSubmissionsPerTick { get; set; } = 6;

	[SettingPropertyBool(
		"{=SAX_MCM_Voices}Native audience voices",
		Order = 0,
		RequireRestart = false,
		IsToggle = true,
		HintText = "{=SAX_MCM_Voices_Hint}Plays native battle cries in batches during audience response without AF TTS. Disabling hides battle-cry count, wave, and interval options.")]
	[SettingPropertyGroup(SceneActionsAudienceVoiceGroup, GroupOrder = 184)]
	public bool AudienceVoicesEnabled { get; set; } = true;

	[SettingPropertyInteger(
		"{=SAX_MCM_VoiceCount}Audience voice count",
		0, 24, "0", Order = 1, RequireRestart = false,
		HintText = "{=SAX_MCM_VoiceCount_Hint}Maximum soldiers using native battle cries in this speech. Affects sound scale and performance only.")]
	[SettingPropertyGroup(SceneActionsAudienceVoiceGroup)]
	public int AudienceVoiceCount { get; set; } = 22;

	// Legacy profile fields retained for compatibility. Battle cries now fire
	// on the same actor and Tick as each cheer cue, so visual wave scheduling
	// owns batching and cadence.
	public int AudienceVoiceWaveSize { get; set; } = 3;

	public float AudienceVoiceWaveIntervalSeconds { get; set; } = 0.18f;

	[SettingPropertyBool(
		"{=SAX_MCM_AudienceReplies}Spoken replies from multiple soldiers",
		Order = 0,
		RequireRestart = false,
		IsToggle = true,
		HintText = "{=SAX_MCM_AudienceReplies_Hint}Lets multiple soldiers answer the speech with short lines. Disabling hides reply count, waves, length, and interval options.")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup, GroupOrder = 185)]
	public bool AudienceRepliesEnabled { get; set; } = true;

	[SettingPropertyInteger(
		"{=SAX_MCM_AudienceReplyCount}Soldiers giving spoken replies",
		0, 100, "0", Order = 1, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyCount_Hint}Maximum soldiers generating written replies for this speech. Each selected soldier is submitted only once.")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public int AudienceReplyCount { get; set; } = 24;

	[SettingPropertyInteger(
		"{=SAX_MCM_AudienceReplyWaveSize}Maximum soldiers replying together",
		2, 20, "0", Order = 2, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyWaveSize_Hint}Maximum written replies in one logical wave. Independent from the per-Mission-tick submission budget.")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public int AudienceReplyWaveSize { get; set; } = 5;

	[SettingPropertyInteger("{=SAX_MCM_AudienceReplyTickBudget}Maximum spoken replies submitted per tick", 2, 20, "0", Order = 11, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyTickBudget_Hint}Hard Mission-tick budget for spoken soldier reply bridge calls; independent from the logical wave size.")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public int MaximumAudienceReplySubmissionsPerTick { get; set; } = 8;

	[SettingPropertyInteger(
		"{=SAX_MCM_AudienceReplyMinimumChars}Suggested minimum spoken reply characters",
		4, 80, "0", Order = 4, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyMinimumChars_Hint}Suggested lower bound sent to AF for each soldier reply. A valid shorter reply is still displayed unchanged.")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public int AudienceReplyMinimumChars { get; set; } = 8;

	[SettingPropertyInteger(
		"{=SAX_MCM_AudienceReplyMaximumChars}Suggested maximum spoken reply characters",
		4, 80, "0", Order = 5, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyMaximumChars_Hint}Suggested upper bound sent to AF for each soldier reply. A valid longer reply is still displayed unchanged; shorter replies remain easier to read in a crowd.")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public int AudienceReplyMaximumChars { get; set; } = 24;

	public int AudienceReplyWaveDefaultsVersion { get; set; } = 0;
	public int CombatSpeechDefaultsVersion { get; set; } = 0;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_AudienceReplyMinInterval}Minimum random reply wave interval",
		0.1f, 0.5f, "0.00 s", Order = 6, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyMinInterval_Hint}Lower bound of the random delay between written-reply waves. Works with the per-tick budget to limit request spikes.")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public float AudienceReplyMinimumIntervalSeconds { get; set; } = 0.2f;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_AudienceReplyMaxInterval}Maximum random reply wave interval",
		0.1f, 0.5f, "0.00 s", Order = 7, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceReplyMaxInterval_Hint}Upper bound of the random delay between written-reply waves. Must not be below the minimum interval.")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public float AudienceReplyMaximumIntervalSeconds { get; set; } = 0.5f;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_AudienceResponseStartDelay}Audience response start delay",
		0.5f, 12f, "0.0 s", Order = 8, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceResponseStartDelay_Hint}Minimum delay after the speech body finishes before written soldier replies and native battle cries begin. Audience reactions never interrupt an unfinished speech.")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public float AudienceResponseStartDelaySeconds { get; set; } = 3f;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_AudienceFinalReactionHold}Final audience reaction hold",
		0.5f, 8f, "0.0 s", Order = 9, RequireRestart = false,
		HintText = "{=SAX_MCM_AudienceFinalReactionHold_Hint}Minimum hold after the final audience reaction is submitted before its action channels may be cleared and Advance is issued.")]
	[SettingPropertyGroup(SceneActionsAudienceReplyGroup)]
	public float AudienceFinalReactionHoldSeconds { get; set; } = 2.5f;

	public float AudienceReplyIntervalSeconds { get; set; } = 1.1f;

	[SettingPropertyBool(
		"{=SAX_MCM_Advance}Command gesture then order all formations to Advance",
		Order = 0,
		RequireRestart = false,
		IsToggle = true,
		HintText = "{=SAX_MCM_Advance_Hint}After the speech and audience response, the speaker performs a command gesture and orders the player's formations to Advance. Allies respond but are not ordered.")]
	[SettingPropertyGroup(SceneActionsAdvanceGroup, GroupOrder = 186)]
	public bool TacticalAdvanceEnabled { get; set; } = true;

	[SettingPropertyFloatingInteger(
		"{=SAX_MCM_AdvanceDelay}Advance delay after command gesture",
		1.5f, 5f, "0.0 s", Order = 1, RequireRestart = false,
		HintText = "{=SAX_MCM_AdvanceDelay_Hint}Delay between the command gesture and Advance orders to the player's formations. Used only while post-speech Advance is enabled.")]
	[SettingPropertyGroup(SceneActionsAdvanceGroup)]
	public float TacticalAdvanceDelaySeconds { get; set; } = 1.8f;

	[SettingPropertyFloatingInteger("{=SAX_MCM_EnemyRadius}Enemy interruption radius", 5f, 75f, "0.0 m", Order = 0, RequireRestart = false,
		HintText = "{=SAX_MCM_EnemyRadius_Hint}When an enemy enters this radius, the speech continues in place. Soldiers may show written replies, but speaker/audience actions, native battle cries, command gestures, and Advance are suppressed.")]
	[SettingPropertyGroup(SceneActionsSafetyGroup, GroupOrder = 187)]
	public float EnemyInterruptRadiusMeters { get; set; } = 10f;

	[SettingPropertyBool(
		"{=SAX_MCM_Notifications}Screen notifications",
		Order = 1,
		RequireRestart = false,
		HintText = "{=SAX_MCM_Notifications_Hint}Shows battle-speech start, cancellation, and completion notices. Disabling affects UI only, not execution.")]
	[SettingPropertyGroup(SceneActionsSafetyGroup)]
	public bool ScreenNotifications { get; set; } = true;

	[SettingPropertyBool(
		"{=SAX_MCM_Diagnostics}Detailed diagnostics",
		Order = 2,
		RequireRestart = false,
		HintText = "{=SAX_MCM_Diagnostics_Hint}Writes detailed natural-action and battle-speech diagnostics. Disabling reduces log I/O without changing behavior.")]
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
