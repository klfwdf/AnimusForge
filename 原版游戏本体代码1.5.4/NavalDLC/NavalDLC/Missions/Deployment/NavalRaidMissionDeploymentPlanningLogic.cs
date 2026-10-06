using System;
using System.Linq;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Deployment;

public class NavalRaidMissionDeploymentPlanningLogic : MissionDeploymentPlanningLogic
{
	public const string DefenderPlayerSpawnEntityTag = "player_spawn_frame";

	private MBList<(Team team, NavalTeamDeploymentPlan plan)> _attackerSideTeamDeploymentPlans = new MBList<(Team, NavalTeamDeploymentPlan)>();

	private MBList<(Team team, DefaultTeamDeploymentPlan plan)> _defenderSideTeamDeploymentPlans = new MBList<(Team, DefaultTeamDeploymentPlan)>();

	private WorldFrame? _defenderSidePlayerSpawnFrame;

	private FormationSceneSpawnEntry[,] _formationSceneSpawnEntries;

	public override void Initialize()
	{
		_attackerSideTeamDeploymentPlans.Clear();
		_defenderSideTeamDeploymentPlans.Clear();
		foreach (Team team in base.Mission.Teams)
		{
			if (team.IsDefender)
			{
				DefaultTeamDeploymentPlan item = new DefaultTeamDeploymentPlan(base.Mission, team);
				_defenderSideTeamDeploymentPlans.Add((team, item));
			}
			else
			{
				NavalTeamDeploymentPlan item2 = new NavalTeamDeploymentPlan(base.Mission, team);
				_attackerSideTeamDeploymentPlans.Add((team, item2));
			}
		}
	}

	public override void ClearDeploymentPlan(Team team)
	{
		GetTeamPlan<ITeamDeploymentPlan>(team).ClearPlan();
	}

	public override bool SupportsReinforcements()
	{
		return true;
	}

	public override bool SupportsNavmesh(Team team)
	{
		if (team.IsDefender)
		{
			return true;
		}
		return false;
	}

	public override void UpdateReinforcementPlan(Team team)
	{
		GetTeamPlan<DefaultTeamDeploymentPlan>(team).UpdateReinforcementPlans();
	}

	public override bool HasPlayerSpawnFrame(BattleSideEnum battleSide)
	{
		if (battleSide == BattleSideEnum.Defender)
		{
			return _defenderSidePlayerSpawnFrame.HasValue;
		}
		return false;
	}

