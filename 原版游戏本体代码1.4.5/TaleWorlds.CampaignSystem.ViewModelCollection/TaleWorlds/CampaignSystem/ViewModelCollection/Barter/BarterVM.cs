using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Barter;

public class BarterVM : ViewModel
{
	private readonly List<Dictionary<BarterGroup, MBBindingList<BarterItemVM>>> _barterList;

	private readonly List<MBBindingList<BarterItemVM>> _offerList;

	private readonly Dictionary<BarterGroup, MBBindingList<BarterItemVM>> _leftList;

	private readonly Dictionary<BarterGroup, MBBindingList<BarterItemVM>> _rightList;

	private readonly bool _isPlayerOfferer;

	private readonly BarterManager _barter;

	private readonly CharacterObject _otherCharacter;

	private readonly PartyBase _otherParty;

	private readonly BarterData _barterData;

	private string _fiefLbl;

	private string _prisonerLbl;

	private string _itemLbl;

	private string _otherLbl;

	private string _cancelLbl;

	private string _resetLbl;

	private string _offerLbl;

	private string _diplomaticLbl;

	private HintViewModel _autoBalanceHint;

	private HeroVM _leftHero;

	private HeroVM _rightHero;

	private string _leftNameLbl;

	private string _rightNameLbl;

	private MBBindingList<BarterItemVM> _leftFiefList;

	private MBBindingList<BarterItemVM> _rightFiefList;

	private MBBindingList<BarterItemVM> _leftPrisonerList;

	private MBBindingList<BarterItemVM> _rightPrisonerList;

	private MBBindingList<BarterItemVM> _leftItemList;

	private MBBindingList<BarterItemVM> _rightItemList;

	private MBBindingList<BarterItemVM> _leftOtherList;

	private MBBindingList<BarterItemVM> _rightOtherList;

	private MBBindingList<BarterItemVM> _leftDiplomaticList;

	private MBBindingList<BarterItemVM> _rightDiplomaticList;

	private MBBindingList<BarterItemVM> _leftGoldList;

	private MBBindingList<BarterItemVM> _rightGoldList;

	private MBBindingList<BarterItemVM> _leftOfferList;

	private MBBindingList<BarterItemVM> _rightOfferList;

	private int _leftMaxGold;

	private int _rightMaxGold;

	private bool _initializationIsOver;

	private bool _isOfferDisabled;

	private int _resultBarOffererPercentage = -1;

	private int _resultBarOtherPercentage = -1;

	private InputKeyItemVM _resetInputKey;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	private IFaction OtherFaction
	{
		get
		{
			if (!_otherCharacter.IsHero)
			{
				return _otherParty.MapFaction;
			}
			return _otherCharacter.HeroObject.Clan;
		}
	}

	[DataSourceProperty]
	public string FiefLbl
	{
		get
		{
			return _fiefLbl;
		}
		set
		{
			if (value != _fiefLbl)
			{
				_fiefLbl = value;
				OnPropertyChangedWithValue(value, "FiefLbl");
			}
		}
	}

	[DataSourceProperty]
	public string PrisonerLbl
	{
		get
		{
			return _prisonerLbl;
		}
		set
		{
			if (value != _prisonerLbl)
			{
				_prisonerLbl = value;
				OnPropertyChangedWithValue(value, "PrisonerLbl");
			}
		}
	}

	[DataSourceProperty]
	public string ItemLbl
	{
		get
		{
			return _itemLbl;
		}
		set
		{
			if (value != _itemLbl)
			{
				_itemLbl = value;
				OnPropertyChangedWithValue(value, "ItemLbl");
			}
		}
	}

	[DataSourceProperty]
	public string OtherLbl
	{
		get
		{
			return _otherLbl;
		}
		set
		{
			if (value != _otherLbl)
			{
				_otherLbl = value;
				OnPropertyChangedWithValue(value, "OtherLbl");
			}
		}
	}

