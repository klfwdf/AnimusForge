using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.AI.Tactics;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline.MissionControllers;

public class NeutralWandererShipSpawnMissionController : MissionLogic
{
	private class WandererShipData
	{
		public readonly int TagNumber;

		public readonly GameEntity SpawnPointEntity;

		private readonly List<GameEntity> _targetPoints = new List<GameEntity>();

		private bool _isTargetReversed;

		public MissionShip WandererShip { get; private set; }

		public GameEntity CurrentTarget { get; private set; }

		public WandererShipData(int tagNumber, GameEntity spawnPointEntity)
		{
			TagNumber = tagNumber;
			SpawnPointEntity = spawnPointEntity;
		}

		public void AddTargetPoint(GameEntity targetPoint)
		{
			_targetPoints.Add(targetPoint);
		}

		public void SetWandererShip(MissionShip ship)
		{
			WandererShip = ship;
		}

		public void ChangeToNextTarget()
		{
			if (CurrentTarget == null)
			{
				CurrentTarget = _targetPoints[0];
				return;
			}
			if (_isTargetReversed)
			{
				for (int num = _targetPoints.Count - 1; num >= 0; num--)
				{
					if (_targetPoints[num] == CurrentTarget)
					{
						if (num == 0)
						{
							_isTargetReversed = false;
							CurrentTarget = _targetPoints[num + 1];
						}
						else
						{
							CurrentTarget = _targetPoints[num - 1];
						}
						break;
					}
				}
				return;
			}
			for (int i = 0; i < _targetPoints.Count; i++)
			{
				if (_targetPoints[i] == CurrentTarget)
				{
					if (i == _targetPoints.Count - 1)
					{
						_isTargetReversed = true;
						CurrentTarget = _targetPoints[i - 1];
					}
					else
					{
						CurrentTarget = _targetPoints[i + 1];
					}
					break;
				}
			}
		}
	}

	private enum WandererShipControllerState
	{
		None,
		SpawnShips,
		SpawnTroops,
		MoveShips,
		End
	}

	private const string WandererShipSpawnPointTagExpression = "wanderer_ship(_\\d+)*_spawnpoint";

	private const string WandererShipTargetPointTagExpression = "wanderer_ship(_\\d+)*_target(_\\d+)*";

	private readonly List<string> _wandererShipIdList = new List<string> { "western_trade_ship_storyline", "sturgia_heavy_ship", "ship_lodya_storyline", "ship_birlinn_storyline" };

	private readonly List<string> _wandererShipTroopIdList = new List<string> { "sea_hounds", "gangradirs_kin_melee" };

	private readonly List<WandererShipData> _wandererShipData = new List<WandererShipData>();

	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	private Queue<Formation> _availableNeutralFormations = new Queue<Formation>();

	private WandererShipControllerState _currentState;

	public override void OnAfterMissionCreated()
	{
		base.OnAfterMissionCreated();
	}

	public override void AfterStart()
	{
		base.AfterStart();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		Team playerAllyTeam = base.Mission.PlayerAllyTeam;
		_availableNeutralFormations.Enqueue(playerAllyTeam.GetFormation(FormationClass.Infantry));
		_availableNeutralFormations.Enqueue(playerAllyTeam.GetFormation(FormationClass.Ranged));
		_availableNeutralFormations.Enqueue(playerAllyTeam.GetFormation(FormationClass.Cavalry));
		_availableNeutralFormations.Enqueue(playerAllyTeam.GetFormation(FormationClass.HorseArcher));
		_availableNeutralFormations.Enqueue(playerAllyTeam.GetFormation(FormationClass.NumberOfDefaultFormations));
		_availableNeutralFormations.Enqueue(playerAllyTeam.GetFormation(FormationClass.HeavyInfantry));
		_availableNeutralFormations.Enqueue(playerAllyTeam.GetFormation(FormationClass.LightCavalry));
		_availableNeutralFormations.Enqueue(playerAllyTeam.GetFormation(FormationClass.HeavyCavalry));
		_availableNeutralFormations.Enqueue(playerAllyTeam.GetFormation(FormationClass.NumberOfRegularFormations));
		_availableNeutralFormations.Enqueue(playerAllyTeam.GetFormation(FormationClass.Bodyguard));
		playerAllyTeam.SetIsEnemyOf(Mission.GetTeam(TeamSideEnum.EnemyTeam), isEnemyOf: false);
		playerAllyTeam.SetIsEnemyOf(Mission.GetTeam(TeamSideEnum.PlayerTeam), isEnemyOf: false);
		CollectWandererShipData();
		_currentState = WandererShipControllerState.SpawnShips;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		switch (_currentState)
		{
		case WandererShipControllerState.SpawnShips:
			SpawnWandererShips();
			_currentState = WandererShipControllerState.SpawnTroops;
			break;
		case WandererShipControllerState.SpawnTroops:
			SpawnWandererShipTroops();
			_currentState = WandererShipControllerState.MoveShips;
			break;
		case WandererShipControllerState.MoveShips:
			HandleWandererShipMovements();
			break;
		case WandererShipControllerState.None:
		case WandererShipControllerState.End:
			break;
		}
	}

