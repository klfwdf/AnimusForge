using System;

namespace TaleWorlds.MountAndBlade;

public static class CompressionBasic
{
	public const float MaxPossibleAbsValueForSecondMaxQuaternionComponent = 0.7071068f;

	public const float MaxPositionZForCompression = 2521f;

	public const float MaxPositionForCompression = 10385f;

	public const float MinPositionForCompression = -100f;

	public static CompressionInfo.Integer PingValueCompressionInfo;

	public static CompressionInfo.Integer LossValueCompressionInfo;

	public static CompressionInfo.Integer ServerPerformanceStateCompressionInfo;

	public static CompressionInfo.UnsignedInteger ColorCompressionInfo;

	public static CompressionInfo.Integer ItemDataValueCompressionInfo;

	public static CompressionInfo.Integer RandomSeedCompressionInfo;

	public static CompressionInfo.Float PositionCompressionInfo;

	public static CompressionInfo.Float LocalPositionCompressionInfo;

	public static CompressionInfo.Float LowResLocalPositionCompressionInfo;

	public static CompressionInfo.Float BigRangeLowResLocalPositionCompressionInfo;

	public static CompressionInfo.Integer PlayerCompressionInfo;

	public static CompressionInfo.UnsignedInteger PeerComponentCompressionInfo;

	public static CompressionInfo.UnsignedInteger GUIDCompressionInfo;

	public static CompressionInfo.Integer FlagsCompressionInfo;

	public static CompressionInfo.Integer GUIDIntCompressionInfo;

	public static CompressionInfo.Integer MissionObjectIDCompressionInfo;

	public static CompressionInfo.Float UnitVectorCompressionInfo;

	public static CompressionInfo.Float LowResRadianCompressionInfo;

	public static CompressionInfo.Float RadianCompressionInfo;

	public static CompressionInfo.Float HighResRadianCompressionInfo;

	public static CompressionInfo.Float ScaleCompressionInfo;

	public static CompressionInfo.Float LowResQuaternionCompressionInfo;

	public static CompressionInfo.Integer OmittedQuaternionComponentIndexCompressionInfo;

	public static CompressionInfo.Float ImpulseCompressionInfo;

	public static CompressionInfo.Integer AnimationKeyCompressionInfo;

	public static CompressionInfo.Float AnimationSpeedCompressionInfo;

	public static CompressionInfo.Float AnimationProgressCompressionInfo;

	public static CompressionInfo.Float VertexAnimationSpeedCompressionInfo;

	public static CompressionInfo.Integer PercentageCompressionInfo;

	public static CompressionInfo.Integer EntityChildCountCompressionInfo;

	public static CompressionInfo.Integer AgentHitDamageCompressionInfo;

	public static CompressionInfo.Integer AgentHitModifiedDamageCompressionInfo;

	public static CompressionInfo.Float AgentHitRelativeSpeedCompressionInfo;

	public static CompressionInfo.Integer AgentHitArmorCompressionInfo;

	public static CompressionInfo.Integer AgentHitBoneIndexCompressionInfo;

	public static CompressionInfo.Integer AgentHitBodyPartCompressionInfo;

	public static CompressionInfo.Integer AgentHitDamageTypeCompressionInfo;

	public static CompressionInfo.Integer RoundGoldAmountCompressionInfo;

	public static CompressionInfo.Integer DebugIntNonCompressionInfo;

	public static CompressionInfo.UnsignedLongInteger DebugULongNonCompressionInfo;

	public static CompressionInfo.Float AgentAgeCompressionInfo;

	public static CompressionInfo.Float FaceKeyDataCompressionInfo;

	public static CompressionInfo.Integer PlayerChosenBadgeCompressionInfo;

	public static CompressionInfo.Integer MaxNumberOfPlayersCompressionInfo;

	public static CompressionInfo.Integer MinNumberOfPlayersForMatchStartCompressionInfo;

	public static CompressionInfo.Integer MapTimeLimitCompressionInfo;

	public static CompressionInfo.Integer RoundTotalCompressionInfo;

	public static CompressionInfo.Integer RoundTimeLimitCompressionInfo;

	public static CompressionInfo.Integer WarmupTimeLimitCompressionInfo;

	public static CompressionInfo.Integer RoundPreparationTimeLimitCompressionInfo;

	public static CompressionInfo.Integer RespawnPeriodCompressionInfo;

	public static CompressionInfo.Integer GoldGainChangePercentageCompressionInfo;

	public static CompressionInfo.Integer SpectatorCameraTypeCompressionInfo;

	public static CompressionInfo.Integer PollAcceptThresholdCompressionInfo;

	public static CompressionInfo.Integer NumberOfBotsTeamCompressionInfo;

	public static CompressionInfo.Integer NumberOfBotsPerFormationCompressionInfo;

	public static CompressionInfo.Integer AutoTeamBalanceLimitCompressionInfo;

	public static CompressionInfo.Integer FriendlyFireDamageCompressionInfo;

	public static CompressionInfo.Integer ForcedAvatarIndexCompressionInfo;

