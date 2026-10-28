using System;
using NavalDLC.ViewModelCollection.Port.PortScreenHandlers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace NavalDLC.ViewModelCollection.Port;

public class PortActionVM : ViewModel
{
	private readonly Action _action;

	private bool _isVisible;

	private bool _isEnabled;

	private string _name;

	private string _additionalInfo;

	private HintViewModel _tooltip;

	[DataSourceProperty]
	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (value != _isVisible)
			{
				_isVisible = value;
				OnPropertyChangedWithValue(value, "IsVisible");
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
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string AdditionalInfo
	{
		get
		{
			return _additionalInfo;
		}
		set
		{
			if (value != _additionalInfo)
			{
				_additionalInfo = value;
				OnPropertyChangedWithValue(value, "AdditionalInfo");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel Tooltip
	{
		get
		{
			return _tooltip;
		}
		set
		{
			if (value != _tooltip)
			{
				_tooltip = value;
				OnPropertyChangedWithValue(value, "Tooltip");
			}
		}
	}

	public PortActionVM(Action action)
	{
		_action = action;
		Tooltip = new HintViewModel();
	}

	public void RefreshWith(PortActionInfo actionInfo)
	{
		IsVisible = actionInfo.IsRelevant;
		IsEnabled = actionInfo.IsEnabled;
		Name = actionInfo.ActionName?.ToString();
		Tooltip.HintText = actionInfo.Tooltip;
	}

	public void ExecuteAction()
	{
		_action?.Invoke();
	}
}
