using System.Collections.Generic;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;

[ViewCreatorModule]
public class MultiplayerMissionViews
{
	[ViewMethod("MultiplayerTeamDeathmatch")]
	public static MissionView[] OpenTeamDeathmatchMission(Mission mission)
	{
		return new List<MissionView>
		{
			MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
			MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
			MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
			MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("TeamDeathmatch"),
			MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, isSingleTeam: false),
			MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
			MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			MultiplayerViewCreator.CreatePollProgressUIHandler(),
			MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new SpectatorCameraView(),
			new MultiplayerSpectatorSilhouetteView()
		}.ToArray();
	}

	[ViewMethod("MultiplayerDuel")]
	public static MissionView[] OpenDuelMission(Mission mission)
	{
		return new List<MissionView>
		{
			MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
			MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
			MultiplayerViewCreator.CreateMultiplayerCultureSelectUIHandler(),
			MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Duel"),
			MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
			MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, isSingleTeam: true),
			MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
			MultiplayerViewCreator.CreateMissionMultiplayerDuelUI(),
			MultiplayerViewCreator.CreatePollProgressUIHandler(),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			new SpectatorCameraView(),
			new MultiplayerSpectatorSilhouetteView()
		}.ToArray();
	}

	[ViewMethod("MultiplayerSiege")]
	public static MissionView[] OpenSiegeMission(Mission mission)
	{
		return new List<MissionView>
		{
			MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
			MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
			MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Siege"),
			MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
			MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, isSingleTeam: false),
			MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
			MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
			MultiplayerViewCreator.CreatePollProgressUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			new SpectatorCameraView(),
			new MultiplayerSpectatorSilhouetteView()
		}.ToArray();
	}

	[ViewMethod("MultiplayerBattle")]
	public static MissionView[] OpenBattle(Mission mission)
	{
		return new List<MissionView>
		{
			MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Battle"),
			MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateOrderTroopPlacerView(null),
			MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
			MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, isSingleTeam: false),
			MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
			MultiplayerViewCreator.CreatePollProgressUIHandler(),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(),
			MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			new SpectatorCameraView(),
			new MultiplayerSpectatorSilhouetteView()
		}.ToArray();
	}

	[ViewMethod("MultiplayerCaptain")]
	public static MissionView[] OpenCaptain(Mission mission)
	{
		return new List<MissionView>
		{
			MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
			MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
			MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
			MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Captain"),
			MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateOrderTroopPlacerView(null),
			new MissionFormationTargetSelectionHandler(),
			ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
			MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
			MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, isSingleTeam: false),
			MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
			MultiplayerViewCreator.CreatePollProgressUIHandler(),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(),
			MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			new SpectatorCameraView(),
			new MultiplayerSpectatorSilhouetteView()
		}.ToArray();
	}

	[ViewMethod("MultiplayerSkirmish")]
	public static MissionView[] OpenSkirmish(Mission mission)
	{
		return new List<MissionView>
		{
			MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
			MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
			MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
			ViewCreator.CreateMissionAgentStatusUIHandler(mission),
			MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Skirmish"),
			MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateOrderTroopPlacerView(null),
			MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
			MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, isSingleTeam: false),
			MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
			MultiplayerViewCreator.CreatePollProgressUIHandler(),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(),
			MultiplayerViewCreator.CreateMultiplayerMissionVoiceChatUIHandler(),
			MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
			ViewCreator.CreateOptionsUIHandler(),
			ViewCreator.CreateMissionMainAgentEquipDropView(mission),
			MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			new MissionBoundaryWallView(),
			new SpectatorCameraView(),
			new MultiplayerSpectatorSilhouetteView()
		}.ToArray();
	}
}
