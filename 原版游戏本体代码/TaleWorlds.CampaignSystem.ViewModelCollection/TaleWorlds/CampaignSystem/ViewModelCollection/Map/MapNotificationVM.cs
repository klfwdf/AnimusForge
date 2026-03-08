using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map
{
	// Token: 0x02000036 RID: 54
	public class MapNotificationVM : ViewModel
	{
		// Token: 0x14000002 RID: 2
		// (add) Token: 0x0600055E RID: 1374 RVA: 0x0001D1C8 File Offset: 0x0001B3C8
		// (remove) Token: 0x0600055F RID: 1375 RVA: 0x0001D200 File Offset: 0x0001B400
		public event Action<MapNotificationItemBaseVM> ReceiveNewNotification;

		// Token: 0x06000560 RID: 1376 RVA: 0x0001D238 File Offset: 0x0001B438
		public MapNotificationVM(INavigationHandler navigationHandler, Action<CampaignVec2> fastMoveCameraToPosition)
		{
			this._navigationHandler = navigationHandler;
			this._fastMoveCameraToPosition = fastMoveCameraToPosition;
			MBInformationManager.OnAddMapNotice += this.AddMapNotification;
			this.NotificationItems = new MBBindingList<MapNotificationItemBaseVM>();
			this.PopulateTypeDictionary();
		}

		// Token: 0x06000561 RID: 1377 RVA: 0x0001D286 File Offset: 0x0001B486
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.NotificationItems.ApplyActionOnAllItems(delegate(MapNotificationItemBaseVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06000562 RID: 1378 RVA: 0x0001D2B8 File Offset: 0x0001B4B8
		private void PopulateTypeDictionary()
		{
			this._itemConstructors.Add(typeof(PeaceMapNotification), typeof(PeaceNotificationItemVM));
			this._itemConstructors.Add(typeof(SettlementRebellionMapNotification), typeof(RebellionNotificationItemVM));
			this._itemConstructors.Add(typeof(WarMapNotification), typeof(WarNotificationItemVM));
			this._itemConstructors.Add(typeof(ArmyDispersionMapNotification), typeof(ArmyDispersionItemVM));
			this._itemConstructors.Add(typeof(ChildBornMapNotification), typeof(NewBornNotificationItemVM));
			this._itemConstructors.Add(typeof(DeathMapNotification), typeof(DeathNotificationItemVM));
			this._itemConstructors.Add(typeof(MarriageMapNotification), typeof(MarriageNotificationItemVM));
			this._itemConstructors.Add(typeof(MarriageOfferMapNotification), typeof(MarriageOfferNotificationItemVM));
			this._itemConstructors.Add(typeof(MercenaryOfferMapNotification), typeof(MercenaryOfferMapNotificationItemVM));
			this._itemConstructors.Add(typeof(VassalOfferMapNotification), typeof(VassalOfferMapNotificationItemVM));
			this._itemConstructors.Add(typeof(ArmyCreationMapNotification), typeof(ArmyCreationNotificationItemVM));
			this._itemConstructors.Add(typeof(KingdomDecisionMapNotification), typeof(KingdomVoteNotificationItemVM));
			this._itemConstructors.Add(typeof(SettlementOwnerChangedMapNotification), typeof(SettlementOwnerChangedNotificationItemVM));
			this._itemConstructors.Add(typeof(SettlementUnderSiegeMapNotification), typeof(SettlementUnderSiegeMapNotificationItemVM));
			this._itemConstructors.Add(typeof(AlleyLeaderDiedMapNotification), typeof(AlleyLeaderDiedMapNotificationItemVM));
			this._itemConstructors.Add(typeof(AlleyUnderAttackMapNotification), typeof(AlleyUnderAttackMapNotificationItemVM));
			this._itemConstructors.Add(typeof(EducationMapNotification), typeof(EducationNotificationItemVM));
			this._itemConstructors.Add(typeof(TraitChangedMapNotification), typeof(TraitChangedNotificationItemVM));
			this._itemConstructors.Add(typeof(RansomOfferMapNotification), typeof(RansomNotificationItemVM));
			this._itemConstructors.Add(typeof(PeaceOfferMapNotification), typeof(PeaceOfferNotificationItemVM));
			this._itemConstructors.Add(typeof(PartyLeaderChangeNotification), typeof(PartyLeaderChangeNotificationVM));
			this._itemConstructors.Add(typeof(HeirComeOfAgeMapNotification), typeof(HeirComeOfAgeNotificationItemVM));
			this._itemConstructors.Add(typeof(KingdomDestroyedMapNotification), typeof(KingdomDestroyedNotificationItemVM));
			this._itemConstructors.Add(typeof(AllianceOfferMapNotification), typeof(AllianceOfferNotificationItemVM));
			this._itemConstructors.Add(typeof(AcceptCallToWarOfferMapNotification), typeof(AcceptCallToWarOfferNotificationItemVM));
			this._itemConstructors.Add(typeof(ProposeCallToWarOfferMapNotification), typeof(ProposeCallToWarOfferNotificationItemVM));
			this._itemConstructors.Add(typeof(TributeFinishedMapNotification), typeof(TributeFinishedMapNotificationVM));
		}

		// Token: 0x06000563 RID: 1379 RVA: 0x0001D60A File Offset: 0x0001B80A
		public void RegisterMapNotificationType(Type data, Type item)
		{
			this._itemConstructors[data] = item;
		}

		// Token: 0x06000564 RID: 1380 RVA: 0x0001D619 File Offset: 0x0001B819
		public override void OnFinalize()
		{
			MBInformationManager.OnAddMapNotice -= this.AddMapNotification;
		}

		// Token: 0x06000565 RID: 1381 RVA: 0x0001D62C File Offset: 0x0001B82C
		public void OnFrameTick(float dt)
		{
			for (int i = 0; i < this.NotificationItems.Count; i++)
			{
				this.NotificationItems[i].ManualRefreshRelevantStatus();
			}
		}

		// Token: 0x06000566 RID: 1382 RVA: 0x0001D660 File Offset: 0x0001B860
		public void OnMenuModeTick(float dt)
		{
			for (int i = 0; i < this.NotificationItems.Count; i++)
			{
				this.NotificationItems[i].ManualRefreshRelevantStatus();
			}
		}

		// Token: 0x06000567 RID: 1383 RVA: 0x0001D694 File Offset: 0x0001B894
		private void RemoveNotificationItem(MapNotificationItemBaseVM item)
		{
			item.OnFinalize();
			this.NotificationItems.Remove(item);
			MBInformationManager.MapNoticeRemoved(item.Data);
		}

		// Token: 0x06000568 RID: 1384 RVA: 0x0001D6B4 File Offset: 0x0001B8B4
		private void OnNotificationItemFocus(MapNotificationItemBaseVM item)
		{
			this.FocusedNotificationItem = item;
		}

		// Token: 0x06000569 RID: 1385 RVA: 0x0001D6C0 File Offset: 0x0001B8C0
		public void AddMapNotification(InformationData data)
		{
			MapNotificationItemBaseVM notificationFromData = this.GetNotificationFromData(data);
			if (notificationFromData != null)
			{
				this.NotificationItems.Add(notificationFromData);
				Action<MapNotificationItemBaseVM> receiveNewNotification = this.ReceiveNewNotification;
				if (receiveNewNotification == null)
				{
					return;
				}
				receiveNewNotification(notificationFromData);
			}
		}

		// Token: 0x0600056A RID: 1386 RVA: 0x0001D6F8 File Offset: 0x0001B8F8
		public void RemoveAllNotifications()
		{
			foreach (MapNotificationItemBaseVM item in this.NotificationItems.ToList<MapNotificationItemBaseVM>())
			{
				this.RemoveNotificationItem(item);
			}
		}

		// Token: 0x0600056B RID: 1387 RVA: 0x0001D750 File Offset: 0x0001B950
		private MapNotificationItemBaseVM GetNotificationFromData(InformationData data)
		{
			Type type = data.GetType();
			MapNotificationItemBaseVM mapNotificationItemBaseVM = null;
			if (this._itemConstructors.ContainsKey(type))
			{
				mapNotificationItemBaseVM = (MapNotificationItemBaseVM)Activator.CreateInstance(this._itemConstructors[type], new object[] { data });
				if (mapNotificationItemBaseVM != null)
				{
					mapNotificationItemBaseVM.OnRemove = new Action<MapNotificationItemBaseVM>(this.RemoveNotificationItem);
					mapNotificationItemBaseVM.OnFocus = new Action<MapNotificationItemBaseVM>(this.OnNotificationItemFocus);
					mapNotificationItemBaseVM.SetNavigationHandler(this._navigationHandler);
					mapNotificationItemBaseVM.SetFastMoveCameraToPosition(this._fastMoveCameraToPosition);
					if (this.RemoveInputKey != null)
					{
						mapNotificationItemBaseVM.RemoveInputKey = this.RemoveInputKey;
					}
				}
			}
			return mapNotificationItemBaseVM;
		}

		// Token: 0x0600056C RID: 1388 RVA: 0x0001D7E9 File Offset: 0x0001B9E9
		public void SetRemoveInputKey(HotKey hotKey)
		{
			this.RemoveInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x17000185 RID: 389
		// (get) Token: 0x0600056D RID: 1389 RVA: 0x0001D7F8 File Offset: 0x0001B9F8
		// (set) Token: 0x0600056E RID: 1390 RVA: 0x0001D800 File Offset: 0x0001BA00
		[DataSourceProperty]
		public InputKeyItemVM RemoveInputKey
		{
			get
			{
				return this._removeInputKey;
			}
			set
			{
				if (value != this._removeInputKey)
				{
					this._removeInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "RemoveInputKey");
					if (this._removeInputKey != null && this.NotificationItems != null)
					{
						for (int i = 0; i < this.NotificationItems.Count; i++)
						{
							this.NotificationItems[i].RemoveInputKey = this._removeInputKey;
						}
					}
				}
			}
		}

		// Token: 0x17000186 RID: 390
		// (get) Token: 0x0600056F RID: 1391 RVA: 0x0001D866 File Offset: 0x0001BA66
		// (set) Token: 0x06000570 RID: 1392 RVA: 0x0001D86E File Offset: 0x0001BA6E
		[DataSourceProperty]
		public MapNotificationItemBaseVM FocusedNotificationItem
		{
			get
			{
				return this._focusedNotificationItem;
			}
			set
			{
				if (value != this._focusedNotificationItem)
				{
					this._focusedNotificationItem = value;
					base.OnPropertyChangedWithValue<MapNotificationItemBaseVM>(value, "FocusedNotificationItem");
				}
			}
		}

		// Token: 0x17000187 RID: 391
		// (get) Token: 0x06000571 RID: 1393 RVA: 0x0001D88C File Offset: 0x0001BA8C
		// (set) Token: 0x06000572 RID: 1394 RVA: 0x0001D894 File Offset: 0x0001BA94
		[DataSourceProperty]
		public MBBindingList<MapNotificationItemBaseVM> NotificationItems
		{
			get
			{
				return this._notificationItems;
			}
			set
			{
				if (value != this._notificationItems)
				{
					this._notificationItems = value;
					base.OnPropertyChangedWithValue<MBBindingList<MapNotificationItemBaseVM>>(value, "NotificationItems");
				}
			}
		}

		// Token: 0x0400024B RID: 587
		private INavigationHandler _navigationHandler;

		// Token: 0x0400024C RID: 588
		private Action<CampaignVec2> _fastMoveCameraToPosition;

		// Token: 0x0400024D RID: 589
		private Dictionary<Type, Type> _itemConstructors = new Dictionary<Type, Type>();

		// Token: 0x0400024E RID: 590
		private InputKeyItemVM _removeInputKey;

		// Token: 0x0400024F RID: 591
		private MapNotificationItemBaseVM _focusedNotificationItem;

		// Token: 0x04000250 RID: 592
		private MBBindingList<MapNotificationItemBaseVM> _notificationItems;
	}
}
