using System.Collections.Generic;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Storyline;
using NavalDLC.View.MissionViews.Storyline;
using NavalDLC.ViewModelCollection.Missions.NameMarkers;
using SandBox.ViewModelCollection.Missions.NameMarker;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.View.Missions;

public class NavalStorylineCaptivityMissionNameMarkerProvider : MissionNameMarkerProvider
{
	private NavalStorylineCaptivityMissionController _captivityMissionController;

	private NavalCaptivityBattleMissionView _captivityMissionView;

	private bool _hasSetTargets;

	protected override void OnInitialize(Mission mission)
	{
		base.OnInitialize(mission);
		_captivityMissionController = mission.GetMissionBehavior<NavalStorylineCaptivityMissionController>();
		_captivityMissionView = mission.GetMissionBehavior<NavalCaptivityBattleMissionView>();
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (!_hasSetTargets && _captivityMissionController.IsInitialized())
		{
			_hasSetTargets = true;
			SetMarkersDirty();
		}
		if (_captivityMissionView.AreMarkersDirty)
		{
			SetMarkersDirty();
			_captivityMissionView.OnDirtyMarkersHandled();
		}
	}

	public override void CreateMarkers(List<MissionNameMarkerTargetBaseVM> markers)
	{
		if (_hasSetTargets)
		{
			ShipControllerMachine markedShipControllerMachine = _captivityMissionController.GetMarkedShipControllerMachine();
			if (markedShipControllerMachine != null)
			{
				NavalMissionShipControlPointMarkerTargetVM navalMissionShipControlPointMarkerTargetVM = new NavalMissionShipControlPointMarkerTargetVM(markedShipControllerMachine);
				navalMissionShipControlPointMarkerTargetVM.IsPersistent = true;
				markers.Add(navalMissionShipControlPointMarkerTargetVM);
			}
		}
	}
}
