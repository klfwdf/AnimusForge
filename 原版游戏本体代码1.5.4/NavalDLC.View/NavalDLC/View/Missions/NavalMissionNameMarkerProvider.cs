using System.Collections.Generic;
using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.ViewModelCollection.Missions.NameMarkers;
using SandBox.ViewModelCollection.Missions.NameMarker;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.View.Missions;

public class NavalMissionNameMarkerProvider : MissionNameMarkerProvider
{
	private MissionShip _lastSteppedShip;

	private MissionShip _lastControlledShip;

	private AgentNavalComponent _mainAgentNavalComponent;

	private NavalShipsLogic _navalShipsLogic;

	protected override void OnInitialize(Mission mission)
	{
		base.OnInitialize(mission);
		_mainAgentNavalComponent = Agent.Main?.GetComponent<AgentNavalComponent>();
		_navalShipsLogic = mission.GetMissionBehavior<NavalShipsLogic>();
		mission.OnMainAgentChanged += OnMainAgentChanged;
	}

	protected override void OnDestroy(Mission mission)
	{
		base.OnDestroy(mission);
		mission.OnMainAgentChanged -= OnMainAgentChanged;
	}

	private void OnMainAgentChanged(Agent oldAgent)
	{
		_mainAgentNavalComponent = Agent.Main?.GetComponent<AgentNavalComponent>();
		SetMarkersDirty();
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_mainAgentNavalComponent == null)
		{
			_mainAgentNavalComponent = Agent.Main?.GetComponent<AgentNavalComponent>();
		}
		if (_mainAgentNavalComponent?.SteppedShip != _lastSteppedShip)
		{
			_lastSteppedShip = _mainAgentNavalComponent?.SteppedShip;
			SetMarkersDirty();
		}
		if (_navalShipsLogic?.PlayerControlledShip != _lastControlledShip)
		{
			_lastControlledShip = _navalShipsLogic?.PlayerControlledShip;
			SetMarkersDirty();
		}
	}

	public override void CreateMarkers(List<MissionNameMarkerTargetBaseVM> markers)
	{
		if (_lastSteppedShip == null || _lastSteppedShip == _lastControlledShip)
		{
			return;
		}
		bool flag = false;
		ShipControllerMachine shipControllerMachine = _lastSteppedShip.ShipControllerMachine;
		bool flag2 = false;
		for (int i = 0; i < markers.Count; i++)
		{
			if (markers[i] is NavalMissionShipControlPointMarkerTargetVM navalMissionShipControlPointMarkerTargetVM && navalMissionShipControlPointMarkerTargetVM.Target == shipControllerMachine && navalMissionShipControlPointMarkerTargetVM.IsPersistent == flag)
			{
				flag2 = true;
				break;
			}
		}
		if (!flag2)
		{
			NavalMissionShipControlPointMarkerTargetVM navalMissionShipControlPointMarkerTargetVM2 = new NavalMissionShipControlPointMarkerTargetVM(shipControllerMachine);
			navalMissionShipControlPointMarkerTargetVM2.IsPersistent = flag;
			markers.Add(navalMissionShipControlPointMarkerTargetVM2);
		}
	}
}