	public static CompressionInfo.Integer IntermissionStateCompressionInfo;

	public static CompressionInfo.Float IntermissionTimerCompressionInfo;

	public static CompressionInfo.Integer IntermissionMapVoteItemCountCompressionInfo;

	public static CompressionInfo.Integer IntermissionVoterCountCompressionInfo;

	public static CompressionInfo.Integer ActionCodeCompressionInfo;

	public static CompressionInfo.Integer AnimationIndexCompressionInfo;

	public static CompressionInfo.Integer CultureIndexCompressionInfo;

	public static CompressionInfo.Integer SoundEventsCompressionInfo;

	public static CompressionInfo.Integer NetworkComponentEventTypeFromServerCompressionInfo;

	public static CompressionInfo.Integer NetworkComponentEventTypeFromClientCompressionInfo;

	public static CompressionInfo.Integer TroopTypeCompressionInfo;

	public static CompressionInfo.Integer BannerDataCountCompressionInfo;

	public static CompressionInfo.Integer BannerDataMeshIdCompressionInfo;

	public static CompressionInfo.Integer BannerDataColorIndexCompressionInfo;

	public static CompressionInfo.Integer BannerDataSizeCompressionInfo;

	public static CompressionInfo.Integer BannerDataRotationCompressionInfo;

