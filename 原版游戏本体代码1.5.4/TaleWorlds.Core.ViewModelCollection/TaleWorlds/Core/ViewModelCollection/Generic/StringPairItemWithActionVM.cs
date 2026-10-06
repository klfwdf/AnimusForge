using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic;

public class StringPairItemWithActionVM : ViewModel
{
	public object Identifier;

	protected Action<object> _onExecute;

	private string _definition;

	private string _value;

	private HintViewModel _hint;

	private bool _isEnabled;

	[DataSourceProperty]
	public string Definition
	{
		get
		{
			return _definition;
		}
		set
		{
			if (value != _definition)
			{
				_definition = value;
				OnPropertyChangedWithValue(value, "Definition");
			}
		}
	}

	[DataSourceProperty]
	public string Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (value != _value)
			{
				_value = value;
				OnPropertyChangedWithValue(value, "Value");
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

	public StringPairItemWithActionVM(Action<object> onExecute, string definition, string value, object identifier)
	{
		_onExecute = onExecute;
		Identifier = identifier;
		Definition = definition;
		Value = value;
		Hint = new HintViewModel();
		IsEnabled = true;
	}

	public void ExecuteAction()
	{
		_onExecute(Identifier);
	}
}
