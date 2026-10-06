using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.HotKeyCategories;
using NavalDLC.ViewModelCollection.Port.PortScreenHandlers;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port;

public class PortVM : ViewModel
{
	private readonly PortScreenHandler _portScreenHandler;

	private readonly PortScreenModes _portScreenMode;

	private readonly Action<Ship> _onShipSelected;

	private readonly Action _onRostersRefreshed;

	private readonly Action<ShipItemVM> _refreshShipVisual;

	private readonly Action _onUpgradeSlotSelected;

	private PortShipStashPopupVM _shipSelectionPopup;

	private readonly bool _isAtPortSettlement;

	private readonly Settlement _portSettlement;

	private readonly MBList<ShipItemVM> _allShips;

	private List<PortChangeInfo> _cachedChanges;

	private PortActionVM _buyAction;

	private PortActionVM _sellAction;

	private PortActionVM _repairAction;

	private PortActionVM _sendToClanAction;

	private PortActionVM _repairAllAction;

	private PortActionVM _stashShipAction;

	private PortActionVM _viewStashedShipsAction;

	private PortActionVM _retrieveStashedShipsAction;

	private bool _isConfirmDisabled;

	private bool _canUseKeyboardInputs;

	private bool _canUseGamepadInputs;

	private bool _isControllingCamera;

	private bool _showPortScreenGamepadInputs;

	private bool _canToggleCamera = true;

	private bool _isMapBarExtended;

	private bool _isAnyUpgradeSlotSelected;

	private bool _isNight;

	private int _totalGoldCost;

	private string _keyboardMoveCameraText;

	private string _cancelText;

	private string _confirmText;

	private string _totalGoldCostText;

	private string _repairText;

	private string _upgradeText;

	private string _buyText;

	private string _sellText;

	private HintViewModel _canConfirmHint;

	private BasicTooltipViewModel _goldCostHint;

	private ShipRosterVM _leftRoster;

	private ShipRosterVM _rightRoster;

	private ShipItemVM _selectedShip;

	private ShipUpgradePieceBaseVM _inspectedUpgrade;

	private ShipUpgradeSlotBaseVM _selectedUpgradeSlot;

	private InputKeyItemVM _resetInputKey;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _selectPreviousShipInputKey;

	private InputKeyItemVM _selectNextShipInputKey;

	private InputKeyItemVM _selectLeftRosterInputKey;

	private InputKeyItemVM _selectRightRosterInputKey;

	private InputKeyItemVM _gamepadToggleCameraInputKey;

	private MBBindingList<InputKeyItemVM> _gamepadCameraControlKeys;

	private InputKeyItemVM _keyboardRotateCameraInputKey;

	private MBBindingList<InputKeyItemVM> _keyboardMoveCameraInputKeys;

	public MBReadOnlyList<ShipItemVM> AllShips => _allShips;

	[DataSourceProperty]
	public PortActionVM BuyAction
	{
		get
		{
			return _buyAction;
		}
		set
		{
			if (value != _buyAction)
			{
				_buyAction = value;
				OnPropertyChangedWithValue(value, "BuyAction");
			}
		}
	}

	[DataSourceProperty]
	public PortActionVM SellAction
	{
		get
		{
			return _sellAction;
		}
		set
		{
			if (value != _sellAction)
			{
				_sellAction = value;
				OnPropertyChangedWithValue(value, "SellAction");
			}
		}
	}

	[DataSourceProperty]
	public PortActionVM RepairAction
	{
		get
		{
			return _repairAction;
		}
		set
		{
			if (value != _repairAction)
			{
				_repairAction = value;
				OnPropertyChangedWithValue(value, "RepairAction");
			}
		}
	}

	[DataSourceProperty]
	public bool IsConfirmDisabled
	{
		get
		{
			return _isConfirmDisabled;
		}
		set
		{
			if (value != _isConfirmDisabled)
			{
				_isConfirmDisabled = value;
				OnPropertyChangedWithValue(value, "IsConfirmDisabled");
			}
		}
	}

	[DataSourceProperty]
	public PortActionVM SendToClanAction
	{
		get
		{
			return _sendToClanAction;
		}
		set
		{
			if (value != _sendToClanAction)
			{
				_sendToClanAction = value;
				OnPropertyChangedWithValue(value, "SendToClanAction");
			}
		}
	}

	[DataSourceProperty]
	public PortActionVM RepairAllAction
	{
		get
		{
			return _repairAllAction;
		}
		set
		{
			if (value != _repairAllAction)
			{
				_repairAllAction = value;
				OnPropertyChangedWithValue(value, "RepairAllAction");
			}
		}
	}

	[DataSourceProperty]
	public PortActionVM StashShipAction
	{
		get
		{
			return _stashShipAction;
		}
		set
		{
			if (value != _stashShipAction)
			{
				_stashShipAction = value;
				OnPropertyChangedWithValue(value, "StashShipAction");
			}
		}
	}

	[DataSourceProperty]
	public PortActionVM ViewStashedShipsAction
	{
		get
		{
			return _viewStashedShipsAction;
		}
		set
		{
			if (value != _viewStashedShipsAction)
			{
				_viewStashedShipsAction = value;
				OnPropertyChangedWithValue(value, "ViewStashedShipsAction");
			}
		}
	}

	[DataSourceProperty]
	public PortActionVM RetrieveStashedShipsAction
	{
		get
		{
			return _retrieveStashedShipsAction;
		}
		set
		{
			if (value != _retrieveStashedShipsAction)
			{
				_retrieveStashedShipsAction = value;
				OnPropertyChangedWithValue(value, "RetrieveStashedShipsAction");
			}
		}
	}

	[DataSourceProperty]
	public PortShipStashPopupVM ShipSelectionPopup
	{
		get
		{
			return _shipSelectionPopup;
		}
		set
		{
			if (value != _shipSelectionPopup)
			{
				_shipSelectionPopup = value;
				OnPropertyChangedWithValue(value, "ShipSelectionPopup");
			}
		}
	}

