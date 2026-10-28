using TaleWorlds.Engine.Options;

namespace TaleWorlds.MountAndBlade.Options.ManagedOptions;

public abstract class ManagedOptionData : IOptionData
{
	public readonly TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType Type;

	private float _value;

	protected ManagedOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType type)
	{
		Type = type;
		_value = TaleWorlds.MountAndBlade.ManagedOptions.GetConfig(type);
	}

	public virtual float GetDefaultValue()
	{
		return TaleWorlds.MountAndBlade.ManagedOptions.GetDefaultConfig(Type);
	}

	public void Commit()
	{
		if (_value != TaleWorlds.MountAndBlade.ManagedOptions.GetConfig(Type))
		{
			TaleWorlds.MountAndBlade.ManagedOptions.SetConfig(Type, _value);
		}
	}

	public float GetValue(bool forceRefresh)
	{
		if (forceRefresh)
		{
			_value = TaleWorlds.MountAndBlade.ManagedOptions.GetConfig(Type);
		}
		return _value;
	}

	public void SetValue(float value)
	{
		_value = value;
	}

	public object GetOptionType()
	{
		return Type;
	}

	public bool IsNative()
	{
		return false;
	}

	public bool IsAction()
	{
		return false;
	}

	public (string, bool) GetIsDisabledAndReasonID()
	{
		TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType type = Type;
		if ((uint)(type - 2) <= 1u && BannerlordConfig.GyroOverrideForAttackDefend)
		{
			return ("str_gyro_overrides_attack_block_direction", true);
		}
		return (string.Empty, false);
	}
}
