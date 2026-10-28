using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticItem;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.ClassFilter;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory;

public class MPArmoryVM : ViewModel
{
	private readonly Action<BasicCharacterObject> _onOpenFacegen;

	private bool _canOpenFacegenBeforeTauntState;

	private BasicCharacterObject _character;

	private MPLobbyClassFilterClassItemVM _currentClassItem;

	private Equipment _lastValidEquipment;

	private Func<string> _getExitText;

	private MPArmoryCosmeticTauntItemVM _tauntItemToRefreshNextAnimationWith;

	private MPArmoryCosmeticTauntItemVM _currentTauntPreviewAnimationSource;

	private bool _isEnabled;

	private bool _isManagingTaunts;

	private bool _isTauntAssignmentActive;

	private bool _canOpenFacegen;

	private MPLobbyClassFilterVM _classFilter;

	private MPArmoryHeroPreviewVM _heroPreview;

	private MPArmoryClassStatsVM _classStats;

	private MPArmoryHeroPerkSelectionVM _heroPerkSelection;

	private MPArmoryCosmeticsVM _cosmetics;

	private string _tauntAssignmentClickToCloseText;

	private string _statsText;

	private string _customizationText;

	private string _facegenText;

	private string _manageTauntsText;

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
	public bool IsManagingTaunts
	{
		get
		{
			return _isManagingTaunts;
		}
		set
		{
			if (value != _isManagingTaunts)
			{
				_isManagingTaunts = value;
				OnPropertyChangedWithValue(value, "IsManagingTaunts");
				Cosmetics.IsManagingTaunts = value;
			}
		}
	}

	[DataSourceProperty]
	public bool IsTauntAssignmentActive
	{
		get
		{
			return _isTauntAssignmentActive;
		}
		set
		{
			if (value != _isTauntAssignmentActive)
			{
				_isTauntAssignmentActive = value;
				OnPropertyChangedWithValue(value, "IsTauntAssignmentActive");
				if (_isTauntAssignmentActive)
				{
					TauntAssignmentClickToCloseText = _getExitText?.Invoke();
				}
			}
		}
	}

