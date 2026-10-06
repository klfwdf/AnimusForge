using TaleWorlds.Engine.Options;

namespace TaleWorlds.MountAndBlade.Options.ManagedOptions;

public class ManagedNumericOptionData : ManagedOptionData, INumericOptionData, IOptionData
{
	private readonly float _minValue;

	private readonly float _maxValue;

	public ManagedNumericOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType type)
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

	private static float GetLimitValue(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType type, bool isMin)
	{
		switch (type)
		{
		case TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.BattleSize:
			return isMin ? BannerlordConfig.MinBattleSize : BannerlordConfig.MaxBattleSize;
		case TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.FirstPersonFov:
			if (!isMin)
			{
				return 100f;
			}
			return 45f;
		case TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.CombatCameraDistance:
			if (!isMin)
			{
				return 2.4f;
			}
			return 0.7f;
		case TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.UIScale:
			if (!isMin)
			{
				return 1f;
			}
			return 0.75f;
		case TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.AutoSaveInterval:
			if (!isMin)
			{
				return 60f;
			}
			return 4f;
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
		case TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.BattleSize:
		case TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.AutoSaveInterval:
		case TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.FirstPersonFov:
			return true;
		default:
			return false;
		}
	}

	public int GetDiscreteIncrementInterval()
	{
		return 1;
	}

	public bool GetShouldUpdateContinuously()
	{
		TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType type = Type;
		if (type == TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.UIScale)
		{
			return false;
		}
		return true;
	}
}
