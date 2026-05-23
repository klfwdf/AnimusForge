using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSettlementSecurityModel : SettlementSecurityModel
{
	private const float GarrisonHighSecurityGain = 3f;

	private const float GarrisonLowSecurityPenalty = -3f;

	private const float NearbyHideoutPenalty = -2f;

	private const float VillageLootedSecurityEffect = -2f;

	private const float UnderSiegeSecurityEffect = -3f;

	private const float MaxProsperityEffect = -5f;

	private const float PerProsperityEffect = -0.0005f;

	private readonly TextObject GarrisonText = GameTexts.FindText("str_garrison");

	private readonly TextObject LootedVillagesText = GameTexts.FindText("str_looted_villages");

	private readonly TextObject CorruptionText = GameTexts.FindText("str_corruption");

	private readonly TextObject NearbyHideoutText = GameTexts.FindText("str_nearby_hideout");

	private readonly TextObject UnderSiegeText = GameTexts.FindText("str_under_siege");

	private readonly TextObject ProsperityText = GameTexts.FindText("str_prosperity");

	private readonly TextObject Security = GameTexts.FindText("str_security");

	private readonly TextObject SecurityDriftText = GameTexts.FindText("str_security_drift");

	private readonly TextObject PatrolPartiesText = GameTexts.FindText("str_patrol_parties");

	public override int MaximumSecurityInSettlement => 100;

	public override int SecurityDriftMedium => 50;

	public override float MapEventSecurityEffectRadius => 50f;

	public override float HideoutClearedSecurityEffectRadius => 100f;

	public override int HideoutClearedSecurityGain => 6;

	public override int ThresholdForTaxCorruption => 50;

	public override int ThresholdForHigherTaxCorruption => 0;

	public override int ThresholdForTaxBoost => 75;

	public override int SettlementTaxBoostPercentage => 5;

	public override int SettlementTaxPenaltyPercentage => 10;

	public override int ThresholdForNotableRelationBonus => 75;

	public override int ThresholdForNotableRelationPenalty => 50;

	public override int DailyNotableRelationBonus => 1;

	public override int DailyNotableRelationPenalty => -1;

	public override int DailyNotablePowerBonus => 1;

	public override int DailyNotablePowerPenalty => -1;

	public override ExplainedNumber CalculateSecurityChange(Town town, bool includeDescriptions = false)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions);
		CalculateInfestedHideoutEffectsOnSecurity(town, ref explainedNumber);
		CalculateRaidedVillageEffectsOnSecurity(town, ref explainedNumber);
		CalculateUnderSiegeEffectsOnSecurity(town, ref explainedNumber);
		CalculateProsperityEffectOnSecurity(town, ref explainedNumber);
		CalculateGarrisonEffectsOnSecurity(town, ref explainedNumber);
		CalculatePolicyEffectsOnSecurity(town, ref explainedNumber);
		CalculateGovernorEffectsOnSecurity(town, ref explainedNumber);
		CalculateProjectEffectsOnSecurity(town, ref explainedNumber);
		CalculateIssueEffectsOnSecurity(town, ref explainedNumber);
		CalculatePerkEffectsOnSecurity(town, ref explainedNumber);
		CalculateSecurityDrift(town, ref explainedNumber);
		CalculateSettlementProjectSecurityBonuses(town, ref explainedNumber);
		CalculateSettlementPatrolPartiesBonuses(town, ref explainedNumber);
		return explainedNumber;
	}

	private void CalculateSettlementPatrolPartiesBonuses(Town town, ref ExplainedNumber result)
	{
		if (town.Settlement.PatrolParty == null)
		{
			return;
		}
		foreach (Building building in town.Buildings)
		{
			if (building.BuildingType == DefaultBuildingTypes.SettlementGuardHouse && building.CurrentLevel > 0)
			{
				result.Add((float)building.CurrentLevel * 0.5f + 0.5f, PatrolPartiesText);
				break;
			}
		}
	}

	private void CalculateSettlementProjectSecurityBonuses(Town town, ref ExplainedNumber result)
	{
		town.AddEffectOfBuildings(BuildingEffectEnum.SecurityPerDay, ref result);
	}

	private void CalculateProsperityEffectOnSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
		explainedNumber.Add(MathF.Max(-5f, -0.0005f * town.Prosperity), ProsperityText);
	}

	private void CalculateUnderSiegeEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
		if (town.Settlement.IsUnderSiege)
		{
			explainedNumber.Add(-3f, UnderSiegeText);
		}
	}

	private void CalculateRaidedVillageEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
		float num = 0f;
		foreach (Village boundVillage in town.Settlement.BoundVillages)
		{
			if (boundVillage.VillageState == Village.VillageStates.Looted)
			{
				num += -2f;
				break;
			}
		}
		explainedNumber.Add(num, LootedVillagesText);
	}

	private void CalculateInfestedHideoutEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
		float num = Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay * 0.5f;
		foreach (Hideout item in Hideout.All)
		{
			if (item.IsInfested && Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, item.Settlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) < num)
			{
				explainedNumber.Add(-2f, NearbyHideoutText);
				break;
			}
		}
	}

	private void CalculateSecurityDrift(Town town, ref ExplainedNumber explainedNumber)
	{
		explainedNumber.Add(-1f * (town.Security - (float)SecurityDriftMedium) / 15f, SecurityDriftText);
	}

	private void CalculatePolicyEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
		Kingdom kingdom = town.Settlement.OwnerClan.Kingdom;
		if (kingdom == null)
		{
			return;
		}
		if (town.IsTown)
		{
			if (kingdom.ActivePolicies.Contains(DefaultPolicies.Bailiffs))
			{
				explainedNumber.Add(1f, DefaultPolicies.Bailiffs.Name);
			}
			if (kingdom.ActivePolicies.Contains(DefaultPolicies.Serfdom))
			{
				explainedNumber.Add(1f, DefaultPolicies.Serfdom.Name);
			}
			if (kingdom.ActivePolicies.Contains(DefaultPolicies.Magistrates))
			{
				explainedNumber.Add(1f, DefaultPolicies.Magistrates.Name);
			}
		}
		if (kingdom.ActivePolicies.Contains(DefaultPolicies.TrialByJury))
		{
			explainedNumber.Add(-0.2f, DefaultPolicies.TrialByJury.Name);
		}
	}

	private void CalculateGovernorEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
	}

	private void CalculateGarrisonEffectsOnSecurity(Town town, ref ExplainedNumber result)
	{
		if (town.GarrisonParty != null && town.GarrisonParty.MemberRoster.Count != 0 && town.GarrisonParty.MemberRoster.TotalHealthyCount != 0)
		{
			ExplainedNumber bonuses = new ExplainedNumber(0.01f);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.OneHanded.StandUnited, town, ref bonuses);
			CalculateStrengthOfGarrisonParty(town.GarrisonParty.Party, out var totalStrength, out var archerStrength, out var cavalryStrength);
			float num = totalStrength * bonuses.ResultNumber;
			result.Add(num, GarrisonText);
			if (PerkHelper.GetPerkValueForTown(DefaultPerks.Leadership.Authority, town))
			{
				result.Add(num * DefaultPerks.Leadership.Authority.PrimaryBonus, DefaultPerks.Leadership.Authority.Name);
			}
			if (PerkHelper.GetPerkValueForTown(DefaultPerks.Riding.ReliefForce, town))
			{
				float num2 = cavalryStrength / totalStrength;
				result.Add(num * num2 * DefaultPerks.Riding.ReliefForce.SecondaryBonus, DefaultPerks.Riding.ReliefForce.Name);
			}
			float num3 = archerStrength / totalStrength;
			if (PerkHelper.GetPerkValueForTown(DefaultPerks.Bow.MountedArchery, town))
			{
				result.Add(num * num3 * DefaultPerks.Bow.MountedArchery.SecondaryBonus, DefaultPerks.Bow.MountedArchery.Name);
			}
			if (PerkHelper.GetPerkValueForTown(DefaultPerks.Bow.RangersSwiftness, town))
			{
				result.Add(num * num3 * DefaultPerks.Bow.RangersSwiftness.SecondaryBonus, DefaultPerks.Bow.RangersSwiftness.Name);
			}
			if (PerkHelper.GetPerkValueForTown(DefaultPerks.Crossbow.RenownMarksmen, town))
			{
				result.Add(num * num3 * DefaultPerks.Crossbow.RenownMarksmen.SecondaryBonus, DefaultPerks.Crossbow.RenownMarksmen.Name);
			}
		}
	}

	private void CalculateStrengthOfGarrisonParty(PartyBase party, out float totalStrength, out float archerStrength, out float cavalryStrength)
	{
		totalStrength = 0f;
		archerStrength = 0f;
		cavalryStrength = 0f;
		float leaderModifier = 0f;
		MapEvent.PowerCalculationContext context = MapEvent.PowerCalculationContext.Siege;
		BattleSideEnum side = BattleSideEnum.Defender;
		if (party.MapEvent != null)
		{
			side = party.Side;
			leaderModifier = party.LeaderHero?.PowerModifier ?? 0f;
			context = party.MapEvent.SimulationContext;
		}
		for (int i = 0; i < party.MemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(i);
			if (elementCopyAtIndex.Character != null)
			{
				float troopPower = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(elementCopyAtIndex.Character, side, context, leaderModifier);
				float num = (float)(elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber) * troopPower;
				if (elementCopyAtIndex.Character.IsMounted)
				{
					cavalryStrength += num;
				}
				if (elementCopyAtIndex.Character.IsRanged)
				{
					archerStrength += num;
				}
				totalStrength += num;
			}
		}
	}

	private void CalculatePerkEffectsOnSecurity(Town town, ref ExplainedNumber result)
	{
		float num = (float)town.Settlement.Parties.Where(delegate(MobileParty x)
		{
			Clan actualClan = x.ActualClan;
			return actualClan != null && !actualClan.IsAtWarWith(town.MapFaction) && (x.LeaderHero?.GetPerkValue(DefaultPerks.Leadership.Presence) ?? false);
		}).Count() * DefaultPerks.Leadership.Presence.PrimaryBonus;
		if (num > 0f)
		{
			result.Add(num, DefaultPerks.Leadership.Presence.Name);
		}
		if (town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Roguery.KnowHow))
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Roguery.KnowHow, town, ref result);
		}
		PerkHelper.AddPerkBonusForTown(DefaultPerks.OneHanded.ToBeBlunt, town, ref result);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Throwing.Focus, town, ref result);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Polearm.Skewer, town, ref result);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Tactics.Gensdarmes, town, ref result);
	}

	private void CalculateProjectEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
	}

	private void CalculateIssueEffectsOnSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
		Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(DefaultIssueEffects.SettlementSecurity, town.Settlement, ref explainedNumber);
	}

	public override float GetLootedNearbyPartySecurityEffect(Town town, float sumOfAttackedPartyStrengths)
	{
		return -1f * sumOfAttackedPartyStrengths * 0.005f;
	}

	public override float GetNearbyBanditPartyDefeatedSecurityEffect(Town town, float sumOfAttackedPartyStrengths)
	{
		return sumOfAttackedPartyStrengths * 0.005f;
	}

	public override void CalculateGoldGainDueToHighSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
		float num = MBMath.Map(town.Security, ThresholdForTaxBoost, MaximumSecurityInSettlement, 0f, SettlementTaxBoostPercentage);
		explainedNumber.AddFactor(num * 0.01f, Security);
	}

	public override void CalculateGoldCutDueToLowSecurity(Town town, ref ExplainedNumber explainedNumber)
	{
		float num = MBMath.Map(town.Security, ThresholdForHigherTaxCorruption, ThresholdForTaxCorruption, SettlementTaxPenaltyPercentage, 0f);
		explainedNumber.AddFactor(-1f * num * 0.01f, CorruptionText);
	}
}
