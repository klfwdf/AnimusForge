namespace TaleWorlds.MountAndBlade;

public static class DefaultTacticalDecisionCodes
{
	public const byte FormationMoveToPoint = 0;

	public const byte FormationDefendPoint = 1;

	public const byte FormationMoveToObject = 10;

	public const byte FormationAttackObject = 11;

	public const byte FormationDefendObject = 12;

	public const byte FormationDefendFormation = 20;

	public const byte FormationAttackFormation = 21;

	public const byte TeamCharge = 30;

	public const byte TeamFallbackToKeep = 31;

	public const byte TeamRetreat = 32;
}
