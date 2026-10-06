using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeClanLeaderAction
{
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
			int highestPoint = heirApparents.OrderByDescending((KeyValuePair<Hero, int> h) => h.Value).FirstOrDefault().Value;
			newLeader = heirApparents.Where((KeyValuePair<Hero, int> h) => h.Value.Equals(highestPoint)).GetRandomElementInefficiently().Key;
		}
		GiveGoldAction.ApplyBetweenCharacters(leader, newLeader, leader.Gold, disableNotification: true);
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
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			if (allAliveHero != newLeader)
			{
				int relationChangeAfterClanLeaderIsDead = Campaign.Current.Models.DiplomacyModel.GetRelationChangeAfterClanLeaderIsDead(leader, allAliveHero);
				int heroRelation = CharacterRelationManager.GetHeroRelation(newLeader, allAliveHero);
				newLeader.SetPersonalRelation(allAliveHero, heroRelation + relationChangeAfterClanLeaderIsDead);
			}
		}
		clan.SetLeader(newLeader);
		CampaignEventDispatcher.Instance.OnClanLeaderChanged(leader, newLeader);
	}

	public static void ApplyWithSelectedNewLeader(Clan clan, Hero newLeader)
	{
		ApplyInternal(clan, newLeader);
	}

	public static void ApplyWithoutSelectedNewLeader(Clan clan)
	{
		ApplyInternal(clan);
	}
}
