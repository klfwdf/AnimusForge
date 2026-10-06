using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public struct FactoredNumber
{
	private float _limitMinValue;

	private float _limitMaxValue;

	private float _sumOfFactors;

	public float ResultNumber => MathF.Clamp(BaseNumber + BaseNumber * _sumOfFactors, LimitMinValue, LimitMaxValue);

	public float BaseNumber { get; private set; }

	public float LimitMinValue => _limitMinValue;

	public float LimitMaxValue => _limitMaxValue;

	public FactoredNumber(float baseNumber = 0f)
	{
		BaseNumber = baseNumber;
		_sumOfFactors = 0f;
		_limitMinValue = float.MinValue;
		_limitMaxValue = float.MaxValue;
	}

	public void Add(float value)
	{
		if (!value.ApproximatelyEqualsTo(0f))
		{
			BaseNumber += value;
		}
	}

	public void AddFactor(float value)
	{
		if (!value.ApproximatelyEqualsTo(0f))
		{
			_sumOfFactors += value;
		}
	}

	public void LimitMin(float minValue)
	{
		_limitMinValue = minValue;
	}

	public void LimitMax(float maxValue)
	{
		_limitMaxValue = maxValue;
	}

	public void Clamp(float minValue, float maxValue)
	{
		LimitMin(minValue);
		LimitMax(maxValue);
	}
}
