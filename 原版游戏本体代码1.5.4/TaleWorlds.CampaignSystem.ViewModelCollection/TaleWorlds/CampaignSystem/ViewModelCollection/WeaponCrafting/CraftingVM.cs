using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Refinement;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Smelting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;

public class CraftingVM : ViewModel
{
	public delegate void OnItemRefreshedDelegate(bool isItemVisible);

	private const int _minimumRequiredStamina = 10;

	public OnItemRefreshedDelegate OnItemRefreshed;

	private readonly Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> _getItemUsageSetFlags;

	private readonly ICraftingCampaignBehavior _craftingBehavior;

	private readonly Action _onClose;

	private readonly Action _resetCamera;

	private readonly Action _onWeaponCrafted;

	private Crafting _crafting;

	private InputKeyItemVM _confirmInputKey;

	private InputKeyItemVM _exitInputKey;

	private InputKeyItemVM _previousTabInputKey;

	private InputKeyItemVM _nextTabInputKey;

	private MBBindingList<InputKeyItemVM> _cameraControlKeys;

	private bool _canSwitchTabs;

	private bool _areGamepadControlHintsEnabled;

	private string _doneLbl;

	private string _cancelLbl;

	private HintViewModel _resetCameraHint;

	private HintViewModel _smeltingHint;

	private HintViewModel _craftingHint;

	private HintViewModel _refiningHint;

	private BasicTooltipViewModel _mainActionHint;

	private int _itemValue = -1;

	private string _currentCategoryText;

	private string _mainActionText;

	private string _craftingText;

	private string _smeltingText;

	private string _refinementText;

	private bool _isMainActionEnabled;

	private MBBindingList<CraftingAvailableHeroItemVM> _availableCharactersForSmithing;

	private CraftingAvailableHeroItemVM _currentCraftingHero;

	private MBBindingList<CraftingResourceItemVM> _playerCurrentMaterials;

	private CraftingHeroPopupVM _craftingHeroPopup;

	private bool _isInSmeltingMode;

	private bool _isInCraftingMode;

	private bool _isInRefinementMode;

	private SmeltingVM _smelting;

	private RefinementVM _refinement;

	private WeaponDesignVM _weaponDesign;

	private bool _isSmeltingItemSelected;

	private bool _isRefinementItemSelected;

	private string _selectItemToSmeltText;

	private string _selectItemToRefineText;

	public ElementNotificationVM _tutorialNotification;

	private string _latestTutorialElementID;

	public InputKeyItemVM ConfirmInputKey
	{
		get
		{
			return _confirmInputKey;
		}
		set
		{
			if (value != _confirmInputKey)
			{
				_confirmInputKey = value;
				OnPropertyChangedWithValue(value, "ConfirmInputKey");
			}
		}
	}

	public InputKeyItemVM ExitInputKey
	{
		get
		{
			return _exitInputKey;
		}
		set
		{
			if (value != _exitInputKey)
			{
				_exitInputKey = value;
				OnPropertyChangedWithValue(value, "ExitInputKey");
			}
		}
	}

	public InputKeyItemVM PreviousTabInputKey
	{
		get
		{
			return _previousTabInputKey;
		}
		set
		{
			if (value != _previousTabInputKey)
			{
				_previousTabInputKey = value;
				OnPropertyChangedWithValue(value, "PreviousTabInputKey");
			}
		}
	}