	[DataSourceProperty]
	public string CancelLbl
	{
		get
		{
			return _cancelLbl;
		}
		set
		{
			if (value != _cancelLbl)
			{
				_cancelLbl = value;
				OnPropertyChangedWithValue(value, "CancelLbl");
			}
		}
	}

	[DataSourceProperty]
	public string ResetLbl
	{
		get
		{
			return _resetLbl;
		}
		set
		{
			if (value != _resetLbl)
			{
				_resetLbl = value;
				OnPropertyChangedWithValue(value, "ResetLbl");
			}
		}
	}

	[DataSourceProperty]
	public string OfferLbl
	{
		get
		{
			return _offerLbl;
		}
		set
		{
			if (value != _offerLbl)
			{
				_offerLbl = value;
				OnPropertyChangedWithValue(value, "OfferLbl");
			}
		}
	}

	[DataSourceProperty]
	public string DiplomaticLbl
	{
		get
		{
			return _diplomaticLbl;
		}
		set
		{
			if (value != _diplomaticLbl)
			{
				_diplomaticLbl = value;
				OnPropertyChangedWithValue(value, "DiplomaticLbl");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AutoBalanceHint
	{
		get
		{
			return _autoBalanceHint;
		}
		set
		{
			if (value != _autoBalanceHint)
			{
				_autoBalanceHint = value;
				OnPropertyChangedWithValue(value, "AutoBalanceHint");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM LeftHero
	{
		get
		{
			return _leftHero;
		}
		set
		{
			if (value != _leftHero)
			{
				_leftHero = value;
				OnPropertyChangedWithValue(value, "LeftHero");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM RightHero
	{
		get
		{
			return _rightHero;
		}
		set
		{
			if (value != _rightHero)
			{
				_rightHero = value;
				OnPropertyChangedWithValue(value, "RightHero");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOfferDisabled
	{
		get
		{
			return _isOfferDisabled;
		}
		set
		{
			if (value != _isOfferDisabled)
			{
				_isOfferDisabled = value;
				OnPropertyChangedWithValue(value, "IsOfferDisabled");
			}
		}
	}

	[DataSourceProperty]
	public int LeftMaxGold
	{
		get
		{
			return _leftMaxGold;
		}
		set
		{
			if (value != _leftMaxGold)
			{
				_leftMaxGold = value;
				OnPropertyChangedWithValue(value, "LeftMaxGold");
			}
		}
	}

	[DataSourceProperty]
	public int RightMaxGold
	{
		get
		{
			return _rightMaxGold;
		}
		set
		{
			if (value != _rightMaxGold)
			{
				_rightMaxGold = value;
				OnPropertyChangedWithValue(value, "RightMaxGold");
			}
		}
	}

	[DataSourceProperty]
	public string LeftNameLbl
	{
		get
		{
			return _leftNameLbl;
		}
		set
		{
			if (value != _leftNameLbl)
			{
				_leftNameLbl = value;
				OnPropertyChangedWithValue(value, "LeftNameLbl");
			}
		}
	}

	[DataSourceProperty]
	public string RightNameLbl
	{
		get
		{
			return _rightNameLbl;
		}
		set
		{
			if (value != _rightNameLbl)
			{
				_rightNameLbl = value;
				OnPropertyChangedWithValue(value, "RightNameLbl");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> LeftFiefList
	{
		get
		{
			return _leftFiefList;
		}
		set
		{
			if (value != _leftFiefList)
			{
				_leftFiefList = value;
				OnPropertyChangedWithValue(value, "LeftFiefList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> RightFiefList
	{
		get
		{
			return _rightFiefList;
		}
		set
		{
			if (value != _rightFiefList)
			{
				_rightFiefList = value;
				OnPropertyChangedWithValue(value, "RightFiefList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> LeftPrisonerList
	{
		get
		{
			return _leftPrisonerList;
		}
		set
		{
			if (value != _leftPrisonerList)
			{
				_leftPrisonerList = value;
				OnPropertyChangedWithValue(value, "LeftPrisonerList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> RightPrisonerList
	{
		get
		{
			return _rightPrisonerList;
		}
		set
		{
			if (value != _rightPrisonerList)
			{
				_rightPrisonerList = value;
				OnPropertyChangedWithValue(value, "RightPrisonerList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> LeftItemList
	{
		get
		{
			return _leftItemList;
		}
		set
		{
			if (value != _leftItemList)
			{
				_leftItemList = value;
				OnPropertyChangedWithValue(value, "LeftItemList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> RightItemList
	{
		get
		{
			return _rightItemList;
		}
		set
		{
			if (value != _rightItemList)
			{
				_rightItemList = value;
				OnPropertyChangedWithValue(value, "RightItemList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> LeftOtherList
	{
		get
		{
			return _leftOtherList;
		}
		set
		{
			if (value != _leftOtherList)
			{
				_leftOtherList = value;
				OnPropertyChangedWithValue(value, "LeftOtherList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> RightOtherList
	{
		get
		{
			return _rightOtherList;
		}
		set
		{
			if (value != _rightOtherList)
			{
				_rightOtherList = value;
				OnPropertyChangedWithValue(value, "RightOtherList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> LeftDiplomaticList
	{
		get
		{
			return _leftDiplomaticList;
		}
		set
		{
			if (value != _leftDiplomaticList)
			{
				_leftDiplomaticList = value;
				OnPropertyChangedWithValue(value, "LeftDiplomaticList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> RightDiplomaticList
	{
		get
		{
			return _rightDiplomaticList;
		}
		set
		{
			if (value != _rightDiplomaticList)
			{
				_rightDiplomaticList = value;
				OnPropertyChangedWithValue(value, "RightDiplomaticList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> LeftOfferList
	{
		get
		{
			return _leftOfferList;
		}
		set
		{
			if (value != _leftOfferList)
			{
				_leftOfferList = value;
				OnPropertyChangedWithValue(value, "LeftOfferList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> RightOfferList
	{
		get
		{
			return _rightOfferList;
		}
		set
		{
			if (value != _rightOfferList)
			{
				_rightOfferList = value;
				OnPropertyChangedWithValue(value, "RightOfferList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> RightGoldList
	{
		get
		{
			return _rightGoldList;
		}
		set
		{
			if (value != _rightGoldList)
			{
				_rightGoldList = value;
				OnPropertyChangedWithValue(value, "RightGoldList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BarterItemVM> LeftGoldList
	{
		get
		{
			return _leftGoldList;
		}
		set
		{
			if (value != _leftGoldList)
			{
				_leftGoldList = value;
				OnPropertyChangedWithValue(value, "LeftGoldList");
			}
		}
	}

	[DataSourceProperty]
	public bool InitializationIsOver
	{
		get
		{
			return _initializationIsOver;
		}
		set
		{
			_initializationIsOver = value;
			OnPropertyChangedWithValue(value, "InitializationIsOver");
		}
	}

	[DataSourceProperty]
	public int ResultBarOtherPercentage
	{
		get
		{
			return _resultBarOtherPercentage;
		}
		set
		{
			_resultBarOtherPercentage = value;
			OnPropertyChangedWithValue(value, "ResultBarOtherPercentage");
		}
	}

	[DataSourceProperty]
	public int ResultBarOffererPercentage
	{
		get
		{
			return _resultBarOffererPercentage;
		}
		set
		{
			_resultBarOffererPercentage = value;
			OnPropertyChangedWithValue(value, "ResultBarOffererPercentage");
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ResetInputKey
	{
		get
		{
			return _resetInputKey;
		}
		set
		{
			if (value != _resetInputKey)
			{
				_resetInputKey = value;
				OnPropertyChangedWithValue(value, "ResetInputKey");
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

	public BarterVM(BarterData args)
	{
		_barterData = args;
		if (_barterData.OtherHero == Hero.MainHero)
		{
			_otherParty = _barterData.OffererParty;
			_otherCharacter = _barterData.OffererHero.CharacterObject ?? CampaignUIHelper.GetVisualPartyLeader(_otherParty);
		}
		else if (_barterData.OtherHero != null)
		{
			_otherCharacter = _barterData.OtherHero.CharacterObject;
			LeftMaxGold = _otherCharacter.HeroObject.Gold;
		}
		else
		{
			_otherParty = _barterData.OtherParty;
			_otherCharacter = CampaignUIHelper.GetVisualPartyLeader(_otherParty);
			LeftMaxGold = _otherParty.MobileParty.PartyTradeGold;
		}
		_barter = Campaign.Current.BarterManager;
		_isPlayerOfferer = _barterData.OffererHero == Hero.MainHero;
		AutoBalanceHint = new HintViewModel();
		LeftFiefList = new MBBindingList<BarterItemVM>();
		RightFiefList = new MBBindingList<BarterItemVM>();
		LeftPrisonerList = new MBBindingList<BarterItemVM>();
		RightPrisonerList = new MBBindingList<BarterItemVM>();
		LeftItemList = new MBBindingList<BarterItemVM>();
		RightItemList = new MBBindingList<BarterItemVM>();
		LeftOtherList = new MBBindingList<BarterItemVM>();
		RightOtherList = new MBBindingList<BarterItemVM>();
		LeftDiplomaticList = new MBBindingList<BarterItemVM>();
		RightDiplomaticList = new MBBindingList<BarterItemVM>();
		LeftGoldList = new MBBindingList<BarterItemVM>();
		RightGoldList = new MBBindingList<BarterItemVM>();
		_leftList = new Dictionary<BarterGroup, MBBindingList<BarterItemVM>>();
		_rightList = new Dictionary<BarterGroup, MBBindingList<BarterItemVM>>();
		_barterList = new List<Dictionary<BarterGroup, MBBindingList<BarterItemVM>>>();
		_offerList = new List<MBBindingList<BarterItemVM>>();
		LeftOfferList = new MBBindingList<BarterItemVM>();
		RightOfferList = new MBBindingList<BarterItemVM>();
		InitBarterList(_barterData);
		OnInitialized();
		RightMaxGold = Hero.MainHero.Gold;
		LeftHero = new HeroVM(_otherCharacter.HeroObject);
		RightHero = new HeroVM(Hero.MainHero);
		SendOffer();
		InitializationIsOver = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		InitializeStaticContent();
		LeftNameLbl = _otherCharacter.Name.ToString();
		RightNameLbl = Hero.MainHero.Name.ToString();
		LeftFiefList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
		RightFiefList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
		LeftPrisonerList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
		RightPrisonerList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
		LeftItemList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
		RightItemList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
		LeftOtherList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
		RightOtherList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
		LeftDiplomaticList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
		RightDiplomaticList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
		LeftGoldList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
		RightGoldList.ApplyActionOnAllItems(delegate(BarterItemVM x)
		{
			x.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey.OnFinalize();
		CancelInputKey.OnFinalize();
		ResetInputKey.OnFinalize();
	}

	private void InitBarterList(BarterData args)
	{
		_leftList.Add(args.GetBarterGroup<FiefBarterGroup>(), LeftFiefList);
		_leftList.Add(args.GetBarterGroup<PrisonerBarterGroup>(), LeftPrisonerList);
		_leftList.Add(args.GetBarterGroup<ItemBarterGroup>(), LeftItemList);
		_leftList.Add(args.GetBarterGroup<OtherBarterGroup>(), LeftOtherList);
		_leftList.Add(args.GetBarterGroup<GoldBarterGroup>(), LeftGoldList);
		_rightList.Add(args.GetBarterGroup<FiefBarterGroup>(), RightFiefList);
		_rightList.Add(args.GetBarterGroup<PrisonerBarterGroup>(), RightPrisonerList);
		_rightList.Add(args.GetBarterGroup<ItemBarterGroup>(), RightItemList);
		_rightList.Add(args.GetBarterGroup<OtherBarterGroup>(), RightOtherList);
		_rightList.Add(args.GetBarterGroup<GoldBarterGroup>(), RightGoldList);
		_barterList.Add(_leftList);
		_barterList.Add(_rightList);
		_offerList.Add(LeftOfferList);
		_offerList.Add(RightOfferList);
		if (_barterData.ContextInitializer != null)
		{
			foreach (Barterable barterable in _barterData.GetBarterables())
			{
				if (barterable.IsContextDependent && _barterData.ContextInitializer(barterable, _barterData))
				{
					ChangeBarterableIsOffered(barterable, newState: true);
				}
			}
		}
		foreach (Barterable barterable2 in args.GetBarterables())
		{
			if (!barterable2.IsOffered && !barterable2.IsContextDependent)
			{
				_barterList[(barterable2.OriginalOwner == Hero.MainHero) ? 1 : 0][barterable2.Group].Add(new BarterItemVM(barterable2, TransferItem, OnOfferedAmountChange));
				continue;
			}
			BarterItemVM barterItemVM = new BarterItemVM(barterable2, TransferItem, OnOfferedAmountChange, barterable2.IsContextDependent);
			_offerList[(barterable2.OriginalOwner == Hero.MainHero) ? 1 : 0].Add(barterItemVM);
			RefreshCompatibility(barterItemVM, gotOffered: true);
		}
		_barterData.GetBarterables().Find((Barterable t) => t.Group.GetType() == typeof(GoldBarterGroup) && t.OriginalOwner == Hero.MainHero);
		_barterData.GetBarterables().Find((Barterable t) => (t.Group.GetType() == typeof(GoldBarterGroup) && _barterData.OffererHero == Hero.MainHero && t.OriginalOwner == _barterData.OtherHero) || (_barterData.OtherHero == Hero.MainHero && t.OriginalOwner == _barterData.OffererHero));
		RefreshOfferLabel();
	}

	private void ChangeBarterableIsOffered(Barterable barterable, bool newState)
	{
		if (barterable.IsOffered == newState)
		{
			return;
		}
		barterable.SetIsOffered(newState);
		OnTransferItem(barterable, isTransferrable: true);
		foreach (Barterable linkedBarterable in barterable.LinkedBarterables)
		{
			OnTransferItem(linkedBarterable, isTransferrable: true);
		}
	}

	public void OnInitialized()
	{
		BarterManager barterManager = Campaign.Current.BarterManager;
		barterManager.Closed = (BarterManager.BarterCloseEventDelegate)Delegate.Combine(barterManager.Closed, new BarterManager.BarterCloseEventDelegate(OnClosed));
	}

	private void OnClosed()
	{
		BarterManager barterManager = Campaign.Current.BarterManager;
		barterManager.Closed = (BarterManager.BarterCloseEventDelegate)Delegate.Remove(barterManager.Closed, new BarterManager.BarterCloseEventDelegate(OnClosed));
	}

	public void ExecuteTransferAllLeftFief()
	{
		ExecuteTransferAll(_otherCharacter, _barterData.GetBarterGroup<FiefBarterGroup>());
	}

	public void ExecuteAutoBalance()
	{
		AutoBalanceAdd();
		AutoBalanceRemove();
		AutoBalanceAdd();
	}

	private void AutoBalanceRemove()
	{
		if ((int)Campaign.Current.BarterManager.GetOfferValue(_otherCharacter.HeroObject, _otherParty, _barterData.OffererParty, _barterData.GetOfferedBarterables()) <= 0)
		{
			return;
		}
		List<(Barterable, int)> newBarterables = BarterHelper.GetAutoBalanceBarterablesToRemove(_barterData, OtherFaction, Clan.PlayerClan.MapFaction, Hero.MainHero).ToList();
		List<(BarterItemVM, int)> list = new List<(BarterItemVM, int)>();
		GetBarterItems(RightGoldList, newBarterables, list);
		GetBarterItems(RightItemList, newBarterables, list);
		GetBarterItems(RightPrisonerList, newBarterables, list);
		GetBarterItems(RightFiefList, newBarterables, list);
		foreach (var (barterItemVM, count) in list)
		{
			OfferItemRemove(barterItemVM, count);
		}
	}

	private void AutoBalanceAdd()
	{
		if ((int)Campaign.Current.BarterManager.GetOfferValue(_otherCharacter.HeroObject, _otherParty, _barterData.OffererParty, _barterData.GetOfferedBarterables()) >= 0)
		{
			return;
		}
		List<(Barterable, int)> newBarterables = BarterHelper.GetAutoBalanceBarterablesAdd(_barterData, OtherFaction, Clan.PlayerClan.MapFaction, Hero.MainHero).ToList();
		List<(BarterItemVM, int)> list = new List<(BarterItemVM, int)>();
		GetBarterItems(RightGoldList, newBarterables, list);
		GetBarterItems(RightItemList, newBarterables, list);
		GetBarterItems(RightPrisonerList, newBarterables, list);
		GetBarterItems(RightFiefList, newBarterables, list);
		foreach (var (barterItemVM, num) in list)
		{
			if (num > 0)
			{
				OfferItemAdd(barterItemVM, num);
			}
		}
	}

	private void GetBarterItems(MBBindingList<BarterItemVM> itemList, List<(Barterable barterable, int count)> newBarterables, List<(BarterItemVM, int)> barterItems)
	{
		foreach (BarterItemVM item2 in itemList)
		{
			foreach (var (barterable, item) in newBarterables)
			{
				if (barterable == item2.Barterable)
				{
					barterItems.Add((item2, item));
				}
			}
		}
	}

	public void ExecuteTransferAllLeftItem()
	{
		ExecuteTransferAll(_otherCharacter, _barterData.GetBarterGroup<ItemBarterGroup>());
	}

	public void ExecuteTransferAllLeftPrisoner()
	{
		ExecuteTransferAll(_otherCharacter, _barterData.GetBarterGroup<PrisonerBarterGroup>());
	}

	public void ExecuteTransferAllLeftOther()
	{
		ExecuteTransferAll(_otherCharacter, _barterData.GetBarterGroup<OtherBarterGroup>());
	}

	public void ExecuteTransferAllRightFief()
	{
		ExecuteTransferAll(CharacterObject.PlayerCharacter, _barterData.GetBarterGroup<FiefBarterGroup>());
	}

	public void ExecuteTransferAllRightItem()
	{
		ExecuteTransferAll(CharacterObject.PlayerCharacter, _barterData.GetBarterGroup<ItemBarterGroup>());
	}

	public void ExecuteTransferAllRightPrisoner()
	{
		ExecuteTransferAll(CharacterObject.PlayerCharacter, _barterData.GetBarterGroup<PrisonerBarterGroup>());
	}

	public void ExecuteTransferAllRightOther()
	{
		ExecuteTransferAll(CharacterObject.PlayerCharacter, _barterData.GetBarterGroup<OtherBarterGroup>());
	}

	private void ExecuteTransferAll(CharacterObject fromCharacter, BarterGroup barterGroup)
	{
		if (barterGroup == null)
		{
			return;
		}
		foreach (BarterItemVM item in new List<BarterItemVM>(_barterList[(fromCharacter == CharacterObject.PlayerCharacter) ? 1 : 0][barterGroup].Where((BarterItemVM barterItem) => !barterItem.Barterable.IsOffered)))
		{
			TransferItem(item, offerAll: true);
		}
		foreach (BarterItemVM item2 in _barterList[(fromCharacter == CharacterObject.PlayerCharacter) ? 1 : 0][barterGroup])
		{
			item2.CurrentOfferedAmount = item2.TotalItemCount;
		}
	}

	private void SendOffer()
	{
		IsOfferDisabled = !IsCurrentOfferAcceptable() || (LeftOfferList.Count == 0 && RightOfferList.Count == 0);
		RefreshResultBar();
	}

	private bool IsCurrentOfferAcceptable()
	{
		return Campaign.Current.BarterManager.IsOfferAcceptable(_barterData, _otherCharacter.HeroObject, _otherParty);
	}

	private void RefreshResultBar()
	{
		long num = 0L;
		long num2 = 0L;
		IFaction otherFaction = OtherFaction;
		foreach (BarterItemVM leftOffer in LeftOfferList)
		{
			int valueForFaction = leftOffer.Barterable.GetValueForFaction(otherFaction);
			if (valueForFaction < 0)
			{
				num2 += valueForFaction;
			}
			else
			{
				num += valueForFaction;
			}
		}
		foreach (BarterItemVM rightOffer in RightOfferList)
		{
			int valueForFaction2 = rightOffer.Barterable.GetValueForFaction(otherFaction);
			if (valueForFaction2 < 0)
			{
				num2 += valueForFaction2;
			}
			else
			{
				num += valueForFaction2;
			}
		}
		double num3 = TaleWorlds.Library.MathF.Max(0f, num);
		double num4 = TaleWorlds.Library.MathF.Max(1f, -num2);
		ResultBarOtherPercentage = TaleWorlds.Library.MathF.Round(num3 / num4 * 100.0);
	}

	private void ExecuteTransferAllGoldLeft()
	{
	}

	private void ExecuteTransferAllGoldRight()
	{
	}

	public void ExecuteOffer()
	{
		Campaign.Current.BarterManager.ApplyAndFinalizePlayerBarter(_barterData.OffererHero, _barterData.OtherHero, _barterData);
	}

	public void ExecuteCancel()
	{
		Campaign.Current.BarterManager.CancelAndFinalizePlayerBarter(_barterData.OffererHero, _barterData.OtherHero, _barterData);
	}

	public void ExecuteReset()
	{
		LeftFiefList.Clear();
		RightFiefList.Clear();
		LeftPrisonerList.Clear();
		RightPrisonerList.Clear();
		LeftItemList.Clear();
		RightItemList.Clear();
		LeftOtherList.Clear();
		RightOtherList.Clear();
		LeftDiplomaticList.Clear();
		RightDiplomaticList.Clear();
		LeftGoldList.Clear();
		RightGoldList.Clear();
		_leftList.Clear();
		_rightList.Clear();
		_barterList.Clear();
		LeftOfferList.Clear();
		RightOfferList.Clear();
		_offerList.Clear();
		foreach (Barterable barterable in _barterData.GetBarterables())
		{
			if (barterable.IsOffered)
			{
				ChangeBarterableIsOffered(barterable, newState: false);
			}
		}
		InitBarterList(_barterData);
		SendOffer();
		InitializationIsOver = true;
		RefreshValues();
	}

	private void TransferItem(BarterItemVM item, bool offerAll)
	{
		ChangeBarterableIsOffered(item.Barterable, !item.IsOffered);
		if (offerAll)
		{
			item.CurrentOfferedAmount = item.TotalItemCount;
		}
		SendOffer();
		RefreshOfferLabel();
		RefreshCompatibility(item, item.IsOffered);
	}

	private void OfferItemAdd(BarterItemVM barterItemVM, int count)
	{
		ChangeBarterableIsOffered(barterItemVM.Barterable, newState: true);
		barterItemVM.CurrentOfferedAmount = (int)TaleWorlds.Library.MathF.Clamp(barterItemVM.CurrentOfferedAmount + count, 0f, barterItemVM.TotalItemCount);
		SendOffer();
		RefreshOfferLabel();
		RefreshCompatibility(barterItemVM, barterItemVM.IsOffered);
	}

	private void OfferItemRemove(BarterItemVM barterItemVM, int count)
	{
		if (barterItemVM.CurrentOfferedAmount <= count)
		{
			ChangeBarterableIsOffered(barterItemVM.Barterable, newState: false);
		}
		else
		{
			barterItemVM.CurrentOfferedAmount = (int)TaleWorlds.Library.MathF.Clamp(barterItemVM.CurrentOfferedAmount - count, 0f, barterItemVM.TotalItemCount);
		}
		SendOffer();
		RefreshOfferLabel();
		RefreshCompatibility(barterItemVM, barterItemVM.IsOffered);
	}

	public void OnTransferItem(Barterable barter, bool isTransferrable)
	{
		int index = ((barter.OriginalOwner == Hero.MainHero) ? 1 : 0);
		if (_barterList.IsEmpty())
		{
			return;
		}
		BarterItemVM barterItemVM = _barterList[index][barter.Group].FirstOrDefault((BarterItemVM i) => i.Barterable == barter);
		if (barterItemVM == null && !_offerList.IsEmpty())
		{
			barterItemVM = _offerList[index].FirstOrDefault((BarterItemVM i) => i.Barterable == barter);
		}
		if (barterItemVM == null)
		{
			return;
		}
		barterItemVM.IsOffered = barter.IsOffered;
		barterItemVM.IsItemTransferrable = isTransferrable;
		if (barterItemVM.IsOffered)
		{
			_offerList[index].Add(barterItemVM);
			if (barterItemVM.IsMultiple)
			{
				barterItemVM.CurrentOfferedAmount = 1;
			}
		}
		else
		{
			_offerList[index].Remove(barterItemVM);
			if (barterItemVM.IsMultiple)
			{
				barterItemVM.CurrentOfferedAmount = 1;
			}
		}
	}

	private void OnOfferedAmountChange()
	{
		SendOffer();
	}

	private void RefreshOfferLabel()
	{
		if (LeftOfferList.Any((BarterItemVM x) => x.Barterable.GetValueForFaction(OtherFaction) < 0) || RightOfferList.Any((BarterItemVM x) => x.Barterable.GetValueForFaction(OtherFaction) < 0))
		{
			OfferLbl = GameTexts.FindText("str_offer").ToString();
		}
		else
		{
			OfferLbl = GameTexts.FindText("str_gift").ToString();
		}
	}

	private void RefreshCompatibility(BarterItemVM lastTransferredItem, bool gotOffered)
	{
		foreach (MBBindingList<BarterItemVM> value in _leftList.Values)
		{
			value.ToList().ForEach(delegate(BarterItemVM b)
			{
				b.RefreshCompabilityWithItem(lastTransferredItem, gotOffered);
			});
		}
		foreach (MBBindingList<BarterItemVM> value2 in _rightList.Values)
		{
			value2.ToList().ForEach(delegate(BarterItemVM b)
			{
				b.RefreshCompabilityWithItem(lastTransferredItem, gotOffered);
			});
		}
	}

	public void SetResetInputKey(HotKey hotkey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void InitializeStaticContent()
	{
		FiefLbl = GameTexts.FindText("str_fiefs").ToString();
		PrisonerLbl = GameTexts.FindText("str_prisoner_tag_name").ToString();
		ItemLbl = GameTexts.FindText("str_item_tag_name").ToString();
		OtherLbl = GameTexts.FindText("str_other").ToString();
		CancelLbl = GameTexts.FindText("str_cancel").ToString();
		ResetLbl = GameTexts.FindText("str_reset").ToString();
		DiplomaticLbl = GameTexts.FindText("str_diplomatic_group").ToString();
		AutoBalanceHint.HintText = new TextObject("{=Ve5jkJqf}Auto Offer");
	}
}
