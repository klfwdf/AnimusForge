using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

public class SPItemVM : ItemVM
{
	public enum ProfitTypes
	{
		HighLoss = -2,
		Loss,
		Default,
		Profit,
		HighProfit
	}

	public static Action<SPItemVM> OnFocus;

	public static Action<SPItemVM, bool> ProcessSellItem;

	public static Action<SPItemVM> ProcessItemSlaughter;

	public static Action<SPItemVM> ProcessItemDonate;

	public static Action<SPItemVM, bool> ProcessLockItem;

	private readonly InventoryScreenHelper.InventoryMode _usageType;

	private Concept _tradeGoodConceptObj;

	private Concept _itemConceptObj;

	private InventoryLogic _inventoryLogic;

	private bool _isFocused;

	private bool _isSelected;

	private int _level;

	private bool _isTransferable;

	private bool _isCivilianItem;

	private bool _isStealthItem;

	private bool _isGenderDifferent;

	private bool _isEquipableItem;

	private bool _canCharacterUseItem;

	private bool _isLocked;

	private bool _isArtifact;

	private bool _canBeSlaughtered;

	private bool _canBeDonated;

	private int _count;

	private int _profitType = -5;

	private int _transactionCount;

	private int _totalCost;

	private bool _isTransferButtonHighlighted;

	private bool _isItemHighlightEnabled;

	private bool _isNew;

	private InventoryTradeVM _tradeData;

	public InventoryLogic.InventorySide InventorySide { get; private set; }

