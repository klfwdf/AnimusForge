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

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000112 RID: 274
	public class DefaultEncounterModel : EncounterModel
	{
		// Token: 0x17000650 RID: 1616
		// (get) Token: 0x060017A6 RID: 6054 RVA: 0x00070400 File Offset: 0x0006E600
		public override float NeededMaximumDistanceForEncounteringMobileParty
		{
			get
			{
				return 0.5f;
			}
		}

		// Token: 0x17000651 RID: 1617
		// (get) Token: 0x060017A7 RID: 6055 RVA: 0x00070407 File Offset: 0x0006E607
		public override float MaximumAllowedDistanceForEncounteringMobilePartyInArmy
		{
			get
			{
				return 1.5f;
			}
		}

		// Token: 0x17000652 RID: 1618
		// (get) Token: 0x060017A8 RID: 6056 RVA: 0x0007040E File Offset: 0x0006E60E
		public override float NeededMaximumDistanceForEncounteringTown
		{
			get
			{
				return 0.05f;
			}
		}

		// Token: 0x17000653 RID: 1619
		// (get) Token: 0x060017A9 RID: 6057 RVA: 0x00070415 File Offset: 0x0006E615
		public override float NeededMaximumDistanceForEncounteringBlockade
		{
			get
			{
				return 3f;
			}
		}

		// Token: 0x17000654 RID: 1620
		// (get) Token: 0x060017AA RID: 6058 RVA: 0x0007041C File Offset: 0x0006E61C
		public override float NeededMaximumDistanceForEncounteringVillage
		{
			get
			{
				return 1f;
			}
		}

		// Token: 0x17000655 RID: 1621
		// (get) Token: 0x060017AB RID: 6059 RVA: 0x00070423 File Offset: 0x0006E623
		public override float GetEncounterJoiningRadius
		{
			get
			{
				return 3f;
			}
		}

		// Token: 0x17000656 RID: 1622
		// (get) Token: 0x060017AC RID: 6060 RVA: 0x0007042A File Offset: 0x0006E62A
		public override float PlayerParleyDistance
		{
			get
			{
				return MobileParty.MainParty.SeeingRange;
			}
		}

		// Token: 0x17000657 RID: 1623
		// (get) Token: 0x060017AD RID: 6061 RVA: 0x00070436 File Offset: 0x0006E636
		public override float GetSettlementBeingNearFieldBattleRadius
		{
			get
			{
				return 3f;
			}
		}

		// Token: 0x060017AE RID: 6062 RVA: 0x0007043D File Offset: 0x0006E63D
		public override bool IsEncounterExemptFromHostileActions(PartyBase side1, PartyBase side2)
		{
			return side1 == null || side2 == null || (side1.IsMobile && side1.MobileParty.AvoidHostileActions) || (side2.IsMobile && side2.MobileParty.AvoidHostileActions);
		}

		// Token: 0x060017AF RID: 6063 RVA: 0x00070474 File Offset: 0x0006E674
		public override Hero GetLeaderOfSiegeEvent(SiegeEvent siegeEvent, BattleSideEnum side)
		{
			IEnumerable<PartyBase> involvedPartiesForEventType = siegeEvent.GetSiegeEventSide(side).GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege);
			if (involvedPartiesForEventType.Count<PartyBase>() == 1)
			{
				return involvedPartiesForEventType.ElementAt(0).LeaderHero;
			}
			IFaction eventFaction = ((side == BattleSideEnum.Attacker) ? siegeEvent.BesiegerCamp.MapFaction : siegeEvent.BesiegedSettlement.MapFaction);
			return this.GetLeaderOfEventInternal(involvedPartiesForEventType, eventFaction);
		}

		// Token: 0x060017B0 RID: 6064 RVA: 0x000704CC File Offset: 0x0006E6CC
		public override bool CanMainHeroDoParleyWithParty(PartyBase partyBase, out TextObject explanation)
		{
			bool result = true;
			explanation = null;
			if (MapEvent.PlayerMapEvent == null && Settlement.CurrentSettlement == null && MobileParty.MainParty.IsActive && !Hero.MainHero.IsPrisoner && partyBase.MapFaction != null && (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty) && partyBase.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
			{
				if (partyBase.MapFaction.IsRebelClan)
				{
					explanation = new TextObject("{=6LG4BDZZ}You can't start parley with Rebels.", null);
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
						float num;
						bool flag = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, settlement, MobileParty.MainParty.IsCurrentlyAtSea && settlement.HasPort, MobileParty.MainParty.NavigationCapability, out num) < Campaign.Current.Models.EncounterModel.PlayerParleyDistance;
						bool flag2;
						if (!Campaign.Current.Models.SettlementAccessModel.IsRequestMeetingOptionAvailable(settlement, out flag2, out explanation) || flag2)
						{
							result = false;
						}
						else if (!flag)
						{
							explanation = new TextObject("{=Y8JPgz1c}You are too far away from {SETTLEMENT} to start parley.", null);
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

		// Token: 0x060017B1 RID: 6065 RVA: 0x00070670 File Offset: 0x0006E870
		public override Hero GetLeaderOfMapEvent(MapEvent mapEvent, BattleSideEnum side)
		{
			IFaction eventFaction = ((side == BattleSideEnum.Attacker) ? mapEvent.AttackerSide.LeaderParty.MapFaction : mapEvent.DefenderSide.LeaderParty.MapFaction);
			return this.GetLeaderOfEventInternal(from x in mapEvent.GetMapEventSide(side).Parties
				select x.Party, eventFaction);
		}

		// Token: 0x060017B2 RID: 6066 RVA: 0x000706DB File Offset: 0x0006E8DB
		private bool IsArmyLeader(Hero hero)
		{
			MobileParty partyBelongedTo = hero.PartyBelongedTo;
			return ((partyBelongedTo != null) ? partyBelongedTo.Army : null) != null && hero.PartyBelongedTo.Army.LeaderParty == hero.PartyBelongedTo;
		}

		// Token: 0x060017B3 RID: 6067 RVA: 0x0007070B File Offset: 0x0006E90B
		private int GetLeadingScore(Hero hero)
		{
			if (!hero.IsKingdomLeader && !this.IsArmyLeader(hero))
			{
				return this.GetCharacterSergeantScore(hero);
			}
			return (int)hero.PartyBelongedTo.GetTotalLandStrengthWithFollowers(true);
		}

		// Token: 0x060017B4 RID: 6068 RVA: 0x00070734 File Offset: 0x0006E934
		private Hero GetLeaderOfEventInternal(IEnumerable<PartyBase> allPartiesThatBelongToASide, IFaction eventFaction)
		{
			Hero hero = null;
			int num = 0;
			foreach (PartyBase partyBase in allPartiesThatBelongToASide)
			{
				Hero leaderHero = partyBase.LeaderHero;
				if (leaderHero != null)
				{
					int leadingScore = this.GetLeadingScore(leaderHero);
					if (hero == null)
					{
						hero = leaderHero;
						num = leadingScore;
					}
					bool flag = leaderHero.MapFaction == eventFaction;
					bool isKingdomLeader = leaderHero.IsKingdomLeader;
					bool flag2 = this.IsArmyLeader(leaderHero);
					bool flag3 = hero.MapFaction == eventFaction;
					bool isKingdomLeader2 = hero.IsKingdomLeader;
					bool flag4 = this.IsArmyLeader(hero);
					if (!flag3 && flag)
					{
						hero = leaderHero;
						num = leadingScore;
					}
					else if (flag == flag3)
					{
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
			}
			return hero;
		}

		// Token: 0x060017B5 RID: 6069 RVA: 0x00070834 File Offset: 0x0006EA34
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

		// Token: 0x060017B6 RID: 6070 RVA: 0x000708D8 File Offset: 0x0006EAD8
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

		// Token: 0x060017B7 RID: 6071 RVA: 0x00070928 File Offset: 0x0006EB28
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

		// Token: 0x060017B8 RID: 6072 RVA: 0x00070978 File Offset: 0x0006EB78
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
				result = HideoutEventComponent.CreateHideoutEvent(attackerParty, defenderParty, false);
				break;
			case MapEvent.BattleTypes.SallyOut:
				Campaign.Current.MapEventManager.StartSallyOutMapEvent(attackerParty, defenderParty);
				break;
			case MapEvent.BattleTypes.SiegeOutside:
				Campaign.Current.MapEventManager.StartSiegeOutsideMapEvent(attackerParty, defenderParty);
				break;
			case MapEvent.BattleTypes.BlockadeBattle:
				result = BlockadeBattleMapEvent.CreateBlockadeBattleMapEvent(attackerParty, defenderParty, false);
				break;
			case MapEvent.BattleTypes.BlockadeSallyOutBattle:
				result = BlockadeBattleMapEvent.CreateBlockadeBattleMapEvent(attackerParty, defenderParty, true);
				break;
			}
			return result;
		}

		// Token: 0x060017B9 RID: 6073 RVA: 0x00070A2C File Offset: 0x0006EC2C
		public override float GetSurrenderChance(MobileParty defenderParty, MobileParty attackerParty)
		{
			float num = defenderParty.Party.CalculateCurrentStrength();
			float num2 = attackerParty.Party.CalculateCurrentStrength();
			if (num.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				return 1f;
			}
			if (num2.ApproximatelyEqualsTo(0f, 1E-05f))
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
				if (defenderParty.ActualClan.StringId == "deserters")
				{
					num3 = 0.005f;
				}
				else
				{
					num3 = 0.1f;
				}
				num4 = -15f;
			}
			else
			{
				Debug.FailedAssert("Unable to calculate threshold and exponentialScalingFactor!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultEncounterModel.cs", "GetSurrenderChance", 342);
			}
			float num5 = num / num2;
			float num6 = num4 * (num5 - num3);
			float num7 = 1f - 1f / (1f + (float)Math.Exp((double)num6));
			if (!MobileParty.MainParty.IsCurrentlyAtSea && Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.Scarface))
			{
				num7 = MathF.Min(1f, num7 * (1f + DefaultPerks.Roguery.Scarface.PrimaryBonus));
			}
			return num7;
		}

		// Token: 0x060017BA RID: 6074 RVA: 0x00070B7C File Offset: 0x0006ED7C
		public override ExplainedNumber GetBribeChance(MobileParty defenderParty, MobileParty attackerParty)
		{
			float num = defenderParty.Party.CalculateCurrentStrength();
			float num2 = attackerParty.Party.CalculateCurrentStrength();
			if (num.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				return new ExplainedNumber(1f, false, null);
			}
			if (num2.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				return new ExplainedNumber(0f, false, null);
			}
			if (num >= num2)
			{
				return new ExplainedNumber(0f, false, null);
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
				Debug.FailedAssert("Unable to calculate threshold and exponentialScalingFactor!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultEncounterModel.cs", "GetBribeChance", 397);
			}
			float num5 = num / num2;
			float num6 = num4 * (num5 - num3);
			ExplainedNumber result = new ExplainedNumber(1f - 1f / (1f + (float)Math.Exp((double)num6)), false, null);
			result.LimitMax(1f, null);
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.Scarface, Hero.MainHero.CharacterObject, true, ref result, false);
			return result;
		}

		// Token: 0x060017BB RID: 6075 RVA: 0x00070CB0 File Offset: 0x0006EEB0
		public override float GetMapEventSideRunAwayChance(MapEventSide mapEventSide)
		{
			float result = 0f;
			if (mapEventSide.MapEvent.EventType != MapEvent.BattleTypes.Siege && mapEventSide.MapEvent.EventType != MapEvent.BattleTypes.SallyOut && mapEventSide.MapEvent.EventType != MapEvent.BattleTypes.SiegeOutside && mapEventSide.MapEvent.EventType != MapEvent.BattleTypes.Raid && mapEventSide != MobileParty.MainParty.MapEventSide)
			{
				result = this.GetRunAwayChanceInternal(mapEventSide);
			}
			return result;
		}

		// Token: 0x060017BC RID: 6076 RVA: 0x00070D14 File Offset: 0x0006EF14
		private float GetRunAwayChanceInternal(MapEventSide mapEventSide)
		{
			MapEvent mapEvent = mapEventSide.MapEvent;
			float num = 0f;
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
				num = 0.2f;
				Hero leaderHero = mapEventSide.LeaderParty.LeaderHero;
				int num2 = ((leaderHero != null) ? leaderHero.GetTraitLevel(DefaultTraits.Valor) : 0);
				num -= (float)num2 * 0.05f;
			}
			return num;
		}

		// Token: 0x060017BD RID: 6077 RVA: 0x00070DC0 File Offset: 0x0006EFC0
		public override void FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(List<MobileParty> partiesToJoinPlayerSide, List<MobileParty> partiesToJoinEnemySide)
		{
			CampaignVec2 campaignVec = MobileParty.MainParty.Position;
			float radius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
			if (PlayerEncounter.Battle != null)
			{
				campaignVec = PlayerEncounter.Battle.Position;
				if (PlayerEncounter.Battle.IsSallyOut)
				{
					campaignVec = ((PlayerSiege.PlayerSiegeEvent != null) ? PlayerSiege.PlayerSiegeEvent : PlayerEncounter.EncounterSettlement.SiegeEvent).BesiegerCamp.LeaderParty.Position;
				}
				else if (PlayerEncounter.Battle.IsBlockade || PlayerEncounter.Battle.IsBlockadeSallyOut)
				{
					Settlement besiegedSettlement = PlayerSiege.BesiegedSettlement;
					campaignVec = ((besiegedSettlement != null) ? besiegedSettlement.PortPosition : PlayerEncounter.Battle.MapEventSettlement.PortPosition);
					radius = Campaign.Current.Models.EncounterModel.NeededMaximumDistanceForEncounteringBlockade * 3f;
				}
			}
			LocatableSearchData<MobileParty> locatableSearchData = MobileParty.StartFindingLocatablesAroundPosition(campaignVec.ToVec2(), radius);
			MobileParty nearbyParty = MobileParty.FindNextLocatable(ref locatableSearchData);
			List<MobileParty> list = new List<MobileParty>();
			List<MobileParty> list2 = new List<MobileParty>();
			Func<MobileParty, bool> <>9__4;
			Func<MobileParty, bool> <>9__5;
			while (nearbyParty != null)
			{
				bool flag = (PlayerEncounter.Battle != null && (PlayerEncounter.Battle.IsBlockade || PlayerEncounter.Battle.IsBlockadeSallyOut)) || MobileParty.MainParty.IsCurrentlyAtSea;
				if (nearbyParty != MobileParty.MainParty && nearbyParty.MapEvent == null && !nearbyParty.IsInRaftState && nearbyParty.SiegeEvent == null && nearbyParty.CurrentSettlement == null && nearbyParty.AttachedTo == null && nearbyParty.IsCurrentlyAtSea == flag && (nearbyParty.IsLordParty || nearbyParty.IsBandit || nearbyParty.IsPatrolParty || nearbyParty.ShouldJoinPlayerBattles))
				{
					if (PlayerEncounter.Battle != null)
					{
						bool flag2 = PlayerEncounter.Battle.CanPartyJoinBattle(nearbyParty.Party, PlayerEncounter.Battle.PlayerSide);
						bool flag3 = PlayerEncounter.Battle.CanPartyJoinBattle(nearbyParty.Party, PlayerEncounter.Battle.PlayerSide.GetOppositeSide());
						if (flag2)
						{
							list.Add(nearbyParty);
						}
						if (flag3)
						{
							list2.Add(nearbyParty);
						}
					}
					else
					{
						if (!nearbyParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction) && nearbyParty.MapFaction.IsAtWarWith(PlayerEncounter.EncounteredParty.MapFaction))
						{
							IEnumerable<MobileParty> source = list2;
							Func<MobileParty, bool> predicate;
							if ((predicate = <>9__4) == null)
							{
								predicate = (<>9__4 = (MobileParty x) => x.MapFaction.IsAtWarWith(nearbyParty.MapFaction));
							}
							if (source.All(predicate))
							{
								list.Add(nearbyParty);
							}
						}
						if (nearbyParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction) && !nearbyParty.MapFaction.IsAtWarWith(PlayerEncounter.EncounteredParty.MapFaction))
						{
							IEnumerable<MobileParty> source2 = list;
							Func<MobileParty, bool> predicate2;
							if ((predicate2 = <>9__5) == null)
							{
								predicate2 = (<>9__5 = (MobileParty x) => x.MapFaction.IsAtWarWith(nearbyParty.MapFaction));
							}
							if (source2.All(predicate2))
							{
								list2.Add(nearbyParty);
							}
						}
					}
				}
				nearbyParty = MobileParty.FindNextLocatable(ref locatableSearchData);
			}
			if (!list2.AnyQ((MobileParty t) => t.ShouldBeIgnored))
			{
				if (!partiesToJoinEnemySide.AnyQ((MobileParty t) => t.ShouldBeIgnored))
				{
					goto IL_3A5;
				}
			}
			Debug.Print("Ally parties wont join player encounter since there is an ignored party in enemy side", 0, Debug.DebugColor.White, 17592186044416UL);
			list.Clear();
			IL_3A5:
			if (!list.AnyQ((MobileParty t) => t.ShouldBeIgnored))
			{
				if (!partiesToJoinPlayerSide.AnyQ((MobileParty t) => t != MobileParty.MainParty && t.ShouldBeIgnored))
				{
					goto IL_411;
				}
			}
			Debug.Print("Enemy parties wont join player encounter since there is an ignored party in ally side", 0, Debug.DebugColor.White, 17592186044416UL);
			list2.Clear();
			IL_411:
			partiesToJoinPlayerSide.AddRange(list.Except(partiesToJoinPlayerSide));
			partiesToJoinEnemySide.AddRange(list2.Except(partiesToJoinEnemySide));
		}

		// Token: 0x060017BE RID: 6078 RVA: 0x000711FC File Offset: 0x0006F3FC
		public override bool CanPlayerForceBanditsToJoin(out TextObject explanation)
		{
			bool perkValue = Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.PartnersInCrime);
			explanation = (perkValue ? null : new TextObject("{=MaetSSa1}You need '{PERK}' perk to make this party join you.", null));
			TextObject textObject = explanation;
			if (textObject != null)
			{
				textObject.SetTextVariable("PERK", DefaultPerks.Roguery.PartnersInCrime.Name);
			}
			return perkValue;
		}

		// Token: 0x060017BF RID: 6079 RVA: 0x0007124C File Offset: 0x0006F44C
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
			bool flag = party.Owner == Hero.MainHero;
			IFaction mapFaction = party.MapFaction;
			bool flag2 = ((mapFaction != null) ? mapFaction.Leader : null) == Hero.MainHero;
			bool flag3 = party.MobileParty != null && party.MobileParty.DefaultBehavior == AiBehavior.EscortParty && party.MobileParty.TargetParty == MobileParty.MainParty;
			bool flag4 = party.MobileParty != null && party.MobileParty.Army != null && party.MobileParty.Army.LeaderParty == MobileParty.MainParty;
			Settlement mapEventSettlement = party.MapEvent.MapEventSettlement;
			bool flag5 = mapEventSettlement != null && mapEventSettlement.OwnerClan.Leader == Hero.MainHero;
			return flag || flag2 || flag3 || flag4 || flag5;
		}
	}
}
