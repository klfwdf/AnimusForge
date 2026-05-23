using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000F0 RID: 240
	public class DefaultArmyManagementCalculationModel : ArmyManagementCalculationModel
	{
		// Token: 0x1700060B RID: 1547
		// (get) Token: 0x06001616 RID: 5654 RVA: 0x000644FD File Offset: 0x000626FD
		public override float AIMobilePartySizeRatioToCallToArmy
		{
			get
			{
				return 0.6f;
			}
		}

		// Token: 0x1700060C RID: 1548
		// (get) Token: 0x06001617 RID: 5655 RVA: 0x00064504 File Offset: 0x00062704
		public override float PlayerMobilePartySizeRatioToCallToArmy
		{
			get
			{
				return 0.4f;
			}
		}

		// Token: 0x1700060D RID: 1549
		// (get) Token: 0x06001618 RID: 5656 RVA: 0x0006450B File Offset: 0x0006270B
		public override float MinimumNeededFoodInDaysToCallToArmy
		{
			get
			{
				return 15f;
			}
		}

		// Token: 0x1700060E RID: 1550
		// (get) Token: 0x06001619 RID: 5657 RVA: 0x00064512 File Offset: 0x00062712
		public override float MaximumDistanceToCallToArmy
		{
			get
			{
				return Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All) * 8f;
			}
		}

		// Token: 0x1700060F RID: 1551
		// (get) Token: 0x0600161A RID: 5658 RVA: 0x00064525 File Offset: 0x00062725
		public override int InfluenceValuePerGold
		{
			get
			{
				return 40;
			}
		}

		// Token: 0x17000610 RID: 1552
		// (get) Token: 0x0600161B RID: 5659 RVA: 0x00064529 File Offset: 0x00062729
		public override int AverageCallToArmyCost
		{
			get
			{
				return 20;
			}
		}

		// Token: 0x17000611 RID: 1553
		// (get) Token: 0x0600161C RID: 5660 RVA: 0x0006452D File Offset: 0x0006272D
		public override int CohesionThresholdForDispersion
		{
			get
			{
				return 10;
			}
		}

		// Token: 0x17000612 RID: 1554
		// (get) Token: 0x0600161D RID: 5661 RVA: 0x00064531 File Offset: 0x00062731
		public override float MaximumWaitTime
		{
			get
			{
				return (float)CampaignTime.HoursInDay * 3f;
			}
		}

		// Token: 0x0600161E RID: 5662 RVA: 0x00064540 File Offset: 0x00062740
		public override float DailyBeingAtArmyInfluenceAward(MobileParty armyMemberParty)
		{
			float num = (armyMemberParty.Party.EstimatedStrength + 20f) / 200f;
			if (PartyBaseHelper.HasFeat(armyMemberParty.Party, DefaultCulturalFeats.EmpireArmyInfluenceFeat))
			{
				num += num * DefaultCulturalFeats.EmpireArmyInfluenceFeat.EffectBonus;
			}
			return num;
		}

		// Token: 0x0600161F RID: 5663 RVA: 0x00064588 File Offset: 0x00062788
		public override int CalculatePartyInfluenceCost(MobileParty armyLeaderParty, MobileParty party)
		{
			if (armyLeaderParty.LeaderHero != null && party.LeaderHero != null && armyLeaderParty.LeaderHero.Clan == party.LeaderHero.Clan)
			{
				return 0;
			}
			float num = (float)armyLeaderParty.LeaderHero.GetRelation(party.LeaderHero);
			float partySizeScore = this.GetPartySizeScore(party);
			float b = (float)MathF.Round(party.Party.EstimatedStrength);
			float num2 = (armyLeaderParty.IsMainParty ? Campaign.Current.Models.ArmyManagementCalculationModel.PlayerMobilePartySizeRatioToCallToArmy : Campaign.Current.Models.ArmyManagementCalculationModel.AIMobilePartySizeRatioToCallToArmy);
			float num3 = ((num < 0f) ? (1f + MathF.Sqrt(MathF.Abs(MathF.Max(-100f, num))) / 10f) : (1f - MathF.Sqrt(MathF.Abs(MathF.Min(100f, num))) / 20f));
			float num4 = 0.5f + MathF.Min(1000f, b) / 100f;
			float num5 = 0.5f + 1f * (1f - (partySizeScore - num2) / (1f - num2));
			float num6;
			float distanceBetweenMobilePartyToMobileParty = DistanceHelper.GetDistanceBetweenMobilePartyToMobileParty(party, armyLeaderParty, party.NavigationCapability, out num6);
			float num7 = 1f + 1f * MathF.Pow(MathF.Min(Campaign.MapDiagonal * 10f, MathF.Max(1f, distanceBetweenMobilePartyToMobileParty)) / Campaign.MapDiagonal, 0.67f);
			float num8 = ((party.LeaderHero != null) ? party.LeaderHero.RandomFloat(0.75f, 1.25f) : 1f);
			float num9 = 1f;
			float num10 = 1f;
			float num11 = 1f;
			Hero leaderHero = armyLeaderParty.LeaderHero;
			if (((leaderHero != null) ? leaderHero.Clan.Kingdom : null) != null)
			{
				if (armyLeaderParty.LeaderHero.Clan.Tier >= 5 && armyLeaderParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Marshals))
				{
					num9 -= 0.1f;
				}
				if (armyLeaderParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.RoyalCommissions))
				{
					if (armyLeaderParty.LeaderHero == armyLeaderParty.LeaderHero.Clan.Kingdom.Leader)
					{
						num9 -= 0.3f;
					}
					else
					{
						num9 += 0.1f;
					}
				}
				if (party.LeaderHero != null)
				{
					if (armyLeaderParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LordsPrivyCouncil) && party.LeaderHero.Clan.Tier <= 4)
					{
						num9 += 0.2f;
					}
					if (armyLeaderParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Senate) && party.LeaderHero.Clan.Tier <= 2)
					{
						num9 += 0.1f;
					}
				}
				if (armyLeaderParty.LeaderHero.GetPerkValue(DefaultPerks.Leadership.InspiringLeader))
				{
					num10 += DefaultPerks.Leadership.InspiringLeader.PrimaryBonus;
				}
				if (armyLeaderParty.LeaderHero.GetPerkValue(DefaultPerks.Tactics.CallToArms))
				{
					num10 += DefaultPerks.Tactics.CallToArms.SecondaryBonus;
				}
			}
			if (PartyBaseHelper.HasFeat(armyLeaderParty.Party, DefaultCulturalFeats.VlandianArmyInfluenceFeat))
			{
				num11 += DefaultCulturalFeats.VlandianArmyInfluenceFeat.EffectBonus;
			}
			if (PartyBaseHelper.HasFeat(armyLeaderParty.Party, DefaultCulturalFeats.SturgianArmyInfluenceCostFeat))
			{
				num11 += DefaultCulturalFeats.SturgianArmyInfluenceCostFeat.EffectBonus;
			}
			return (int)(0.65f * num3 * num4 * num8 * num7 * num5 * num9 * num10 * num11 * (float)this.AverageCallToArmyCost);
		}

		// Token: 0x06001620 RID: 5664 RVA: 0x00064908 File Offset: 0x00062B08
		public override List<MobileParty> GetMobilePartiesToCallToArmy(MobileParty leaderParty)
		{
			List<MobileParty> list = new List<MobileParty>();
			bool flag = false;
			bool flag2 = false;
			if (leaderParty.LeaderHero != null)
			{
				foreach (Settlement settlement in leaderParty.MapFaction.Settlements)
				{
					if (settlement.IsFortification && settlement.SiegeEvent != null)
					{
						flag = true;
						if (settlement.OwnerClan == leaderParty.LeaderHero.Clan)
						{
							flag2 = true;
						}
					}
				}
			}
			int b = ((leaderParty.MapFaction.IsKingdomFaction && (Kingdom)leaderParty.MapFaction != null) ? ((Kingdom)leaderParty.MapFaction).Armies.Count : 0);
			float num = (1.5f - (float)MathF.Min(2, b) * 0.05f - ((Hero.MainHero.MapFaction == leaderParty.MapFaction) ? 0.05f : 0f)) * (1f - 0.5f * MathF.Sqrt(MathF.Min(leaderParty.LeaderHero.Clan.Influence, 900f)) * 0.033333335f);
			num *= (flag2 ? 1.25f : 1f);
			num *= (flag ? 1.125f : 1f);
			num *= leaderParty.LeaderHero.RandomFloat(0.85f, 1f);
			float num2 = MathF.Min(leaderParty.LeaderHero.Clan.Influence, 900f) * MathF.Min(1f, num);
			List<ValueTuple<MobileParty, float>> list2 = new List<ValueTuple<MobileParty, float>>();
			foreach (WarPartyComponent warPartyComponent in leaderParty.MapFaction.WarPartyComponents)
			{
				MobileParty mobileParty = warPartyComponent.MobileParty;
				Hero leaderHero = mobileParty.LeaderHero;
				if (mobileParty.IsLordParty && mobileParty.Army == null && mobileParty != leaderParty && leaderHero != null && !mobileParty.IsMainParty && leaderHero != leaderHero.MapFaction.Leader && !mobileParty.Ai.DoNotMakeNewDecisions)
				{
					Settlement currentSettlement = mobileParty.CurrentSettlement;
					if (((currentSettlement != null) ? currentSettlement.SiegeEvent : null) == null && !mobileParty.IsDisbanding && (float)mobileParty.GetNumDaysForFoodToLast() > Campaign.Current.Models.ArmyManagementCalculationModel.MinimumNeededFoodInDaysToCallToArmy && mobileParty.PartySizeRatio > Campaign.Current.Models.ArmyManagementCalculationModel.AIMobilePartySizeRatioToCallToArmy && leaderHero.CanLeadParty() && !mobileParty.IsInRaftState && mobileParty.MapEvent == null && mobileParty.BesiegedSettlement == null)
					{
						IDisbandPartyCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
						if (campaignBehavior == null || !campaignBehavior.IsPartyWaitingForDisband(mobileParty))
						{
							float maximumDistanceToCallToArmy = Campaign.Current.Models.ArmyManagementCalculationModel.MaximumDistanceToCallToArmy;
							float num3;
							if (DistanceHelper.GetDistanceBetweenMobilePartyToMobileParty(mobileParty, leaderParty, mobileParty.NavigationCapability, out num3) < maximumDistanceToCallToArmy)
							{
								bool flag3 = false;
								using (List<ValueTuple<MobileParty, float>>.Enumerator enumerator3 = list2.GetEnumerator())
								{
									while (enumerator3.MoveNext())
									{
										if (enumerator3.Current.Item1 == mobileParty)
										{
											flag3 = true;
											break;
										}
									}
								}
								if (!flag3)
								{
									int num4 = Campaign.Current.Models.ArmyManagementCalculationModel.CalculatePartyInfluenceCost(leaderParty, mobileParty);
									float estimatedStrength = mobileParty.Party.EstimatedStrength;
									float num5 = 1f - (float)mobileParty.Party.MemberRoster.TotalWounded / (float)mobileParty.Party.MemberRoster.TotalManCount;
									float item = estimatedStrength / ((float)num4 + 0.1f) * num5;
									list2.Add(new ValueTuple<MobileParty, float>(mobileParty, item));
								}
							}
						}
					}
				}
			}
			int num7;
			do
			{
				float num6 = 0.01f;
				num7 = -1;
				for (int i = 0; i < list2.Count; i++)
				{
					ValueTuple<MobileParty, float> valueTuple = list2[i];
					if (valueTuple.Item2 > num6)
					{
						num7 = i;
						num6 = valueTuple.Item2;
					}
				}
				if (num7 >= 0)
				{
					MobileParty item2 = list2[num7].Item1;
					int num8 = Campaign.Current.Models.ArmyManagementCalculationModel.CalculatePartyInfluenceCost(leaderParty, item2);
					list2[num7] = new ValueTuple<MobileParty, float>(item2, 0f);
					if (num2 > (float)num8)
					{
						num2 -= (float)num8;
						list.Add(item2);
					}
				}
			}
			while (num7 >= 0);
			return list;
		}

		// Token: 0x06001621 RID: 5665 RVA: 0x00064DBC File Offset: 0x00062FBC
		public override int CalculateTotalInfluenceCost(Army army, float percentage)
		{
			int num = this.CalculateTotalInfluenceCostInternal(army, percentage);
			if (army != MobileParty.MainParty.Army)
			{
				num = (int)((float)num * 0.25f);
			}
			return num;
		}

		// Token: 0x06001622 RID: 5666 RVA: 0x00064DEC File Offset: 0x00062FEC
		private int CalculateTotalInfluenceCostInternal(Army army, float percentage)
		{
			int num = 0;
			foreach (MobileParty party in from p in army.Parties
				where !p.IsMainParty
				select p)
			{
				num += this.CalculatePartyInfluenceCost(army.LeaderParty, party);
			}
			ExplainedNumber explainedNumber = new ExplainedNumber((float)num, false, null);
			if (army.LeaderParty.MapFaction.IsKingdomFaction && ((Kingdom)army.LeaderParty.MapFaction).ActivePolicies.Contains(DefaultPolicies.RoyalCommissions))
			{
				explainedNumber.AddFactor(-0.3f, null);
			}
			if (army.LeaderParty.LeaderHero.GetPerkValue(DefaultPerks.Tactics.Encirclement))
			{
				explainedNumber.AddFactor(DefaultPerks.Tactics.Encirclement.SecondaryBonus, null);
			}
			return MathF.Ceiling(explainedNumber.ResultNumber * percentage / 100f);
		}

		// Token: 0x06001623 RID: 5667 RVA: 0x00064EF0 File Offset: 0x000630F0
		public override float GetPartySizeScore(MobileParty party)
		{
			return MathF.Min(1f, party.PartySizeRatio);
		}

		// Token: 0x06001624 RID: 5668 RVA: 0x00064F04 File Offset: 0x00063104
		public override ExplainedNumber CalculateDailyCohesionChange(Army army, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(-2f, includeDescriptions, null);
			this.CalculateCohesionChangeInternal(army, ref result);
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.HordeLeader, army.LeaderParty, false, ref result, army.LeaderParty.IsCurrentlyAtSea);
			SiegeEvent siegeEvent = army.LeaderParty.SiegeEvent;
			if (siegeEvent != null && siegeEvent.BesiegerCamp.IsBesiegerSideParty(army.LeaderParty) && army.LeaderParty.HasPerk(DefaultPerks.Engineering.CampBuilding, false))
			{
				result.AddFactor(DefaultPerks.Engineering.CampBuilding.PrimaryBonus, DefaultPerks.Engineering.CampBuilding.Name);
			}
			return result;
		}

		// Token: 0x06001625 RID: 5669 RVA: 0x00064F98 File Offset: 0x00063198
		private void CalculateCohesionChangeInternal(Army army, ref ExplainedNumber cohesionChange)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			foreach (MobileParty mobileParty in army.LeaderParty.AttachedParties)
			{
				if (mobileParty.Party.IsStarving)
				{
					num++;
				}
				if (mobileParty.Morale <= 25f)
				{
					num2++;
				}
				if (mobileParty.Party.NumberOfHealthyMembers <= 10)
				{
					num3++;
				}
				num4++;
			}
			float num5 = (float)(-(float)num4);
			float num6 = -((float)(num + 1) / 2f);
			float num7 = -((float)(num2 + 1) / 2f);
			float num8 = -((float)(num3 + 1) / 2f);
			if (army.LeaderParty != MobileParty.MainParty)
			{
				num5 *= 0.25f;
				num6 *= 0.25f;
				num7 *= 0.25f;
				num8 *= 0.25f;
			}
			cohesionChange.Add(num5, this._numberOfPartiesText, null);
			cohesionChange.Add(num6, this._numberOfStarvingPartiesText, null);
			cohesionChange.Add(num7, this._numberOfLowMoralePartiesText, null);
			cohesionChange.Add(num8, this._numberOfLessMemberPartiesText, null);
		}

		// Token: 0x06001626 RID: 5670 RVA: 0x000650C8 File Offset: 0x000632C8
		public override int CalculateNewCohesion(Army army, PartyBase newParty, int calculatedCohesion, int sign)
		{
			if (army == null)
			{
				return calculatedCohesion;
			}
			sign = MathF.Sign(sign);
			int num = ((sign == 1) ? (army.Parties.Count - 1) : army.Parties.Count);
			int num2 = (calculatedCohesion * num + 100 * sign) / (num + sign);
			if (num2 > 100)
			{
				return 100;
			}
			if (num2 >= 0)
			{
				return num2;
			}
			return 0;
		}

		// Token: 0x06001627 RID: 5671 RVA: 0x00065121 File Offset: 0x00063321
		public override int GetCohesionBoostInfluenceCost(Army army, int percentageToBoost = 100)
		{
			return this.CalculateTotalInfluenceCostInternal(army, (float)percentageToBoost);
		}

		// Token: 0x06001628 RID: 5672 RVA: 0x0006512C File Offset: 0x0006332C
		public override int GetPartyRelation(Hero hero)
		{
			if (hero == null)
			{
				return -101;
			}
			if (hero == Hero.MainHero)
			{
				return 101;
			}
			return Hero.MainHero.GetRelation(hero);
		}

		// Token: 0x06001629 RID: 5673 RVA: 0x0006514C File Offset: 0x0006334C
		public override bool CanPlayerCreateArmy(out TextObject disabledReason)
		{
			if (Clan.PlayerClan.Kingdom == null)
			{
				disabledReason = new TextObject("{=XSQ0Y9gy}You need to be a part of a kingdom to create an army.", null);
				return false;
			}
			if (Clan.PlayerClan.IsUnderMercenaryService)
			{
				disabledReason = new TextObject("{=aRhQzJca}Mercenaries cannot create or manage armies.", null);
				return false;
			}
			if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
			{
				disabledReason = new TextObject("{=NAA4pajB}You need to leave your current army to create a new one.", null);
				return false;
			}
			if (MobileParty.MainParty.IsCurrentlyAtSea)
			{
				disabledReason = GameTexts.FindText("str_cannot_gather_army_at_sea", null);
				return false;
			}
			if (Hero.MainHero.IsPrisoner)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_prisoner", null);
				return false;
			}
			if (MobileParty.MainParty.IsInRaftState)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_raft_state", null);
				return false;
			}
			if (CampaignMission.Current != null)
			{
				disabledReason = new TextObject("{=FdzsOvDq}This action is disabled while in a mission", null);
				return false;
			}
			if (PlayerEncounter.Current != null)
			{
				if (PlayerEncounter.EncounterSettlement == null)
				{
					disabledReason = GameTexts.FindText("str_action_disabled_reason_encounter", null);
					return false;
				}
				Village village = PlayerEncounter.EncounterSettlement.Village;
				if (village != null && village.VillageState == Village.VillageStates.BeingRaided)
				{
					MapEvent mapEvent = MobileParty.MainParty.MapEvent;
					if (mapEvent != null && mapEvent.IsRaid)
					{
						disabledReason = GameTexts.FindText("str_action_disabled_reason_raid", null);
						return false;
					}
				}
				if (PlayerEncounter.EncounterSettlement.IsUnderSiege)
				{
					disabledReason = GameTexts.FindText("str_action_disabled_reason_siege", null);
					return false;
				}
			}
			else
			{
				if (PlayerSiege.PlayerSiegeEvent != null)
				{
					disabledReason = GameTexts.FindText("str_action_disabled_reason_siege", null);
					return false;
				}
				if (MobileParty.MainParty.MapEvent != null)
				{
					disabledReason = new TextObject("{=MIylzRc5}You can't perform this action while you are in a map event.", null);
					return false;
				}
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x0600162A RID: 5674 RVA: 0x000652DC File Offset: 0x000634DC
		public override bool CheckPartyEligibility(MobileParty party, out TextObject explanation)
		{
			bool result = true;
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				result = false;
				explanation = GameTexts.FindText("str_action_disabled_reason_siege", null);
			}
			else if (party == null)
			{
				result = false;
				explanation = new TextObject("{=f6vTzVar}Does not have a mobile party.", null);
			}
			else
			{
				Hero leaderHero = party.LeaderHero;
				IFaction mapFaction = Hero.MainHero.MapFaction;
				if (leaderHero == ((mapFaction != null) ? mapFaction.Leader : null))
				{
					result = false;
					explanation = new TextObject("{=ipLqVv1f}You cannot invite the ruler's party to your army.", null);
				}
				else
				{
					if (party.Army != null)
					{
						Army army = party.Army;
						MobileParty partyBelongedTo = Hero.MainHero.PartyBelongedTo;
						if (army != ((partyBelongedTo != null) ? partyBelongedTo.Army : null))
						{
							result = false;
							explanation = new TextObject("{=aROohsat}Already in another army.", null);
							return result;
						}
					}
					if (party.Army != null)
					{
						Army army2 = party.Army;
						MobileParty partyBelongedTo2 = Hero.MainHero.PartyBelongedTo;
						if (army2 == ((partyBelongedTo2 != null) ? partyBelongedTo2.Army : null))
						{
							result = false;
							explanation = new TextObject("{=Vq8yavES}Already in army.", null);
							return result;
						}
					}
					if (party.MapEvent != null || party.SiegeEvent != null || (party.CurrentSettlement != null && party.CurrentSettlement.IsUnderSiege))
					{
						result = false;
						explanation = new TextObject("{=pkbUiKFJ}Currently fighting an enemy.", null);
					}
					else if (this.GetPartySizeScore(party) <= Campaign.Current.Models.ArmyManagementCalculationModel.PlayerMobilePartySizeRatioToCallToArmy)
					{
						result = false;
						explanation = new TextObject("{=SVJlOYCB}Party has less men than 40% of it's party size limit.", null);
					}
					else
					{
						if (!party.IsDisbanding)
						{
							IDisbandPartyCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
							if (campaignBehavior == null || !campaignBehavior.IsPartyWaitingForDisband(party))
							{
								if (MobileParty.MainParty.IsCurrentlyAtSea)
								{
									result = false;
									explanation = ((!party.HasNavalNavigationCapability) ? new TextObject("{=nqq84Dzq}Party cannot reach your army since it has no ships.", null) : new TextObject("{=gFixGQsr}You cannot call a party to your army while your party is at sea.", null));
									return result;
								}
								if (party.IsInRaftState)
								{
									result = false;
									explanation = new TextObject("{=TbXDmh3t}This party is lost at sea.", null);
									return result;
								}
								float num;
								if (DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(party, MobileParty.MainParty, party.NavigationCapability, out num) > Campaign.Current.Models.ArmyManagementCalculationModel.MaximumDistanceToCallToArmy)
								{
									result = false;
									explanation = new TextObject("{=UINgZDN5}You can not call a party that is far away.", null);
									return result;
								}
								explanation = null;
								return result;
							}
						}
						result = false;
						explanation = new TextObject("{=tFGM0yav}This party is disbanding.", null);
					}
				}
			}
			return result;
		}

		// Token: 0x04000758 RID: 1880
		private readonly TextObject _numberOfPartiesText = GameTexts.FindText("str_number_of_parties", null);

		// Token: 0x04000759 RID: 1881
		private readonly TextObject _numberOfStarvingPartiesText = GameTexts.FindText("str_number_of_starving_parties", null);

		// Token: 0x0400075A RID: 1882
		private readonly TextObject _numberOfLowMoralePartiesText = GameTexts.FindText("str_number_of_low_morale_parties", null);

		// Token: 0x0400075B RID: 1883
		private readonly TextObject _numberOfLessMemberPartiesText = GameTexts.FindText("str_number_of_less_member_parties", null);
	}
}
