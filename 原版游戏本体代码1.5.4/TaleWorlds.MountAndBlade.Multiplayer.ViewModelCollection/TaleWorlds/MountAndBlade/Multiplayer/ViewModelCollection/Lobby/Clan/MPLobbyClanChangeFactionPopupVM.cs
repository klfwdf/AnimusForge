using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Clan;

public class MPLobbyClanChangeFactionPopupVM : ViewModel
{
	private MPCultureItemVM _selectedFaction;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private bool _isSelected;

	private bool _canChangeFaction;

	private string _titleText;

	private string _applyText;

	private MBBindingList<MPCultureItemVM> _factionsList;

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
	public bool CanChangeFaction
	{
		get
		{
			return _canChangeFaction;
		}
		set
		{
			if (value != _canChangeFaction)
			{
				_canChangeFaction = value;
				OnPropertyChanged("CanChangeFaction");
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
	public MBBindingList<MPCultureItemVM> FactionsList
	{
		get
		{
			return _factionsList;
		}
		set
		{
			if (value != _factionsList)
			{
				_factionsList = value;
				OnPropertyChanged("FactionsList");
			}
		}
	}

	public MPLobbyClanChangeFactionPopupVM()
	{
		PrepareFactionsList();
		CanChangeFaction = false;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=ghjSIyIL}Choose Culture").ToString();
		ApplyText = new TextObject("{=BAaS5Dkc}Apply").ToString();
	}

	private void PrepareFactionsList()
	{
		_selectedFaction = null;
		FactionsList = new MBBindingList<MPCultureItemVM>
		{
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("vlandia").StringId, OnFactionSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("sturgia").StringId, OnFactionSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("empire").StringId, OnFactionSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("battania").StringId, OnFactionSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("khuzait").StringId, OnFactionSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("aserai").StringId, OnFactionSelection)
		};
	}

	private void OnFactionSelection(MPCultureItemVM faction)
	{
		if (faction != _selectedFaction)
		{
			if (_selectedFaction != null)
			{
				_selectedFaction.IsSelected = false;
			}
			_selectedFaction = faction;
			if (_selectedFaction != null)
			{
				_selectedFaction.IsSelected = true;
				CanChangeFaction = true;
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

	public async void ExecuteChangeFaction()
	{
		BasicCultureObject basicCultureObject = Game.Current.ObjectManager.GetObject<BasicCultureObject>(_selectedFaction.CultureCode);
		Banner banner = new Banner(NetworkMain.GameClient.ClanInfo.Sigil);
		banner.ChangeIconColors(basicCultureObject.ForegroundColor1);
		banner.ChangePrimaryColor(basicCultureObject.BackgroundColor1);
		if (await NetworkMain.GameClient.ChangeClanSigil(banner.Serialize()) & await NetworkMain.GameClient.ChangeClanFaction(_selectedFaction.CultureCode))
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
