using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.Source.Missions;

public class BattleSpawnLogic : MissionLogic
{
	public const string BattleTag = "battle_set";

	public const string SallyOutTag = "sally_out_set";

	public const string ReliefForceAttackTag = "relief_force_attack_set";

	private const string SpawnPointSetCommonTag = "spawnpoint_set";

	private readonly string _selectedSpawnPointSetTag;

	private bool _isScenePrepared;

	public BattleSpawnLogic(string selectedSpawnPointSetTag)
	{
		_selectedSpawnPointSetTag = selectedSpawnPointSetTag;
	}

	public override void OnPreMissionTick(float dt)
	{
		if (_isScenePrepared)
		{
			return;
		}
		WeakGameEntity weakGameEntity = base.Mission.Scene.FindWeakEntityWithTag(_selectedSpawnPointSetTag);
		if (weakGameEntity != null)
		{
			List<WeakGameEntity> list = base.Mission.Scene.FindWeakEntitiesWithTag("spawnpoint_set").ToList();
			list.Remove(weakGameEntity);
			foreach (WeakGameEntity item in list)
			{
				item.Remove(76);
			}
		}
		_isScenePrepared = true;
	}
}
