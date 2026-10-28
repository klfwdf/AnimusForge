namespace TaleWorlds.MountAndBlade;

public interface IFormationUnit
{
	IFormationArrangement Formation { get; }

	int FormationFileIndex { get; set; }

	int FormationRankIndex { get; set; }

	IFormationUnit FollowedUnit { get; }

	bool IsShieldUsageEncouraged { get; }

	bool IsPlayerUnit { get; }
}
