using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class DefaultMissionDeploymentPlan : IMissionDeploymentPlan
{
	private readonly Mission _mission;

	private readonly MBList<(Team team, DefaultTeamDeploymentPlan plan)> _teamDeploymentPlans = new MBList<(Team, DefaultTeamDeploymentPlan)>();

	private readonly WorldFrame?[] _playerSpawnFrames = new WorldFrame?[2];

	private FormationSceneSpawnEntry[,] _formationSceneSpawnEntries;

	public DefaultMissionDeploymentPlan(Mission mission)
	{
		_mission = mission;
	}

	public void Initialize()
	{
		foreach (Team team in _mission.Teams)
		{
			if (team.Side != BattleSideEnum.None)
			{
				DefaultTeamDeploymentPlan item = new DefaultTeamDeploymentPlan(_mission, team);
				_teamDeploymentPlans.Add((team, item));
			}
		}
		for (int i = 0; i < 2; i++)
		{
			_playerSpawnFrames[i] = null;
		}
	}

	public void ClearDeploymentPlan(Team team)
	{
		GetTeamPlan(team).ClearPlan();
	}

	public void ClearReinforcementPlan(Team team)
	{
		GetTeamPlan(team).ClearPlan(isReinforcement: true);
	}

	public bool HasPlayerSpawnFrame(BattleSideEnum battleSide)
	{
		return _playerSpawnFrames[(int)battleSide].HasValue;
	}

	public bool GetPlayerSpawnFrame(BattleSideEnum battleSide, out WorldPosition position, out Vec2 direction)
	{
		WorldFrame? worldFrame = _playerSpawnFrames[(int)battleSide];
		if (worldFrame.HasValue)
		{
			position = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, worldFrame.Value.Origin.GetGroundVec3(), hasValidZ: false);
			direction = worldFrame.Value.Rotation.f.AsVec2.Normalized();
			return true;
		}
		position = WorldPosition.Invalid;
		direction = Vec2.Invalid;
		return false;
	}

	public static bool HasSignificantMountedTroops(int footTroopCount, int mountedTroopCount)
	{
		return (float)mountedTroopCount / Math.Max(mountedTroopCount + footTroopCount, 1f) >= 0.1f;
	}

	public void ClearAddedTroops(Team team, bool isReinforcement = false)
	{
		GetTeamPlan(team).ClearAddedTroops(isReinforcement);
	}

	public void ClearAll()
	{
		foreach (var teamDeploymentPlan in _teamDeploymentPlans)
		{
			DefaultTeamDeploymentPlan item = teamDeploymentPlan.plan;
			item.ClearAddedTroops();
			item.ClearPlan();
			item.ClearAddedTroops(isReinforcement: true);
			item.ClearPlan(isReinforcement: true);
		}
	}

	public void AddTroops(Team team, FormationClass formationClass, int footTroopCount, int mountedTroopCount = 0, bool isReinforcement = false)
	{
		_ = team.Side;
		GetTeamPlan(team).AddTroops(formationClass, footTroopCount, mountedTroopCount, isReinforcement);
	}

	public void SetSpawnWithHorses(Team team, bool spawnWithHorses)
	{
		GetTeamPlan(team).SetSpawnWithHorses(spawnWithHorses);
	}

	public void MakeDefaultDeploymentPlans()
	{
		foreach (var teamDeploymentPlan in _teamDeploymentPlans)
		{
			Team item = teamDeploymentPlan.team;
			MakeDeploymentPlan(item);
			MakeReinforcementDeploymentPlan(item);
		}
	}

	public void MakeDeploymentPlan(Team team, float spawnPathOffset = 0f, float targetOffset = 0f)
	{
		if (!IsPlanMade(team))
		{
			MakeDeploymentPlanAux(team, isReinforcement: false, spawnPathOffset, targetOffset);
			if (IsPlanMade(team, out var isFirstPlan))
			{
				_mission.OnDeploymentPlanMade(team, isFirstPlan);
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

	public bool RemakeDeploymentPlan(Team team)
	{
		IsPlanMade(team);
		float spawnPathOffset = GetSpawnPathOffset(team);
		float targetOffset = GetTargetOffset(team);
		(int, int)[] array = new(int, int)[11];
		foreach (Agent item in _mission.AllAgents.Where((Agent agent) => agent.IsHuman && agent.Team != null && agent.Team == team && agent.Formation != null))
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
			MakeDeploymentPlan(team, spawnPathOffset, targetOffset);
			return IsPlanMade(team);
		}
		return false;
	}

	public bool IsPositionInsideDeploymentBoundaries(Team team, in Vec2 position)
	{
		(string, MBList<Vec2>) containingBoundaryTuple;
		return GetTeamPlan(team).IsPositionInsideDeploymentBoundaries(in position, out containingBoundaryTuple);
	}

	public Vec2 GetClosestDeploymentBoundaryPosition(Team team, in Vec2 position)
	{
		return GetTeamPlan(team).GetClosestDeploymentBoundaryPosition(in position);
	}

	public bool SupportsReinforcements()
	{
		return true;
	}

	public bool SupportsNavmesh(Team team)
	{
		return true;
	}

	public bool GetPathDeploymentBoundaryIntersection(Team team, in WorldPosition startPosition, in WorldPosition endPosition, out WorldPosition intersection)
	{
		return GetTeamPlan(team).GetPathDeploymentBoundaryIntersection(in startPosition, in endPosition, out intersection);
	}

	public bool IsPositionInsideSiegeDeploymentBoundaries(in Vec2 position)
	{
		bool result = false;
		foreach (ICollection<Vec2> value in _mission.Boundaries.Values)
		{
			if (MBSceneUtilities.IsPointInsideBoundaries(in position, value.ToMBList()))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public float GetSpawnPathOffset(Team team)
	{
		return GetTeamPlan(team).GetSpawnPathOffset();
	}

	public float GetTargetOffset(Team team)
	{
		return GetTeamPlan(team).GetTargetOffset();
	}

	public int GetTroopCount(Team team, bool isReinforcement = false)
	{
		return GetTeamPlan(team).GetTroopCount(isReinforcement);
	}

	public IFormationDeploymentPlan GetFormationPlan(Team team, FormationClass fClass, bool isReinforcement)
	{
		return GetTeamPlan(team).GetFormationPlan(fClass, isReinforcement);
	}

	public bool IsPlanMade(Team team)
	{
		return GetTeamPlanAux(team)?.IsPlanMade() ?? false;
	}

	public bool IsPlanMade(Team team, out bool isFirstPlan)
	{
		DefaultTeamDeploymentPlan teamPlanAux = GetTeamPlanAux(team);
		isFirstPlan = false;
		if (teamPlanAux != null && teamPlanAux.IsPlanMade())
		{
			isFirstPlan = teamPlanAux.IsFirstPlan();
			return true;
		}
		return false;
	}

	public bool IsReinforcementPlanMade(Team team)
	{
		return GetTeamPlanAux(team)?.IsPlanMade(isReinforcement: true) ?? false;
	}

	public bool IsInitialPlanSuitableForFormations(Team team, (int footTroopCount, int mountedTroopCount)[] troopDataPerFormationClass)
	{
		return GetTeamPlan(team).IsInitialPlanSuitableForFormations(troopDataPerFormationClass);
	}

	public bool HasDeploymentBoundaries(Team team)
	{
		return GetTeamPlanAux(team)?.HasDeploymentBoundaries() ?? false;
	}

	public MatrixFrame GetDeploymentFrame(Team team)
	{
		return GetTeamPlan(team).GetDeploymentFrame();
	}

	public void ProjectPositionToDeploymentBoundaries(Team team, ref WorldPosition endPosition)
	{
		if (HasDeploymentBoundaries(team) && !IsPositionInsideDeploymentBoundaries(team, endPosition.AsVec2))
		{
			MatrixFrame deploymentFrame = GetDeploymentFrame(team);
			if (GetPathDeploymentBoundaryIntersection(team, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, deploymentFrame.origin, hasValidZ: false), in endPosition, out var intersection))
			{
				endPosition = intersection;
			}
		}
	}

	public MBReadOnlyList<(string id, MBList<Vec2> points)> GetDeploymentBoundaries(Team team)
	{
		return GetTeamPlan(team).GetDeploymentBoundaries();
	}

	public Vec3 GetMeanPosition(Team team, bool isReinforcement = false)
	{
		return GetTeamPlan(team).GetMeanPosition(isReinforcement);
	}

	public void UpdateReinforcementPlan(Team team)
	{
		GetTeamPlan(team).UpdateReinforcementPlans();
	}

	public MatrixFrame GetZoomFocusFrame(Team team)
	{
		return GetDeploymentFrame(team);
	}

	public float GetZoomOffset(Team team, float fovAngle)
	{
		return 0.2f * (float)GetTroopCount(team);
	}

	private void MakeDeploymentPlanAux(Team team, bool isReinforcement = false, float spawnOffset = 0f, float targetOffset = 0f)
	{
		DefaultTeamDeploymentPlan teamPlan = GetTeamPlan(team);
		if (teamPlan.IsPlanMade(isReinforcement))
		{
			teamPlan.ClearPlan(isReinforcement);
		}
		if (!_mission.HasSpawnPath && _formationSceneSpawnEntries == null)
		{
			ReadSpawnEntitiesFromScene(_mission.IsFieldBattle);
		}
		teamPlan.MakeDeploymentPlan(spawnOffset, targetOffset, _formationSceneSpawnEntries, isReinforcement);
	}

	private void ReadSpawnEntitiesFromScene(bool isFieldBattle)
	{
		for (int i = 0; i < 2; i++)
		{
			_playerSpawnFrames[i] = null;
		}
		_formationSceneSpawnEntries = new FormationSceneSpawnEntry[2, 11];
		Scene scene = _mission.Scene;
		if (isFieldBattle)
		{
			for (int j = 0; j < 2; j++)
			{
				string text = ((j == 1) ? "attacker_" : "defender_");
				for (int k = 0; k < 11; k++)
				{
					FormationClass formationClass = (FormationClass)k;
					WeakGameEntity weakGameEntity = scene.FindWeakEntityWithTag(text + formationClass.GetName().ToLower());
					if (weakGameEntity == null)
					{
						FormationClass formationClass2 = formationClass.FallbackClass();
						int num = (int)formationClass2;
						GameEntity spawnEntity = _formationSceneSpawnEntries[j, num].SpawnEntity;
						weakGameEntity = ((spawnEntity != null) ? spawnEntity.WeakEntity : scene.FindWeakEntityWithTag(text + formationClass2.GetName().ToLower()));
						formationClass = ((weakGameEntity != null) ? formationClass2 : FormationClass.NumberOfAllFormations);
					}
					GameEntity gameEntity = null;
					if (weakGameEntity.IsValid)
					{
						gameEntity = GameEntity.CreateFromWeakEntity(weakGameEntity);
					}
					_formationSceneSpawnEntries[j, k] = new FormationSceneSpawnEntry(formationClass, gameEntity, gameEntity);
				}
			}
		}
		else
		{
			GameEntity gameEntity2 = null;
			if (_mission.IsSallyOutBattle)
			{
				gameEntity2 = scene.FindEntityWithTag("sally_out_ambush_battle_set");
			}
			if (gameEntity2 != null)
			{
				ReadSallyOutEntitiesFromScene(gameEntity2);
			}
			else
			{
				ReadSiegeBattleEntitiesFromScene(scene, BattleSideEnum.Defender);
			}
			ReadSiegeBattleEntitiesFromScene(scene, BattleSideEnum.Attacker);
		}
	}

	private void ReadSallyOutEntitiesFromScene(GameEntity sallyOutSetEntity)
	{
		int num = 0;
		MatrixFrame globalFrame = sallyOutSetEntity.GetFirstChildEntityWithTag("sally_out_ambush_player").GetGlobalFrame();
		WorldPosition origin = new WorldPosition(_mission.Scene, UIntPtr.Zero, globalFrame.origin, hasValidZ: false);
		_playerSpawnFrames[num] = new WorldFrame(globalFrame.rotation, origin);
		GameEntity firstChildEntityWithTag = sallyOutSetEntity.GetFirstChildEntityWithTag("sally_out_ambush_infantry");
		GameEntity firstChildEntityWithTag2 = sallyOutSetEntity.GetFirstChildEntityWithTag("sally_out_ambush_archer");
		GameEntity firstChildEntityWithTag3 = sallyOutSetEntity.GetFirstChildEntityWithTag("sally_out_ambush_cavalry");
		for (int i = 0; i < 11; i++)
		{
			FormationClass formationClass = (FormationClass)i;
			FormationClass formationClass2 = formationClass.FallbackClass();
			GameEntity gameEntity = null;
			switch (formationClass2)
			{
			case FormationClass.Infantry:
				gameEntity = firstChildEntityWithTag;
				break;
			case FormationClass.Ranged:
				gameEntity = firstChildEntityWithTag2;
				break;
			case FormationClass.Cavalry:
			case FormationClass.HorseArcher:
				gameEntity = firstChildEntityWithTag3;
				break;
			}
			_formationSceneSpawnEntries[num, i] = new FormationSceneSpawnEntry(formationClass, gameEntity, gameEntity);
		}
	}

	private void ReadSiegeBattleEntitiesFromScene(Scene missionScene, BattleSideEnum battleSide)
	{
		int num = (int)battleSide;
		string text = battleSide.ToString().ToLower() + "_";
		for (int i = 0; i < 11; i++)
		{
			FormationClass formationClass = (FormationClass)i;
			string text2 = text + formationClass.GetName().ToLower();
			string tag = text2 + "_reinforcement";
			GameEntity gameEntity = missionScene.FindEntityWithTag(text2);
			GameEntity gameEntity2 = null;
			if (gameEntity == null)
			{
				FormationClass formationClass2 = formationClass.FallbackClass();
				int num2 = (int)formationClass2;
				FormationSceneSpawnEntry formationSceneSpawnEntry = _formationSceneSpawnEntries[num, num2];
				if (formationSceneSpawnEntry.SpawnEntity != null)
				{
					gameEntity = formationSceneSpawnEntry.SpawnEntity;
					gameEntity2 = formationSceneSpawnEntry.ReinforcementSpawnEntity;
				}
				else
				{
					text2 = text + formationClass2.GetName().ToLower();
					tag = text2 + "_reinforcement";
					gameEntity = missionScene.FindEntityWithTag(text2);
					gameEntity2 = missionScene.FindEntityWithTag(tag);
				}
				formationClass = ((gameEntity != null) ? formationClass2 : FormationClass.NumberOfAllFormations);
			}
			else
			{
				gameEntity2 = missionScene.FindEntityWithTag(tag);
			}
			if (gameEntity2 == null)
			{
				gameEntity2 = gameEntity;
			}
			_formationSceneSpawnEntries[num, i] = new FormationSceneSpawnEntry(formationClass, gameEntity, gameEntity2);
		}
	}

	private DefaultTeamDeploymentPlan GetTeamPlan(Team team)
	{
		return GetTeamPlanAux(team);
	}

	private DefaultTeamDeploymentPlan GetTeamPlanAux(Team team)
	{
		return _teamDeploymentPlans.FirstOrDefault(((Team team, DefaultTeamDeploymentPlan plan) t) => t.team == team).plan;
	}

	bool IMissionDeploymentPlan.IsPositionInsideDeploymentBoundaries(Team team, in Vec2 position)
	{
		return IsPositionInsideDeploymentBoundaries(team, in position);
	}

	Vec2 IMissionDeploymentPlan.GetClosestDeploymentBoundaryPosition(Team team, in Vec2 position)
	{
		return GetClosestDeploymentBoundaryPosition(team, in position);
	}

	bool IMissionDeploymentPlan.GetPathDeploymentBoundaryIntersection(Team team, in WorldPosition startPosition, in WorldPosition endPosition, out WorldPosition intersection)
	{
		return GetPathDeploymentBoundaryIntersection(team, in startPosition, in endPosition, out intersection);
	}
}
