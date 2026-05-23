using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors;

public class DiplomaticBartersBehavior : CampaignBehaviorBase
{
	private const int MinimumDaysOfTributeNeededForPeace = 5;

	private const float IndependentClanLikelihoodThresholdToMakePeace = 0.5f;

	private const float IndependentClanPeaceConsiderChance = 0.5f;

	private const float IndependentClanConsiderPeaceWithAnotherClanChance = 0.5f;

	private const float ClanLeaveKingdomChance = 0.4f;

	private const float ClanConsideringWarDeclarationChance = 0.7f;

	private const int IndependentClanLeaderMinimumRelationForDeclaringPeaceWithKingdom = -65;

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
	}

	private void DailyTickClan(Clan clan)
	{
		bool flag = false;
		foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
		{
			if (warPartyComponent.MobileParty.MapEvent != null)
			{
				flag = true;
				break;
			}
		}
		MBList<Clan> e = Clan.NonBanditFactions.ToMBList();
		if (clan == Clan.PlayerClan || clan.CurrentTotalStrength <= 0f || clan.IsEliminated || clan.IsBanditFaction || clan.IsRebelClan)
		{
			return;
		}
		if (clan.Kingdom == null && MBRandom.RandomFloat < 0.5f)
		{
			if (MBRandom.RandomFloat < 0.5f)
			{
				Clan randomElement = e.GetRandomElement();
				if (randomElement.Kingdom == null && randomElement != Clan.PlayerClan && clan.IsAtWarWith(randomElement) && !clan.IsMinorFaction && !randomElement.IsMinorFaction)
				{
					ConsiderPeace(clan, randomElement);
				}
				return;
			}
			bool flag2 = true;
			if (clan.Settlements.Count > 0 && MBRandom.RandomFloat < 0.5f)
			{
				flag2 = false;
			}
			if (!flag2)
			{
				return;
			}
			Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate((Kingdom x) => x.IsAtWarWith(clan) && !x.IsAtConstantWarWith(clan));
			if (randomElementWithPredicate != null && randomElementWithPredicate != Clan.PlayerClan.Kingdom)
			{
				int relation = clan.Leader.GetRelation(randomElementWithPredicate.Leader);
				if (relation > -65 && MBMath.Map(relation, -100f, 100f, 0f, 1f) < MBRandom.RandomFloat)
				{
					MakePeaceAction.Apply(clan, randomElementWithPredicate);
				}
			}
		}
		else if (MBRandom.RandomFloat < 0.2f && !clan.IsUnderMercenaryService && clan.Kingdom != null && !clan.IsClanTypeMercenary)
		{
			if (!(MBRandom.RandomFloat < 0.1f))
			{
				return;
			}
			Clan randomElement2 = e.GetRandomElement();
			int num = 0;
			while (randomElement2.Kingdom == null || clan.Kingdom == randomElement2.Kingdom || randomElement2.IsEliminated)
			{
				randomElement2 = e.GetRandomElement();
				num++;
				if (num >= 20)
				{
					break;
				}
			}
			if (randomElement2.Kingdom != null && clan.Kingdom != randomElement2.Kingdom && !Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(clan, randomElement2.Kingdom) && !flag && randomElement2.MapFaction.IsKingdomFaction && !randomElement2.IsEliminated && randomElement2 != Clan.PlayerClan && randomElement2.MapFaction.Leader != Hero.MainHero && clan.WarPartyComponents.All((WarPartyComponent x) => x.MobileParty.MapEvent == null))
			{
				ConsiderDefection(clan, randomElement2.MapFaction as Kingdom);
			}
		}
		else if (MBRandom.RandomFloat < ((clan.MapFaction.Leader == Hero.MainHero) ? 0.2f : 0.4f))
		{
			Kingdom kingdom = Kingdom.All[MBRandom.RandomInt(Kingdom.All.Count)];
			int num2 = 0;
			foreach (Kingdom item in Kingdom.All)
			{
				num2 = ((item.Culture != clan.Culture) ? (num2 + 1) : (num2 + 10));
			}
			int num3 = (int)(MBRandom.RandomFloat * (float)num2);
			foreach (Kingdom item2 in Kingdom.All)
			{
				num3 = ((item2.Culture != clan.Culture) ? (num3 - 1) : (num3 - 10));
				if (num3 < 0)
				{
					kingdom = item2;
					break;
				}
			}
			if (kingdom.Leader == Hero.MainHero || kingdom.IsEliminated || (clan.Kingdom != null && !clan.IsUnderMercenaryService) || clan.MapFaction == kingdom || clan.MapFaction.IsAtWarWith(kingdom) || Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(clan, kingdom) || !clan.WarPartyComponents.All((WarPartyComponent x) => x.MobileParty.MapEvent == null) || !clan.ShouldStayInKingdomUntil.IsPast)
			{
				return;
			}
			bool flag3 = true;
			if (!clan.IsMinorFaction)
			{
				foreach (Kingdom item3 in Kingdom.All)
				{
					if (item3 != kingdom && clan.IsAtWarWith(item3) && !item3.IsAtWarWith(kingdom) && !(kingdom.CurrentTotalStrength > 10f * item3.CurrentTotalStrength))
					{
						flag3 = false;
						break;
					}
				}
			}
			if (flag3)
			{
				if (clan.IsMinorFaction)
				{
					ConsiderClanJoinAsMercenary(clan, kingdom);
				}
				else
				{
					ConsiderClanJoin(clan, kingdom);
				}
			}
		}
		else if (MBRandom.RandomFloat < 0.4f)
		{
			if (clan.Kingdom != null && !flag && clan.Kingdom.RulingClan != clan && clan != Clan.PlayerClan && clan.ShouldStayInKingdomUntil.IsPast && clan.WarPartyComponents.All((WarPartyComponent x) => x.MobileParty.MapEvent == null))
			{
				if (clan.IsMinorFaction)
				{
					ConsiderClanLeaveAsMercenary(clan);
				}
				else
				{
					ConsiderClanLeaveKingdom(clan);
				}
			}
		}
		else if (MBRandom.RandomFloat < 0.7f)
		{
			Clan randomElement3 = e.GetRandomElement();
			IFaction mapFaction = randomElement3.MapFaction;
			if (!clan.IsMinorFaction && (!mapFaction.IsMinorFaction || mapFaction == Clan.PlayerClan) && clan.Kingdom == null && randomElement3 != clan && !mapFaction.IsEliminated && mapFaction.WarPartyComponents.Count > 0 && clan.WarPartyComponents.Count > 0 && !clan.IsAtWarWith(mapFaction) && clan != Clan.PlayerClan)
			{
				ConsiderWar(clan, mapFaction);
			}
		}
	}

	private void ConsiderClanLeaveKingdom(Clan clan)
	{
		LeaveKingdomAsClanBarterable leaveKingdomAsClanBarterable = new LeaveKingdomAsClanBarterable(clan.Leader, null);
		if (leaveKingdomAsClanBarterable.GetValueForFaction(clan) > 0)
		{
			leaveKingdomAsClanBarterable.Apply();
		}
	}

	private void ConsiderClanLeaveAsMercenary(Clan clan)
	{
		LeaveKingdomAsClanBarterable leaveKingdomAsClanBarterable = new LeaveKingdomAsClanBarterable(clan.Leader, null);
		if (leaveKingdomAsClanBarterable.GetValueForFaction(clan) > 500)
		{
			leaveKingdomAsClanBarterable.Apply();
		}
	}

	private void ConsiderClanJoin(Clan clan, Kingdom kingdom)
	{
		JoinKingdomAsClanBarterable joinKingdomAsClanBarterable = new JoinKingdomAsClanBarterable(clan.Leader, kingdom);
		if (joinKingdomAsClanBarterable.GetValueForFaction(clan) + joinKingdomAsClanBarterable.GetValueForFaction(kingdom) > 0)
		{
			Campaign.Current.BarterManager.ExecuteAiBarter(clan, kingdom, clan.Leader, kingdom.Leader, joinKingdomAsClanBarterable);
		}
	}

	private void ConsiderClanJoinAsMercenary(Clan clan, Kingdom kingdom)
	{
		MercenaryJoinKingdomBarterable mercenaryJoinKingdomBarterable = new MercenaryJoinKingdomBarterable(clan.Leader, null, kingdom);
		if (mercenaryJoinKingdomBarterable.GetValueForFaction(clan) + mercenaryJoinKingdomBarterable.GetValueForFaction(kingdom) > 0)
		{
			Campaign.Current.BarterManager.ExecuteAiBarter(clan, kingdom, clan.Leader, kingdom.Leader, mercenaryJoinKingdomBarterable);
		}
	}

	private void ConsiderDefection(Clan clan1, Kingdom kingdom)
	{
		JoinKingdomAsClanBarterable joinKingdomAsClanBarterable = new JoinKingdomAsClanBarterable(clan1.Leader, kingdom, isDefecting: true);
		int valueForFaction = joinKingdomAsClanBarterable.GetValueForFaction(clan1);
		int valueForFaction2 = joinKingdomAsClanBarterable.GetValueForFaction(kingdom);
		int num = valueForFaction + valueForFaction2;
		int num2 = 0;
		if (valueForFaction < 0)
		{
			num2 = -valueForFaction;
		}
		if (num > 0 && (float)num2 <= (float)kingdom.Leader.Gold * 0.5f)
		{
			Campaign.Current.BarterManager.ExecuteAiBarter(clan1, kingdom, clan1.Leader, kingdom.Leader, joinKingdomAsClanBarterable);
		}
	}

	private void ConsiderPeace(Clan clan1, Clan clan2)
	{
		PeaceBarterable peaceBarterable = new PeaceBarterable(clan1.Leader, clan1.MapFaction, clan2.MapFaction, CampaignTime.Years(1f));
		if (peaceBarterable.GetValueForFaction(clan1) + peaceBarterable.GetValueForFaction(clan2) > 0)
		{
			Campaign.Current.BarterManager.ExecuteAiBarter(clan1, clan2, clan1.Leader, clan2.Leader, peaceBarterable);
		}
	}

	private void ConsiderWar(Clan clan, IFaction otherMapFaction)
	{
		DeclareWarBarterable declareWarBarterable = new DeclareWarBarterable(clan, otherMapFaction);
		if (declareWarBarterable.GetValueForFaction(clan) > 1000)
		{
			declareWarBarterable.Apply();
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