	public override bool GetPlayerSpawnFrame(BattleSideEnum battleSide, out WorldPosition position, out Vec2 direction)
	{
		if (battleSide == BattleSideEnum.Defender && _defenderSidePlayerSpawnFrame.HasValue)
		{
			position = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, _defenderSidePlayerSpawnFrame.Value.Origin.GetGroundVec3(), hasValidZ: false);
			direction = _defenderSidePlayerSpawnFrame.Value.Rotation.f.AsVec2.Normalized();
			return true;
		}
		position = WorldPosition.Invalid;
		direction = Vec2.Invalid;
		return false;
	}

	public void ClearAddedShips(Team team)
	{
		GetTeamPlan<NavalTeamDeploymentPlan>(team).ClearAddedShips();
	}

	public void ClearAddedTroops(Team team)
	{
		GetTeamPlan<DefaultTeamDeploymentPlan>(team).ClearAddedTroops();
	}

	public override void ClearAll()
	{
		foreach (var defenderSideTeamDeploymentPlan in _defenderSideTeamDeploymentPlans)
		{
			defenderSideTeamDeploymentPlan.plan.ClearAddedTroops();
			defenderSideTeamDeploymentPlan.plan.ClearPlan();
		}
		foreach (var attackerSideTeamDeploymentPlan in _attackerSideTeamDeploymentPlans)
		{
			attackerSideTeamDeploymentPlan.plan.ClearAddedShips();
			attackerSideTeamDeploymentPlan.plan.ClearPlan();
		}
	}

	public void AddShip(Team team, FormationClass formationIndex, IShipOrigin shipOrigin)
	{
		GetTeamPlan<NavalTeamDeploymentPlan>(team).AddShip(formationIndex, shipOrigin);
	}

	public bool RemoveShip(Team team, FormationClass formationIndex)
	{
		return GetTeamPlan<NavalTeamDeploymentPlan>(team).RemoveShip(formationIndex);
	}

	public void AddTroops(Team team, FormationClass formationClass, int footTroopCount, int mountedTroopCount = 0, bool isReinforcement = false)
	{
		_ = team.Side;
		GetTeamPlan<DefaultTeamDeploymentPlan>(team).AddTroops(formationClass, footTroopCount, mountedTroopCount, isReinforcement);
	}

	public void SetSpawnWithHorses(Team team, bool spawnWithHorses)
	{
		GetTeamPlan<DefaultTeamDeploymentPlan>(team).SetSpawnWithHorses(spawnWithHorses);
	}

	public override void MakeDeploymentPlan(Team team, float spawnPathOffset = 0f, float targetOffset = 0f)
	{
		if (!IsPlanMade(team))
		{
			MakeDeploymentPlanAux(team, isReinforcement: false);
			if (IsPlanMade(team, out var isFirstPlan))
			{
				base.Mission.OnDeploymentPlanMade(team, isFirstPlan);
			}
		}
	}

	public void MakeReinforcementDeploymentPlan(Team team)
	{
		if (!IsReinforcementPlanMade(team))
		{
			MakeDeploymentPlanAux(team, isReinforcement: true);
		}
	}

	public override bool RemakeDeploymentPlan(Team team)
	{
		IsPlanMade(team);
		if (team.IsDefender)
		{
			(int, int)[] array = new(int, int)[11];
			foreach (Agent item in base.Mission.AllAgents.Where((Agent agent) => agent.IsHuman && agent.Team != null && agent.Team == team && agent.Formation != null))
			{
				int formationIndex = (int)item.Formation.FormationIndex;
				(int, int) tuple = array[formationIndex];
				array[formationIndex] = (item.HasMount ? (tuple.Item1, tuple.Item2 + 1) : (tuple.Item1 + 1, tuple.Item2));
			}
			if (!IsInitialPlanSuitableForFormations(team, array))
			{
				ClearAddedTroops(team);
				ClearDeploymentPlan(team);
				for (int num = 0; num < 11; num++)
				{
					var (num2, num3) = array[num];
					if (num2 + num3 > 0)
					{
						AddTroops(team, (FormationClass)num, num2, num3);
					}
				}
				MakeDeploymentPlan(team);
				return IsPlanMade(team);
			}
			return false;
		}
		ClearAddedShips(team);
		ClearDeploymentPlan(team);
		NavalShipsLogic missionBehavior = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		for (int num4 = 0; num4 < 11; num4++)
		{
			FormationClass formationIndex2 = (FormationClass)num4;
			ShipAssignment shipAssignment = missionBehavior.GetShipAssignment(team.TeamSide, formationIndex2);
			if (shipAssignment.IsSet)
			{
				AddShip(team, formationIndex2, shipAssignment.ShipOrigin);
			}
		}
		MakeDeploymentPlan(team);
		return IsPlanMade(team);
	}

	public override bool IsPositionInsideDeploymentBoundaries(Team team, in Vec2 position)
	{
		ITeamDeploymentPlan teamPlan = GetTeamPlan<ITeamDeploymentPlan>(team);
		(string, MBList<Vec2>) containingBoundaryTuple;
		if (teamPlan.HasDeploymentBoundaries())
		{
			return teamPlan.IsPositionInsideDeploymentBoundaries(in position, out containingBoundaryTuple);
		}
		Debug.FailedAssert("Cannot check if position is within deployment boundaries as requested team " + team.TeamIndex + " does not have deployment boundaries.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\Deployment\\NavalRaidMissionDeploymentPlanningLogic.cs", "IsPositionInsideDeploymentBoundaries", 278);
		return false;
	}

	public override Vec2 GetClosestDeploymentBoundaryPosition(Team team, in Vec2 position)
	{
		ITeamDeploymentPlan teamPlan = GetTeamPlan<ITeamDeploymentPlan>(team);
		if (teamPlan.HasDeploymentBoundaries())
		{
			return teamPlan.GetClosestDeploymentBoundaryPosition(in position);
		}
		Debug.FailedAssert("Cannot retrieve closest deployment boundary position as requested team (index: " + team.TeamIndex + ") does not have deployment boundaries.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\Deployment\\NavalRaidMissionDeploymentPlanningLogic.cs", "GetClosestDeploymentBoundaryPosition", 290);
		return position;
	}

	public override void ProjectPositionToDeploymentBoundaries(Team team, ref WorldPosition endPosition)
	{
		if (HasDeploymentBoundaries(team) && !IsPositionInsideDeploymentBoundaries(team, endPosition.AsVec2) && GetPathDeploymentBoundaryIntersection(team, GetNavmeshValidPositionInDeploymentZone(team), in endPosition, out var foundPosition))
		{
			endPosition = foundPosition;
		}
	}

	public override bool GetPathDeploymentBoundaryIntersection(Team team, in WorldPosition startPosition, in WorldPosition endPosition, out WorldPosition intersection)
	{
		return GetTeamPlan<DefaultTeamDeploymentPlan>(team).GetPathDeploymentBoundaryIntersection(in startPosition, in endPosition, out intersection);
	}

	public override float GetSpawnPathOffset(Team team)
	{
		return 0f;
	}

	public override MatrixFrame GetZoomFocusFrame(Team team)
	{
		Vec2 halfExtents;
		if (team.IsDefender)
		{
			return GetFormationsCenterFrameAndExtents(team, out halfExtents);
		}
		GetTeamPlan<NavalTeamDeploymentPlan>(team);
		return GetFormationsCenterFrameAndExtents(team, out halfExtents);
	}

	public override float GetZoomOffset(Team team, float fovAngle)
	{
		ITeamDeploymentPlan teamPlan = GetTeamPlan<ITeamDeploymentPlan>(team);
		Vec2 halfExtents;
		MatrixFrame formationsCenterFrameAndExtents = GetFormationsCenterFrameAndExtents(team, out halfExtents);
		float num = float.MinValue;
		for (int i = 0; i < 11; i++)
		{
			IFormationDeploymentPlan formationPlan = teamPlan.GetFormationPlan((FormationClass)i);
			if (formationPlan.HasFrame())
			{
				float b = formationPlan.GetFrame().origin.AsVec2.DistanceSquared(formationsCenterFrameAndExtents.origin.AsVec2);
				num = TaleWorlds.Library.MathF.Max(num, b);
			}
		}
		return (TaleWorlds.Library.MathF.Sqrt(num) + 20f) / TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Tan(fovAngle / 2f), 0.01f);
	}

	public override IFormationDeploymentPlan GetFormationPlan(Team team, FormationClass fClass, bool isReinforcement = false)
	{
		ITeamDeploymentPlan teamPlan = GetTeamPlan<ITeamDeploymentPlan>(team);
		if (team.IsAttacker)
		{
			return teamPlan.GetFormationPlan(fClass);
		}
		return teamPlan.GetFormationPlan(fClass, isReinforcement);
	}

	public override bool IsPlanMade(Team team)
	{
		return GetTeamPlanAux(team)?.IsPlanMade() ?? false;
	}

	public bool IsReinforcementPlanMade(Team team)
	{
		return GetTeamPlanAux(team)?.IsPlanMade(isReinforcement: true) ?? false;
	}

	public override bool IsPlanMade(Team team, out bool isFirstPlan)
	{
		isFirstPlan = false;
		ITeamDeploymentPlan teamPlanAux = GetTeamPlanAux(team);
		if (teamPlanAux != null && teamPlanAux.IsPlanMade())
		{
			isFirstPlan = teamPlanAux.IsFirstPlan();
			return true;
		}
		return false;
	}

	public override bool HasDeploymentBoundaries(Team team)
	{
		return GetTeamPlanAux(team)?.HasDeploymentBoundaries() ?? false;
	}

	public override MatrixFrame GetDeploymentZoneFrame(Team team)
	{
		return GetTeamPlan<ITeamDeploymentPlan>(team).GetDeploymentZoneFrame();
	}

	public override MatrixFrame GetFormationsCenterFrameAndExtents(Team team, out Vec2 halfExtents, bool ignoreDimensionlessFormations = true)
	{
		if (team.IsAttacker)
		{
			return GetTeamPlan<NavalTeamDeploymentPlan>(team).GetFormationsCenterFrameAndExtents(out halfExtents, ignoreDimensionlessFormations);
		}
		return GetTeamPlan<DefaultTeamDeploymentPlan>(team).GetFormationsCenterFrameAndExtents(out halfExtents, ignoreDimensionlessFormations);
	}

	public float GetTargetOffset(Team team)
	{
		return GetTeamPlan<ITeamDeploymentPlan>(team).GetTargetOffset();
	}

	public override MBReadOnlyList<(string, MBList<Vec2>)> GetDeploymentBoundaries(Team team)
	{
		return GetTeamPlan<ITeamDeploymentPlan>(team).GetDeploymentBoundaries();
	}

	public virtual bool GetMeanBoundaryPosition(Team team, out Vec2 meanPosition, int boundaryIndex = 0)
	{
		NavalTeamDeploymentPlan teamPlan = GetTeamPlan<NavalTeamDeploymentPlan>(team);
		if (teamPlan != null && teamPlan.HasDeploymentBoundaries())
		{
			meanPosition = teamPlan.GetMeanBoundaryPosition(boundaryIndex);
			return true;
		}
		meanPosition = Vec2.Invalid;
		return false;
	}

	private T GetTeamPlan<T>(Team team) where T : ITeamDeploymentPlan
	{
		ITeamDeploymentPlan teamPlanAux;
		if ((teamPlanAux = GetTeamPlanAux(team)) is T)
		{
			return (T)teamPlanAux;
		}
		Debug.FailedAssert("Unable to cast team plan to given type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\Deployment\\NavalRaidMissionDeploymentPlanningLogic.cs", "GetTeamPlan", 530);
		return default(T);
	}

	private ITeamDeploymentPlan GetTeamPlanAux(Team team)
	{
		if (team.IsDefender)
		{
			return _defenderSideTeamDeploymentPlans.FirstOrDefault(((Team team, DefaultTeamDeploymentPlan plan) t) => t.team == team).plan;
		}
		if (team.IsAttacker)
		{
			return _attackerSideTeamDeploymentPlans.FirstOrDefault(((Team team, NavalTeamDeploymentPlan plan) t) => t.team == team).plan;
		}
		return null;
	}

	private void MakeDeploymentPlanAux(Team team, bool isReinforcement)
	{
		ITeamDeploymentPlan teamPlan = GetTeamPlan<ITeamDeploymentPlan>(team);
		if (teamPlan.IsPlanMade(isReinforcement))
		{
			teamPlan.ClearPlan();
		}
		if (_formationSceneSpawnEntries == null)
		{
			ReadSpawnEntitiesFromScene();
		}
		teamPlan.MakeDeploymentPlan(0f, 0f, _formationSceneSpawnEntries, isReinforcement);
	}

	private void ReadSpawnEntitiesFromScene()
	{
		_defenderSidePlayerSpawnFrame = null;
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("player_spawn_frame");
		if (gameEntity != null)
		{
			MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
			_defenderSidePlayerSpawnFrame = new WorldFrame(origin: new WorldPosition(base.Mission.Scene, UIntPtr.Zero, globalFrame.origin, hasValidZ: false), rotation: globalFrame.rotation);
		}
		_formationSceneSpawnEntries = new FormationSceneSpawnEntry[2, 11];
		Scene scene = base.Mission.Scene;
		for (int i = 0; i < 2; i++)
		{
			string text = ((i == 1) ? "attacker_" : "defender_");
			for (int j = 0; j < 11; j++)
			{
				FormationClass formationClass = (FormationClass)j;
				string text2 = text + formationClass.GetName().ToLower();
				string tag = text2 + "_reinforcement";
				WeakGameEntity weakGameEntity = scene.FindWeakEntityWithTag(text2);
				WeakGameEntity? weakGameEntity2 = null;
				if (weakGameEntity == null)
				{
					FormationClass formationClass2 = formationClass.FallbackClass();
					int num = (int)formationClass2;
					FormationSceneSpawnEntry formationSceneSpawnEntry = _formationSceneSpawnEntries[i, num];
					if (formationSceneSpawnEntry.SpawnEntity != null)
					{
						weakGameEntity = formationSceneSpawnEntry.SpawnEntity.WeakEntity;
						weakGameEntity2 = formationSceneSpawnEntry.ReinforcementSpawnEntity.WeakEntity;
					}
					else
					{
						text2 = text + formationClass2.GetName().ToLower();
						tag = text2 + "_reinforcement";
						weakGameEntity = scene.FindWeakEntityWithTag(text2);
						weakGameEntity2 = scene.FindWeakEntityWithTag(tag);
					}
					formationClass = ((weakGameEntity != null) ? formationClass2 : FormationClass.NumberOfAllFormations);
				}
				else
				{
					weakGameEntity2 = scene.FindWeakEntityWithTag(tag);
				}
				GameEntity gameEntity2 = null;
				GameEntity gameEntity3 = null;
				if (weakGameEntity.IsValid)
				{
					gameEntity2 = GameEntity.CreateFromWeakEntity(weakGameEntity);
					if (weakGameEntity2.HasValue && weakGameEntity2.Value.IsValid)
					{
						gameEntity3 = GameEntity.CreateFromWeakEntity(weakGameEntity2.Value);
					}
				}
				if (gameEntity3 == null)
				{
					gameEntity3 = gameEntity2;
				}
				_formationSceneSpawnEntries[i, j] = new FormationSceneSpawnEntry(formationClass, gameEntity2, gameEntity3);
			}
		}
	}

	private bool IsInitialPlanSuitableForFormations(Team team, (int footTroopCount, int mountedTroopCount)[] troopDataPerFormationClass)
	{
		return GetTeamPlan<DefaultTeamDeploymentPlan>(team).IsInitialPlanSuitableForFormations(troopDataPerFormationClass);
	}

	private WorldPosition GetNavmeshValidPositionInDeploymentZone(Team team)
	{
		ITeamDeploymentPlan teamPlan = GetTeamPlan<ITeamDeploymentPlan>(team);
		Scene scene = Mission.Current.Scene;
		Vec3 position = teamPlan.GetDeploymentZoneFrame().origin;
		UIntPtr navigationMeshForPosition = scene.GetNavigationMeshForPosition(in position);
		if (navigationMeshForPosition != UIntPtr.Zero)
		{
			return new WorldPosition(scene, navigationMeshForPosition, position, hasValidZ: false);
		}
		for (FormationClass formationClass = FormationClass.Infantry; formationClass < FormationClass.NumberOfAllFormations; formationClass++)
		{
			IFormationDeploymentPlan formationPlan = teamPlan.GetFormationPlan(formationClass);
			if (formationPlan.HasFrame())
			{
				Vec3 origin = formationPlan.GetFrame().origin;
				return new WorldPosition(scene, UIntPtr.Zero, origin, hasValidZ: false);
			}
		}
		Debug.FailedAssert("Unable to find a formation frame that is on navmesh", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\Deployment\\NavalRaidMissionDeploymentPlanningLogic.cs", "GetNavmeshValidPositionInDeploymentZone", 693);
		return new WorldPosition(scene, UIntPtr.Zero, position, hasValidZ: false);
	}
}
