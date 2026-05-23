using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class HeroHelper
{
	public static TextObject GetLastSeenText(Hero hero)
	{
		TextObject textObject;
		if (hero.LastKnownClosestSettlement == null)
		{
			textObject = GameTexts.FindText("str_never_seen_encyclopedia_entry");
		}
		else
		{
			textObject = GameTexts.FindText("str_last_seen_encyclopedia_entry");
			textObject.SetTextVariable("SETTLEMENT", hero.LastKnownClosestSettlement.EncyclopediaLinkWithName);
			textObject.SetTextVariable("IS_IN_SETTLEMENT", (hero.LastKnownClosestSettlement == hero.CurrentSettlement) ? 1 : 0);
		}
		return textObject;
	}

	public static Settlement GetClosestSettlement(Hero hero)
	{
		Settlement settlement = null;
		if (hero.CurrentSettlement != null)
		{
			settlement = hero.CurrentSettlement;
		}
		else
		{
			PartyBase partyBase = hero.PartyBelongedTo?.Party ?? hero.PartyBelongedToAsPrisoner;
			if (partyBase != null)
			{
				if (partyBase.IsSettlement)
				{
					settlement = partyBase.Settlement;
				}
				else if (partyBase.IsMobile)
				{
					MobileParty mobileParty = partyBase.MobileParty;
					float averageDistanceBetweenClosestTwoTownsWithNavigationType = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All);
					if (mobileParty.Position.IsValid())
					{
						float num = Campaign.MapDiagonalSquared;
						LocatableSearchData<Settlement> data = Settlement.StartFindingLocatablesAroundPosition(mobileParty.Position.ToVec2(), averageDistanceBetweenClosestTwoTownsWithNavigationType * 1.5f);
						Settlement settlement2 = Settlement.FindNextLocatable(ref data);
						while (settlement2 != null && (settlement2.IsVillage || settlement2.IsFortification))
						{
							float num2 = settlement2.Position.DistanceSquared(mobileParty.Position);
							if (num2 < num)
							{
								num = num2;
							}
							settlement2 = Settlement.FindNextLocatable(ref data);
						}
						settlement = settlement2 ?? SettlementHelper.FindNearestSettlementToMobileParty(mobileParty, MobileParty.NavigationType.All, (Settlement x) => x.IsVillage || x.IsFortification);
					}
					else
					{
						Debug.FailedAssert("Mobileparty is nowhere to be found", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetClosestSettlement", 2409);
					}
				}
			}
			else if (PlayerEncounter.Current != null && PlayerEncounter.Battle != null)
			{
				if (PlayerEncounter.Current.EncounterSettlementAux != null)
				{
					return PlayerEncounter.Current.EncounterSettlementAux;
				}
				BattleSideEnum otherSide = PlayerEncounter.Battle.GetOtherSide(PlayerEncounter.Battle.PlayerSide);
				if (PlayerEncounter.Battle.PartiesOnSide(otherSide).Any((MapEventParty x) => x.Party.Owner == hero))
				{
					settlement = SettlementHelper.FindNearestSettlementToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsVillage || x.IsFortification);
				}
			}
		}
		if (settlement != null && !settlement.IsVillage && !settlement.IsFortification)
		{
			settlement = SettlementHelper.FindNearestSettlementToSettlement(settlement, MobileParty.NavigationType.All, (Settlement x) => x.IsVillage || x.IsFortification);
		}
		return settlement;
	}

	public static bool LordWillConspireWithLord(Hero lord, Hero otherLord, bool suggestingBetrayal)
	{
		Hero.OneToOneConversationHero.MapFaction.Leader.SetTextVariables();
		int num = 0;
		num += otherLord.RandomInt(-9, 11);
		num += lord.GetTraitLevel(DefaultTraits.Honor);
		if (suggestingBetrayal)
		{
			num--;
		}
		if (suggestingBetrayal && Hero.OneToOneConversationHero.Clan == Hero.OneToOneConversationHero.MapFaction.Leader.Clan)
		{
			TextObject textObject = new TextObject("{=0M6ApEr2}Surely you know that {FIRST_NAME} is {RELATIONSHIP} as well as my liege, and will always be able to count on my loyalty.");
			textObject.SetTextVariable("FIRST_NAME", Hero.OneToOneConversationHero.MapFaction.Leader.FirstName);
			textObject.SetTextVariable("RELATIONSHIP", ConversationHelper.HeroRefersToHero(Hero.OneToOneConversationHero, Hero.OneToOneConversationHero.MapFaction.Leader, uppercaseFirst: true));
			MBTextManager.SetTextVariable("CONSPIRE_REFUSAL", textObject);
			return false;
		}
		if (num < 0)
		{
			if (suggestingBetrayal)
			{
				MBTextManager.SetTextVariable("CONSPIRE_REFUSAL", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_liege_support", lord.CharacterObject));
			}
			else
			{
				MBTextManager.SetTextVariable("CONSPIRE_REFUSAL", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_lord_intrigue_refuses", lord.CharacterObject));
			}
			return false;
		}
		return true;
	}

	public static bool UnderPlayerCommand(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		if ((hero.MapFaction == null || hero.MapFaction.Leader != Hero.MainHero) && (!hero.IsNotable || hero.HomeSettlement.OwnerClan != Hero.MainHero.Clan))
		{
			return hero.IsPlayerCompanion;
		}
		return true;
	}

	public static TextObject GetTitleInIndefiniteCase(Hero hero)
	{
		string text = hero.MapFaction.Culture.StringId;
		if (hero.IsFemale)
		{
			text += "_f";
		}
		if (hero.MapFaction.IsKingdomFaction && hero.MapFaction.Leader == hero)
		{
			return GameTexts.FindText("str_faction_ruler", text);
		}
		return GameTexts.FindText("str_faction_official", text);
	}

	public static TextObject GetCharacterTypeName(Hero hero)
	{
		if (hero.IsArtisan)
		{
			return GameTexts.FindText("str_charactertype_artisan");
		}
		if (hero.IsGangLeader)
		{
			return GameTexts.FindText("str_charactertype_gangleader");
		}
		if (hero.IsPreacher)
		{
			return GameTexts.FindText("str_charactertype_preacher");
		}
		if (hero.IsMerchant)
		{
			return GameTexts.FindText("str_charactertype_merchant");
		}
		if (hero.IsHeadman)
		{
			return GameTexts.FindText("str_charactertype_headman");
		}
		if (hero.IsRuralNotable)
		{
			return GameTexts.FindText("str_charactertype_ruralnotable");
		}
		if (hero.IsWanderer)
		{
			return GameTexts.FindText("str_charactertype_wanderer");
		}
		Clan clan = hero.Clan;
		if (clan != null && clan.IsClanTypeMercenary)
		{
			return GameTexts.FindText("str_charactertype_mercenary");
		}
		if (hero.IsMinorFactionHero)
		{
			return GameTexts.FindText("str_charactertype_minorfaction");
		}
		if (hero.IsLord)
		{
			if (hero.IsFemale)
			{
				return GameTexts.FindText("str_charactertype_lady");
			}
			return GameTexts.FindText("str_charactertype_lord");
		}
		return GameTexts.FindText("str_charactertype_unknown");
	}

	public static TextObject GetOccupiedEventReasonText(Hero hero)
	{
		if (!hero.CanHaveCampaignIssues())
		{
			return GameTexts.FindText("str_hero_busy_issue_quest");
		}
		return GameTexts.FindText("str_hero_busy");
	}

	public static List<string> OrderHeroesOnPlayerSideByPriority(bool includeArmyLeader = false, bool includePlayerCompanions = false)
	{
		List<Hero> list = new List<Hero>();
		foreach (MapEventParty item in MobileParty.MainParty.MapEvent.PartiesOnSide(MobileParty.MainParty.MapEvent.PlayerSide))
		{
			if (item.Party.LeaderHero != null && (includeArmyLeader || item.Party.MobileParty?.Army?.LeaderParty != item.Party.MobileParty))
			{
				list.Add(item.Party.LeaderHero);
			}
			if (!(item.Party.MobileParty == MobileParty.MainParty && includePlayerCompanions))
			{
				continue;
			}
			foreach (Hero companion in Clan.PlayerClan.Companions)
			{
				if (companion.PartyBelongedTo == MobileParty.MainParty)
				{
					list.Add(companion);
				}
			}
		}
		return list.OrderByDescending((Hero t) => Campaign.Current.Models.EncounterModel.GetCharacterSergeantScore(t)).ToList().ConvertAll((Hero t) => t.CharacterObject.StringId);
	}

	public static bool WillLordAttack()
	{
		if (PlayerEncounter.Current != null && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender && (PlayerEncounter.EncounteredMobileParty == null || PlayerEncounter.EncounteredMobileParty.Ai.DoNotAttackMainPartyUntil.IsPast))
		{
			if (Hero.OneToOneConversationHero == null)
			{
				return false;
			}
			if (Campaign.Current.CurrentConversationContext == ConversationContext.FreeOrCapturePrisonerHero || Campaign.Current.CurrentConversationContext == ConversationContext.CapturedLord || Hero.OneToOneConversationHero.IsPrisoner)
			{
				return false;
			}
			PartyBase partyBase = ((Campaign.Current.ConversationManager.ConversationParty == null) ? PlayerEncounter.EncounteredParty : Campaign.Current.ConversationManager.ConversationParty.Party);
			if (partyBase.Owner != null && partyBase.LeaderHero != null && FactionManager.IsAtWarAgainstFaction(partyBase.MapFaction, Hero.MainHero.MapFaction))
			{
				return true;
			}
		}
		return false;
	}

	public static void SetPlayerSalutation()
	{
		if (Hero.OneToOneConversationHero.IsLord)
		{
			MBTextManager.SetTextVariable("PLAYER_SALUTATION", Hero.MainHero.Name);
		}
		else if (Hero.OneToOneConversationHero.IsPlayerCompanion)
		{
			MBTextManager.SetTextVariable("PLAYER_SALUTATION", GameTexts.FindText("str_player_salutation_captain"));
		}
		else if (Hero.MainHero.IsFemale)
		{
			MBTextManager.SetTextVariable("PLAYER_SALUTATION", GameTexts.FindText("str_player_salutation_madame"));
		}
		else
		{
			MBTextManager.SetTextVariable("PLAYER_SALUTATION", GameTexts.FindText("str_player_salutation_sir"));
		}
	}

	public static void SpawnHeroForTheFirstTime(Hero hero, Settlement spawnSettlement)
	{
		hero.BornSettlement = spawnSettlement;
		EnterSettlementAction.ApplyForCharacterOnly(hero, spawnSettlement);
		hero.ChangeState(Hero.CharacterStates.Active);
	}

	public static int DefaultRelation(Hero hero, Hero otherHero)
	{
		int middleAdultHoodAge = Campaign.Current.Models.AgeModel.MiddleAdultHoodAge;
		if (hero.Clan != null && hero.Clan.IsNoble && hero.Clan == otherHero.Clan)
		{
			return 40;
		}
		if (hero.MapFaction == otherHero.MapFaction && hero.CharacterObject.Culture == otherHero.CharacterObject.Culture && hero.Age > (float)middleAdultHoodAge && otherHero.Age > (float)middleAdultHoodAge && NPCPersonalityClashWithNPC(hero, otherHero) > 40)
		{
			return -5;
		}
		if (hero.MapFaction == otherHero.MapFaction && hero.CharacterObject.Culture == otherHero.CharacterObject.Culture && hero.Age > (float)middleAdultHoodAge && otherHero.Age > (float)middleAdultHoodAge)
		{
			return 25;
		}
		if (hero.MapFaction == otherHero.MapFaction && hero.CharacterObject.Culture == otherHero.CharacterObject.Culture)
		{
			return 10;
		}
		return 0;
	}

	public static bool IsCompanionInPlayerParty(Hero hero)
	{
		if (hero != null && hero.IsPlayerCompanion)
		{
			return hero.PartyBelongedTo == MobileParty.MainParty;
		}
		return false;
	}

	public static bool NPCPoliticalDifferencesWithNPC(Hero firstNPC, Hero secondNPC)
	{
		bool num = firstNPC.GetTraitLevel(DefaultTraits.Egalitarian) > 0;
		bool flag = firstNPC.GetTraitLevel(DefaultTraits.Oligarchic) > 0;
		bool flag2 = firstNPC.GetTraitLevel(DefaultTraits.Authoritarian) > 0;
		bool flag3 = secondNPC.GetTraitLevel(DefaultTraits.Egalitarian) > 0;
		bool flag4 = secondNPC.GetTraitLevel(DefaultTraits.Oligarchic) > 0;
		bool flag5 = secondNPC.GetTraitLevel(DefaultTraits.Authoritarian) > 0;
		if (num != flag3)
		{
			return true;
		}
		if (flag != flag4)
		{
			return true;
		}
		if (flag2 != flag5)
		{
			return true;
		}
		return false;
	}

	public static int NPCPersonalityClashWithNPC(Hero firstNPC, Hero secondNPC)
	{
		int num = 0;
		foreach (TraitObject item in DefaultTraits.Personality)
		{
			int traitLevel = firstNPC.CharacterObject.GetTraitLevel(item);
			int traitLevel2 = secondNPC.CharacterObject.GetTraitLevel(item);
			if ((traitLevel > 0 && traitLevel2 < 0) || (traitLevel < 0 && traitLevel2 > 0))
			{
				num--;
			}
			if ((traitLevel > 0 && traitLevel2 > 0) || (traitLevel < 0 && traitLevel2 < 0))
			{
				num++;
			}
		}
		return num * 5;
	}

	public static int TraitHarmony(Hero considerer, TraitObject trait, Hero consideree, bool sensitive)
	{
		int traitLevel = considerer.GetTraitLevel(trait);
		int traitLevel2 = consideree.GetTraitLevel(trait);
		if (traitLevel > 0 && traitLevel2 > 0)
		{
			return 3;
		}
		if (traitLevel == 0 && traitLevel2 > 0)
		{
			return 1;
		}
		if (traitLevel < 0 && traitLevel2 < 0)
		{
			return 1;
		}
		if (traitLevel > 0 && traitLevel2 < 0)
		{
			return -3;
		}
		if (traitLevel == 0 && traitLevel2 < 0)
		{
			return -1;
		}
		if (traitLevel < 0 && traitLevel2 > 0)
		{
			return -1;
		}
		return 0;
	}

	public static float CalculateReliabilityConstant(Hero hero, float maxValueConstant = 1f)
	{
		int traitLevel = hero.GetTraitLevel(DefaultTraits.Honor);
		return maxValueConstant * ((2.5f + (float)MathF.Min(2, MathF.Max(-2, traitLevel))) / 5f);
	}

	public static void SetPropertiesToTextObject(this Hero hero, TextObject textObject, string tagName)
	{
		StringHelpers.SetCharacterProperties(tagName, hero.CharacterObject, textObject);
	}

	public static void SetPropertiesToTextObject(this Settlement settlement, TextObject textObject, string tagName)
	{
		StringHelpers.SetSettlementProperties(tagName, settlement, textObject);
	}

	public static bool HeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int index)
	{
		return index <= Campaign.Current.Models.VolunteerModel.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero);
	}

	public static List<CharacterObject> GetVolunteerTroopsOfHeroForRecruitment(Hero hero)
	{
		List<CharacterObject> list = new List<CharacterObject>();
		if (hero.IsAlive)
		{
			for (int i = 0; i < 6; i++)
			{
				list.Add(hero.VolunteerTypes[i]);
			}
		}
		return list;
	}

	public static Clan GetRandomClanForNotable(Hero notable)
	{
		float num = 0f;
		List<Clan> list = new List<Clan>();
		if (notable.IsPreacher)
		{
			num = 0.5f;
			list = Clan.NonBanditFactions.Where((Clan x) => x.IsSect).ToList();
		}
		if (notable.IsGangLeader)
		{
			num = 0.5f;
			list = Clan.NonBanditFactions.Where((Clan x) => x.IsMafia).ToList();
		}
		if (MBRandom.RandomFloat >= num)
		{
			return null;
		}
		foreach (Hero notable2 in notable.HomeSettlement.Notables)
		{
			if (list.Contains(notable2.SupporterOf))
			{
				list.Remove(notable2.SupporterOf);
			}
		}
		float num2 = 0f;
		ILookup<Clan, Settlement> lookup = Settlement.All.Where((Settlement x) => x.IsTown || x.IsHideout).ToLookup((Settlement x) => x.OwnerClan);
		foreach (Clan item in list)
		{
			num2 += GetProbabilityForClan(item, lookup[item], notable);
		}
		num2 *= MBRandom.RandomFloat;
		foreach (Clan item2 in list)
		{
			num2 -= GetProbabilityForClan(item2, lookup[item2], notable);
			if (num2 <= 0f)
			{
				return item2;
			}
		}
		return null;
	}

	private static float GetProbabilityForClan(Clan clan, IEnumerable<Settlement> applicableSettlements, Hero notable)
	{
		float num = 1f;
		if (clan.Culture == notable.Culture)
		{
			num *= 3f;
		}
		float num2 = float.MaxValue;
		foreach (Settlement applicableSettlement in applicableSettlements)
		{
			float num3 = applicableSettlement.Position.DistanceSquared(notable.HomeSettlement.Position);
			if (num3 < num2)
			{
				num2 = num3;
			}
		}
		return num / num2;
	}

	public static CampaignTime GetRandomBirthDayForAge(float age)
	{
		float valueInDays = MBRandom.RandomFloatRanged(0f, CampaignTime.Now.GetDayOfYear);
		float valueInYears = (float)CampaignTime.Now.GetYear - age;
		return CampaignTime.Days(valueInDays) + CampaignTime.Years(valueInYears);
	}

	public static void GetRandomDeathDayAndBirthDay(int deathAge, out CampaignTime birthday, out CampaignTime deathday)
	{
		int daysInYear = CampaignTime.DaysInYear;
		int num = MBRandom.RandomInt(daysInYear);
		birthday = CampaignTime.Years(CampaignTime.Now.GetYear - deathAge - 1) - CampaignTime.Days(num);
		deathday = birthday + CampaignTime.Years(deathAge) + CampaignTime.Days(MBRandom.RandomInt(daysInYear - 1));
	}

	public static float StartRecruitingMoneyLimit(Hero hero)
	{
		if (hero.Clan == Clan.PlayerClan)
		{
			return 0f;
		}
		return 50f + ((hero.PartyBelongedTo != null) ? ((float)MathF.Min(150, hero.PartyBelongedTo.MemberRoster.TotalManCount) * 20f) : 0f);
	}

	public static float StartRecruitingMoneyLimitForClanLeader(Hero hero)
	{
		if (hero.Clan == Clan.PlayerClan)
		{
			return 0f;
		}
		return 50f + ((hero.Clan != null && hero.Clan.Leader != null && hero.Clan.Leader.PartyBelongedTo != null) ? ((float)hero.Clan.Leader.PartyBelongedTo.TotalWage + (float)hero.Clan.Leader.PartyBelongedTo.MemberRoster.TotalManCount * 40f) : 0f);
	}

	public static TextObject GetPersonalityTraitChangeName(TraitObject traitObject, Hero hero, bool isPositive)
	{
		if (DefaultTraits.Personality.Contains(traitObject))
		{
			int traitLevel = hero.GetTraitLevel(traitObject);
			string id = "str_trait_name_" + traitObject.StringId.ToLower();
			string variation = (isPositive ? "3" : "1");
			if (traitLevel < 0)
			{
				variation = (isPositive ? "3" : "0");
			}
			else if (traitLevel > 0)
			{
				variation = (isPositive ? "4" : "1");
			}
			return GameTexts.FindText(id, variation);
		}
		Debug.FailedAssert("Given trait is not a personality trait!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetPersonalityTraitChangeName", 2983);
		return TextObject.GetEmpty();
	}
}
