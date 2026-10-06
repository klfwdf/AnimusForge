using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class EquipmentActionItemVM : ViewModel
{
	private readonly Action<EquipmentActionItemVM> _onSelection;

	public object Identifier;

	private string _actionText;

	private string _typeAsString;

	private bool _isSelected;

	private bool _isWielded;

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
	public bool IsWielded
	{
		get
		{
			return _isWielded;
		}
		set
		{
			if (value != _isWielded)
			{
				_isWielded = value;
				OnPropertyChangedWithValue(value, "IsWielded");
			}
		}
	}

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
				if (value)
				{
					_onSelection(this);
				}
			}
		}
	}

	[DataSourceProperty]
	public string TypeAsString
	{
		get
		{
			return _typeAsString;
		}
		set
		{
			if (value != _typeAsString)
			{
				_typeAsString = value;
				OnPropertyChangedWithValue(value, "TypeAsString");
			}
		}
	}

	public EquipmentActionItemVM(string item, string itemTypeAsString, object identifier, Action<EquipmentActionItemVM> onSelection, bool isCurrentlyWielded = false)
	{
		Identifier = identifier;
		ActionText = item;
		TypeAsString = itemTypeAsString;
		IsWielded = isCurrentlyWielded;
		_onSelection = onSelection;
	}
}