	public InputKeyItemVM NextTabInputKey
	{
		get
		{
			return _nextTabInputKey;
		}
		set
		{
			if (value != _nextTabInputKey)
			{
				_nextTabInputKey = value;
				OnPropertyChangedWithValue(value, "NextTabInputKey");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<InputKeyItemVM> CameraControlKeys
	{
		get
		{
			return _cameraControlKeys;
		}
		set
		{
			if (value != _cameraControlKeys)
			{
				_cameraControlKeys = value;
				OnPropertyChangedWithValue(value, "CameraControlKeys");
			}
		}
	}

	public bool CanSwitchTabs
	{
		get
		{
			return _canSwitchTabs;
		}
		set
		{
			if (value != _canSwitchTabs)
			{
				_canSwitchTabs = value;
				OnPropertyChangedWithValue(value, "CanSwitchTabs");
			}
		}
	}

	public bool AreGamepadControlHintsEnabled
	{
		get
		{
			return _areGamepadControlHintsEnabled;
		}
		set
		{
			if (value != _areGamepadControlHintsEnabled)
			{
				_areGamepadControlHintsEnabled = value;
				OnPropertyChangedWithValue(value, "AreGamepadControlHintsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CraftingResourceItemVM> PlayerCurrentMaterials
	{
		get
		{
			return _playerCurrentMaterials;
		}
		set
		{
			if (value != _playerCurrentMaterials)
			{
				_playerCurrentMaterials = value;
				OnPropertyChangedWithValue(value, "PlayerCurrentMaterials");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CraftingAvailableHeroItemVM> AvailableCharactersForSmithing
	{
		get
		{
			return _availableCharactersForSmithing;
		}
		set
		{
			if (value != _availableCharactersForSmithing)
			{
				_availableCharactersForSmithing = value;
				OnPropertyChangedWithValue(value, "AvailableCharactersForSmithing");
			}
		}
	}

	[DataSourceProperty]
	public CraftingAvailableHeroItemVM CurrentCraftingHero
	{
		get
		{
			return _currentCraftingHero;
		}
		set
		{
			if (value != _currentCraftingHero)
			{
				if (_currentCraftingHero != null)
				{
					_currentCraftingHero.IsSelected = false;
				}
				_currentCraftingHero = value;
				if (_currentCraftingHero != null)
				{
					_currentCraftingHero.IsSelected = true;
				}
				_craftingBehavior.SetActiveCraftingHero(_currentCraftingHero?.Hero);
				OnPropertyChangedWithValue(value, "CurrentCraftingHero");
			}
		}
	}

	[DataSourceProperty]
	public CraftingHeroPopupVM CraftingHeroPopup
	{
		get
		{
			return _craftingHeroPopup;
		}
		set
		{
			if (value != _craftingHeroPopup)
			{
				_craftingHeroPopup = value;
				OnPropertyChangedWithValue(value, "CraftingHeroPopup");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentCategoryText
	{
		get
		{
			return _currentCategoryText;
		}
		set
		{
			if (value != _currentCategoryText)
			{
				_currentCategoryText = value;
				OnPropertyChangedWithValue(value, "CurrentCategoryText");
			}
		}
	}

	[DataSourceProperty]
	public string CraftingText
	{
		get
		{
			return _craftingText;
		}
		set
		{
			if (value != _craftingText)
			{
				_craftingText = value;
				OnPropertyChangedWithValue(value, "CraftingText");
			}
		}
	}

	[DataSourceProperty]
	public string SmeltingText
	{
		get
		{
			return _smeltingText;
		}
		set
		{
			if (value != _smeltingText)
			{
				_smeltingText = value;
				OnPropertyChangedWithValue(value, "SmeltingText");
			}
		}
	}

	[DataSourceProperty]
	public string RefinementText
	{
		get
		{
			return _refinementText;
		}
		set
		{
			if (value != _refinementText)
			{
				_refinementText = value;
				OnPropertyChangedWithValue(value, "RefinementText");
			}
		}
	}

	[DataSourceProperty]
	public string MainActionText
	{
		get
		{
			return _mainActionText;
		}
		set
		{
			if (value != _mainActionText)
			{
				_mainActionText = value;
				OnPropertyChangedWithValue(value, "MainActionText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainActionEnabled
	{
		get
		{
			return _isMainActionEnabled;
		}
		set
		{
			if (value != _isMainActionEnabled)
			{
				_isMainActionEnabled = value;
				OnPropertyChangedWithValue(value, "IsMainActionEnabled");
			}
		}
	}

	[DataSourceProperty]
	public int ItemValue
	{
		get
		{
			return _itemValue;
		}
		set
		{
			if (value != _itemValue)
			{
				_itemValue = value;
				OnPropertyChangedWithValue(value, "ItemValue");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel CraftingHint
	{
		get
		{
			return _craftingHint;
		}
		set
		{
			if (value != _craftingHint)
			{
				_craftingHint = value;
				OnPropertyChangedWithValue(value, "CraftingHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RefiningHint
	{
		get
		{
			return _refiningHint;
		}
		set
		{
			if (value != _refiningHint)
			{
				_refiningHint = value;
				OnPropertyChangedWithValue(value, "RefiningHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel SmeltingHint
	{
		get
		{
			return _smeltingHint;
		}
		set
		{
			if (value != _smeltingHint)
			{
				_smeltingHint = value;
				OnPropertyChangedWithValue(value, "SmeltingHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ResetCameraHint
	{
		get
		{
			return _resetCameraHint;
		}
		set
		{
			if (value != _resetCameraHint)
			{
				_resetCameraHint = value;
				OnPropertyChangedWithValue(value, "ResetCameraHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel MainActionHint
	{
		get
		{
			return _mainActionHint;
		}
		set
		{
			if (value != _mainActionHint)
			{
				_mainActionHint = value;
				OnPropertyChangedWithValue(value, "MainActionHint");
			}
		}
	}

	[DataSourceProperty]
	public string DoneLbl
	{
		get
		{
			return _doneLbl;
		}
		set
		{
			if (value != _doneLbl)
			{
				_doneLbl = value;
				OnPropertyChangedWithValue(value, "DoneLbl");
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
	public SmeltingVM Smelting
	{
		get
		{
			return _smelting;
		}
		set
		{
			if (value != _smelting)
			{
				_smelting = value;
				OnPropertyChangedWithValue(value, "Smelting");
			}
		}
	}

	[DataSourceProperty]
	public WeaponDesignVM WeaponDesign
	{
		get
		{
			return _weaponDesign;
		}
		set
		{
			if (value != _weaponDesign)
			{
				_weaponDesign = value;
				OnPropertyChangedWithValue(value, "WeaponDesign");
			}
		}
	}

	[DataSourceProperty]
	public RefinementVM Refinement
	{
		get
		{
			return _refinement;
		}
		set
		{
			if (value != _refinement)
			{
				_refinement = value;
				OnPropertyChangedWithValue(value, "Refinement");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInCraftingMode
	{
		get
		{
			return _isInCraftingMode;
		}
		set
		{
			if (value != _isInCraftingMode)
			{
				_isInCraftingMode = value;
				OnPropertyChangedWithValue(value, "IsInCraftingMode");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInSmeltingMode
	{
		get
		{
			return _isInSmeltingMode;
		}
		set
		{
			if (value != _isInSmeltingMode)
			{
				_isInSmeltingMode = value;
				OnPropertyChangedWithValue(value, "IsInSmeltingMode");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInRefinementMode
	{
		get
		{
			return _isInRefinementMode;
		}
		set
		{
			if (value != _isInRefinementMode)
			{
				_isInRefinementMode = value;
				OnPropertyChangedWithValue(value, "IsInRefinementMode");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSmeltingItemSelected
	{
		get
		{
			return _isSmeltingItemSelected;
		}
		set
		{
			if (value != _isSmeltingItemSelected)
			{
				_isSmeltingItemSelected = value;
				OnPropertyChangedWithValue(value, "IsSmeltingItemSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRefinementItemSelected
	{
		get
		{
			return _isRefinementItemSelected;
		}
		set
		{
			if (value != _isRefinementItemSelected)
			{
				_isRefinementItemSelected = value;
				OnPropertyChangedWithValue(value, "IsRefinementItemSelected");
			}
		}
	}

	[DataSourceProperty]
	public string SelectItemToSmeltText
	{
		get
		{
			return _selectItemToSmeltText;
		}
		set
		{
			if (value != _selectItemToSmeltText)
			{
				_selectItemToSmeltText = value;
				OnPropertyChangedWithValue(value, "SelectItemToSmeltText");
			}
		}
	}

	[DataSourceProperty]
	public string SelectItemToRefineText
	{
		get
		{
			return _selectItemToRefineText;
		}
		set
		{
			if (value != _selectItemToRefineText)
			{
				_selectItemToRefineText = value;
				OnPropertyChangedWithValue(value, "SelectItemToRefineText");
			}
		}
	}

	[DataSourceProperty]
	public ElementNotificationVM TutorialNotification
	{
		get
		{
			return _tutorialNotification;
		}
		set
		{
			if (value != _tutorialNotification)
			{
				_tutorialNotification = value;
				OnPropertyChangedWithValue(value, "TutorialNotification");
			}
		}
	}

	public CraftingVM(Crafting crafting, Action onClose, Action resetCamera, Action onWeaponCrafted, Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> getItemUsageSetFlags)
	{
		_crafting = crafting;
		_onClose = onClose;
		_resetCamera = resetCamera;
		_onWeaponCrafted = onWeaponCrafted;
		_craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
		_getItemUsageSetFlags = getItemUsageSetFlags;
		AvailableCharactersForSmithing = new MBBindingList<CraftingAvailableHeroItemVM>();
		MainActionHint = new BasicTooltipViewModel();
		TutorialNotification = new ElementNotificationVM();
		CameraControlKeys = new MBBindingList<InputKeyItemVM>();
		if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
		{
			IEnumerable<Hero> availableHeroesForCrafting = CraftingHelper.GetAvailableHeroesForCrafting();
			Hero activeCraftingHero = _craftingBehavior.GetActiveCraftingHero();
			foreach (Hero item in availableHeroesForCrafting)
			{
				CraftingAvailableHeroItemVM craftingAvailableHeroItemVM = new CraftingAvailableHeroItemVM(item, UpdateCraftingHero);
				AvailableCharactersForSmithing.Add(craftingAvailableHeroItemVM);
				if (item == activeCraftingHero)
				{
					CurrentCraftingHero = craftingAvailableHeroItemVM;
				}
			}
			if (CurrentCraftingHero == null)
			{
				CurrentCraftingHero = AvailableCharactersForSmithing.FirstOrDefault();
			}
		}
		else
		{
			CurrentCraftingHero = new CraftingAvailableHeroItemVM(Hero.MainHero, UpdateCraftingHero);
		}
		UpdateCurrentMaterialsAvailable();
		Smelting = new SmeltingVM(OnSmeltItemSelection, UpdateAll);
		Refinement = new RefinementVM(OnRefinementSelectionChange, GetCurrentCraftingHero);
		WeaponDesign = new WeaponDesignVM(_crafting, _craftingBehavior, OnRequireUpdateFromWeaponDesign, _onWeaponCrafted, GetCurrentCraftingHero, RefreshHeroAvailabilities, _getItemUsageSetFlags);
		CraftingHeroPopup = new CraftingHeroPopupVM(GetCraftingHeroes);
		UpdateCraftingPerks();
		ExecuteSwitchToCrafting();
		RefreshValues();
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DoneLbl = GameTexts.FindText("str_done").ToString();
		CancelLbl = GameTexts.FindText("str_exit").ToString();
		ResetCameraHint = new HintViewModel(GameTexts.FindText("str_reset_camera"));
		CraftingHint = new HintViewModel(GameTexts.FindText("str_crafting"));
		RefiningHint = new HintViewModel(GameTexts.FindText("str_refining"));
		SmeltingHint = new HintViewModel(GameTexts.FindText("str_smelting"));
		RefinementText = GameTexts.FindText("str_crafting_category_refinement").ToString();
		CraftingText = GameTexts.FindText("str_crafting_category_crafting").ToString();
		SmeltingText = GameTexts.FindText("str_crafting_category_smelting").ToString();
		SelectItemToSmeltText = new TextObject("{=rUeWBOOi}Select an item to smelt").ToString();
		SelectItemToRefineText = new TextObject("{=BqLsZhhr}Select an item to refine").ToString();
		TutorialNotification.RefreshValues();
		_availableCharactersForSmithing.ApplyActionOnAllItems(delegate(CraftingAvailableHeroItemVM x)
		{
			x.RefreshValues();
		});
		_playerCurrentMaterials.ApplyActionOnAllItems(delegate(CraftingResourceItemVM x)
		{
			x.RefreshValues();
		});
		_currentCraftingHero?.RefreshValues();
		CraftingHeroPopup.RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		WeaponDesign.OnFinalize();
		CraftingHeroPopup.OnFinalize();
		ConfirmInputKey?.OnFinalize();
		ExitInputKey?.OnFinalize();
		PreviousTabInputKey?.OnFinalize();
		NextTabInputKey?.OnFinalize();
		foreach (InputKeyItemVM cameraControlKey in CameraControlKeys)
		{
			cameraControlKey?.OnFinalize();
		}
		Game.Current?.EventManager?.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	private void OnRequireUpdateFromWeaponDesign()
	{
		OnItemRefreshed?.Invoke(isItemVisible: true);
		UpdateAll();
	}

	public void OnCraftingLogicRefreshed(Crafting newCraftingLogic)
	{
		_crafting = newCraftingLogic;
		WeaponDesign.OnCraftingLogicRefreshed(newCraftingLogic);
	}

	private void UpdateCurrentMaterialCosts()
	{
		for (int i = 0; i < 9; i++)
		{
			PlayerCurrentMaterials[i].ResourceAmount = MobileParty.MainParty.ItemRoster.GetItemNumber(PlayerCurrentMaterials[i].ResourceItem);
			PlayerCurrentMaterials[i].ResourceChangeAmount = 0;
		}
		if (IsInSmeltingMode)
		{
			if (Smelting.CurrentSelectedItem != null)
			{
				int[] smeltingOutputForItem = Campaign.Current.Models.SmithingModel.GetSmeltingOutputForItem(Smelting.CurrentSelectedItem.EquipmentElement.Item);
				for (int j = 0; j < 9; j++)
				{
					PlayerCurrentMaterials[j].ResourceChangeAmount = smeltingOutputForItem[j];
				}
			}
		}
		else
		{
			if (IsInRefinementMode)
			{
				RefinementActionItemVM currentSelectedAction = Refinement.CurrentSelectedAction;
				if (currentSelectedAction == null)
				{
					return;
				}
				Crafting.RefiningFormula refineFormula = currentSelectedAction.RefineFormula;
				SmithingModel smithingModel = Campaign.Current.Models.SmithingModel;
				for (int k = 0; k < 9; k++)
				{
					PlayerCurrentMaterials[k].ResourceChangeAmount = 0;
					if (smithingModel.GetCraftingMaterialItem(refineFormula.Input1) == PlayerCurrentMaterials[k].ResourceItem)
					{
						PlayerCurrentMaterials[k].ResourceChangeAmount -= refineFormula.Input1Count;
					}
					else if (smithingModel.GetCraftingMaterialItem(refineFormula.Input2) == PlayerCurrentMaterials[k].ResourceItem)
					{
						PlayerCurrentMaterials[k].ResourceChangeAmount -= refineFormula.Input2Count;
					}
					else if (smithingModel.GetCraftingMaterialItem(refineFormula.Output) == PlayerCurrentMaterials[k].ResourceItem)
					{
						PlayerCurrentMaterials[k].ResourceChangeAmount += refineFormula.OutputCount;
					}
					else if (smithingModel.GetCraftingMaterialItem(refineFormula.Output2) == PlayerCurrentMaterials[k].ResourceItem)
					{
						PlayerCurrentMaterials[k].ResourceChangeAmount += refineFormula.Output2Count;
					}
				}
				int[] array = new int[9];
				foreach (CraftingResourceItemVM inputMaterial in currentSelectedAction.InputMaterials)
				{
					array[(int)inputMaterial.ResourceMaterial] -= inputMaterial.ResourceAmount;
				}
				{
					foreach (CraftingResourceItemVM outputMaterial in currentSelectedAction.OutputMaterials)
					{
						array[(int)outputMaterial.ResourceMaterial] += outputMaterial.ResourceAmount;
					}
					return;
				}
			}
			int[] smithingCostsForWeaponDesign = Campaign.Current.Models.SmithingModel.GetSmithingCostsForWeaponDesign(_crafting.CurrentWeaponDesign);
			for (int l = 0; l < 9; l++)
			{
				PlayerCurrentMaterials[l].ResourceChangeAmount = smithingCostsForWeaponDesign[l];
			}
		}
	}

	private void UpdateCurrentMaterialsAvailable()
	{
		if (PlayerCurrentMaterials == null)
		{
			PlayerCurrentMaterials = new MBBindingList<CraftingResourceItemVM>();
			for (int i = 0; i < 9; i++)
			{
				PlayerCurrentMaterials.Add(new CraftingResourceItemVM((CraftingMaterials)i, 0));
			}
		}
		for (int j = 0; j < 9; j++)
		{
			ItemObject craftingMaterialItem = Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem((CraftingMaterials)j);
			PlayerCurrentMaterials[j].ResourceAmount = MobileParty.MainParty.ItemRoster.GetItemNumber(craftingMaterialItem);
		}
	}

	private void UpdateAll()
	{
		UpdateCurrentMaterialCosts();
		UpdateCurrentMaterialsAvailable();
		RefreshEnableMainAction();
		UpdateCraftingStamina();
		UpdateCraftingSkills();
		RefreshHeroAvailabilities((!IsInCraftingMode) ? null : WeaponDesign.ActiveCraftingOrder?.CraftingOrder);
	}

	private void UpdateCraftingSkills()
	{
		foreach (CraftingAvailableHeroItemVM item in AvailableCharactersForSmithing)
		{
			item.RefreshSkills();
		}
	}

	private void UpdateCraftingStamina()
	{
		foreach (CraftingAvailableHeroItemVM item in AvailableCharactersForSmithing)
		{
			item.RefreshStamina();
		}
	}

	private void UpdateCraftingPerks()
	{
		foreach (CraftingAvailableHeroItemVM item in AvailableCharactersForSmithing)
		{
			item.RefreshPerks();
		}
	}

	private void RefreshHeroAvailabilities(CraftingOrder order)
	{
		foreach (CraftingAvailableHeroItemVM item in AvailableCharactersForSmithing)
		{
			item.RefreshOrderAvailability(order);
		}
	}

	private void RefreshEnableMainAction()
	{
		if (Campaign.Current.GameMode == CampaignGameMode.Tutorial)
		{
			IsMainActionEnabled = true;
			return;
		}
		IsMainActionEnabled = true;
		if (!HaveEnergy())
		{
			IsMainActionEnabled = false;
			if (MainActionHint != null)
			{
				MainActionHint = new BasicTooltipViewModel(() => new TextObject("{=PRE5RKpp}You must rest and spend time before you can do this action.").ToString());
			}
		}
		else if (!HaveMaterialsNeeded())
		{
			IsMainActionEnabled = false;
			if (MainActionHint != null)
			{
				MainActionHint = new BasicTooltipViewModel(() => new TextObject("{=gduqxfck}You don't have all required materials!").ToString());
			}
		}
		if (IsInSmeltingMode)
		{
			IsMainActionEnabled = IsMainActionEnabled && Smelting.IsAnyItemSelected;
			IsSmeltingItemSelected = Smelting.IsAnyItemSelected;
			if (!IsSmeltingItemSelected && MainActionHint != null)
			{
				MainActionHint = new BasicTooltipViewModel(() => new TextObject("{=SzuCFlNq}No item selected.").ToString());
			}
			return;
		}
		if (IsInRefinementMode)
		{
			IsMainActionEnabled = IsMainActionEnabled && Refinement.IsValidRefinementActionSelected;
			IsRefinementItemSelected = Refinement.IsValidRefinementActionSelected;
			if (!IsRefinementItemSelected && MainActionHint != null)
			{
				MainActionHint = new BasicTooltipViewModel(() => new TextObject("{=SzuCFlNq}No item selected.").ToString());
			}
			return;
		}
		if (WeaponDesign != null)
		{
			if (!WeaponDesign.HaveUnlockedAllSelectedPieces())
			{
				IsMainActionEnabled = false;
				if (MainActionHint != null)
				{
					MainActionHint = new BasicTooltipViewModel(() => new TextObject("{=Wir2xZIg}You haven't unlocked some of the selected pieces.").ToString());
				}
			}
			else if (!WeaponDesign.CanCompleteOrder())
			{
				IsMainActionEnabled = false;
				if (MainActionHint != null)
				{
					CraftingOrder order = WeaponDesign.ActiveCraftingOrder?.CraftingOrder;
					ItemObject item = _crafting.GetCurrentCraftedItemObject();
					MainActionHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetOrderCannotBeCompletedReasonTooltip(order, item));
				}
			}
		}
		if (IsMainActionEnabled && MainActionHint != null)
		{
			MainActionHint = new BasicTooltipViewModel();
		}
	}

	private bool HaveEnergy()
	{
		if (CurrentCraftingHero?.Hero != null)
		{
			return _craftingBehavior.GetHeroCraftingStamina(CurrentCraftingHero.Hero) > 10;
		}
		return true;
	}

	private bool HaveMaterialsNeeded()
	{
		return !PlayerCurrentMaterials.Any((CraftingResourceItemVM m) => m.ResourceChangeAmount + m.ResourceAmount < 0);
	}

	public void UpdateCraftingHero(CraftingAvailableHeroItemVM newHero)
	{
		CurrentCraftingHero = newHero;
		CraftingHeroPopupVM craftingHeroPopup = CraftingHeroPopup;
		if (craftingHeroPopup != null && craftingHeroPopup.IsVisible)
		{
			CraftingHeroPopup.ExecuteClosePopup();
		}
		WeaponDesign.OnCraftingHeroChanged(newHero);
		Refinement.OnCraftingHeroChanged(newHero);
		Smelting.OnCraftingHeroChanged(newHero);
		RefreshEnableMainAction();
		UpdateCraftingSkills();
	}

	public (bool isConfirmSuccessful, bool isMainActionExecuted) ExecuteConfirm()
	{
		CraftingHistoryVM craftingHistory = WeaponDesign.CraftingHistory;
		if (craftingHistory != null && craftingHistory.IsVisible)
		{
			if (WeaponDesign.CraftingHistory.SelectedDesign != null)
			{
				WeaponDesign.CraftingHistory.ExecuteDone();
				return (isConfirmSuccessful: true, isMainActionExecuted: false);
			}
		}
		else
		{
			CraftingOrderPopupVM craftingOrderPopup = WeaponDesign.CraftingOrderPopup;
			if (craftingOrderPopup != null && !craftingOrderPopup.IsVisible)
			{
				WeaponClassSelectionPopupVM weaponClassSelectionPopup = WeaponDesign.WeaponClassSelectionPopup;
				if (weaponClassSelectionPopup != null && !weaponClassSelectionPopup.IsVisible)
				{
					CraftingHeroPopupVM craftingHeroPopup = CraftingHeroPopup;
					if (craftingHeroPopup != null && !craftingHeroPopup.IsVisible)
					{
						if (WeaponDesign.IsInFinalCraftingStage)
						{
							if (WeaponDesign.CraftingResultPopup.CanConfirm)
							{
								WeaponDesign.CraftingResultPopup.ExecuteFinalizeCrafting();
								return (isConfirmSuccessful: true, isMainActionExecuted: false);
							}
						}
						else if (IsMainActionEnabled)
						{
							ExecuteMainAction();
							return (isConfirmSuccessful: true, isMainActionExecuted: true);
						}
					}
				}
			}
		}
		return (isConfirmSuccessful: false, isMainActionExecuted: false);
	}

	public void ExecuteCancel()
	{
		CraftingHistoryVM craftingHistory = WeaponDesign.CraftingHistory;
		if (craftingHistory != null && craftingHistory.IsVisible)
		{
			WeaponDesign.CraftingHistory.ExecuteCancel();
			return;
		}
		CraftingHeroPopupVM craftingHeroPopup = CraftingHeroPopup;
		if (craftingHeroPopup != null && craftingHeroPopup.IsVisible)
		{
			CraftingHeroPopup.ExecuteClosePopup();
			return;
		}
		CraftingOrderPopupVM craftingOrderPopup = WeaponDesign.CraftingOrderPopup;
		if (craftingOrderPopup != null && craftingOrderPopup.IsVisible)
		{
			WeaponDesign.CraftingOrderPopup.ExecuteCloseWithoutSelection();
			return;
		}
		WeaponClassSelectionPopupVM weaponClassSelectionPopup = WeaponDesign.WeaponClassSelectionPopup;
		if (weaponClassSelectionPopup != null && weaponClassSelectionPopup.IsVisible)
		{
			WeaponDesign.WeaponClassSelectionPopup.ExecuteClosePopup();
		}
		else if (WeaponDesign.IsInFinalCraftingStage)
		{
			if (WeaponDesign.CraftingResultPopup.CanConfirm)
			{
				WeaponDesign.CraftingResultPopup.ExecuteFinalizeCrafting();
			}
		}
		else
		{
			Smelting.SaveItemLockStates();
			Game.Current.GameStateManager.PopState();
		}
	}

	public void ExecuteMainAction()
	{
		if (IsInSmeltingMode)
		{
			Smelting.TrySmeltingSelectedItems(CurrentCraftingHero.Hero);
		}
		else if (IsInRefinementMode)
		{
			Refinement.ExecuteSelectedRefinement(CurrentCraftingHero.Hero);
		}
		else if (Campaign.Current.GameMode == CampaignGameMode.Tutorial)
		{
			if (GameStateManager.Current.ActiveState is CraftingState craftingState)
			{
				ItemObject currentCraftedItemObject = craftingState.CraftingLogic.GetCurrentCraftedItemObject(forceReCreate: true);
				ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(currentCraftedItemObject.WeaponDesign.HashedCode) ?? MBObjectManager.Instance.RegisterObject(currentCraftedItemObject);
				PartyBase.MainParty.ItemRoster.AddToCounts(item, 1);
				WeaponDesign.IsInFinalCraftingStage = false;
			}
		}
		else
		{
			if (!HaveMaterialsNeeded() || !HaveEnergy())
			{
				return;
			}
			Hero hero = GetCurrentCraftingHero()?.Hero;
			ItemModifier craftedWeaponModifier = Campaign.Current.Models.SmithingModel.GetCraftedWeaponModifier(_crafting.CurrentWeaponDesign, hero);
			_craftingBehavior.SetCurrentItemModifier(craftedWeaponModifier);
			if (WeaponDesign.IsInOrderMode)
			{
				WeaponDesign.CraftedItemObject = _craftingBehavior.CreateCraftedWeaponInCraftingOrderMode(hero, WeaponDesign.ActiveCraftingOrder?.CraftingOrder, _crafting.CurrentWeaponDesign);
			}
			else
			{
				WeaponDesign.CraftedItemObject = _craftingBehavior.CreateCraftedWeaponInFreeBuildMode(hero, _crafting.CurrentWeaponDesign, _craftingBehavior.GetCurrentItemModifier());
			}
			WeaponDesign.IsInFinalCraftingStage = true;
			WeaponDesign.CreateCraftingResultPopup();
			_onWeaponCrafted?.Invoke();
		}
		if (!IsInSmeltingMode)
		{
			UpdateAll();
		}
	}

	public void ExecuteResetCamera()
	{
		_resetCamera();
	}

	private CraftingAvailableHeroItemVM GetCurrentCraftingHero()
	{
		return CurrentCraftingHero;
	}

	private MBBindingList<CraftingAvailableHeroItemVM> GetCraftingHeroes()
	{
		return AvailableCharactersForSmithing;
	}

	public void SetConfirmInputKey(HotKey hotKey)
	{
		ConfirmInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetExitInputKey(HotKey hotKey)
	{
		ExitInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetPreviousTabInputKey(HotKey hotKey)
	{
		PreviousTabInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void SetNextTabInputKey(HotKey hotKey)
	{
		NextTabInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}

	public void AddCameraControlInputKey(HotKey hotKey)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}

	public void AddCameraControlInputKey(GameKey gameKey)
	{
		InputKeyItemVM item = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}

	public void AddCameraControlInputKey(GameAxisKey gameAxisKey)
	{
		TextObject forcedName = GameTexts.FindText("str_key_name", "CraftingHotkeyCategory_" + gameAxisKey.Id);
		InputKeyItemVM item = InputKeyItemVM.CreateFromForcedID(gameAxisKey.AxisKey.ToString(), forcedName, isConsoleOnly: true);
		CameraControlKeys.Add(item);
	}

	public void ExecuteSwitchToCrafting()
	{
		IsInSmeltingMode = false;
		IsInCraftingMode = true;
		IsInRefinementMode = false;
		CurrentCategoryText = new TextObject("{=POjDNVW3}Forging").ToString();
		MainActionText = GameTexts.FindText("str_crafting_category_crafting").ToString();
		OnItemRefreshed?.Invoke(isItemVisible: true);
		UpdateCurrentMaterialCosts();
		UpdateAll();
		WeaponDesign?.ChangeModeIfHeroIsUnavailable();
	}

	public void ExecuteSwitchToSmelting()
	{
		IsInSmeltingMode = true;
		IsInCraftingMode = false;
		IsInRefinementMode = false;
		CurrentCategoryText = new TextObject("{=4cU98rkg}Smelting").ToString();
		MainActionText = GameTexts.FindText("str_crafting_category_smelting").ToString();
		OnItemRefreshed?.Invoke(isItemVisible: false);
		UpdateCurrentMaterialCosts();
		Smelting.RefreshList();
		UpdateAll();
	}

	public void ExecuteSwitchToRefinement()
	{
		IsInSmeltingMode = false;
		IsInCraftingMode = false;
		IsInRefinementMode = true;
		CurrentCategoryText = new TextObject("{=p7raHA9x}Refinement").ToString();
		MainActionText = GameTexts.FindText("str_crafting_category_refinement").ToString();
		OnItemRefreshed?.Invoke(isItemVisible: false);
		UpdateCurrentMaterialCosts();
		Refinement.RefreshRefinementActionsList(CurrentCraftingHero.Hero);
		UpdateAll();
	}

	private void OnRefinementSelectionChange()
	{
		UpdateCurrentMaterialCosts();
		RefreshEnableMainAction();
	}

	private void OnSmeltItemSelection()
	{
		UpdateCurrentMaterialCosts();
		RefreshEnableMainAction();
	}

	public void SetCurrentDesignManually(CraftingTemplate craftingTemplate, (CraftingPiece, int)[] pieces)
	{
		if (!IsInCraftingMode)
		{
			ExecuteSwitchToCrafting();
		}
		WeaponDesign.SetDesignManually(craftingTemplate, pieces, forceChangeTemplate: true);
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (obj.NewNotificationElementID != _latestTutorialElementID)
		{
			if (_latestTutorialElementID != null)
			{
				TutorialNotification.ElementID = string.Empty;
			}
			_latestTutorialElementID = obj.NewNotificationElementID;
			if (_latestTutorialElementID != null)
			{
				TutorialNotification.ElementID = _latestTutorialElementID;
			}
		}
	}
}
