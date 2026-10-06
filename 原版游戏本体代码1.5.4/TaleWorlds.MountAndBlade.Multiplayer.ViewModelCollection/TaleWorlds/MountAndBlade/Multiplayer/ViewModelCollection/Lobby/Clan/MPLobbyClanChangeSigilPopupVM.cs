using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanChangeSigilPopupVM : ViewModel
{
	private MPLobbySigilItemVM _selectedSigilIcon;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private bool _isSelected;

	private bool _canChangeSigil;

	private string _titleText;

	private string _applyText;

	private MBBindingList<MPLobbySigilItemVM> _iconsList;

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChanged("CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChanged("DoneInputKey");
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

	[DataSourceProperty]
	public bool CanChangeSigil
	{
		get
		{
			return _canChangeSigil;
		}
		set
		{
			if (value != _canChangeSigil)
			{
				_canChangeSigil = value;
				OnPropertyChanged("CanChangeSigil");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChanged("TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string ApplyText
	{
		get
		{
			return _applyText;
		}
		set
		{
			if (value != _applyText)
			{
				_applyText = value;
				OnPropertyChanged("ApplyText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbySigilItemVM> IconsList
	{
		get
		{
			return _iconsList;
		}
		set
		{
			if (value != _iconsList)
			{
				_iconsList = value;
				OnPropertyChanged("IconsList");
			}
		}
	}

	public MPLobbyClanChangeSigilPopupVM()
	{
		PrepareSigilIconsList();
		CanChangeSigil = false;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=q7VcSSbp}Choose Sigil").ToString();
		ApplyText = new TextObject("{=BAaS5Dkc}Apply").ToString();
	}

	private void PrepareSigilIconsList()
	{
		IconsList = new MBBindingList<MPLobbySigilItemVM>();
		_selectedSigilIcon = null;
		foreach (BannerIconGroup bannerIconGroup in BannerManager.Instance.BannerIconGroups)
		{
			if (bannerIconGroup.IsPattern)
			{
				continue;
			}
			foreach (KeyValuePair<int, BannerIconData> availableIcon in bannerIconGroup.AvailableIcons)
			{
				MPLobbySigilItemVM item = new MPLobbySigilItemVM(availableIcon.Key, OnSigilIconSelection);
				IconsList.Add(item);
			}
		}
	}

	private void OnSigilIconSelection(MPLobbySigilItemVM sigilIcon)
	{
		if (sigilIcon != _selectedSigilIcon)
		{
			if (_selectedSigilIcon != null)
			{
				_selectedSigilIcon.IsSelected = false;
			}
			_selectedSigilIcon = sigilIcon;
			if (_selectedSigilIcon != null)
			{
				_selectedSigilIcon.IsSelected = true;
				CanChangeSigil = true;
			}
		}
	}

	public void ExecuteOpenPopup()
	{
		IsSelected = true;
	}

	public void ExecuteClosePopup()
	{
		IsSelected = false;
	}

	public async void ExecuteChangeSigil()
	{
		BasicCultureObject basicCultureObject = Game.Current.ObjectManager.GetObject<BasicCultureObject>(NetworkMain.GameClient.ClanInfo.Faction);
		Banner banner = new Banner(basicCultureObject.Banner, basicCultureObject.BackgroundColor1, basicCultureObject.ForegroundColor1);
		banner.SetIconMeshId(_selectedSigilIcon.IconID);
		if (await NetworkMain.GameClient.ChangeClanSigil(banner.Serialize()))
		{
			ExecuteClosePopup();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CancelInputKey?.OnFinalize();
		DoneInputKey?.OnFinalize();
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
