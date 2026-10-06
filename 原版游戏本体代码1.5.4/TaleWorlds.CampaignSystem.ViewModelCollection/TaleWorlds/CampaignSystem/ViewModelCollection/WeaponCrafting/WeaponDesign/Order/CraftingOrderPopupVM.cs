using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order;

public class CraftingOrderPopupVM : ViewModel
{
	private class OrderComparer : IComparer<CraftingOrder>
	{
		public int Compare(CraftingOrder x, CraftingOrder y)
		{
			return (int)(x.OrderDifficulty - y.OrderDifficulty);
		}
	}

	private Action<CraftingOrderItemVM> _onDoneAction;

	private Func<CraftingAvailableHeroItemVM> _getCurrentCraftingHero;

	private Func<CraftingOrder, IEnumerable<CraftingStatData>> _getOrderStatDatas;

	private readonly ICraftingCampaignBehavior _craftingBehavior;

	private bool _isVisible;

	private int _questType;

	private string _orderCountText;

	private MBBindingList<CraftingOrderItemVM> _craftingOrders;

	private CraftingOrderItemVM _selectedCraftingOrder;

	public bool HasOrders => CraftingOrders.Count > 0;

	public bool HasEnabledOrders => CraftingOrders.Count((CraftingOrderItemVM x) => x.IsEnabled) > 0;

	[DataSourceProperty]
	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (value != _isVisible)
			{
				_isVisible = value;
				OnPropertyChangedWithValue(value, "IsVisible");
				Game.Current?.EventManager.TriggerEvent(new CraftingOrderSelectionOpenedEvent(_isVisible));
			}
		}
	}

	[DataSourceProperty]
	public int QuestType
	{
		get
		{
			return _questType;
		}
		set
		{
			if (value != _questType)
			{
				_questType = value;
				OnPropertyChangedWithValue(value, "QuestType");
			}
		}
	}

	[DataSourceProperty]
	public string OrderCountText
	{
		get
		{
			return _orderCountText;
		}
		set
		{
			if (value != _orderCountText)
			{
				_orderCountText = value;
				OnPropertyChangedWithValue(value, "OrderCountText");
			}
		}
	}

	[DataSourceProperty]
	public CraftingOrderItemVM SelectedCraftingOrder
	{
		get
		{
			return _selectedCraftingOrder;
		}
		set
		{
			if (value != _selectedCraftingOrder)
			{
				_selectedCraftingOrder = value;
				OnPropertyChangedWithValue(value, "SelectedCraftingOrder");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CraftingOrderItemVM> CraftingOrders
	{
		get
		{
			return _craftingOrders;
		}
		set
		{
			if (value != _craftingOrders)
			{
				_craftingOrders = value;
				OnPropertyChangedWithValue(value, "CraftingOrders");
			}
		}
	}

	public CraftingOrderPopupVM(Action<CraftingOrderItemVM> onDoneAction, Func<CraftingAvailableHeroItemVM> getCurrentCraftingHero, Func<CraftingOrder, IEnumerable<CraftingStatData>> getOrderStatDatas)
	{
		_onDoneAction = onDoneAction;
		_getCurrentCraftingHero = getCurrentCraftingHero;
		_getOrderStatDatas = getOrderStatDatas;
		_craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
		CraftingOrders = new MBBindingList<CraftingOrderItemVM>();
	}

	public void RefreshOrders()
	{
		CraftingOrders.Clear();
		if (Campaign.Current.GameMode == CampaignGameMode.Tutorial)
		{
			return;
		}
		CraftingCampaignBehavior.CraftingOrderSlots craftingOrderSlots = _craftingBehavior.CraftingOrders[Settlement.CurrentSettlement?.Town];
		if (craftingOrderSlots != null)
		{
			OrderComparer comparer = new OrderComparer();
			List<CraftingOrder> list = craftingOrderSlots.CustomOrders.Where((CraftingOrder x) => x != null).ToList();
			list.Sort(comparer);
			List<CraftingOrder> list2 = craftingOrderSlots.Slots.Where((CraftingOrder x) => x != null).ToList();
			list2.Sort(comparer);
			CampaignUIHelper.IssueQuestFlags issueQuestFlags = CampaignUIHelper.IssueQuestFlags.None;
			for (int num = 0; num < list.Count; num++)
			{
				List<CraftingStatData> orderStatDatas = _getOrderStatDatas(list[num]).ToList();
				CampaignUIHelper.IssueQuestFlags questFlagsForOrder = GetQuestFlagsForOrder(list[num]);
				CraftingOrders.Add(new CraftingOrderItemVM(list[num], SelectOrder, _getCurrentCraftingHero, orderStatDatas, questFlagsForOrder));
				issueQuestFlags |= questFlagsForOrder;
			}
			QuestType = (int)issueQuestFlags;
			for (int num2 = 0; num2 < list2.Count; num2++)
			{
				List<CraftingStatData> orderStatDatas2 = _getOrderStatDatas(list2[num2]).ToList();
				CraftingOrders.Add(new CraftingOrderItemVM(list2[num2], SelectOrder, _getCurrentCraftingHero, orderStatDatas2));
			}
			TextObject textObject = new TextObject("{=MkVTRqAw}Orders ({ORDER_COUNT})");
			textObject.SetTextVariable("ORDER_COUNT", CraftingOrders.Count);
			OrderCountText = textObject.ToString();
		}
	}

	private CampaignUIHelper.IssueQuestFlags GetQuestFlagsForOrder(CraftingOrder order)
	{
		if (Campaign.Current.QuestManager.TrackedObjects.ContainsKey(order))
		{
			return CampaignUIHelper.IssueQuestFlags.ActiveIssue;
		}
		return CampaignUIHelper.IssueQuestFlags.None;
	}

	public void SelectOrder(CraftingOrderItemVM order)
	{
		if (SelectedCraftingOrder != null)
		{
			SelectedCraftingOrder.IsSelected = false;
		}
		SelectedCraftingOrder = order;
		SelectedCraftingOrder.IsSelected = true;
		_onDoneAction(order);
		IsVisible = false;
	}

	public void ExecuteOpenPopup()
	{
		IsVisible = true;
	}

	public void ExecuteCloseWithoutSelection()
	{
		IsVisible = false;
	}
}
