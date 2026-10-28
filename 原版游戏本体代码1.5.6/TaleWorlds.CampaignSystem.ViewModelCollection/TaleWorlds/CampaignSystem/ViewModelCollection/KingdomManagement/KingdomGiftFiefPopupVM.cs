using System;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;

public class KingdomGiftFiefPopupVM : ViewModel
{
	private Settlement _settlementToGive;

	private Action _onSettlementGranted;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	private bool _isAnyClanSelected;

	private MBBindingList<KingdomClanItemVM> _clans;

	private KingdomClanItemVM _currentSelectedClan;

	private KingdomClanSortControllerVM _clanSortController;

	private bool _isOpen;

	private string _titleText;

	private string _giftText;

	private string _cancelText;

	private string _bannerText;

	private string _nameText;

	private string _influenceText;

	private string _membersText;

	private string _fiefsText;

	private string _typeText;

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
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

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
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyClanSelected
	{
		get
		{
			return _isAnyClanSelected;
		}
		set
		{
			if (value != _isAnyClanSelected)
			{
				_isAnyClanSelected = value;
				OnPropertyChangedWithValue(value, "IsAnyClanSelected");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomClanItemVM> Clans
	{
		get
		{
			return _clans;
		}
		set
		{
			if (value != _clans)
			{
				_clans = value;
				OnPropertyChangedWithValue(value, "Clans");
			}
		}
	}

	[DataSourceProperty]
	public KingdomClanItemVM CurrentSelectedClan
	{
		get
		{
			return _currentSelectedClan;
		}
		set
		{
			if (value != _currentSelectedClan)
			{
				_currentSelectedClan = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedClan");
			}
		}
	}

	[DataSourceProperty]
	public KingdomClanSortControllerVM ClanSortController
	{
		get
		{
			return _clanSortController;
		}
		set
		{
			if (value != _clanSortController)
			{
				_clanSortController = value;
				OnPropertyChangedWithValue(value, "ClanSortController");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOpen
	{
		get
		{
			return _isOpen;
		}
		set
		{
			if (value != _isOpen)
			{
				_isOpen = value;
				OnPropertyChangedWithValue(value, "IsOpen");
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
	public string GiftText
	{
		get
		{
			return _giftText;
		}
		set
		{
			if (value != _giftText)
			{
				_giftText = value;
				OnPropertyChangedWithValue(value, "GiftText");
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
	public string BannerText
	{
		get
		{
			return _bannerText;
		}
		set
		{
			if (value != _bannerText)
			{
				_bannerText = value;
				OnPropertyChangedWithValue(value, "BannerText");
			}
		}
	}

	[DataSourceProperty]
	public string TypeText
	{
		get
		{
			return _typeText;
		}
		set
		{
			if (value != _typeText)
			{
				_typeText = value;
				OnPropertyChangedWithValue(value, "TypeText");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string InfluenceText
	{
		get
		{
			return _influenceText;
		}
		set
		{
			if (value != _influenceText)
			{
				_influenceText = value;
				OnPropertyChangedWithValue(value, "InfluenceText");
			}
		}
	}

	[DataSourceProperty]
	public string FiefsText
	{
		get
		{
			return _fiefsText;
		}
		set
		{
			if (value != _fiefsText)
			{
				_fiefsText = value;
				OnPropertyChangedWithValue(value, "FiefsText");
			}
		}
	}

	[DataSourceProperty]
	public string MembersText
	{
		get
		{
			return _membersText;
		}
		set
		{
			if (value != _membersText)
			{
				_membersText = value;
				OnPropertyChangedWithValue(value, "MembersText");
			}
		}
	}

	public KingdomGiftFiefPopupVM(Action onSettlementGranted)
	{
		_clans = new MBBindingList<KingdomClanItemVM>();
		_onSettlementGranted = onSettlementGranted;
		ClanSortController = new KingdomClanSortControllerVM(ref _clans);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=rOKAvjtT}Gift Settlement").ToString();
		GiftText = GameTexts.FindText("str_gift").ToString();
		CancelText = GameTexts.FindText("str_cancel").ToString();
		NameText = GameTexts.FindText("str_scoreboard_header", "name").ToString();
		InfluenceText = GameTexts.FindText("str_influence").ToString();
		FiefsText = GameTexts.FindText("str_fiefs").ToString();
		MembersText = GameTexts.FindText("str_members").ToString();
		BannerText = GameTexts.FindText("str_banner").ToString();
		TypeText = GameTexts.FindText("str_sort_by_type_label").ToString();
	}

	private void SetCurrentSelectedClan(KingdomClanItemVM clan)
	{
		if (clan != CurrentSelectedClan)
		{
			if (CurrentSelectedClan != null)
			{
				CurrentSelectedClan.IsSelected = false;
			}
			CurrentSelectedClan = clan;
			CurrentSelectedClan.IsSelected = true;
			IsAnyClanSelected = true;
		}
	}

	private void RefreshClanList()
	{
		Clans.Clear();
		foreach (Clan clan in Clan.PlayerClan.Kingdom.Clans)
		{
			if (FactionHelper.CanClanBeGrantedFief(clan))
			{
				Clans.Add(new KingdomClanItemVM(clan, SetCurrentSelectedClan));
			}
		}
		if (Clans.Count > 0)
		{
			SetCurrentSelectedClan(Clans[0]);
		}
		if (ClanSortController != null)
		{
			ClanSortController.SortByCurrentState();
		}
	}

	public void OpenWith(Settlement settlement)
	{
		_settlementToGive = settlement;
		RefreshClanList();
		IsOpen = true;
	}

	public void ExecuteGiftSettlement()
	{
		if (_settlementToGive != null && CurrentSelectedClan != null)
		{
			Campaign.Current.KingdomManager.GiftSettlementOwnership(_settlementToGive, CurrentSelectedClan.Clan);
			ExecuteClose();
			_onSettlementGranted();
		}
	}

	public void ExecuteClose()
	{
		_settlementToGive = null;
		IsOpen = false;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
