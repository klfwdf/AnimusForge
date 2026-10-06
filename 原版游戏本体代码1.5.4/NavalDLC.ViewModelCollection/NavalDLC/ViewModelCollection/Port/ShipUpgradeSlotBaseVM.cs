using System;
using System.Collections.Generic;
using NavalDLC.ViewModelCollection.Port.PortScreenHandlers;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port;

public class ShipUpgradeSlotBaseVM : ViewModel
{
	public delegate void ShipPieceSelectedDelegate(Ship ship, string shipSlotTag, string slotTypeId, ShipUpgradePieceBaseVM pieceVM);

	protected class UpgradePieceComparer : IComparer<ShipUpgradePieceBaseVM>
	{
		public int Compare(ShipUpgradePieceBaseVM x, ShipUpgradePieceBaseVM y)
		{
			int num = x.UpgradePieceTier.CompareTo(y.UpgradePieceTier);
			if (num != 0)
			{
				return num;
			}
			return ResolveEquality(x, y);
		}

		private int ResolveEquality(ShipUpgradePieceBaseVM x, ShipUpgradePieceBaseVM y)
		{
			return x.Name.CompareTo(y.Name);
		}
	}

	public readonly Ship Ship;

	public readonly string ShipSlotTag;

	private readonly TextObject _nameText;

	private TextObject _actionInfoTooltip;

	private readonly Action<ShipUpgradeSlotBaseVM> _onSelected;

	private bool _canTradeUpgrades;

	private bool _isChanged;

	private bool _isSelected;

	private bool _hasSelectedPiece;

	private bool _isEmpty = true;

	private bool _isUnexamined;

	private HintViewModel _clearSlotHint;

	private string _slotName;

	private string _slotTypeId;

	private HintViewModel _slotHint;

	private ShipUpgradePieceBaseVM _selectedPiece;

	private MBBindingList<ShipUpgradePieceBaseVM> _availablePieces;

	private bool _anyBetterPiecesAvailable;

	[DataSourceProperty]
	public bool CanTradeUpgrades
	{
		get
		{
			return _canTradeUpgrades;
		}
		set
		{
			if (value != _canTradeUpgrades)
			{
				_canTradeUpgrades = value;
				OnPropertyChangedWithValue(value, "CanTradeUpgrades");
			}
		}
	}

