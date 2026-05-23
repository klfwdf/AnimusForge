using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order
{
	// Token: 0x02000110 RID: 272
	public class CraftingOrderPopupVM : ViewModel
	{
		// Token: 0x17000851 RID: 2129
		// (get) Token: 0x060018D1 RID: 6353 RVA: 0x0005E6BE File Offset: 0x0005C8BE
		public bool HasOrders
		{
			get
			{
				return this.CraftingOrders.Count > 0;
			}
		}

		// Token: 0x17000852 RID: 2130
		// (get) Token: 0x060018D2 RID: 6354 RVA: 0x0005E6CE File Offset: 0x0005C8CE
		public bool HasEnabledOrders
		{
			get
			{
				return this.CraftingOrders.Count((CraftingOrderItemVM x) => x.IsEnabled) > 0;
			}
		}

		// Token: 0x060018D3 RID: 6355 RVA: 0x0005E6FD File Offset: 0x0005C8FD
		public CraftingOrderPopupVM(Action<CraftingOrderItemVM> onDoneAction, Func<CraftingAvailableHeroItemVM> getCurrentCraftingHero, Func<CraftingOrder, IEnumerable<CraftingStatData>> getOrderStatDatas)
		{
			this._onDoneAction = onDoneAction;
			this._getCurrentCraftingHero = getCurrentCraftingHero;
			this._getOrderStatDatas = getOrderStatDatas;
			this._craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
			this.CraftingOrders = new MBBindingList<CraftingOrderItemVM>();
		}

		// Token: 0x060018D4 RID: 6356 RVA: 0x0005E738 File Offset: 0x0005C938
		public void RefreshOrders()
		{
			this.CraftingOrders.Clear();
			if (Campaign.Current.GameMode == CampaignGameMode.Tutorial)
			{
				return;
			}
			IReadOnlyDictionary<Town, CraftingCampaignBehavior.CraftingOrderSlots> craftingOrders = this._craftingBehavior.CraftingOrders;
			Settlement currentSettlement = Settlement.CurrentSettlement;
			CraftingCampaignBehavior.CraftingOrderSlots craftingOrderSlots = craftingOrders[(currentSettlement != null) ? currentSettlement.Town : null];
			if (craftingOrderSlots == null)
			{
				return;
			}
			CraftingOrderPopupVM.OrderComparer comparer = new CraftingOrderPopupVM.OrderComparer();
			List<CraftingOrder> list = (from x in craftingOrderSlots.CustomOrders
				where x != null
				select x).ToList<CraftingOrder>();
			list.Sort(comparer);
			List<CraftingOrder> list2 = (from x in craftingOrderSlots.Slots
				where x != null
				select x).ToList<CraftingOrder>();
			list2.Sort(comparer);
			CampaignUIHelper.IssueQuestFlags issueQuestFlags = CampaignUIHelper.IssueQuestFlags.None;
			for (int i = 0; i < list.Count; i++)
			{
				List<CraftingStatData> orderStatDatas = this._getOrderStatDatas(list[i]).ToList<CraftingStatData>();
				CampaignUIHelper.IssueQuestFlags questFlagsForOrder = this.GetQuestFlagsForOrder(list[i]);
				this.CraftingOrders.Add(new CraftingOrderItemVM(list[i], new Action<CraftingOrderItemVM>(this.SelectOrder), this._getCurrentCraftingHero, orderStatDatas, questFlagsForOrder));
				issueQuestFlags |= questFlagsForOrder;
			}
			this.QuestType = (int)issueQuestFlags;
			for (int j = 0; j < list2.Count; j++)
			{
				List<CraftingStatData> orderStatDatas2 = this._getOrderStatDatas(list2[j]).ToList<CraftingStatData>();
				this.CraftingOrders.Add(new CraftingOrderItemVM(list2[j], new Action<CraftingOrderItemVM>(this.SelectOrder), this._getCurrentCraftingHero, orderStatDatas2, CampaignUIHelper.IssueQuestFlags.None));
			}
			TextObject textObject = new TextObject("{=MkVTRqAw}Orders ({ORDER_COUNT})", null);
			textObject.SetTextVariable("ORDER_COUNT", this.CraftingOrders.Count);
			this.OrderCountText = textObject.ToString();
		}

		// Token: 0x060018D5 RID: 6357 RVA: 0x0005E905 File Offset: 0x0005CB05
		private CampaignUIHelper.IssueQuestFlags GetQuestFlagsForOrder(CraftingOrder order)
		{
			if (Campaign.Current.QuestManager.TrackedObjects.ContainsKey(order))
			{
				return CampaignUIHelper.IssueQuestFlags.ActiveIssue;
			}
			return CampaignUIHelper.IssueQuestFlags.None;
		}

		// Token: 0x060018D6 RID: 6358 RVA: 0x0005E921 File Offset: 0x0005CB21
		public void SelectOrder(CraftingOrderItemVM order)
		{
			if (this.SelectedCraftingOrder != null)
			{
				this.SelectedCraftingOrder.IsSelected = false;
			}
			this.SelectedCraftingOrder = order;
			this.SelectedCraftingOrder.IsSelected = true;
			this._onDoneAction(order);
			this.IsVisible = false;
		}

		// Token: 0x060018D7 RID: 6359 RVA: 0x0005E95D File Offset: 0x0005CB5D
		public void ExecuteOpenPopup()
		{
			this.IsVisible = true;
		}

		// Token: 0x060018D8 RID: 6360 RVA: 0x0005E966 File Offset: 0x0005CB66
		public void ExecuteCloseWithoutSelection()
		{
			this.IsVisible = false;
		}

		// Token: 0x17000853 RID: 2131
		// (get) Token: 0x060018D9 RID: 6361 RVA: 0x0005E96F File Offset: 0x0005CB6F
		// (set) Token: 0x060018DA RID: 6362 RVA: 0x0005E977 File Offset: 0x0005CB77
		[DataSourceProperty]
		public bool IsVisible
		{
			get
			{
				return this._isVisible;
			}
			set
			{
				if (value != this._isVisible)
				{
					this._isVisible = value;
					base.OnPropertyChangedWithValue(value, "IsVisible");
					Game game = Game.Current;
					if (game == null)
					{
						return;
					}
					game.EventManager.TriggerEvent<CraftingOrderSelectionOpenedEvent>(new CraftingOrderSelectionOpenedEvent(this._isVisible));
				}
			}
		}

		// Token: 0x17000854 RID: 2132
		// (get) Token: 0x060018DB RID: 6363 RVA: 0x0005E9B4 File Offset: 0x0005CBB4
		// (set) Token: 0x060018DC RID: 6364 RVA: 0x0005E9BC File Offset: 0x0005CBBC
		[DataSourceProperty]
		public int QuestType
		{
			get
			{
				return this._questType;
			}
			set
			{
				if (value != this._questType)
				{
					this._questType = value;
					base.OnPropertyChangedWithValue(value, "QuestType");
				}
			}
		}

		// Token: 0x17000855 RID: 2133
		// (get) Token: 0x060018DD RID: 6365 RVA: 0x0005E9DA File Offset: 0x0005CBDA
		// (set) Token: 0x060018DE RID: 6366 RVA: 0x0005E9E2 File Offset: 0x0005CBE2
		[DataSourceProperty]
		public string OrderCountText
		{
			get
			{
				return this._orderCountText;
			}
			set
			{
				if (value != this._orderCountText)
				{
					this._orderCountText = value;
					base.OnPropertyChangedWithValue<string>(value, "OrderCountText");
				}
			}
		}

		// Token: 0x17000856 RID: 2134
		// (get) Token: 0x060018DF RID: 6367 RVA: 0x0005EA05 File Offset: 0x0005CC05
		// (set) Token: 0x060018E0 RID: 6368 RVA: 0x0005EA0D File Offset: 0x0005CC0D
		[DataSourceProperty]
		public CraftingOrderItemVM SelectedCraftingOrder
		{
			get
			{
				return this._selectedCraftingOrder;
			}
			set
			{
				if (value != this._selectedCraftingOrder)
				{
					this._selectedCraftingOrder = value;
					base.OnPropertyChangedWithValue<CraftingOrderItemVM>(value, "SelectedCraftingOrder");
				}
			}
		}

		// Token: 0x17000857 RID: 2135
		// (get) Token: 0x060018E1 RID: 6369 RVA: 0x0005EA2B File Offset: 0x0005CC2B
		// (set) Token: 0x060018E2 RID: 6370 RVA: 0x0005EA33 File Offset: 0x0005CC33
		[DataSourceProperty]
		public MBBindingList<CraftingOrderItemVM> CraftingOrders
		{
			get
			{
				return this._craftingOrders;
			}
			set
			{
				if (value != this._craftingOrders)
				{
					this._craftingOrders = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingOrderItemVM>>(value, "CraftingOrders");
				}
			}
		}

		// Token: 0x04000B68 RID: 2920
		private Action<CraftingOrderItemVM> _onDoneAction;

		// Token: 0x04000B69 RID: 2921
		private Func<CraftingAvailableHeroItemVM> _getCurrentCraftingHero;

		// Token: 0x04000B6A RID: 2922
		private Func<CraftingOrder, IEnumerable<CraftingStatData>> _getOrderStatDatas;

		// Token: 0x04000B6B RID: 2923
		private readonly ICraftingCampaignBehavior _craftingBehavior;

		// Token: 0x04000B6C RID: 2924
		private bool _isVisible;

		// Token: 0x04000B6D RID: 2925
		private int _questType;

		// Token: 0x04000B6E RID: 2926
		private string _orderCountText;

		// Token: 0x04000B6F RID: 2927
		private MBBindingList<CraftingOrderItemVM> _craftingOrders;

		// Token: 0x04000B70 RID: 2928
		private CraftingOrderItemVM _selectedCraftingOrder;

		// Token: 0x02000270 RID: 624
		private class OrderComparer : IComparer<CraftingOrder>
		{
			// Token: 0x06002557 RID: 9559 RVA: 0x00080991 File Offset: 0x0007EB91
			public int Compare(CraftingOrder x, CraftingOrder y)
			{
				return (int)(x.OrderDifficulty - y.OrderDifficulty);
			}
		}
	}
}
