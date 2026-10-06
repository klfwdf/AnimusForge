using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party;

public class PartyTradeVM : ViewModel
{
	private readonly PartyScreenLogic _partyScreenLogic;

	private readonly Action<int, bool> _onApplyTransaction;

	private readonly bool _isPrisoner;

	private TroopRosterElement _referenceTroopRoster;

	private readonly PartyScreenLogic.PartyRosterSide _side;

	private PartyScreenLogic.PartyRosterSide _otherSide;

	private bool _isTransfarable;

	private string _thisStockLbl;

	private string _totalStockLbl;

	private int _thisStock = -1;

	private int _initialThisStock;

	private int _otherStock;

	private int _initialOtherStock;

	private int _totalStock;

	private bool _isThisStockIncreasable;

	private bool _isOtherStockIncreasable;

	private HintViewModel _takeHint;

	private HintViewModel _giveHint;

	[DataSourceProperty]
	public bool IsTransfarable
	{
		get
		{
			return _isTransfarable;
		}
		set
		{
			if (value != _isTransfarable)
			{
				_isTransfarable = value;
				OnPropertyChangedWithValue(value, "IsTransfarable");
			}
		}
	}

	[DataSourceProperty]
	public string ThisStockLbl
	{
		get
		{
			return _thisStockLbl;
		}
		set
		{
			if (value != _thisStockLbl)
			{
				_thisStockLbl = value;
				OnPropertyChangedWithValue(value, "ThisStockLbl");
			}
		}
	}

	[DataSourceProperty]
	public string TotalStockLbl
	{
		get
		{
			return _totalStockLbl;
		}
		set
		{
			if (value != _totalStockLbl)
			{
				_totalStockLbl = value;
				OnPropertyChangedWithValue(value, "TotalStockLbl");
			}
		}
	}

	[DataSourceProperty]
	public int ThisStock
	{
		get
		{
			return _thisStock;
		}
		set
		{
			if (value != _thisStock)
			{
				_thisStock = value;
				OnPropertyChangedWithValue(value, "ThisStock");
				ThisStockUpdated();
			}
		}
	}

	[DataSourceProperty]
	public int InitialThisStock
	{
		get
		{
			return _initialThisStock;
		}
		set
		{
			if (value != _initialThisStock)
			{
				_initialThisStock = value;
				OnPropertyChangedWithValue(value, "InitialThisStock");
			}
		}
	}

	[DataSourceProperty]
	public int OtherStock
	{
		get
		{
			return _otherStock;
		}
		set
		{
			if (value != _otherStock)
			{
				_otherStock = value;
				OnPropertyChangedWithValue(value, "OtherStock");
			}
		}
	}

	[DataSourceProperty]
	public int InitialOtherStock
	{
		get
		{
			return _initialOtherStock;
		}
		set
		{
			if (value != _initialOtherStock)
			{
				_initialOtherStock = value;
				OnPropertyChangedWithValue(value, "InitialOtherStock");
			}
		}
	}

	[DataSourceProperty]
	public int TotalStock
	{
		get
		{
			return _totalStock;
		}
		set
		{
			if (value != _totalStock)
			{
				_totalStock = value;
				OnPropertyChangedWithValue(value, "TotalStock");
			}
		}
	}

