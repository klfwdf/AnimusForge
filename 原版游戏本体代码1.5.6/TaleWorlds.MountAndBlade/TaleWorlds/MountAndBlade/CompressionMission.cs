using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class CompressionMission
{
	public static CompressionInfo.Float DebugScaleValueCompressionInfo;

	public static CompressionInfo.Integer AgentCompressionInfo;

	public static CompressionInfo.Integer WeaponAttachmentIndexCompressionInfo;

	public static CompressionInfo.Integer AgentOffsetCompressionInfo;

	public static CompressionInfo.Integer AgentHealthCompressionInfo;

	public static CompressionInfo.Integer AgentControllerCompressionInfo;

	public static CompressionInfo.Integer TeamCompressionInfo;

	public static CompressionInfo.Integer TeamSideCompressionInfo;

	public static CompressionInfo.Integer RoundEndReasonCompressionInfo;

	public static CompressionInfo.Integer TeamScoreCompressionInfo;

	public static CompressionInfo.Integer FactionCompressionInfo;

	public static CompressionInfo.Integer MissionOrderTypeCompressionInfo;

	public static CompressionInfo.Integer MissionRoundCountCompressionInfo;

	public static CompressionInfo.Integer MissionRoundStateCompressionInfo;

	public static CompressionInfo.Integer RoundTimeCompressionInfo;

	public static CompressionInfo.Integer SelectedTroopIndexCompressionInfo;

	public static CompressionInfo.Integer MissileCompressionInfo;

	public static CompressionInfo.Float MissileSpeedCompressionInfo;

	public static CompressionInfo.Integer MissileCollisionReactionCompressionInfo;

	public static CompressionInfo.Integer FlagCapturePointIndexCompressionInfo;

	public static CompressionInfo.Integer FlagpoleIndexCompressionInfo;

	public static CompressionInfo.Float FlagCapturePointDurationCompressionInfo;

	public static CompressionInfo.Float FlagProgressCompressionInfo;

	public static CompressionInfo.Float FlagClassicProgressCompressionInfo;

	public static CompressionInfo.Integer FlagDirectionEnumCompressionInfo;

	public static CompressionInfo.Float FlagSpeedCompressionInfo;

	public static CompressionInfo.Integer FlagCaptureResultCompressionInfo;

	public static CompressionInfo.Integer UsableGameObjectDestructionStateCompressionInfo;

	public static CompressionInfo.Float UsableGameObjectHealthCompressionInfo;

	public static CompressionInfo.Float UsableGameObjectBlowMagnitude;

	public static CompressionInfo.Float UsableGameObjectBlowDirection;

	public static CompressionInfo.Float CapturePointProgressCompressionInfo;

	public static CompressionInfo.Integer ItemSlotCompressionInfo;

	public static CompressionInfo.Integer WieldSlotCompressionInfo;

	public static CompressionInfo.Integer ItemDataCompressionInfo;

	public static CompressionInfo.Integer WeaponReloadPhaseCompressionInfo;

	public static CompressionInfo.Integer WeaponUsageIndexCompressionInfo;

	public static CompressionInfo.Integer TauntIndexCompressionInfo;

	public static CompressionInfo.Integer BarkIndexCompressionInfo;

	public static CompressionInfo.Integer UsageDirectionCompressionInfo;

	public static CompressionInfo.Float SpawnedItemVelocityCompressionInfo;

	public static CompressionInfo.Float SpawnedItemAngularVelocityCompressionInfo;

	public static CompressionInfo.UnsignedInteger SpawnedItemWeaponSpawnFlagCompressionInfo;

	public static CompressionInfo.Integer RangedSiegeWeaponAmmoCompressionInfo;

	public static CompressionInfo.Integer RangedSiegeWeaponAmmoIndexCompressionInfo;

	public static CompressionInfo.Integer RangedSiegeWeaponStateCompressionInfo;

	public static CompressionInfo.Integer SiegeLadderStateCompressionInfo;

	public static CompressionInfo.Integer BatteringRamStateCompressionInfo;

	public static CompressionInfo.Integer SiegeLadderAnimationStateCompressionInfo;

	public static CompressionInfo.Float SiegeMachineComponentAngularSpeedCompressionInfo;

	public static CompressionInfo.Integer SiegeTowerGateStateCompressionInfo;

	public static CompressionInfo.Integer NumberOfPacesCompressionInfo;

	public static CompressionInfo.Float WalkingSpeedLimitCompressionInfo;

	public static CompressionInfo.Float StepSizeCompressionInfo;

	public static CompressionInfo.Integer BoneIndexCompressionInfo;

	public static CompressionInfo.Integer AgentPrefabComponentIndexCompressionInfo;

	public static CompressionInfo.Integer AttachedWeaponsCompressionInfo;

	public static CompressionInfo.Integer MultiplayerPollRejectReasonCompressionInfo;

	public static CompressionInfo.Integer MultiplayerNotificationCompressionInfo;

	public static CompressionInfo.Integer MultiplayerNotificationParameterCompressionInfo;

	public static CompressionInfo.Integer PerkListIndexCompressionInfo;

	public static CompressionInfo.Integer PerkIndexCompressionInfo;

	public static CompressionInfo.Float FlagDominationMoraleCompressionInfo;

	public static CompressionInfo.Integer TdmGoldChangeCompressionInfo;

	public static CompressionInfo.Integer TdmGoldGainTypeCompressionInfo;

	public static CompressionInfo.Integer DuelAreaIndexCompressionInfo;

	public static CompressionInfo.Integer AutomatedBattleIndexCompressionInfo;

	public static CompressionInfo.Integer SiegeMoraleCompressionInfo;

	public static CompressionInfo.Integer SiegeMoralePerFlagCompressionInfo;

	public static CompressionInfo.Integer ActionSetCompressionInfo;

	public static CompressionInfo.Integer MonsterUsageSetCompressionInfo;

	public static CompressionInfo.Integer OrderTypeCompressionInfo;

	public static CompressionInfo.Integer FormationClassCompressionInfo;

	public static CompressionInfo.Float OrderPositionCompressionInfo;

	public static CompressionInfo.Integer SynchedMissionObjectReadableRecordTypeIndex;

	static CompressionMission()
	{
		DebugScaleValueCompressionInfo = new CompressionInfo.Float(0.5f, 1.5f, 13);
		AgentCompressionInfo = new CompressionInfo.Integer(-1, 11);
		WeaponAttachmentIndexCompressionInfo = new CompressionInfo.Integer(0, 8);
		AgentOffsetCompressionInfo = new CompressionInfo.Integer(0, 8);
		AgentHealthCompressionInfo = new CompressionInfo.Integer(-1, 11);
		AgentControllerCompressionInfo = new CompressionInfo.Integer(0, 2, maximumValueGiven: true);
		TeamCompressionInfo = new CompressionInfo.Integer(-1, 10);
		TeamSideCompressionInfo = new CompressionInfo.Integer(-1, 4);
		RoundEndReasonCompressionInfo = new CompressionInfo.Integer(-1, 2, maximumValueGiven: true);
		TeamScoreCompressionInfo = new CompressionInfo.Integer(-1023000, 1023000, maximumValueGiven: true);
		FactionCompressionInfo = new CompressionInfo.Integer(0, 4);
		MissionOrderTypeCompressionInfo = new CompressionInfo.Integer(-1, 5);
		MissionRoundCountCompressionInfo = new CompressionInfo.Integer(-1, 7);
		MissionRoundStateCompressionInfo = new CompressionInfo.Integer(-1, 5, maximumValueGiven: true);
		RoundTimeCompressionInfo = new CompressionInfo.Integer(0, MultiplayerOptions.OptionType.RoundTimeLimit.GetMaximumValue(), maximumValueGiven: true);
		SelectedTroopIndexCompressionInfo = new CompressionInfo.Integer(-1, 15, maximumValueGiven: true);
		MissileCompressionInfo = new CompressionInfo.Integer(0, 10);
		MissileSpeedCompressionInfo = new CompressionInfo.Float(0f, 12, 0.05f);
		MissileCollisionReactionCompressionInfo = new CompressionInfo.Integer(0, 3, maximumValueGiven: true);
		FlagCapturePointIndexCompressionInfo = new CompressionInfo.Integer(0, 3);
		FlagpoleIndexCompressionInfo = new CompressionInfo.Integer(0, 5, maximumValueGiven: true);
		FlagCapturePointDurationCompressionInfo = new CompressionInfo.Float(-1f, 14, 0.01f);
		FlagProgressCompressionInfo = new CompressionInfo.Float(-1f, 1f, 12);
		FlagClassicProgressCompressionInfo = new CompressionInfo.Float(0f, 1f, 11);
		FlagDirectionEnumCompressionInfo = new CompressionInfo.Integer(-1, 2, maximumValueGiven: true);
		FlagSpeedCompressionInfo = new CompressionInfo.Float(-1f, 14, 0.01f);
		FlagCaptureResultCompressionInfo = new CompressionInfo.Integer(0, 3, maximumValueGiven: true);
		UsableGameObjectDestructionStateCompressionInfo = new CompressionInfo.Integer(0, 3);
		UsableGameObjectHealthCompressionInfo = new CompressionInfo.Float(-1f, 18, 0.1f);
		UsableGameObjectBlowMagnitude = new CompressionInfo.Float(0f, DestructableComponent.MaxBlowMagnitude, 8);
		UsableGameObjectBlowDirection = new CompressionInfo.Float(-1f, 1f, 7);
		CapturePointProgressCompressionInfo = new CompressionInfo.Float(0f, 1f, 10);
		ItemSlotCompressionInfo = new CompressionInfo.Integer(0, 4, maximumValueGiven: true);
		WieldSlotCompressionInfo = new CompressionInfo.Integer(-1, 4, maximumValueGiven: true);
		ItemDataCompressionInfo = new CompressionInfo.Integer(0, 10);
		WeaponReloadPhaseCompressionInfo = new CompressionInfo.Integer(0, 9, maximumValueGiven: true);
		WeaponUsageIndexCompressionInfo = new CompressionInfo.Integer(0, 2);
		TauntIndexCompressionInfo = new CompressionInfo.Integer(0, TauntUsageManager.Instance.GetTauntItemCount() - 1, maximumValueGiven: true);
		BarkIndexCompressionInfo = new CompressionInfo.Integer(0, SkinVoiceManager.VoiceType.MpBarks.Length - 1, maximumValueGiven: true);
		UsageDirectionCompressionInfo = new CompressionInfo.Integer(-1, 9, maximumValueGiven: true);
		SpawnedItemVelocityCompressionInfo = new CompressionInfo.Float(-50f, 50f, 12);
		SpawnedItemAngularVelocityCompressionInfo = new CompressionInfo.Float(-10f, 10f, 12);
		SpawnedItemWeaponSpawnFlagCompressionInfo = new CompressionInfo.UnsignedInteger(0u, EnumHelper.GetCombinedUIntEnumFlagsValue(typeof(Mission.WeaponSpawnFlags)), maximumValueGiven: true);
		RangedSiegeWeaponAmmoCompressionInfo = new CompressionInfo.Integer(0, 7);
		RangedSiegeWeaponAmmoIndexCompressionInfo = new CompressionInfo.Integer(0, 3);
		RangedSiegeWeaponStateCompressionInfo = new CompressionInfo.Integer(0, 8, maximumValueGiven: true);
		SiegeLadderStateCompressionInfo = new CompressionInfo.Integer(0, 9, maximumValueGiven: true);
		BatteringRamStateCompressionInfo = new CompressionInfo.Integer(0, 2, maximumValueGiven: true);
		SiegeLadderAnimationStateCompressionInfo = new CompressionInfo.Integer(0, 2, maximumValueGiven: true);
		SiegeMachineComponentAngularSpeedCompressionInfo = new CompressionInfo.Float(-20f, 20f, 12);
		SiegeTowerGateStateCompressionInfo = new CompressionInfo.Integer(0, 3, maximumValueGiven: true);
		NumberOfPacesCompressionInfo = new CompressionInfo.Integer(0, 3);
		WalkingSpeedLimitCompressionInfo = new CompressionInfo.Float(-0.01f, 9, 0.01f);
		StepSizeCompressionInfo = new CompressionInfo.Float(-0.01f, 7, 0.01f);
		BoneIndexCompressionInfo = new CompressionInfo.Integer(0, 63, maximumValueGiven: true);
		AgentPrefabComponentIndexCompressionInfo = new CompressionInfo.Integer(0, 4);
		AttachedWeaponsCompressionInfo = new CompressionInfo.Integer(-1, 11);
		MultiplayerPollRejectReasonCompressionInfo = new CompressionInfo.Integer(0, 3, maximumValueGiven: true);
		MultiplayerNotificationCompressionInfo = new CompressionInfo.Integer(0, MultiplayerGameNotificationsComponent.NotificationCount, maximumValueGiven: true);
		MultiplayerNotificationParameterCompressionInfo = new CompressionInfo.Integer(-1, 8);
		PerkListIndexCompressionInfo = new CompressionInfo.Integer(0, 2);
		PerkIndexCompressionInfo = new CompressionInfo.Integer(0, 4);
		FlagDominationMoraleCompressionInfo = new CompressionInfo.Float(-1f, 8, 0.01f);
		TdmGoldChangeCompressionInfo = new CompressionInfo.Integer(0, 2000, maximumValueGiven: true);
		TdmGoldGainTypeCompressionInfo = new CompressionInfo.Integer(0, 12);
		DuelAreaIndexCompressionInfo = new CompressionInfo.Integer(0, 4);
		AutomatedBattleIndexCompressionInfo = new CompressionInfo.Integer(0, 10, maximumValueGiven: true);
		SiegeMoraleCompressionInfo = new CompressionInfo.Integer(0, 1440, maximumValueGiven: true);
		SiegeMoralePerFlagCompressionInfo = new CompressionInfo.Integer(0, 90, maximumValueGiven: true);
		OrderTypeCompressionInfo = new CompressionInfo.Integer(0, 41, maximumValueGiven: true);
		FormationClassCompressionInfo = new CompressionInfo.Integer(-1, 10, maximumValueGiven: true);
		OrderPositionCompressionInfo = new CompressionInfo.Float(-100000f, 100000f, 24);
		SynchedMissionObjectReadableRecordTypeIndex = new CompressionInfo.Integer(-1, 8);
	}
}