	static CompressionBasic()
	{
		PingValueCompressionInfo = new CompressionInfo.Integer(0, 1023, maximumValueGiven: true);
		LossValueCompressionInfo = new CompressionInfo.Integer(0, 100, maximumValueGiven: true);
		ServerPerformanceStateCompressionInfo = new CompressionInfo.Integer(0, 2, maximumValueGiven: true);
		ColorCompressionInfo = new CompressionInfo.UnsignedInteger(0u, 32);
		ItemDataValueCompressionInfo = new CompressionInfo.Integer(0, 16);
		RandomSeedCompressionInfo = new CompressionInfo.Integer(0, 2000, maximumValueGiven: true);
		PositionCompressionInfo = new CompressionInfo.Float(-100f, 10385f, 22);
		LocalPositionCompressionInfo = new CompressionInfo.Float(-32f, 32f, 16);
		LowResLocalPositionCompressionInfo = new CompressionInfo.Float(-32f, 32f, 12);
		BigRangeLowResLocalPositionCompressionInfo = new CompressionInfo.Float(-1000f, 1000f, 16);
		PlayerCompressionInfo = new CompressionInfo.Integer(-1, 1022, maximumValueGiven: true);
		PeerComponentCompressionInfo = new CompressionInfo.UnsignedInteger(0u, 32);
		GUIDCompressionInfo = new CompressionInfo.UnsignedInteger(0u, 32);
		FlagsCompressionInfo = new CompressionInfo.Integer(0, 30);
		GUIDIntCompressionInfo = new CompressionInfo.Integer(-1, 31);
		MissionObjectIDCompressionInfo = new CompressionInfo.Integer(-1, 8190, maximumValueGiven: true);
		UnitVectorCompressionInfo = new CompressionInfo.Float(-1.024f, 10, 0.002f);
		LowResRadianCompressionInfo = new CompressionInfo.Float(-3.1515927f, 3.1515927f, 8);
		RadianCompressionInfo = new CompressionInfo.Float(-3.1515927f, 3.1515927f, 10);
		HighResRadianCompressionInfo = new CompressionInfo.Float(-3.1515927f, 3.1515927f, 13);
		ScaleCompressionInfo = new CompressionInfo.Float(-0.001f, 10, 0.01f);
		LowResQuaternionCompressionInfo = new CompressionInfo.Float(-0.7071068f, 0.7071068f, 6);
		OmittedQuaternionComponentIndexCompressionInfo = new CompressionInfo.Integer(0, 3, maximumValueGiven: true);
		ImpulseCompressionInfo = new CompressionInfo.Float(-500f, 16, 0.0153f);
		AnimationKeyCompressionInfo = new CompressionInfo.Integer(0, 8000, maximumValueGiven: true);
		AnimationSpeedCompressionInfo = new CompressionInfo.Float(0f, 9, 0.01f);
		AnimationProgressCompressionInfo = new CompressionInfo.Float(0f, 1f, 9);
		VertexAnimationSpeedCompressionInfo = new CompressionInfo.Float(0f, 9, 0.1f);
		PercentageCompressionInfo = new CompressionInfo.Integer(0, 100, maximumValueGiven: true);
		EntityChildCountCompressionInfo = new CompressionInfo.Integer(0, 8);
		AgentHitDamageCompressionInfo = new CompressionInfo.Integer(0, 2000, maximumValueGiven: true);
		AgentHitModifiedDamageCompressionInfo = new CompressionInfo.Integer(-2000, 2000, maximumValueGiven: true);
		AgentHitRelativeSpeedCompressionInfo = new CompressionInfo.Float(0f, 17, 0.01f);
		AgentHitArmorCompressionInfo = new CompressionInfo.Integer(0, 200, maximumValueGiven: true);
		AgentHitBoneIndexCompressionInfo = new CompressionInfo.Integer(-1, 63, maximumValueGiven: true);
		AgentHitBodyPartCompressionInfo = new CompressionInfo.Integer(-1, 8, maximumValueGiven: true);
		AgentHitDamageTypeCompressionInfo = new CompressionInfo.Integer(-1, 2, maximumValueGiven: true);
		RoundGoldAmountCompressionInfo = new CompressionInfo.Integer(-1, 2000, maximumValueGiven: true);
		DebugIntNonCompressionInfo = new CompressionInfo.Integer(int.MinValue, 32);
		DebugULongNonCompressionInfo = new CompressionInfo.UnsignedLongInteger(0uL, 64);
		AgentAgeCompressionInfo = new CompressionInfo.Float(0f, 128f, 10);
		FaceKeyDataCompressionInfo = new CompressionInfo.Float(0f, 1f, 10);
		PlayerChosenBadgeCompressionInfo = new CompressionInfo.Integer(-1, 8);
		MaxNumberOfPlayersCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.MaxNumberOfPlayers.GetMinimumValue(), MultiplayerOptions.OptionType.MaxNumberOfPlayers.GetMaximumValue(), maximumValueGiven: true);
		MinNumberOfPlayersForMatchStartCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetMinimumValue(), MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetMaximumValue(), maximumValueGiven: true);
		MapTimeLimitCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.MapTimeLimit.GetMinimumValue(), MultiplayerOptions.OptionType.MapTimeLimit.GetMaximumValue(), maximumValueGiven: true);
		RoundTotalCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.RoundTotal.GetMinimumValue(), MultiplayerOptions.OptionType.RoundTotal.GetMaximumValue(), maximumValueGiven: true);
		RoundTimeLimitCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.RoundTimeLimit.GetMinimumValue(), MultiplayerOptions.OptionType.RoundTimeLimit.GetMaximumValue(), maximumValueGiven: true);
		WarmupTimeLimitCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.WarmupTimeLimitInSeconds.GetMinimumValue(), MultiplayerOptions.OptionType.WarmupTimeLimitInSeconds.GetMaximumValue(), maximumValueGiven: true);
		RoundPreparationTimeLimitCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetMinimumValue(), MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetMaximumValue(), maximumValueGiven: true);
		RespawnPeriodCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.RespawnPeriodTeam1.GetMinimumValue(), MultiplayerOptions.OptionType.RespawnPeriodTeam1.GetMaximumValue(), maximumValueGiven: true);
		GoldGainChangePercentageCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.GoldGainChangePercentageTeam1.GetMinimumValue(), MultiplayerOptions.OptionType.GoldGainChangePercentageTeam1.GetMaximumValue(), maximumValueGiven: true);
		SpectatorCameraTypeCompressionInfo = new CompressionInfo.Integer(-1, 7, maximumValueGiven: true);
		PollAcceptThresholdCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.PollAcceptThreshold.GetMinimumValue(), MultiplayerOptions.OptionType.PollAcceptThreshold.GetMaximumValue(), maximumValueGiven: true);
		NumberOfBotsTeamCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetMinimumValue(), MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetMaximumValue(), maximumValueGiven: true);
		NumberOfBotsPerFormationCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetMinimumValue(), MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetMaximumValue(), maximumValueGiven: true);
		AutoTeamBalanceLimitCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetMinimumValue(), MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetMaximumValue(), maximumValueGiven: true);
		FriendlyFireDamageCompressionInfo = new CompressionInfo.Integer(MultiplayerOptions.OptionType.FriendlyFireDamageMeleeFriendPercent.GetMinimumValue(), MultiplayerOptions.OptionType.FriendlyFireDamageMeleeFriendPercent.GetMaximumValue(), maximumValueGiven: true);
		ForcedAvatarIndexCompressionInfo = new CompressionInfo.Integer(-1, 99, maximumValueGiven: true);
		IntermissionStateCompressionInfo = new CompressionInfo.Integer(0, Enum.GetNames(typeof(MultiplayerIntermissionState)).Length - 1, maximumValueGiven: false);
		IntermissionTimerCompressionInfo = new CompressionInfo.Float(0f, 240f, 14);
		IntermissionMapVoteItemCountCompressionInfo = new CompressionInfo.Integer(0, 99, maximumValueGiven: true);
		IntermissionVoterCountCompressionInfo = new CompressionInfo.Integer(0, 1022, maximumValueGiven: true);
		TroopTypeCompressionInfo = new CompressionInfo.Integer(-1, 2, maximumValueGiven: true);
		BannerDataCountCompressionInfo = new CompressionInfo.Integer(0, 31, maximumValueGiven: true);
		BannerDataMeshIdCompressionInfo = new CompressionInfo.Integer(0, 13);
		BannerDataColorIndexCompressionInfo = new CompressionInfo.Integer(0, 10);
		BannerDataSizeCompressionInfo = new CompressionInfo.Integer(-8000, 8000, maximumValueGiven: true);
		BannerDataRotationCompressionInfo = new CompressionInfo.Integer(0, 360, maximumValueGiven: true);
	}
}
