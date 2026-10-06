using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.BannerBuilder;

public class BannerBuilderColorItemVM : ViewModel
{
	private readonly Action<BannerBuilderColorItemVM> _onItemSelection;

	private bool _isSelected;

	private string _colorAsStr;

	public int ColorID { get; private set; }

	public BannerColor BannerColor { get; private set; }

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
	public string ColorAsStr
	{
		get
		{
			return _colorAsStr;
		}
		set
		{
			if (value != _colorAsStr)
			{
				_colorAsStr = value;
				OnPropertyChangedWithValue(value, "ColorAsStr");
			}
		}
	}

	public BannerBuilderColorItemVM(Action<BannerBuilderColorItemVM> onItemSelection, int key, BannerColor value)
	{
		_onItemSelection = onItemSelection;
		ColorID = key;
		BannerColor = value;
		ColorAsStr = Color.FromUint(value.Color).ToString();
	}

	public void ExecuteSelection()
	{
		_onItemSelection?.Invoke(this);
	}
}
