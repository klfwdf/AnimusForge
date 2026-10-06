using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class SellPrisonersAction
{
	private static void ApplyInternal(PartyBase sellerParty, PartyBase buyerParty, TroopRoster prisoners, bool applyConsequences)
	{
		Settlement settlement = sellerParty.Settlement ?? buyerParty?.Settlement;
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		int num = 0;
		bool flag = false;
		foreach (TroopRosterElement item in prisoners.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (!character.IsHero)
			{
				if (applyConsequences)
				{
					sellerParty.PrisonRoster.AddToCounts(character, -item.Number, insertAtFront: false, -item.WoundedNumber);
				}
			}
			else if (character.HeroObject != Hero.MainHero)
			{
				if (buyerParty != null)
				{
					if (!buyerParty.MapFaction.IsAtWarWith(character.HeroObject.MapFaction))
					{
						if (character.HeroObject.Clan == Clan.PlayerClan)
						{
							EndCaptivityAction.ApplyByReleasedByCompensation(character.HeroObject);
						}
						else
						{
							EndCaptivityAction.ApplyByRansom(character.HeroObject, null);
						}
					}
					else
					{
						if (sellerParty.MapFaction == buyerParty.MapFaction && sellerParty != PartyBase.MainParty)
						{
							flag = true;
							troopRoster.Add(item);
						}
						TransferPrisonerAction.Apply(character, sellerParty, buyerParty);
					}
				}
				else
				{
					EndCaptivityAction.ApplyByRansom(character.HeroObject, null);
				}
				if (settlement != null)
				{
					CampaignEventDispatcher.Instance.OnPrisonersChangeInSettlement(settlement, null, character.HeroObject, takenFromDungeon: false);
				}
			}
			if (applyConsequences && !flag && character != Hero.MainHero.CharacterObject)
			{
				int num2 = Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(character, sellerParty?.LeaderHero);
				num += item.Number * num2;
			}
		}
		if (applyConsequences && !flag && num > 0)
		{
			if (sellerParty.IsMobile)
			{
				Hero recipientHero = null;
				if (sellerParty.LeaderHero != null && sellerParty.LeaderHero.HeroState == Hero.CharacterStates.Active)
				{
					recipientHero = sellerParty.LeaderHero;
				}
				else if (sellerParty.Owner != null && sellerParty.Owner.HeroState == Hero.CharacterStates.Active)
				{
					recipientHero = sellerParty.Owner;
				}
				else if (sellerParty.MobileParty.ActualClan?.Leader != null)
				{
					recipientHero = sellerParty.MobileParty.ActualClan.Leader;
				}
				GiveGoldAction.ApplyBetweenCharacters(null, recipientHero, num);
			}
			else
			{
				bool disableNotification = buyerParty?.Settlement?.OwnerClan != Clan.PlayerClan;
				GiveGoldAction.ApplyForPartyToSettlement(null, sellerParty.Settlement, num, disableNotification);
			}
		}
		if (sellerParty.IsMobile)
		{
			SkillLevelingManager.OnPrisonerSell(sellerParty.MobileParty, in prisoners);
		}
		CampaignEventDispatcher.Instance.OnPrisonerSold(sellerParty, buyerParty, prisoners);
		if (settlement != null && troopRoster.Count > 0)
		{
			CampaignEventDispatcher.Instance.OnPrisonerDonatedToSettlement(sellerParty.MobileParty, troopRoster.ToFlattenedRoster(), settlement);
		}
	}

	public static void ApplyForAllPrisoners(PartyBase sellerParty, PartyBase buyerParty)
	{
		ApplyInternal(sellerParty, buyerParty, sellerParty.PrisonRoster.CloneRosterData(), applyConsequences: true);
	}

	public static void ApplyForSelectedPrisoners(PartyBase sellerParty, PartyBase buyerParty, TroopRoster prisoners)
	{
		ApplyInternal(sellerParty, buyerParty, prisoners, applyConsequences: true);
	}

	public static void ApplyByPartyScreen(TroopRoster prisoners)
	{
		ApplyInternal(PartyBase.MainParty, Hero.MainHero.CurrentSettlement.Party, prisoners, applyConsequences: false);
	}
}
