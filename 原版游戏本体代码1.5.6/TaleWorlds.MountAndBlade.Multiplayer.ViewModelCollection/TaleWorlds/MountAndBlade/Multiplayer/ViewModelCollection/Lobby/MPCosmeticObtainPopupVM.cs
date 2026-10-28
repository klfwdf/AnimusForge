using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticItem;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby;

public class MPCosmeticObtainPopupVM : ViewModel
{
	public enum CosmeticObtainState
	{
		Initialized,
		Ongoing,
		FinishedSuccessfully,
		FinishedUnsuccessfully
	}

	private readonly Action<string, int> _onItemObtained;

	private readonly Func<string> _getExitText;

	private int _currentTauntUsageIndex;

	private string _activeCosmeticID = string.Empty;

	private readonly Dictionary<BasicCultureObject, string> _cultureShieldItemIDs;

	private TextObject _currentLootTextObject = new TextObject("{=7vbGaapv}Your Loot: {LOOT}");

	private const string _purchaseCompleteSound = "event:/ui/multiplayer/shop_purchase_complete";

	private List<EquipmentElement> _characterEquipments;

	private bool _isEnabled;

	private bool _canObtain;

	private bool _isOpenedWithClothingItem;

	private bool _isOpenedWithSigilItem;

	private bool _isOpenedWithTauntItem;

	private bool _isObtainSuccessful;

	private int _obtainState;

	private string _animationVariationText;

	private string _obtainDescriptionText;

	private string _continueText;

	private string _notEnoughLootText;

	private string _obtainResultText;

	private string _previewAsText;

	private string _currentLootText;

	private string _clickToCloseText;

	private CharacterViewModel _characterVisual;

	private MPLobbyCosmeticSigilItemVM _sigilItem;

	private MPArmoryCosmeticClothingItemVM _item;

	private MPArmoryCosmeticTauntItemVM _tauntItem;

	private ItemCollectionElementViewModel _itemVisual;

	private MBBindingList<MPCultureItemVM> _cultures;

