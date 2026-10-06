using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultVassalRewardsModel : VassalRewardsModel
{
	private const int VassalRewardBannerLevel = 2;

	public override int RelationRewardWithLeader => 10;

	public override float InfluenceReward => 10f;

	public override ItemRoster GetEquipmentRewardsForJoiningKingdom(Kingdom kingdom)
	{
		ItemRoster itemRoster = new ItemRoster();
		foreach (ItemObject vassalRewardItem in kingdom.Culture.VassalRewardItems)
		{
			itemRoster.AddToCounts(vassalRewardItem, 1);
		}
		ItemObject randomBannerAtLevel = GetRandomBannerAtLevel(2, kingdom.Culture);
		if (randomBannerAtLevel != null)
		{
			itemRoster.AddToCounts(randomBannerAtLevel, 1);
		}
		return itemRoster;
	}

	private ItemObject GetRandomBannerAtLevel(int bannerLevel, CultureObject culture = null)
	{
		MBList<ItemObject> e = Campaign.Current.Models.BannerItemModel.GetPossibleRewardBannerItems().ToMBList();
		if (culture == null)
		{
			return e.GetRandomElementWithPredicate((ItemObject i) => (i.ItemComponent as BannerComponent).BannerLevel == bannerLevel);
		}
		return e.GetRandomElementWithPredicate((ItemObject i) => (i.ItemComponent as BannerComponent).BannerLevel == bannerLevel && i.Culture == culture);
	}

	public override TroopRoster GetTroopRewardsForJoiningKingdom(Kingdom kingdom)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		foreach (PartyTemplateStack stack in kingdom.Culture.VassalRewardTroopsPartyTemplate.Stacks)
		{
			troopRoster.AddToCounts(stack.Character, stack.MaxValue);
		}
		return troopRoster;
	}
}
