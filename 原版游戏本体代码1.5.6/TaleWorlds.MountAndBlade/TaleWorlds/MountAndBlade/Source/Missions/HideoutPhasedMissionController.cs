using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Source.Missions;

public class HideoutPhasedMissionController : MissionLogic
{
	public const int PhaseCount = 4;

	private GameEntity[] _spawnPoints;

	private Stack<MatrixFrame[]> _spawnPointFrames;

	private bool _isNewlyPopulatedFormationGivenOrder = true;

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Logic;

	private bool IsPhasingInitialized => true;

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_isNewlyPopulatedFormationGivenOrder)
		{
			return;
		}
		foreach (Team team in base.Mission.Teams)
		{
			if (team.Side != BattleSideEnum.Defender)
			{
				continue;
			}
			foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					item.SetMovementOrder(MovementOrder.MovementOrderMove(item.CachedMedianPosition));
					_isNewlyPopulatedFormationGivenOrder = true;
				}
			}
		}
	}

	protected override void OnEndMission()
	{
		base.Mission.AreOrderGesturesEnabled_AdditionalCondition -= AreOrderGesturesEnabled_AdditionalCondition;
	}

	public override void OnBehaviorInitialize()
	{
		ReadySpawnPointLogic();
		base.Mission.AreOrderGesturesEnabled_AdditionalCondition += AreOrderGesturesEnabled_AdditionalCondition;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		DefaultBattleMissionAgentSpawnLogic missionBehavior = base.Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
		if (missionBehavior != null && IsPhasingInitialized)
		{
			missionBehavior.AddPhaseChangeAction(BattleSideEnum.Defender, OnPhaseChanged);
		}
	}

	private void ReadySpawnPointLogic()
	{
		List<WeakGameEntity> list = Mission.Current.GetActiveEntitiesWithScriptComponentOfType<HideoutSpawnPointGroup>().ToList();
		if (list.Count == 0)
		{
			return;
		}
		HideoutSpawnPointGroup[] array = new HideoutSpawnPointGroup[list.Count];
		foreach (WeakGameEntity item in list)
		{
			HideoutSpawnPointGroup firstScriptOfType = item.GetFirstScriptOfType<HideoutSpawnPointGroup>();
			array[firstScriptOfType.PhaseNumber - 1] = firstScriptOfType;
		}
		List<HideoutSpawnPointGroup> list2 = array.ToList();
		list2.RemoveAt(0);
		for (int i = 0; i < 3; i++)
		{
			list2.RemoveAt(MBRandom.RandomInt(list2.Count));
		}
		_spawnPointFrames = new Stack<MatrixFrame[]>();
		for (int j = 0; j < array.Length; j++)
		{
			if (!list2.Contains(array[j]))
			{
				_spawnPointFrames.Push(array[j].GetSpawnPointFrames());
				Debug.Print("Spawn " + array[j].PhaseNumber + " is active.", 0, Debug.DebugColor.Green, 64uL);
			}
			array[j].RemoveWithAllChildren();
		}
		CreateSpawnPoints();
	}

	private void CreateSpawnPoints()
	{
		MatrixFrame[] array = _spawnPointFrames.Pop();
		_spawnPoints = new GameEntity[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].IsIdentity)
			{
				_spawnPoints[i] = GameEntity.CreateEmpty(base.Mission.Scene);
				_spawnPoints[i].SetGlobalFrame(in array[i]);
				_spawnPoints[i].AddTag("defender_" + ((FormationClass)i).GetName().ToLower());
			}
		}
	}

	private void OnPhaseChanged()
	{
		if (_spawnPointFrames.Count == 0)
		{
			Debug.FailedAssert("No position left.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\MissionLogics\\HideoutPhasedMissionController.cs", "OnPhaseChanged", 142);
			return;
		}
		for (int i = 0; i < _spawnPoints.Length; i++)
		{
			if (!(_spawnPoints[i] == null))
			{
				_spawnPoints[i].Remove(78);
			}
		}
		CreateSpawnPoints();
		_isNewlyPopulatedFormationGivenOrder = false;
	}

	private bool AreOrderGesturesEnabled_AdditionalCondition()
	{
		return false;
	}
}