	private InputKeyItemVM _doneInputKey;

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
			}
		}
	}

	[DataSourceProperty]
	public bool CanObtain
	{
		get
		{
			return _canObtain;
		}
		set
		{
			if (value != _canObtain)
			{
				_canObtain = value;
				OnPropertyChangedWithValue(value, "CanObtain");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOpenedWithClothingItem
	{
		get
		{
			return _isOpenedWithClothingItem;
		}
		set
		{
			if (value != _isOpenedWithClothingItem)
			{
				_isOpenedWithClothingItem = value;
				OnPropertyChangedWithValue(value, "IsOpenedWithClothingItem");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOpenedWithSigilItem
	{
		get
		{
			return _isOpenedWithSigilItem;
		}
		set
		{
			if (value != _isOpenedWithSigilItem)
			{
				_isOpenedWithSigilItem = value;
				OnPropertyChangedWithValue(value, "IsOpenedWithSigilItem");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOpenedWithTauntItem
	{
		get
		{
			return _isOpenedWithTauntItem;
		}
		set
		{
			if (value != _isOpenedWithTauntItem)
			{
				_isOpenedWithTauntItem = value;
				OnPropertyChangedWithValue(value, "IsOpenedWithTauntItem");
			}
		}
	}

	[DataSourceProperty]
	public bool IsObtainSuccessful
	{
		get
		{
			return _isObtainSuccessful;
		}
		set
		{
			if (value != _isObtainSuccessful)
			{
				_isObtainSuccessful = value;
				OnPropertyChangedWithValue(value, "IsObtainSuccessful");
			}
		}
	}

	[DataSourceProperty]
	public int ObtainState
	{
		get
		{
			return _obtainState;
		}
		set
		{
			if (value != _obtainState)
			{
				_obtainState = value;
				OnPropertyChangedWithValue(value, "ObtainState");
			}
		}
	}

	[DataSourceProperty]
	public string ObtainDescriptionText
	{
		get
		{
			return _obtainDescriptionText;
		}
		set
		{
			if (value != _obtainDescriptionText)
			{
				_obtainDescriptionText = value;
				OnPropertyChangedWithValue(value, "ObtainDescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string AnimationVariationText
	{
		get
		{
			return _animationVariationText;
		}
		set
		{
			if (value != _animationVariationText)
			{
				_animationVariationText = value;
				OnPropertyChangedWithValue(value, "AnimationVariationText");
			}
		}
	}

	[DataSourceProperty]
	public string ContinueText
	{
		get
		{
			return _continueText;
		}
		set
		{
			if (value != _continueText)
			{
				_continueText = value;
				OnPropertyChangedWithValue(value, "ContinueText");
			}
		}
	}

	[DataSourceProperty]
	public string NotEnoughLootText
	{
		get
		{
			return _notEnoughLootText;
		}
		set
		{
			if (value != _notEnoughLootText)
			{
				_notEnoughLootText = value;
				OnPropertyChangedWithValue(value, "NotEnoughLootText");
			}
		}
	}

	[DataSourceProperty]
	public string ObtainResultText
	{
		get
		{
			return _obtainResultText;
		}
		set
		{
			if (value != _obtainResultText)
			{
				_obtainResultText = value;
				OnPropertyChangedWithValue(value, "ObtainResultText");
			}
		}
	}

	[DataSourceProperty]
	public string PreviewAsText
	{
		get
		{
			return _previewAsText;
		}
		set
		{
			if (value != _previewAsText)
			{
				_previewAsText = value;
				OnPropertyChangedWithValue(value, "PreviewAsText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentLootText
	{
		get
		{
			return _currentLootText;
		}
		set
		{
			if (value != _currentLootText)
			{
				_currentLootText = value;
				OnPropertyChangedWithValue(value, "CurrentLootText");
			}
		}
	}

	[DataSourceProperty]
	public string ClickToCloseText
	{
		get
		{
			return _clickToCloseText;
		}
		set
		{
			if (value != _clickToCloseText)
			{
				_clickToCloseText = value;
				OnPropertyChangedWithValue(value, "ClickToCloseText");
			}
		}
	}

	[DataSourceProperty]
	public CharacterViewModel CharacterVisual
	{
		get
		{
			return _characterVisual;
		}
		set
		{
			if (value != _characterVisual)
			{
				_characterVisual = value;
				OnPropertyChangedWithValue(value, "CharacterVisual");
			}
		}
	}

	[DataSourceProperty]
	public MPLobbyCosmeticSigilItemVM SigilItem
	{
		get
		{
			return _sigilItem;
		}
		set
		{
			if (value != _sigilItem)
			{
				_sigilItem = value;
				OnPropertyChangedWithValue(value, "SigilItem");
			}
		}
	}

	[DataSourceProperty]
	public MPArmoryCosmeticClothingItemVM Item
	{
		get
		{
			return _item;
		}
		set
		{
			if (value != _item)
			{
				_item = value;
				OnPropertyChangedWithValue(value, "Item");
			}
		}
	}

	[DataSourceProperty]
	public MPArmoryCosmeticTauntItemVM TauntItem
	{
		get
		{
			return _tauntItem;
		}
		set
		{
			if (value != _tauntItem)
			{
				_tauntItem = value;
				OnPropertyChangedWithValue(value, "TauntItem");
			}
		}
	}

	[DataSourceProperty]
	public ItemCollectionElementViewModel ItemVisual
	{
		get
		{
			return _itemVisual;
		}
		set
		{
			if (value != _itemVisual)
			{
				_itemVisual = value;
				OnPropertyChangedWithValue(value, "ItemVisual");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPCultureItemVM> Cultures
	{
		get
		{
			return _cultures;
		}
		set
		{
			if (value != _cultures)
			{
				_cultures = value;
				OnPropertyChangedWithValue(value, "Cultures");
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

	public MPCosmeticObtainPopupVM(Action<string, int> onItemObtained, Func<string> getContinueKeyText)
	{
		_onItemObtained = onItemObtained;
		_getExitText = getContinueKeyText;
		_characterEquipments = new List<EquipmentElement>();
		ItemVisual = new ItemCollectionElementViewModel();
		Cultures = new MBBindingList<MPCultureItemVM>
		{
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("vlandia").StringId, OnCultureSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("sturgia").StringId, OnCultureSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("empire").StringId, OnCultureSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("battania").StringId, OnCultureSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("khuzait").StringId, OnCultureSelection),
			new MPCultureItemVM(Game.Current.ObjectManager.GetObject<BasicCultureObject>("aserai").StringId, OnCultureSelection)
		};
		_cultureShieldItemIDs = new Dictionary<BasicCultureObject, string>
		{
			[Cultures[0].Culture] = "mp_tall_heater_shield_light",
			[Cultures[1].Culture] = "mp_worn_kite_shield",
			[Cultures[2].Culture] = "mp_leather_bound_kite_shield",
			[Cultures[3].Culture] = "mp_highland_riders_shield",
			[Cultures[4].Culture] = "mp_eastern_wicker_shield",
			[Cultures[5].Culture] = "mp_desert_oval_shield"
		};
		CharacterVisual = new CharacterViewModel();
		MPArmoryCosmeticsVM.OnEquipmentRefreshed += OnCharacterEquipmentRefreshed;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		ContinueText = GameTexts.FindText("str_continue").ToString();
		NotEnoughLootText = new TextObject("{=FzFqhHKU}Not enough loot").ToString();
		PreviewAsText = new TextObject("{=V0bpuzV3}Preview as").ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		MPArmoryCosmeticsVM.OnEquipmentRefreshed -= OnCharacterEquipmentRefreshed;
	}

	private void OnCharacterEquipmentRefreshed(List<EquipmentElement> equipments)
	{
		_characterEquipments.Clear();
		_characterEquipments.AddRange(equipments);
	}

	public void OpenWith(MPArmoryCosmeticClothingItemVM item)
	{
		OnOpened();
		Item = item;
		ItemVisual.FillFrom(item.EquipmentElement);
		ItemVisual.BannerCode = "";
		ItemVisual.InitialPanRotation = 0f;
		ObtainDescriptionText = new TextObject("{=7uILxbP5}You will obtain this item").ToString();
		_activeCosmeticID = item.CosmeticID;
		IsOpenedWithClothingItem = true;
		GameTexts.SetVariable("STR1", GameTexts.FindText("str_continue"));
		GameTexts.SetVariable("STR2", item.Cost);
		ContinueText = GameTexts.FindText("str_STR1_space_STR2").ToString();
		CanObtain = Item?.Cost <= NetworkMain.GameClient.PlayerData.Gold;
	}

	public void OpenWith(MPArmoryCosmeticTauntItemVM item, CharacterViewModel sourceCharacter)
	{
		OnOpened();
		TauntCosmeticElement tauntCosmeticElement = item.TauntCosmeticElement;
		TauntItem = item;
		Equipment equipment = LobbyTauntHelper.PrepareForTaunt(Equipment.CreateFromEquipmentCode(sourceCharacter.EquipmentCode), tauntCosmeticElement);
		equipment.GetInitialWeaponIndicesToEquip(out var mainHandWeaponIndex, out var offHandWeaponIndex, out var isMainHandNotUsableWithOneHand);
		CharacterVisual.RightHandWieldedEquipmentIndex = (int)mainHandWeaponIndex;
		if (!isMainHandNotUsableWithOneHand)
		{
			CharacterVisual.LeftHandWieldedEquipmentIndex = (int)offHandWeaponIndex;
		}
		CharacterVisual.FillFrom(sourceCharacter);
		CharacterVisual.SetEquipment(equipment);
		string defaultAction = TauntUsageManager.Instance.GetDefaultAction(TauntUsageManager.Instance.GetIndexOfAction(tauntCosmeticElement.Id));
		CharacterVisual.ExecuteStartCustomAnimation(TauntUsageManager.Instance.GetDefaultAction(TauntUsageManager.Instance.GetIndexOfAction(tauntCosmeticElement.Id)), loop: true, 0.35f);
		AnimationVariationText = GetAnimationVariationText(defaultAction);
		ItemVisual.BannerCode = "";
		ItemVisual.InitialPanRotation = 0f;
		_activeCosmeticID = tauntCosmeticElement.Id;
		IsOpenedWithTauntItem = true;
		ObtainDescriptionText = new TextObject("{=6mrCNU5U}You will obtain this taunt").ToString();
		GameTexts.SetVariable("STR1", GameTexts.FindText("str_continue"));
		GameTexts.SetVariable("STR2", item.Cost);
		ContinueText = GameTexts.FindText("str_STR1_space_STR2").ToString();
		CanObtain = TauntItem?.Cost <= NetworkMain.GameClient.PlayerData.Gold;
	}

	private string GetAnimationVariationText(string animationName)
	{
		if (animationName.EndsWith("leftstance"))
		{
			return new TextObject("{=8DSymjRe}Left Stance").ToString();
		}
		if (animationName.EndsWith("bow"))
		{
			return new TextObject("{=5rj7xQE4}Bow").ToString();
		}
		return new TextObject("{=fMSYE6Ii}Default").ToString();
	}

	public void ExecuteSelectNextAnimation(int increment)
	{
		if (TauntItem?.TauntCosmeticElement == null)
		{
			Debug.FailedAssert("Invalid taunt cosmetic item", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\MPCosmeticObtainPopupVM.cs", "ExecuteSelectNextAnimation", 177);
			return;
		}
		TauntUsageManager.TauntUsageSet usageSet = TauntUsageManager.Instance.GetUsageSet(_tauntItem.TauntCosmeticElement.Id);
		if (usageSet == null)
		{
			Debug.FailedAssert("No usage set for taunt: " + TauntItem.TauntCosmeticElement.Id, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\MPCosmeticObtainPopupVM.cs", "ExecuteSelectNextAnimation", 185);
			return;
		}
		MBReadOnlyList<TauntUsageManager.TauntUsage> usages = usageSet.GetUsages();
		if (usages == null || usages.Count == 0)
		{
			Debug.FailedAssert("No usages assigned for taunt usage set: " + TauntItem.TauntCosmeticElement.Id, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Lobby\\MPCosmeticObtainPopupVM.cs", "ExecuteSelectNextAnimation", 193);
			return;
		}
		_currentTauntUsageIndex += increment;
		if (_currentTauntUsageIndex >= usages.Count)
		{
			_currentTauntUsageIndex = 0;
		}
		else if (_currentTauntUsageIndex < 0)
		{
			_currentTauntUsageIndex = usages.Count - 1;
		}
		string action = usages[_currentTauntUsageIndex].GetAction();
		CharacterVisual.ExecuteStartCustomAnimation(usages[_currentTauntUsageIndex].GetAction(), loop: true);
		AnimationVariationText = GetAnimationVariationText(action);
	}

	public void OpenWith(MPLobbyCosmeticSigilItemVM sigilItem)
	{
		OnOpened();
		SigilItem = sigilItem;
		_activeCosmeticID = sigilItem.CosmeticID;
		IsOpenedWithSigilItem = true;
		ItemVisual.InitialPanRotation = -3.3f;
		ObtainDescriptionText = new TextObject("{=7uILxbP5}You will obtain this item").ToString();
		MPCultureItemVM mPCultureItemVM = Cultures.FirstOrDefault((MPCultureItemVM c) => c.IsSelected);
		if (mPCultureItemVM != null)
		{
			mPCultureItemVM.IsSelected = false;
		}
		Cultures[0].IsSelected = true;
		OnCultureSelection(Cultures[0]);
		GameTexts.SetVariable("STR1", GameTexts.FindText("str_continue"));
		GameTexts.SetVariable("STR2", sigilItem.Cost);
		ContinueText = GameTexts.FindText("str_STR1_space_STR2").ToString();
		CanObtain = SigilItem.Cost <= NetworkMain.GameClient.PlayerData.Gold;
	}

	private void OnOpened()
	{
		Item = null;
		SigilItem = null;
		IsOpenedWithSigilItem = false;
		IsOpenedWithClothingItem = false;
		IsOpenedWithTauntItem = false;
		IsObtainSuccessful = false;
		ObtainState = 0;
		IsEnabled = true;
		_currentLootTextObject.SetTextVariable("LOOT", NetworkMain.GameClient.PlayerData.Gold);
		CurrentLootText = _currentLootTextObject.ToString();
		ClickToCloseText = _getExitText?.Invoke();
	}

	internal async void ExecuteAction()
	{
		if (ObtainState == 2 || ObtainState == 3)
		{
			ExecuteClosePopup();
		}
		else
		{
			if (ObtainState != 0)
			{
				return;
			}
			ObtainState = 1;
			(bool, int) obj = await NetworkMain.GameClient.BuyCosmetic(_activeCosmeticID);
			bool item = obj.Item1;
			int item2 = obj.Item2;
			ContinueText = GameTexts.FindText("str_continue").ToString();
			if (item)
			{
				if (Item != null)
				{
					Item.IsUnlocked = true;
				}
				else if (SigilItem != null)
				{
					SigilItem.IsUnlocked = true;
				}
				NetworkMain.GameClient.PlayerData.Gold = item2;
				ObtainResultText = new TextObject("{=V0k0urbO}Item obtained").ToString();
				string arg = (IsOpenedWithSigilItem ? SigilItem.CosmeticID : (IsOpenedWithClothingItem ? Item.CosmeticID : string.Empty));
				_onItemObtained(arg, item2);
				IsObtainSuccessful = true;
				ObtainState = 2;
				SoundEvent.PlaySound2D("event:/ui/multiplayer/shop_purchase_complete");
			}
			else
			{
				ObtainResultText = new TextObject("{=XtVZe9cC}Item can not be obtained").ToString();
				IsObtainSuccessful = false;
				ObtainState = 3;
			}
		}
	}

	public void ExecuteClosePopup()
	{
		IsEnabled = false;
	}

	private void OnCultureSelection(MPCultureItemVM cultureItem)
	{
		ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(_cultureShieldItemIDs[cultureItem.Culture]);
		Banner banner = Banner.CreateOneColoredBannerWithOneIcon(cultureItem.Culture.BackgroundColor1, cultureItem.Culture.ForegroundColor1, SigilItem.IconID);
		ItemVisual.FillFrom(new EquipmentElement(item), banner);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
