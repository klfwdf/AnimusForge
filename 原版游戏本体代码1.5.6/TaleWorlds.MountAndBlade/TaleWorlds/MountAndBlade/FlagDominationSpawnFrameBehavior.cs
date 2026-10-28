using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class FlagDominationSpawnFrameBehavior : SpawnFrameBehaviorBase
{
	private List<GameEntity>[] _spawnPointsByTeam;

	private List<GameEntity>[] _spawnZonesByTeam;

	public override void Initialize()
	{
		base.Initialize();
		_spawnPointsByTeam = new List<GameEntity>[2];
		_spawnZonesByTeam = new List<GameEntity>[2];
		_spawnPointsByTeam[1] = SpawnPoints.Where((GameEntity x) => x.HasTag("attacker")).ToList();
		_spawnPointsByTeam[0] = SpawnPoints.Where((GameEntity x) => x.HasTag("defender")).ToList();
		_spawnZonesByTeam[1] = (from sz in _spawnPointsByTeam[1].Select((GameEntity sp) => sp.Parent).Distinct()
			where sz != null
			select sz).ToList();
		_spawnZonesByTeam[0] = (from sz in _spawnPointsByTeam[0].Select((GameEntity sp) => sp.Parent).Distinct()
			where sz != null
			select sz).ToList();
	}

	public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn)
	{
		GameEntity bestZone = GetBestZone(team, isInitialSpawn);
		List<GameEntity> spawnPointList = ((!(bestZone != null)) ? _spawnPointsByTeam[(int)team.Side].ToList() : _spawnPointsByTeam[(int)team.Side].Where((GameEntity sp) => sp.Parent == bestZone).ToList());
		return GetBestSpawnPoint(spawnPointList, hasMount);
	}

	private GameEntity GetBestZone(Team team, bool isInitialSpawn)
	{
		if (_spawnZonesByTeam[(int)team.Side].Count == 0)
		{
			return null;
		}
		if (isInitialSpawn)
		{
			return _spawnZonesByTeam[(int)team.Side].Single((GameEntity sz) => sz.HasTag("starting"));
		}
		List<GameEntity> list = _spawnZonesByTeam[(int)team.Side].Where((GameEntity sz) => !sz.HasTag("starting")).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		float[] array = new float[list.Count];
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component?.Team == null || component.Team.Side == BattleSideEnum.None || component.ControlledAgent == null || !component.ControlledAgent.IsActive())
			{
				continue;
			}
			for (int num = 0; num < list.Count; num++)
			{
				Vec3 globalPosition = list[num].GlobalPosition;
				if (component.Team != team)
				{
					array[num] -= 1f / (0.0001f + component.ControlledAgent.Position.Distance(globalPosition)) * 1f;
				}
				else
				{
					array[num] += 1f / (0.0001f + component.ControlledAgent.Position.Distance(globalPosition)) * 1.5f;
				}
			}
		}
		int num2 = -1;
		for (int num3 = 0; num3 < array.Length; num3++)
		{
			if (num2 < 0 || array[num3] > array[num2])
			{
				num2 = num3;
			}
		}
		return list[num2];
	}

	private MatrixFrame GetBestSpawnPoint(List<GameEntity> spawnPointList, bool hasMount)
	{
		float num = float.MinValue;
		int index = -1;
		for (int i = 0; i < spawnPointList.Count; i++)
		{
			float num2 = MBRandom.RandomFloat * 0.2f;
			AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, spawnPointList[i].GlobalPosition.AsVec2, 2f);
			while (searchStruct.LastFoundAgent != null)
			{
				float num3 = searchStruct.LastFoundAgent.Position.DistanceSquared(spawnPointList[i].GlobalPosition);
				if (num3 < 4f)
				{
					float num4 = MathF.Sqrt(num3);
					num2 -= (2f - num4) * 5f;
				}
				AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
			}
			if (hasMount && spawnPointList[i].HasTag("exclude_mounted"))
			{
				num2 -= 100f;
			}
			if (num2 > num)
			{
				num = num2;
				index = i;
			}
		}
		MatrixFrame globalFrame = spawnPointList[index].GetGlobalFrame();
		globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return globalFrame;
	}
}
