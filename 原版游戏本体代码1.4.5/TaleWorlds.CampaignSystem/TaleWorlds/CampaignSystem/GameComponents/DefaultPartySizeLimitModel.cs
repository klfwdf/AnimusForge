using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartySizeLimitModel : PartySizeLimitModel
{
	private enum LimitType
	{
		MobilePartySizeLimit,
		GarrisonPartySizeLimit,
		PrisonerSizeLimit
	}

	private const int BaseMobilePartySize = 20;

	private const int BaseMobilePartyPrisonerSize = 10;

	private const int BaseSettlementPrisonerSize = 60;

	private const int SettlementPrisonerSizeBonusPerWallLevel = 40;

	private const int BaseGarrisonPartySize = 200;

	private const int BasePatrolPartySize = 10;

	private const int TownGarrisonSizeBonus = 200;

	private const int AdditionalPartySizeForCheat = 5000;

	private const int OneVillagerPerHearth = 40;

	private const int AdditionalPartySizeLimitPerTier = 15;

	private const int AdditionalPartySizeLimitForLeaderPerTier = 25;

	private readonly TextObject _leadershipSkillLevelBonusText = GameTexts.FindText("str_leadership_skill_level_bonus");

	private readonly TextObject _leadershipPerkUltimateLeaderBonusText = GameTexts.FindText("str_leadership_perk_bonus");

	private readonly TextObject _wallLevelBonusText = GameTexts.FindText("str_map_tooltip_wall_level");

	private readonly TextObject _baseSizeText = GameTexts.FindText("str_base_size");

	private readonly TextObject _clanTierText = GameTexts.FindText("str_clan_tier_bonus");

	private readonly TextObject _renownText = GameTexts.FindText("str_renown_bonus");

	private readonly TextObject _clanLeaderText = GameTexts.FindText("str_clan_leader_bonus");

	private readonly TextObject _factionLeaderText = GameTexts.FindText("str_faction_leader_bonus");

	private readonly TextObject _leaderLevelText = GameTexts.FindText("str_leader_level_bonus");

	private readonly TextObject _townBonusText = GameTexts.FindText("str_town_bonus");

	private readonly TextObject _minorFactionText = GameTexts.FindText("str_minor_faction_bonus");

	private readonly TextObject _currentPartySizeBonusText = GameTexts.FindText("str_current_party_size_bonus");

	private readonly TextObject _randomSizeBonusTemporary = new TextObject("{=hynFV8jC}Extra size bonus (Perk-like Effect)");

	private static bool _addAdditionalPartySizeAsCheat;

	private static bool _addAdditionalPrisonerSizeAsCheat;

	public override int MinimumNumberOfVillagersAtVillagerParty => 12;

	public override ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool includeDescriptions = false)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions);
		if (party.IsMobile)
		{
			if (party.MobileParty.IsGarrison)
			{
				return CalculateGarrisonPartySizeLimit(party.MobileParty.GarrisonPartyComponent.Settlement, includeDescriptions);
			}
			if (party.MobileParty.IsPatrolParty)
			{
				return CalculatePatrolPartySizeLimit(party.MobileParty, includeDescriptions);
			}
			return CalculateMobilePartyMemberSizeLimit(party.MobileParty, includeDescriptions);
		}
		return result;
	}

	private ExplainedNumber CalculatePatrolPartySizeLimit(MobileParty mobileParty, bool includeDescriptions)
	{
		new ExplainedNumber(10f, includeDescriptions);
		foreach (Building building in mobileParty.HomeSettlement.Town.Buildings)
		{
			if (building.BuildingType == DefaultBuildingTypes.SettlementGuardHouse)
			{
				return new ExplainedNumber(GetPatrolPartySizeLimitFromGuardHouseLevel(building.CurrentLevel), includeDescriptions);
			}
		}
		return new ExplainedNumber(0f, includeDescriptions);
	}

	private int GetPatrolPartySizeLimitFromGuardHouseLevel(int level)
	{
		return 10 + 5 * level;
	}

	public override ExplainedNumber GetPartyPrisonerSizeLimit(PartyBase party, bool includeDescriptions = false)
	{
		if (party.IsSettlement)
		{
			return CalculateSettlementPartyPrisonerSizeLimitInternal(party.Settlement, includeDescriptions);
		}
		return CalculateMobilePartyPrisonerSizeLimitInternal(party, includeDescriptions);
	}

	private ExplainedNumber CalculateMobilePartyMemberSizeLimit(MobileParty party, bool includeDescriptions = false)
	{
		ExplainedNumber result = new ExplainedNumber(20f, includeDescriptions, _baseSizeText);
		if (party.LeaderHero != null && party.LeaderHero.Clan != null && !party.IsCaravan)
		{
			CalculateBaseMemberSize(party.LeaderHero, party.MapFaction, party.ActualClan, ref result);
			SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.StewardPartySizeBonus, party, ref result);
			if (_addAdditionalPartySizeAsCheat && party.IsMainParty && Game.Current.CheatMode)
			{
				result.Add(5000f, new TextObject("{=!}Additional size from extra party cheat"));
			}
		}
		else if (party.IsCaravan)
		{
			if (party.Party.Owner == Hero.MainHero)
			{
				int num = (party.CaravanPartyComponent.IsElite ? 30 : 10);
				if (party.CaravanPartyComponent.CanHaveNavalNavigationCapability)
				{
					num = (party.CaravanPartyComponent.IsElite ? 46 : 33);
				}
				result.Add(num, _randomSizeBonusTemporary);
			}
			else
			{
				Hero owner = party.Party.Owner;
				if (owner != null && owner.IsNotable)
				{
					result.Add(10 * ((party.Party.Owner.Power < 100f) ? 1 : ((party.Party.Owner.Power < 200f) ? 2 : 3)), _randomSizeBonusTemporary);
				}
			}
		}
		else if (party.IsVillager)
		{
			result.Add(40f, _randomSizeBonusTemporary);
		}
		if (party.IsCurrentlyAtSea)
		{
			foreach (Ship ship in party.Ships)
			{
				result.AddFactor(ship.CrewCapacityBonusFactor, ship.Name);
			}
		}
		return result;
	}

	public override ExplainedNumber CalculateGarrisonPartySizeLimit(Settlement settlement, bool includeDescriptions = false)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(200f, includeDescriptions, _baseSizeText);
		SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.LeadershipGarrisonSizeBonus, settlement.OwnerClan.Leader.CharacterObject, ref explainedNumber);
		if (settlement.IsTown)
		{
			explainedNumber.Add(200f, _townBonusText);
		}
		AddGarrisonOwnerPerkEffects(settlement, ref explainedNumber);
		AddSettlementProjectBonuses(settlement, ref explainedNumber);
		return explainedNumber;
	}

	private ExplainedNumber CalculateSettlementPartyPrisonerSizeLimitInternal(Settlement settlement, bool includeDescriptions = false)
	{
		ExplainedNumber result = new ExplainedNumber(60f, includeDescriptions, _baseSizeText);
		int num = settlement.Town?.GetWallLevel() ?? 0;
		if (num > 0)
		{
			result.Add(num * 40, _wallLevelBonusText);
		}
		AddSettlementProjectPrisonerBonuses(settlement, ref result);
		return result;
	}

	private ExplainedNumber CalculateMobilePartyPrisonerSizeLimitInternal(PartyBase party, bool includeDescriptions = false)
	{
		ExplainedNumber result = new ExplainedNumber(10f, includeDescriptions, _baseSizeText);
		result.Add(GetCurrentPartySizeEffect(party), _currentPartySizeBonusText);
		AddMobilePartyLeaderPrisonerSizePerkEffects(party, ref result);
		if (_addAdditionalPrisonerSizeAsCheat && party.IsMobile && party.MobileParty.IsMainParty && Game.Current.CheatMode)
		{
			result.Add(5000f, new TextObject("{=!}Additional size from extra prisoner cheat"));
		}
		return result;
	}

	private void AddMobilePartyLeaderPrisonerSizePerkEffects(PartyBase party, ref ExplainedNumber result)
	{
		if (party.LeaderHero != null)
		{
			if (party.LeaderHero.GetPerkValue(DefaultPerks.TwoHanded.Terror))
			{
				result.Add(DefaultPerks.TwoHanded.Terror.SecondaryBonus, DefaultPerks.TwoHanded.Terror.Name);
			}
			if (!party.MobileParty.IsCurrentlyAtSea && party.LeaderHero.GetPerkValue(DefaultPerks.Athletics.Stamina))
			{
				result.Add(DefaultPerks.Athletics.Stamina.SecondaryBonus, DefaultPerks.Athletics.Stamina.Name);
			}
			if (party.LeaderHero.GetPerkValue(DefaultPerks.Roguery.Manhunter))
			{
				result.Add(DefaultPerks.Roguery.Manhunter.SecondaryBonus, DefaultPerks.Roguery.Manhunter.Name);
			}
			if (party.LeaderHero != null && party.LeaderHero.GetPerkValue(DefaultPerks.Scouting.VantagePoint))
			{
				result.Add(DefaultPerks.Scouting.VantagePoint.SecondaryBonus, DefaultPerks.Scouting.VantagePoint.Name);
			}
		}
	}

	private void AddGarrisonOwnerPerkEffects(Settlement currentSettlement, ref ExplainedNumber result)
	{
		if (currentSettlement != null && currentSettlement.IsFortification)
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.OneHanded.CorpsACorps, currentSettlement.Town, ref result);
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Leadership.VeteransRespect, currentSettlement.Town, ref result);
		}
	}

	public override int GetNextClanTierPartySizeEffectChangeForHero(Hero hero)
	{
		int tierEffectInternal = GetTierEffectInternal(hero.Clan.Tier, hero.Clan.Leader == hero);
		return GetTierEffectInternal(hero.Clan.Tier + 1, hero.Clan.Leader == hero) - tierEffectInternal;
	}

	private int GetTierEffectInternal(int tier, bool isHeroClanLeader)
	{
		if (tier >= 1)
		{
			if (isHeroClanLeader)
			{
				return 25 * tier;
			}
			return 15 * tier;
		}
		return 0;
	}

	public override int GetAssumedPartySizeForLordParty(Hero leaderHero, IFaction partyMapFaction, Clan actualClan)
	{
		ExplainedNumber result = new ExplainedNumber(20f, includeDescriptions: false, _baseSizeText);
		if (leaderHero != null && leaderHero.Clan != null)
		{
			CalculateBaseMemberSize(leaderHero, partyMapFaction, actualClan, ref result);
			SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.StewardPartySizeBonus, ref result, leaderHero.GetSkillValue(DefaultSkills.Steward));
		}
		return (int)result.ResultNumber;
	}

	public override int GetClanTierPartySizeEffectForHero(Hero hero)
	{
		return GetTierEffectInternal(hero.Clan.Tier, hero.Clan.Leader == hero);
	}

	private void AddSettlementProjectBonuses(Settlement settlement, ref ExplainedNumber result)
	{
		if (settlement != null && settlement.IsFortification)
		{
			settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.GarrisonCapacity, ref result);
		}
	}

	private void AddSettlementProjectPrisonerBonuses(Settlement settlement, ref ExplainedNumber result)
	{
		if (settlement != null && settlement.IsFortification)
		{
			settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.PrisonCapacity, ref result);
		}
	}

	private int GetCurrentPartySizeEffect(PartyBase party)
	{
		return party.NumberOfHealthyMembers / 2;
	}

	private void CalculateBaseMemberSize(Hero partyLeader, IFaction partyMapFaction, Clan actualClan, ref ExplainedNumber result)
	{
		if (partyMapFaction != null && partyMapFaction.IsKingdomFaction && partyLeader.MapFaction.Leader == partyLeader)
		{
			result.Add(20f, _factionLeaderText);
		}
		if (partyLeader.GetPerkValue(DefaultPerks.OneHanded.Prestige))
		{
			result.Add(DefaultPerks.OneHanded.Prestige.SecondaryBonus, DefaultPerks.OneHanded.Prestige.Name);
		}
		if (partyLeader.GetPerkValue(DefaultPerks.TwoHanded.Hope))
		{
			result.Add(DefaultPerks.TwoHanded.Hope.SecondaryBonus, DefaultPerks.TwoHanded.Hope.Name);
		}
		if (partyLeader.GetPerkValue(DefaultPerks.Athletics.ImposingStature))
		{
			result.Add(DefaultPerks.Athletics.ImposingStature.SecondaryBonus, DefaultPerks.Athletics.ImposingStature.Name);
		}
		if (partyLeader.GetPerkValue(DefaultPerks.Bow.MerryMen))
		{
			result.Add(DefaultPerks.Bow.MerryMen.PrimaryBonus, DefaultPerks.Bow.MerryMen.Name);
		}
		if (partyLeader.GetPerkValue(DefaultPerks.Tactics.HordeLeader))
		{
			result.Add(DefaultPerks.Tactics.HordeLeader.PrimaryBonus, DefaultPerks.Tactics.HordeLeader.Name);
		}
		if (partyLeader.GetPerkValue(DefaultPerks.Scouting.MountedScouts))
		{
			result.Add(DefaultPerks.Scouting.MountedScouts.SecondaryBonus, DefaultPerks.Scouting.MountedScouts.Name);
		}
		if (partyLeader.GetPerkValue(DefaultPerks.Leadership.Authority))
		{
			result.Add(DefaultPerks.Leadership.Authority.SecondaryBonus, DefaultPerks.Leadership.Authority.Name);
		}
		if (partyLeader.GetPerkValue(DefaultPerks.Leadership.UpliftingSpirit))
		{
			result.Add(DefaultPerks.Leadership.UpliftingSpirit.SecondaryBonus, DefaultPerks.Leadership.UpliftingSpirit.Name);
		}
		if (partyLeader.GetPerkValue(DefaultPerks.Leadership.TalentMagnet))
		{
			result.Add(DefaultPerks.Leadership.TalentMagnet.PrimaryBonus, DefaultPerks.Leadership.TalentMagnet.Name);
		}
		if (partyLeader.GetSkillValue(DefaultSkills.Leadership) > Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus && partyLeader.GetPerkValue(DefaultPerks.Leadership.UltimateLeader))
		{
			int num = partyLeader.GetSkillValue(DefaultSkills.Leadership) - Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus;
			result.Add((float)num * DefaultPerks.Leadership.UltimateLeader.PrimaryBonus, _leadershipPerkUltimateLeaderBonusText);
		}
		if (actualClan != null && actualClan.Leader?.GetPerkValue(DefaultPerks.Leadership.LeaderOfMasses) == true)
		{
			int num2 = 0;
			foreach (Settlement settlement in actualClan.Settlements)
			{
				if (settlement.IsTown)
				{
					num2++;
				}
			}
			float num3 = (float)num2 * DefaultPerks.Leadership.LeaderOfMasses.PrimaryBonus;
			if (num3 > 0f)
			{
				result.Add(num3, DefaultPerks.Leadership.LeaderOfMasses.Name);
			}
		}
		if (partyLeader.Clan.Leader == partyLeader)
		{
			if (partyLeader.Clan.Tier >= 5 && partyMapFaction.IsKingdomFaction && ((Kingdom)partyMapFaction).ActivePolicies.Contains(DefaultPolicies.NobleRetinues))
			{
				result.Add(40f, DefaultPolicies.NobleRetinues.Name);
			}
			if (partyMapFaction.IsKingdomFaction && partyMapFaction.Leader == partyLeader && ((Kingdom)partyMapFaction).ActivePolicies.Contains(DefaultPolicies.RoyalGuard))
			{
				result.Add(60f, DefaultPolicies.RoyalGuard.Name);
			}
		}
		result.Add(Campaign.Current.Models.PartySizeLimitModel.GetClanTierPartySizeEffectForHero(partyLeader), _clanTierText);
	}

	private float GetPartySizeRatioForSize(PartyTemplateObject partyTemplate, int desiredSize)
	{
		float num = 0f;
		int num2 = partyTemplate.Stacks.Sum((PartyTemplateStack s) => s.MinValue);
		int num3 = partyTemplate.Stacks.Sum((PartyTemplateStack s) => s.MaxValue);
		if (desiredSize < num2)
		{
			return (float)desiredSize / (float)num2 - 1f;
		}
		if (num2 <= desiredSize && desiredSize <= num3)
		{
			return (float)(desiredSize - num2) / (float)(num3 - num2);
		}
		return (float)desiredSize / (float)num3;
	}

	private float GetInitialPartySizeRatioForMobileParty(MobileParty party, PartyTemplateObject partyTemplate)
	{
		float num = 0f;
		if (party.IsBandit)
		{
			if (!partyTemplate.ShipHulls.IsEmpty())
			{
				return (MBRandom.RandomFloat < 0.4f) ? MBRandom.RandomFloatRanged(0f, 0.33f) : MBRandom.RandomFloatRanged(0.66f, 1f);
			}
			float playerProgress = Campaign.Current.PlayerProgress;
			float num2 = 0.4f + 0.8f * playerProgress;
			float num3 = MBRandom.RandomFloatRanged(0.2f, 0.8f);
			return num2 * num3;
		}
		if (party.IsCaravan && party.Owner == Hero.MainHero)
		{
			return 1f;
		}
		if (party.IsPatrolParty)
		{
			return 1f;
		}
		return party.RandomFloat();
	}

	public override int GetIdealVillagerPartySize(Village village)
	{
		float num = 0f;
		foreach (var production in village.VillageType.Productions)
		{
			float resultNumber = Campaign.Current.Models.VillageProductionCalculatorModel.CalculateDailyProductionAmount(village, production.Item1).ResultNumber;
			num += resultNumber;
		}
		float num2 = ((num > 10f) ? (40f * (1f - (TaleWorlds.Library.MathF.Min(40f, num) - 10f) / 60f)) : 40f);
		return MinimumNumberOfVillagersAtVillagerParty + (int)(village.Hearth / num2);
	}

	public override TroopRoster FindAppropriateInitialRosterForMobileParty(MobileParty party, PartyTemplateObject partyTemplate)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		float initialPartySizeRatioForMobileParty = GetInitialPartySizeRatioForMobileParty(party, partyTemplate);
		for (int i = 0; i < partyTemplate.Stacks.Count; i++)
		{
			int minValue = partyTemplate.Stacks[i].MinValue;
			int maxValue = partyTemplate.Stacks[i].MaxValue;
			int num = minValue;
			if (initialPartySizeRatioForMobileParty <= 0f)
			{
				num = minValue;
			}
			else if (initialPartySizeRatioForMobileParty <= 1f)
			{
				num = MBRandom.RoundRandomized((float)minValue + (float)(maxValue - minValue) * initialPartySizeRatioForMobileParty);
			}
			else
			{
				Debug.FailedAssert("initialPartySizeRatio should not be above 1", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultPartySizeLimitModel.cs", "FindAppropriateInitialRosterForMobileParty", 538);
				num = maxValue;
			}
			if (party.IsVillager)
			{
				Village village = party.VillagerPartyComponent.Village;
				if (village.Bound?.Town?.Governor != null && village.Bound.Town.Governor.GetPerkValue(DefaultPerks.Scouting.VillageNetwork))
				{
					num = TaleWorlds.Library.MathF.Round((float)num * (1f + DefaultPerks.Scouting.VillageNetwork.SecondaryBonus));
				}
			}
			if (num > 0)
			{
				CharacterObject character = partyTemplate.Stacks[i].Character;
				troopRoster.AddToCounts(character, num);
			}
		}
		return troopRoster;
	}

	public override List<Ship> FindAppropriateInitialShipsForMobileParty(MobileParty party, PartyTemplateObject partyTemplate)
	{
		List<Ship> list = new List<Ship>();
		float initialPartySizeRatioForMobileParty = GetInitialPartySizeRatioForMobileParty(party, partyTemplate);
		if (partyTemplate.ShipHulls != null && partyTemplate.ShipHulls.Count > 0)
		{
			foreach (ShipTemplateStack shipHull in partyTemplate.ShipHulls)
			{
				int minValue = shipHull.MinValue;
				int maxValue = shipHull.MaxValue;
				int num = minValue;
				num = ((initialPartySizeRatioForMobileParty <= 0f) ? MBRandom.RoundRandomized(Math.Max(0f, (float)minValue + (float)minValue * initialPartySizeRatioForMobileParty)) : ((!(initialPartySizeRatioForMobileParty <= 1f)) ? MBRandom.RoundRandomized((float)maxValue * initialPartySizeRatioForMobileParty) : MBRandom.RoundRandomized((float)minValue + (float)(maxValue - minValue) * initialPartySizeRatioForMobileParty)));
				for (int i = 0; i < num; i++)
				{
					list.Add(new Ship(shipHull.ShipHull));
				}
			}
		}
		return list;
	}
}
