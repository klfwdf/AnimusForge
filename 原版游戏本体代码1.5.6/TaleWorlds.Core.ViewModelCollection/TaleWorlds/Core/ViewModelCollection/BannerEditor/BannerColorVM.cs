using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.BannerEditor;

public class BannerColorVM : ViewModel
{
	private Action<BannerColorVM> _onSelection;

	private string _colorAsStr;

	private bool _isSelected;

	public int ColorID { get; }

	public uint Color { get; }

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

	public BannerColorVM(int colorID, uint color, Action<BannerColorVM> onSelection)
	{
		Color = color;
		ColorAsStr = TaleWorlds.Library.Color.FromUint(Color).ToString();
		ColorID = colorID;
		_onSelection = onSelection;
	}

	public void ExecuteSelectIcon()
	{
		_onSelection(this);
	}

	public void SetOnSelectionAction(Action<BannerColorVM> onSelection)
	{
		_onSelection = onSelection;
		IsSelected = false;
	}
}
