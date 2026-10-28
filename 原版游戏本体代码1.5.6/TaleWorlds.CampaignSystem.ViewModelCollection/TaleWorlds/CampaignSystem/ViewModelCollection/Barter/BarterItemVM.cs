using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Barter;

public class BarterItemVM : EncyclopediaLinkVM
{
	public delegate void BarterTransferEventDelegate(BarterItemVM itemVM, bool transferAll);

	public static bool IsEntireStackModifierActive;

	public static bool IsFiveStackModifierActive;

	private readonly BarterTransferEventDelegate _onTransfer;

	private readonly Action _onAmountChange;

	private bool _isFixed;

	private List<Barterable> _incompatibleItems = new List<Barterable>();

	public Barterable Barterable;

	public bool _isOffered;

	private bool _isItemTransferrable = true;

	private string _itemLbl;

	private string _fiefFileName;

	private string _barterableType = "NULL";

	private string _currentOfferedAmountText;

	private ImageIdentifierVM _visualIdentifier;

	private bool _isSelectorActive;

	private bool _hasVisualIdentifier;

	private bool _isMultiple;

	private int _totalItemCount;

	private string _totalItemCountText;

	private int _currentOfferedAmount;

	[DataSourceProperty]
	public int TotalItemCount
	{
		get
		{
			return _totalItemCount;
		}
		set
		{
			if (_totalItemCount != value)
			{
				_totalItemCount = value;
				OnPropertyChangedWithValue(value, "TotalItemCount");
				TotalItemCountText = CampaignUIHelper.GetAbbreviatedValueTextFromValue(value);
			}
		}
	}

	[DataSourceProperty]
	public string TotalItemCountText
	{
		get
		{
			return _totalItemCountText;
		}
		set
		{
			if (_totalItemCountText != value)
			{
				_totalItemCountText = value;
				OnPropertyChangedWithValue(value, "TotalItemCountText");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentOfferedAmount
	{
		get
		{
			return _currentOfferedAmount;
		}
		set
		{
			if (_currentOfferedAmount != value)
			{
				Barterable.CurrentAmount = value;
				_onAmountChange?.Invoke();
				_currentOfferedAmount = value;
				OnPropertyChangedWithValue(value, "CurrentOfferedAmount");
				CurrentOfferedAmountText = CampaignUIHelper.GetAbbreviatedValueTextFromValue(value);
			}
		}
	}

	[DataSourceProperty]
	public string CurrentOfferedAmountText
	{
		get
		{
			return _currentOfferedAmountText;
		}
		set
		{
			if (_currentOfferedAmountText != value)
			{
				_currentOfferedAmountText = value;
				OnPropertyChangedWithValue(value, "CurrentOfferedAmountText");
			}
		}
	}

	[DataSourceProperty]
	public string BarterableType
	{
		get
		{
			return _barterableType;
		}
		set
		{
			if (_barterableType != value)
			{
				_barterableType = value;
				OnPropertyChangedWithValue(value, "BarterableType");
			}
		}
	}

	[DataSourceProperty]
	public bool HasVisualIdentifier
	{
		get
		{
			return _hasVisualIdentifier;
		}
		set
		{
			if (_hasVisualIdentifier != value)
			{
				_hasVisualIdentifier = value;
				OnPropertyChangedWithValue(value, "HasVisualIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMultiple
	{
		get
		{
			return _isMultiple;
		}
		set
		{
			if (_isMultiple != value)
			{
				_isMultiple = value;
				OnPropertyChangedWithValue(value, "IsMultiple");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelectorActive
	{
		get
		{
			return _isSelectorActive;
		}
		set
		{
			if (_isSelectorActive != value)
			{
				_isSelectorActive = value;
				OnPropertyChangedWithValue(value, "IsSelectorActive");
			}
		}
	}

	[DataSourceProperty]
	public ImageIdentifierVM VisualIdentifier
	{
		get
		{
			return _visualIdentifier;
		}
		set
		{
			if (_visualIdentifier != value)
			{
				_visualIdentifier = value;
				OnPropertyChangedWithValue(value, "VisualIdentifier");
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
			_itemLbl = value;
			OnPropertyChangedWithValue(value, "ItemLbl");
		}
	}

	[DataSourceProperty]
	public string FiefFileName
	{
		get
		{
			return _fiefFileName;
		}
		set
		{
			_fiefFileName = value;
			OnPropertyChangedWithValue(value, "FiefFileName");
		}
	}

	[DataSourceProperty]
	public bool IsItemTransferrable
	{
		get
		{
			return _isItemTransferrable;
		}
		set
		{
			if (_isFixed)
			{
				value = false;
			}
			if (_isItemTransferrable != value)
			{
				_isItemTransferrable = value;
				OnPropertyChangedWithValue(value, "IsItemTransferrable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOffered
	{
		get
		{
			return _isOffered;
		}
		set
		{
			if (value != _isOffered)
			{
				_isOffered = value;
				OnPropertyChangedWithValue(value, "IsOffered");
			}
		}
	}

	public BarterItemVM(Barterable barterable, BarterTransferEventDelegate OnTransfer, Action onAmountChange, bool isFixed = false)
	{
		Barterable = barterable;
		base.ActiveLink = barterable.GetEncyclopediaLink();
		_onTransfer = OnTransfer;
		_onAmountChange = onAmountChange;
		_isFixed = isFixed;
		IsItemTransferrable = !isFixed;
		BarterableType = Barterable.StringID;
		ImageIdentifier visualIdentifier = Barterable.GetVisualIdentifier();
		HasVisualIdentifier = visualIdentifier != null;
		if (visualIdentifier != null)
		{
			VisualIdentifier = new GenericImageIdentifierVM(visualIdentifier);
		}
		else
		{
			VisualIdentifier = null;
			if (Barterable is FiefBarterable fiefBarterable)
			{
				FiefFileName = fiefBarterable.TargetSettlement.SettlementComponent.BackgroundMeshName;
			}
		}
		TotalItemCount = Barterable.MaxAmount;
		CurrentOfferedAmount = 1;
		IsMultiple = TotalItemCount > 1;
		IsOffered = Barterable.IsOffered;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ItemLbl = Barterable.Name.ToString();
	}

	public void RefreshCompabilityWithItem(BarterItemVM item, bool isItemGotOffered)
	{
		if (isItemGotOffered && !item.Barterable.IsCompatible(Barterable))
		{
			_incompatibleItems.Add(item.Barterable);
		}
		else if (!isItemGotOffered && _incompatibleItems.Contains(item.Barterable))
		{
			_incompatibleItems.Remove(item.Barterable);
		}
		IsItemTransferrable = _incompatibleItems.Count <= 0;
	}

	public void ExecuteAddOffered()
	{
		int num = (IsEntireStackModifierActive ? TotalItemCount : (CurrentOfferedAmount + ((!IsFiveStackModifierActive) ? 1 : 5)));
		CurrentOfferedAmount = ((num < TotalItemCount) ? num : TotalItemCount);
	}

	public void ExecuteRemoveOffered()
	{
		int num = (IsEntireStackModifierActive ? 1 : (CurrentOfferedAmount - ((!IsFiveStackModifierActive) ? 1 : 5)));
		CurrentOfferedAmount = ((num <= 1) ? 1 : num);
	}

	public void ExecuteAction()
	{
		if (IsItemTransferrable)
		{
			_onTransfer(this, transferAll: false);
		}
	}
}
