using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipControl;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline.MissionControllers;

public class Quest5WanderingShipsMissionLogic : MissionLogic
{
	private const string PropShip1StringId = "nord_medium_ship";

	private const string PropShip2StringId = "eastern_heavy_ship";

	private const string PropShipTroopStringId = "gangster_1";

	private const int WayPoint1Count = 6;

	private const int WayPoint2Count = 6;

	private const float WayPointSuccessDistance = 10f;

	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	private MissionShip _propShip1;

	private MissionShip _propShip2;

	private List<GameEntity> _wayPoints1 = new List<GameEntity>();

	private List<GameEntity> _wayPoints2 = new List<GameEntity>();

	private int _currentWaypointIndex1;

	private int _currentWaypointIndex2;

	public override void EarlyStart()
	{
		base.Mission.Teams.Add(BattleSideEnum.Defender, Clan.PlayerClan.Color, Clan.PlayerClan.Color2, Clan.PlayerClan.Banner);
		base.Mission.PlayerTeam = base.Mission.DefenderTeam;
	}

	public override void AfterStart()
	{
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_navalAgentsLogic.UpdateTeamAgentsData();
		SetupPropShips();
	}

	private void SetupPropShips()
	{
		InitializeWaypoints();
		SpawnPropShips();
	}

	private void InitializeWaypoints()
	{
		for (int i = 1; i <= 6; i++)
		{
			GameEntity item = Mission.Current.Scene.FindEntityWithTag("propship_1_waypoint_" + i);
			_wayPoints1.Add(item);
		}
		for (int j = 1; j <= 6; j++)
		{
			GameEntity item2 = Mission.Current.Scene.FindEntityWithTag("propship_2_waypoint_" + j);
			_wayPoints2.Add(item2);
		}
	}

	private void SpawnPropShips()
	{
		_propShip1 = CreateShip("nord_medium_ship", "propship_1_waypoint_1", base.Mission.PlayerAllyTeam.GetFormation(FormationClass.Infantry));
		_propShip1.SetController(ShipControllerType.AI);
		SpawnPropShipAgents(_propShip1, "gangster_1");
		_propShip2 = CreateShip("eastern_heavy_ship", "propship_2_waypoint_1", base.Mission.PlayerAllyTeam.GetFormation(FormationClass.Cavalry));
		_propShip2.SetController(ShipControllerType.AI);
		SpawnPropShipAgents(_propShip2, "gangster_1");
	}

	private MissionShip CreateShip(string shipHullId, string spawnPointId, Formation formation, bool spawnAnchored = false, List<KeyValuePair<string, string>> additionalUpgradePieces = null, Figurehead figurehead = null)
	{
		GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag(spawnPointId);
		MatrixFrame shipFrame = gameEntity.GetGlobalFrame();
		float waterLevelAtPosition = Mission.Current.Scene.GetWaterLevelAtPosition(gameEntity.GlobalPosition.AsVec2, useWaterRenderer: false, checkWaterBodyEntities: false);
		shipFrame.origin = new Vec3(gameEntity.GlobalPosition.x, gameEntity.GlobalPosition.y, waterLevelAtPosition);
		Ship ship = new Ship(Campaign.Current.ObjectManager.GetObject<ShipHull>(shipHullId));
		if (additionalUpgradePieces != null)
		{
			foreach (KeyValuePair<string, string> additionalUpgradePiece in additionalUpgradePieces)
			{
				ShipUpgradePiece newUpgradePiece = MBObjectManager.Instance.GetObject<ShipUpgradePiece>(additionalUpgradePiece.Value);
				ship.EquipUpgradePiece(additionalUpgradePiece.Key, newUpgradePiece);
			}
		}
		if (figurehead != null)
		{
			ship.ChangeFigurehead(figurehead);
		}
		MissionShip missionShip = _navalShipsLogic.SpawnShip(ship, in shipFrame, formation.Team, formation, spawnAnchored);
		missionShip.ShipOrder.FormationJoinShip(formation);
		return missionShip;
	}

	private void SpawnPropShipAgents(MissionShip ship, string troopType)
	{
		int num = ship.CrewSizeOnMainDeck / 2;
		NavalAgentsLogic missionBehavior = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		missionBehavior.SetDesiredTroopCountOfShip(ship, num);
		BasicCharacterObject basicCharacterObject = Campaign.Current.ObjectManager.GetObject<CharacterObject>(troopType);
		List<MatrixFrame> list = ship.OuterDeckLocalFrames.Concat(ship.InnerDeckLocalFrames).ToList();
		for (int i = 0; i < list.Count() && i < num; i++)
		{
			MatrixFrame matrixFrame = list[i];
			Vec3 position = matrixFrame.origin;
			Vec2 direction = matrixFrame.rotation.f.AsVec2;
			AgentBuildData agentBuildData = new AgentBuildData(basicCharacterObject).TroopOrigin(new SimpleAgentOrigin(basicCharacterObject)).Team(ship.Team).InitialPosition(in position)
				.InitialDirection(in direction)
				.NoHorses(noHorses: true)
				.NoWeapons(noWeapons: false);
			Agent agent = Mission.Current.SpawnAgent(agentBuildData);
			missionBehavior.AddAgentToShip(agent, ship);
			agent.SetAgentFlags((AgentFlag)((uint)agent.GetAgentFlags() & 0xFFFEFFFFu));
			agent.ToggleInvulnerable();
		}
	}

	public override void OnMissionTick(float dt)
	{
		HandlePropShipOrders();
	}

	private void HandlePropShipOrders()
	{
		if (!_wayPoints1.IsEmpty())
		{
			GameEntity gameEntity = _wayPoints1[_currentWaypointIndex1];
			if ((gameEntity.GlobalPosition - _propShip1.GlobalFrame.origin).LengthSquared <= 100f)
			{
				_currentWaypointIndex1 = (_currentWaypointIndex1 + 1) % 6;
				gameEntity = _wayPoints1[_currentWaypointIndex1];
			}
			_propShip1.ShipOrder.SetShipMovementOrder(gameEntity.GlobalPosition.AsVec2);
		}
		if (!_wayPoints2.IsEmpty())
		{
			GameEntity gameEntity2 = _wayPoints2[_currentWaypointIndex2];
			if ((gameEntity2.GlobalPosition - _propShip2.GlobalFrame.origin).LengthSquared <= 100f)
			{
				_currentWaypointIndex2 = (_currentWaypointIndex2 + 1) % 6;
				gameEntity2 = _wayPoints2[_currentWaypointIndex2];
			}
			_propShip2.ShipOrder.SetShipMovementOrder(gameEntity2.GlobalPosition.AsVec2);
		}
	}

	public void OnPhase2Started()
	{
		if (_propShip1 != null)
		{
			_navalShipsLogic.RemoveShip(_propShip1);
		}
		if (_propShip2 != null)
		{
			_navalShipsLogic.RemoveShip(_propShip2);
		}
	}
}
