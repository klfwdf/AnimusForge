using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.GameOver;

public class GameOverStatCategoryVM : ViewModel
{
	private readonly StatCategory _category;

	private readonly Action<GameOverStatCategoryVM> _onSelect;

	private string _name;

	private string _id;

	private bool _isSelected;

	private MBBindingList<GameOverStatItemVM> _items;

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
	public string ID
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
				OnPropertyChangedWithValue(value, "ID");
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
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GameOverStatItemVM> Items
	{
		get
		{
			return _items;
		}
		set
		{
			if (value != _items)
			{
				_items = value;
				OnPropertyChangedWithValue(value, "Items");
			}
		}
	}

	public GameOverStatCategoryVM(StatCategory category, Action<GameOverStatCategoryVM> onSelect)
	{
		_category = category;
		_onSelect = onSelect;
		Items = new MBBindingList<GameOverStatItemVM>();
		ID = category.ID;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Items.Clear();
		Name = GameTexts.FindText("str_game_over_stat_category", _category.ID).ToString();
		foreach (StatItem item in _category.Items)
		{
			Items.Add(new GameOverStatItemVM(item));
		}
	}

	public void ExecuteSelectCategory()
	{
		_onSelect?.DynamicInvokeWithLog(this);
	}
}
