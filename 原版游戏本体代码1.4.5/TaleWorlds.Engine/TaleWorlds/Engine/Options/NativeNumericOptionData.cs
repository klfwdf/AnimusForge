namespace TaleWorlds.Engine.Options;

public class NativeNumericOptionData : NativeOptionData, INumericOptionData, IOptionData
{
	private readonly float _minValue;

	private readonly float _maxValue;

	public NativeNumericOptionData(NativeOptions.NativeOptionsType type)
		: base(type)
	{
		_minValue = GetLimitValue(Type, isMin: true);
		_maxValue = GetLimitValue(Type, isMin: false);
	}

	public float GetMinValue()
	{
		return _minValue;
	}

	public float GetMaxValue()
	{
		return _maxValue;
	}

	private static float GetLimitValue(NativeOptions.NativeOptionsType type, bool isMin)
	{
		switch (type)
		{
		case NativeOptions.NativeOptionsType.Brightness:
			if (!isMin)
			{
				return 100f;
			}
			return 0f;
		case NativeOptions.NativeOptionsType.BrightnessMin:
			if (!isMin)
			{
				return 0.3f;
			}
			return 0f;
		case NativeOptions.NativeOptionsType.BrightnessMax:
			if (!isMin)
			{
				return 1f;
			}
			return 0.7f;
		case NativeOptions.NativeOptionsType.ExposureCompensation:
			if (!isMin)
			{
				return 2f;
			}
			return -2f;
		case NativeOptions.NativeOptionsType.ResolutionScale:
			if (!isMin)
			{
				return 100f;
			}
			return 50f;
		case NativeOptions.NativeOptionsType.FrameLimiter:
			if (!isMin)
			{
				return 360f;
			}
			return 30f;
		case NativeOptions.NativeOptionsType.SharpenAmount:
			if (!isMin)
			{
				return 100f;
			}
			return 0f;
		case NativeOptions.NativeOptionsType.DynamicResolutionTarget:
			if (!isMin)
			{
				return 240f;
			}
			return 30f;
		case NativeOptions.NativeOptionsType.TrailAmount:
			if (!isMin)
			{
				return 1f;
			}
			return 0f;
		case NativeOptions.NativeOptionsType.MouseYMovementScale:
			if (!isMin)
			{
				return 4f;
			}
			return 0.25f;
		case NativeOptions.NativeOptionsType.MouseSensitivity:
			if (!isMin)
			{
				return 1f;
			}
			return 0.3f;
		case NativeOptions.NativeOptionsType.GyroAimSensitivity:
			if (!isMin)
			{
				return 1f;
			}
			return 0f;
		default:
			if (!isMin)
			{
				return 1f;
			}
			return 0f;
		}
	}

	public bool GetIsDiscrete()
	{
		switch (Type)
		{
		case NativeOptions.NativeOptionsType.ResolutionScale:
		case NativeOptions.NativeOptionsType.FrameLimiter:
		case NativeOptions.NativeOptionsType.Brightness:
		case NativeOptions.NativeOptionsType.SharpenAmount:
		case NativeOptions.NativeOptionsType.BrightnessMin:
		case NativeOptions.NativeOptionsType.BrightnessMax:
		case NativeOptions.NativeOptionsType.ExposureCompensation:
		case NativeOptions.NativeOptionsType.DynamicResolutionTarget:
			return true;
		default:
			return false;
		}
	}

	public int GetDiscreteIncrementInterval()
	{
		NativeOptions.NativeOptionsType type = Type;
		if (type == NativeOptions.NativeOptionsType.SharpenAmount)
		{
			return 5;
		}
		return 1;
	}

	public bool GetShouldUpdateContinuously()
	{
		return true;
	}
}