	[DataSourceProperty]
	public bool CanOpenFacegen
	{
		get
		{
			return _canOpenFacegen;
		}
		set
		{
			if (value != _canOpenFacegen)
			{
				_canOpenFacegen = value;
				OnPropertyChangedWithValue(value, "CanOpenFacegen");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyClassFilterVM ClassFilter
	{
		get
		{
			return _classFilter;
		}
		set
		{
			if (value != _classFilter)
			{
				_classFilter = value;
				OnPropertyChangedWithValue(value, "ClassFilter");
			}
		}
	}

	[DataSourceProperty]
	public MPArmoryHeroPreviewVM HeroPreview
	{
		get
		{
			return _heroPreview;
		}
		set
		{
			if (value != _heroPreview)
			{
				_heroPreview = value;
				OnPropertyChangedWithValue(value, "HeroPreview");
			}
		}
	}

	[DataSourceProperty]
	public MPArmoryClassStatsVM ClassStats
	{
		get
		{
			return _classStats;
		}
		set
		{
			if (value != _classStats)
			{
				_classStats = value;
				OnPropertyChangedWithValue(value, "ClassStats");
			}
		}
	}

	[DataSourceProperty]
	public MPArmoryHeroPerkSelectionVM HeroPerkSelection
	{
		get
		{
			return _heroPerkSelection;
		}
		set
		{
			if (value != _heroPerkSelection)
			{
				_heroPerkSelection = value;
				OnPropertyChangedWithValue(value, "HeroPerkSelection");
			}
		}
	}

	[DataSourceProperty]
	public MPArmoryCosmeticsVM Cosmetics
	{
		get
		{
			return _cosmetics;
		}
		set
		{
			if (value != _cosmetics)
			{
				_cosmetics = value;
				OnPropertyChangedWithValue(value, "Cosmetics");
			}
		}
	}

	[DataSourceProperty]
	public string TauntAssignmentClickToCloseText
	{
		get
		{
			return _tauntAssignmentClickToCloseText;
		}
		set
		{
			if (value != _tauntAssignmentClickToCloseText)
			{
				_tauntAssignmentClickToCloseText = value;
				OnPropertyChangedWithValue(value, "TauntAssignmentClickToCloseText");
			}
		}
	}

	[DataSourceProperty]
	public string StatsText
	{
		get
		{
			return _statsText;
		}
		set
		{
			if (value != _statsText)
			{
				_statsText = value;
				OnPropertyChangedWithValue(value, "StatsText");
			}
		}
	}

	[DataSourceProperty]
	public string CustomizationText
	{
		get
		{
			return _customizationText;
		}
		set
		{
			if (value != _customizationText)
			{
				_customizationText = value;
				OnPropertyChangedWithValue(value, "CustomizationText");
			}
		}
	}

	[DataSourceProperty]
	public string FacegenText
	{
		get
		{
			return _facegenText;
		}
		set
		{
			if (value != _facegenText)
			{
				_facegenText = value;
				OnPropertyChangedWithValue(value, "FacegenText");
			}
		}
	}

	[DataSourceProperty]
	public string ManageTauntsText
	{
		get
		{
			return _manageTauntsText;
		}
		set
		{
			if (value != _manageTauntsText)
			{
				_manageTauntsText = value;
				OnPropertyChangedWithValue(value, "ManageTauntsText");
			}
		}
	}

	public MPArmoryVM(Action<BasicCharacterObject> onOpenFacegen, Action<MPArmoryCosmeticItemBaseVM> onItemObtainRequested, Func<string> getExitText)
	{
		_getExitText = getExitText;
		_onOpenFacegen = onOpenFacegen;
		_character = MBObjectManager.Instance.GetObject<BasicCharacterObject>("mp_character");
		CanOpenFacegen = true;
		ClassFilter = new MPLobbyClassFilterVM(OnSelectedClassChanged);
		HeroPreview = new MPArmoryHeroPreviewVM();
		ClassStats = new MPArmoryClassStatsVM();
		HeroPerkSelection = new MPArmoryHeroPerkSelectionVM(OnSelectPerk, ForceRefreshCharacter);
		Cosmetics = new MPArmoryCosmeticsVM(GetSelectedPerks);
		InitalizeCallbacks();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		StatsText = new TextObject("{=ffjTMejn}Stats").ToString();
		CustomizationText = new TextObject("{=sPkRekRL}Customization").ToString();
		FacegenText = new TextObject("{=RSx1e5Wf}Edit Character").ToString();
		RefreshManageTauntButtonText();
		ClassFilter.RefreshValues();
		HeroPreview.RefreshValues();
		ClassStats.RefreshValues();
		Cosmetics.RefreshValues();
		HeroPerkSelection.RefreshValues();
	}

	private void RefreshManageTauntButtonText()
	{
		ManageTauntsText = (IsManagingTaunts ? new TextObject("{=WiNRdfsm}Done").ToString() : new TextObject("{=58O7bWrD}Manage Taunts").ToString());
	}

	public override void OnFinalize()
	{
		HeroPreview = null;
		_character = null;
		FinalizeCallbacks();
		Cosmetics.OnFinalize();
		base.OnFinalize();
	}

	private void InitalizeCallbacks()
	{
		CharacterViewModel.OnCustomAnimationFinished = (Action<CharacterViewModel>)Delegate.Combine(CharacterViewModel.OnCustomAnimationFinished, new Action<CharacterViewModel>(OnCharacterCustomAnimationFinished));
		MPArmoryCosmeticsVM.OnCosmeticPreview += OnHeroPreviewItemEquipped;
		MPArmoryCosmeticsVM.OnRemoveCosmeticFromPreview += RemoveHeroPreviewItem;
		MPArmoryCosmeticsVM.OnTauntAssignmentRefresh += OnTauntAssignmentRefresh;
	}

	private void FinalizeCallbacks()
	{
		CharacterViewModel.OnCustomAnimationFinished = (Action<CharacterViewModel>)Delegate.Remove(CharacterViewModel.OnCustomAnimationFinished, new Action<CharacterViewModel>(OnCharacterCustomAnimationFinished));
		MPArmoryCosmeticsVM.OnCosmeticPreview -= OnHeroPreviewItemEquipped;
		MPArmoryCosmeticsVM.OnRemoveCosmeticFromPreview -= RemoveHeroPreviewItem;
		MPArmoryCosmeticsVM.OnTauntAssignmentRefresh -= OnTauntAssignmentRefresh;
	}

	public void OnTick(float dt)
	{
		if (_tauntItemToRefreshNextAnimationWith != null)
		{
			MPArmoryCosmeticTauntItemVM tauntItemToRefreshNextAnimationWith = _tauntItemToRefreshNextAnimationWith;
			Equipment equipment = LobbyTauntHelper.PrepareForTaunt(Equipment.CreateFromEquipmentCode(HeroPreview.HeroVisual.EquipmentCode), tauntItemToRefreshNextAnimationWith.TauntCosmeticElement);
			equipment.GetInitialWeaponIndicesToEquip(out var mainHandWeaponIndex, out var offHandWeaponIndex, out var isMainHandNotUsableWithOneHand);
			HeroPreview.HeroVisual.EquipmentCode = equipment.CalculateEquipmentCode();
			HeroPreview.HeroVisual.RightHandWieldedEquipmentIndex = (int)mainHandWeaponIndex;
			if (!isMainHandNotUsableWithOneHand)
			{
				HeroPreview.HeroVisual.LeftHandWieldedEquipmentIndex = (int)offHandWeaponIndex;
			}
			HeroPreview.HeroVisual.ExecuteStartCustomAnimation(CosmeticsManagerHelper.GetSuitableTauntActionForEquipment(equipment, tauntItemToRefreshNextAnimationWith.TauntCosmeticElement), loop: false, 0.25f);
			_currentTauntPreviewAnimationSource = _tauntItemToRefreshNextAnimationWith;
			_tauntItemToRefreshNextAnimationWith = null;
		}
		if (HeroPreview.HeroVisual.IsPlayingCustomAnimations && _currentTauntPreviewAnimationSource != null)
		{
			float num = TaleWorlds.Library.MathF.Clamp(HeroPreview.HeroVisual.CustomAnimationProgressRatio, 0f, 1f);
			_currentTauntPreviewAnimationSource.PreviewAnimationRatio = (int)(num * 100f);
		}
	}

	public void RefreshPlayerData(PlayerData playerData)
	{
		if (_character != null)
		{
			_character.UpdatePlayerCharacterBodyProperties(playerData.BodyProperties, playerData.Race, playerData.IsFemale);
			_character.Age = playerData.BodyProperties.Age;
			HeroPreview.SetCharacter(_character, playerData.BodyProperties.DynamicProperties, playerData.Race, playerData.IsFemale);
			ForceRefreshCharacter();
			Cosmetics.RefreshPlayerData(playerData);
		}
	}

	public void ForceRefreshCharacter()
	{
		OnSelectedClassChanged(ClassFilter.SelectedClassItem, forceUpdate: true);
	}

	private void OnIsEnabledChanged()
	{
		PlayerData playerData = NetworkMain.GameClient.PlayerData;
		if (IsEnabled && playerData != null)
		{
			RefreshPlayerData(playerData);
		}
		if (IsEnabled)
		{
			Cosmetics.RefreshCosmeticInfoFromNetwork();
			if (IsManagingTaunts)
			{
				ExecuteToggleManageTauntsState();
			}
			return;
		}
		foreach (HeroPerkVM item in HeroPerkSelection?.Perks)
		{
			item.Hint?.ExecuteEndHint();
		}
	}

	private void OnSelectedClassChanged(MPLobbyClassFilterClassItemVM selectedClassItem, bool forceUpdate = false)
	{
		if (HeroPreview != null && ClassStats != null && HeroPerkSelection != null && (_currentClassItem != selectedClassItem || forceUpdate))
		{
			_currentClassItem = selectedClassItem;
			HeroPerkSelection.RefreshPerksListWithHero(selectedClassItem.HeroClass);
			HeroPreview.SetCharacterClass(selectedClassItem.HeroClass.HeroCharacter);
			HeroPreview.SetCharacterPerks(HeroPerkSelection.CurrentSelectedPerks);
			ClassStats.RefreshWith(selectedClassItem.HeroClass);
			ClassStats.HeroInformation.RefreshWith(HeroPerkSelection.CurrentHeroClass, HeroPerkSelection.Perks.Select((HeroPerkVM x) => x.SelectedPerk).ToList());
			Cosmetics.RefreshSelectedClass(selectedClassItem.HeroClass, HeroPerkSelection.CurrentSelectedPerks);
			Cosmetics.RefreshCosmeticInfoFromNetwork();
		}
	}

	public void SetCanOpenFacegen(bool enabled)
	{
		CanOpenFacegen = enabled;
	}

	private void ExecuteOpenFaceGen()
	{
		_onOpenFacegen?.Invoke(_character);
	}

	public void ExecuteClearTauntSelection()
	{
		Cosmetics.ClearTauntSelections();
	}

	public void ExecuteToggleManageTauntsState()
	{
		IsManagingTaunts = !IsManagingTaunts;
		if (IsManagingTaunts)
		{
			_canOpenFacegenBeforeTauntState = CanOpenFacegen;
			Cosmetics.RefreshAvailableCategoriesBy(CosmeticsManager.CosmeticType.Taunt);
			Cosmetics.TauntSlots.ApplyActionOnAllItems(delegate(MPArmoryCosmeticTauntSlotVM s)
			{
				s.IsEnabled = true;
			});
			Cosmetics.TauntSlots.ApplyActionOnAllItems(delegate(MPArmoryCosmeticTauntSlotVM s)
			{
				s.IsFocused = false;
			});
		}
		else
		{
			Cosmetics.RefreshAvailableCategoriesBy(CosmeticsManager.CosmeticType.Clothing);
			Cosmetics.TauntSlots.ApplyActionOnAllItems(delegate(MPArmoryCosmeticTauntSlotVM s)
			{
				s.IsEnabled = false;
			});
			Cosmetics.ClearTauntSelections();
		}
		CanOpenFacegen = !IsManagingTaunts && _canOpenFacegenBeforeTauntState;
		RefreshManageTauntButtonText();
	}

	public void ExecuteSelectFocusedSlot()
	{
		foreach (MPArmoryCosmeticTauntSlotVM tauntSlot in Cosmetics.TauntSlots)
		{
			if (tauntSlot.IsFocused)
			{
				tauntSlot.ExecuteSelect();
				break;
			}
		}
	}

	public void ExecuteEmptyFocusedSlot()
	{
		foreach (MPArmoryCosmeticTauntSlotVM tauntSlot in Cosmetics.TauntSlots)
		{
			if (tauntSlot.IsFocused)
			{
				MPArmoryCosmeticTauntItemVM assignedTauntItem = tauntSlot.AssignedTauntItem;
				if (assignedTauntItem != null && assignedTauntItem.IsUsed)
				{
					tauntSlot.AssignedTauntItem.ExecuteAction();
				}
				tauntSlot.EmptySlotKeyVisual.SetForcedVisibility(false);
				tauntSlot.SelectKeyVisual.SetForcedVisibility(false);
				break;
			}
		}
	}

	private void OnSelectPerk(HeroPerkVM heroPerk, MPPerkVM candidate)
	{
		if (ClassStats.HeroInformation.HeroClass != null && HeroPerkSelection.CurrentHeroClass != null)
		{
			List<IReadOnlyPerkObject> currentSelectedPerks = HeroPerkSelection.CurrentSelectedPerks;
			if (currentSelectedPerks.Count > 0)
			{
				ClassStats.HeroInformation.RefreshWith(HeroPerkSelection.CurrentHeroClass, currentSelectedPerks);
				HeroPreview.SetCharacterPerks(currentSelectedPerks);
				Cosmetics.RefreshSelectedClass(_currentClassItem.HeroClass, currentSelectedPerks);
			}
		}
	}

	private void RemoveHeroPreviewItem(MPArmoryCosmeticItemBaseVM itemVM)
	{
		if (itemVM is MPArmoryCosmeticClothingItemVM { EquipmentElement: var equipmentElement })
		{
			EquipmentIndex cosmeticEquipmentIndex = equipmentElement.Item.GetCosmeticEquipmentIndex();
			HeroPreview?.HeroVisual.SetEquipment(cosmeticEquipmentIndex, default(EquipmentElement));
			string text = HeroPreview?.HeroVisual.EquipmentCode;
			if (!string.IsNullOrEmpty(text))
			{
				_lastValidEquipment = Equipment.CreateFromEquipmentCode(text);
			}
		}
	}

	private void OnHeroPreviewItemEquipped(MPArmoryCosmeticItemBaseVM itemVM)
	{
		if (itemVM is MPArmoryCosmeticClothingItemVM { EquipmentElement: var equipmentElement })
		{
			EquipmentIndex cosmeticEquipmentIndex = equipmentElement.Item.GetCosmeticEquipmentIndex();
			HeroPreview?.HeroVisual.SetEquipment(cosmeticEquipmentIndex, equipmentElement);
			string text = HeroPreview?.HeroVisual.EquipmentCode;
			if (!string.IsNullOrEmpty(text))
			{
				_lastValidEquipment = Equipment.CreateFromEquipmentCode(text);
			}
		}
		else
		{
			if (!(itemVM is MPArmoryCosmeticTauntItemVM tauntItemToRefreshNextAnimationWith))
			{
				return;
			}
			if (HeroPreview?.HeroVisual != null)
			{
				HeroPreview.HeroVisual.ExecuteStopCustomAnimation();
				if (_lastValidEquipment != null)
				{
					HeroPreview.HeroVisual.SetEquipment(_lastValidEquipment);
				}
			}
			_tauntItemToRefreshNextAnimationWith = tauntItemToRefreshNextAnimationWith;
		}
	}

	private void OnCharacterCustomAnimationFinished(CharacterViewModel character)
	{
		if (character == HeroPreview.HeroVisual && _lastValidEquipment != null && HeroPreview?.HeroVisual != null)
		{
			HeroPreview.HeroVisual.SetEquipment(_lastValidEquipment);
			HeroPreview.HeroVisual.LeftHandWieldedEquipmentIndex = -1;
			HeroPreview.HeroVisual.RightHandWieldedEquipmentIndex = -1;
			_currentTauntPreviewAnimationSource.PreviewAnimationRatio = 0f;
			_currentTauntPreviewAnimationSource = null;
		}
	}

	private void OnTauntAssignmentRefresh()
	{
		IsTauntAssignmentActive = Cosmetics.SelectedTauntItem != null || Cosmetics.SelectedTauntSlot != null;
	}

	private void ResetHeroEquipment()
	{
		HeroPreview?.HeroVisual.SetEquipment(new Equipment());
	}

	public static void ApplyPerkEffectsToEquipment(ref Equipment equipment, List<IReadOnlyPerkObject> selectedPerks)
	{
		IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = MPPerkObject.GetOnSpawnPerkHandler(selectedPerks)?.GetAlternativeEquipments(isPlayer: true);
		if (enumerable == null)
		{
			return;
		}
		foreach (var item in enumerable)
		{
			equipment[item.Item1] = item.Item2;
		}
	}

	private List<IReadOnlyPerkObject> GetSelectedPerks()
	{
		return HeroPerkSelection.CurrentSelectedPerks;
	}
}
