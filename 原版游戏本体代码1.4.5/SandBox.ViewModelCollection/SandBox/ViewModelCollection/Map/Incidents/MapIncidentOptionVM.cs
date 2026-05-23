using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Map.Incidents;

public class MapIncidentOptionVM : ViewModel
{
	public readonly int Index;

	private readonly TextObject _descriptionText;

	private readonly List<TextObject> _hints;

	private readonly Action<MapIncidentOptionVM> _onSelected;

	private readonly Action<MapIncidentOptionVM> _onFocused;

	private bool _isSelected;

	private bool _isFocused;

	private string _description;

	private string _hint;

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		set
		{
			if (value != _isFocused)
			{
				_isFocused = value;
				OnPropertyChangedWithValue(value, "IsFocused");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	[DataSourceProperty]
	public string Hint
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

	public MapIncidentOptionVM(TextObject description, List<TextObject> hints, int index, Action<MapIncidentOptionVM> onSelected, Action<MapIncidentOptionVM> onFocused)
	{
		Index = index;
		_descriptionText = description;
		_hints = hints.ToList();
		_onSelected = onSelected;
		_onFocused = onFocused;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Description = _descriptionText.ToString();
		Hint = CampaignUIHelper.MergeTextObjectsWithNewline(_hints);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
	}

	public void ExecuteSelect()
	{
		_onSelected(this);
	}

	public void ExecuteFocus()
	{
		_onFocused(this);
	}

	public void ExecuteUnfocus()
	{
		_onFocused(null);
	}
}
