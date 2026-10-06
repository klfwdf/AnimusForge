using System;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Map.Incidents;

public class MapIncidentOptionVM : ViewModel
{
	public readonly int Index;

	private readonly TextObject _descriptionText;

	private readonly Action<MapIncidentOptionVM> _onSelected;

	private readonly Action<MapIncidentOptionVM> _onFocused;

	private bool _isSelected;

	private bool _isFocused;

	private string _description;

	private MapIncidentHintVM _hint;

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
	public MapIncidentHintVM Hint
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

	public MapIncidentOptionVM(TextObject description, IncidentHint hint, int index, Action<MapIncidentOptionVM> onSelected, Action<MapIncidentOptionVM> onFocused)
	{
		Index = index;
		_descriptionText = description;
		_onSelected = onSelected;
		_onFocused = onFocused;
		Hint = new MapIncidentHintVM(hint);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Description = _descriptionText.ToString();
		Hint.RefreshValues();
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
