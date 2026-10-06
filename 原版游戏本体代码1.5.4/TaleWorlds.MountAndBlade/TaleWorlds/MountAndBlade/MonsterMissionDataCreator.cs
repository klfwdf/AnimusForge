using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class MonsterMissionDataCreator : IMonsterMissionDataCreator
{
	IMonsterMissionData IMonsterMissionDataCreator.CreateMonsterMissionData(Monster monster)
	{
		return new MonsterMissionData(monster);
	}
}
