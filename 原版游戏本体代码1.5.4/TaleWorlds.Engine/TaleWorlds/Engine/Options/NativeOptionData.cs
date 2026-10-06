namespace TaleWorlds.Engine.Options;

public abstract class NativeOptionData : IOptionData
{
	public readonly NativeOptions.NativeOptionsType Type;

	private float _value;

	protected NativeOptionData(NativeOptions.NativeOptionsType type)
	{
		Type = type;
		_value = NativeOptions.GetConfig(type);
	}

	public virtual float GetDefaultValue()
	{
		return NativeOptions.GetDefaultConfig(Type);
	}

	public void Commit()
	{
		NativeOptions.SetConfig(Type, _value);
	}

	public float GetValue(bool forceRefresh)
	{
		if (forceRefresh)
		{
			_value = NativeOptions.GetConfig(Type);
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
		return true;
	}

	public bool IsAction()
	{
		return false;
	}

	public (string, bool) GetIsDisabledAndReasonID()
	{
		switch (Type)
		{
		case NativeOptions.NativeOptionsType.GyroAimSensitivity:
			if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableGyroAssistedAim) != 1f)
			{
				return ("str_gyro_disabled", true);
			}
			break;
		case NativeOptions.NativeOptionsType.ResolutionScale:
			if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DLSS) != 0f)
			{
				return ("str_dlss_enabled", true);
			}
			if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DynamicResolution) != 0f)
			{
				return ("str_dynamic_resolution_enabled", true);
			}
			break;
		case NativeOptions.NativeOptionsType.DLSS:
			if (!NativeOptions.GetIsDLSSAvailable())
			{
				return ("str_dlss_not_available", true);
			}
			break;
		case NativeOptions.NativeOptionsType.DynamicResolution:
			if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DLSS) != 0f)
			{
				return ("str_dlss_enabled", true);
			}
			break;
		case NativeOptions.NativeOptionsType.DynamicResolutionTarget:
			if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DynamicResolution) == 0f)
			{
				return ("str_dynamic_resolution_disabled", true);
			}
			if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DLSS) != 0f)
			{
				return ("str_dlss_enabled", true);
			}
			break;
		}
		return (string.Empty, false);
	}
}
