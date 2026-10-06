namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;

public struct CustomBattleCompositionData
{
	public readonly bool IsValid;

	public readonly float RangedPercentage;

	public readonly float CavalryPercentage;

	public readonly float RangedCavalryPercentage;

	public CustomBattleCompositionData(float rangedPercentage, float cavalryPercentage, float rangedCavalryPercentage)
	{
		RangedPercentage = rangedPercentage;
		CavalryPercentage = cavalryPercentage;
		RangedCavalryPercentage = rangedCavalryPercentage;
		IsValid = true;
	}
}
