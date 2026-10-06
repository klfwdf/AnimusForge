using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PartiesSellLootCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (!Campaign.Current.GameStarted || mobileParty == null || FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, settlement.MapFaction) || mobileParty.IsMainParty || !mobileParty.IsLordParty || mobileParty.IsDisbanding || !settlement.IsTown)
		{
			return;
		}
		int gold = settlement.SettlementComponent.Gold;
		for (int i = 0; i < mobileParty.ItemRoster.Count; i++)
		{
			ItemRosterElement subject = mobileParty.ItemRoster[i];
			ItemObject item = subject.EquipmentElement.Item;
			ItemModifier itemModifier = subject.EquipmentElement.ItemModifier;
			int amount = subject.Amount;
			if (!item.IsFood && (item.ItemType != ItemObject.ItemTypeEnum.Horse || !item.HorseComponent.IsRideable || itemModifier != null || item.HorseComponent.IsPackAnimal))
			{
				int itemPrice = settlement.Town.GetItemPrice(subject.EquipmentElement, mobileParty, isSelling: true);
				int num = ((itemPrice * amount < gold) ? amount : (gold / itemPrice));
				if (num > 0)
				{
					SellItemsAction.Apply(mobileParty.Party, settlement.Party, subject, num, settlement);
				}
			}
		}
	}
}
