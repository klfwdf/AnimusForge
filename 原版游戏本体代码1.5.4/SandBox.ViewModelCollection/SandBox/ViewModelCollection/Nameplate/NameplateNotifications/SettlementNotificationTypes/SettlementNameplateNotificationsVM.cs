using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes;

public class SettlementNameplateNotificationsVM : ViewModel
{
	private readonly Settlement _settlement;

	private int _tickSinceEnabled;

	private const int _maxTickDeltaToCongregate = 10;

	private MBBindingList<SettlementNotificationItemBaseVM> _notifications;

	public bool IsEventsRegistered { get; private set; }

	public MBBindingList<SettlementNotificationItemBaseVM> Notifications
	{
		get
		{
			return _notifications;
		}
		set
		{
			if (value != _notifications)
			{
				_notifications = value;
				OnPropertyChangedWithValue(value, "Notifications");
			}
		}
	}

	public SettlementNameplateNotificationsVM(Settlement settlement)
	{
		_settlement = settlement;
		Notifications = new MBBindingList<SettlementNotificationItemBaseVM>();
	}

	public void Tick()
	{
		_tickSinceEnabled++;
	}

	private void OnTroopRecruited(Hero recruiterHero, Settlement settlement, Hero troopSource, CharacterObject troop, int amount)
	{
		if (amount > 0 && settlement == _settlement && _settlement.IsInspected && recruiterHero != null && (recruiterHero.CurrentSettlement == _settlement || (recruiterHero.PartyBelongedTo != null && recruiterHero.PartyBelongedTo.LastVisitedSettlement == _settlement)))
		{
			TroopRecruitmentNotificationItemVM updatableNotificationByPredicate = GetUpdatableNotificationByPredicate((TroopRecruitmentNotificationItemVM n) => n.RecruiterHero == recruiterHero);
			if (updatableNotificationByPredicate != null)
			{
				updatableNotificationByPredicate.AddNewAction(amount);
			}
			else
			{
				Notifications.Add(new TroopRecruitmentNotificationItemVM(RemoveItem, recruiterHero, amount, _tickSinceEnabled));
			}
		}
	}

	private void OnCaravanTransactionCompleted(MobileParty caravanParty, Town town, List<(EquipmentElement, int)> items)
	{
		if (_settlement == town.Owner.Settlement)
		{
			CaravanTransactionNotificationItemVM updatableNotificationByPredicate = GetUpdatableNotificationByPredicate((CaravanTransactionNotificationItemVM n) => n.CaravanParty == caravanParty);
			if (updatableNotificationByPredicate != null)
			{
				updatableNotificationByPredicate.AddNewItems(items);
			}
			else
			{
				Notifications.Add(new CaravanTransactionNotificationItemVM(RemoveItem, caravanParty, items, _tickSinceEnabled));
			}
		}
	}

	private void OnPrisonerSold(PartyBase sellerParty, PartyBase buyerParty, TroopRoster prisoners)
	{
		if (sellerParty.IsMobile && buyerParty != null && buyerParty.IsSettlement && buyerParty.Settlement == _settlement && _settlement.IsInspected && prisoners.Count > 0 && sellerParty.LeaderHero != null)
		{
			MobileParty sellerMobileParty = sellerParty.MobileParty;
			PrisonerSoldNotificationItemVM updatableNotificationByPredicate = GetUpdatableNotificationByPredicate((PrisonerSoldNotificationItemVM n) => n.Party == sellerMobileParty);
			if (updatableNotificationByPredicate != null)
			{
				updatableNotificationByPredicate.AddNewPrisoners(prisoners);
			}
			else
			{
				Notifications.Add(new PrisonerSoldNotificationItemVM(RemoveItem, sellerMobileParty, prisoners, _tickSinceEnabled));
			}
		}
	}

	private void OnTroopGivenToSettlement(Hero giverHero, Settlement givenSettlement, TroopRoster givenTroops)
	{
		if (_settlement.IsInspected && givenTroops.TotalManCount > 0 && giverHero != null && givenSettlement == _settlement)
		{
			TroopGivenToSettlementNotificationItemVM updatableNotificationByPredicate = GetUpdatableNotificationByPredicate((TroopGivenToSettlementNotificationItemVM n) => n.GiverHero == giverHero);
			if (updatableNotificationByPredicate != null)
			{
				updatableNotificationByPredicate.AddNewAction(givenTroops);
			}
			else
			{
				Notifications.Add(new TroopGivenToSettlementNotificationItemVM(RemoveItem, giverHero, givenTroops, _tickSinceEnabled));
			}
		}
	}

	private void OnItemSold(PartyBase receiverParty, PartyBase payerParty, ItemRosterElement item, int number, Settlement currentSettlement)
	{
		if (_settlement.IsInspected && number > 0 && currentSettlement == _settlement)
		{
			int num = ((!receiverParty.IsSettlement) ? 1 : (-1));
			ItemSoldNotificationItemVM updatableNotificationByPredicate = GetUpdatableNotificationByPredicate((ItemSoldNotificationItemVM n) => n.Item.EquipmentElement.Item == item.EquipmentElement.Item && (n.PayerParty == receiverParty || n.PayerParty == payerParty));
			if (updatableNotificationByPredicate != null)
			{
				updatableNotificationByPredicate.AddNewTransaction(number * num);
			}
			else
			{
				Notifications.Add(new ItemSoldNotificationItemVM(RemoveItem, receiverParty, payerParty, item, number * num, _tickSinceEnabled));
			}
		}
	}

