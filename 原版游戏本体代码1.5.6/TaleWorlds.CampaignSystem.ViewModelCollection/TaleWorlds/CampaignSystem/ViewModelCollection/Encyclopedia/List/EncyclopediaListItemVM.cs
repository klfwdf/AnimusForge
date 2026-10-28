using System;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;

public class EncyclopediaListItemVM : ViewModel
{
	private readonly string _type;

	private readonly Action _onShowTooltip;

	private string _id;

	private string _name;

	private string _comparedValue;

	private bool _isFiltered;

	private bool _isBookmarked;

	private bool _playerCanSeeValues;

	public object Object { get; private set; }

	public EncyclopediaListItem ListItem { get; }

	[DataSourceProperty]
	public bool IsFiltered
	{
		get
		{
			return _isFiltered;
		}
		set
		{
			if (value != _isFiltered)
			{
				_isFiltered = value;
				OnPropertyChangedWithValue(value, "IsFiltered");
			}
		}
	}

	[DataSourceProperty]
	public bool PlayerCanSeeValues
	{
		get
		{
			return _playerCanSeeValues;
		}
		set
		{
			if (value != _playerCanSeeValues)
			{
				_playerCanSeeValues = value;
				OnPropertyChangedWithValue(value, "PlayerCanSeeValues");
			}
		}
	}

	[DataSourceProperty]
	public string Id
	{
		get
		{
			return _id;
		}
		set
		{
			if (value != _id)
			{
				_id = value;
				OnPropertyChangedWithValue(value, "Id");
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
	public string ComparedValue
	{
		get
		{
			return _comparedValue;
		}
		set
		{
			if (value != _comparedValue)
			{
				_comparedValue = value;
				OnPropertyChangedWithValue(value, "ComparedValue");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBookmarked
	{
		get
		{
			return _isBookmarked;
		}
		set
		{
			if (value != _isBookmarked)
			{
				_isBookmarked = value;
				OnPropertyChangedWithValue(value, "IsBookmarked");
			}
		}
	}

	public EncyclopediaListItemVM(EncyclopediaListItem listItem)
	{
		Object = listItem.Object;
		Id = listItem.Id;
		_type = listItem.TypeName;
		ListItem = listItem;
		PlayerCanSeeValues = listItem.PlayerCanSeeValues;
		_onShowTooltip = listItem.OnShowTooltip;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = ListItem.Name;
	}

	public void Execute()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(_type, Id);
	}

	public void SetComparedValue(EncyclopediaListItemComparerBase comparer)
	{
		ComparedValue = comparer.GetComparedValueText(ListItem);
	}

	public void ExecuteBeginTooltip()
	{
		_onShowTooltip?.Invoke();
	}

	public void ExecuteEndTooltip()
	{
		if (_onShowTooltip != null)
		{
			MBInformationManager.HideInformations();
		}
	}
}
