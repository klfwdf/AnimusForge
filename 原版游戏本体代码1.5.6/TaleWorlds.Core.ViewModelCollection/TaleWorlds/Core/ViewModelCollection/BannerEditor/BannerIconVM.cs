using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.BannerEditor;

public class BannerIconVM : ViewModel
{
	private readonly Action<BannerIconVM> _onSelection;

	private string _iconPath;

	private bool _isSelected;

	public int IconID { get; }

	[DataSourceProperty]
	public string IconPath
	{
		get
		{
			return _iconPath;
		}
		set
		{
			if (value != _iconPath)
			{
				_iconPath = value;
				OnPropertyChangedWithValue(value, "IconPath");
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

	public BannerIconVM(int iconID, Action<BannerIconVM> onSelection)
	{
		IconPath = iconID.ToString();
		IconID = iconID;
		_onSelection = onSelection;
	}

	public void ExecuteSelectIcon()
	{
		_onSelection(this);
	}
}
