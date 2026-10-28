using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class ShipRetreatLogic : MissionLogic
{
	private const float RetreatCheckInterval = 5f;

	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	private NavalBattleEndLogic _navalBattleEndLogic;

	private BasicMissionTimer _checkRetreatingTimer;

	private MBList<MissionShip> _tempRetreatedShips = new MBList<MissionShip>();

	private MBList<Agent> _tempOffShipAgents = new MBList<Agent>();

	private MBList<IAgentOriginBase> _tempRoutedReservedTroops = new MBList<IAgentOriginBase>();

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_checkRetreatingTimer = new BasicMissionTimer();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_navalBattleEndLogic = base.Mission.GetMissionBehavior<NavalBattleEndLogic>();
	}

	public override void OnDeploymentFinished()
	{
		_checkRetreatingTimer.Reset();
	}

	public override void OnMissionTick(float dt)
	{
		if (!base.Mission.IsDeploymentFinished || !(_checkRetreatingTimer.ElapsedTime > 5f))
		{
			return;
		}
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			if (allShip.IsShipOrderActive && allShip.ShipOrder.MovementOrderEnum == ShipOrder.ShipMovementOrderEnum.Retreat)
			{
				Vec2 asVec = allShip.GlobalFrame.origin.AsVec2;
				float num = allShip.Physics.PhysicsBoundingBoxWithChildrenSize.y / 2f + 0.5f;
				if (asVec.DistanceSquared(base.Mission.GetClosestBoundaryPosition(asVec)) < num * num || !base.Mission.IsPositionInsideBoundaries(asVec))
				{
					_tempRetreatedShips.Add(allShip);
				}
			}
		}
		while (_tempRetreatedShips.Count > 0)
		{
			MissionShip missionShip = _tempRetreatedShips[_tempRetreatedShips.Count - 1];
			_tempRetreatedShips.RemoveAt(_tempRetreatedShips.Count - 1);
			foreach (Agent item in _navalAgentsLogic.GetActiveAgentsOfShip(missionShip))
			{
				if (item.GetComponent<AgentNavalComponent>().SteppedShip != missionShip)
				{
					_tempOffShipAgents.Add(item);
				}
			}
			while (_tempOffShipAgents.Count > 0)
			{
				Agent agent = _tempOffShipAgents[_tempOffShipAgents.Count - 1];
				_tempOffShipAgents.RemoveAt(_tempOffShipAgents.Count - 1);
				_navalAgentsLogic.RemoveAgentFromShip(agent, missionShip);
			}
			_navalAgentsLogic.FillReservedTroopsOfShip(missionShip, _tempRoutedReservedTroops);
			while (_tempRoutedReservedTroops.Count > 0)
			{
				IAgentOriginBase agentOriginBase = _tempRoutedReservedTroops[_tempRoutedReservedTroops.Count - 1];
				_tempRoutedReservedTroops.RemoveAt(_tempRoutedReservedTroops.Count - 1);
				agentOriginBase.SetRouted(isOrderRetreat: true);
			}
			_navalShipsLogic.RemoveShip(missionShip);
		}
		_checkRetreatingTimer.Reset();
	}
}
