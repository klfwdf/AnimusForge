using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPLobbySigilItemVM : ViewModel
{
	private readonly Action<MPLobbySigilItemVM> _onSelection;

	private string _iconPath;

	private bool _isSelected;

	public int IconID { get; private set; }

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
				OnPropertyChanged("IconPath");
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
				OnPropertyChanged("IsSelected");
			}
		}
	}

	public MPLobbySigilItemVM()
	{
		RefreshWith(0);
	}

	public MPLobbySigilItemVM(int iconID, Action<MPLobbySigilItemVM> onSelection)
	{
		RefreshWith(iconID);
		_onSelection = onSelection;
	}

	public void RefreshWith(int iconID)
	{
		IconPath = iconID.ToString();
		IconID = iconID;
	}

	public void RefreshWith(Banner banner)
	{
		RefreshWith(banner.GetIconMeshId());
	}

	public void RefreshWith(string bannerCode)
	{
		RefreshWith(new Banner(bannerCode));
	}

	private void ExecuteSelectIcon()
	{
		_onSelection?.Invoke(this);
	}
}