	[DataSourceProperty]
	public bool IsChanged
	{
		get
		{
			return _isChanged;
		}
		set
		{
			if (value != _isChanged)
			{
				_isChanged = value;
				OnPropertyChangedWithValue(value, "IsChanged");
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
	public bool HasSelectedPiece
	{
		get
		{
			return _hasSelectedPiece;
		}
		set
		{
			if (value != _hasSelectedPiece)
			{
				_hasSelectedPiece = value;
				OnPropertyChangedWithValue(value, "HasSelectedPiece");
				IsEmpty = !_hasSelectedPiece;
			}
		}
	}

	[DataSourceProperty]
	public bool IsEmpty
	{
		get
		{
			return _isEmpty;
		}
		set
		{
			if (value != _isEmpty)
			{
				_isEmpty = value;
				OnPropertyChangedWithValue(value, "IsEmpty");
				HasSelectedPiece = !_isEmpty;
			}
		}
	}

	[DataSourceProperty]
	public bool IsUnexamined
	{
		get
		{
			return _isUnexamined;
		}
		set
		{
			if (value != _isUnexamined)
			{
				_isUnexamined = value;
				OnPropertyChangedWithValue(value, "IsUnexamined");
			}
		}
	}

	[DataSourceProperty]
	public string SlotName
	{
		get
		{
			return _slotName;
		}
		set
		{
			if (value != _slotName)
			{
				_slotName = value;
				OnPropertyChangedWithValue(value, "SlotName");
			}
		}
	}

	[DataSourceProperty]
	public string SlotTypeId
	{
		get
		{
			return _slotTypeId;
		}
		set
		{
			if (value != _slotTypeId)
			{
				_slotTypeId = value;
				OnPropertyChangedWithValue(value, "SlotTypeId");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ClearSlotHint
	{
		get
		{
			return _clearSlotHint;
		}
		set
		{
			if (value != _clearSlotHint)
			{
				_clearSlotHint = value;
				OnPropertyChangedWithValue(value, "ClearSlotHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel SlotHint
	{
		get
		{
			return _slotHint;
		}
		set
		{
			if (value != _slotHint)
			{
				_slotHint = value;
				OnPropertyChangedWithValue(value, "SlotHint");
			}
		}
	}

	[DataSourceProperty]
	public ShipUpgradePieceBaseVM SelectedPiece
	{
		get
		{
			return _selectedPiece;
		}
		set
		{
			if (value != _selectedPiece)
			{
				if (_selectedPiece != null)
				{
					_selectedPiece.IsSelected = false;
				}
				_selectedPiece = value;
				OnPropertyChangedWithValue(value, "SelectedPiece");
				if (_selectedPiece != null)
				{
					_selectedPiece.IsSelected = true;
				}
				HasSelectedPiece = _selectedPiece != null;
				IsChanged = GetIsChanged();
				UpdateAnyBetterPiecesAvailable();
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ShipUpgradePieceBaseVM> AvailablePieces
	{
		get
		{
			return _availablePieces;
		}
		set
		{
			if (value != _availablePieces)
			{
				_availablePieces = value;
				OnPropertyChangedWithValue(value, "AvailablePieces");
			}
		}
	}

	[DataSourceProperty]
	public bool AnyBetterPiecesAvailable
	{
		get
		{
			return _anyBetterPiecesAvailable;
		}
		set
		{
			if (value != _anyBetterPiecesAvailable)
			{
				_anyBetterPiecesAvailable = value;
				OnPropertyChangedWithValue(value, "AnyBetterPiecesAvailable");
			}
		}
	}

	public static event ShipPieceSelectedDelegate OnShipPieceSelected;

	public ShipUpgradeSlotBaseVM(Ship ship, TextObject slotName, string shipSlotTag, string slotTypeId, Action<ShipUpgradeSlotBaseVM> onSelected)
	{
		_onSelected = onSelected;
		Ship = ship;
		ShipSlotTag = shipSlotTag;
		SlotTypeId = slotTypeId;
		_nameText = slotName;
		AvailablePieces = new MBBindingList<ShipUpgradePieceBaseVM>();
		SlotHint = new HintViewModel();
		ClearSlotHint = new HintViewModel(new TextObject("{=pJgyBSz7}Clear Slot"));
		IsChanged = false;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		AvailablePieces.ApplyActionOnAllItems(delegate(ShipUpgradePieceBaseVM p)
		{
			p.RefreshValues();
		});
		SlotName = _nameText.ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		AvailablePieces.ApplyActionOnAllItems(delegate(ShipUpgradePieceBaseVM p)
		{
			p.OnFinalize();
		});
		if (SelectedPiece != null && !AvailablePieces.Contains(SelectedPiece))
		{
			SelectedPiece.OnFinalize();
		}
	}

	public void Update()
	{
		bool isUnexamined = false;
		for (int i = 0; i < AvailablePieces.Count; i++)
		{
			AvailablePieces[i].Update();
			if (AvailablePieces[i].IsUnexamined)
			{
				isUnexamined = true;
			}
		}
		SelectedPiece?.Update();
		ShipUpgradePieceBaseVM selectedPiece = SelectedPiece;
		if (selectedPiece != null && selectedPiece.IsUnexamined)
		{
			isUnexamined = true;
		}
		IsUnexamined = isUnexamined;
	}

	public virtual void ResetPieces()
	{
	}

	public void UpdateEnabledStatus(in PortActionInfo actionInfo)
	{
		CanTradeUpgrades = actionInfo.IsEnabled;
		_actionInfoTooltip = actionInfo.Tooltip;
		SlotHint.HintText = ((SelectedPiece == null) ? _actionInfoTooltip : null);
		if (CanTradeUpgrades && AvailablePieces.Count == 0 && SelectedPiece == null)
		{
			CanTradeUpgrades = false;
			SlotHint.HintText = new TextObject("{=s96ObCLT}There are no available upgrades for this slot");
		}
		if (!CanTradeUpgrades && IsSelected)
		{
			ExecuteDeselect();
		}
		if (TextObject.IsNullOrEmpty(SlotHint.HintText) && SelectedPiece == null)
		{
			SlotHint.HintText = new TextObject("{=!}" + SlotName);
		}
	}

	protected virtual void OnPieceSelected(ShipUpgradePieceBaseVM piece)
	{
		SelectedPiece = piece;
		ShipUpgradeSlotBaseVM.OnShipPieceSelected?.Invoke(Ship, ShipSlotTag, SlotTypeId, SelectedPiece);
	}

	protected virtual bool GetIsChanged()
	{
		return false;
	}

	public void ExecuteClearSlot()
	{
		ExecuteInspectEnd();
		OnPieceSelected(null);
	}

	public void ExecuteSelect()
	{
		_onSelected?.Invoke(this);
	}

	public void ExecuteDeselect()
	{
		_onSelected?.Invoke(null);
	}

	public void ExecuteInspectBegin()
	{
		SelectedPiece?.InspectPiece(isInspectedFromSlot: true, _actionInfoTooltip);
	}

	public void ExecuteInspectEnd()
	{
		SelectedPiece?.ExecuteInspectEnd();
	}

	protected void UpdateAnyBetterPiecesAvailable()
	{
		int num = (int)((SelectedPiece != null) ? SelectedPiece.UpgradePieceTier : ((ShipUpgradePieceBaseVM.ShipUpgradePieceTier)(-1)));
		int num2 = -1;
		for (int i = 0; i < AvailablePieces.Count; i++)
		{
			ShipUpgradePieceBaseVM shipUpgradePieceBaseVM = AvailablePieces[i];
			if (!shipUpgradePieceBaseVM.IsDisabled)
			{
				num2 = TaleWorlds.Library.MathF.Max((int)shipUpgradePieceBaseVM.UpgradePieceTier, num2);
			}
		}
		AnyBetterPiecesAvailable = num2 > num;
	}
}
