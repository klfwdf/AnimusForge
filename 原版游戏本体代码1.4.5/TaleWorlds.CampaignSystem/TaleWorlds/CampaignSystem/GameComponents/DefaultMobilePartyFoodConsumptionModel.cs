using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultMobilePartyFoodConsumptionModel : MobilePartyFoodConsumptionModel
{
	private const float MinimumDailyFoodConsumption = 0.01f;

	private static readonly TextObject _partyConsumption = new TextObject("{=UrFzdy4z}Daily Consumption");

	public override int NumberOfMenOnMapToEatOneFood => 20;

	public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party, bool includeDescription = false)
	{
		int num = party.Party.NumberOfAllMembers + party.Party.NumberOfPrisoners / 2;
		num = ((num < 1) ? 1 : num);
		return new ExplainedNumber((0f - (float)num) / (float)NumberOfMenOnMapToEatOneFood, includeDescription);
	}

	public override ExplainedNumber CalculateDailyFoodConsumptionf(MobileParty party, ExplainedNumber baseConsumption)
	{
		CalculatePerkEffects(party, ref baseConsumption);
		baseConsumption.LimitMax(-0.01f);
		return baseConsumption;
	}

	private void CalculatePerkEffects(MobileParty party, ref ExplainedNumber result)
	{
		int num = 0;
		for (int i = 0; i < party.MemberRoster.Count; i++)
		{
			if (party.MemberRoster.GetCharacterAtIndex(i).Culture.IsBandit)
			{
				num += party.MemberRoster.GetElementNumber(i);
			}
		}
		for (int j = 0; j < party.PrisonRoster.Count; j++)
		{
			if (party.PrisonRoster.GetCharacterAtIndex(j).Culture.IsBandit)
			{
				num += party.PrisonRoster.GetElementNumber(j);
			}
		}
		if (party.LeaderHero != null && party.LeaderHero.GetPerkValue(DefaultPerks.Roguery.Promises) && num > 0)
		{
			float value = (float)num / (float)party.MemberRoster.TotalManCount * DefaultPerks.Roguery.Promises.PrimaryBonus;
			result.AddFactor(value, DefaultPerks.Roguery.Promises.Name);
		}
		PerkHelper.AddPerkBonusForParty(DefaultPerks.Athletics.Spartan, party, isPrimaryBonus: false, ref result, party.IsCurrentlyAtSea);
		if (!party.IsCurrentlyAtSea)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.WarriorsDiet, party, isPrimaryBonus: true, ref result);
		}
		if (party.EffectiveQuartermaster != null)
		{
			PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Steward.PriceOfLoyalty, party.EffectiveQuartermaster.CharacterObject, DefaultSkills.Steward, applyPrimaryBonus: true, ref result, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
		}
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
		if (faceTerrainType == TerrainType.Forest || faceTerrainType == TerrainType.Steppe)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Foragers, party, isPrimaryBonus: true, ref result);
		}
		if (party.IsGarrison && party.CurrentSettlement != null && party.CurrentSettlement.Town.IsUnderSiege)
		{
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Athletics.StrongLegs, party.CurrentSettlement.Town, ref result);
		}
		if (party.Army != null)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.StiffUpperLip, party, isPrimaryBonus: true, ref result, party.IsCurrentlyAtSea);
		}
		if (party.SiegeEvent?.BesiegerCamp != null)
		{
			if (party.HasPerk(DefaultPerks.Steward.SoundReserves, checkSecondaryRole: true))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.SoundReserves, party, isPrimaryBonus: false, ref result);
			}
			if (party.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(party.Party) && party.HasPerk(DefaultPerks.Steward.MasterOfPlanning))
			{
				result.AddFactor(DefaultPerks.Steward.MasterOfPlanning.PrimaryBonus, DefaultPerks.Steward.MasterOfPlanning.Name);
			}
		}
	}

	public override bool DoesPartyConsumeFood(MobileParty mobileParty)
	{
		if (mobileParty.IsActive && (mobileParty.LeaderHero == null || mobileParty.LeaderHero.IsLord || mobileParty.LeaderHero.Clan == Clan.PlayerClan || mobileParty.LeaderHero.IsMinorFactionHero) && !mobileParty.IsGarrison && !mobileParty.IsCaravan && !mobileParty.IsBandit && !mobileParty.IsMilitia && !mobileParty.IsPatrolParty)
		{
			return !mobileParty.IsVillager;
		}
		return false;
	}
}
