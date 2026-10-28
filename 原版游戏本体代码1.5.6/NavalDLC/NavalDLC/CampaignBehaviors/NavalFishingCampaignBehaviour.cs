using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.CampaignBehaviors;

public class NavalFishingCampaignBehaviour : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
	}

	private void OnDailyTickSettlement(Settlement settlement)
	{
		if (settlement.IsVillage && settlement.Village.TradeBound != null)
		{
			ExplainedNumber bonuses = new ExplainedNumber(0f, false, null);
			PerkHelper.AddPerkBonusForTown(NavalPerks.Shipmaster.NightRaider, settlement.Village.TradeBound.Town, isPrimaryBonus: false, ref bonuses);
			if (bonuses.RoundedResultNumber > 0)
			{
				ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>("fish");
				int roundedResultNumber = bonuses.RoundedResultNumber;
				settlement.Village.Owner.ItemRoster.AddToCounts(itemObject, roundedResultNumber);
				CampaignEventDispatcher.Instance.OnItemProduced(itemObject, settlement.Village.Owner.Settlement, roundedResultNumber);
			}
		}
	}

	private void OnHourlyTickParty(MobileParty party)
	{
		if (party.HasPerk(NavalPerks.Shipmaster.MasterAngler))
		{
			float primaryBonus = NavalPerks.Shipmaster.MasterAngler.PrimaryBonus;
			if (MBRandom.RandomFloat < primaryBonus)
			{
				ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("fish");
				party.ItemRoster.AddToCounts(item, 1);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
