using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000423 RID: 1059
	public class PartiesSellLootCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060042F0 RID: 17136 RVA: 0x0014344B File Offset: 0x0014164B
		public override void RegisterEvents()
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
		}

		// Token: 0x060042F1 RID: 17137 RVA: 0x00143464 File Offset: 0x00141664
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060042F2 RID: 17138 RVA: 0x00143468 File Offset: 0x00141668
		public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (Campaign.Current.GameStarted && mobileParty != null && !FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, settlement.MapFaction) && !mobileParty.IsMainParty && mobileParty.IsLordParty && !mobileParty.IsDisbanding && settlement.IsTown)
			{
				int gold = settlement.SettlementComponent.Gold;
				for (int i = 0; i < mobileParty.ItemRoster.Count; i++)
				{
					ItemRosterElement subject = mobileParty.ItemRoster[i];
					ItemObject item = subject.EquipmentElement.Item;
					ItemModifier itemModifier = subject.EquipmentElement.ItemModifier;
					int amount = subject.Amount;
					if (!item.IsFood && (item.ItemType != ItemObject.ItemTypeEnum.Horse || !item.HorseComponent.IsRideable || itemModifier != null || item.HorseComponent.IsPackAnimal))
					{
						int itemPrice = settlement.Town.GetItemPrice(subject.EquipmentElement, mobileParty, true);
						int num = ((itemPrice * amount < gold) ? amount : (gold / itemPrice));
						if (num > 0)
						{
							SellItemsAction.Apply(mobileParty.Party, settlement.Party, subject, num, settlement);
						}
					}
				}
			}
		}
	}
}
