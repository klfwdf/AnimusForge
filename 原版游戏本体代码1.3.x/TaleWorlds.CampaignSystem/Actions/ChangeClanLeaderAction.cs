using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x02000496 RID: 1174
	public static class ChangeClanLeaderAction
	{
		// Token: 0x0600494D RID: 18765 RVA: 0x00170E00 File Offset: 0x0016F000
		private static void ApplyInternal(Clan clan, Hero newLeader = null)
		{
			Hero leader = clan.Leader;
			if (newLeader == null)
			{
				Dictionary<Hero, int> heirApparents = clan.GetHeirApparents();
				if (heirApparents.Count == 0)
				{
					return;
				}
				int highestPoint = (from h in heirApparents
					orderby h.Value descending
					select h).FirstOrDefault<KeyValuePair<Hero, int>>().Value;
				newLeader = (from h in heirApparents
					where h.Value.Equals(highestPoint)
					select h).GetRandomElementInefficiently<KeyValuePair<Hero, int>>().Key;
			}
			GiveGoldAction.ApplyBetweenCharacters(leader, newLeader, leader.Gold, true);
			if (newLeader.GovernorOf != null)
			{
				ChangeGovernorAction.RemoveGovernorOf(newLeader);
			}
			if (!newLeader.IsPrisoner && !newLeader.IsFugitive && !newLeader.IsReleased && !newLeader.IsTraveling)
			{
				MobileParty mobileParty = newLeader.PartyBelongedTo;
				if (mobileParty == null)
				{
					mobileParty = MobilePartyHelper.CreateNewClanMobileParty(newLeader, clan);
				}
				if (mobileParty.LeaderHero != newLeader)
				{
					mobileParty.ChangePartyLeader(newLeader);
				}
			}
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				if (hero != newLeader)
				{
					int relationChangeAfterClanLeaderIsDead = Campaign.Current.Models.DiplomacyModel.GetRelationChangeAfterClanLeaderIsDead(leader, hero);
					int heroRelation = CharacterRelationManager.GetHeroRelation(newLeader, hero);
					newLeader.SetPersonalRelation(hero, heroRelation + relationChangeAfterClanLeaderIsDead);
				}
			}
			clan.SetLeader(newLeader);
			CampaignEventDispatcher.Instance.OnClanLeaderChanged(leader, newLeader);
		}

		// Token: 0x0600494E RID: 18766 RVA: 0x00170F74 File Offset: 0x0016F174
		public static void ApplyWithSelectedNewLeader(Clan clan, Hero newLeader)
		{
			ChangeClanLeaderAction.ApplyInternal(clan, newLeader);
		}

		// Token: 0x0600494F RID: 18767 RVA: 0x00170F7D File Offset: 0x0016F17D
		public static void ApplyWithoutSelectedNewLeader(Clan clan)
		{
			ChangeClanLeaderAction.ApplyInternal(clan, null);
		}
	}
}
