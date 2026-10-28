using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View;

public static class ViewCreator
{
	public static ScreenBase CreateCreditsScreen()
	{
		return ViewCreatorManager.CreateScreenView<CreditsScreen>();
	}

	public static ScreenBase CreateOptionsScreen(bool fromMainMenu)
	{
		return ViewCreatorManager.CreateScreenView<OptionsScreen>(new object[1] { fromMainMenu });
	}

	public static ScreenBase CreateMBFaceGeneratorScreen(BasicCharacterObject character, bool openedFromMultiplayer = false, IFaceGeneratorCustomFilter filter = null)
	{
		return ViewCreatorManager.CreateScreenView<FaceGeneratorScreen>(new object[3] { character, openedFromMultiplayer, filter });
	}

	public static MissionView CreateMissionAgentStatusUIHandler(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionAgentStatusUIHandler>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionMainAgentEquipDropView(Mission mission)
	{
		return ViewCreatorManager.CreateMissionView<MissionMainAgentEquipDropView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionSiegeEngineMarkerView(Mission mission)
	{
		return ViewCreatorManager.CreateMissionView<MissionSiegeEngineMarkerView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionMainAgentEquipmentController(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionMainAgentEquipmentControllerView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionMainAgentCheerBarkControllerView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionMainAgentCheerBarkControllerView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionAgentLockVisualizerView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionAgentLockVisualizerView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateOptionsUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MissionOptionsUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateSingleplayerMissionKillNotificationUIHandler()
	{
		return ViewCreatorManager.CreateMissionView<MissionSingleplayerKillNotificationUIHandler>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMissionAgentLabelUIHandler(Mission mission)
	{
		return ViewCreatorManager.CreateMissionView<MissionAgentLabelView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionOrderUIHandler(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionOrderUIHandler>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionOrderOfBattleUIHandler(Mission mission, OrderOfBattleVM dataSource)
	{
		return ViewCreatorManager.CreateMissionView<MissionOrderOfBattleUIHandler>(isNetwork: false, mission, new object[1] { dataSource });
	}

	public static MissionView CreateMissionSpectatorControlView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionSpectatorControlView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionBattleScoreUIHandler(Mission mission, ScoreboardBaseVM dataSource)
	{
		return ViewCreatorManager.CreateMissionView<MissionBattleScoreUIHandler>(isNetwork: false, mission, new object[1] { dataSource });
	}

	public static MissionView CreateMissionBoundaryCrossingView()
	{
		return ViewCreatorManager.CreateMissionView<MissionBoundaryCrossingView>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMissionLeaveView()
	{
		return ViewCreatorManager.CreateMissionView<MissionLeaveView>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreatePhotoModeView()
	{
		return ViewCreatorManager.CreateMissionView<PhotoModeView>(isNetwork: false, null, Array.Empty<object>());
	}

	public static MissionView CreateMissionSingleplayerEscapeMenu(bool isIronmanMode)
	{
		return ViewCreatorManager.CreateMissionView<MissionSingleplayerEscapeMenu>(isNetwork: false, null, new object[1] { isIronmanMode });
	}

	public static MissionView CreateOrderTroopPlacerView(OrderController orderController)
	{
		return ViewCreatorManager.CreateMissionViewWithArgs<OrderTroopPlacer>(new object[1] { orderController });
	}

	public static MissionView CreateMissionFormationMarkerUIHandler(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionFormationMarkerUIHandler>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionHintView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionHintView>(mission != null, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionObjectiveView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionObjectiveView>(mission != null, mission, Array.Empty<object>());
	}
}
