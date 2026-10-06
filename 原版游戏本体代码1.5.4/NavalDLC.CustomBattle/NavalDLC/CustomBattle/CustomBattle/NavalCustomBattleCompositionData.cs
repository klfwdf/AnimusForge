namespace NavalDLC.CustomBattle.CustomBattle;

public struct NavalCustomBattleCompositionData
{
	public readonly bool IsValid;

	public readonly float RangedPercentage;

	public readonly float CavalryPercentage;

	public readonly float RangedCavalryPercentage;

	public NavalCustomBattleCompositionData(float rangedPercentage, float cavalryPercentage, float rangedCavalryPercentage)
	{
		RangedPercentage = rangedPercentage;
		CavalryPercentage = cavalryPercentage;
		RangedCavalryPercentage = rangedCavalryPercentage;
		IsValid = true;
	}
}