	[DataSourceProperty]
	public bool IsThisStockIncreasable
	{
		get
		{
			return _isThisStockIncreasable;
		}
		set
		{
			if (value != _isThisStockIncreasable)
			{
				_isThisStockIncreasable = value;
				OnPropertyChangedWithValue(value, "IsThisStockIncreasable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOtherStockIncreasable
	{
		get
		{
			return _isOtherStockIncreasable;
		}
		set
		{
			if (value != _isOtherStockIncreasable)
			{
				_isOtherStockIncreasable = value;
				OnPropertyChangedWithValue(value, "IsOtherStockIncreasable");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel TakeHint
	{
		get
		{
			return _takeHint;
		}
		set
		{
			if (value != _takeHint)
			{
				_takeHint = value;
				OnPropertyChangedWithValue(value, "TakeHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel GiveHint
	{
		get
		{
			return _giveHint;
		}
		set
		{
			if (value != _giveHint)
			{
				_giveHint = value;
				OnPropertyChangedWithValue(value, "GiveHint");
			}
		}
	}

	public static event Action RemoveZeroCounts;

	public PartyTradeVM(PartyScreenLogic partyScreenLogic, TroopRosterElement troopRoster, PartyScreenLogic.PartyRosterSide side, bool isTransfarable, bool isPrisoner, Action<int, bool> onApplyTransaction)
	{
		_partyScreenLogic = partyScreenLogic;
		_referenceTroopRoster = troopRoster;
		_side = side;
		_onApplyTransaction = onApplyTransaction;
		_otherSide = ((side != PartyScreenLogic.PartyRosterSide.Right) ? PartyScreenLogic.PartyRosterSide.Right : PartyScreenLogic.PartyRosterSide.Left);
		IsTransfarable = isTransfarable;
		_isPrisoner = isPrisoner;
		TakeHint = new HintViewModel(GameTexts.FindText("str_take"));
		GiveHint = new HintViewModel(GameTexts.FindText("str_give"));
		UpdateTroopData(troopRoster, side);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ThisStockLbl = GameTexts.FindText("str_party_your_party").ToString();
		TotalStockLbl = GameTexts.FindText("str_party_total_units").ToString();
	}

	public void UpdateTroopData(TroopRosterElement troopRoster, PartyScreenLogic.PartyRosterSide side, bool forceUpdate = true)
	{
		if (side != PartyScreenLogic.PartyRosterSide.Left && side != PartyScreenLogic.PartyRosterSide.Right)
		{
			Debug.FailedAssert("Troop has to be either from left or right side", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Party\\PartyTradeVM.cs", "UpdateTroopData", 54);
			return;
		}
		TroopRosterElement? troopRosterElement = null;
		TroopRosterElement? troopRosterElement2 = null;
		troopRosterElement = troopRoster;
		troopRosterElement2 = FindTroopFromSide(troopRoster.Character, _otherSide, _isPrisoner);
		InitialThisStock = troopRosterElement?.Number ?? 0;
		InitialOtherStock = troopRosterElement2?.Number ?? 0;
		TotalStock = InitialThisStock + InitialOtherStock;
		ThisStock = InitialThisStock;
		OtherStock = InitialOtherStock;
		if (forceUpdate)
		{
			ThisStockUpdated();
		}
	}

	public TroopRosterElement? FindTroopFromSide(CharacterObject character, PartyScreenLogic.PartyRosterSide side, bool isPrisoner)
	{
		TroopRosterElement? result = null;
		TroopRoster[] array = (isPrisoner ? _partyScreenLogic.PrisonerRosters : _partyScreenLogic.MemberRosters);
		int num = array[(uint)side].FindIndexOfTroop(character);
		if (num >= 0)
		{
			result = array[(uint)side].GetElementCopyAtIndex(num);
		}
		return result;
	}

	private void ThisStockUpdated()
	{
		ExecuteApplyTransaction();
		OtherStock = TotalStock - ThisStock;
		IsThisStockIncreasable = OtherStock > 0;
		IsOtherStockIncreasable = OtherStock < TotalStock && IsTransfarable;
	}

	public void ExecuteIncreasePlayerStock()
	{
		if (OtherStock > 0)
		{
			ThisStock++;
		}
	}

	public void ExecuteIncreaseOtherStock()
	{
		if (OtherStock < TotalStock)
		{
			ThisStock--;
		}
	}

	public void ExecuteReset()
	{
		OtherStock = InitialOtherStock;
	}

	public void ExecuteApplyTransaction()
	{
		int num = ThisStock - InitialThisStock;
		bool arg = (num >= 0 && _side == PartyScreenLogic.PartyRosterSide.Right) || (num <= 0 && _side == PartyScreenLogic.PartyRosterSide.Left);
		if (num != 0 && _onApplyTransaction != null)
		{
			if (num < 0)
			{
				_ = _otherSide;
			}
			else
			{
				_ = _side;
			}
			int arg2 = TaleWorlds.Library.MathF.Abs(num);
			_onApplyTransaction(arg2, arg);
		}
	}

	public void ExecuteRemoveZeroCounts()
	{
		PartyTradeVM.RemoveZeroCounts?.Invoke();
	}
}
