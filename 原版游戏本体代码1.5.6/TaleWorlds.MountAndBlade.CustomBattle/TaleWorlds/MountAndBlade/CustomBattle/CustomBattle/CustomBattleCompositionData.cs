namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;

public struct CustomBattleCompositionData(float rangedPercentage, float cavalryPercentage, float rangedCavalryPercentage)
{
	public readonly bool IsValid = true;

	public readonly float RangedPercentage = rangedPercentage;

	public readonly float CavalryPercentage = cavalryPercentage;

	public readonly float RangedCavalryPercentage = rangedCavalryPercentage;
}
