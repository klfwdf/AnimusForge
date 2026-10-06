using System;
using NavalDLC.View.MissionViews;
using NavalDLC.View.MissionViews.Storyline;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.View;

public static class NavalViewCreator
{
	public static MissionView CreateNavalOrderUIHandler(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<NavalMissionOrderUIHandler>(isNetwork: false, mission, Array.Empty<object>());
	}

	public static MissionView CreateNavalOrderOfBattleView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<NavalOrderOfBattleView>(isNetwork: false, mission, new object[1] { mission });
	}

	public static MissionView CreateNavalShipMarkerUIHandler(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<NavalMissionShipMarkerUIHandler>(isNetwork: false, mission, Array.Empty<object>());
	}

	public static MissionView CreateNavalShipTargetSelectionHandler(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<NavalShipTargetSelectionHandler>(isNetwork: false, mission, Array.Empty<object>());
	}

	public static MissionView CreateMissionShipControlView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<MissionShipControlView>(isNetwork: false, mission, Array.Empty<object>());
	}

	public static MissionView CreateNavalMissionCaptureShipView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<NavalMissionCaptureShipView>(isNetwork: false, mission, Array.Empty<object>());
	}

	public static MissionView CreateQuest5SetPieceBattleMissionView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<Quest5SetPieceBattleMissionView>(isNetwork: false, mission, Array.Empty<object>());
	}

	public static MissionView CreateQuest5SetPieceBattleBossFightCameraView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<Quest5SetPieceBattleBossFightCameraView>(isNetwork: false, mission, Array.Empty<object>());
	}

	public static MissionView CreateQuest5SetPieceBattleInteriorConversationCameraView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<Quest5SetPieceBattleInteriorConversationCameraView>(isNetwork: false, mission, Array.Empty<object>());
	}

	public static MissionView CreateCaptivityMissionView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<NavalCaptivityBattleMissionView>(isNetwork: false, mission, Array.Empty<object>());
	}

	public static MissionView CreateFloatingFortressView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<FloatingFortressView>(isNetwork: false, mission, Array.Empty<object>());
	}

	public static MissionView CreatePirateBattleMissionView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<NavalStorylinePirateBattleMissionView>(isNetwork: false, mission, Array.Empty<object>());
	}

	public static MissionView CreateHelpingAnAllyMissionView(Mission mission = null)
	{
		return ViewCreatorManager.CreateMissionView<HelpingAnAllyMissionView>(isNetwork: false, mission, Array.Empty<object>());
	}
}
