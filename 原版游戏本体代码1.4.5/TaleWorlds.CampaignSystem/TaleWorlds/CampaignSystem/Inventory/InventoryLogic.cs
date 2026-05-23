using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Inventory;

public class InventoryLogic
{
	public enum TransferType
	{
		Neutral,
		Sell,
		Buy
	}

	public enum InventorySide
	{
		OtherInventory = 0,
		PlayerInventory = 1,
		CivilianEquipment = 2,
		BattleEquipment = 3,
		StealthEquipment = 4,
		None = -1
	}

	public delegate void AfterResetDelegate(InventoryLogic inventoryLogic, bool fromCancel);

	public delegate void TotalAmountChangeDelegate(int newTotalAmount);

	public delegate void ProcessResultListDelegate(InventoryLogic inventoryLogic, List<TransferCommandResult> results);

	private class PartyEquipment
	{
		public Dictionary<CharacterObject, Equipment[]> CharacterEquipments { get; private set; }

		public PartyEquipment(MobileParty party)
		{
			CharacterEquipments = new Dictionary<CharacterObject, Equipment[]>();
			InitializeCopyFrom(party);
		}

		public void InitializeCopyFrom(MobileParty party)
		{
			CharacterEquipments = new Dictionary<CharacterObject, Equipment[]>();
			for (int i = 0; i < party.MemberRoster.Count; i++)
			{
				CharacterObject character = party.MemberRoster.GetElementCopyAtIndex(i).Character;
				if (character.IsHero)
				{
					CharacterEquipments.Add(character, new Equipment[3]
					{
						new Equipment(character.FirstBattleEquipment),
						new Equipment(character.FirstCivilianEquipment),
						new Equipment(character.FirstStealthEquipment)
					});
				}
			}
		}

		internal void ResetEquipment()
		{
			foreach (KeyValuePair<CharacterObject, Equipment[]> characterEquipment in CharacterEquipments)
			{
				Equipment[] value = characterEquipment.Value;
				foreach (Equipment equipment in value)
				{
					if (equipment.IsBattle)
					{
						characterEquipment.Key.FirstBattleEquipment.FillFrom(equipment);
					}
					else if (equipment.IsCivilian)
					{
						characterEquipment.Key.FirstCivilianEquipment.FillFrom(equipment);
					}
					else if (equipment.IsStealth)
					{
						characterEquipment.Key.FirstStealthEquipment.FillFrom(equipment);
					}
					else
					{
						Debug.FailedAssert("Equipment type cannot be found!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Inventory\\InventoryLogic.cs", "ResetEquipment", 1182);
					}
				}
			}
		}

		public void SetReference(PartyEquipment partyEquipment)
		{
			CharacterEquipments.Clear();
			CharacterEquipments = partyEquipment.CharacterEquipments;
		}

		public bool IsEqual(PartyEquipment partyEquipment)
		{
			if (partyEquipment.CharacterEquipments.Keys.Count != CharacterEquipments.Keys.Count)
			{
				return false;
			}
			foreach (CharacterObject key in partyEquipment.CharacterEquipments.Keys)
			{
				if (!CharacterEquipments.Keys.Contains(key))
				{
					return false;
				}
				if (!CharacterEquipments.TryGetValue(key, out var value))
				{
					return false;
				}
				if (!partyEquipment.CharacterEquipments.TryGetValue(key, out var value2) || value2.Length != value.Length)
				{
					return false;
				}
				for (int i = 0; i < value.Length; i++)
				{
					if (!value[i].IsEquipmentEqualTo(value2[i]))
					{
						return false;
					}
				}
			}
			return true;
		}
	}

	private class ItemLog : IReadOnlyCollection<int>, IEnumerable<int>, IEnumerable
	{
		private List<int> _transactions = new List<int>();

		private bool _isSelling;

		public bool IsSelling => _isSelling;

		public int Count => ((IReadOnlyCollection<int>)_transactions).Count;

		private void AddTransaction(int price, bool isSelling)
		{
			if (_transactions.IsEmpty())
			{
				_isSelling = isSelling;
			}
			_transactions.Add(price);
		}

		private void RemoveLastTransaction()
		{
			if (!_transactions.IsEmpty())
			{
				_transactions.RemoveAt(_transactions.Count - 1);
			}
			else
			{
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Inventory\\InventoryLogic.cs", "RemoveLastTransaction", 1262);
			}
		}

		public void RecordTransaction(int price, bool isSelling)
		{
			if (!_transactions.IsEmpty() && isSelling != _isSelling)
			{
				RemoveLastTransaction();
			}
			else
			{
				AddTransaction(price, isSelling);
			}
		}

		public bool GetLastTransaction(out int price, out bool isSelling)
		{
			if (_transactions.IsEmpty())
			{
				price = 0;
				isSelling = false;
				return false;
			}
			price = _transactions[_transactions.Count - 1];
			isSelling = _isSelling;
			return true;
		}

