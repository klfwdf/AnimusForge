using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Generic;

public class StringItemWithEnabledAndHintVM : ViewModel
{
	public object Identifier;

	protected Action<object> _onExecute;

	private HintViewModel _hint;

	private string _actionText;

	private bool _isEnabled;

	[DataSourceProperty]
	public string ActionText
	{
		get
		{
			return _actionText;
		}
		set
		{
			if (value != _actionText)
			{
				_actionText = value;
				OnPropertyChangedWithValue(value, "ActionText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	public StringItemWithEnabledAndHintVM(Action<object> onExecute, string item, bool enabled, object identifier, TextObject hintText = null)
	{
		_onExecute = onExecute;
		Identifier = identifier;
		ActionText = item;
		IsEnabled = enabled;
		Hint = new HintViewModel(hintText ?? TextObject.GetEmpty());
	}

	public void ExecuteAction()
	{
		if (IsEnabled)
		{
			_onExecute(Identifier);
		}
	}
}
