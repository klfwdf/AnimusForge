using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000448 RID: 1096
	public class TradeSkillCampaignBehavior : CampaignBehaviorBase, IPlayerTradeBehavior
	{
		// Token: 0x060045D9 RID: 17881 RVA: 0x0015B95E File Offset: 0x00159B5E
		public override void RegisterEvents()
		{
			CampaignEvents.PlayerInventoryExchangeEvent.AddNonSerializedListener(this, new Action<List<ValueTuple<ItemRosterElement, int>>, List<ValueTuple<ItemRosterElement, int>>, bool>(this.PlayerInventoryUpdated));
		}

		// Token: 0x060045DA RID: 17882 RVA: 0x0015B978 File Offset: 0x00159B78
		private void RecordPurchases(ItemRosterElement itemRosterElement, int totalPrice)
		{
			TradeSkillCampaignBehavior.ItemTradeData itemTradeData;
			if (!this.ItemsTradeData.TryGetValue(itemRosterElement.EquipmentElement.Item, out itemTradeData))
			{
				itemTradeData = default(TradeSkillCampaignBehavior.ItemTradeData);
			}
			int num = itemTradeData.NumItemsPurchased + itemRosterElement.Amount;
			float averagePrice = (itemTradeData.AveragePrice * (float)itemTradeData.NumItemsPurchased + (float)totalPrice) / MathF.Max(0.0001f, (float)num);
			this.ItemsTradeData[itemRosterElement.EquipmentElement.Item] = new TradeSkillCampaignBehavior.ItemTradeData(averagePrice, num);
		}

		// Token: 0x060045DB RID: 17883 RVA: 0x0015B9FC File Offset: 0x00159BFC
		private int RecordSales(ItemRosterElement itemRosterElement, int totalPrice, bool isTrading)
		{
			int result = 0;
			TradeSkillCampaignBehavior.ItemTradeData itemTradeData;
			if (this.ItemsTradeData.TryGetValue(itemRosterElement.EquipmentElement.Item, out itemTradeData))
			{
				if (isTrading)
				{
					int num = MathF.Min(itemTradeData.NumItemsPurchased, itemRosterElement.Amount);
					int num2 = itemTradeData.NumItemsPurchased - num;
					float f = (float)num * itemTradeData.AveragePrice;
					float num3 = (float)totalPrice / MathF.Max(0.001f, (float)itemRosterElement.Amount);
					int num4 = MathF.Round((float)num * num3);
					result = MathF.Max(0, num4 - MathF.Floor(f));
					if (num2 == 0)
					{
						this.ItemsTradeData.Remove(itemRosterElement.EquipmentElement.Item);
					}
					else
					{
						this.ItemsTradeData[itemRosterElement.EquipmentElement.Item] = new TradeSkillCampaignBehavior.ItemTradeData(itemTradeData.AveragePrice, num2);
					}
				}
				else
				{
					int num5 = MobileParty.MainParty.ItemRoster.FindIndexOfElement(itemRosterElement.EquipmentElement);
					if (num5 == -1)
					{
						this.ItemsTradeData.Remove(itemRosterElement.EquipmentElement.Item);
					}
					else
					{
						int amount = MobileParty.MainParty.ItemRoster.GetElementCopyAtIndex(num5).Amount;
						if (itemTradeData.NumItemsPurchased > amount)
						{
							this.ItemsTradeData[itemRosterElement.EquipmentElement.Item] = new TradeSkillCampaignBehavior.ItemTradeData(itemTradeData.AveragePrice, amount);
						}
					}
				}
			}
			return result;
		}

		// Token: 0x060045DC RID: 17884 RVA: 0x0015BB68 File Offset: 0x00159D68
		private int GetAveragePriceForItem(ItemRosterElement itemRosterElement)
		{
			TradeSkillCampaignBehavior.ItemTradeData itemTradeData;
			if (!this.ItemsTradeData.TryGetValue(itemRosterElement.EquipmentElement.Item, out itemTradeData))
			{
				return 0;
			}
			return MathF.Round(itemTradeData.AveragePrice);
		}

		// Token: 0x060045DD RID: 17885 RVA: 0x0015BBA0 File Offset: 0x00159DA0
		private void PlayerInventoryUpdated(List<ValueTuple<ItemRosterElement, int>> purchasedItems, List<ValueTuple<ItemRosterElement, int>> soldItems, bool isTrading)
		{
			int num = 0;
			if (isTrading)
			{
				foreach (ValueTuple<ItemRosterElement, int> valueTuple in purchasedItems)
				{
					this.ProcessPurchases(valueTuple.Item1, valueTuple.Item2);
				}
			}
			foreach (ValueTuple<ItemRosterElement, int> valueTuple2 in soldItems)
			{
				num += this.ProcessSales(valueTuple2.Item1, valueTuple2.Item2, isTrading);
			}
			if (isTrading)
			{
				SkillLevelingManager.OnTradeProfitMade(PartyBase.MainParty, num);
				CampaignEventDispatcher.Instance.OnPlayerTradeProfit(num);
			}
		}

		// Token: 0x060045DE RID: 17886 RVA: 0x0015BC64 File Offset: 0x00159E64
		private int ProcessSales(ItemRosterElement itemRosterElement, int totalPrice, bool isTrading)
		{
			if (itemRosterElement.EquipmentElement.ItemModifier == null)
			{
				return this.RecordSales(itemRosterElement, totalPrice, isTrading);
			}
			return 0;
		}

		// Token: 0x060045DF RID: 17887 RVA: 0x0015BC90 File Offset: 0x00159E90
		private void ProcessPurchases(ItemRosterElement itemRosterElement, int totalPrice)
		{
			if (itemRosterElement.EquipmentElement.ItemModifier == null)
			{
				this.RecordPurchases(itemRosterElement, totalPrice);
			}
		}

		// Token: 0x060045E0 RID: 17888 RVA: 0x0015BCB6 File Offset: 0x00159EB6
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<ItemObject, TradeSkillCampaignBehavior.ItemTradeData>>("ItemsTradeData", ref this.ItemsTradeData);
		}

		// Token: 0x060045E1 RID: 17889 RVA: 0x0015BCCC File Offset: 0x00159ECC
		public int GetProjectedProfit(ItemRosterElement itemRosterElement, int itemCost)
		{
			if (itemRosterElement.EquipmentElement.ItemModifier != null)
			{
				return 0;
			}
			int averagePriceForItem = this.GetAveragePriceForItem(itemRosterElement);
			return itemCost - averagePriceForItem;
		}

		// Token: 0x04001385 RID: 4997
		private Dictionary<ItemObject, TradeSkillCampaignBehavior.ItemTradeData> ItemsTradeData = new Dictionary<ItemObject, TradeSkillCampaignBehavior.ItemTradeData>();

		// Token: 0x02000858 RID: 2136
		public class TradeSkillCampaignBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x06006734 RID: 26420 RVA: 0x001C36CB File Offset: 0x001C18CB
			public TradeSkillCampaignBehaviorTypeDefiner()
				: base(150794)
			{
			}

			// Token: 0x06006735 RID: 26421 RVA: 0x001C36D8 File Offset: 0x001C18D8
			protected override void DefineStructTypes()
			{
				base.AddStructDefinition(typeof(TradeSkillCampaignBehavior.ItemTradeData), 10, null);
			}

			// Token: 0x06006736 RID: 26422 RVA: 0x001C36ED File Offset: 0x001C18ED
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(Dictionary<ItemObject, TradeSkillCampaignBehavior.ItemTradeData>));
			}
		}

		// Token: 0x02000859 RID: 2137
		internal struct ItemTradeData
		{
			// Token: 0x06006737 RID: 26423 RVA: 0x001C36FF File Offset: 0x001C18FF
			public ItemTradeData(float averagePrice, int numItemsPurchased)
			{
				this.AveragePrice = averagePrice;
				this.NumItemsPurchased = numItemsPurchased;
			}

			// Token: 0x06006738 RID: 26424 RVA: 0x001C3710 File Offset: 0x001C1910
			public static void AutoGeneratedStaticCollectObjectsItemTradeData(object o, List<object> collectedObjects)
			{
				((TradeSkillCampaignBehavior.ItemTradeData)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x06006739 RID: 26425 RVA: 0x001C372C File Offset: 0x001C192C
			private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
			}

			// Token: 0x0600673A RID: 26426 RVA: 0x001C372E File Offset: 0x001C192E
			internal static object AutoGeneratedGetMemberValueAveragePrice(object o)
			{
				return ((TradeSkillCampaignBehavior.ItemTradeData)o).AveragePrice;
			}

			// Token: 0x0600673B RID: 26427 RVA: 0x001C3740 File Offset: 0x001C1940
			internal static object AutoGeneratedGetMemberValueNumItemsPurchased(object o)
			{
				return ((TradeSkillCampaignBehavior.ItemTradeData)o).NumItemsPurchased;
			}

			// Token: 0x04002396 RID: 9110
			[SaveableField(10)]
			public readonly float AveragePrice;

			// Token: 0x04002397 RID: 9111
			[SaveableField(20)]
			public readonly int NumItemsPurchased;
		}
	}
}
