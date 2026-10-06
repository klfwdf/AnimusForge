using System;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;

public static class MultiplayerViewCreator
{
	public static MissionView CreateMissionMultiplayerPreloadView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionMultiplayerPreloadView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionScoreBoardUIHandler(Mission mission, bool isSingleTeam)
	{
		return ViewCreatorManager.CreateMissionView<MissionScoreboardUIHandler>(mission != null, mission, new object[1] { isSingleTeam });
	}

	public static MissionView CreateMultiplayerEndOfRoundUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MultiplayerEndOfRoundUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMultiplayerTeamSelectUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MultiplayerTeamSelectUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMultiplayerCultureSelectUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MultiplayerCultureSelectUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateLobbyEquipmentUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MissionLobbyEquipmentUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreatePollProgressUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MultiplayerPollProgressUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMissionMultiplayerEscapeMenu(string gameType)
	{
		return ViewCreatorManager.CreateMissionView<MissionMultiplayerEscapeMenu>(isNetwork: false, null, new object[1] { gameType });
	}

	public static MissionView CreateMissionMultiplayerPracticeEscapeMenu()
	{
		return ViewCreatorManager.CreateMissionView<MissionMultiplayerPracticeEscapeMenu>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMissionKillNotificationUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MissionMultiplayerKillNotificationUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMissionServerStatusUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MissionMultiplayerServerStatusUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMultiplayerAdminPanelUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MultiplayerAdminPanelUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMultiplayerFactionBanVoteUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MultiplayerFactionBanVoteUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMultiplayerMissionHUDExtensionUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MissionMultiplayerHUDExtensionUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMultiplayerMissionVoiceChatUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MissionMultiplayerVoiceChatUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMultiplayerMissionOrderUIHandler(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MultiplayerMissionOrderUIHandler>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMultiplayerMissionDeathCardUIHandler(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionMultiplayerDeathCardUIHandler>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionMultiplayerDuelUI()
	{
		return ViewCreatorManager.CreateMissionView<MissionMultiplayerDuelUI>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMultiplayerEndOfBattleUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MultiplayerEndOfBattleUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMissionFlagMarkerUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MissionMultiplayerMarkerUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}
}
