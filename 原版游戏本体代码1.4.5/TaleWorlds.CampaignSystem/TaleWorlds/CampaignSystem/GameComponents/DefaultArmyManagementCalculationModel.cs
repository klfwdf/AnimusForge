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
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultArmyManagementCalculationModel : ArmyManagementCalculationModel
{
	private const float MinimumInfluenceNeededToCreateArmy = 100f;

	private readonly TextObject _numberOfPartiesText = GameTexts.FindText("str_number_of_parties");

	private readonly TextObject _numberOfStarvingPartiesText = GameTexts.FindText("str_number_of_starving_parties");

	private readonly TextObject _numberOfLowMoralePartiesText = GameTexts.FindText("str_number_of_low_morale_parties");

	private readonly TextObject _numberOfLessMemberPartiesText = GameTexts.FindText("str_number_of_less_member_parties");

	public override float AIMobilePartySizeRatioToCallToArmy => 0.6f;

	public override float PlayerMobilePartySizeRatioToCallToArmy => 0.4f;

	public override float MinimumNeededFoodInDaysToCallToArmy => 15f;

	public override float MaximumDistanceToCallToArmy => Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.All) * 8f;

	public override int InfluenceValuePerGold => 40;

	public override int AverageCallToArmyCost => 20;

	public override int CohesionThresholdForDispersion => 10;

	public override float MaximumWaitTime => (float)CampaignTime.HoursInDay * 3f;

	public override float DailyBeingAtArmyInfluenceAward(MobileParty armyMemberParty)
	{
		float num = (armyMemberParty.Party.EstimatedStrength + 20f) / 200f;
		if (PartyBaseHelper.HasFeat(armyMemberParty.Party, DefaultCulturalFeats.EmpireArmyInfluenceFeat))
		{
			num += num * DefaultCulturalFeats.EmpireArmyInfluenceFeat.EffectBonus;
		}
		return num;
	}

	public override int CalculatePartyInfluenceCost(MobileParty armyLeaderParty, MobileParty party)
	{
		if (armyLeaderParty.LeaderHero != null && party.LeaderHero != null && armyLeaderParty.LeaderHero.Clan == party.LeaderHero.Clan)
		{
			return 0;
		}
		float num = armyLeaderParty.LeaderHero.GetRelation(party.LeaderHero);
		float partySizeScore = GetPartySizeScore(party);
		float b = MathF.Round(party.Party.EstimatedStrength);
		float num2 = (armyLeaderParty.IsMainParty ? Campaign.Current.Models.ArmyManagementCalculationModel.PlayerMobilePartySizeRatioToCallToArmy : Campaign.Current.Models.ArmyManagementCalculationModel.AIMobilePartySizeRatioToCallToArmy);
		float num3 = ((num < 0f) ? (1f + MathF.Sqrt(MathF.Abs(MathF.Max(-100f, num))) / 10f) : (1f - MathF.Sqrt(MathF.Abs(MathF.Min(100f, num))) / 20f));
		float num4 = 0.5f + MathF.Min(1000f, b) / 100f;
		float num5 = 0.5f + 1f * (1f - (partySizeScore - num2) / (1f - num2));
		float landRatio;
		float distanceBetweenMobilePartyToMobileParty = DistanceHelper.GetDistanceBetweenMobilePartyToMobileParty(party, armyLeaderParty, party.NavigationCapability, out landRatio);
		float num6 = 1f + 1f * MathF.Pow(MathF.Min(Campaign.MapDiagonal * 10f, MathF.Max(1f, distanceBetweenMobilePartyToMobileParty)) / Campaign.MapDiagonal, 0.67f);
		float num7 = ((party.LeaderHero != null) ? party.LeaderHero.RandomFloat(0.75f, 1.25f) : 1f);
		float num8 = 1f;
		float num9 = 1f;
		float num10 = 1f;
		if (armyLeaderParty.LeaderHero?.Clan.Kingdom != null)
		{
			if (armyLeaderParty.LeaderHero.Clan.Tier >= 5 && armyLeaderParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Marshals))
			{
				num8 -= 0.1f;
			}
			if (armyLeaderParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.RoyalCommissions))
			{
				num8 = ((armyLeaderParty.LeaderHero != armyLeaderParty.LeaderHero.Clan.Kingdom.Leader) ? (num8 + 0.1f) : (num8 - 0.3f));
			}
			if (party.LeaderHero != null)
			{
				if (armyLeaderParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.LordsPrivyCouncil) && party.LeaderHero.Clan.Tier <= 4)
				{
					num8 += 0.2f;
				}
				if (armyLeaderParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Senate) && party.LeaderHero.Clan.Tier <= 2)
				{
					num8 += 0.1f;
				}
			}
			if (armyLeaderParty.LeaderHero.GetPerkValue(DefaultPerks.Leadership.InspiringLeader))
			{
				num9 += DefaultPerks.Leadership.InspiringLeader.PrimaryBonus;
			}
			if (armyLeaderParty.LeaderHero.GetPerkValue(DefaultPerks.Tactics.CallToArms))
			{
				num9 += DefaultPerks.Tactics.CallToArms.SecondaryBonus;
			}
		}
		if (PartyBaseHelper.HasFeat(armyLeaderParty.Party, DefaultCulturalFeats.VlandianArmyInfluenceFeat))
		{
			num10 += DefaultCulturalFeats.VlandianArmyInfluenceFeat.EffectBonus;
		}
		if (PartyBaseHelper.HasFeat(armyLeaderParty.Party, DefaultCulturalFeats.SturgianArmyInfluenceCostFeat))
		{
			num10 += DefaultCulturalFeats.SturgianArmyInfluenceCostFeat.EffectBonus;
		}
		return (int)(0.65f * num3 * num4 * num7 * num6 * num5 * num8 * num9 * num10 * (float)AverageCallToArmyCost);
	}

	public override bool CanLordCreateArmy(MobileParty mobileParty, out MBList<MobileParty> possibleArmyMembers)
	{
		possibleArmyMembers = new MBList<MobileParty>();
		Kingdom kingdom = mobileParty.MapFaction as Kingdom;
		if (!mobileParty.IsCurrentlyAtSea && mobileParty.LeaderHero.Clan.Influence > 100f && !mobileParty.LeaderHero.Clan.IsUnderMercenaryService && (float)mobileParty.GetNumDaysForFoodToLast() > Campaign.Current.Models.MobilePartyAIModel.NeededFoodsInDaysThresholdForSiege && kingdom.FactionsAtWarWith.AnyQ((IFaction x) => x.Fiefs.Any()) && mobileParty.PartySizeRatio > Campaign.Current.Models.ArmyManagementCalculationModel.AIMobilePartySizeRatioToCallToArmy && (mobileParty.LeaderHero.Clan.Leader == mobileParty.LeaderHero || (mobileParty.LeaderHero.Clan.Leader.PartyBelongedTo == null && mobileParty.LeaderHero.Clan.WarPartyComponents != null && mobileParty.LeaderHero.Clan.WarPartyComponents.FirstOrDefault() == mobileParty.WarPartyComponent)))
		{
			GetInfluenceBudgetWhileCreatingArmy(mobileParty);
			List<(MobileParty, float, int)> list = new List<(MobileParty, float, int)>();
			foreach (WarPartyComponent warPartyComponent in mobileParty.MapFaction.WarPartyComponents)
			{
				MobileParty mobileParty2 = warPartyComponent.MobileParty;
				Hero leaderHero = mobileParty2.LeaderHero;
				if (!mobileParty2.IsLordParty || mobileParty2.Army != null || mobileParty2 == mobileParty || leaderHero == null || mobileParty2.IsMainParty || leaderHero == leaderHero.MapFaction.Leader || mobileParty2.Ai.DoNotMakeNewDecisions || mobileParty2.CurrentSettlement?.SiegeEvent != null || mobileParty2.IsDisbanding || !((float)mobileParty2.GetNumDaysForFoodToLast() > Campaign.Current.Models.ArmyManagementCalculationModel.MinimumNeededFoodInDaysToCallToArmy) || !(mobileParty2.PartySizeRatio > Campaign.Current.Models.ArmyManagementCalculationModel.AIMobilePartySizeRatioToCallToArmy) || !leaderHero.CanLeadParty() || mobileParty2.IsInRaftState || mobileParty2.MapEvent != null || mobileParty2.BesiegedSettlement != null)
				{
					continue;
				}
				IDisbandPartyCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
				if (campaignBehavior != null && campaignBehavior.IsPartyWaitingForDisband(mobileParty2))
				{
					continue;
				}
				float maximumDistanceToCallToArmy = Campaign.Current.Models.ArmyManagementCalculationModel.MaximumDistanceToCallToArmy;
				if (!(DistanceHelper.GetDistanceBetweenMobilePartyToMobileParty(mobileParty2, mobileParty, mobileParty2.NavigationCapability, out var _) < maximumDistanceToCallToArmy))
				{
					continue;
				}
				bool flag = false;
				foreach (var item2 in list)
				{
					if (item2.Item1 == mobileParty2)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					int num = Campaign.Current.Models.ArmyManagementCalculationModel.CalculatePartyInfluenceCost(mobileParty, mobileParty2);
					float estimatedStrength = mobileParty2.Party.EstimatedStrength;
					float num2 = 1f - (float)mobileParty2.Party.MemberRoster.TotalWounded / (float)mobileParty2.Party.MemberRoster.TotalManCount;
					float item = estimatedStrength / ((float)num + 0.1f) * num2;
					list.Add((mobileParty2, item, num));
				}
			}
			list = list.OrderByQ(((MobileParty, float, int) x) => x.Item2).ToListQ();
			int count = kingdom.WarPartyComponents.Count;
			int num3 = kingdom.Armies.SumQ((Army x) => x.Parties.Count);
			int num4 = MathF.Ceiling((float)count * 0.7f - (float)num3);
			if (num4 > 0)
			{
				if (num4 < list.Count)
				{
					list.RemoveRange(num4, list.Count - num4);
				}
				possibleArmyMembers = list.SelectQ(((MobileParty, float, int) x) => x.Item1).ToMBList();
				if (possibleArmyMembers.AnyQ())
				{
					if (kingdom.Settlements.Count == 0)
					{
						return true;
					}
					float num5 = mobileParty.Party.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.Siege);
					foreach (MobileParty possibleArmyMember in possibleArmyMembers)
					{
						num5 += possibleArmyMember.Party.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.Siege);
					}
					if (num5 < 1000f)
					{
						possibleArmyMembers.Clear();
						return false;
					}
					return true;
				}
			}
		}
		return false;
	}

	public override int CalculateTotalInfluenceCost(Army army, float percentage)
	{
		int num = CalculateTotalInfluenceCostInternal(army, percentage);
		if (army != MobileParty.MainParty.Army)
		{
			num = (int)((float)num * 0.25f);
		}
		return num;
	}

	private int CalculateTotalInfluenceCostInternal(Army army, float percentage)
	{
		int num = 0;
		foreach (MobileParty item in army.Parties.Where((MobileParty p) => !p.IsMainParty))
		{
			num += CalculatePartyInfluenceCost(army.LeaderParty, item);
		}
		ExplainedNumber explainedNumber = new ExplainedNumber(num);
		if (army.LeaderParty.MapFaction.IsKingdomFaction && ((Kingdom)army.LeaderParty.MapFaction).ActivePolicies.Contains(DefaultPolicies.RoyalCommissions))
		{
			explainedNumber.AddFactor(-0.3f, DefaultPolicies.RoyalCommissions.Name);
		}
		if (army.LeaderParty.LeaderHero.GetPerkValue(DefaultPerks.Tactics.Encirclement))
		{
			explainedNumber.AddFactor(DefaultPerks.Tactics.Encirclement.SecondaryBonus);
		}
		return MathF.Ceiling(explainedNumber.ResultNumber * percentage / 100f);
	}

	public override float GetPartySizeScore(MobileParty party)
	{
		return MathF.Min(1f, party.PartySizeRatio);
	}

	public override ExplainedNumber CalculateDailyCohesionChange(Army army, bool includeDescriptions = false)
	{
		ExplainedNumber cohesionChange = new ExplainedNumber(-2f, includeDescriptions);
		CalculateCohesionChangeInternal(army, ref cohesionChange);
		PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.HordeLeader, army.LeaderParty, isPrimaryBonus: false, ref cohesionChange, army.LeaderParty.IsCurrentlyAtSea);
		SiegeEvent siegeEvent = army.LeaderParty.SiegeEvent;
		if (siegeEvent != null && siegeEvent.BesiegerCamp.IsBesiegerSideParty(army.LeaderParty) && army.LeaderParty.HasPerk(DefaultPerks.Engineering.CampBuilding))
		{
			cohesionChange.AddFactor(DefaultPerks.Engineering.CampBuilding.PrimaryBonus, DefaultPerks.Engineering.CampBuilding.Name);
		}
		return cohesionChange;
	}

	private void CalculateCohesionChangeInternal(Army army, ref ExplainedNumber cohesionChange)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		foreach (MobileParty attachedParty in army.LeaderParty.AttachedParties)
		{
			if (attachedParty.Party.IsStarving)
			{
				num++;
			}
			if (attachedParty.Morale <= 25f)
			{
				num2++;
			}
			if (attachedParty.Party.NumberOfHealthyMembers <= 10)
			{
				num3++;
			}
			num4++;
		}
		float num5 = -num4;
		float num6 = 0f - (float)(num + 1) / 2f;
		float num7 = 0f - (float)(num2 + 1) / 2f;
		float num8 = 0f - (float)(num3 + 1) / 2f;
		if (army.LeaderParty != MobileParty.MainParty)
		{
			num5 *= 0.25f;
			num6 *= 0.25f;
			num7 *= 0.25f;
			num8 *= 0.25f;
		}
		cohesionChange.Add(num5, _numberOfPartiesText);
		cohesionChange.Add(num6, _numberOfStarvingPartiesText);
		cohesionChange.Add(num7, _numberOfLowMoralePartiesText);
		cohesionChange.Add(num8, _numberOfLessMemberPartiesText);
	}

	public override int CalculateNewCohesion(Army army, PartyBase newParty, int calculatedCohesion, int sign)
	{
		if (army == null)
		{
			return calculatedCohesion;
		}
		sign = MathF.Sign(sign);
		int num = ((sign == 1) ? (army.Parties.Count - 1) : army.Parties.Count);
		int num2 = (calculatedCohesion * num + 100 * sign) / (num + sign);
		if (num2 <= 100)
		{
			if (num2 >= 0)
			{
				return num2;
			}
			return 0;
		}
		return 100;
	}

	public override int GetCohesionBoostInfluenceCost(Army army, int percentageToBoost = 100)
	{
		return CalculateTotalInfluenceCostInternal(army, percentageToBoost);
	}

	private float GetInfluenceBudgetWhileCreatingArmy(MobileParty mobileParty)
	{
		return mobileParty.LeaderHero.Clan.Influence * 0.7f;
	}

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

	public override bool CanPlayerCreateArmy(out TextObject disabledReason)
	{
		if (Clan.PlayerClan.Kingdom == null)
		{
			disabledReason = new TextObject("{=XSQ0Y9gy}You need to be a part of a kingdom to create an army.");
			return false;
		}
		if (Clan.PlayerClan.IsUnderMercenaryService)
		{
			disabledReason = new TextObject("{=aRhQzJca}Mercenaries cannot create or manage armies.");
			return false;
		}
		if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
		{
			disabledReason = new TextObject("{=NAA4pajB}You need to leave your current army to create a new one.");
			return false;
		}
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			disabledReason = GameTexts.FindText("str_cannot_gather_army_at_sea");
			return false;
		}
		if (Hero.MainHero.IsPrisoner)
		{
			disabledReason = GameTexts.FindText("str_action_disabled_reason_prisoner");
			return false;
		}
		if (MobileParty.MainParty.IsInRaftState)
		{
			disabledReason = GameTexts.FindText("str_action_disabled_reason_raft_state");
			return false;
		}
		if (CampaignMission.Current != null)
		{
			disabledReason = new TextObject("{=FdzsOvDq}This action is disabled while in a mission");
			return false;
		}
		if (PlayerEncounter.Current != null)
		{
			if (PlayerEncounter.EncounterSettlement == null)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_encounter");
				return false;
			}
			Village village = PlayerEncounter.EncounterSettlement.Village;
			if (village != null && village.VillageState == Village.VillageStates.BeingRaided)
			{
				MapEvent mapEvent = MobileParty.MainParty.MapEvent;
				if (mapEvent != null && mapEvent.IsRaid)
				{
					disabledReason = GameTexts.FindText("str_action_disabled_reason_raid");
					return false;
				}
			}
			if (PlayerEncounter.EncounterSettlement.IsUnderSiege)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_siege");
				return false;
			}
		}
		else
		{
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				disabledReason = GameTexts.FindText("str_action_disabled_reason_siege");
				return false;
			}
			if (MobileParty.MainParty.MapEvent != null)
			{
				disabledReason = new TextObject("{=MIylzRc5}You can't perform this action while you are in a map event.");
				return false;
			}
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	public override bool CheckPartyEligibility(MobileParty party, out TextObject explanation)
	{
		bool result = true;
		if (PlayerSiege.PlayerSiegeEvent != null)
		{
			result = false;
			explanation = GameTexts.FindText("str_action_disabled_reason_siege");
		}
		else if (party == null)
		{
			result = false;
			explanation = new TextObject("{=f6vTzVar}Does not have a mobile party.");
		}
		else if (party.LeaderHero == Hero.MainHero.MapFaction?.Leader)
		{
			result = false;
			explanation = new TextObject("{=ipLqVv1f}You cannot invite the ruler's party to your army.");
		}
		else if (party.Army != null && party.Army != Hero.MainHero.PartyBelongedTo?.Army)
		{
			result = false;
			explanation = new TextObject("{=aROohsat}Already in another army.");
		}
		else if (party.Army != null && party.Army == Hero.MainHero.PartyBelongedTo?.Army)
		{
			result = false;
			explanation = new TextObject("{=Vq8yavES}Already in army.");
		}
		else if (party.MapEvent != null || party.SiegeEvent != null || (party.CurrentSettlement != null && party.CurrentSettlement.IsUnderSiege))
		{
			result = false;
			explanation = new TextObject("{=pkbUiKFJ}Currently fighting an enemy.");
		}
		else if (GetPartySizeScore(party) <= Campaign.Current.Models.ArmyManagementCalculationModel.PlayerMobilePartySizeRatioToCallToArmy)
		{
			result = false;
			explanation = new TextObject("{=SVJlOYCB}Party has less men than 40% of it's party size limit.");
		}
		else
		{
			if (!party.IsDisbanding)
			{
				IDisbandPartyCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
				if (campaignBehavior == null || !campaignBehavior.IsPartyWaitingForDisband(party))
				{
					float landRatio;
					if (MobileParty.MainParty.IsCurrentlyAtSea)
					{
						result = false;
						explanation = ((!party.HasNavalNavigationCapability) ? new TextObject("{=nqq84Dzq}Party cannot reach your army since it has no ships.") : new TextObject("{=gFixGQsr}You cannot call a party to your army while your party is at sea."));
					}
					else if (party.IsInRaftState)
					{
						result = false;
						explanation = new TextObject("{=TbXDmh3t}This party is lost at sea.");
					}
					else if (DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(party, MobileParty.MainParty, party.NavigationCapability, out landRatio) > Campaign.Current.Models.ArmyManagementCalculationModel.MaximumDistanceToCallToArmy)
					{
						result = false;
						explanation = new TextObject("{=UINgZDN5}You can not call a party that is far away.");
					}
					else
					{
						explanation = null;
					}
					goto IL_0201;
				}
			}
			result = false;
			explanation = new TextObject("{=tFGM0yav}This party is disbanding.");
		}
		goto IL_0201;
		IL_0201:
		return result;
	}
}
