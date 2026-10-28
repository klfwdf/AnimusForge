using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.FaceGenerator;

public class FacegenListItemVM : ViewModel
{
	private readonly Action<FacegenListItemVM, bool> _setSelected;

	private string _imagePath;

	private bool _isSelected = true;

	private int _index = -1;

	[DataSourceProperty]
	public string ImagePath
	{
		get
		{
			return _imagePath;
		}
		set
		{
			if (value != _imagePath)
			{
				_imagePath = value;
				OnPropertyChangedWithValue(value, "ImagePath");
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
	public int Index
	{
		get
		{
			return _index;
		}
		set
		{
			if (value != _index)
			{
				_index = value;
				OnPropertyChangedWithValue(value, "Index");
			}
		}
	}

	public void ExecuteAction()
	{
		_setSelected(this, arg2: true);
	}

	public FacegenListItemVM(string imagePath, int index, Action<FacegenListItemVM, bool> setSelected)
	{
		ImagePath = imagePath;
		Index = index;
		IsSelected = false;
		_setSelected = setSelected;
	}
}
