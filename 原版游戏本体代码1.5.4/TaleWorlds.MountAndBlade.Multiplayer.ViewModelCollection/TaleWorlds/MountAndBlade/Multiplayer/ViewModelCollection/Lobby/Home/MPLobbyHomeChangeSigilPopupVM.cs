using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Home;

public class MPLobbyHomeChangeSigilPopupVM : ViewModel
{
	private class SigilItemUnlockStatusComparer : IComparer<MPLobbyCosmeticSigilItemVM>
	{
		public int Compare(MPLobbyCosmeticSigilItemVM x, MPLobbyCosmeticSigilItemVM y)
		{
			return y.IsUnlocked.CompareTo(x.IsUnlocked);
		}
	}

	private readonly Action<MPLobbyCosmeticSigilItemVM> _onItemObtainRequested;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private bool _isEnabled;

	private bool _isLoading;

	private bool _isInClan;

	private bool _isUsingClanSigil;

	private string _titleText;

	private string _changeText;

	private string _cancelText;

	private int _loot;

	private MBBindingList<MPLobbyCosmeticSigilItemVM> _sigilList;

	public MPLobbyCosmeticSigilItemVM SelectedSigil { get; private set; }

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
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
				OnIsEnabledChanged();
			}
		}
	}

	[DataSourceProperty]
	public bool IsLoading
	{
		get
		{
			return _isLoading;
		}
		set
		{
			if (value != _isLoading)
			{
				_isLoading = value;
				OnPropertyChangedWithValue(value, "IsLoading");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInClan
	{
		get
		{
			return _isInClan;
		}
		set
		{
			if (value != _isInClan)
			{
				_isInClan = value;
				OnPropertyChangedWithValue(value, "IsInClan");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUsingClanSigil
	{
		get
		{
			return _isUsingClanSigil;
		}
		set
		{
			if (value != _isUsingClanSigil)
			{
				_isUsingClanSigil = value;
				OnPropertyChangedWithValue(value, "IsUsingClanSigil");
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
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string ChangeText
	{
		get
		{
			return _changeText;
		}
		set
		{
			if (value != _changeText)
			{
				_changeText = value;
				OnPropertyChangedWithValue(value, "ChangeText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
			}
		}
	}

	[DataSourceProperty]
	public int Loot
	{
		get
		{
			return _loot;
		}
		set
		{
			if (value != _loot)
			{
				_loot = value;
				OnPropertyChangedWithValue(value, "Loot");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPLobbyCosmeticSigilItemVM> SigilList
	{
		get
		{
			return _sigilList;
		}
		set
		{
			if (value != _sigilList)
			{
				_sigilList = value;
				OnPropertyChangedWithValue(value, "SigilList");
			}
		}
	}

	public MPLobbyHomeChangeSigilPopupVM(Action<MPLobbyCosmeticSigilItemVM> onItemObtainRequested)
	{
		_onItemObtainRequested = onItemObtainRequested;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=7R0i82Nw}Change Sigil").ToString();
		ChangeText = new TextObject("{=Ba50zU7Z}Change").ToString();
		CancelText = GameTexts.FindText("str_cancel").ToString();
	}

	private void RefreshSigilList()
	{
		SigilList = new MBBindingList<MPLobbyCosmeticSigilItemVM>();
		SelectedSigil = null;
		MBReadOnlyList<CosmeticElement> cosmeticElementsList = CosmeticsManager.CosmeticElementsList;
		IReadOnlyList<string> ownedCosmetics = NetworkMain.GameClient.OwnedCosmetics;
		for (int i = 0; i < cosmeticElementsList.Count; i++)
		{
			if (cosmeticElementsList[i].Type == CosmeticsManager.CosmeticType.Sigil)
			{
				SigilCosmeticElement sigilCosmeticElement = cosmeticElementsList[i] as SigilCosmeticElement;
				MPLobbyCosmeticSigilItemVM mPLobbyCosmeticSigilItemVM = new MPLobbyCosmeticSigilItemVM(new Banner(sigilCosmeticElement.BannerCode).GetIconMeshId(), (int)sigilCosmeticElement.Rarity, sigilCosmeticElement.Cost, sigilCosmeticElement.Id);
				mPLobbyCosmeticSigilItemVM.IsUnlocked = ownedCosmetics.Contains(sigilCosmeticElement.Id) || sigilCosmeticElement.IsFree;
				SigilList.Add(mPLobbyCosmeticSigilItemVM);
			}
		}
		IsUsingClanSigil = NetworkMain.GameClient.PlayerData.IsUsingClanSigil;
		SelectPlayerSigil(NetworkMain.GameClient.PlayerData);
		Loot = NetworkMain.GameClient.PlayerData.Gold;
		SigilList.Sort(new SigilItemUnlockStatusComparer());
	}

	private void SelectPlayerSigil(PlayerData playerData)
	{
		int playerBannerID = new Banner(playerData.Sigil).GetIconMeshId();
		OnSigilSelected(SigilList.First((MPLobbyCosmeticSigilItemVM s) => s.IconID == playerBannerID));
	}

	public void Open()
	{
		IsInClan = NetworkMain.GameClient.IsInClan;
		IsEnabled = true;
	}

	public void ExecuteClosePopup()
	{
		IsEnabled = false;
	}

	public async void ExecuteChangeSigil()
	{
		if (await NetworkMain.GameClient.ChangeSigil(SelectedSigil.CosmeticID))
		{
			NetworkMain.GameClient.PlayerData.IsUsingClanSigil = IsUsingClanSigil;
			IsEnabled = false;
		}
	}

	private void OnSigilObtainRequested(MPLobbyCosmeticSigilItemVM sigilItem)
	{
		_onItemObtainRequested(sigilItem);
	}

	private void OnSigilSelected(MPLobbyCosmeticSigilItemVM sigilItem)
	{
		if (sigilItem != SelectedSigil)
		{
			if (SelectedSigil != null)
			{
				SelectedSigil.IsUsed = false;
			}
			SelectedSigil = sigilItem;
			if (SelectedSigil != null)
			{
				SelectedSigil.IsUsed = true;
			}
		}
	}

	public void OnLootUpdated(int finalLoot)
	{
		Loot = finalLoot;
	}

	private void OnIsEnabledChanged()
	{
		if (IsEnabled)
		{
			RefreshSigilList();
			MPLobbyCosmeticSigilItemVM.SetOnObtainRequestedCallback(OnSigilObtainRequested);
			MPLobbyCosmeticSigilItemVM.SetOnSelectionCallback(OnSigilSelected);
		}
		else
		{
			MPLobbyCosmeticSigilItemVM.ResetOnObtainRequestedCallback();
			MPLobbyCosmeticSigilItemVM.ResetOnSelectionCallback();
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
