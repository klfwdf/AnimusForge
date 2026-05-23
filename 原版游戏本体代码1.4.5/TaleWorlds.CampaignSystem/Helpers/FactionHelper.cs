using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class FactionHelper
{
	public static float FindPotentialStrength(IFaction faction)
	{
		float num = 0f;
		if (faction.IsKingdomFaction)
		{
			Kingdom kingdom = (Kingdom)faction;
			foreach (Clan clan in kingdom.Clans)
			{
				float num2 = (clan.IsUnderMercenaryService ? (((float)kingdom.Leader.Gold > 100000f) ? 0.3f : (0.3f - (1f - (float)kingdom.Leader.Gold / 100000f) * 0.3f)) : 1f);
				num += num2 * (float)clan.Tier * 100f;
			}
		}
		else if (faction.IsClan)
		{
			num += (float)((Clan)faction).Tier * 100f;
		}
		return num * 2f;
	}

	public static IEnumerable<Kingdom> GetEnemyKingdoms(IFaction faction)
	{
		return faction.FactionsAtWarWith.Where((IFaction x) => x.IsKingdomFaction).Cast<Kingdom>();
	}

	public static IEnumerable<StanceLink> GetStances(IFaction faction)
	{
		List<StanceLink> list = new List<StanceLink>();
		foreach (Kingdom item in Kingdom.All)
		{
			if (item != faction)
			{
				StanceLink stanceWith = faction.GetStanceWith(item);
				if (stanceWith != null)
				{
					list.Add(stanceWith);
				}
			}
		}
		foreach (Clan item2 in Clan.All)
		{
			if (item2 != faction)
			{
				StanceLink stanceWith2 = faction.GetStanceWith(item2);
				if (stanceWith2 != null)
				{
					list.Add(stanceWith2);
				}
			}
		}
		return list;
	}

	public static float GetPowerRatioToEnemies(Kingdom kingdom)
	{
		float currentTotalStrength = kingdom.CurrentTotalStrength;
		float totalEnemyKingdomPower = GetTotalEnemyKingdomPower(kingdom);
		return currentTotalStrength / (totalEnemyKingdomPower + 0.0001f);
	}

	private static List<TextObject> IsFactionNameApplicable(string name)
	{
		List<TextObject> list = new List<TextObject>();
		if (name == null)
		{
			Debug.FailedAssert("Calling IsFactionNameApplicable with null string!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "IsFactionNameApplicable", 5504);
			name = string.Empty;
		}
		if (name.Length > 50 || name.Length < 1)
		{
			TextObject item = GameTexts.FindText("str_faction_name_invalid_character_count").SetTextVariable("MIN", 1).SetTextVariable("MAX", 50);
			list.Add(item);
		}
		if (Common.TextContainsSpecialCharacters(name))
		{
			list.Add(GameTexts.FindText("str_faction_name_invalid_characters"));
		}
		if (name.StartsWith(" ") || name.EndsWith(" "))
		{
			list.Add(new TextObject("{=LCOZZMta}Faction name cannot start or end with a white space"));
		}
		if (name.Contains("  "))
		{
			list.Add(new TextObject("{=CtsdrQ9N}Faction name cannot contain consecutive white spaces"));
		}
		return list;
	}

	public static Tuple<bool, string> IsClanNameApplicable(string name)
	{
		string item = string.Empty;
		List<TextObject> list = IsFactionNameApplicable(name);
		MBReadOnlyList<Clan> all = Clan.All;
		if (all != null && all.Any((Clan x) => x != Clan.PlayerClan && string.Equals(x.Name.ToString(), name, StringComparison.InvariantCultureIgnoreCase)))
		{
			list.Add(GameTexts.FindText("str_clan_name_invalid_already_exist"));
		}
		bool item2 = list.Count == 0;
		if (list.Count == 1)
		{
			item = list[0].ToString();
		}
		else if (list.Count > 1)
		{
			TextObject textObject = list[0];
			for (int num = 1; num < list.Count; num++)
			{
				textObject = GameTexts.FindText("str_string_newline_newline_string").SetTextVariable("STR1", textObject.ToString()).SetTextVariable("STR2", list[num].ToString());
			}
			item = textObject.ToString();
		}
		return new Tuple<bool, string>(item2, item);
	}

	public static Tuple<bool, string> IsKingdomNameApplicable(string name)
	{
		string item = string.Empty;
		List<TextObject> list = IsFactionNameApplicable(name);
		MBReadOnlyList<Kingdom> all = Kingdom.All;
		if (all != null && all.Any((Kingdom x) => x != Clan.PlayerClan.Kingdom && string.Equals(x.Name.ToString(), name, StringComparison.InvariantCultureIgnoreCase)))
		{
			list.Add(GameTexts.FindText("str_kingdom_name_invalid_already_exist"));
		}
		bool item2 = list.Count == 0;
		if (list.Count == 1)
		{
			item = list[0].ToString();
		}
		else if (list.Count > 1)
		{
			TextObject textObject = list[0];
			for (int num = 1; num < list.Count; num++)
			{
				textObject = GameTexts.FindText("str_string_newline_newline_string").SetTextVariable("STR1", textObject.ToString()).SetTextVariable("STR2", list[num].ToString());
			}
			item = textObject.ToString();
		}
		return new Tuple<bool, string>(item2, item);
	}

	public static float GetPowerRatioToTributePayedKingdoms(Kingdom kingdom)
	{
		float currentTotalStrength = kingdom.CurrentTotalStrength;
		float totalTributePayedKingdomsPower = GetTotalTributePayedKingdomsPower(kingdom);
		return currentTotalStrength / (totalTributePayedKingdomsPower + 0.0001f);
	}

	public static bool CanClanBeGrantedFief(Clan clan)
	{
		if (clan != Clan.PlayerClan)
		{
			return !clan.IsUnderMercenaryService;
		}
		return false;
	}

	public static bool CanPlayerEnterFaction(bool asVassal = false)
	{
		float num = Campaign.Current.Settlements.Where((Settlement settlement) => (settlement.IsVillage || settlement.IsTown || settlement.IsCastle) && settlement.OwnerClan.Leader == Hero.MainHero).Sum((Settlement settlement) => settlement.GetSettlementValueForFaction(Hero.OneToOneConversationHero.MapFaction));
		float num2 = (asVassal ? 50f : 10f);
		float num3 = Clan.PlayerClan.Renown + (asVassal ? (num / 5000f) : 0f) + (asVassal ? ((float)Hero.MainHero.Gold / 10000f) : 0f) + TaleWorlds.Library.MathF.Min(num2, Clan.PlayerClan.Renown) / num2 * 0.2f * Clan.PlayerClan.CurrentTotalStrength + Hero.OneToOneConversationHero.MapFaction.Leader.GetRelationWithPlayer() * 2f;
		if (!asVassal)
		{
			return num3 > 25f;
		}
		return num3 > 150f;
	}

	public static float GetTotalEnemyKingdomPower(Kingdom kingdom)
	{
		float num = 0f;
		foreach (Kingdom enemyKingdom in GetEnemyKingdoms(kingdom))
		{
			num += enemyKingdom.CurrentTotalStrength;
		}
		return num;
	}

	public static float GetTotalTributePayedKingdomsPower(Kingdom kingdom)
	{
		float num = 0f;
		foreach (StanceLink stance in GetStances(kingdom))
		{
			IFaction faction = ((stance.Faction1 == kingdom) ? stance.Faction2 : stance.Faction1);
			if (stance.IsNeutral)
			{
				int dailyTributeToPay = stance.GetDailyTributeToPay(kingdom);
				if (dailyTributeToPay < 0)
				{
					float num2 = TaleWorlds.Library.MathF.Sqrt(TaleWorlds.Library.MathF.Min(1f, (float)(-dailyTributeToPay) / 4000f));
					num += num2 * faction.CurrentTotalStrength;
				}
			}
		}
		return num;
	}

	public static IEnumerable<Army> GetKingdomArmies(IFaction mapFaction)
	{
		if (!mapFaction.IsKingdomFaction)
		{
			return new List<Army>();
		}
		return ((Kingdom)mapFaction).Armies;
	}

	public static float SettlementProsperityEffectOnGarrisonSizeConstant(Town town)
	{
		return 2.2f * (0.1f + 0.9f * TaleWorlds.Library.MathF.Sqrt(TaleWorlds.Library.MathF.Min(town.Prosperity, 5000f) / 5000f));
	}

	public static float SettlementFoodPotentialEffectOnGarrisonSizeConstant(Settlement settlement)
	{
		int num = 0;
		if (settlement.IsFortification)
		{
			foreach (Village village in settlement.Town.Villages)
			{
				num += 5 * ((village.Hearth < 200f) ? 1 : ((village.Hearth < 600f) ? 2 : 3));
			}
		}
		return 0.5f + 0.5f * (float)TaleWorlds.Library.MathF.Min(50, num) / 50f;
	}

	public static float OwnerClanEconomyEffectOnGarrisonSizeConstant(Clan clan)
	{
		if (clan != null && clan.Leader != null)
		{
			if ((float)clan.Leader.Gold > 160000f)
			{
				return 1.5f + 0.5f * TaleWorlds.Library.MathF.Min(1f, ((float)clan.Leader.Gold - 160000f) / 160000f);
			}
			if ((float)clan.Leader.Gold > 80000f)
			{
				return 1f + 0.5f * TaleWorlds.Library.MathF.Min(1f, ((float)clan.Leader.Gold - 80000f) / 80000f);
			}
			if ((float)clan.Leader.Gold < 40000f)
			{
				return 1f - 0.75f * (1f - (float)clan.Leader.Gold / 40000f);
			}
			return 1f;
		}
		return 1f;
	}

	public static float FindIdealGarrisonStrengthPerWalledCenter(Kingdom kingdom, Clan clan = null)
	{
		if (kingdom == null && clan == null)
		{
			return 0f;
		}
		float num = 0f;
		int num2 = kingdom?.Clans.Count((Clan x) => !x.IsUnderMercenaryService) ?? 0;
		MBReadOnlyList<Town> obj = ((kingdom != null) ? kingdom.Fiefs : clan.Fiefs);
		float num3 = ((kingdom != null) ? ((kingdom.CurrentTotalStrength + (float)(num2 * 500)) / 2f) : clan.CurrentTotalStrength);
		float num4 = 0f;
		float num5 = 0f;
		foreach (Town item in obj)
		{
			float num6 = SettlementProsperityEffectOnGarrisonSizeConstant(item);
			float num7 = SettlementFoodPotentialEffectOnGarrisonSizeConstant(item.Settlement);
			num6 *= num7;
			float num8 = OwnerClanEconomyEffectOnGarrisonSizeConstant(item.OwnerClan);
			num5 += num6;
			num6 *= num8;
			num += num6 * 60f;
			num4 += num6;
		}
		float num9 = num3 * 0.5f / num4;
		float num10 = num / num5;
		return 5f + (num9 + num10) / 2f;
	}

	public static void FinishAllRelatedHostileActionsOfNobleToFaction(Hero noble, IFaction faction)
	{
		if (noble.PartyBelongedTo != null && noble.PartyBelongedTo.MapEvent != null && ((noble.PartyBelongedTo.MapEvent.AttackerSide.LeaderParty == noble.PartyBelongedTo.Party && ((faction.IsKingdomFaction && noble.PartyBelongedTo.MapEvent.DefenderSide.LeaderParty.MapFaction == faction) || (!faction.IsKingdomFaction && noble.PartyBelongedTo.MapEvent.DefenderSide.LeaderParty.Owner != null && noble.PartyBelongedTo.MapEvent.DefenderSide.LeaderParty.Owner.Clan == faction))) || (noble.PartyBelongedTo.MapEvent.DefenderSide.LeaderParty == noble.PartyBelongedTo.Party && ((faction.IsKingdomFaction && noble.PartyBelongedTo.MapEvent.AttackerSide.LeaderParty.MapFaction == faction) || (!faction.IsKingdomFaction && noble.PartyBelongedTo.MapEvent.AttackerSide.LeaderParty.Owner != null && noble.PartyBelongedTo.MapEvent.AttackerSide.LeaderParty.Owner.Clan == faction)))))
		{
			noble.PartyBelongedTo.MapEvent.DiplomaticallyFinished = true;
			List<PartyBase> list = new List<PartyBase>();
			foreach (MapEventParty party in noble.PartyBelongedTo.MapEvent.AttackerSide.Parties)
			{
				list.Add(party.Party);
			}
			if (noble.PartyBelongedTo.MapEvent.MapEventSettlement != null)
			{
				foreach (WarPartyComponent warPartyComponent in noble.PartyBelongedTo.MapEvent.MapEventSettlement.MapFaction.WarPartyComponents)
				{
					MobileParty mobileParty = warPartyComponent.MobileParty;
					if (mobileParty.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty.TargetSettlement == noble.PartyBelongedTo.MapEvent.MapEventSettlement && mobileParty.CurrentSettlement == null)
					{
						mobileParty.SetMoveModeHold();
					}
				}
			}
			noble.PartyBelongedTo.MapEvent.Update();
			foreach (PartyBase item in list)
			{
				if (item.IsMobile)
				{
					item.MobileParty.SetMoveModeHold();
				}
			}
		}
		if (noble.PartyBelongedTo == null)
		{
			return;
		}
		MobileParty partyBelongedTo = noble.PartyBelongedTo;
		if (partyBelongedTo.BesiegedSettlement != null && ((faction.IsKingdomFaction && partyBelongedTo.BesiegedSettlement.MapFaction == faction) || (!faction.IsKingdomFaction && partyBelongedTo.BesiegedSettlement.OwnerClan == faction)))
		{
			foreach (WarPartyComponent warPartyComponent2 in partyBelongedTo.BesiegedSettlement.MapFaction.WarPartyComponents)
			{
				MobileParty mobileParty2 = warPartyComponent2.MobileParty;
				if (mobileParty2.DefaultBehavior == AiBehavior.DefendSettlement && mobileParty2.TargetSettlement == partyBelongedTo.BesiegedSettlement && mobileParty2.CurrentSettlement == null)
				{
					mobileParty2.SetMoveModeHold();
				}
			}
			partyBelongedTo.BesiegerCamp = null;
			partyBelongedTo.SetMoveModeHold();
		}
		if ((partyBelongedTo.DefaultBehavior == AiBehavior.RaidSettlement || partyBelongedTo.DefaultBehavior == AiBehavior.BesiegeSettlement || partyBelongedTo.DefaultBehavior == AiBehavior.AssaultSettlement) && ((faction.IsKingdomFaction && partyBelongedTo.TargetSettlement.MapFaction == faction) || (!faction.IsKingdomFaction && partyBelongedTo.TargetSettlement.OwnerClan == faction)))
		{
			if (partyBelongedTo.Army != null)
			{
				partyBelongedTo.Army.FinishArmyObjective();
			}
			partyBelongedTo.SetMoveModeHold();
		}
		if (partyBelongedTo.ShortTermBehavior == AiBehavior.EngageParty && partyBelongedTo.ShortTermTargetParty != null && partyBelongedTo.ShortTermTargetParty.MapFaction == faction)
		{
			partyBelongedTo.SetMoveModeHold();
		}
	}

	public static void FinishAllRelatedHostileActionsOfFactionToFaction(IFaction faction1, IFaction faction2)
	{
		foreach (Hero aliveLord in faction1.AliveLords)
		{
			FinishAllRelatedHostileActionsOfNobleToFaction(aliveLord, faction2);
		}
	}

	public static void FinishAllRelatedHostileActions(Clan clan1, Clan clan2)
	{
		foreach (Hero aliveLord in clan1.AliveLords)
		{
			FinishAllRelatedHostileActionsOfNobleToFaction(aliveLord, clan2);
		}
		foreach (Hero aliveLord2 in clan2.AliveLords)
		{
			FinishAllRelatedHostileActionsOfNobleToFaction(aliveLord2, clan1);
		}
	}

	public static void FinishAllRelatedHostileActions(Kingdom kingdom1, Kingdom kingdom2)
	{
		foreach (Clan clan in kingdom1.Clans)
		{
			FinishAllRelatedHostileActionsOfFactionToFaction(clan, kingdom2);
		}
		foreach (Clan clan2 in kingdom2.Clans)
		{
			FinishAllRelatedHostileActionsOfFactionToFaction(clan2, kingdom1);
		}
	}

	public static void AdjustFactionStancesForClanJoiningKingdom(Clan joiningClan, Kingdom kingdomToJoin)
	{
		foreach (StanceLink stance in GetStances(joiningClan))
		{
			if (Campaign.Current.Models.DiplomacyModel.IsAtConstantWar(stance.Faction1, stance.Faction2))
			{
				continue;
			}
			IFaction faction = ((stance.Faction1 == joiningClan) ? stance.Faction2 : stance.Faction1);
			if (stance.IsAtWar)
			{
				if (!kingdomToJoin.IsAtWarWith(faction))
				{
					MakePeaceAction.Apply(joiningClan, faction);
					FinishAllRelatedHostileActionsOfFactionToFaction(joiningClan, faction);
					FinishAllRelatedHostileActionsOfFactionToFaction(faction, joiningClan);
				}
			}
			else
			{
				stance.ResetPeaceStats();
			}
		}
	}

	public static TextObject GetTermUsedByOtherFaction(IFaction faction, IFaction otherFaction, bool pejorative)
	{
		if (faction.IsMinorFaction || faction.IsEliminated)
		{
			TextObject textObject = new TextObject("{=n48jo6Qn}the {FACTION_NAME}");
			textObject.SetTextVariable("FACTION_NAME", faction.Name);
			return textObject;
		}
		if (otherFaction.Culture != faction.Culture)
		{
			int num = 0;
			foreach (Kingdom item in Kingdom.All)
			{
				if (item.Culture == faction.Culture)
				{
					num++;
				}
			}
			TextObject obj = ((num == 1) ? new TextObject("{=bIWDtytH}the {ETHNIC_TERM}") : new TextObject("{=JrT9bBEK}{FACTION_LIEGE}'s {ETHNIC_TERM}"));
			obj.SetTextVariable("ETHNIC_TERM", GameTexts.FindText("str_neutral_term_for_culture", faction.Culture.StringId));
			obj.SetTextVariable("FACTION_LIEGE", (faction.Leader != null) ? faction.Leader.Name : TextObject.GetEmpty());
			return obj;
		}
		TextObject obj2 = ((!pejorative) ? new TextObject("{=WWFnlL3O}{FACTION_LIEGE}'s followers") : new TextObject("{=uujU2fSA}{FACTION_LIEGE}'s scum"));
		obj2.SetTextVariable("FACTION_LIEGE", (faction.Leader != null) ? faction.Leader.Name : TextObject.GetEmpty());
		return obj2;
	}

	public static TextObject GetFormalNameForFactionCulture(CultureObject factionCulture)
	{
		return GameTexts.FindText("str_faction_formal_name_for_culture", factionCulture.StringId);
	}

	public static TextObject GetInformalNameForFactionCulture(CultureObject factionCulture)
	{
		return GameTexts.FindText("str_faction_informal_name_for_culture", factionCulture.StringId);
	}

	public static TextObject GetAdjectiveForFactionCulture(CultureObject factionCulture)
	{
		return GameTexts.FindText("str_adjective_for_culture", factionCulture.StringId);
	}

	public static TextObject GetAdjectiveForFaction(IFaction faction)
	{
		if (faction is Kingdom)
		{
			return GameTexts.FindText("str_adjective_for_faction", faction.StringId);
		}
		return faction.Name;
	}

	public static TextObject GenerateClanNameforPlayer()
	{
		CultureObject culture = CharacterObject.PlayerCharacter.Culture;
		if (culture.StringId == "vlandia")
		{
			return new TextObject("{=Uk3qRuCS}dey Corvand");
		}
		return NameGenerator.Current.GenerateClanName(culture, null);
	}

	public static float GetDistanceToClosestNonAllyFortificationOfFaction(IFaction faction)
	{
		float num = float.MaxValue;
		if (faction.FactionMidSettlement != null)
		{
			foreach (Town allFief in Town.AllFiefs)
			{
				Settlement settlement = allFief.Settlement;
				if (settlement.MapFaction != faction)
				{
					float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, faction.FactionMidSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All);
					if (num > distance)
					{
						num = distance;
					}
				}
			}
		}
		return num;
	}

	public static Settlement GetMidSettlementOfFaction(IFaction faction)
	{
		Settlement result = null;
		if (faction.Settlements.Count == 0)
		{
			if (faction is Clan clan)
			{
				result = clan.HomeSettlement;
			}
			else if (faction is Kingdom kingdom)
			{
				result = kingdom.InitialHomeSettlement;
			}
		}
		else
		{
			float num = float.MaxValue;
			result = faction.Settlements[0];
			foreach (Settlement settlement in faction.Settlements)
			{
				float num2 = 0f;
				foreach (Settlement settlement2 in faction.Settlements)
				{
					if (settlement != settlement2)
					{
						float num3 = Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, settlement2, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All);
						if (settlement2.IsVillage)
						{
							num3 *= 0.1f;
						}
						else if (settlement2.IsCastle)
						{
							num3 *= 0.25f;
						}
						num2 += num3;
					}
				}
				if (num > num2)
				{
					num = num2;
					result = settlement;
				}
			}
		}
		return result;
	}

	public static List<IFaction> GetPossibleKingdomsToDeclareWar(Kingdom kingdom)
	{
		List<IFaction> list = new List<IFaction>();
		foreach (Kingdom item in Kingdom.All)
		{
			if (item != kingdom && !FactionManager.IsAtWarAgainstFaction(item, kingdom))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static List<IFaction> GetPossibleKingdomsToDeclarePeace(Kingdom kingdom)
	{
		List<IFaction> list = new List<IFaction>();
		foreach (Kingdom item in Kingdom.All)
		{
			if (item != kingdom && FactionManager.IsAtWarAgainstFaction(item, kingdom))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static IEnumerable<Clan> GetAllyMinorFactions(CharacterObject otherCharacter)
	{
		throw new NotImplementedException();
	}

	public static Clan ChooseHeirClanForFiefs(Clan oldClan)
	{
		Clan clan = null;
		if (oldClan.Kingdom != null)
		{
			clan = ((oldClan.Kingdom.IsEliminated || oldClan.Kingdom.RulingClan == oldClan) ? oldClan.Kingdom.Clans.GetRandomElementWithPredicate((Clan t) => t != oldClan && !t.IsEliminated && !t.IsMinorFaction && !t.AliveLords.IsEmpty() && t.AliveLords.Any((Hero k) => !k.IsChild)) : oldClan.Kingdom.RulingClan);
		}
		if (clan == null)
		{
			float num = float.MaxValue;
			foreach (Clan item in Clan.All.Where((Clan t) => t != oldClan && !t.IsEliminated && !t.IsMinorFaction && !t.AliveLords.IsEmpty() && t.AliveLords.Any((Hero k) => !k.IsChild) && !t.IsBanditFaction))
			{
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(item.FactionMidSettlement, oldClan.FactionMidSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All);
				if (distance < num)
				{
					clan = item;
					num = distance;
				}
			}
			if (clan?.Kingdom != null && !clan.Kingdom.IsEliminated)
			{
				clan = clan.Kingdom.RulingClan;
			}
		}
		if (clan == null)
		{
			clan = Clan.PlayerClan;
		}
		return clan;
	}

	private static bool IsMainClanMemberAvailableForRelocate(Hero hero, out TextObject explanation)
	{
		if (hero.Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
		{
			explanation = new TextObject("{=HAo6iIda}{HERO.NAME} is not eligible.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (hero.PartyBelongedTo != null)
		{
			if (hero.PartyBelongedTo.LeaderHero == hero)
			{
				explanation = new TextObject("{=kNW1qYSi}{HERO.NAME} is leading a party right now.");
				explanation.SetCharacterProperties("HERO", hero.CharacterObject);
				return false;
			}
			if (hero.PartyBelongedTo.MapEvent != null)
			{
				explanation = new TextObject("{=haY6IEw2}{HERO.NAME} is currently in a battle right now.");
				explanation.SetCharacterProperties("HERO", hero.CharacterObject);
				return false;
			}
		}
		if (hero.IsPrisoner)
		{
			explanation = new TextObject("{=hv1ARuaU}{HERO.NAME} is in prison right now.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (hero.IsReleased)
		{
			explanation = new TextObject("{=jGIw0Xku}{HERO.NAME} has just escaped from {?HERO.GENDER}her{?}his{\\?} captors and is currently recovering.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (hero.IsFugitive || hero.IsDisabled || !hero.CanBeGovernorOrHavePartyRole())
		{
			explanation = new TextObject("{=nMmYZ3xi}{HERO.NAME} is not available right now.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (hero.IsTraveling)
		{
			explanation = new TextObject("{=287Epvf0}{HERO.NAME} is traveling right now.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (Campaign.Current.IssueManager.IssueSolvingCompanionList.Contains(hero))
		{
			explanation = new TextObject("{=se5704KH}{HERO.NAME} is solving an issue right now.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (Campaign.Current.GetCampaignBehavior<IAlleyCampaignBehavior>().IsHeroAlleyLeaderOfAnyPlayerAlley(hero))
		{
			explanation = new TextObject("{=WBcw6Z9W}{HERO.NAME} is leading an alley.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		explanation = null;
		return true;
	}

	public static bool CanPlayerOfferMercenaryService(Kingdom offerKingdom, out List<IFaction> playerWars, out List<IFaction> warsOfFactionToJoin)
	{
		playerWars = new List<IFaction>();
		warsOfFactionToJoin = new List<IFaction>();
		float strengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom = Campaign.Current.Models.DiplomacyModel.GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(offerKingdom);
		foreach (Kingdom item in Kingdom.All)
		{
			if (Clan.PlayerClan.MapFaction.IsAtWarWith(item) && item.CurrentTotalStrength > strengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom)
			{
				playerWars.Add(item);
			}
		}
		foreach (Kingdom item2 in Kingdom.All)
		{
			if (offerKingdom.IsAtWarWith(item2))
			{
				warsOfFactionToJoin.Add(item2);
			}
		}
		if (Clan.PlayerClan.Kingdom == null && !Clan.PlayerClan.IsAtWarWith(offerKingdom) && Clan.PlayerClan.Tier >= Campaign.Current.Models.ClanTierModel.MercenaryEligibleTier && offerKingdom.Leader.GetRelationWithPlayer() >= (float)Campaign.Current.Models.DiplomacyModel.MinimumRelationWithConversationCharacterToJoinKingdom && warsOfFactionToJoin.Intersect(playerWars).Count() == playerWars.Count)
		{
			return Clan.PlayerClan.Settlements.IsEmpty();
		}
		return false;
	}

	public static bool CanPlayerOfferVassalage(Kingdom offerKingdom, out List<IFaction> playerWars, out List<IFaction> warsOfFactionToJoin)
	{
		playerWars = new List<IFaction>();
		warsOfFactionToJoin = new List<IFaction>();
		float strengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom = Campaign.Current.Models.DiplomacyModel.GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(offerKingdom);
		foreach (Kingdom item in Kingdom.All)
		{
			if (Clan.PlayerClan.MapFaction.IsAtWarWith(item) && item.CurrentTotalStrength > strengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom)
			{
				playerWars.Add(item);
			}
		}
		foreach (Kingdom item2 in Kingdom.All)
		{
			if (offerKingdom.IsAtWarWith(item2))
			{
				warsOfFactionToJoin.Add(item2);
			}
		}
		if ((Clan.PlayerClan.Kingdom == null || Clan.PlayerClan.IsUnderMercenaryService) && !Clan.PlayerClan.IsAtWarWith(offerKingdom) && Clan.PlayerClan.Tier >= Campaign.Current.Models.ClanTierModel.VassalEligibleTier && !offerKingdom.IsEliminated && offerKingdom.Leader.GetRelationWithPlayer() >= (float)Campaign.Current.Models.DiplomacyModel.MinimumRelationWithConversationCharacterToJoinKingdom)
		{
			return warsOfFactionToJoin.Intersect(playerWars).Count() == playerWars.Count;
		}
		return false;
	}

	public static bool IsMainClanMemberAvailableForRecall(Hero hero, MobileParty targetParty, out TextObject explanation)
	{
		if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsMainParty)
		{
			explanation = new TextObject("{=uhOCqJwd}{HERO.NAME} is already in the main party.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (hero.CurrentSettlement != null && (hero.CurrentSettlement.IsUnderSiege || hero.CurrentSettlement.IsUnderRaid))
		{
			explanation = new TextObject("{=L9nn40qu}{HERO.NAME}{.o} location is under attack right now.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (Hero.MainHero.IsPrisoner)
		{
			explanation = new TextObject("{=jRslIaiU}You can't recall a clan member while you are a prisoner.");
			return false;
		}
		if (MobileParty.MainParty.MapEvent != null)
		{
			explanation = new TextObject("{=h0pBxG09}You can't recall a clan member while you are in a map event.");
			return false;
		}
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			explanation = new TextObject("{=3V2BTAfB}You cannot do this action when you are at sea.");
			return false;
		}
		if (!IsMainClanMemberAvailableForRelocate(hero, out explanation))
		{
			return false;
		}
		return true;
	}

	public static bool IsMainClanMemberAvailableForPartyLeaderChange(Hero hero, bool isSend, MobileParty targetParty, out TextObject explanation)
	{
		int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
		if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsMainParty && !isSend)
		{
			explanation = new TextObject("{=uhOCqJwd}{HERO.NAME} is already in the main party.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (targetParty.MemberRoster.Count == 1 && targetParty.LeaderHero != null)
		{
			explanation = new TextObject("{=pwuEqegC}Party leader is the only member of the party right now.");
			return false;
		}
		if (targetParty.MapEvent != null)
		{
			explanation = new TextObject("{=yC52EBCb}Target party is currently in a battle right now.");
			return false;
		}
		if (targetParty.Army != null)
		{
			explanation = new TextObject("{=2iRg3vpP}Target party is currently in an army right now.");
			return false;
		}
		if (targetParty.IsCurrentlyAtSea)
		{
			explanation = new TextObject("{=TbD2qPLy}Target party is currently sailing.");
			return false;
		}
		if (hero.CurrentSettlement != null && (hero.CurrentSettlement.IsUnderSiege || hero.CurrentSettlement.IsUnderRaid))
		{
			explanation = new TextObject("{=L9nn40qu}{HERO.NAME}{.o} location is under attack right now.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (hero.GovernorOf != null)
		{
			explanation = new TextObject("{=bgVZcd1I}{HERO.NAME} is a governor.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsCurrentlyAtSea)
		{
			explanation = new TextObject("{=1ELK1UbN}{HERO.NAME} is currently sailing.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (!IsMainClanMemberAvailableForRelocate(hero, out explanation))
		{
			return false;
		}
		if (partyGoldLowerThreshold - hero.Gold > Hero.MainHero.Gold)
		{
			explanation = new TextObject("{=xpCdwmlX}You don't have enough gold to make {HERO.NAME} a party leader.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		explanation = new TextObject("{=NAseSXPl}It would take {HOUR} {?HOUR > 1}hours{?}hour{\\?} for {HERO.NAME} to arrive at your party.");
		explanation.SetCharacterProperties("HERO", hero.CharacterObject);
		float resultNumber = Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, targetParty.Party).ResultNumber;
		explanation.SetTextVariable("HOUR", (int)Math.Ceiling(resultNumber));
		return true;
	}

	public static bool IsMainClanMemberAvailableForSendingSettlement(Hero hero, Settlement targetSettlement, out TextObject explanation)
	{
		if (hero.PartyBelongedTo != null && (hero.PartyBelongedTo.IsCurrentlyAtSea || hero.PartyBelongedTo.IsInRaftState))
		{
			explanation = new TextObject("{=1ELK1UbN}{HERO.NAME} is currently sailing.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (hero.CurrentSettlement != null && (hero.CurrentSettlement.IsUnderSiege || hero.CurrentSettlement.IsUnderRaid))
		{
			explanation = new TextObject("{=L9nn40qu}{HERO.NAME}{.o} location is under attack right now.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (targetSettlement.IsUnderRaid || targetSettlement.IsUnderSiege)
		{
			explanation = new TextObject("{=1tGP6vJn}Target settlement is under attack right now.");
			return false;
		}
		if (hero.GovernorOf != null)
		{
			explanation = new TextObject("{=bgVZcd1I}{HERO.NAME} is a governor.");
			explanation.SetCharacterProperties("HERO", hero.CharacterObject);
			return false;
		}
		if (!IsMainClanMemberAvailableForRelocate(hero, out explanation))
		{
			return false;
		}
		explanation = new TextObject("{=NAseSXPl}It would take {HOUR} {?HOUR > 1}hours{?}hour{\\?} for {HERO.NAME} to arrive at your party.");
		explanation.SetCharacterProperties("HERO", hero.CharacterObject);
		float resultNumber = Campaign.Current.Models.DelayedTeleportationModel.GetTeleportationDelayAsHours(hero, targetSettlement.Party).ResultNumber;
		explanation.SetTextVariable("HOUR", (int)Math.Ceiling(resultNumber));
		return true;
	}

	public static bool IsMainClanMemberAvailableForSendingSettlementAsGovernor(Hero hero, Settlement settlementOfGovernor, out TextObject explanation)
	{
		if (hero.PartyBelongedToAsPrisoner != null)
		{
			explanation = new TextObject("{=knwId8DG}You cannot assign a prisoner as a governor of a settlement");
			return false;
		}
		if (hero == Hero.MainHero)
		{
			explanation = new TextObject("{=uoDuiBZR}You cannot assign yourself as a governor");
			return false;
		}
		if (hero.PartyBelongedTo != null)
		{
			if (hero.PartyBelongedTo.IsCurrentlyAtSea || hero.PartyBelongedTo.IsInRaftState)
			{
				explanation = new TextObject("{=1ELK1UbN}{HERO.NAME} is currently sailing.");
				explanation.SetCharacterProperties("HERO", hero.CharacterObject);
				return false;
			}
			if (hero.PartyBelongedTo.LeaderHero == hero)
			{
				explanation = new TextObject("{=pWObBhj5}You cannot assign a party leader as a new governor of a settlement");
				return false;
			}
		}
		if (hero.IsFugitive)
		{
			explanation = new TextObject("{=KghY9qwl}You cannot assign a fugitive as the new governor of a settlement");
			return false;
		}
		if (hero.IsReleased)
		{
			explanation = new TextObject("{=mOFjZuSf}You cannot assign a newly released hero as the new governor of a settlement");
			return false;
		}
		if (settlementOfGovernor != null)
		{
			explanation = new TextObject("{=YbGu9rSH}This character is already the governor of {SETTLEMENT_NAME}");
			explanation.SetTextVariable("SETTLEMENT_NAME", settlementOfGovernor.Town.Name);
			return false;
		}
		if (!IsMainClanMemberAvailableForRelocate(hero, out explanation))
		{
			return false;
		}
		explanation = null;
		return true;
	}
}
