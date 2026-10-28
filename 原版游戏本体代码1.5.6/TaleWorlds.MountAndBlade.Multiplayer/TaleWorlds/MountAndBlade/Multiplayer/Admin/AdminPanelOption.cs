using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

internal class AdminPanelOption<T> : IAdminPanelOptionInternal<T>, IAdminPanelOptionInternal, IAdminPanelOption<T>, IAdminPanelOption
{
	private MultiplayerOptions.OptionType _optionType;

	private MultiplayerOptions.MultiplayerOptionsAccessMode _accessMode;

	private readonly string _uniqueId;

	private bool _isRequired;

	private bool _requiresRestart;

	private Action _onRefresh;

	private List<Action> _onValueChangedAdditionalCallbacks;

	private Action<T> _onApplied;

	private TextObject _nameTextObj;

	private TextObject _descriptionTextObj;

	protected T DefaultValue { get; private set; }

	protected T InitialValue { get; private set; }

	protected T CurrentValue { get; private set; }

	public string UniqueId => _uniqueId;

	public bool IsRequired => _isRequired;

	public bool RequiresMissionRestart => _requiresRestart;

	public bool IsDirty => !AreEqualValues(InitialValue, CurrentValue);

	public bool CanRevertToDefaultValue
	{
		get
		{
			if (!AreEqualValues(DefaultValue, CurrentValue))
			{
				return OnGetCanRevertToDefaultValue();
			}
			return false;
		}
	}

	public string Name => _nameTextObj?.ToString() ?? string.Empty;

	public string Description => _descriptionTextObj?.ToString() ?? string.Empty;

	public AdminPanelOption(string uniqueId)
	{
		_uniqueId = uniqueId;
		_onValueChangedAdditionalCallbacks = new List<Action>();
		_optionType = MultiplayerOptions.OptionType.NumOfSlots;
		_accessMode = MultiplayerOptions.MultiplayerOptionsAccessMode.NumAccessModes;
	}

	protected virtual void OnValueChanged(T previousValue, T newValue)
	{
		OnRefresh();
	}

	protected virtual bool OnGetCanRevertToDefaultValue()
	{
		return true;
	}

	protected virtual T GetOptionValue(MultiplayerOptions.OptionType optionType, MultiplayerOptions.MultiplayerOptionsAccessMode accessMode = MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions)
	{
		switch (optionType.GetOptionProperty().OptionValueType)
		{
		case MultiplayerOptions.OptionValueType.Bool:
			return (T)(object)optionType.GetBoolValue(accessMode);
		case MultiplayerOptions.OptionValueType.Integer:
			return (T)(object)optionType.GetIntValue(accessMode);
		case MultiplayerOptions.OptionValueType.String:
			return (T)(object)optionType.GetStrValue(accessMode);
		case MultiplayerOptions.OptionValueType.Enum:
			Debug.FailedAssert("Unsupported option value type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Admin\\Internal\\AdminPanelOption.cs", "GetOptionValue", 63);
			break;
		}
		return default(T);
	}

	protected virtual bool AreEqualValues(T first, T second)
	{
		if (first == null && second == null)
		{
			return true;
		}
		if (first != null && second == null)
		{
			return false;
		}
		if (first == null && second != null)
		{
			return false;
		}
		return first.Equals(second);
	}

	public void AddValueChangedCallback(Action callback)
	{
		_onValueChangedAdditionalCallbacks.Add(callback);
	}

	public void RemoveValueChangedCallback(Action callback)
	{
		_onValueChangedAdditionalCallbacks.Remove(callback);
	}

	public virtual void OnFinalize()
	{
		_onValueChangedAdditionalCallbacks.Clear();
		_onRefresh = null;
		_onApplied = null;
	}

	protected virtual void OnRefresh()
	{
		_onRefresh?.Invoke();
	}

	public AdminPanelOption<T> BuildOptionType(MultiplayerOptions.OptionType optionType, MultiplayerOptions.MultiplayerOptionsAccessMode accessMode = MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions, bool buildDefaultValue = true, bool buildInitialValue = true)
	{
		_optionType = optionType;
		_accessMode = accessMode;
		if (buildDefaultValue)
		{
			T optionValue = GetOptionValue(optionType, MultiplayerOptions.MultiplayerOptionsAccessMode.DefaultMapOptions);
			BuildDefaultValue(optionValue);
		}
		if (buildInitialValue)
		{
			T optionValue2 = GetOptionValue(optionType);
			BuildInitialValue(optionValue2);
		}
		return this;
	}

	public AdminPanelOption<T> BuildIsRequired(bool isRequired)
	{
		_isRequired = isRequired;
		return this;
	}

	public AdminPanelOption<T> BuildRequiresRestart(bool requiresRestart)
	{
		_requiresRestart = requiresRestart;
		return this;
	}

	public AdminPanelOption<T> BuildName(TextObject name)
	{
		_nameTextObj = name;
		return this;
	}

	public AdminPanelOption<T> BuildDescription(TextObject description)
	{
		_descriptionTextObj = description;
		return this;
	}

	public AdminPanelOption<T> BuildInitialValue(T value)
	{
		InitialValue = value;
		SetValue(InitialValue);
		return this;
	}

	public AdminPanelOption<T> BuildDefaultValue(T value)
	{
		DefaultValue = value;
		SetValue(DefaultValue);
		return this;
	}

	public AdminPanelOption<T> BuildOnAppliedCallback(Action<T> onApplied)
	{
		_onApplied = onApplied;
		return this;
	}

	public T GetValue()
	{
		return CurrentValue;
	}

	public void SetValue(T value)
	{
		T currentValue = CurrentValue;
		CurrentValue = value;
		if (!AreEqualValues(currentValue, CurrentValue))
		{
			OnValueChanged(currentValue, CurrentValue);
		}
		for (int i = 0; i < _onValueChangedAdditionalCallbacks.Count; i++)
		{
			_onValueChangedAdditionalCallbacks[i]?.Invoke();
		}
	}

	public virtual bool GetIsAvailable()
	{
		return true;
	}

	public void OnApplyChanges()
	{
		InitialValue = CurrentValue;
		_onApplied?.Invoke(CurrentValue);
	}

	public void RevertChanges()
	{
		SetValue(InitialValue);
	}

	public void RestoreDefaults()
	{
		SetValue(DefaultValue);
	}

	public void SetOnRefreshCallback(Action callback)
	{
		_onRefresh = callback;
	}

	public virtual bool GetIsDisabled(out string reason)
	{
		reason = string.Empty;
		return false;
	}

	public MultiplayerOptions.OptionType GetOptionType()
	{
		return _optionType;
	}

	public MultiplayerOptions.MultiplayerOptionsAccessMode GetOptionAccessMode()
	{
		return _accessMode;
	}
}