	[DataSourceProperty]
	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		set
		{
			if (value != _isFocused)
			{
				_isFocused = value;
				OnPropertyChangedWithValue(value, "IsFocused");
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
	public bool IsArtifact
	{
		get
		{
			return _isArtifact;
		}
		set
		{
			if (value != _isArtifact)
			{
				_isArtifact = value;
				OnPropertyChangedWithValue(value, "IsArtifact");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTransferable
	{
		get
		{
			return _isTransferable;
		}
		set
		{
			if (value != _isTransferable)
			{
				_isTransferable = value;
				OnPropertyChangedWithValue(value, "IsTransferable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTransferButtonHighlighted
	{
		get
		{
			return _isTransferButtonHighlighted;
		}
		set
		{
			if (value != _isTransferButtonHighlighted)
			{
				_isTransferButtonHighlighted = value;
				OnPropertyChangedWithValue(value, "IsTransferButtonHighlighted");
			}
		}
	}

	[DataSourceProperty]
	public bool IsItemHighlightEnabled
	{
		get
		{
			return _isItemHighlightEnabled;
		}
		set
		{
			if (value != _isItemHighlightEnabled)
			{
				_isItemHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsItemHighlightEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCivilianItem
	{
		get
		{
			return _isCivilianItem;
		}
		set
		{
			if (value != _isCivilianItem)
			{
				_isCivilianItem = value;
				OnPropertyChangedWithValue(value, "IsCivilianItem");
			}
		}
	}

	[DataSourceProperty]
	public bool IsStealthItem
	{
		get
		{
			return _isStealthItem;
		}
		set
		{
			if (value != _isStealthItem)
			{
				_isStealthItem = value;
				OnPropertyChangedWithValue(value, "IsStealthItem");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNew
	{
		get
		{
			return _isNew;
		}
		set
		{
			if (value != _isNew)
			{
				_isNew = value;
				OnPropertyChangedWithValue(value, "IsNew");
			}
		}
	}

	[DataSourceProperty]
	public bool IsGenderDifferent
	{
		get
		{
			return _isGenderDifferent;
		}
		set
		{
			if (value != _isGenderDifferent)
			{
				_isGenderDifferent = value;
				OnPropertyChangedWithValue(value, "IsGenderDifferent");
			}
		}
	}

	[DataSourceProperty]
	public bool CanBeSlaughtered
	{
		get
		{
			return _canBeSlaughtered;
		}
		set
		{
			if (value != _canBeSlaughtered)
			{
				_canBeSlaughtered = value;
				OnPropertyChangedWithValue(value, "CanBeSlaughtered");
			}
		}
	}

	[DataSourceProperty]
	public bool CanBeDonated
	{
		get
		{
			return _canBeDonated;
		}
		set
		{
			if (value != _canBeDonated)
			{
				_canBeDonated = value;
				OnPropertyChangedWithValue(value, "CanBeDonated");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEquipableItem
	{
		get
		{
			return _isEquipableItem;
		}
		set
		{
			if (value != _isEquipableItem)
			{
				_isEquipableItem = value;
				OnPropertyChangedWithValue(value, "IsEquipableItem");
			}
		}
	}

	[DataSourceProperty]
	public bool CanCharacterUseItem
	{
		get
		{
			return _canCharacterUseItem;
		}
		set
		{
			if (value != _canCharacterUseItem)
			{
				_canCharacterUseItem = value;
				OnPropertyChangedWithValue(value, "CanCharacterUseItem");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLocked
	{
		get
		{
			return _isLocked;
		}
		set
		{
			if (value != _isLocked)
			{
				_isLocked = value;
				OnPropertyChangedWithValue(value, "IsLocked");
				ProcessLockItem(this, value);
			}
		}
	}

	[DataSourceProperty]
	public int ItemCount
	{
		get
		{
			return _count;
		}
		set
		{
			if (value != _count)
			{
				_count = value;
				OnPropertyChangedWithValue(value, "ItemCount");
				UpdateTotalCost();
				UpdateTradeData(forceUpdateAmounts: false);
			}
		}
	}

	[DataSourceProperty]
	public int ItemLevel
	{
		get
		{
			return _level;
		}
		set
		{
			if (value != _level)
			{
				_level = value;
				OnPropertyChangedWithValue(value, "ItemLevel");
			}
		}
	}

	[DataSourceProperty]
	public int ProfitType
	{
		get
		{
			return _profitType;
		}
		set
		{
			if (value != _profitType)
			{
				_profitType = value;
				OnPropertyChangedWithValue(value, "ProfitType");
			}
		}
	}

	[DataSourceProperty]
	public int TransactionCount
	{
		get
		{
			return _transactionCount;
		}
		set
		{
			if (value != _transactionCount)
			{
				_transactionCount = value;
				OnPropertyChangedWithValue(value, "TransactionCount");
				UpdateTotalCost();
			}
		}
	}

	[DataSourceProperty]
	public int TotalCost
	{
		get
		{
			return _totalCost;
		}
		set
		{
			if (value != _totalCost)
			{
				_totalCost = value;
				OnPropertyChangedWithValue(value, "TotalCost");
			}
		}
	}

	[DataSourceProperty]
	public InventoryTradeVM TradeData
	{
		get
		{
			return _tradeData;
		}
		set
		{
			if (value != _tradeData)
			{
				_tradeData = value;
				OnPropertyChangedWithValue(value, "TradeData");
			}
		}
	}

	public SPItemVM()
	{
		base.StringId = "";
		base.ImageIdentifier = new ItemImageIdentifierVM(null);
		_itemType = EquipmentIndex.None;
	}

	public SPItemVM(InventoryLogic inventoryLogic, bool isHeroFemale, bool canCharacterUseItem, InventoryScreenHelper.InventoryMode usageType, ItemRosterElement newItem, InventoryLogic.InventorySide inventorySide, int itemCost = 0, EquipmentIndex? itemType = EquipmentIndex.None)
	{
		if (newItem.EquipmentElement.Item != null)
		{
			_usageType = usageType;
			_tradeGoodConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_trade_goods");
			_itemConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_item");
			_inventoryLogic = inventoryLogic;
			ItemRosterElement = new ItemRosterElement(newItem.EquipmentElement, newItem.Amount);
			base.ItemCost = itemCost;
			ItemCount = newItem.Amount;
			TransactionCount = 1;
			ItemLevel = newItem.EquipmentElement.Item.Difficulty;
			InventorySide = inventorySide;
			if (itemType.HasValue && itemType != EquipmentIndex.None)
			{
				_itemType = itemType.Value;
			}
			OnItemTypeUpdated();
			base.ItemDescription = newItem.EquipmentElement.GetModifiedItemName().ToString();
			base.StringId = CampaignUIHelper.GetItemLockStringID(newItem.EquipmentElement);
			base.ImageIdentifier = new ItemImageIdentifierVM(newItem.EquipmentElement.Item, Clan.PlayerClan?.Banner.Serialize());
			IsCivilianItem = newItem.EquipmentElement.Item.ItemFlags.HasAnyFlag(ItemFlags.Civilian);
			IsStealthItem = newItem.EquipmentElement.Item.ItemFlags.HasAnyFlag(ItemFlags.Stealth);
			IsGenderDifferent = (isHeroFemale && ItemRosterElement.EquipmentElement.Item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale)) || (!isHeroFemale && ItemRosterElement.EquipmentElement.Item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale));
			CanCharacterUseItem = canCharacterUseItem;
			IsArtifact = newItem.EquipmentElement.Item.IsUniqueItem;
			CanBeDonated = _inventoryLogic?.CanDonateItem(ItemRosterElement, InventorySide) ?? false;
			TradeData = new InventoryTradeVM(_inventoryLogic, ItemRosterElement, inventorySide, OnTradeApplyTransaction);
			InventoryScreenHelper.InventoryMode inventoryMode = InventoryScreenHelper.GetActiveInventoryState()?.InventoryMode ?? InventoryScreenHelper.InventoryMode.Default;
			IsTransferable = !ItemRosterElement.EquipmentElement.IsQuestItem && ItemRosterElement.EquipmentElement.Item.IsTransferable && (inventoryMode != InventoryScreenHelper.InventoryMode.Warehouse || ItemRosterElement.EquipmentElement.Item.IsTradeGood);
			TradeData.IsTradeable = IsTransferable;
			IsEquipableItem = (InventoryScreenHelper.GetInventoryItemTypeOfItem(newItem.EquipmentElement.Item) & InventoryScreenHelper.InventoryItemType.Equipable) != 0;
			UpdateCanBeSlaughtered();
			UpdateHintTexts();
			UpdateProfitType();
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (ItemRosterElement.EquipmentElement.Item != null)
		{
			base.ItemDescription = ItemRosterElement.EquipmentElement.GetModifiedItemName()?.ToString() ?? "";
		}
		else
		{
			base.ItemDescription = "";
		}
	}

	public void RefreshWith(SPItemVM itemVM, InventoryLogic.InventorySide inventorySide)
	{
		InventorySide = inventorySide;
		if (itemVM == null)
		{
			Reset();
			return;
		}
		base.ItemDescription = itemVM.ItemDescription;
		base.ItemCost = itemVM.ItemCost;
		base.TypeName = itemVM.TypeName;
		_itemType = itemVM.ItemType;
		ItemCount = itemVM.ItemCount;
		TransactionCount = itemVM.TransactionCount;
		ItemLevel = itemVM.ItemLevel;
		base.StringId = itemVM.StringId;
		base.ImageIdentifier = itemVM.ImageIdentifier.Clone();
		ItemRosterElement = itemVM.ItemRosterElement;
		IsCivilianItem = itemVM.IsCivilianItem;
		IsStealthItem = itemVM.IsStealthItem;
		IsGenderDifferent = itemVM.IsGenderDifferent;
		IsEquipableItem = itemVM.IsEquipableItem;
		CanCharacterUseItem = CanCharacterUseItem;
		IsArtifact = itemVM.IsArtifact;
		IsSelected = itemVM.IsSelected;
		_inventoryLogic = itemVM._inventoryLogic;
		IsTransferable = itemVM.IsTransferable;
		UpdateCanBeSlaughtered();
		UpdateHintTexts();
		CanBeDonated = _inventoryLogic?.CanDonateItem(ItemRosterElement, InventorySide) ?? false;
		TradeData = new InventoryTradeVM(_inventoryLogic, itemVM.ItemRosterElement, inventorySide, OnTradeApplyTransaction);
		UpdateProfitType();
		base.Version++;
	}

	private void Reset()
	{
		base.ItemDescription = "";
		base.ItemCost = 0;
		base.TypeName = "";
		_itemType = EquipmentIndex.None;
		ItemCount = 0;
		TransactionCount = 0;
		ItemLevel = 0;
		base.StringId = "";
		base.ImageIdentifier = new ItemImageIdentifierVM(null);
		ItemRosterElement = default(ItemRosterElement);
		ProfitType = 0;
		IsCivilianItem = true;
		IsStealthItem = false;
		IsGenderDifferent = false;
		IsEquipableItem = true;
		IsArtifact = false;
		IsSelected = false;
		TradeData = new InventoryTradeVM(_inventoryLogic, ItemRosterElement, InventoryLogic.InventorySide.None, OnTradeApplyTransaction);
		base.Version++;
	}

	private void UpdateProfitType()
	{
		ProfitType = 0;
		if (Campaign.Current == null || (InventoryScreenHelper.GetActiveInventoryState()?.InventoryMode ?? InventoryScreenHelper.InventoryMode.Default) != InventoryScreenHelper.InventoryMode.Trade)
		{
			return;
		}
		if (InventorySide == InventoryLogic.InventorySide.PlayerInventory)
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero == null || !mainHero.GetPerkValue(DefaultPerks.Trade.Appraiser))
			{
				Hero mainHero2 = Hero.MainHero;
				if (mainHero2 == null || !mainHero2.GetPerkValue(DefaultPerks.Trade.WholeSeller))
				{
					return;
				}
			}
			IPlayerTradeBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IPlayerTradeBehavior>();
			if (campaignBehavior != null)
			{
				int num = -campaignBehavior.GetProjectedProfit(ItemRosterElement, base.ItemCost) + base.ItemCost;
				ProfitType = (int)GetProfitTypeFromDiff(num, base.ItemCost);
			}
		}
		else
		{
			if (InventorySide != InventoryLogic.InventorySide.OtherInventory || Settlement.CurrentSettlement == null || (!Settlement.CurrentSettlement.IsFortification && !Settlement.CurrentSettlement.IsVillage))
			{
				return;
			}
			Hero mainHero3 = Hero.MainHero;
			if (mainHero3 == null || !mainHero3.GetPerkValue(DefaultPerks.Trade.CaravanMaster))
			{
				Hero mainHero4 = Hero.MainHero;
				if (mainHero4 == null || !mainHero4.GetPerkValue(DefaultPerks.Trade.MarketDealer))
				{
					return;
				}
			}
			float averagePriceFactorItemCategory = _inventoryLogic.GetAveragePriceFactorItemCategory(ItemRosterElement.EquipmentElement.Item.ItemCategory);
			Town town = (Settlement.CurrentSettlement.IsVillage ? Settlement.CurrentSettlement.Village.Bound.Town : Settlement.CurrentSettlement.Town);
			if (averagePriceFactorItemCategory != -99f)
			{
				ProfitType = (int)GetProfitTypeFromDiff(town.MarketData.GetPriceFactor(ItemRosterElement.EquipmentElement.Item.ItemCategory), averagePriceFactorItemCategory);
			}
		}
	}

	public void ExecuteBuySingle()
	{
		ExecuteBuy(1);
	}

	public void ExecuteBuy(int amount)
	{
		TransactionCount = amount;
		ItemVM.ProcessBuyItem(this, arg2: false);
	}

	public void ExecuteSellSingle()
	{
		ExecuteSell(1);
	}

	public void ExecuteSell(int amount)
	{
		TransactionCount = amount;
		ProcessSellItem(this, arg2: false);
	}

	private void OnTradeApplyTransaction(int amount, bool isBuying)
	{
		TransactionCount = amount;
		if (isBuying)
		{
			ItemVM.ProcessBuyItem(this, arg2: true);
		}
		else
		{
			ProcessSellItem(this, arg2: true);
		}
	}

	public void ExecuteSellItem()
	{
		ProcessSellItem(this, arg2: false);
	}

	public void ExecuteConcept()
	{
		if (_tradeGoodConceptObj != null)
		{
			ItemObject item = ItemRosterElement.EquipmentElement.Item;
			if (item != null && item.Type == ItemObject.ItemTypeEnum.Goods)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(_tradeGoodConceptObj.EncyclopediaLink);
				return;
			}
		}
		if (_itemConceptObj != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(_itemConceptObj.EncyclopediaLink);
		}
	}

	public void ExecuteResetTrade()
	{
		TradeData.ExecuteReset();
	}

	private void UpdateTotalCost()
	{
		if (TransactionCount > 0 && _inventoryLogic != null && !InventoryLogic.IsEquipmentSide(InventorySide))
		{
			TotalCost = _inventoryLogic.GetItemTotalPrice(ItemRosterElement, TransactionCount, out var _, InventorySide == InventoryLogic.InventorySide.OtherInventory);
		}
	}

	public void UpdateTradeData(bool forceUpdateAmounts)
	{
		TradeData?.UpdateItemData(ItemRosterElement, InventorySide, forceUpdateAmounts);
		UpdateProfitType();
	}

	public void ExecuteSlaughterItem()
	{
		if (CanBeSlaughtered)
		{
			ProcessItemSlaughter(this);
		}
	}

	public void ExecuteDonateItem()
	{
		if (CanBeDonated)
		{
			ProcessItemDonate(this);
		}
	}

	public void ExecuteSetFocused()
	{
		IsFocused = true;
		OnFocus?.Invoke(this);
	}

	public void ExecuteSetUnfocused()
	{
		IsFocused = false;
		OnFocus?.Invoke(null);
	}

	public void UpdateCanBeSlaughtered()
	{
		InventoryLogic inventoryLogic = _inventoryLogic;
		CanBeSlaughtered = inventoryLogic != null && inventoryLogic.CanSlaughterItem(ItemRosterElement, InventorySide) && !ItemRosterElement.EquipmentElement.IsQuestItem;
	}

	private string GetStackModifierString()
	{
		if (InventoryLogic.IsEquipmentSide(InventorySide))
		{
			return string.Empty;
		}
		TextObject allStackText = ((_inventoryLogic?.OtherParty == null) ? GameTexts.FindText("str_entire_stack_shortcut_discard") : GameTexts.FindText("str_entire_stack_shortcut_transfer"));
		TextObject fiveStackText = ((_inventoryLogic?.OtherParty == null) ? GameTexts.FindText("str_five_stack_shortcut_discard") : GameTexts.FindText("str_five_stack_shortcut_transfer"));
		return CampaignUIHelper.GetStackModifierString(allStackText, fiveStackText, ItemCount >= 5);
	}

	private string GetTextWithStackModifierText(string mainText)
	{
		string stackModifierString = GetStackModifierString();
		if (string.IsNullOrEmpty(stackModifierString))
		{
			return mainText;
		}
		return GameTexts.FindText("str_string_newline_string").SetTextVariable("STR1", mainText).SetTextVariable("STR2", stackModifierString)
			.ToString();
	}

	public void UpdateHintTexts()
	{
		base.SlaughterHint = new BasicTooltipViewModel(() => GetTextWithStackModifierText(GameTexts.FindText("str_inventory_slaughter").ToString()));
		base.DonateHint = new BasicTooltipViewModel(() => GetTextWithStackModifierText(GameTexts.FindText("str_inventory_donate").ToString()));
		base.PreviewHint = new HintViewModel(GameTexts.FindText("str_inventory_preview"));
		base.EquipHint = new HintViewModel(GameTexts.FindText("str_inventory_equip"));
		base.UnequipHint = new HintViewModel(GameTexts.FindText("str_inventory_unequip"));
		base.LockHint = new HintViewModel(GameTexts.FindText("str_lock_in_inventory").SetTextVariable("TRANSFERABLE", GameTexts.FindText("str_items").ToString()));
		if (_usageType == InventoryScreenHelper.InventoryMode.Loot || _usageType == InventoryScreenHelper.InventoryMode.Stash)
		{
			base.BuyAndEquipHint = new BasicTooltipViewModel(() => GameTexts.FindText("str_inventory_take_and_equip").ToString());
			base.BuyHint = new BasicTooltipViewModel(() => GetTextWithStackModifierText(GameTexts.FindText("str_take").ToString()));
		}
		else if (_usageType == InventoryScreenHelper.InventoryMode.Default)
		{
			base.BuyAndEquipHint = new BasicTooltipViewModel(() => GameTexts.FindText("str_inventory_take_and_equip").ToString());
			base.BuyHint = new BasicTooltipViewModel(() => GetTextWithStackModifierText(GameTexts.FindText("str_take").ToString()));
		}
		else
		{
			base.BuyAndEquipHint = new BasicTooltipViewModel(() => GameTexts.FindText("str_inventory_buy_and_equip").ToString());
			base.BuyHint = new BasicTooltipViewModel(() => GetTextWithStackModifierText(GameTexts.FindText("str_inventory_buy").ToString()));
		}
		if (!IsTransferable)
		{
			base.SellHint = new BasicTooltipViewModel(() => new TextObject("{=8xKky9ja}This item cannot be traded or discarded").ToString());
		}
		else if (_usageType == InventoryScreenHelper.InventoryMode.Loot || _usageType == InventoryScreenHelper.InventoryMode.Stash)
		{
			base.SellHint = new BasicTooltipViewModel(() => GetTextWithStackModifierText(GameTexts.FindText("str_give").ToString()));
		}
		else if (_usageType == InventoryScreenHelper.InventoryMode.Default)
		{
			base.SellHint = new BasicTooltipViewModel(() => GetTextWithStackModifierText(GameTexts.FindText("str_inventory_discard").ToString()));
		}
		else
		{
			base.SellHint = new BasicTooltipViewModel(() => GetTextWithStackModifierText(GameTexts.FindText("str_inventory_sell").ToString()));
		}
	}

	public static ProfitTypes GetProfitTypeFromDiff(float averageValue, float currentValue)
	{
		if (averageValue != 0f)
		{
			if (averageValue < currentValue * 0.8f)
			{
				return ProfitTypes.HighProfit;
			}
			if (averageValue < currentValue * 0.95f)
			{
				return ProfitTypes.Profit;
			}
			if (averageValue > currentValue * 1.05f)
			{
				return ProfitTypes.Loss;
			}
			if (averageValue > currentValue * 1.2f)
			{
				return ProfitTypes.HighLoss;
			}
			return ProfitTypes.Default;
		}
		return ProfitTypes.Default;
	}
}
