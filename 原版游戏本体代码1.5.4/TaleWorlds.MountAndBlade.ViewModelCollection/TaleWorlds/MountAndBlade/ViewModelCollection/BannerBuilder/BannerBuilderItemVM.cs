using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.BannerBuilder;

public class BannerBuilderItemVM : ViewModel
{
	private readonly Action<BannerBuilderItemVM> _onItemSelection;

	public int _meshID;

	public string _meshIDAsString;

	public bool _isSelected;

	public BannerIconData IconData { get; private set; }

	public string BackgroundTextureID { get; private set; }

	[DataSourceProperty]
	public int MeshID
	{
		get
		{
			return _meshID;
		}
		set
		{
			if (value != _meshID)
			{
				_meshID = value;
				OnPropertyChangedWithValue(value, "MeshID");
				MeshIDAsString = _meshID.ToString();
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
	public string MeshIDAsString
	{
		get
		{
			return _meshIDAsString;
		}
		set
		{
			if (value != _meshIDAsString)
			{
				_meshIDAsString = value;
				OnPropertyChangedWithValue(value, "MeshIDAsString");
			}
		}
	}

	public BannerBuilderItemVM(int key, BannerIconData iconData, Action<BannerBuilderItemVM> onItemSelection)
	{
		MeshID = key;
		IconData = iconData;
		_onItemSelection = onItemSelection;
	}

	public BannerBuilderItemVM(int key, string backgroundTextureID, Action<BannerBuilderItemVM> onItemSelection)
	{
		MeshID = key;
		BackgroundTextureID = backgroundTextureID;
		_onItemSelection = onItemSelection;
	}

	public void ExecuteSelection()
	{
		_onItemSelection?.Invoke(this);
	}
}
