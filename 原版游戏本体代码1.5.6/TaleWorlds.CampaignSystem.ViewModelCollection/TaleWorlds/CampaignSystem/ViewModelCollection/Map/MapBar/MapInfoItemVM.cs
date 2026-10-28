using System;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Library.Information;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;

public class MapInfoItemVM : ViewModel
{
	public readonly string ItemId;

	private readonly BasicTooltipViewModel _tooltip;

	private readonly TooltipTriggerVM _tooltipTrigger;

	private bool _hasWarning;

	private int _intValue;

	private float _floatValue;

	private string _visualId;

	private string _value;

	[DataSourceProperty]
	public bool HasWarning
	{
		get
		{
			return _hasWarning;
		}
		set
		{
			if (value != _hasWarning)
			{
				_hasWarning = value;
				OnPropertyChangedWithValue(value, "HasWarning");
			}
		}
	}

	[DataSourceProperty]
	public int IntValue
	{
		get
		{
			return _intValue;
		}
		set
		{
			if (value != _intValue)
			{
				_intValue = value;
				OnPropertyChangedWithValue(value, "IntValue");
				FloatValue = value;
			}
		}
	}

	[DataSourceProperty]
	public float FloatValue
	{
		get
		{
			return _floatValue;
		}
		set
		{
			if (value != _floatValue)
			{
				_floatValue = value;
				OnPropertyChangedWithValue(value, "FloatValue");
				IntValue = (int)value;
			}
		}
	}

	[DataSourceProperty]
	public string VisualId
	{
		get
		{
			return _visualId;
		}
		set
		{
			if (value != _visualId)
			{
				_visualId = value;
				OnPropertyChangedWithValue(value, "VisualId");
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

	public MapInfoItemVM(string itemId, Func<List<TooltipProperty>> getTooltip)
	{
		ItemId = itemId;
		VisualId = itemId;
		_tooltip = new BasicTooltipViewModel(getTooltip);
		FloatValue = -1f;
	}

	public MapInfoItemVM(string itemId, TooltipTriggerVM tooltipTrigger)
	{
		ItemId = itemId;
		VisualId = itemId;
		_tooltipTrigger = tooltipTrigger;
		FloatValue = -1f;
	}

	public void ExecuteBeginHint()
	{
		if (_tooltip != null)
		{
			_tooltip.ExecuteBeginHint();
		}
		else if (_tooltipTrigger != null)
		{
			_tooltipTrigger.ExecuteBeginHint();
		}
	}

	public void ExecuteEndHint()
	{
		if (_tooltip != null)
		{
			_tooltip.ExecuteEndHint();
		}
		else if (_tooltipTrigger != null)
		{
			_tooltipTrigger.ExecuteEndHint();
		}
	}

	public void SetOverriddenVisualId(string visualId)
	{
		VisualId = (string.IsNullOrEmpty(visualId) ? ItemId : visualId);
	}
}
