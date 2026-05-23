using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultEncounterModel : EncounterModel
{
	public override float NeededMaximumLandDistanceForEncounteringMobileParty => 0.5f;

	public override float NeededMaximumNavalDistanceForEncounteringMobileParty => 0f;

	public override float MaximumAllowedLandDistanceForEncounteringMobilePartyInArmy => 1.5f;

	public override float MaximumAllowedNavalDistanceForEncounteringMobilePartyInArmy => 0f;

	public override float NeededMaximumDistanceForEncounteringTown => 0.05f;

	public override float NeededMaximumDistanceForEncounteringBlockade => 3f;

	public override float NeededMaximumDistanceForEncounteringVillage => 1f;

	public override float GetEncounterJoiningRadius => 3f;

	public override float PlayerParleyDistance => MobileParty.MainParty.SeeingRange;

	public override float GetSettlementBeingNearFieldBattleRadius => 3f;

	public override int MinimumNumberOfMenForAttackingVillageViaScene => 1;

	public override bool IsEncounterExemptFromHostileActions(PartyBase side1, PartyBase side2)
	{
		if (side1 != null && side2 != null && (!side1.IsMobile || !side1.MobileParty.AvoidHostileActions))
		{
			if (side2.IsMobile)
			{
				return side2.MobileParty.AvoidHostileActions;
			}
			return false;
		}
		return true;
	}

	public override Hero GetLeaderOfSiegeEvent(SiegeEvent siegeEvent, BattleSideEnum side)
	{
		IEnumerable<PartyBase> involvedPartiesForEventType = siegeEvent.GetSiegeEventSide(side).GetInvolvedPartiesForEventType();
		if (involvedPartiesForEventType.Count() == 1)
		{
			return involvedPartiesForEventType.ElementAt(0).LeaderHero;
		}
		IFaction eventFaction = ((side == BattleSideEnum.Attacker) ? siegeEvent.BesiegerCamp.MapFaction : siegeEvent.BesiegedSettlement.MapFaction);
		return GetLeaderOfEventInternal(involvedPartiesForEventType, eventFaction);
	}

	public override bool CanMainHeroDoParleyWithParty(PartyBase partyBase, out TextObject explanation)
	{
		bool result = true;
		explanation = null;
		if (MapEvent.PlayerMapEvent == null && Settlement.CurrentSettlement == null && MobileParty.MainParty.IsActive && !Hero.MainHero.IsPrisoner && partyBase.MapFaction != null && (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty) && partyBase.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
		{
			if (partyBase.MapFaction.IsRebelClan)
			{
				explanation = new TextObject("{=6LG4BDZZ}You can't start parley with Rebels.");
				result = false;
			}
			else
			{
				if (partyBase.IsMobile)
				{
					return false;
				}
				if (partyBase.IsSettlement && partyBase.Settlement.IsFortification && !partyBase.Settlement.IsUnderSiege && partyBase.Settlement.IsInspected)
				{
					Settlement settlement = partyBase.Settlement;
					float estimatedLandRatio;
					bool flag = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, settlement, MobileParty.MainParty.IsCurrentlyAtSea && settlement.HasPort, MobileParty.MainParty.NavigationCapability, out estimatedLandRatio) < Campaign.Current.Models.EncounterModel.PlayerParleyDistance;
					if (!Campaign.Current.Models.SettlementAccessModel.IsRequestMeetingOptionAvailable(settlement, out var disableOption, out explanation) || disableOption)
					{
						result = false;
					}
					else if (!flag)
					{
						explanation = new TextObject("{=Y8JPgz1c}You are too far away from {SETTLEMENT} to start parley.");
						explanation.SetTextVariable("SETTLEMENT", partyBase.Settlement.Name);
						result = false;
					}
				}
				else
				{
					result = false;
				}
			}
		}
		else
		{
			result = false;
		}
		return result;
	}

	public override Hero GetLeaderOfMapEvent(MapEvent mapEvent, BattleSideEnum side)
	{
		IFaction eventFaction = ((side == BattleSideEnum.Attacker) ? mapEvent.AttackerSide.LeaderParty.MapFaction : mapEvent.DefenderSide.LeaderParty.MapFaction);
		return GetLeaderOfEventInternal(mapEvent.GetMapEventSide(side).Parties.Select((MapEventParty x) => x.Party), eventFaction);
	}

	private bool IsArmyLeader(Hero hero)
	{
		if (hero.PartyBelongedTo?.Army != null)
		{
			return hero.PartyBelongedTo.Army.LeaderParty == hero.PartyBelongedTo;
		}
		return false;
	}

	private int GetLeadingScore(Hero hero)
	{
		if (!hero.IsKingdomLeader && !IsArmyLeader(hero))
		{
			return GetCharacterSergeantScore(hero);
		}
		return (int)hero.PartyBelongedTo.GetTotalLandStrengthWithFollowers();
	}

	private Hero GetLeaderOfEventInternal(IEnumerable<PartyBase> allPartiesThatBelongToASide, IFaction eventFaction)
	{
		Hero hero = null;
		int num = 0;
		foreach (PartyBase item in allPartiesThatBelongToASide)
		{
			Hero leaderHero = item.LeaderHero;
			if (leaderHero == null)
			{
				continue;
			}
			int leadingScore = GetLeadingScore(leaderHero);
			if (hero == null)
			{
				hero = leaderHero;
				num = leadingScore;
			}
			bool flag = leaderHero.MapFaction == eventFaction;
			bool isKingdomLeader = leaderHero.IsKingdomLeader;
			bool flag2 = IsArmyLeader(leaderHero);
			bool flag3 = hero.MapFaction == eventFaction;
			bool isKingdomLeader2 = hero.IsKingdomLeader;
			bool flag4 = IsArmyLeader(hero);
			if (!flag3 && flag)
			{
				hero = leaderHero;
				num = leadingScore;
			}
			else
			{
				if (flag != flag3)
				{
					continue;
				}
				if (isKingdomLeader)
				{
					if (!isKingdomLeader2 || leadingScore > num)
					{
						hero = leaderHero;
						num = leadingScore;
					}
				}
				else if (flag2)
				{
					if ((!isKingdomLeader2 && !flag4) || (flag4 && !isKingdomLeader2 && leadingScore > num))
					{
						hero = leaderHero;
						num = leadingScore;
					}
				}
				else if (!isKingdomLeader2 && !flag4 && leadingScore > num)
				{
					hero = leaderHero;
					num = leadingScore;
				}
			}
		}
		return hero;
	}

	public override int GetCharacterSergeantScore(Hero hero)
	{
		int num = 0;
		Clan clan = hero.Clan;
		if (clan != null)
		{
			num += clan.Tier * ((hero == clan.Leader) ? 100 : 20);
			if (clan.Kingdom != null && clan.Kingdom.Leader == hero)
			{
				num += 2000;
			}
		}
		MobileParty partyBelongedTo = hero.PartyBelongedTo;
		if (partyBelongedTo != null)
		{
			if (partyBelongedTo.Army != null && partyBelongedTo.Army.LeaderParty == partyBelongedTo)
			{
				num += partyBelongedTo.Army.Parties.Count * 200;
			}
			num += partyBelongedTo.MemberRoster.TotalManCount - partyBelongedTo.MemberRoster.TotalWounded;
		}
		return num;
	}

	public override IEnumerable<PartyBase> GetDefenderPartiesOfSettlement(Settlement settlement, MapEvent.BattleTypes mapEventType)
	{
		if (settlement.IsFortification)
		{
			return settlement.Town.GetDefenderParties(mapEventType);
		}
		if (settlement.IsVillage)
		{
			return settlement.Village.GetDefenderParties(mapEventType);
		}
		if (settlement.IsHideout)
		{
			return settlement.Hideout.GetDefenderParties(mapEventType);
		}
		return null;
	}

	public override PartyBase GetNextDefenderPartyOfSettlement(Settlement settlement, ref int partyIndex, MapEvent.BattleTypes mapEventType)
	{
		if (settlement.IsFortification)
		{
			return settlement.Town.GetNextDefenderParty(ref partyIndex, mapEventType);
		}
		if (settlement.IsVillage)
		{
			return settlement.Village.GetNextDefenderParty(ref partyIndex, mapEventType);
		}
		if (settlement.IsHideout)
		{
			return settlement.Hideout.GetNextDefenderParty(ref partyIndex, mapEventType);
		}
		return null;
	}

	public override MapEventComponent CreateMapEventComponentForEncounter(PartyBase attackerParty, PartyBase defenderParty, MapEvent.BattleTypes battleType)
	{
		MapEventComponent result = null;
		switch (battleType)
		{
		case MapEvent.BattleTypes.FieldBattle:
			result = FieldBattleEventComponent.CreateFieldBattleEvent(attackerParty, defenderParty);
			break;
		case MapEvent.BattleTypes.Raid:
			result = RaidEventComponent.CreateRaidEvent(attackerParty, defenderParty);
			break;
		case MapEvent.BattleTypes.Siege:
			Campaign.Current.MapEventManager.StartSiegeMapEvent(attackerParty, defenderParty);
			break;
		case MapEvent.BattleTypes.Hideout:
			result = HideoutEventComponent.CreateHideoutEvent(attackerParty, defenderParty, isSendTroops: false);
			break;
		case MapEvent.BattleTypes.SallyOut:
			Campaign.Current.MapEventManager.StartSallyOutMapEvent(attackerParty, defenderParty);
			break;
		case MapEvent.BattleTypes.SiegeOutside:
			Campaign.Current.MapEventManager.StartSiegeOutsideMapEvent(attackerParty, defenderParty);
			break;
		case MapEvent.BattleTypes.BlockadeBattle:
			result = BlockadeBattleMapEvent.CreateBlockadeBattleMapEvent(attackerParty, defenderParty, isSallyOut: false);
			break;
		case MapEvent.BattleTypes.BlockadeSallyOutBattle:
			result = BlockadeBattleMapEvent.CreateBlockadeBattleMapEvent(attackerParty, defenderParty, isSallyOut: true);
			break;
		}
		return result;
	}

	public override float GetSurrenderChance(MobileParty defenderParty, MobileParty attackerParty)
	{
		float num = defenderParty.Party.CalculateCurrentStrength();
		float num2 = attackerParty.Party.CalculateCurrentStrength();
		if (num.ApproximatelyEqualsTo(0f))
		{
			return 1f;
		}
		if (num2.ApproximatelyEqualsTo(0f))
		{
			return 0f;
		}
		if (num >= num2)
		{
			return 0f;
		}
		float num3 = 0f;
		float num4 = 0f;
		if (defenderParty.IsVillager)
		{
			num3 = 0.23f;
			num4 = -13f;
		}
		else if (defenderParty.IsCaravan)
		{
			num3 = 0.3f;
			num4 = -10f;
		}
		else if (defenderParty.IsBandit)
		{
			num3 = (defenderParty.IsCurrentlyAtSea ? 0.2f : ((!(defenderParty.ActualClan.StringId == "deserters")) ? 0.1f : 0.005f));
			num4 = -15f;
		}
		else
		{
			Debug.FailedAssert("Unable to calculate threshold and exponentialScalingFactor!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultEncounterModel.cs", "GetSurrenderChance", 351);
		}
		float num5 = num / num2;
		float num6 = num4 * (num5 - num3);
		float num7 = 1f - 1f / (1f + (float)Math.Exp(num6));
		if (!MobileParty.MainParty.IsCurrentlyAtSea && Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.Scarface))
		{
			num7 = TaleWorlds.Library.MathF.Min(1f, num7 * (1f + DefaultPerks.Roguery.Scarface.PrimaryBonus));
		}
		return num7;
	}

	public override ExplainedNumber GetBribeChance(MobileParty defenderParty, MobileParty attackerParty)
	{
		float num = defenderParty.Party.CalculateCurrentStrength();
		float num2 = attackerParty.Party.CalculateCurrentStrength();
		if (num.ApproximatelyEqualsTo(0f))
		{
			return new ExplainedNumber(1f);
		}
		if (num2.ApproximatelyEqualsTo(0f))
		{
			return new ExplainedNumber(0f, includeDescriptions: false, null);
		}
		if (num >= num2)
		{
			return new ExplainedNumber(0f, includeDescriptions: false, null);
		}
		float num3 = 0f;
		float num4 = 0f;
		if (defenderParty.IsVillager)
		{
			num3 = 0.3f;
			num4 = -10f;
		}
		else if (defenderParty.IsCaravan)
		{
			num3 = 0.52f;
			num4 = -10f;
		}
		else if (defenderParty.IsBandit)
		{
			num3 = 0.2f;
			num4 = -15f;
		}
		else
		{
			Debug.FailedAssert("Unable to calculate threshold and exponentialScalingFactor!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultEncounterModel.cs", "GetBribeChance", 406);
		}
		float num5 = num / num2;
		float num6 = num4 * (num5 - num3);
		ExplainedNumber bonuses = new ExplainedNumber(1f - 1f / (1f + (float)Math.Exp(num6)));
		bonuses.LimitMax(1f);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.Scarface, Hero.MainHero.CharacterObject, isPrimaryBonus: true, ref bonuses);
		return bonuses;
	}

	public override float GetMapEventSideRunAwayChance(MapEventSide mapEventSide)
	{
		float result = 0f;
		if (mapEventSide.MapEvent.EventType != MapEvent.BattleTypes.Siege && mapEventSide.MapEvent.EventType != MapEvent.BattleTypes.SallyOut && mapEventSide.MapEvent.EventType != MapEvent.BattleTypes.SiegeOutside && mapEventSide.MapEvent.EventType != MapEvent.BattleTypes.Raid && mapEventSide != MobileParty.MainParty.MapEventSide)
		{
			result = GetRunAwayChanceInternal(mapEventSide);
		}
		return result;
	}

	private float GetRunAwayChanceInternal(MapEventSide mapEventSide)
	{
		MapEvent mapEvent = mapEventSide.MapEvent;
		float result = 0f;
		if (mapEvent.UpdateCount >= 8 && mapEventSide.LeaderParty.IsMobile && mapEventSide.GetSideMorale() <= 20f)
		{
			for (int i = 0; i < 4; i++)
			{
				BattleSideEnum battleSideEnum = mapEvent.WonRounds[mapEvent.WonRounds.Count - 1 - i];
				if (battleSideEnum == mapEventSide.MissionSide || battleSideEnum == BattleSideEnum.None)
				{
					return 0f;
				}
			}
			result = 0.2f;
			int num = mapEventSide.LeaderParty.LeaderHero?.GetTraitLevel(DefaultTraits.Valor) ?? 0;
			result -= (float)num * 0.05f;
		}
		return result;
	}

	public override void FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(List<MobileParty> partiesToJoinPlayerSide, List<MobileParty> partiesToJoinEnemySide)
	{
		CampaignVec2 campaignVec = MobileParty.MainParty.Position;
		float radius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
		if (PlayerSiege.PlayerSiegeEvent != null)
		{
			radius = Campaign.Current.Models.MobilePartyAIModel.SettlementDefendingWaitingPositionRadius * 1.25f;
		}
		if (PlayerEncounter.Battle != null)
		{
			campaignVec = PlayerEncounter.Battle.Position;
			if (PlayerEncounter.Battle.IsSallyOut)
			{
				campaignVec = ((PlayerSiege.PlayerSiegeEvent != null) ? PlayerSiege.PlayerSiegeEvent : PlayerEncounter.EncounterSettlement.SiegeEvent).BesiegerCamp.LeaderParty.Position;
			}
			else if (PlayerEncounter.Battle.IsBlockade || PlayerEncounter.Battle.IsBlockadeSallyOut)
			{
				campaignVec = PlayerSiege.BesiegedSettlement?.PortPosition ?? PlayerEncounter.Battle.MapEventSettlement.PortPosition;
				radius = Campaign.Current.Models.EncounterModel.NeededMaximumDistanceForEncounteringBlockade * 3f;
			}
		}
		LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(campaignVec.ToVec2(), radius);
		MobileParty nearbyParty = MobileParty.FindNextLocatable(ref data);
		List<MobileParty> list = new List<MobileParty>();
		List<MobileParty> list2 = new List<MobileParty>();
		for (; nearbyParty != null; nearbyParty = MobileParty.FindNextLocatable(ref data))
		{
			bool flag = (PlayerEncounter.Battle != null && (PlayerEncounter.Battle.IsBlockade || PlayerEncounter.Battle.IsBlockadeSallyOut)) || MobileParty.MainParty.IsCurrentlyAtSea;
			if (nearbyParty == MobileParty.MainParty || nearbyParty.MapEvent != null || nearbyParty.IsInRaftState || nearbyParty.SiegeEvent != null || nearbyParty.CurrentSettlement != null || nearbyParty.AttachedTo != null)
			{
				continue;
			}
			if (nearbyParty.IsCurrentlyAtSea != flag)
			{
				MapEvent battle = PlayerEncounter.Battle;
				if (battle == null || battle.MapEventSettlement?.IsVillage != true)
				{
					continue;
				}
			}
			if (!nearbyParty.IsLordParty && !nearbyParty.IsBandit && !nearbyParty.IsPatrolParty && !nearbyParty.ShouldJoinPlayerBattles)
			{
				continue;
			}
			if (PlayerEncounter.Battle != null)
			{
				bool num = PlayerEncounter.Battle.CanPartyJoinBattle(nearbyParty.Party, PlayerEncounter.Battle.PlayerSide);
				bool flag2 = PlayerEncounter.Battle.CanPartyJoinBattle(nearbyParty.Party, PlayerEncounter.Battle.PlayerSide.GetOppositeSide());
				if (num)
				{
					list.Add(nearbyParty);
				}
				if (flag2)
				{
					list2.Add(nearbyParty);
				}
			}
			else
			{
				if (!nearbyParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction) && nearbyParty.MapFaction.IsAtWarWith(PlayerEncounter.EncounteredParty.MapFaction) && list2.All((MobileParty x) => x.MapFaction.IsAtWarWith(nearbyParty.MapFaction)))
				{
					list.Add(nearbyParty);
				}
				if (nearbyParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction) && !nearbyParty.MapFaction.IsAtWarWith(PlayerEncounter.EncounteredParty.MapFaction) && list.All((MobileParty x) => x.MapFaction.IsAtWarWith(nearbyParty.MapFaction)))
				{
					list2.Add(nearbyParty);
				}
			}
		}
		if (list2.AnyQ((MobileParty t) => t.ShouldBeIgnored) || partiesToJoinEnemySide.AnyQ((MobileParty t) => t.ShouldBeIgnored))
		{
			Debug.Print("Ally parties wont join player encounter since there is an ignored party in enemy side");
			list.Clear();
		}
		if (list.AnyQ((MobileParty t) => t.ShouldBeIgnored) || partiesToJoinPlayerSide.AnyQ((MobileParty t) => t != MobileParty.MainParty && t.ShouldBeIgnored))
		{
			Debug.Print("Enemy parties wont join player encounter since there is an ignored party in ally side");
			list2.Clear();
		}
		partiesToJoinPlayerSide.AddRange(list.Except(partiesToJoinPlayerSide));
		partiesToJoinEnemySide.AddRange(list2.Except(partiesToJoinEnemySide));
	}

	public override bool CanPlayerForceBanditsToJoin(out TextObject explanation)
	{
		bool perkValue = Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.PartnersInCrime);
		explanation = (perkValue ? null : new TextObject("{=MaetSSa1}You need '{PERK}' perk to make this party join you."));
		explanation?.SetTextVariable("PERK", DefaultPerks.Roguery.PartnersInCrime.Name);
		return perkValue;
	}

	public override bool IsPartyUnderPlayerCommand(PartyBase party)
	{
		if (party == PartyBase.MainParty)
		{
			return true;
		}
		if (party.Side != PartyBase.MainParty.Side)
		{
			return false;
		}
		bool num = party.Owner == Hero.MainHero;
		bool flag = party.MapFaction?.Leader == Hero.MainHero;
		bool flag2 = party.MobileParty != null && party.MobileParty.DefaultBehavior == AiBehavior.EscortParty && party.MobileParty.TargetParty == MobileParty.MainParty;
		bool flag3 = party.MobileParty != null && party.MobileParty.Army != null && party.MobileParty.Army.LeaderParty == MobileParty.MainParty;
		Settlement mapEventSettlement = party.MapEvent.MapEventSettlement;
		bool flag4 = mapEventSettlement != null && mapEventSettlement.OwnerClan.Leader == Hero.MainHero;
		return num || flag || flag2 || flag3 || flag4;
	}

	public override MBReadOnlyList<MobileParty> GetPartiesToTeleportOnMapEventFinalize(MapEvent mapEvent)
	{
		MBReadOnlyList<MapEventParty> mBReadOnlyList = ((!mapEvent.IsPlayerMapEvent) ? mapEvent.GetMapEventSide(mapEvent.DefeatedSide).Parties : mapEvent.GetMapEventSide(mapEvent.PlayerSide.GetOppositeSide()).Parties);
		MBList<MobileParty> mBList = new MBList<MobileParty>();
		foreach (MapEventParty item in mBReadOnlyList)
		{
			if (item.Party.IsMobile && item.Party.MobileParty.IsActive && item.Party.NumberOfHealthyMembers > 0 && !item.Party.MobileParty.IsGarrison && (item.Party.MobileParty.Army == null || item.Party.MobileParty.Army.LeaderParty == item.Party.MobileParty || item.Party.MobileParty.AttachedTo == null))
			{
				mBList.Add(item.Party.MobileParty);
			}
		}
		return mBList;
	}
}
