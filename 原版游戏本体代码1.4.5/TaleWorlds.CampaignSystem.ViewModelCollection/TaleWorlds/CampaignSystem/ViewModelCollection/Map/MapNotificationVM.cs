using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map;

public class MapNotificationVM : ViewModel
{
	private INavigationHandler _navigationHandler;

	private Action<CampaignVec2> _fastMoveCameraToPosition;

	private Dictionary<Type, Type> _itemConstructors = new Dictionary<Type, Type>();

	private InputKeyItemVM _removeInputKey;

	private MapNotificationItemBaseVM _focusedNotificationItem;

	private MBBindingList<MapNotificationItemBaseVM> _notificationItems;

	[DataSourceProperty]
	public InputKeyItemVM RemoveInputKey
	{
		get
		{
			return _removeInputKey;
		}
		set
		{
			if (value == _removeInputKey)
			{
				return;
			}
			_removeInputKey = value;
			OnPropertyChangedWithValue(value, "RemoveInputKey");
			if (_removeInputKey != null && NotificationItems != null)
			{
				for (int i = 0; i < NotificationItems.Count; i++)
				{
					NotificationItems[i].RemoveInputKey = _removeInputKey;
				}
			}
		}
	}

	[DataSourceProperty]
	public MapNotificationItemBaseVM FocusedNotificationItem
	{
		get
		{
			return _focusedNotificationItem;
		}
		set
		{
			if (value != _focusedNotificationItem)
			{
				_focusedNotificationItem = value;
				OnPropertyChangedWithValue(value, "FocusedNotificationItem");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MapNotificationItemBaseVM> NotificationItems
	{
		get
		{
			return _notificationItems;
		}
		set
		{
			if (value != _notificationItems)
			{
				_notificationItems = value;
				OnPropertyChangedWithValue(value, "NotificationItems");
			}
		}
	}

	public event Action<MapNotificationItemBaseVM> ReceiveNewNotification;

	public MapNotificationVM(INavigationHandler navigationHandler, Action<CampaignVec2> fastMoveCameraToPosition)
	{
		_navigationHandler = navigationHandler;
		_fastMoveCameraToPosition = fastMoveCameraToPosition;
		MBInformationManager.OnAddMapNotice += AddMapNotification;
		NotificationItems = new MBBindingList<MapNotificationItemBaseVM>();
		PopulateTypeDictionary();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NotificationItems.ApplyActionOnAllItems(delegate(MapNotificationItemBaseVM x)
		{
			x.RefreshValues();
		});
	}

	private void PopulateTypeDictionary()
	{
		_itemConstructors.Add(typeof(PeaceMapNotification), typeof(PeaceNotificationItemVM));
		_itemConstructors.Add(typeof(SettlementRebellionMapNotification), typeof(RebellionNotificationItemVM));
		_itemConstructors.Add(typeof(WarMapNotification), typeof(WarNotificationItemVM));
		_itemConstructors.Add(typeof(ArmyDispersionMapNotification), typeof(ArmyDispersionItemVM));
		_itemConstructors.Add(typeof(ChildBornMapNotification), typeof(NewBornNotificationItemVM));
		_itemConstructors.Add(typeof(DeathMapNotification), typeof(DeathNotificationItemVM));
		_itemConstructors.Add(typeof(MarriageMapNotification), typeof(MarriageNotificationItemVM));
		_itemConstructors.Add(typeof(MarriageOfferMapNotification), typeof(MarriageOfferNotificationItemVM));
		_itemConstructors.Add(typeof(MercenaryOfferMapNotification), typeof(MercenaryOfferMapNotificationItemVM));
		_itemConstructors.Add(typeof(VassalOfferMapNotification), typeof(VassalOfferMapNotificationItemVM));
		_itemConstructors.Add(typeof(ArmyCreationMapNotification), typeof(ArmyCreationNotificationItemVM));
		_itemConstructors.Add(typeof(KingdomDecisionMapNotification), typeof(KingdomVoteNotificationItemVM));
		_itemConstructors.Add(typeof(SettlementOwnerChangedMapNotification), typeof(SettlementOwnerChangedNotificationItemVM));
		_itemConstructors.Add(typeof(SettlementUnderSiegeMapNotification), typeof(SettlementUnderSiegeMapNotificationItemVM));
		_itemConstructors.Add(typeof(AlleyLeaderDiedMapNotification), typeof(AlleyLeaderDiedMapNotificationItemVM));
		_itemConstructors.Add(typeof(AlleyUnderAttackMapNotification), typeof(AlleyUnderAttackMapNotificationItemVM));
		_itemConstructors.Add(typeof(EducationMapNotification), typeof(EducationNotificationItemVM));
		_itemConstructors.Add(typeof(TraitChangedMapNotification), typeof(TraitChangedNotificationItemVM));
		_itemConstructors.Add(typeof(RansomOfferMapNotification), typeof(RansomNotificationItemVM));
		_itemConstructors.Add(typeof(PeaceOfferMapNotification), typeof(PeaceOfferNotificationItemVM));
		_itemConstructors.Add(typeof(PartyLeaderChangeNotification), typeof(PartyLeaderChangeNotificationVM));
		_itemConstructors.Add(typeof(HeirComeOfAgeMapNotification), typeof(HeirComeOfAgeNotificationItemVM));
		_itemConstructors.Add(typeof(KingdomDestroyedMapNotification), typeof(KingdomDestroyedNotificationItemVM));
		_itemConstructors.Add(typeof(AllianceOfferMapNotification), typeof(AllianceOfferNotificationItemVM));
		_itemConstructors.Add(typeof(AcceptCallToWarOfferMapNotification), typeof(AcceptCallToWarOfferNotificationItemVM));
		_itemConstructors.Add(typeof(ProposeCallToWarOfferMapNotification), typeof(ProposeCallToWarOfferNotificationItemVM));
		_itemConstructors.Add(typeof(TributeFinishedMapNotification), typeof(TributeFinishedMapNotificationVM));
	}

	public void RegisterMapNotificationType(Type data, Type item)
	{
		_itemConstructors[data] = item;
	}

	public override void OnFinalize()
	{
		MBInformationManager.OnAddMapNotice -= AddMapNotification;
	}

	public void OnFrameTick(float dt)
	{
		for (int i = 0; i < NotificationItems.Count; i++)
		{
			NotificationItems[i].ManualRefreshRelevantStatus();
		}
	}

	public void OnMenuModeTick(float dt)
	{
		for (int i = 0; i < NotificationItems.Count; i++)
		{
			NotificationItems[i].ManualRefreshRelevantStatus();
		}
	}

	private void RemoveNotificationItem(MapNotificationItemBaseVM item)
	{
		item.OnFinalize();
		NotificationItems.Remove(item);
		MBInformationManager.MapNoticeRemoved(item.Data);
	}

	private void OnNotificationItemFocus(MapNotificationItemBaseVM item)
	{
		FocusedNotificationItem = item;
	}

	public void AddMapNotification(InformationData data)
	{
		MapNotificationItemBaseVM notificationFromData = GetNotificationFromData(data);
		if (notificationFromData != null)
		{
			NotificationItems.Add(notificationFromData);
			this.ReceiveNewNotification?.Invoke(notificationFromData);
		}
	}

	public void RemoveAllNotifications()
	{
		foreach (MapNotificationItemBaseVM item in NotificationItems.ToList())
		{
			RemoveNotificationItem(item);
		}
	}

	private MapNotificationItemBaseVM GetNotificationFromData(InformationData data)
	{
		Type type = data.GetType();
		MapNotificationItemBaseVM mapNotificationItemBaseVM = null;
		if (_itemConstructors.ContainsKey(type))
		{
			mapNotificationItemBaseVM = (MapNotificationItemBaseVM)Activator.CreateInstance(_itemConstructors[type], data);
			if (mapNotificationItemBaseVM != null)
			{
				mapNotificationItemBaseVM.OnRemove = RemoveNotificationItem;
				mapNotificationItemBaseVM.OnFocus = OnNotificationItemFocus;
				mapNotificationItemBaseVM.SetNavigationHandler(_navigationHandler);
				mapNotificationItemBaseVM.SetFastMoveCameraToPosition(_fastMoveCameraToPosition);
				if (RemoveInputKey != null)
				{
					mapNotificationItemBaseVM.RemoveInputKey = RemoveInputKey;
				}
			}
		}
		return mapNotificationItemBaseVM;
	}

	public void SetRemoveInputKey(HotKey hotKey)
	{
		RemoveInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
