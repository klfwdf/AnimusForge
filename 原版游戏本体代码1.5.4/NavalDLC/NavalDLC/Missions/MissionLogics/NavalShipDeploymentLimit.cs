using TaleWorlds.Library;

namespace NavalDLC.Missions.MissionLogics;

public struct NavalShipDeploymentLimit
{
	public readonly int PartiesLimit;

	public readonly int SkeletalCrewLimit;

	public readonly int BattleAllocationLimit;

	public bool IsValid => NetDeploymentLimit > 0;

	public int NetDeploymentLimit => MathF.Min(MathF.Min(PartiesLimit, SkeletalCrewLimit), BattleAllocationLimit);

	public int NetDeploymentLimitWithoutSkeletal => MathF.Min(PartiesLimit, BattleAllocationLimit);

	public NavalShipDeploymentLimit(int partiesLimit, int skeletalCrewLimit, int battleAllocationLimit = 8)
	{
		PartiesLimit = partiesLimit;
		SkeletalCrewLimit = skeletalCrewLimit;
		BattleAllocationLimit = battleAllocationLimit;
	}

	public NavalShipDeploymentLimit(int commonLimit)
	{
		PartiesLimit = commonLimit;
		SkeletalCrewLimit = commonLimit;
		BattleAllocationLimit = commonLimit;
	}

	public static NavalShipDeploymentLimit Invalid()
	{
		return new NavalShipDeploymentLimit(0, 0, 0);
	}

	public static NavalShipDeploymentLimit Max()
	{
		return new NavalShipDeploymentLimit(8, 8);
	}
}
