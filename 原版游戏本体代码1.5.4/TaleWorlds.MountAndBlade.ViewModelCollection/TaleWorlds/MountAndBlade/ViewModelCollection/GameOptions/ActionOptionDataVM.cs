using System;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

public class ActionOptionDataVM : GenericOptionDataVM
{
	private readonly Action _onAction;

	private readonly TextObject _optionActionName;

	private string _actionName;

	[DataSourceProperty]
	public string ActionName
	{
		get
		{
			return _actionName;
		}
		set
		{
			if (value != _actionName)
			{
				_actionName = value;
				OnPropertyChangedWithValue(value, "ActionName");
			}
		}
	}

	public ActionOptionDataVM(Action onAction, OptionsVM optionsVM, IOptionData option, TextObject name, TextObject optionActionName, TextObject description)
		: base(optionsVM, option, name, description, OptionsVM.OptionsDataType.ActionOption)
	{
		_onAction = onAction;
		_optionActionName = optionActionName;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_optionActionName != null)
		{
			ActionName = _optionActionName.ToString();
		}
	}

	private void ExecuteAction()
	{
		_onAction?.DynamicInvokeWithLog();
	}

	public override void Cancel()
	{
	}

	public override bool IsChanged()
	{
		return false;
	}

	public override void ResetData()
	{
	}

	public override void SetValue(float value)
	{
	}

	public override void UpdateValue()
	{
	}

	public override void ApplyValue()
	{
	}
}
