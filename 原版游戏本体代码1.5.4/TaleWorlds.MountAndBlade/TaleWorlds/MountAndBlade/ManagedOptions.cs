using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public static class ManagedOptions
{
	public enum ManagedOptionsType
	{
		Language,
		GyroOverrideForAttackDefend,
		ControlBlockDirection,
		ControlAttackDirection,
		NumberOfCorpses,
		BattleSize,
		ReinforcementWaveCount,
		TurnCameraWithHorseInFirstPerson,
		ShowBlood,
		ShowAttackDirection,
		ShowTargetingReticle,
		AutoSaveInterval,
		FriendlyTroopsBannerOpacity,
		AlwaysShowFriendlyTroopBannersType,
		ShowFormationDistances,
		ReportDamage,
		ReportBark,
		LockTarget,
		EnableTutorialHints,
		ReportCasualtiesType,
		ReportExperience,
		ReportPersonalDamage,
		FirstPersonFov,
		CombatCameraDistance,
		EnableDamageTakenVisuals,
		EnableVoiceChat,
		EnableDeathIcon,
		EnableNetworkAlertIcons,
		ForceVSyncInMenus,
		EnableVerticalAimCorrection,
		ZoomSensitivityModifier,
		UIScale,
		CrosshairType,
		EnableGenericAvatars,
		EnableGenericNames,
		OrderType,
		OrderLayoutType,
		AutoTrackAttackedSettlements,
		StopGameOnFocusLost,
		SlowDownOnOrder,
		HideFullServers,
		HideEmptyServers,
		HidePasswordProtectedServers,
		HideUnofficialServers,
		HideModuleIncompatibleServers,
		HideBattleUI,
		UnitSpawnPrioritization,
		EnableSingleplayerChatBox,
		EnableMultiplayerChatBox,
		VoiceLanguage,
		PlayerReceivedDamageDifficulty,
		MapDoubleClickBehavior,
		ManagedOptionTypeCount
	}

	public delegate void OnManagedOptionChangedDelegate(ManagedOptionsType changedManagedOptionsType);

	public static OnManagedOptionChangedDelegate OnManagedOptionChanged;

	public static float GetConfig(ManagedOptionsType type)
	{
		switch (type)
		{
		case ManagedOptionsType.GyroOverrideForAttackDefend:
			return BannerlordConfig.GyroOverrideForAttackDefend ? 1 : 0;
		case ManagedOptionsType.ControlBlockDirection:
			return BannerlordConfig.DefendDirectionControl;
		case ManagedOptionsType.ControlAttackDirection:
			return BannerlordConfig.AttackDirectionControl;
		case ManagedOptionsType.NumberOfCorpses:
			return BannerlordConfig.NumberOfCorpses;
		case ManagedOptionsType.ShowBlood:
			return BannerlordConfig.ShowBlood ? 1 : 0;
		case ManagedOptionsType.BattleSize:
			return BannerlordConfig.BattleSize;
		case ManagedOptionsType.ReinforcementWaveCount:
			return BannerlordConfig.ReinforcementWaveCount;
		case ManagedOptionsType.TurnCameraWithHorseInFirstPerson:
			return BannerlordConfig.TurnCameraWithHorseInFirstPerson;
		case ManagedOptionsType.ShowAttackDirection:
			return BannerlordConfig.DisplayAttackDirection ? 1 : 0;
		case ManagedOptionsType.ShowTargetingReticle:
			return BannerlordConfig.DisplayTargetingReticule ? 1 : 0;
		case ManagedOptionsType.AutoSaveInterval:
			return BannerlordConfig.AutoSaveInterval;
		case ManagedOptionsType.FriendlyTroopsBannerOpacity:
			return BannerlordConfig.FriendlyTroopsBannerOpacity;
		case ManagedOptionsType.AlwaysShowFriendlyTroopBannersType:
			return BannerlordConfig.AlwaysShowFriendlyTroopBannersType;
		case ManagedOptionsType.ShowFormationDistances:
			return BannerlordConfig.ShowFormationDistances ? 1 : 0;
		case ManagedOptionsType.ReportDamage:
			return BannerlordConfig.ReportDamage ? 1 : 0;
		case ManagedOptionsType.ReportBark:
			return BannerlordConfig.ReportBark ? 1 : 0;
		case ManagedOptionsType.LockTarget:
			return BannerlordConfig.LockTarget ? 1 : 0;
		case ManagedOptionsType.EnableTutorialHints:
			return BannerlordConfig.EnableTutorialHints ? 1 : 0;
		case ManagedOptionsType.ReportCasualtiesType:
			return BannerlordConfig.KillFeedVisualType;
		case ManagedOptionsType.AutoTrackAttackedSettlements:
			return BannerlordConfig.AutoTrackAttackedSettlements;
		case ManagedOptionsType.StopGameOnFocusLost:
			return BannerlordConfig.StopGameOnFocusLost ? 1 : 0;
		case ManagedOptionsType.SlowDownOnOrder:
			return BannerlordConfig.SlowDownOnOrder ? 1 : 0;
		case ManagedOptionsType.ReportPersonalDamage:
			return BannerlordConfig.ReportPersonalDamage ? 1 : 0;
		case ManagedOptionsType.ReportExperience:
			return BannerlordConfig.ReportExperience ? 1 : 0;
		case ManagedOptionsType.ForceVSyncInMenus:
			return BannerlordConfig.ForceVSyncInMenus ? 1 : 0;
		case ManagedOptionsType.FirstPersonFov:
			return BannerlordConfig.FirstPersonFov;
		case ManagedOptionsType.UIScale:
			return BannerlordConfig.UIScale;
		case ManagedOptionsType.CombatCameraDistance:
			return BannerlordConfig.CombatCameraDistance;
		case ManagedOptionsType.Language:
			return LocalizedTextManager.GetLanguageIds(NativeConfig.IsDevelopmentMode).IndexOf(BannerlordConfig.Language);
		case ManagedOptionsType.EnableDamageTakenVisuals:
			return BannerlordConfig.EnableDamageTakenVisuals ? 1 : 0;
		case ManagedOptionsType.EnableVoiceChat:
			return BannerlordConfig.EnableVoiceChat ? 1 : 0;
		case ManagedOptionsType.EnableSingleplayerChatBox:
			return BannerlordConfig.EnableSingleplayerChatBox ? 1 : 0;
		case ManagedOptionsType.EnableMultiplayerChatBox:
			return BannerlordConfig.EnableMultiplayerChatBox ? 1 : 0;
		case ManagedOptionsType.EnableDeathIcon:
			return BannerlordConfig.EnableDeathIcon ? 1 : 0;
		case ManagedOptionsType.EnableNetworkAlertIcons:
			return BannerlordConfig.EnableNetworkAlertIcons ? 1 : 0;
		case ManagedOptionsType.EnableVerticalAimCorrection:
			return BannerlordConfig.EnableVerticalAimCorrection ? 1 : 0;
		case ManagedOptionsType.ZoomSensitivityModifier:
			return BannerlordConfig.ZoomSensitivityModifier;
		case ManagedOptionsType.OrderType:
			return BannerlordConfig.OrderType;
		case ManagedOptionsType.OrderLayoutType:
			return BannerlordConfig.OrderLayoutType;
		case ManagedOptionsType.CrosshairType:
			return BannerlordConfig.CrosshairType;
		case ManagedOptionsType.EnableGenericAvatars:
			return BannerlordConfig.EnableGenericAvatars ? 1 : 0;
		case ManagedOptionsType.EnableGenericNames:
			return BannerlordConfig.EnableGenericNames ? 1 : 0;
		case ManagedOptionsType.HideFullServers:
			return BannerlordConfig.HideFullServers ? 1 : 0;
		case ManagedOptionsType.HideEmptyServers:
			return BannerlordConfig.HideEmptyServers ? 1 : 0;
		case ManagedOptionsType.HidePasswordProtectedServers:
			return BannerlordConfig.HidePasswordProtectedServers ? 1 : 0;
		case ManagedOptionsType.HideUnofficialServers:
			return BannerlordConfig.HideUnofficialServers ? 1 : 0;
		case ManagedOptionsType.HideModuleIncompatibleServers:
			return BannerlordConfig.HideModuleIncompatibleServers ? 1 : 0;
		case ManagedOptionsType.UnitSpawnPrioritization:
			return BannerlordConfig.UnitSpawnPrioritization;
		case ManagedOptionsType.HideBattleUI:
			return BannerlordConfig.HideBattleUI ? 1 : 0;
		case ManagedOptionsType.VoiceLanguage:
			return LocalizedVoiceManager.GetVoiceLanguageIds().IndexOf(BannerlordConfig.VoiceLanguage);
		case ManagedOptionsType.PlayerReceivedDamageDifficulty:
			return BannerlordConfig.PlayerReceivedDamageDifficulty;
		case ManagedOptionsType.MapDoubleClickBehavior:
			return BannerlordConfig.MapDoubleClickBehavior;
		default:
			Debug.FailedAssert("ManagedOptionsType not found", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Options\\ManagedOptions\\ManagedOptions.cs", "GetConfig", 180);
			return 0f;
		}
	}

	public static float GetDefaultConfig(ManagedOptionsType type)
	{
		switch (type)
		{
		case ManagedOptionsType.GyroOverrideForAttackDefend:
			return 0f;
		case ManagedOptionsType.ControlBlockDirection:
			return 0f;
		case ManagedOptionsType.ControlAttackDirection:
			return 1f;
		case ManagedOptionsType.NumberOfCorpses:
			return 3f;
		case ManagedOptionsType.ShowBlood:
			return 1f;
		case ManagedOptionsType.BattleSize:
			return 2f;
		case ManagedOptionsType.ReinforcementWaveCount:
			return 3f;
		case ManagedOptionsType.TurnCameraWithHorseInFirstPerson:
			return 2f;
		case ManagedOptionsType.ShowAttackDirection:
			return 1f;
		case ManagedOptionsType.ShowTargetingReticle:
			return 1f;
		case ManagedOptionsType.AutoSaveInterval:
			return 30f;
		case ManagedOptionsType.FriendlyTroopsBannerOpacity:
			return 1f;
		case ManagedOptionsType.AlwaysShowFriendlyTroopBannersType:
			return 1f;
		case ManagedOptionsType.ShowFormationDistances:
			return 0f;
		case ManagedOptionsType.ReportDamage:
			return 1f;
		case ManagedOptionsType.ReportBark:
			return 1f;
		case ManagedOptionsType.LockTarget:
			return 0f;
		case ManagedOptionsType.EnableTutorialHints:
			return 1f;
		case ManagedOptionsType.ReportCasualtiesType:
			return 1f;
		case ManagedOptionsType.StopGameOnFocusLost:
			return 1f;
		case ManagedOptionsType.SlowDownOnOrder:
			return 1f;
		case ManagedOptionsType.AutoTrackAttackedSettlements:
			return 0f;
		case ManagedOptionsType.ReportPersonalDamage:
			return 1f;
		case ManagedOptionsType.ReportExperience:
			return 1f;
		case ManagedOptionsType.ForceVSyncInMenus:
			return 1f;
		case ManagedOptionsType.FirstPersonFov:
			return 65f;
		case ManagedOptionsType.OrderType:
			return 0f;
		case ManagedOptionsType.OrderLayoutType:
			return 0f;
		case ManagedOptionsType.UIScale:
			return 1f;
		case ManagedOptionsType.CombatCameraDistance:
			return 1f;
		case ManagedOptionsType.EnableDamageTakenVisuals:
			return 1f;
		case ManagedOptionsType.EnableVoiceChat:
			return 1f;
		case ManagedOptionsType.EnableSingleplayerChatBox:
			return 1f;
		case ManagedOptionsType.EnableMultiplayerChatBox:
			return 1f;
		case ManagedOptionsType.EnableDeathIcon:
			return 1f;
		case ManagedOptionsType.EnableNetworkAlertIcons:
			return 1f;
		case ManagedOptionsType.EnableVerticalAimCorrection:
			return 1f;
		case ManagedOptionsType.ZoomSensitivityModifier:
			return 0.66666f;
		case ManagedOptionsType.CrosshairType:
			return 0f;
		case ManagedOptionsType.Language:
			return LocalizedTextManager.GetLanguageIds(NativeConfig.IsDevelopmentMode).IndexOf(BannerlordConfig.DefaultLanguage);
		case ManagedOptionsType.EnableGenericAvatars:
			return 0f;
		case ManagedOptionsType.EnableGenericNames:
			return 0f;
		case ManagedOptionsType.HideFullServers:
			return 0f;
		case ManagedOptionsType.HideEmptyServers:
			return 0f;
		case ManagedOptionsType.HidePasswordProtectedServers:
			return 0f;
		case ManagedOptionsType.HideUnofficialServers:
			return 0f;
		case ManagedOptionsType.HideModuleIncompatibleServers:
			return 0f;
		case ManagedOptionsType.UnitSpawnPrioritization:
			return 0f;
		case ManagedOptionsType.HideBattleUI:
			return 0f;
		case ManagedOptionsType.VoiceLanguage:
			return LocalizedVoiceManager.GetVoiceLanguageIds().IndexOf(BannerlordConfig.VoiceLanguage);
		case ManagedOptionsType.PlayerReceivedDamageDifficulty:
			return 0f;
		case ManagedOptionsType.MapDoubleClickBehavior:
			return 0f;
		default:
			Debug.FailedAssert("ManagedOptionsType not found", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Options\\ManagedOptions\\ManagedOptions.cs", "GetDefaultConfig", 293);
			return 0f;
		}
	}

	[MBCallback(null, true)]
	internal static int GetConfigCount()
	{
		return 52;
	}

	[MBCallback(null, true)]
	internal static float GetConfigValue(int type)
	{
		return GetConfig((ManagedOptionsType)type);
	}

	public static void SetConfig(ManagedOptionsType type, float value)
	{
		switch (type)
		{
		case ManagedOptionsType.GyroOverrideForAttackDefend:
			BannerlordConfig.GyroOverrideForAttackDefend = value != 0f;
			break;
		case ManagedOptionsType.ControlBlockDirection:
			BannerlordConfig.DefendDirectionControl = (int)value;
			break;
		case ManagedOptionsType.ControlAttackDirection:
			BannerlordConfig.AttackDirectionControl = (int)value;
			break;
		case ManagedOptionsType.NumberOfCorpses:
			BannerlordConfig.NumberOfCorpses = (int)value;
			break;
		case ManagedOptionsType.ShowBlood:
			BannerlordConfig.ShowBlood = (double)value != 0.0;
			break;
		case ManagedOptionsType.BattleSize:
			BannerlordConfig.BattleSize = (int)value;
			break;
		case ManagedOptionsType.ReinforcementWaveCount:
			BannerlordConfig.ReinforcementWaveCount = (int)value;
			break;
		case ManagedOptionsType.TurnCameraWithHorseInFirstPerson:
			BannerlordConfig.TurnCameraWithHorseInFirstPerson = (int)value;
			break;
		case ManagedOptionsType.ShowAttackDirection:
			BannerlordConfig.DisplayAttackDirection = (double)value != 0.0;
			break;
		case ManagedOptionsType.ShowTargetingReticle:
			BannerlordConfig.DisplayTargetingReticule = value != 0f;
			break;
		case ManagedOptionsType.AutoSaveInterval:
			BannerlordConfig.AutoSaveInterval = (int)value;
			break;
		case ManagedOptionsType.FriendlyTroopsBannerOpacity:
			BannerlordConfig.FriendlyTroopsBannerOpacity = value;
			break;
		case ManagedOptionsType.AlwaysShowFriendlyTroopBannersType:
			BannerlordConfig.AlwaysShowFriendlyTroopBannersType = (int)value;
			break;
		case ManagedOptionsType.ShowFormationDistances:
			BannerlordConfig.ShowFormationDistances = value != 0f;
			break;
		case ManagedOptionsType.ReportDamage:
			BannerlordConfig.ReportDamage = value != 0f;
			break;
		case ManagedOptionsType.ReportBark:
			BannerlordConfig.ReportBark = value != 0f;
			break;
		case ManagedOptionsType.LockTarget:
			BannerlordConfig.LockTarget = value != 0f;
			break;
		case ManagedOptionsType.EnableTutorialHints:
			BannerlordConfig.EnableTutorialHints = value != 0f;
			break;
		case ManagedOptionsType.ReportCasualtiesType:
			BannerlordConfig.KillFeedVisualType = (int)value;
			break;
		case ManagedOptionsType.StopGameOnFocusLost:
			BannerlordConfig.StopGameOnFocusLost = value != 0f;
			break;
		case ManagedOptionsType.SlowDownOnOrder:
			BannerlordConfig.SlowDownOnOrder = value != 0f;
			break;
		case ManagedOptionsType.AutoTrackAttackedSettlements:
			BannerlordConfig.AutoTrackAttackedSettlements = (int)value;
			break;
		case ManagedOptionsType.ReportPersonalDamage:
			BannerlordConfig.ReportPersonalDamage = value != 0f;
			break;
		case ManagedOptionsType.ReportExperience:
			BannerlordConfig.ReportExperience = value != 0f;
			break;
		case ManagedOptionsType.UIScale:
			BannerlordConfig.UIScale = value;
			break;
		case ManagedOptionsType.ForceVSyncInMenus:
			BannerlordConfig.ForceVSyncInMenus = value != 0f;
			break;
		case ManagedOptionsType.FirstPersonFov:
			BannerlordConfig.FirstPersonFov = value;
			break;
		case ManagedOptionsType.CombatCameraDistance:
			BannerlordConfig.CombatCameraDistance = value;
			break;
		case ManagedOptionsType.EnableDamageTakenVisuals:
			BannerlordConfig.EnableDamageTakenVisuals = value != 0f;
			break;
		case ManagedOptionsType.EnableVoiceChat:
			BannerlordConfig.EnableVoiceChat = value != 0f;
			break;
		case ManagedOptionsType.EnableSingleplayerChatBox:
			BannerlordConfig.EnableSingleplayerChatBox = value != 0f;
			break;
		case ManagedOptionsType.EnableMultiplayerChatBox:
			BannerlordConfig.EnableMultiplayerChatBox = value != 0f;
			break;
		case ManagedOptionsType.EnableDeathIcon:
			BannerlordConfig.EnableDeathIcon = value != 0f;
			break;
		case ManagedOptionsType.EnableNetworkAlertIcons:
			BannerlordConfig.EnableNetworkAlertIcons = value != 0f;
			break;
		case ManagedOptionsType.EnableVerticalAimCorrection:
			BannerlordConfig.EnableVerticalAimCorrection = value != 0f;
			break;
		case ManagedOptionsType.ZoomSensitivityModifier:
			BannerlordConfig.ZoomSensitivityModifier = value;
			break;
		case ManagedOptionsType.CrosshairType:
			BannerlordConfig.CrosshairType = (int)value;
			break;
		case ManagedOptionsType.EnableGenericAvatars:
			BannerlordConfig.EnableGenericAvatars = value != 0f;
			break;
		case ManagedOptionsType.EnableGenericNames:
			BannerlordConfig.EnableGenericNames = value != 0f;
			break;
		case ManagedOptionsType.HideFullServers:
			BannerlordConfig.HideFullServers = value != 0f;
			break;
		case ManagedOptionsType.HideEmptyServers:
			BannerlordConfig.HideEmptyServers = value != 0f;
			break;
		case ManagedOptionsType.HidePasswordProtectedServers:
			BannerlordConfig.HidePasswordProtectedServers = value != 0f;
			break;
		case ManagedOptionsType.HideUnofficialServers:
			BannerlordConfig.HideUnofficialServers = value != 0f;
			break;
		case ManagedOptionsType.HideModuleIncompatibleServers:
			BannerlordConfig.HideModuleIncompatibleServers = value != 0f;
			break;
		case ManagedOptionsType.OrderType:
			BannerlordConfig.OrderType = (int)value;
			break;
		case ManagedOptionsType.OrderLayoutType:
			BannerlordConfig.OrderLayoutType = (int)value;
			break;
		case ManagedOptionsType.Language:
		{
			List<string> voiceLanguageIds = LocalizedTextManager.GetLanguageIds(NativeConfig.IsDevelopmentMode);
			if (value >= 0f && value < (float)voiceLanguageIds.Count)
			{
				BannerlordConfig.Language = voiceLanguageIds[(int)value];
				break;
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Options\\ManagedOptions\\ManagedOptions.cs", "SetConfig", 459);
			BannerlordConfig.Language = voiceLanguageIds[0];
			break;
		}
		case ManagedOptionsType.UnitSpawnPrioritization:
			BannerlordConfig.UnitSpawnPrioritization = (int)value;
			break;
		case ManagedOptionsType.HideBattleUI:
			BannerlordConfig.HideBattleUI = value != 0f;
			break;
		case ManagedOptionsType.VoiceLanguage:
		{
			List<string> voiceLanguageIds = LocalizedVoiceManager.GetVoiceLanguageIds();
			if (value >= 0f && value < (float)voiceLanguageIds.Count)
			{
				BannerlordConfig.VoiceLanguage = voiceLanguageIds[(int)value];
				break;
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Options\\ManagedOptions\\ManagedOptions.cs", "SetConfig", 477);
			BannerlordConfig.VoiceLanguage = voiceLanguageIds[0];
			break;
		}
		case ManagedOptionsType.PlayerReceivedDamageDifficulty:
			BannerlordConfig.PlayerReceivedDamageDifficulty = (int)value;
			break;
		case ManagedOptionsType.MapDoubleClickBehavior:
			BannerlordConfig.MapDoubleClickBehavior = (int)value;
			break;
		}
		OnManagedOptionChanged?.Invoke(type);
	}

	public static SaveResult SaveConfig()
	{
		return BannerlordConfig.Save();
	}
}