	private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails updateType, Hero relatedHero)
	{
		if (updateType == IssueBase.IssueUpdateDetails.IssueFinishedByAILord && relatedHero != null && relatedHero.CurrentSettlement == _settlement)
		{
			Notifications.Add(new IssueSolvedByLordNotificationItemVM(RemoveItem, relatedHero, _tickSinceEnabled));
		}
	}

	private void OnShipOwnerChanged(Ship ship, PartyBase oldOwner, ChangeShipOwnerAction.ShipOwnerChangeDetail detail)
	{
		if (_settlement.IsInspected && detail == ChangeShipOwnerAction.ShipOwnerChangeDetail.ApplyByTrade && (oldOwner == _settlement.Party || ship.Owner == _settlement.Party))
		{
			bool flag = ship.Owner == _settlement.Party;
			int amount = ((!flag) ? 1 : (-1));
			PartyBase heroParty = (flag ? oldOwner : ship.Owner);
			ShipSoldNotificationItemVM updatableNotificationByPredicate = GetUpdatableNotificationByPredicate((ShipSoldNotificationItemVM n) => n.Ship.ShipHull.Name == ship.ShipHull.Name && n.SettlementParty == _settlement.Party && n.HeroParty == heroParty);
			if (updatableNotificationByPredicate != null)
			{
				updatableNotificationByPredicate.AddNewTransaction(amount);
			}
			else
			{
				Notifications.Add(new ShipSoldNotificationItemVM(RemoveItem, ship, _settlement.Party, heroParty, amount, _tickSinceEnabled));
			}
		}
	}

	private void RemoveItem(SettlementNotificationItemBaseVM item)
	{
		Notifications.Remove(item);
	}

	public void RegisterEvents()
	{
		if (!IsEventsRegistered)
		{
			CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
			CampaignEvents.OnPrisonerSoldEvent.AddNonSerializedListener(this, OnPrisonerSold);
			CampaignEvents.OnCaravanTransactionCompletedEvent.AddNonSerializedListener(this, OnCaravanTransactionCompleted);
			CampaignEvents.OnTroopGivenToSettlementEvent.AddNonSerializedListener(this, OnTroopGivenToSettlement);
			CampaignEvents.OnItemSoldEvent.AddNonSerializedListener(this, OnItemSold);
			CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueUpdated);
			CampaignEvents.OnShipOwnerChangedEvent.AddNonSerializedListener(this, OnShipOwnerChanged);
			_tickSinceEnabled = 0;
			IsEventsRegistered = true;
		}
	}

	public void UnloadEvents()
	{
		if (IsEventsRegistered)
		{
			CampaignEvents.OnTroopRecruitedEvent.ClearListeners(this);
			CampaignEvents.OnItemSoldEvent.ClearListeners(this);
			CampaignEvents.OnPrisonerSoldEvent.ClearListeners(this);
			CampaignEvents.OnCaravanTransactionCompletedEvent.ClearListeners(this);
			CampaignEvents.OnTroopGivenToSettlementEvent.ClearListeners(this);
			CampaignEvents.OnIssueUpdatedEvent.ClearListeners(this);
			CampaignEvents.OnShipOwnerChangedEvent.ClearListeners(this);
			_tickSinceEnabled = 0;
			IsEventsRegistered = false;
		}
	}

	public bool IsValidItemForNotification(ItemRosterElement item)
	{
		switch (item.EquipmentElement.Item.Type)
		{
		case ItemObject.ItemTypeEnum.Horse:
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
		case ItemObject.ItemTypeEnum.Polearm:
		case ItemObject.ItemTypeEnum.Arrows:
		case ItemObject.ItemTypeEnum.Bolts:
		case ItemObject.ItemTypeEnum.SlingStones:
		case ItemObject.ItemTypeEnum.Shield:
		case ItemObject.ItemTypeEnum.Bow:
		case ItemObject.ItemTypeEnum.Crossbow:
		case ItemObject.ItemTypeEnum.Sling:
		case ItemObject.ItemTypeEnum.Thrown:
		case ItemObject.ItemTypeEnum.Goods:
		case ItemObject.ItemTypeEnum.HeadArmor:
		case ItemObject.ItemTypeEnum.BodyArmor:
		case ItemObject.ItemTypeEnum.LegArmor:
		case ItemObject.ItemTypeEnum.HandArmor:
		case ItemObject.ItemTypeEnum.Pistol:
		case ItemObject.ItemTypeEnum.Musket:
		case ItemObject.ItemTypeEnum.Bullets:
		case ItemObject.ItemTypeEnum.Animal:
		case ItemObject.ItemTypeEnum.ChestArmor:
		case ItemObject.ItemTypeEnum.Cape:
		case ItemObject.ItemTypeEnum.HorseHarness:
			return true;
		default:
			return false;
		}
	}

	private T GetUpdatableNotificationByPredicate<T>(Func<T, bool> predicate) where T : SettlementNotificationItemBaseVM
	{
		for (int i = 0; i < Notifications.Count; i++)
		{
			SettlementNotificationItemBaseVM settlementNotificationItemBaseVM = Notifications[i];
			if (_tickSinceEnabled - settlementNotificationItemBaseVM.CreatedTick < 10 && settlementNotificationItemBaseVM is T val && predicate(val))
			{
				return val;
			}
		}
		return null;
	}
}