	private void CollectWandererShipData()
	{
		foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTagExpression("wanderer_ship(_\\d+)*_spawnpoint"))
		{
			int tagNumber = int.Parse(item.Tags.FirstOrDefault().Split(new char[1] { '_' })[2]);
			_wandererShipData.Add(new WandererShipData(tagNumber, item));
		}
		Dictionary<int, List<GameEntity>> dictionary = new Dictionary<int, List<GameEntity>>();
		foreach (GameEntity item2 in Mission.Current.Scene.FindEntitiesWithTagExpression("wanderer_ship(_\\d+)*_target(_\\d+)*"))
		{
			int key = int.Parse(item2.Tags.FirstOrDefault().Split(new char[1] { '_' })[2]);
			if (!dictionary.ContainsKey(key))
			{
				dictionary[key] = new List<GameEntity>();
			}
			dictionary[key].Add(item2);
		}
		foreach (KeyValuePair<int, List<GameEntity>> targetKvp in dictionary)
		{
			GameEntity[] array = new GameEntity[targetKvp.Value.Count];
			foreach (GameEntity item3 in targetKvp.Value)
			{
				int num = int.Parse(item3.Tags.FirstOrDefault().Split(new char[1] { '_' })[^1]);
				array[num - 1] = item3;
			}
			WandererShipData wandererShipData = _wandererShipData.First((WandererShipData d) => d.TagNumber == targetKvp.Key);
			GameEntity[] array2 = array;
			foreach (GameEntity targetPoint in array2)
			{
				wandererShipData.AddTargetPoint(targetPoint);
			}
		}
	}

	private void SpawnWandererShips()
	{
		foreach (WandererShipData wandererShipDatum in _wandererShipData)
		{
			if (!_availableNeutralFormations.IsEmpty())
			{
				MissionShip wandererShip = CreateShip(_wandererShipIdList.GetRandomElement(), wandererShipDatum.SpawnPointEntity, _availableNeutralFormations.Dequeue());
				wandererShipDatum.SetWandererShip(wandererShip);
			}
		}
	}

	private MissionShip CreateShip(string shipHullId, GameEntity spawnPoint, Formation formation)
	{
		MatrixFrame shipFrame = spawnPoint.GetGlobalFrame();
		float waterLevelAtPosition = Mission.Current.Scene.GetWaterLevelAtPosition(spawnPoint.GlobalPosition.AsVec2, useWaterRenderer: false, checkWaterBodyEntities: false);
		shipFrame.origin = new Vec3(spawnPoint.GlobalPosition.x, spawnPoint.GlobalPosition.y, waterLevelAtPosition);
		Ship shipOrigin = new Ship(Campaign.Current.ObjectManager.GetObject<ShipHull>(shipHullId));
		MissionShip missionShip = _navalShipsLogic.SpawnShip(shipOrigin, in shipFrame, formation.Team, formation);
		missionShip.ShipOrder.FormationJoinShip(formation);
		return missionShip;
	}

	private void SpawnWandererShipTroops()
	{
		Team playerAllyTeam = base.Mission.PlayerAllyTeam;
		TeamAINavalComponent teamAI = new TeamAINavalComponent(base.Mission, playerAllyTeam, 5f, 1f);
		playerAllyTeam.AddTeamAI(teamAI);
		playerAllyTeam.AddTacticOption(new TacticNavalBalancedOffense(playerAllyTeam));
		_navalAgentsLogic.SetDeploymentMode(value: true);
		_navalShipsLogic.SetDeploymentMode(value: true);
		foreach (WandererShipData wandererShipDatum in _wandererShipData)
		{
			CharacterObject troop = MBObjectManager.Instance.GetObject<CharacterObject>(_wandererShipTroopIdList.GetRandomElement());
			int num = MBRandom.RandomInt(7, 13);
			_navalAgentsLogic.SetDesiredTroopCountOfShip(wandererShipDatum.WandererShip, num);
			for (int i = 0; i < num; i++)
			{
				_navalAgentsLogic.AddReservedTroopToShip(new SimpleAgentOrigin(troop), wandererShipDatum.WandererShip);
			}
		}
		_navalAgentsLogic.SetDeploymentMode(value: false);
		_navalShipsLogic.SetDeploymentMode(value: false);
	}

	private void HandleWandererShipMovements()
	{
		foreach (WandererShipData wandererShipDatum in _wandererShipData)
		{
			if (wandererShipDatum.CurrentTarget == null || wandererShipDatum.WandererShip.GlobalFrame.origin.Distance(wandererShipDatum.CurrentTarget.GlobalPosition) <= 100f)
			{
				wandererShipDatum.ChangeToNextTarget();
			}
			else
			{
				wandererShipDatum.WandererShip.ShipOrder.SetShipMovementOrder(wandererShipDatum.CurrentTarget.GlobalPosition.AsVec2);
			}
		}
	}
}
