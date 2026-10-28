namespace TaleWorlds.Engine.Options;

public interface INumericOptionData : IOptionData
{
	float GetMinValue();

	float GetMaxValue();

	bool GetIsDiscrete();

	int GetDiscreteIncrementInterval();

	bool GetShouldUpdateContinuously();
}
