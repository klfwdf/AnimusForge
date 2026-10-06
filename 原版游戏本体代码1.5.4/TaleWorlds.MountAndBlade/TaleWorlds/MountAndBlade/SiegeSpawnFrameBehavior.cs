using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade;

public class SiegeSpawnFrameBehavior : SpawnFrameBehaviorBase
{
	public const string SpawnZoneTagAffix = "sp_zone_";

	public const string SpawnZoneEnableTagAffix = "enable_";

	public const string SpawnZoneDisableTagAffix = "disable_";

	public const int StartingActiveSpawnZoneIndex = 0;

	private List<GameEntity>[] _spawnPointsByTeam;

	private List<GameEntity>[] _spawnZonesByTeam;

	private int _activeSpawnZoneIndex;

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
		_activeSpawnZoneIndex = 0;
	}

	public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn)
	{
		List<GameEntity> list = new List<GameEntity>();
		GameEntity gameEntity = _spawnZonesByTeam[(int)team.Side].First((GameEntity sz) => sz.HasTag(string.Format("{0}{1}", "sp_zone_", _activeSpawnZoneIndex)));
		list.AddRange(from sp in gameEntity.GetChildren()
			where sp.HasTag("spawnpoint")
			select sp);
		return GetSpawnFrameFromSpawnPoints(list, team, hasMount);
	}

	public void OnFlagDeactivated(FlagCapturePoint flag)
	{
		_activeSpawnZoneIndex++;
	}
}
