using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyWageModel : PartyWageModel
{
	private readonly TextObject _cultureText = GameTexts.FindText("str_culture");

	private readonly TextObject _buildingEffects = GameTexts.FindText("str_building_effects");

	private const float MercenaryWageFactor = 1.5f;

	public override int MaxWagePaymentLimit => 10000;

	public override int GetCharacterWage(CharacterObject character)
	{
		int num = character.Tier switch
		{
			0 => 1, 
			1 => 2, 
			2 => 3, 
			3 => 5, 
			4 => 8, 
			5 => 12, 
			6 => 17, 
			_ => 23, 
		};
		if (character.Occupation == Occupation.Mercenary)
		{
			num = (int)((float)num * 1.5f);
		}
		return num;
	}

	public override ExplainedNumber GetTotalWage(MobileParty mobileParty, TroopRoster troopRoster, bool includeDescriptions = false)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		bool flag = !mobileParty.HasPerk(DefaultPerks.Steward.AidCorps);
		int num7 = 0;
		int num8 = 0;
		for (int i = 0; i < troopRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = troopRoster.GetElementCopyAtIndex(i);
			CharacterObject character = elementCopyAtIndex.Character;
			if (!flag)
			{
				_ = elementCopyAtIndex.Number;
				_ = elementCopyAtIndex.WoundedNumber;
			}
			else
			{
				_ = elementCopyAtIndex.Number;
			}
			if (character.IsHero)
			{
				bool flag2 = mobileParty.IsMainParty && character.HeroObject.Clan == Clan.PlayerClan && character.HeroObject.Occupation == Occupation.Lord;
				if (elementCopyAtIndex.Character.HeroObject != character.HeroObject.Clan?.Leader && !flag2)
				{
					num = ((mobileParty.LeaderHero == null || !mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Steward.PaidInPromise)) ? (num + character.TroopWage) : (num + MathF.Round((float)character.TroopWage * (1f + DefaultPerks.Steward.PaidInPromise.PrimaryBonus))));
				}
				continue;
			}
			int num9 = character.TroopWage * elementCopyAtIndex.Number;
			num += num9;
			if (character.Culture.IsBandit)
			{
				num6 += num9;
			}
			if (character.IsInfantry)
			{
				num2 += num9;
			}
			if (character.IsMounted)
			{
				num3 += num9;
			}
			if (character.Occupation == Occupation.CaravanGuard)
			{
				num7 += num9;
			}
			if (character.Occupation == Occupation.Mercenary)
			{
				num8 += num9;
			}
			if (character.IsRanged)
			{
				num4 += num9;
				if (character.Tier >= 4)
				{
					num5 += num9;
				}
			}
		}
		if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Roguery.DeepPockets))
		{
			num -= num6;
			ExplainedNumber bonuses = new ExplainedNumber(num6);
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.DeepPockets, mobileParty.LeaderHero.CharacterObject, isPrimaryBonus: false, ref bonuses);
			num += (int)bonuses.ResultNumber;
		}
		if (num5 > 0)
		{
			num -= num5;
			ExplainedNumber stat = new ExplainedNumber(num5);
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Crossbow.PickedShots, mobileParty, isPrimaryBonus: true, ref stat, mobileParty.IsCurrentlyAtSea);
			num += (int)stat.ResultNumber;
		}
		ExplainedNumber bonuses2 = new ExplainedNumber(num, includeDescriptions);
		bonuses2.LimitMin(0f);
		ExplainedNumber result = new ExplainedNumber(1f);
		if (mobileParty.IsGarrison && mobileParty.CurrentSettlement?.Town != null)
		{
			if (mobileParty.CurrentSettlement.IsFortification)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.OneHanded.MilitaryTradition, mobileParty.CurrentSettlement.Town, ref bonuses2);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.TwoHanded.Berserker, mobileParty.CurrentSettlement.Town, ref bonuses2);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.DrillSergant, mobileParty.CurrentSettlement.Town, ref bonuses2);
				float troopRatio = (float)num2 / bonuses2.BaseNumber;
				CalculatePartialGarrisonWageReduction(troopRatio, mobileParty, DefaultPerks.Polearm.StandardBearer, ref bonuses2, isSecondaryEffect: true);
				float troopRatio2 = (float)num4 / bonuses2.BaseNumber;
				CalculatePartialGarrisonWageReduction(troopRatio2, mobileParty, DefaultPerks.Crossbow.PeasantLeader, ref bonuses2, isSecondaryEffect: true);
				float troopRatio3 = (float)num3 / bonuses2.BaseNumber;
				CalculatePartialGarrisonWageReduction(troopRatio3, mobileParty, DefaultPerks.Riding.CavalryTactics, ref bonuses2, isSecondaryEffect: true);
			}
			if (mobileParty.CurrentSettlement.IsCastle)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Bow.HunterClan, mobileParty.CurrentSettlement.Town, ref bonuses2);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.StiffUpperLip, mobileParty.CurrentSettlement.Town, ref bonuses2);
			}
			if (mobileParty.CurrentSettlement.Owner.Culture.HasFeat(DefaultCulturalFeats.EmpireGarrisonWageFeat))
			{
				bonuses2.AddFactor(DefaultCulturalFeats.EmpireGarrisonWageFeat.EffectBonus, _cultureText);
			}
			mobileParty.CurrentSettlement.Town.AddEffectOfBuildings(BuildingEffectEnum.GarrisonWageReduction, ref result);
		}
		float value = ((mobileParty.LeaderHero != null && mobileParty.LeaderHero.Clan.Kingdom != null && !mobileParty.LeaderHero.Clan.IsUnderMercenaryService && mobileParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.MilitaryCoronae)) ? 0.1f : 0f);
		if (mobileParty.HasPerk(DefaultPerks.Trade.SwordForBarter, checkSecondaryRole: true))
		{
			float num10 = (float)num7 / bonuses2.BaseNumber;
			if (num10 > 0f)
			{
				float value2 = DefaultPerks.Trade.SwordForBarter.SecondaryBonus * num10;
				bonuses2.AddFactor(value2, DefaultPerks.Trade.SwordForBarter.Name);
			}
		}
		if (mobileParty.HasPerk(DefaultPerks.Steward.Contractors))
		{
			float num11 = (float)num8 / bonuses2.BaseNumber;
			if (num11 > 0f)
			{
				float value3 = DefaultPerks.Steward.Contractors.PrimaryBonus * num11;
				bonuses2.AddFactor(value3, DefaultPerks.Steward.Contractors.Name);
			}
		}
		if (mobileParty.HasPerk(DefaultPerks.Trade.MercenaryConnections, checkSecondaryRole: true))
		{
			float num12 = (float)num8 / bonuses2.BaseNumber;
			if (num12 > 0f)
			{
				float value4 = DefaultPerks.Trade.MercenaryConnections.SecondaryBonus * num12;
				bonuses2.AddFactor(value4, DefaultPerks.Trade.MercenaryConnections.Name);
			}
		}
		bonuses2.AddFactor(value, DefaultPolicies.MilitaryCoronae.Name);
		bonuses2.AddFactor(result.ResultNumber - 1f, _buildingEffects);
		if (PartyBaseHelper.HasFeat(mobileParty.Party, DefaultCulturalFeats.AseraiIncreasedWageFeat))
		{
			bonuses2.AddFactor(DefaultCulturalFeats.AseraiIncreasedWageFeat.EffectBonus, _cultureText);
		}
		if (!mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Steward.Frugal))
		{
			bonuses2.AddFactor(DefaultPerks.Steward.Frugal.PrimaryBonus, DefaultPerks.Steward.Frugal.Name);
		}
		if (mobileParty.Army != null)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.EfficientCampaigner, mobileParty, isPrimaryBonus: false, ref bonuses2, mobileParty.IsCurrentlyAtSea);
		}
		if (mobileParty.SiegeEvent != null && mobileParty.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(mobileParty.Party) && mobileParty.HasPerk(DefaultPerks.Steward.MasterOfWarcraft))
		{
			bonuses2.AddFactor(DefaultPerks.Steward.MasterOfWarcraft.PrimaryBonus, DefaultPerks.Steward.MasterOfWarcraft.Name);
		}
		if (mobileParty.EffectiveQuartermaster != null)
		{
			PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Steward.PriceOfLoyalty, mobileParty.EffectiveQuartermaster.CharacterObject, DefaultSkills.Steward, applyPrimaryBonus: true, ref bonuses2, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
		}
		if (mobileParty.CurrentSettlement != null && mobileParty.LeaderHero != null && mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Trade.ContentTrades))
		{
			bonuses2.AddFactor(DefaultPerks.Trade.ContentTrades.SecondaryBonus, DefaultPerks.Trade.ContentTrades.Name);
		}
		return bonuses2;
	}

	private void CalculatePartialGarrisonWageReduction(float troopRatio, MobileParty mobileParty, PerkObject perk, ref ExplainedNumber garrisonWageReductionMultiplier, bool isSecondaryEffect)
	{
		if (troopRatio > 0f && mobileParty.CurrentSettlement.Town.Governor != null && PerkHelper.GetPerkValueForTown(perk, mobileParty.CurrentSettlement.Town))
		{
			garrisonWageReductionMultiplier.AddFactor(isSecondaryEffect ? (perk.SecondaryBonus * troopRatio) : (perk.PrimaryBonus * troopRatio), perk.Name);
		}
	}

	public override ExplainedNumber GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
	{
		ExplainedNumber result = ((troop.Level <= 1) ? new ExplainedNumber(10f) : ((troop.Level <= 6) ? new ExplainedNumber(20f) : ((troop.Level <= 11) ? new ExplainedNumber(50f) : ((troop.Level <= 16) ? new ExplainedNumber(100f) : ((troop.Level <= 21) ? new ExplainedNumber(200f) : ((troop.Level <= 26) ? new ExplainedNumber(400f) : ((troop.Level <= 31) ? new ExplainedNumber(600f) : ((troop.Level > 36) ? new ExplainedNumber(1500f) : new ExplainedNumber(1000f)))))))));
		if (troop.Equipment.Horse.Item != null && !withoutItemCost)
		{
			if (troop.Level < 26)
			{
				result.Add(150f);
			}
			else
			{
				result.Add(500f);
			}
		}
		bool flag = troop.Occupation == Occupation.Mercenary || troop.Occupation == Occupation.Gangster || troop.Occupation == Occupation.CaravanGuard;
		if (flag)
		{
			result.Add(result.BaseNumber * 2f);
		}
		if (buyerHero != null)
		{
			if (troop.Tier >= 2 && buyerHero.GetPerkValue(DefaultPerks.Throwing.HeadHunter))
			{
				result.AddFactor(DefaultPerks.Throwing.HeadHunter.SecondaryBonus);
			}
			if (troop.IsInfantry)
			{
				if (buyerHero.GetPerkValue(DefaultPerks.OneHanded.ChinkInTheArmor))
				{
					result.AddFactor(DefaultPerks.OneHanded.ChinkInTheArmor.SecondaryBonus);
				}
				if (buyerHero.GetPerkValue(DefaultPerks.TwoHanded.ShowOfStrength))
				{
					result.AddFactor(DefaultPerks.TwoHanded.ShowOfStrength.SecondaryBonus);
				}
				if (buyerHero.GetPerkValue(DefaultPerks.Polearm.HardyFrontline))
				{
					result.AddFactor(DefaultPerks.Polearm.HardyFrontline.SecondaryBonus);
				}
			}
			else if (troop.IsRanged)
			{
				if (buyerHero.GetPerkValue(DefaultPerks.Bow.RenownedArcher))
				{
					result.AddFactor(DefaultPerks.Bow.RenownedArcher.SecondaryBonus);
				}
				if (buyerHero.GetPerkValue(DefaultPerks.Crossbow.Piercer))
				{
					result.AddFactor(DefaultPerks.Crossbow.Piercer.SecondaryBonus);
				}
			}
			if (troop.IsMounted && buyerHero.Culture.HasFeat(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat))
			{
				result.AddFactor(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat.EffectBonus, _cultureText);
			}
			if (buyerHero.IsPartyLeader && buyerHero.GetPerkValue(DefaultPerks.Steward.Frugal))
			{
				result.AddFactor(DefaultPerks.Steward.Frugal.SecondaryBonus);
			}
			if (flag)
			{
				if (buyerHero.GetPerkValue(DefaultPerks.Trade.SwordForBarter))
				{
					result.AddFactor(DefaultPerks.Trade.SwordForBarter.PrimaryBonus);
				}
				if (buyerHero.GetPerkValue(DefaultPerks.Charm.SlickNegotiator))
				{
					result.AddFactor(DefaultPerks.Charm.SlickNegotiator.PrimaryBonus);
				}
			}
			result.LimitMin(1f);
		}
		return result;
	}
}
