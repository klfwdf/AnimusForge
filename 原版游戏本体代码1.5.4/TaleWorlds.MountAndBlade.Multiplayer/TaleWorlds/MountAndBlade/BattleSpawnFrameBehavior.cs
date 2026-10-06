using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BattleSpawnFrameBehavior : SpawnFrameBehaviorBase
{
	private List<GameEntity> _spawnPointsOfAttackers;

	private List<GameEntity> _spawnPointsOfDefenders;

	public override void Initialize()
	{
		base.Initialize();
		_spawnPointsOfAttackers = SpawnPoints.Where((GameEntity x) => x.HasTag("attacker")).ToList();
		_spawnPointsOfDefenders = SpawnPoints.Where((GameEntity x) => x.HasTag("defender")).ToList();
	}

	public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn)
	{
		List<GameEntity> list = ((team == Mission.Current.AttackerTeam) ? _spawnPointsOfAttackers : _spawnPointsOfDefenders).ToList();
		float num = float.MinValue;
		int index = -1;
		for (int i = 0; i < list.Count; i++)
		{
			float num2 = MBRandom.RandomFloat * 0.2f;
			AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, list[i].GlobalPosition.AsVec2, 2f);
			while (searchStruct.LastFoundAgent != null)
			{
				float num3 = searchStruct.LastFoundAgent.Position.DistanceSquared(list[i].GlobalPosition);
				if (num3 < 4f)
				{
					float num4 = MathF.Sqrt(num3);
					num2 -= (2f - num4) * 5f;
				}
				AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
			}
			if (hasMount && list[i].HasTag("exclude_mounted"))
			{
				num2 -= 100f;
			}
			if (num2 > num)
			{
				num = num2;
				index = i;
			}
		}
		return list[index].GetGlobalFrame();
	}
}