	[DataSourceProperty]
	public bool CanUseKeyboardInputs
	{
		get
		{
			return _canUseKeyboardInputs;
		}
		set
		{
			if (value != _canUseKeyboardInputs)
			{
				_canUseKeyboardInputs = value;
				OnPropertyChangedWithValue(value, "CanUseKeyboardInputs");
			}
		}
	}

	[DataSourceProperty]
	public bool CanUseGamepadInputs
	{
		get
		{
			return _canUseGamepadInputs;
		}
		set
		{
			if (value != _canUseGamepadInputs)
			{
				_canUseGamepadInputs = value;
				OnPropertyChangedWithValue(value, "CanUseGamepadInputs");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowPortScreenGamepadInputs
	{
		get
		{
			return _showPortScreenGamepadInputs;
		}
		set
		{
			if (value != _showPortScreenGamepadInputs)
			{
				_showPortScreenGamepadInputs = value;
				OnPropertyChangedWithValue(value, "ShowPortScreenGamepadInputs");
			}
		}
	}

	[DataSourceProperty]
	public bool IsControllingCamera
	{
		get
		{
			return _isControllingCamera;
		}
		set
		{
			if (value != _isControllingCamera)
			{
				_isControllingCamera = value;
				OnPropertyChangedWithValue(value, "IsControllingCamera");
				UpdateGamepadCameraControlButtonsVisibility();
			}
		}
	}

	[DataSourceProperty]
	public bool CanToggleCamera
	{
		get
		{
			return _canToggleCamera;
		}
		set
		{
			if (value != _canToggleCamera)
			{
				_canToggleCamera = value;
				OnPropertyChangedWithValue(value, "CanToggleCamera");
				UpdateGamepadCameraControlButtonsVisibility();
			}
		}
	}

	[DataSourceProperty]
	public bool IsMapBarExtended
	{
		get
		{
			return _isMapBarExtended;
		}
		set
		{
			if (value != _isMapBarExtended)
			{
				_isMapBarExtended = value;
				OnPropertyChangedWithValue(value, "IsMapBarExtended");
			}
		}
	}

	[DataSourceProperty]
	public string KeyboardMoveCameraText
	{
		get
		{
			return _keyboardMoveCameraText;
		}
		set
		{
			if (value != _keyboardMoveCameraText)
			{
				_keyboardMoveCameraText = value;
				OnPropertyChangedWithValue(value, "KeyboardMoveCameraText");
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
	public string ConfirmText
	{
		get
		{
			return _confirmText;
		}
		set
		{
			if (value != _confirmText)
			{
				_confirmText = value;
				OnPropertyChangedWithValue(value, "ConfirmText");
			}
		}
	}

	[DataSourceProperty]
	public int TotalGoldCost
	{
		get
		{
			return _totalGoldCost;
		}
		set
		{
			if (value != _totalGoldCost)
			{
				_totalGoldCost = value;
				OnPropertyChangedWithValue(value, "TotalGoldCost");
			}
		}
	}

	[DataSourceProperty]
	public string TotalGoldCostText
	{
		get
		{
			return _totalGoldCostText;
		}
		set
		{
			if (value != _totalGoldCostText)
			{
				_totalGoldCostText = value;
				OnPropertyChangedWithValue(value, "TotalGoldCostText");
			}
		}
	}

	[DataSourceProperty]
	public string RepairText
	{
		get
		{
			return _repairText;
		}
		set
		{
			if (value != _repairText)
			{
				_repairText = value;
				OnPropertyChangedWithValue(value, "RepairText");
			}
		}
	}

	[DataSourceProperty]
	public string UpgradeText
	{
		get
		{
			return _upgradeText;
		}
		set
		{
			if (value != _upgradeText)
			{
				_upgradeText = value;
				OnPropertyChangedWithValue(value, "UpgradeText");
			}
		}
	}

	[DataSourceProperty]
	public string BuyText
	{
		get
		{
			return _buyText;
		}
		set
		{
			if (value != _buyText)
			{
				_buyText = value;
				OnPropertyChangedWithValue(value, "BuyText");
			}
		}
	}

	[DataSourceProperty]
	public string SellText
	{
		get
		{
			return _sellText;
		}
		set
		{
			if (value != _sellText)
			{
				_sellText = value;
				OnPropertyChangedWithValue(value, "SellText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAnyUpgradeSlotSelected
	{
		get
		{
			return _isAnyUpgradeSlotSelected;
		}
		set
		{
			if (value != _isAnyUpgradeSlotSelected)
			{
				_isAnyUpgradeSlotSelected = value;
				OnPropertyChangedWithValue(value, "IsAnyUpgradeSlotSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNight
	{
		get
		{
			return _isNight;
		}
		set
		{
			if (value == _isNight)
			{
				return;
			}
			_isNight = value;
			OnPropertyChangedWithValue(value, "IsNight");
			foreach (ShipItemVM allShip in AllShips)
			{
				allShip.IsNight = value;
			}
		}
	}

	[DataSourceProperty]
	public ShipRosterVM LeftRoster
	{
		get
		{
			return _leftRoster;
		}
		set
		{
			if (value != _leftRoster)
			{
				_leftRoster = value;
				OnPropertyChangedWithValue(value, "LeftRoster");
			}
		}
	}

	[DataSourceProperty]
	public ShipRosterVM RightRoster
	{
		get
		{
			return _rightRoster;
		}
		set
		{
			if (value != _rightRoster)
			{
				_rightRoster = value;
				OnPropertyChangedWithValue(value, "RightRoster");
			}
		}
	}

	[DataSourceProperty]
	public ShipItemVM SelectedShip
	{
		get
		{
			return _selectedShip;
		}
		set
		{
			if (value != _selectedShip)
			{
				if (_selectedShip != null)
				{
					_selectedShip.IsSelected = false;
				}
				_selectedShip = value;
				OnPropertyChangedWithValue(value, "SelectedShip");
				if (_selectedShip != null)
				{
					_selectedShip.IsSelected = true;
				}
			}
		}
	}

	[DataSourceProperty]
	public ShipUpgradeSlotBaseVM SelectedUpgradeSlot
	{
		get
		{
			return _selectedUpgradeSlot;
		}
		set
		{
			if (value != _selectedUpgradeSlot)
			{
				_selectedUpgradeSlot = value;
				OnPropertyChangedWithValue(value, "SelectedUpgradeSlot");
				IsAnyUpgradeSlotSelected = _selectedUpgradeSlot != null;
			}
		}
	}

	[DataSourceProperty]
	public ShipUpgradePieceBaseVM InspectedUpgrade
	{
		get
		{
			return _inspectedUpgrade;
		}
		set
		{
			if (value != _inspectedUpgrade)
			{
				_inspectedUpgrade = value;
				OnPropertyChangedWithValue(value, "InspectedUpgrade");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CanConfirmHint
	{
		get
		{
			return _canConfirmHint;
		}
		set
		{
			if (value != _canConfirmHint)
			{
				_canConfirmHint = value;
				OnPropertyChangedWithValue(value, "CanConfirmHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel GoldCostHint
	{
		get
		{
			return _goldCostHint;
		}
		set
		{
			if (value != _goldCostHint)
			{
				_goldCostHint = value;
				OnPropertyChangedWithValue(value, "GoldCostHint");
			}
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
	public InputKeyItemVM SelectPreviousShipInputKey
	{
		get
		{
			return _selectPreviousShipInputKey;
		}
		set
		{
			if (value != _selectPreviousShipInputKey)
			{
				_selectPreviousShipInputKey = value;
				OnPropertyChangedWithValue(value, "SelectPreviousShipInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM SelectNextShipInputKey
	{
		get
		{
			return _selectNextShipInputKey;
		}
		set
		{
			if (value != _selectNextShipInputKey)
			{
				_selectNextShipInputKey = value;
				OnPropertyChangedWithValue(value, "SelectNextShipInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM SelectLeftRosterInputKey
	{
		get
		{
			return _selectLeftRosterInputKey;
		}
		set
		{
			if (value != _selectLeftRosterInputKey)
			{
				_selectLeftRosterInputKey = value;
				OnPropertyChangedWithValue(value, "SelectLeftRosterInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM SelectRightRosterInputKey
	{
		get
		{
			return _selectRightRosterInputKey;
		}
		set
		{
			if (value != _selectRightRosterInputKey)
			{
				_selectRightRosterInputKey = value;
				OnPropertyChangedWithValue(value, "SelectRightRosterInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM GamepadToggleCameraInputKey
	{
		get
		{
			return _gamepadToggleCameraInputKey;
		}
		set
		{
			if (value != _gamepadToggleCameraInputKey)
			{
				_gamepadToggleCameraInputKey = value;
				OnPropertyChangedWithValue(value, "GamepadToggleCameraInputKey");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<InputKeyItemVM> GamepadCameraControlKeys
	{
		get
		{
			return _gamepadCameraControlKeys;
		}
		set
		{
			if (value != _gamepadCameraControlKeys)
			{
				_gamepadCameraControlKeys = value;
				OnPropertyChangedWithValue(value, "GamepadCameraControlKeys");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<InputKeyItemVM> KeyboardMoveCameraInputKeys
	{
		get
		{
			return _keyboardMoveCameraInputKeys;
		}
		set
		{
			if (value != _keyboardMoveCameraInputKeys)
			{
				_keyboardMoveCameraInputKeys = value;
				OnPropertyChangedWithValue(value, "KeyboardMoveCameraInputKeys");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM KeyboardRotateCameraInputKey
	{
		get
		{
			return _keyboardRotateCameraInputKey;
		}
		set
		{
			if (value != _keyboardRotateCameraInputKey)
			{
				_keyboardRotateCameraInputKey = value;
				OnPropertyChangedWithValue(value, "KeyboardRotateCameraInputKey");
			}
		}
	}

	public PortVM(PortScreenHandler portScreenHandler, PortScreenModes portScreenMode, Action<Ship> onShipSelected, Action onRostersRefreshed, Action<ShipItemVM> refreshShipVisual, Action onUpgradeSlotSelected, bool isAtPortSettlement = false, Settlement portSettlement = null)
	{
		_portScreenHandler = portScreenHandler;
		_portScreenMode = portScreenMode;
		_onShipSelected = onShipSelected;
		_onRostersRefreshed = onRostersRefreshed;
		_refreshShipVisual = refreshShipVisual;
		_onUpgradeSlotSelected = onUpgradeSlotSelected;
		_isAtPortSettlement = isAtPortSettlement;
		_portSettlement = portSettlement;
		ShipItemVM.OnSelected += OnShipSelected;
		ShipItemVM.OnRenamed += OnShipRenamed;
		ShipItemVM.OnNameReset += OnShipNameReset;
		ShipUpgradePieceBaseVM.OnInspected += OnShipPieceInspected;
		ShipUpgradeSlotBaseVM.OnShipPieceSelected += OnShipPieceSelected;
		ShipUpgradeContainerVM.OnSlotSelected = (ShipUpgradeContainerVM.ShipSlotSelectedDelegate)Delegate.Combine(ShipUpgradeContainerVM.OnSlotSelected, new ShipUpgradeContainerVM.ShipSlotSelectedDelegate(OnUpgradeSlotSelected));
		ShipFigureheadSlotVM.GetCurrentFigurehead += GetCurrentFigurehead;
		ShipFigureheadSlotVM.GetShipOfFigurehead += GetShipOfFigurehead;
		ShipFigureheadSlotVM.GetIsRightSide += GetIsShipRightSide;
		ShipUpgradePieceVM.GetUpgradePrice += GetUpgradePrice;
		_allShips = new MBList<ShipItemVM>();
		for (int i = 0; i < _portScreenHandler.LeftShips.Count; i++)
		{
			_allShips.Add(new ShipItemVM(_portScreenHandler.LeftShips[i]));
		}
		for (int j = 0; j < _portScreenHandler.RightShips.Count; j++)
		{
			_allShips.Add(new ShipItemVM(_portScreenHandler.RightShips[j]));
		}
		if (_isAtPortSettlement)
		{
			for (int k = 0; k < Settlement.CurrentSettlement.ShipStash.Count; k++)
			{
				_allShips.Add(new ShipItemVM(Settlement.CurrentSettlement.ShipStash[k]));
			}
		}
		for (int l = 0; l < _allShips.Count; l++)
		{
			_allShips[l].RefreshProperties(_portScreenHandler);
		}
		_cachedChanges = new List<PortChangeInfo>();
		CanConfirmHint = new HintViewModel();
		GoldCostHint = new BasicTooltipViewModel(() => GetGoldCostTooltip());
		LeftRoster = new ShipRosterVM(OnLeftRosterSelected);
		RightRoster = new ShipRosterVM(OnRightRosterSelected);
		BuyAction = new PortActionVM(ExecuteBuy);
		SellAction = new PortActionVM(ExecuteSell);
		RepairAction = new PortActionVM(ExecuteRepair);
		RepairAllAction = new PortActionVM(ExecuteRepairAll);
		SendToClanAction = new PortActionVM(ExecuteSendToClan);
		ShipSelectionPopup = new PortShipStashPopupVM();
		StashShipAction = new PortActionVM(ExecuteSendToStash);
		ViewStashedShipsAction = new PortActionVM(ExecuteOpenViewStashPopup);
		GamepadCameraControlKeys = new MBBindingList<InputKeyItemVM>();
		KeyboardMoveCameraInputKeys = new MBBindingList<InputKeyItemVM>();
		RefreshRosters();
		RefreshActionAvailabilities();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CancelText = GameTexts.FindText("str_cancel").ToString();
		ConfirmText = GameTexts.FindText("str_confirm").ToString();
		LeftRoster.RefreshValues();
		RightRoster.RefreshValues();
		KeyboardMoveCameraText = GameTexts.FindText("str_key_name", typeof(PortHotKeyCategory).Name + "_MovementAxisX").ToString();
		DoneInputKey?.RefreshValues();
		ResetInputKey?.RefreshValues();
		CancelInputKey?.RefreshValues();
		foreach (InputKeyItemVM gamepadCameraControlKey in GamepadCameraControlKeys)
		{
			gamepadCameraControlKey.RefreshValues();
		}
		foreach (InputKeyItemVM keyboardMoveCameraInputKey in KeyboardMoveCameraInputKeys)
		{
			keyboardMoveCameraInputKey.RefreshValues();
		}
		KeyboardRotateCameraInputKey?.RefreshValues();
		UpdateTotalGoldCost();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		LeftRoster.OnFinalize();
		RightRoster.OnFinalize();
		ShipItemVM.OnSelected -= OnShipSelected;
		ShipItemVM.OnRenamed -= OnShipRenamed;
		ShipItemVM.OnNameReset -= OnShipNameReset;
		ShipUpgradePieceBaseVM.OnInspected -= OnShipPieceInspected;
		ShipUpgradeSlotBaseVM.OnShipPieceSelected -= OnShipPieceSelected;
		ShipUpgradeContainerVM.OnSlotSelected = (ShipUpgradeContainerVM.ShipSlotSelectedDelegate)Delegate.Remove(ShipUpgradeContainerVM.OnSlotSelected, new ShipUpgradeContainerVM.ShipSlotSelectedDelegate(OnUpgradeSlotSelected));
		ShipFigureheadSlotVM.GetCurrentFigurehead -= GetCurrentFigurehead;
		ShipFigureheadSlotVM.GetShipOfFigurehead -= GetShipOfFigurehead;
		ShipFigureheadSlotVM.GetIsRightSide -= GetIsShipRightSide;
		ShipUpgradePieceVM.GetUpgradePrice -= GetUpgradePrice;
		ShipSelectionPopup?.OnFinalize();
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
		ResetInputKey?.OnFinalize();
		foreach (InputKeyItemVM gamepadCameraControlKey in GamepadCameraControlKeys)
		{
			gamepadCameraControlKey.OnFinalize();
		}
		foreach (InputKeyItemVM keyboardMoveCameraInputKey in KeyboardMoveCameraInputKeys)
		{
			keyboardMoveCameraInputKey.OnFinalize();
		}
		KeyboardRotateCameraInputKey?.OnFinalize();
	}

	public void OnTick(float dt)
	{
		for (int i = 0; i < _allShips.Count; i++)
		{
			_allShips[i].Upgrades.Update();
		}
	}

	public void UpdateGamepadCameraControlButtonsVisibility()
	{
		bool? forcedVisibility = null;
		bool? forcedVisibility2 = null;
		if (!IsControllingCamera)
		{
			forcedVisibility = false;
		}
		if (!CanToggleCamera)
		{
			forcedVisibility2 = false;
		}
		for (int i = 0; i < GamepadCameraControlKeys.Count; i++)
		{
			InputKeyItemVM inputKeyItemVM = GamepadCameraControlKeys[i];
			if (inputKeyItemVM != GamepadToggleCameraInputKey)
			{
				inputKeyItemVM.SetForcedVisibility(forcedVisibility);
			}
			else
			{
				inputKeyItemVM.SetForcedVisibility(forcedVisibility2);
			}
		}
	}

	private void UpdateTotalGoldCost()
	{
		TotalGoldCost = _portScreenHandler.GetTotalGoldCost();
		_cachedChanges = _portScreenHandler.GetChanges();
		if (TotalGoldCost > 0 || (TotalGoldCost == 0 && _cachedChanges.Count > 0))
		{
			TotalGoldCostText = new TextObject("{=jM8XqvAD}You will pay {GOLD}{GOLD_ICON}").SetTextVariable("GOLD", TotalGoldCost).SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">").ToString();
		}
		else if (TotalGoldCost < 0)
		{
			TotalGoldCostText = new TextObject("{=6ELEOERd}You will receive {GOLD}{GOLD_ICON}").SetTextVariable("GOLD", -TotalGoldCost).SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">").ToString();
		}
		else
		{
			TotalGoldCostText = string.Empty;
		}
		UpdateCanConfirm();
	}

	private void UpdateCanConfirm()
	{
		if (_portScreenHandler.GetCanConfirm(out var disabledHint))
		{
			IsConfirmDisabled = false;
			return;
		}
		IsConfirmDisabled = true;
		CanConfirmHint.HintText = disabledHint;
	}

	private List<TooltipProperty> GetGoldCostTooltip()
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		if (TotalGoldCost >= 0)
		{
			foreach (PortChangeInfo cachedChange in _cachedChanges)
			{
				list.Add(new TooltipProperty(cachedChange.Description, ((int)cachedChange.GoldCost).ToString("+#;-#;0"), 0));
			}
		}
		else if (TotalGoldCost < 0)
		{
			foreach (PortChangeInfo cachedChange2 in _cachedChanges)
			{
				list.Add(new TooltipProperty(cachedChange2.Description, (-(int)cachedChange2.GoldCost).ToString("+#;-#;0"), 0));
			}
		}
		return list;
	}

	public bool AreThereAnyChanges()
	{
		return _portScreenHandler.AreThereAnyChanges();
	}

	public void SelectFirstAvailableRosterAndShip()
	{
		ShipRosterVM shipRosterVM;
		ShipRosterVM shipRosterVM2;
		if (_portScreenMode == PortScreenModes.LootMode)
		{
			shipRosterVM = LeftRoster;
			shipRosterVM2 = RightRoster;
		}
		else
		{
			shipRosterVM = RightRoster;
			shipRosterVM2 = LeftRoster;
		}
		if (shipRosterVM.HasAnyShips)
		{
			shipRosterVM.ExecuteSelectRoster();
			shipRosterVM.Ships[0].ExecuteSelect();
		}
		else if (shipRosterVM2.HasAnyShips)
		{
			shipRosterVM2.ExecuteSelectRoster();
			shipRosterVM2.Ships[0].ExecuteSelect();
		}
		else
		{
			Debug.FailedAssert("There are no ships on either roster!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\Port\\PortVM.cs", "SelectFirstAvailableRosterAndShip", 306);
		}
	}

	private void SelectClosestShipFromActiveRoster(int previousSelectedIndex)
	{
		ShipRosterVM selectedRoster = GetSelectedRoster();
		if (!selectedRoster.HasAnyShips || previousSelectedIndex < 0)
		{
			SelectFirstAvailableRosterAndShip();
			return;
		}
		int index = TaleWorlds.Library.MathF.Min(selectedRoster.Ships.Count - 1, previousSelectedIndex);
		selectedRoster.Ships[index].ExecuteSelect();
	}

	private ShipRosterVM GetSelectedRoster()
	{
		if (!LeftRoster.IsSelected)
		{
			return RightRoster;
		}
		return LeftRoster;
	}

	public void ExecuteCancelWithoutInquiry()
	{
		ExecuteCancel();
	}

	public void ExecuteCancel(bool showCancelInquiry = false)
	{
		if (_portScreenMode == PortScreenModes.LootMode)
		{
			if (AreThereAnyChanges())
			{
				InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_cancelling_changes").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), ExecuteCancelInternal, null));
			}
			else if (LeftRoster.HasAnyShips)
			{
				InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_leaving_ships_behind").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), ExecuteCancelInternal, null));
			}
			else
			{
				ExecuteCancelInternal();
			}
		}
		else if (showCancelInquiry && AreThereAnyChanges())
		{
			InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_cancelling_changes").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), ExecuteCancelInternal, null));
		}
		else
		{
			ExecuteCancelInternal();
		}
	}

	private void ExecuteCancelInternal()
	{
		GameStateManager.Current.PopState();
	}

	public void ExecuteConfirm()
	{
		if (!IsConfirmDisabled)
		{
			if (_portScreenMode == PortScreenModes.LootMode && LeftRoster.HasAnyShips)
			{
				InformationManager.ShowInquiry(new InquiryData("", GameTexts.FindText("str_leaving_ships_behind").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), ExecuteConfirmInternal, null));
			}
			else
			{
				ExecuteConfirmInternal();
			}
		}
	}

	private void ExecuteConfirmInternal()
	{
		_portScreenHandler.OnConfirmChanges();
		GameStateManager.Current.PopState();
	}

	public void ExecuteReset()
	{
		int previousSelectedIndex = GetSelectedRoster().Ships.IndexOf(SelectedShip);
		_portScreenHandler.ResetChanges();
		for (int i = 0; i < _allShips.Count; i++)
		{
			_allShips[i].Upgrades.ResetUpgradePieces();
		}
		RefreshRosters();
		SelectClosestShipFromActiveRoster(previousSelectedIndex);
		UpdateTotalGoldCost();
	}

	public void ExecuteRepair()
	{
		_portScreenHandler.OnRepairShip(SelectedShip.Ship);
		SelectedShip.CurrentHp = SelectedShip.MaxHp;
		SelectedShip.IsRepaired = true;
		UpdateTotalGoldCost();
		RefreshRosters();
	}

	public void ExecuteRepairAll()
	{
		foreach (ShipItemVM ship2 in RightRoster.Ships)
		{
			Ship ship = ship2.Ship;
			PortActionInfo canRepairShip = _portScreenHandler.GetCanRepairShip(ship);
			if (canRepairShip.IsRelevant && canRepairShip.IsEnabled)
			{
				_portScreenHandler.OnRepairShip(ship);
				ship2.CurrentHp = ship2.MaxHp;
				ship2.IsRepaired = true;
			}
		}
		UpdateTotalGoldCost();
		RefreshRosters();
	}

	public void ExecuteSendToClan()
	{
		int previousSelectedIndex = GetSelectedRoster().Ships.IndexOf(SelectedShip);
		_portScreenHandler.OnSendToClan(SelectedShip.Ship);
		UpdateTotalGoldCost();
		RefreshRosters();
		SelectClosestShipFromActiveRoster(previousSelectedIndex);
	}

	public void ExecuteBuy()
	{
		int previousSelectedIndex = GetSelectedRoster().Ships.IndexOf(SelectedShip);
		_portScreenHandler.OnBuyShip(SelectedShip.Ship);
		UpdateTotalGoldCost();
		RefreshRosters();
		SelectClosestShipFromActiveRoster(previousSelectedIndex);
	}

	public void ExecuteSell()
	{
		bool flag = false;
		for (int i = 0; i < _portScreenHandler.SelectedShipPieces.Count; i++)
		{
			if (_portScreenHandler.SelectedShipPieces[i].Ship == SelectedShip.Ship)
			{
				flag = true;
			}
		}
		for (int j = 0; j < _portScreenHandler.SelectedFigureheads.Count; j++)
		{
			if (_portScreenHandler.SelectedFigureheads[j].Ship == SelectedShip.Ship)
			{
				flag = true;
			}
		}
		if (SelectedShip.IsRepaired || SelectedShip.IsRenamed || flag)
		{
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=2H95Y2vK}Sell Ship?").ToString(), new TextObject("{=baQh2cwb}Selling this ship will revert your previous changes to it. Are you sure?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), ExecuteSellAux, null));
		}
		else
		{
			ExecuteSellAux();
		}
	}

	private void ExecuteSellAux()
	{
		int previousSelectedIndex = GetSelectedRoster().Ships.IndexOf(SelectedShip);
		_portScreenHandler.OnSellShip(SelectedShip.Ship);
		SelectedShip.Upgrades.ResetUpgradePieces();
		UpdateTotalGoldCost();
		RefreshRosters();
		SelectClosestShipFromActiveRoster(previousSelectedIndex);
	}

	public void ExecuteSendToStash()
	{
		int previousSelectedIndex = GetSelectedRoster().Ships.IndexOf(SelectedShip);
		_portScreenHandler.OnSendToStash(SelectedShip.Ship);
		UpdateTotalGoldCost();
		RefreshRosters();
		SelectClosestShipFromActiveRoster(previousSelectedIndex);
	}

	public void ExecuteOpenViewStashPopup()
	{
		ShipSelectionPopup.Open(_portSettlement, _portScreenHandler, GetShipItemVM, OnViewStashPopupClosed);
	}

	private ShipItemVM GetShipItemVM(Ship ship)
	{
		return AllShips.FirstOrDefault((ShipItemVM x) => x.Ship == ship);
	}

	private void OnViewStashPopupClosed(List<Ship> takenShips)
	{
		foreach (Ship takenShip in takenShips)
		{
			_portScreenHandler.OnRetrieveFromStash(takenShip);
		}
		UpdateTotalGoldCost();
		RefreshRosters();
	}

	public void ExecuteDeselectSlot()
	{
		SelectedUpgradeSlot?.ExecuteDeselect();
	}

	public bool ExecuteSelectPreviousShip()
	{
		ShipRosterVM selectedRoster = GetSelectedRoster();
		if (!selectedRoster.HasAnyShips)
		{
			return false;
		}
		int num = selectedRoster.Ships.IndexOf(SelectedShip);
		if (num == -1)
		{
			Debug.FailedAssert("Selected ship not found in selected roster!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\Port\\PortVM.cs", "ExecuteSelectPreviousShip", 617);
			selectedRoster.Ships[0].ExecuteSelect();
		}
		else
		{
			int num2 = num - 1;
			if (num2 < 0)
			{
				num2 = selectedRoster.Ships.Count - 1;
			}
			selectedRoster.Ships[num2].ExecuteSelect();
		}
		return true;
	}

	public bool ExecuteSelectNextShip()
	{
		ShipRosterVM selectedRoster = GetSelectedRoster();
		if (!selectedRoster.HasAnyShips)
		{
			return false;
		}
		int num = selectedRoster.Ships.IndexOf(SelectedShip);
		if (num == -1)
		{
			Debug.FailedAssert("Selected ship not found in selected roster!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\Port\\PortVM.cs", "ExecuteSelectNextShip", 646);
			selectedRoster.Ships[0].ExecuteSelect();
		}
		else
		{
			int num2 = num + 1;
			if (num2 >= selectedRoster.Ships.Count)
			{
				num2 = 0;
			}
			selectedRoster.Ships[num2].ExecuteSelect();
		}
		return true;
	}

	private void OnLeftRosterSelected()
	{
		if (!LeftRoster.IsSelected)
		{
			LeftRoster.IsSelected = true;
			RightRoster.IsSelected = false;
			if (LeftRoster.HasAnyShips)
			{
				LeftRoster.Ships[0].ExecuteSelect();
			}
			RefreshActionAvailabilities();
		}
	}

	private void OnRightRosterSelected()
	{
		if (!RightRoster.IsSelected)
		{
			LeftRoster.IsSelected = false;
			RightRoster.IsSelected = true;
			if (RightRoster.HasAnyShips)
			{
				RightRoster.Ships[0].ExecuteSelect();
			}
			RefreshActionAvailabilities();
		}
	}

	private void OnShipPieceInspected(ShipUpgradePieceBaseVM piece)
	{
		if (InspectedUpgrade != null && InspectedUpgrade != piece)
		{
			InspectedUpgrade.IsInspected = false;
		}
		if (piece != null)
		{
			InspectedUpgrade = piece;
			InspectedUpgrade.IsInspected = true;
		}
	}

	public void OnShipPieceSelected(Ship ship, string shipSlotTag, string slotTypeId, ShipUpgradePieceBaseVM pieceVM)
	{
		if (ship == null || string.IsNullOrEmpty(shipSlotTag))
		{
			Debug.FailedAssert("Ship piece selected in an invalid state!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\Port\\PortVM.cs", "OnShipPieceSelected", 716);
		}
		else if (pieceVM == null || !pieceVM.IsDisabled)
		{
			if (string.Equals(slotTypeId, "figurehead", StringComparison.InvariantCultureIgnoreCase))
			{
				_portScreenHandler.OnFigureheadSelected(ship, (pieceVM as ShipFigureheadVM)?.Figurehead);
				UpdateAvailableFigureheads();
			}
			else
			{
				_portScreenHandler.OnUpgradePieceSelected(ship, shipSlotTag, (pieceVM as ShipUpgradePieceVM)?.Piece);
			}
			RefreshSelectedShipProperties();
			UpdateTotalGoldCost();
			_refreshShipVisual?.Invoke(AllShips.FirstOrDefault((ShipItemVM x) => x.Ship == ship));
		}
	}

	public void UpdateAvailableFigureheads()
	{
		for (int i = 0; i < _allShips.Count; i++)
		{
			GetFigureheadSlot(_allShips[i])?.UpdateAvailableFigureheads();
		}
	}

	public Figurehead GetCurrentFigurehead(Ship ship)
	{
		foreach (PortScreenHandler.ShipFigureheadInfo selectedFigurehead in _portScreenHandler.SelectedFigureheads)
		{
			if (selectedFigurehead.Ship == ship)
			{
				return selectedFigurehead.Figurehead;
			}
		}
		return ship.Figurehead;
	}

	public Ship GetShipOfFigurehead(Figurehead figurehead, bool isRightSide)
	{
		MBReadOnlyList<Ship> mBReadOnlyList = (isRightSide ? _portScreenHandler.RightShips : _portScreenHandler.LeftShips);
		for (int i = 0; i < mBReadOnlyList.Count; i++)
		{
			Ship ship = mBReadOnlyList[i];
			if (GetCurrentFigurehead(ship) == figurehead)
			{
				return ship;
			}
		}
		return null;
	}

	private ShipFigureheadSlotVM GetFigureheadSlot(ShipItemVM ship)
	{
		return ship.Upgrades.UpgradeSlots.FirstOrDefault((ShipUpgradeSlotBaseVM x) => x is ShipFigureheadSlotVM) as ShipFigureheadSlotVM;
	}

	private bool GetIsShipRightSide(Ship ship)
	{
		return _portScreenHandler.RightShips.Contains(ship);
	}

	public void OnUpgradeSlotSelected(ShipUpgradeSlotBaseVM slot)
	{
		SelectedUpgradeSlot = slot;
		if (SelectedUpgradeSlot == null)
		{
			InformationManager.HideTooltip();
			ShipUpgradePieceBaseVM inspectedUpgrade = InspectedUpgrade;
			if (inspectedUpgrade == null || !inspectedUpgrade.IsInspectedFromSlot)
			{
				OnShipPieceInspected(null);
			}
		}
		_onUpgradeSlotSelected?.Invoke();
	}

	public int GetUpgradePrice(Ship ship, ShipUpgradePiece piece)
	{
		return _portScreenHandler.GetUpgradeCostOfShip(ship, piece, isRightSideUpgrading: true);
	}

	private void OnShipRenamed(ShipItemVM ship, string newName)
	{
		_portScreenHandler.OnRenameShip(ship.Ship, newName);
		ship.RefreshProperties(_portScreenHandler);
		UpdateTotalGoldCost();
	}

	private void OnShipNameReset(ShipItemVM ship)
	{
		_portScreenHandler.OnResetShipName(ship.Ship);
		ship.RefreshProperties(_portScreenHandler);
		UpdateTotalGoldCost();
	}

	private void OnShipSelected(ShipItemVM ship)
	{
		if (SelectedShip != ship)
		{
			SelectedShip?.Upgrades?.SelectedSlot?.ExecuteDeselect();
			InformationManager.HideTooltip();
			OnShipPieceInspected(null);
			SelectedShip = ship;
			RefreshSelectedShipProperties();
			_onShipSelected?.Invoke(SelectedShip?.Ship);
		}
	}

	private void RefreshSelectedShipProperties()
	{
		if (SelectedShip == null)
		{
			return;
		}
		SelectedShip.RefreshProperties(_portScreenHandler);
		MBList<(string, ShipUpgradePiece)> mBList = new MBList<(string, ShipUpgradePiece)>();
		for (int i = 0; i < SelectedShip.Upgrades.UpgradeSlots.Count; i++)
		{
			ShipUpgradeSlotBaseVM shipUpgradeSlotBaseVM = SelectedShip.Upgrades.UpgradeSlots[i];
			if (shipUpgradeSlotBaseVM.IsChanged && shipUpgradeSlotBaseVM is ShipUpgradeSlotVM)
			{
				mBList.Add((shipUpgradeSlotBaseVM.ShipSlotTag, (shipUpgradeSlotBaseVM.SelectedPiece as ShipUpgradePieceVM)?.Piece));
			}
		}
		SelectedShip.Stats.RefreshStats(SelectedShip.CurrentHp, mBList);
		RefreshActionAvailabilities();
	}

	private void RefreshRosters()
	{
		LeftRoster.SetRosterName(_portScreenHandler.GetLeftRosterName());
		RightRoster.SetRosterName(_portScreenHandler.GetRightRosterName());
		LeftRoster.SetRosterOwner(_portScreenHandler.GetLeftSideOwnerParty());
		RightRoster.SetRosterOwner(_portScreenHandler.GetRightSideOwnerParty());
		GetRosterDifferences(_allShips, _portScreenHandler.LeftShips, LeftRoster.Ships, out var removedShips, out var addedShips);
		GetRosterDifferences(_allShips, _portScreenHandler.RightShips, RightRoster.Ships, out var removedShips2, out var addedShips2);
		LeftRoster.RefreshShips(removedShips, addedShips, _portScreenHandler.LeftShips);
		RightRoster.RefreshShips(removedShips2, addedShips2, _portScreenHandler.RightShips);
		for (int i = 0; i < _allShips.Count; i++)
		{
			_allShips[i].RefreshProperties(_portScreenHandler);
		}
		RefreshSelectedShipProperties();
		UpdateAvailableFigureheads();
		_onRostersRefreshed?.Invoke();
	}

	private static void GetRosterDifferences(MBReadOnlyList<ShipItemVM> allShips, MBReadOnlyList<Ship> currentShips, MBBindingList<ShipItemVM> dataSourceShips, out MBReadOnlyList<ShipItemVM> removedShips, out MBReadOnlyList<ShipItemVM> addedShips)
	{
		MBList<ShipItemVM> mBList = new MBList<ShipItemVM>();
		MBList<ShipItemVM> mBList2 = new MBList<ShipItemVM>();
		for (int i = 0; i < dataSourceShips.Count; i++)
		{
			ShipItemVM shipItemVM = dataSourceShips[i];
			Ship ship = shipItemVM.Ship;
			if (!currentShips.Contains(ship))
			{
				mBList.Add(shipItemVM);
			}
		}
		for (int j = 0; j < currentShips.Count; j++)
		{
			Ship ship2 = currentShips[j];
			bool flag = false;
			for (int k = 0; k < dataSourceShips.Count; k++)
			{
				if (dataSourceShips[k].Ship == ship2)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				continue;
			}
			ShipItemVM shipItemVM2 = null;
			for (int l = 0; l < allShips.Count; l++)
			{
				if (allShips[l].Ship == ship2)
				{
					shipItemVM2 = allShips[l];
					break;
				}
			}
			if (shipItemVM2 == null)
			{
				Debug.FailedAssert($"Unable to find vm for ship: {ship2}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\Port\\PortVM.cs", "GetRosterDifferences", 939);
			}
			else
			{
				mBList2.Add(shipItemVM2);
			}
		}
		removedShips = mBList;
		addedShips = mBList2;
	}

	private void RefreshActionAvailabilities()
	{
		if (SelectedShip != null)
		{
			PortActionInfo canBuyShip = _portScreenHandler.GetCanBuyShip(SelectedShip.Ship);
			BuyAction.RefreshWith(canBuyShip);
			BuyAction.AdditionalInfo = GetGoldCostText(canBuyShip.GoldCost);
			PortActionInfo canSellShip = _portScreenHandler.GetCanSellShip(SelectedShip.Ship);
			SellAction.RefreshWith(canSellShip);
			SellAction.AdditionalInfo = GetGoldCostText(canSellShip.GoldCost);
			PortActionInfo canRepairShip = _portScreenHandler.GetCanRepairShip(SelectedShip.Ship);
			RepairAction.RefreshWith(canRepairShip);
			RepairAction.AdditionalInfo = GetGoldCostText(canRepairShip.GoldCost);
			PortActionInfo canRepairAll = _portScreenHandler.GetCanRepairAll(SelectedShip.Ship);
			RepairAllAction.RefreshWith(canRepairAll);
			RepairAllAction.AdditionalInfo = GetGoldCostText(canRepairAll.GoldCost);
			PortActionInfo actionInfo = _portScreenHandler.GetCanUpgradeShip(SelectedShip.Ship);
			SelectedShip.Upgrades.UpdateEnabledStatus(in actionInfo);
			UpgradeText = actionInfo.ActionName?.ToString();
			PortActionInfo canRenameShip = _portScreenHandler.GetCanRenameShip(SelectedShip.Ship);
			SelectedShip.PlayerCanChangeShipName = canRenameShip.IsRelevant && canRenameShip.IsEnabled;
			SelectedShip.ChangeShipNameHint = new HintViewModel(canRenameShip.Tooltip);
			PortActionInfo canSendToClan = _portScreenHandler.GetCanSendToClan(SelectedShip.Ship);
			SendToClanAction.RefreshWith(canSendToClan);
			SendToClanAction.AdditionalInfo = string.Empty;
			PortActionInfo canStashShip = _portScreenHandler.GetCanStashShip(SelectedShip.Ship);
			StashShipAction.RefreshWith(canStashShip);
			StashShipAction.AdditionalInfo = string.Empty;
			PortActionInfo canViewStash = _portScreenHandler.GetCanViewStash(RightRoster.IsSelected);
			ViewStashedShipsAction.RefreshWith(canViewStash);
			ViewStashedShipsAction.AdditionalInfo = string.Empty;
		}
	}

	private static string GetGoldCostText(int cost)
	{
		if (cost == 0)
		{
			return string.Empty;
		}
		return new TextObject("{=ePmSvu1s}{AMOUNT}{GOLD_ICON}").SetTextVariable("AMOUNT", cost).ToString();
	}

	public void SetResetInputKey(HotKey hotKey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotKey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		ShipSelectionPopup.SetCancelInputKey(hotKey);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		ShipSelectionPopup.SetDoneInputKey(hotKey);
	}

	public void SetSelectPreviousShipInputKey(HotKey hotKey)
	{
		SelectPreviousShipInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetSelectNextShipInputKey(HotKey hotKey)
	{
		SelectNextShipInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetSelectLeftRosterInputKey(HotKey hotKey)
	{
		SelectLeftRosterInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetSelectRightRosterInputKey(HotKey hotKey)
	{
		SelectRightRosterInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetGamepadToggleCameraInputKey(HotKey hotKey)
	{
		InputKeyItemVM inputKeyItemVM = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		GamepadCameraControlKeys.Add(inputKeyItemVM);
		GamepadToggleCameraInputKey = inputKeyItemVM;
		UpdateGamepadCameraControlButtonsVisibility();
	}

	public void AddGamepadCameraControlInputKey(HotKey hotKey)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		GamepadCameraControlKeys.Add(item);
		UpdateGamepadCameraControlButtonsVisibility();
	}

	public void AddGamepadCameraControlInputKey(GameAxisKey gameAxisKey)
	{
		TextObject forcedName = GameTexts.FindText("str_key_name", typeof(PortHotKeyCategory).Name + "_" + gameAxisKey.Id);
		InputKeyItemVM item = InputKeyItemVM.CreateFromForcedID(gameAxisKey.AxisKey.ToString(), forcedName, isConsoleOnly: true);
		GamepadCameraControlKeys.Add(item);
		UpdateGamepadCameraControlButtonsVisibility();
	}

	public void AddKeyboardMoveCameraInputKey(GameKey gameKey)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: false);
		KeyboardMoveCameraInputKeys.Add(item);
	}

	public void SetKeyboardRotateCameraInputKey(HotKey hotKey)
	{
		TextObject forcedName = GameTexts.FindText("str_key_name", typeof(PortHotKeyCategory).Name + "_CameraAxisX");
		InputKeyItemVM keyboardRotateCameraInputKey = InputKeyItemVM.CreateFromForcedID(hotKey.ToString(), forcedName, isConsoleOnly: false);
		KeyboardRotateCameraInputKey = keyboardRotateCameraInputKey;
	}
}
