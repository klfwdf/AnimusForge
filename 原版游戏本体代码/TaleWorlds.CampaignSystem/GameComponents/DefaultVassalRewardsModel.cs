using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000161 RID: 353
	public class DefaultVassalRewardsModel : VassalRewardsModel
	{
		// Token: 0x170006D7 RID: 1751
		// (get) Token: 0x06001AC5 RID: 6853 RVA: 0x0008976D File Offset: 0x0008796D
		public override int RelationRewardWithLeader
		{
			get
			{
				return 10;
			}
		}

		// Token: 0x170006D8 RID: 1752
		// (get) Token: 0x06001AC6 RID: 6854 RVA: 0x00089771 File Offset: 0x00087971
		public override float InfluenceReward
		{
			get
			{
				return 10f;
			}
		}

		// Token: 0x06001AC7 RID: 6855 RVA: 0x00089778 File Offset: 0x00087978
		public override ItemRoster GetEquipmentRewardsForJoiningKingdom(Kingdom kingdom)
		{
			ItemRoster itemRoster = new ItemRoster();
			foreach (ItemObject item in kingdom.Culture.VassalRewardItems)
			{
				itemRoster.AddToCounts(item, 1);
			}
			ItemObject randomBannerAtLevel = this.GetRandomBannerAtLevel(2, kingdom.Culture);
			if (randomBannerAtLevel != null)
			{
				itemRoster.AddToCounts(randomBannerAtLevel, 1);
			}
			return itemRoster;
		}

		// Token: 0x06001AC8 RID: 6856 RVA: 0x000897F4 File Offset: 0x000879F4
		private ItemObject GetRandomBannerAtLevel(int bannerLevel, CultureObject culture = null)
		{
			MBList<ItemObject> e = Campaign.Current.Models.BannerItemModel.GetPossibleRewardBannerItems().ToMBList<ItemObject>();
			if (culture == null)
			{
				return e.GetRandomElementWithPredicate((ItemObject i) => (i.ItemComponent as BannerComponent).BannerLevel == bannerLevel);
			}
			return e.GetRandomElementWithPredicate((ItemObject i) => (i.ItemComponent as BannerComponent).BannerLevel == bannerLevel && i.Culture == culture);
		}

		// Token: 0x06001AC9 RID: 6857 RVA: 0x0008985C File Offset: 0x00087A5C
		public override TroopRoster GetTroopRewardsForJoiningKingdom(Kingdom kingdom)
		{
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			foreach (PartyTemplateStack partyTemplateStack in kingdom.Culture.VassalRewardTroopsPartyTemplate.Stacks)
			{
				troopRoster.AddToCounts(partyTemplateStack.Character, partyTemplateStack.MaxValue, false, 0, 0, true, -1);
			}
			return troopRoster;
		}

		// Token: 0x040008F2 RID: 2290
		private const int VassalRewardBannerLevel = 2;
	}
}
