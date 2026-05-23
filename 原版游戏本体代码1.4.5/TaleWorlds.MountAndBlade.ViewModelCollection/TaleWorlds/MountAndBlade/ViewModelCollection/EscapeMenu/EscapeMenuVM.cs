using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

public class EscapeMenuVM : ViewModel
{
	private readonly TextObject _titleObj;

	private string _title;

	private MBBindingList<EscapeMenuItemVM> _menuItems;

	private GameTipsVM _tips;

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EscapeMenuItemVM> MenuItems
	{
		get
		{
			return _menuItems;
		}
		set
		{
			if (value != _menuItems)
			{
				_menuItems = value;
				OnPropertyChangedWithValue(value, "MenuItems");
			}
		}
	}

	[DataSourceProperty]
	public GameTipsVM Tips
	{
		get
		{
			return _tips;
		}
		set
		{
			if (value != _tips)
			{
				_tips = value;
				OnPropertyChangedWithValue(value, "Tips");
			}
		}
	}

	public EscapeMenuVM(IEnumerable<EscapeMenuItemVM> items, TextObject title = null)
	{
		_titleObj = title;
		MenuItems = new MBBindingList<EscapeMenuItemVM>();
		if (items != null)
		{
			foreach (EscapeMenuItemVM item in items)
			{
				MenuItems.Add(item);
			}
		}
		Tips = new GameTipsVM(isAutoChangeEnabled: true, navigationButtonsEnabled: true);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Title = _titleObj?.ToString() ?? "";
		MenuItems.ApplyActionOnAllItems(delegate(EscapeMenuItemVM x)
		{
			x.RefreshValues();
		});
		Tips.RefreshValues();
	}

	public virtual void Tick(float dt)
	{
	}

	public void RefreshItems(IEnumerable<EscapeMenuItemVM> items)
	{
		MenuItems.Clear();
		foreach (EscapeMenuItemVM item in items)
		{
			MenuItems.Add(item);
		}
	}
}