		public IEnumerator<int> GetEnumerator()
		{
			return ((IEnumerable<int>)_transactions).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<int>)_transactions).GetEnumerator();
		}
	}

	public class CapacityData
	{
		private readonly Func<int> _getCapacity;

		private readonly Func<TextObject> _getCapacityExceededWarningText;

		private readonly Func<TextObject> _getCapacityExceededHintText;

		private readonly bool _forceTransaction;

		public CapacityData(Func<int> getCapacity, Func<TextObject> getCapacityExceededWarningText, Func<TextObject> getCapacityExceededHintText, bool forceTransaction = false)
		{
			_getCapacity = getCapacity;
			_getCapacityExceededWarningText = getCapacityExceededWarningText;
			_getCapacityExceededHintText = getCapacityExceededHintText;
			_forceTransaction = forceTransaction;
		}

		public int GetCapacity()
		{
			return _getCapacity?.Invoke() ?? (-1);
		}

		public bool CanForceTransaction()
		{
			return _forceTransaction;
		}

		public TextObject GetCapacityExceededWarningText()
		{
			return _getCapacityExceededWarningText?.Invoke();
		}

		public TextObject GetCapacityExceededHintText()
		{
			return _getCapacityExceededHintText?.Invoke();
		}
	}

	private class TransactionHistory
	{
		private Dictionary<EquipmentElement, ItemLog> _transactionLogs = new Dictionary<EquipmentElement, ItemLog>();

		public bool IsEmpty => _transactionLogs.IsEmpty();

		internal void RecordTransaction(EquipmentElement elementToTransfer, bool isSelling, int price)
		{
			if (!_transactionLogs.TryGetValue(elementToTransfer, out var value))
			{
				value = new ItemLog();
				_transactionLogs[elementToTransfer] = value;
			}
			value.RecordTransaction(price, isSelling);
		}

		public void Clear()
		{
			_transactionLogs.Clear();
		}

		public bool GetLastTransfer(EquipmentElement equipmentElement, out int lastPrice, out bool lastIsSelling)
		{
			ItemLog value;
			bool num = _transactionLogs.TryGetValue(equipmentElement, out value);
			lastPrice = 0;
			lastIsSelling = false;
			if (!num)
			{
				return false;
			}
			return value.GetLastTransaction(out lastPrice, out lastIsSelling);
		}

		internal List<(ItemRosterElement, int)> GetTransferredItems(bool isSelling)
		{
			List<(ItemRosterElement, int)> list = new List<(ItemRosterElement, int)>();
			foreach (KeyValuePair<EquipmentElement, ItemLog> transactionLog in _transactionLogs)
			{
				if (transactionLog.Value.Count > 0 && !transactionLog.Value.IsSelling == isSelling)
				{
					int item = transactionLog.Value.Sum();
					list.Add((new ItemRosterElement(transactionLog.Key.Item, transactionLog.Value.Count, transactionLog.Key.ItemModifier), item));
				}
			}
			return list;
		}

		internal List<(ItemRosterElement, int)> GetBoughtItems()
		{
			return GetTransferredItems(isSelling: true);
		}

		internal List<(ItemRosterElement, int)> GetSoldItems()
		{
			return GetTransferredItems(isSelling: false);
		}
	}

	private ItemRoster[] _rosters;

	private ItemRoster[] _rostersBackup;

	private IWorkshopWarehouseCampaignBehavior _workshopWarehouseBehavior;

	public bool IsPreviewingItem;

	private PartyEquipment _partyInitialEquipment;

	private PartyEquipment _otherPartyInitialEquipment;

	private float _xpGainFromDonations;

	private int _transactionDebt;

	private bool _playerAcceptsTraderOffer;

	private TransactionHistory _transactionHistory = new TransactionHistory();

	private Dictionary<ItemCategory, float> _itemCategoryAverages = new Dictionary<ItemCategory, float>();

	private bool _useBasePrices;

	public InventoryScreenHelper.InventoryCategoryType MerchantItemType = InventoryScreenHelper.InventoryCategoryType.None;

	private InventoryScreenHelper.InventoryMode _inventoryMode;

	public bool DisableNetwork { get; set; }

	public Action<int> TotalAmountChange { get; set; }

	public Action DonationXpChange { get; set; }

	public TroopRoster RightMemberRoster { get; private set; }

	public TroopRoster LeftMemberRoster { get; private set; }

	public CharacterObject InitialEquipmentCharacter { get; private set; }

	public bool IsTrading { get; private set; }

	public bool IsSpecialActionsPermitted { get; private set; }

	public CharacterObject OwnerCharacter { get; private set; }

	public MobileParty OwnerParty { get; private set; }

	public PartyBase OtherParty { get; private set; }

	public IMarketData MarketData { get; private set; }

	public CapacityData OtherSideCapacityData { get; private set; }

	public int OtherSideCurrentWeight
	{
		get
		{
			float num = 0f;
			MobileParty mobileParty = OtherParty?.MobileParty;
			if (mobileParty != null)
			{
				ItemRoster itemRoster = _rosters[0];
				InventoryCapacityModel inventoryCapacityModel = Campaign.Current.Models.InventoryCapacityModel;
				for (int i = 0; i < itemRoster.Count; i++)
				{
					num += inventoryCapacityModel.GetItemEffectiveWeight(itemRoster[i].EquipmentElement, mobileParty, mobileParty.IsCurrentlyAtSea, out var _) * (float)itemRoster[i].Amount;
				}
			}
			else if (_inventoryMode == InventoryScreenHelper.InventoryMode.Warehouse && _workshopWarehouseBehavior != null)
			{
				num = _workshopWarehouseBehavior.GetWarehouseItemRosterWeight(MobileParty.MainParty.CurrentSettlement);
			}
			return TaleWorlds.Library.MathF.Ceiling(num);
		}
	}

	public TextObject LeftRosterName { get; private set; }

	public bool CanGainXpFromDiscarding { get; private set; }

	public bool IsOtherPartyFromPlayerClan { get; private set; }

	public InventoryListener InventoryListener { get; private set; }

	public int TotalAmount => TransactionDebt;

	public PartyBase OppositePartyFromListener => InventoryListener.GetOppositeParty();

	public SettlementComponent CurrentSettlementComponent => Settlement.CurrentSettlement?.SettlementComponent;

	public MobileParty CurrentMobileParty
	{
		get
		{
			if (PlayerEncounter.Current != null)
			{
				return PlayerEncounter.EncounteredParty.MobileParty;
			}
			if (PartyBase.MainParty.MapEvent?.GetLeaderParty(PartyBase.MainParty.OpponentSide)?.MobileParty != null)
			{
				return PartyBase.MainParty.MapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide).MobileParty;
			}
			return null;
		}
	}

	public int TransactionDebt
	{
		get
		{
			return _transactionDebt;
		}
		private set
		{
			if (value != _transactionDebt)
			{
				_transactionDebt = value;
				TotalAmountChange(_transactionDebt);
			}
		}
	}

	public float XpGainFromDonations
	{
		get
		{
			return _xpGainFromDonations;
		}
		private set
		{
			if (value != _xpGainFromDonations)
			{
				_xpGainFromDonations = value;
				if (_xpGainFromDonations < 0f)
				{
					_xpGainFromDonations = 0f;
				}
				DonationXpChange?.Invoke();
			}
		}
	}

	public event AfterResetDelegate AfterReset;

	public event ProcessResultListDelegate AfterTransfer;

	public InventoryLogic(MobileParty ownerParty, CharacterObject ownerCharacter, PartyBase merchantParty)
	{
		_rosters = new ItemRoster[2];
		_rostersBackup = new ItemRoster[2];
		OwnerParty = ownerParty;
		OwnerCharacter = ownerCharacter;
		OtherParty = merchantParty;
	}

	public InventoryLogic(PartyBase merchantParty)
		: this(MobileParty.MainParty, CharacterObject.PlayerCharacter, merchantParty)
	{
	}

	public void Initialize(ItemRoster leftItemRoster, MobileParty party, bool isTrading, bool isSpecialActionsPermitted, CharacterObject initialCharacterOfRightRoster, InventoryScreenHelper.InventoryCategoryType merchantItemType, IMarketData marketData, bool useBasePrices, InventoryScreenHelper.InventoryMode inventoryMode, TextObject leftRosterName = null, TroopRoster leftMemberRoster = null, CapacityData otherSideCapacityData = null)
	{
		Initialize(leftItemRoster, party.ItemRoster, party.MemberRoster, isTrading, isSpecialActionsPermitted, initialCharacterOfRightRoster, merchantItemType, marketData, useBasePrices, inventoryMode, leftRosterName, leftMemberRoster, otherSideCapacityData);
	}

	public void Initialize(ItemRoster leftItemRoster, ItemRoster rightItemRoster, TroopRoster rightMemberRoster, bool isTrading, bool isSpecialActionsPermitted, CharacterObject initialCharacterOfRightRoster, InventoryScreenHelper.InventoryCategoryType merchantItemType, IMarketData marketData, bool useBasePrices, InventoryScreenHelper.InventoryMode inventoryMode, TextObject leftRosterName = null, TroopRoster leftMemberRoster = null, CapacityData otherSideCapacityData = null)
	{
		OtherSideCapacityData = otherSideCapacityData;
		MarketData = marketData;
		TransactionDebt = 0;
		MerchantItemType = merchantItemType;
		InventoryListener = new FakeInventoryListener();
		_useBasePrices = useBasePrices;
		LeftRosterName = leftRosterName;
		IsTrading = isTrading;
		IsSpecialActionsPermitted = isSpecialActionsPermitted;
		_inventoryMode = inventoryMode;
		IsOtherPartyFromPlayerClan = OtherParty?.MobileParty?.ActualClan == Hero.MainHero.Clan;
		InitializeRosters(leftItemRoster, rightItemRoster, rightMemberRoster, initialCharacterOfRightRoster, leftMemberRoster);
		_transactionHistory.Clear();
		InitializeCategoryAverages();
		CanGainXpFromDiscarding = _inventoryMode == InventoryScreenHelper.InventoryMode.Loot || (_inventoryMode == InventoryScreenHelper.InventoryMode.Default && OtherParty == null);
		InitializeXpGainFromDonations();
		if (_inventoryMode == InventoryScreenHelper.InventoryMode.Warehouse)
		{
			_workshopWarehouseBehavior = Campaign.Current.GetCampaignBehavior<IWorkshopWarehouseCampaignBehavior>();
		}
	}

	private void InitializeRosters(ItemRoster leftItemRoster, ItemRoster rightItemRoster, TroopRoster rightMemberRoster, CharacterObject initialCharacterOfRightRoster, TroopRoster leftMemberRoster = null)
	{
		_rosters[0] = leftItemRoster;
		_rosters[1] = rightItemRoster;
		RightMemberRoster = rightMemberRoster;
		LeftMemberRoster = leftMemberRoster;
		InitialEquipmentCharacter = initialCharacterOfRightRoster;
		SetCurrentStateAsInitial();
	}

	public int GetItemTotalPrice(ItemRosterElement itemRosterElement, int absStockChange, out int lastPrice, bool isBuying)
	{
		lastPrice = GetItemPrice(itemRosterElement.EquipmentElement, isBuying);
		return lastPrice;
	}

	public void SetPlayerAcceptTraderOffer()
	{
		_playerAcceptsTraderOffer = true;
	}

	public bool DoneLogic()
	{
		if (IsPreviewingItem)
		{
			return false;
		}
		SettlementComponent currentSettlementComponent = CurrentSettlementComponent;
		MobileParty currentMobileParty = CurrentMobileParty;
		PartyBase partyBase = null;
		if (currentMobileParty != null)
		{
			partyBase = currentMobileParty.Party;
		}
		else if (currentSettlementComponent != null)
		{
			partyBase = currentSettlementComponent.Owner;
		}
		if (!_playerAcceptsTraderOffer)
		{
			_ = InventoryListener?.GetGold() + TotalAmount < 0;
		}
		if (InventoryListener != null && IsTrading && OwnerCharacter.HeroObject.Gold - TotalAmount < 0)
		{
			MBInformationManager.AddQuickInformation(GameTexts.FindText("str_warning_you_dont_have_enough_money"));
			return false;
		}
		if (_playerAcceptsTraderOffer)
		{
			_playerAcceptsTraderOffer = false;
			if (InventoryListener != null)
			{
				int gold = InventoryListener.GetGold();
				TransactionDebt = -gold;
			}
		}
		if (OwnerCharacter != null && OwnerCharacter.HeroObject != null && IsTrading)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, OwnerCharacter.HeroObject, TaleWorlds.Library.MathF.Min(-TotalAmount, InventoryListener.GetGold()));
			if (currentSettlementComponent != null && currentSettlementComponent.IsTown && OwnerCharacter.GetPerkValue(DefaultPerks.Trade.TrickleDown))
			{
				int num = 0;
				List<(ItemRosterElement, int)> boughtItems = _transactionHistory.GetBoughtItems();
				int num2 = 0;
				while (boughtItems != null && num2 < boughtItems.Count)
				{
					ItemObject item = boughtItems[num2].Item1.EquipmentElement.Item;
					if (item != null && item.IsTradeGood)
					{
						num += boughtItems[num2].Item2;
					}
					num2++;
				}
				if (num >= 10000)
				{
					for (int i = 0; i < currentSettlementComponent.Settlement.Notables.Count; i++)
					{
						if (currentSettlementComponent.Settlement.Notables[i].IsMerchant)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(currentSettlementComponent.Settlement.Notables[i], OwnerCharacter.HeroObject, TaleWorlds.Library.MathF.Floor(DefaultPerks.Trade.TrickleDown.PrimaryBonus));
						}
					}
				}
			}
		}
		if (CanGainXpFromDiscarding)
		{
			CampaignEventDispatcher.Instance.OnItemsDiscardedByPlayer(_rosters[0]);
		}
		CampaignEventDispatcher.Instance.OnPlayerInventoryExchange(_transactionHistory.GetBoughtItems(), _transactionHistory.GetSoldItems(), IsTrading);
		if (currentSettlementComponent != null && InventoryListener != null && IsTrading)
		{
			InventoryListener.SetGold(InventoryListener.GetGold() + TotalAmount);
		}
		else if (currentMobileParty?.Party.LeaderHero != null && IsTrading)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, CurrentMobileParty.Party.LeaderHero, TotalAmount);
			if (CurrentMobileParty.Party.LeaderHero.CompanionOf != null)
			{
				CurrentMobileParty.AddTaxGold((int)((float)TotalAmount * 0.1f));
			}
		}
		else if (partyBase != null && partyBase.LeaderHero == null && IsTrading)
		{
			GiveGoldAction.ApplyForCharacterToParty(null, partyBase, TotalAmount);
		}
		_partyInitialEquipment = new PartyEquipment(OwnerParty);
		if (IsOtherPartyFromPlayerClan && LeftMemberRoster != null)
		{
			_otherPartyInitialEquipment = new PartyEquipment(OtherParty.MobileParty);
		}
		return true;
	}

	public List<(ItemRosterElement, int)> GetBoughtItems()
	{
		return _transactionHistory.GetBoughtItems();
	}

	public List<(ItemRosterElement, int)> GetSoldItems()
	{
		return _transactionHistory.GetSoldItems();
	}

	public bool CanInventoryCapacityIncrease(InventorySide side)
	{
		if (_inventoryMode == InventoryScreenHelper.InventoryMode.Warehouse)
		{
			return side != InventorySide.OtherInventory;
		}
		return true;
	}

	public bool GetCanItemIncreaseInventoryCapacity(ItemObject item)
	{
		return item.HasHorseComponent;
	}

	private void InitializeCategoryAverages()
	{
		if (Campaign.Current == null || Settlement.CurrentSettlement == null)
		{
			return;
		}
		Town town = (Settlement.CurrentSettlement.IsVillage ? Settlement.CurrentSettlement.Village.Bound.Town : Settlement.CurrentSettlement.Town);
		foreach (ItemCategory item in ItemCategories.All)
		{
			float num = 0f;
			for (int i = 0; i < Town.AllTowns.Count; i++)
			{
				if (Town.AllTowns[i] != town)
				{
					num += Town.AllTowns[i].MarketData.GetPriceFactor(item);
				}
			}
			float num2 = num / (float)(Town.AllTowns.Count - 1);
			_itemCategoryAverages.Add(item, num2);
			Debug.Print($"Average value of {item.GetName()} : {num2}");
		}
	}

	private void InitializeXpGainFromDonations()
	{
		XpGainFromDonations = 0f;
		bool num = PerkHelper.PlayerHasAnyItemDonationPerk();
		bool flag = _inventoryMode == InventoryScreenHelper.InventoryMode.Loot;
		if (num && flag)
		{
			XpGainFromDonations = Campaign.Current.Models.ItemDiscardModel.GetXpBonusForDiscardingItems(_rosters[0]);
		}
	}

	private void HandleDonationOnTransferItem(ItemRosterElement rosterElement, int amount, bool isBuying, bool isSelling)
	{
		ItemObject item = rosterElement.EquipmentElement.Item;
		ItemDiscardModel itemDiscardModel = Campaign.Current.Models.ItemDiscardModel;
		if (CanGainXpFromDiscarding && (isSelling || isBuying) && item != null)
		{
			XpGainFromDonations += itemDiscardModel.GetXpBonusForDiscardingItem(item, amount) * (isSelling ? 1 : (-1));
		}
	}

	public float GetAveragePriceFactorItemCategory(ItemCategory category)
	{
		if (_itemCategoryAverages.ContainsKey(category))
		{
			return _itemCategoryAverages[category];
		}
		return -99f;
	}

	public bool IsThereAnyChanges()
	{
		if (!IsThereAnyChangeBetweenRosters(_rosters[1], _rostersBackup[1]) && _partyInitialEquipment.IsEqual(new PartyEquipment(OwnerParty)))
		{
			PartyEquipment otherPartyInitialEquipment = _otherPartyInitialEquipment;
			if (otherPartyInitialEquipment == null)
			{
				return false;
			}
			return !otherPartyInitialEquipment.IsEqual(new PartyEquipment(OtherParty?.MobileParty));
		}
		return true;
	}

	private bool IsThereAnyChangeBetweenRosters(ItemRoster roster1, ItemRoster roster2)
	{
		if (roster1.Count != roster2.Count)
		{
			return true;
		}
		foreach (ItemRosterElement item in roster1)
		{
			if (!roster2.Any((ItemRosterElement e) => e.IsEqualTo(item)))
			{
				return true;
			}
		}
		return false;
	}

	public void Reset(bool fromCancel)
	{
		ResetLogic(fromCancel);
	}

	private void ResetLogic(bool fromCancel)
	{
		Debug.Print("InventoryLogic::Reset");
		for (int i = 0; i < 2; i++)
		{
			_rosters[i].Clear();
			_rosters[i].Add(_rostersBackup[i]);
		}
		TransactionDebt = 0;
		_transactionHistory.Clear();
		InitializeXpGainFromDonations();
		_partyInitialEquipment.ResetEquipment();
		_otherPartyInitialEquipment?.ResetEquipment();
		this.AfterReset?.Invoke(this, fromCancel);
		List<TransferCommandResult> resultList = new List<TransferCommandResult>();
		if (!fromCancel)
		{
			OnAfterTransfer(resultList);
		}
	}

	public bool CanPlayerCompleteTransaction()
	{
		int num = OtherSideCapacityData?.GetCapacity() ?? (-1);
		if (num != -1 && OtherSideCurrentWeight > num && !OtherSideCapacityData.CanForceTransaction())
		{
			return false;
		}
		if (IsPreviewingItem && IsTrading && TotalAmount > 0)
		{
			if (TotalAmount >= 0)
			{
				return OwnerCharacter.HeroObject.Gold - TotalAmount >= 0;
			}
			return false;
		}
		return true;
	}

	public bool CanSlaughterItem(ItemRosterElement element, InventorySide sideOfItem)
	{
		if (IsTrading && !_transactionHistory.IsEmpty)
		{
			return false;
		}
		if (IsSpecialActionsPermitted && IsSlaughterable(element.EquipmentElement.Item) && sideOfItem == InventorySide.PlayerInventory && element.Amount > 0)
		{
			return !_transactionHistory.GetBoughtItems().Any(((ItemRosterElement, int) i) => i.Item1.EquipmentElement.Item == element.EquipmentElement.Item);
		}
		return false;
	}

	public bool IsSlaughterable(ItemObject item)
	{
		if (item.Type != ItemObject.ItemTypeEnum.Animal)
		{
			return item.Type == ItemObject.ItemTypeEnum.Horse;
		}
		return true;
	}

	public bool CanDonateItem(ItemRosterElement element, InventorySide sideOfItem)
	{
		if (Game.Current.IsDevelopmentMode && IsSpecialActionsPermitted && element.Amount > 0 && IsDonatable(element.EquipmentElement.Item))
		{
			return sideOfItem == InventorySide.PlayerInventory;
		}
		return false;
	}

	public bool IsDonatable(ItemObject item)
	{
		if (item.Type != ItemObject.ItemTypeEnum.Arrows && item.Type != ItemObject.ItemTypeEnum.BodyArmor && item.Type != ItemObject.ItemTypeEnum.Bolts && item.Type != ItemObject.ItemTypeEnum.SlingStones && item.Type != ItemObject.ItemTypeEnum.Bow && item.Type != ItemObject.ItemTypeEnum.Bullets && item.Type != ItemObject.ItemTypeEnum.Cape && item.Type != ItemObject.ItemTypeEnum.ChestArmor && item.Type != ItemObject.ItemTypeEnum.Crossbow && item.Type != ItemObject.ItemTypeEnum.Sling && item.Type != ItemObject.ItemTypeEnum.HandArmor && item.Type != ItemObject.ItemTypeEnum.HeadArmor && item.Type != ItemObject.ItemTypeEnum.HorseHarness && item.Type != ItemObject.ItemTypeEnum.LegArmor && item.Type != ItemObject.ItemTypeEnum.Musket && item.Type != ItemObject.ItemTypeEnum.OneHandedWeapon && item.Type != ItemObject.ItemTypeEnum.Pistol && item.Type != ItemObject.ItemTypeEnum.Polearm && item.Type != ItemObject.ItemTypeEnum.Shield && item.Type != ItemObject.ItemTypeEnum.Thrown)
		{
			return item.Type == ItemObject.ItemTypeEnum.TwoHandedWeapon;
		}
		return true;
	}

	public void SetInventoryListener(InventoryListener inventoryListener)
	{
		InventoryListener = inventoryListener;
	}

	public int GetItemPrice(EquipmentElement equipmentElement, bool isBuying = false)
	{
		bool flag = !isBuying;
		bool flag2 = false;
		int result = 0;
		if (_transactionHistory.GetLastTransfer(equipmentElement, out var lastPrice, out var lastIsSelling) && lastIsSelling != flag)
		{
			flag2 = true;
			result = lastPrice;
		}
		if (_useBasePrices)
		{
			return equipmentElement.GetBaseValue();
		}
		if (flag2)
		{
			return result;
		}
		return MarketData.GetPrice(equipmentElement, OwnerParty, flag, OtherParty);
	}

	public int GetCostOfItemRosterElement(ItemRosterElement itemRosterElement, InventorySide side)
	{
		bool isBuying = side == InventorySide.OtherInventory && IsTrading;
		return GetItemPrice(itemRosterElement.EquipmentElement, isBuying);
	}

	private void OnAfterTransfer(List<TransferCommandResult> resultList)
	{
		this.AfterTransfer?.Invoke(this, resultList);
		foreach (TransferCommandResult result in resultList)
		{
			if (result.EffectedNumber > 0)
			{
				Game.Current.EventManager.TriggerEvent(new InventoryTransferItemEvent(result.EffectedItemRosterElement.EquipmentElement.Item, result.ResultSide == InventorySide.PlayerInventory));
			}
		}
	}

	public void AddTransferCommand(TransferCommand command)
	{
		ProcessTransferCommand(command);
	}

	public void AddTransferCommands(IEnumerable<TransferCommand> commands)
	{
		foreach (TransferCommand command in commands)
		{
			ProcessTransferCommand(command);
		}
	}

	public bool CheckItemRosterHasElement(InventorySide side, ItemRosterElement rosterElement, int number)
	{
		int num = _rosters[(int)side].FindIndexOfElement(rosterElement.EquipmentElement);
		if (num != -1)
		{
			return _rosters[(int)side].GetElementCopyAtIndex(num).Amount >= number;
		}
		return false;
	}

	private void ProcessTransferCommand(TransferCommand command)
	{
		List<TransferCommandResult> resultList = TransferItem(ref command);
		OnAfterTransfer(resultList);
	}

	private List<TransferCommandResult> TransferItem(ref TransferCommand transferCommand)
	{
		List<TransferCommandResult> list = new List<TransferCommandResult>();
		Debug.Print(string.Format("TransferItem Name: {0} | From: {1} To: {2} | Amount: {3}", transferCommand.ElementToTransfer.EquipmentElement.Item?.Name.ToString() ?? "null", transferCommand.FromSide, transferCommand.ToSide, transferCommand.Amount));
		if (transferCommand.ElementToTransfer.EquipmentElement.Item != null && TransferIsMovementValid(ref transferCommand) && DoesTransferItemExist(ref transferCommand))
		{
			int num = 0;
			bool flag = false;
			if (!IsEquipmentSide(transferCommand.FromSide) && transferCommand.FromSide != InventorySide.None)
			{
				int index = _rosters[(int)transferCommand.FromSide].FindIndexOfElement(transferCommand.ElementToTransfer.EquipmentElement);
				ItemRosterElement elementCopyAtIndex = _rosters[(int)transferCommand.FromSide].GetElementCopyAtIndex(index);
				flag = transferCommand.Amount == elementCopyAtIndex.Amount;
			}
			bool flag2 = IsSell(transferCommand.FromSide, transferCommand.ToSide);
			bool flag3 = IsBuy(transferCommand.FromSide, transferCommand.ToSide);
			for (int i = 0; i < transferCommand.Amount; i++)
			{
				if (IsEquipmentSide(transferCommand.ToSide) && transferCommand.ToSideEquipment[(int)transferCommand.ToEquipmentIndex].Item != null)
				{
					TransferCommand transferCommand2 = TransferCommand.Transfer(1, transferCommand.ToSide, InventorySide.PlayerInventory, new ItemRosterElement(transferCommand.ToSideEquipment[(int)transferCommand.ToEquipmentIndex], 1), transferCommand.ToEquipmentIndex, EquipmentIndex.None, transferCommand.Character);
					list.AddRange(TransferItem(ref transferCommand2));
				}
				EquipmentElement equipmentElement = transferCommand.ElementToTransfer.EquipmentElement;
				int itemPrice = GetItemPrice(equipmentElement, flag3);
				if (flag3 || flag2)
				{
					_transactionHistory.RecordTransaction(equipmentElement, flag2, itemPrice);
				}
				if (IsTrading)
				{
					if (flag3)
					{
						num += itemPrice;
					}
					else if (flag2)
					{
						num -= itemPrice;
					}
				}
				if (IsEquipmentSide(transferCommand.FromSide))
				{
					ItemRosterElement itemRosterElement = new ItemRosterElement(transferCommand.FromSideEquipment[(int)transferCommand.FromEquipmentIndex], transferCommand.Amount);
					itemRosterElement.Amount--;
					transferCommand.FromSideEquipment[(int)transferCommand.FromEquipmentIndex] = itemRosterElement.EquipmentElement;
				}
				else if (transferCommand.FromSide == InventorySide.PlayerInventory || transferCommand.FromSide == InventorySide.OtherInventory)
				{
					_rosters[(int)transferCommand.FromSide].AddToCounts(transferCommand.ElementToTransfer.EquipmentElement, -1);
				}
				if (IsEquipmentSide(transferCommand.ToSide))
				{
					ItemRosterElement elementToTransfer = transferCommand.ElementToTransfer;
					elementToTransfer.Amount = 1;
					transferCommand.ToSideEquipment[(int)transferCommand.ToEquipmentIndex] = elementToTransfer.EquipmentElement;
				}
				else if (transferCommand.ToSide == InventorySide.PlayerInventory || transferCommand.ToSide == InventorySide.OtherInventory)
				{
					_rosters[(int)transferCommand.ToSide].AddToCounts(transferCommand.ElementToTransfer.EquipmentElement, 1);
				}
			}
			if (IsEquipmentSide(transferCommand.FromSide))
			{
				ItemRosterElement effectedItemRosterElement = new ItemRosterElement(transferCommand.FromSideEquipment[(int)transferCommand.FromEquipmentIndex], transferCommand.Amount);
				effectedItemRosterElement.Amount--;
				list.Add(new TransferCommandResult(transferCommand.FromSide, effectedItemRosterElement, -transferCommand.Amount, effectedItemRosterElement.Amount, transferCommand.FromEquipmentIndex, transferCommand.Character));
			}
			else if (transferCommand.FromSide == InventorySide.PlayerInventory || transferCommand.FromSide == InventorySide.OtherInventory)
			{
				if (flag)
				{
					list.Add(new TransferCommandResult(transferCommand.FromSide, new ItemRosterElement(transferCommand.ElementToTransfer.EquipmentElement, 0), -transferCommand.Amount, 0, transferCommand.FromEquipmentIndex, transferCommand.Character));
				}
				else
				{
					int index2 = _rosters[(int)transferCommand.FromSide].FindIndexOfElement(transferCommand.ElementToTransfer.EquipmentElement);
					ItemRosterElement elementCopyAtIndex2 = _rosters[(int)transferCommand.FromSide].GetElementCopyAtIndex(index2);
					list.Add(new TransferCommandResult(transferCommand.FromSide, elementCopyAtIndex2, -transferCommand.Amount, elementCopyAtIndex2.Amount, transferCommand.FromEquipmentIndex, transferCommand.Character));
				}
			}
			if (IsEquipmentSide(transferCommand.ToSide))
			{
				ItemRosterElement elementToTransfer2 = transferCommand.ElementToTransfer;
				elementToTransfer2.Amount = 1;
				list.Add(new TransferCommandResult(transferCommand.ToSide, elementToTransfer2, 1, 1, transferCommand.ToEquipmentIndex, transferCommand.Character));
			}
			else if (transferCommand.ToSide == InventorySide.PlayerInventory || transferCommand.ToSide == InventorySide.OtherInventory)
			{
				int index3 = _rosters[(int)transferCommand.ToSide].FindIndexOfElement(transferCommand.ElementToTransfer.EquipmentElement);
				ItemRosterElement elementCopyAtIndex3 = _rosters[(int)transferCommand.ToSide].GetElementCopyAtIndex(index3);
				list.Add(new TransferCommandResult(transferCommand.ToSide, elementCopyAtIndex3, transferCommand.Amount, elementCopyAtIndex3.Amount, transferCommand.ToEquipmentIndex, transferCommand.Character));
			}
			HandleDonationOnTransferItem(transferCommand.ElementToTransfer, transferCommand.Amount, flag3, flag2);
			TransactionDebt += num;
		}
		return list;
	}

	public static bool IsEquipmentSide(InventorySide side)
	{
		if (side != InventorySide.CivilianEquipment && side != InventorySide.BattleEquipment)
		{
			return side == InventorySide.StealthEquipment;
		}
		return true;
	}

	private bool IsSell(InventorySide fromSide, InventorySide toSide)
	{
		if (toSide == InventorySide.OtherInventory)
		{
			if (!IsEquipmentSide(fromSide))
			{
				return fromSide == InventorySide.PlayerInventory;
			}
			return true;
		}
		return false;
	}

	private bool IsBuy(InventorySide fromSide, InventorySide toSide)
	{
		if (fromSide == InventorySide.OtherInventory)
		{
			if (!IsEquipmentSide(toSide))
			{
				return toSide == InventorySide.PlayerInventory;
			}
			return true;
		}
		return false;
	}

	public void SlaughterItem(ItemRosterElement itemRosterElement)
	{
		List<TransferCommandResult> list = new List<TransferCommandResult>();
		EquipmentElement equipmentElement = itemRosterElement.EquipmentElement;
		int meatCount = equipmentElement.Item.HorseComponent.MeatCount;
		int hideCount = equipmentElement.Item.HorseComponent.HideCount;
		int index = _rosters[1].AddToCounts(DefaultItems.Meat, meatCount);
		ItemRosterElement elementCopyAtIndex = _rosters[1].GetElementCopyAtIndex(index);
		bool num = itemRosterElement.Amount == 1;
		int index2 = _rosters[1].AddToCounts(itemRosterElement.EquipmentElement, -1);
		if (num)
		{
			list.Add(new TransferCommandResult(InventorySide.PlayerInventory, new ItemRosterElement(equipmentElement, 0), -1, 0, EquipmentIndex.None, null));
		}
		else
		{
			ItemRosterElement elementCopyAtIndex2 = _rosters[1].GetElementCopyAtIndex(index2);
			list.Add(new TransferCommandResult(InventorySide.PlayerInventory, elementCopyAtIndex2, -1, elementCopyAtIndex2.Amount, EquipmentIndex.None, null));
		}
		list.Add(new TransferCommandResult(InventorySide.PlayerInventory, elementCopyAtIndex, meatCount, elementCopyAtIndex.Amount, EquipmentIndex.None, null));
		if (hideCount > 0)
		{
			int index3 = _rosters[1].AddToCounts(DefaultItems.Hides, hideCount);
			ItemRosterElement elementCopyAtIndex3 = _rosters[1].GetElementCopyAtIndex(index3);
			list.Add(new TransferCommandResult(InventorySide.PlayerInventory, elementCopyAtIndex3, hideCount, elementCopyAtIndex3.Amount, EquipmentIndex.None, null));
		}
		SetCurrentStateAsInitial();
		OnAfterTransfer(list);
	}

	public void DonateItem(ItemRosterElement itemRosterElement)
	{
		List<TransferCommandResult> list = new List<TransferCommandResult>();
		int tier = (int)itemRosterElement.EquipmentElement.Item.Tier;
		int num = 100 * (tier + 1);
		InventorySide inventorySide = InventorySide.PlayerInventory;
		int index = _rosters[(int)inventorySide].AddToCounts(itemRosterElement.EquipmentElement, -1);
		ItemRosterElement elementCopyAtIndex = _rosters[(int)inventorySide].GetElementCopyAtIndex(index);
		list.Add(new TransferCommandResult(InventorySide.PlayerInventory, elementCopyAtIndex, -1, elementCopyAtIndex.Amount, EquipmentIndex.None, null));
		if (num > 0)
		{
			TroopRosterElement randomElementWithPredicate = PartyBase.MainParty.MemberRoster.GetTroopRoster().GetRandomElementWithPredicate((TroopRosterElement m) => !m.Character.IsHero && m.Character.UpgradeTargets.Length != 0);
			if (randomElementWithPredicate.Character != null)
			{
				PartyBase.MainParty.MemberRoster.AddXpToTroop(randomElementWithPredicate.Character, num);
				TextObject textObject = new TextObject("{=Kwja0a4s}Added {XPAMOUNT} amount of xp to {TROOPNAME}");
				textObject.SetTextVariable("XPAMOUNT", num);
				textObject.SetTextVariable("TROOPNAME", randomElementWithPredicate.Character.Name.ToString());
				Debug.Print(textObject.ToString());
				MBInformationManager.AddQuickInformation(textObject);
			}
		}
		SetCurrentStateAsInitial();
		OnAfterTransfer(list);
	}

	private static bool TransferIsMovementValid(ref TransferCommand transferCommand)
	{
		if (transferCommand.ElementToTransfer.EquipmentElement.IsQuestItem && (transferCommand.ElementToTransfer.EquipmentElement.Item.BannerComponent?.BannerEffect == null || ((transferCommand.FromSide != InventorySide.PlayerInventory || !IsEquipmentSide(transferCommand.ToSide)) && (!IsEquipmentSide(transferCommand.FromSide) || transferCommand.ToSide != InventorySide.PlayerInventory))))
		{
			return false;
		}
		bool result = false;
		if (IsEquipmentSide(transferCommand.ToSide))
		{
			InventoryScreenHelper.InventoryItemType inventoryItemTypeOfItem = InventoryScreenHelper.GetInventoryItemTypeOfItem(transferCommand.ElementToTransfer.EquipmentElement.Item);
			switch (transferCommand.ToEquipmentIndex)
			{
			case EquipmentIndex.WeaponItemBeginSlot:
			case EquipmentIndex.Weapon1:
			case EquipmentIndex.Weapon2:
			case EquipmentIndex.Weapon3:
				result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Weapon || inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Shield;
				break;
			case EquipmentIndex.NumAllWeaponSlots:
				result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.HeadArmor;
				break;
			case EquipmentIndex.Body:
				result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.BodyArmor;
				break;
			case EquipmentIndex.Leg:
				result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.LegArmor;
				break;
			case EquipmentIndex.Gloves:
				result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.HandArmor;
				break;
			case EquipmentIndex.Cape:
				result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Cape;
				break;
			case EquipmentIndex.ArmorItemEndSlot:
				result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Horse;
				break;
			case EquipmentIndex.HorseHarness:
				result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.HorseHarness;
				break;
			case EquipmentIndex.ExtraWeaponSlot:
				result = inventoryItemTypeOfItem == InventoryScreenHelper.InventoryItemType.Banner;
				break;
			}
		}
		else
		{
			result = true;
		}
		return result;
	}

	private bool DoesTransferItemExist(ref TransferCommand transferCommand)
	{
		if (transferCommand.FromSide == InventorySide.OtherInventory || transferCommand.FromSide == InventorySide.PlayerInventory)
		{
			return CheckItemRosterHasElement(transferCommand.FromSide, transferCommand.ElementToTransfer, transferCommand.Amount);
		}
		if (transferCommand.FromSide != InventorySide.None)
		{
			if (transferCommand.FromSideEquipment[(int)transferCommand.FromEquipmentIndex].Item != null)
			{
				return transferCommand.ElementToTransfer.EquipmentElement.IsEqualTo(transferCommand.FromSideEquipment[(int)transferCommand.FromEquipmentIndex]);
			}
			return false;
		}
		return false;
	}

	public void TransferOne(ItemRosterElement itemRosterElement)
	{
	}

	public int GetElementCountOnSide(InventorySide side)
	{
		return _rosters[(int)side].Count;
	}

	public IReadOnlyList<ItemRosterElement> GetElementsInInitialRoster(InventorySide side)
	{
		return _rostersBackup[(int)side];
	}

	public IReadOnlyList<ItemRosterElement> GetElementsInRoster(InventorySide side)
	{
		return _rosters[(int)side];
	}

	private void SetCurrentStateAsInitial()
	{
		for (int i = 0; i < _rostersBackup.Length; i++)
		{
			_rostersBackup[i] = new ItemRoster(_rosters[i]);
		}
		_partyInitialEquipment = new PartyEquipment(OwnerParty);
		if (IsOtherPartyFromPlayerClan && LeftMemberRoster != null)
		{
			_otherPartyInitialEquipment = new PartyEquipment(OtherParty.MobileParty);
		}
	}

	public ItemRosterElement? FindItemFromSide(InventorySide side, EquipmentElement item)
	{
		int num = _rosters[(int)side].FindIndexOfElement(item);
		if (num >= 0)
		{
			return _rosters[(int)side].ElementAt(num);
		}
		return null;
	}
}
