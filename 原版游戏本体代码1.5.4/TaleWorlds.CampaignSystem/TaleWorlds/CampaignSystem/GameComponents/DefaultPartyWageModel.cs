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
		Hero perkOwnerHero = null;
		bool flag = !mobileParty.HasPerk(DefaultPerks.Steward.AidCorps, out perkOwnerHero);
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
					if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Steward.PaidInPromise))
					{
						int num9 = MathF.Round((float)character.TroopWage * (1f + DefaultPerks.Steward.PaidInPromise.PrimaryBonus));
						num += num9;
					}
					else
					{
						num += character.TroopWage;
					}
				}
				continue;
			}
			int num10 = character.TroopWage * elementCopyAtIndex.Number;
			num += num10;
			if (character.Culture.IsBandit)
			{
				num6 += num10;
			}
			if (character.IsInfantry)
			{
				num2 += num10;
			}
			if (character.IsMounted)
			{
				num3 += num10;
			}
			if (character.Occupation == Occupation.CaravanGuard)
			{
				num7 += num10;
			}
			if (character.Occupation == Occupation.Mercenary)
			{
				num8 += num10;
			}
			if (character.IsRanged)
			{
				num4 += num10;
				if (character.Tier >= 4)
				{
					num5 += num10;
				}
			}
		}
		if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Roguery.DeepPockets))
		{
			num -= num6;
			ExplainedNumber bonuses = new ExplainedNumber(num6);
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.DeepPockets, mobileParty.CurrentBattleEnvironment, mobileParty.LeaderHero.CharacterObject, isPrimaryBonus: false, ref bonuses);
			num += (int)bonuses.ResultNumber;
		}
		if (num5 > 0)
		{
			num -= num5;
			ExplainedNumber stat = new ExplainedNumber(num5);
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Crossbow.PickedShots, mobileParty, isPrimaryBonus: true, ref stat);
			num += (int)stat.ResultNumber;
		}
		ExplainedNumber bonuses2 = new ExplainedNumber(num, includeDescriptions);
		bonuses2.LimitMin(0f);
		ExplainedNumber result = new ExplainedNumber(1f);
		if (mobileParty.IsGarrison && mobileParty.CurrentSettlement?.Town != null)
		{
			if (mobileParty.CurrentSettlement.IsFortification)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.OneHanded.MilitaryTradition, mobileParty.CurrentSettlement.Town, isPrimaryBonus: false, ref bonuses2);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.TwoHanded.Berserker, mobileParty.CurrentSettlement.Town, isPrimaryBonus: false, ref bonuses2);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.DrillSergant, mobileParty.CurrentSettlement.Town, isPrimaryBonus: false, ref bonuses2);
				float troopRatio = (float)num2 / bonuses2.BaseNumber;
				CalculatePartialGarrisonWageReduction(troopRatio, mobileParty, DefaultPerks.Polearm.StandardBearer, ref bonuses2, isSecondaryEffect: true);
				float troopRatio2 = (float)num4 / bonuses2.BaseNumber;
				CalculatePartialGarrisonWageReduction(troopRatio2, mobileParty, DefaultPerks.Crossbow.PeasantLeader, ref bonuses2, isSecondaryEffect: true);
				float troopRatio3 = (float)num3 / bonuses2.BaseNumber;
				CalculatePartialGarrisonWageReduction(troopRatio3, mobileParty, DefaultPerks.Riding.CavalryTactics, ref bonuses2, isSecondaryEffect: true);
			}
			if (mobileParty.CurrentSettlement.IsCastle)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Bow.HunterClan, mobileParty.CurrentSettlement.Town, isPrimaryBonus: false, ref bonuses2);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.StiffUpperLip, mobileParty.CurrentSettlement.Town, isPrimaryBonus: false, ref bonuses2);
			}
			FeatHelper.ApplyCultureFeat(mobileParty.CurrentSettlement.Owner.Culture, DefaultCulturalFeats.EmpireGarrisonWageFeat, ref bonuses2);
			mobileParty.CurrentSettlement.Town.AddEffectOfBuildings(BuildingEffectEnum.GarrisonWageReduction, ref result);
		}
		float value = ((mobileParty.LeaderHero != null && mobileParty.LeaderHero.Clan.Kingdom != null && !mobileParty.LeaderHero.Clan.IsUnderMercenaryService && mobileParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.MilitaryCoronae)) ? 0.1f : 0f);
		Hero perkOwnerHero2 = null;
		if (mobileParty.HasPerk(DefaultPerks.Trade.SwordForBarter, out perkOwnerHero2, checkSecondaryRole: true))
		{
			float num11 = (float)num7 / bonuses2.BaseNumber;
			if (num11 > 0f)
			{
				float value2 = DefaultPerks.Trade.SwordForBarter.SecondaryBonus * num11;
				bonuses2.AddFactor(value2, DefaultPerks.Trade.SwordForBarter.Name);
			}
		}
		Hero perkOwnerHero3 = null;
		if (mobileParty.HasPerk(DefaultPerks.Steward.Contractors, out perkOwnerHero3))
		{
			float num12 = (float)num8 / bonuses2.BaseNumber;
			if (num12 > 0f)
			{
				_ = bonuses2.BaseNumber;
				_ = DefaultPerks.Steward.Contractors.PrimaryBonus;
				bonuses2.AddFactor(DefaultPerks.Steward.Contractors.PrimaryBonus * num12, DefaultPerks.Steward.Contractors.Name);
			}
		}
		Hero perkOwnerHero4 = null;
		if (mobileParty.HasPerk(DefaultPerks.Trade.MercenaryConnections, out perkOwnerHero4, checkSecondaryRole: true))
		{
			float num13 = (float)num8 / bonuses2.BaseNumber;
			if (num13 > 0f)
			{
				_ = bonuses2.BaseNumber;
				_ = DefaultPerks.Trade.MercenaryConnections.SecondaryBonus;
				bonuses2.AddFactor(DefaultPerks.Trade.MercenaryConnections.SecondaryBonus * num13, DefaultPerks.Trade.MercenaryConnections.Name);
			}
		}
		bonuses2.AddFactor(value, DefaultPolicies.MilitaryCoronae.Name);
		bonuses2.AddFactor(result.ResultNumber - 1f, _buildingEffects);
		FeatHelper.ApplyCultureFeat(mobileParty.Party, DefaultCulturalFeats.AseraiIncreasedWageFeat, ref bonuses2);
		PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.Frugal, mobileParty, isPrimaryBonus: true, ref bonuses2);
		if (mobileParty.Army != null)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.EfficientCampaigner, mobileParty, isPrimaryBonus: false, ref bonuses2);
		}
		if (mobileParty.SiegeEvent != null && mobileParty.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(mobileParty.Party))
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.MasterOfWarcraft, mobileParty, isPrimaryBonus: true, ref bonuses2);
		}
		if (mobileParty.EffectiveQuartermaster != null)
		{
			PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Steward.PriceOfLoyalty, mobileParty.CurrentBattleEnvironment, mobileParty.EffectiveQuartermaster.CharacterObject, DefaultSkills.Steward, isPrimaryBonus: true, ref bonuses2, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
		}
		if (mobileParty.CurrentSettlement != null)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.ContentTrades, mobileParty, isPrimaryBonus: false, ref bonuses2);
		}
		if (mobileParty.LeaderHero != null)
		{
			TraitEffectHelper.ApplyTraitEffect(mobileParty.LeaderHero, DefaultPersonalityTraitEffects.GenerosityUpkeepReductionEffect, ref bonuses2);
		}
		else if (mobileParty.IsGarrison)
		{
			Hero hero = mobileParty.CurrentSettlement?.Town?.Governor;
			if (hero != null && hero.CurrentSettlement == mobileParty.CurrentSettlement)
			{
				TraitEffectHelper.ApplyTraitEffect(hero, DefaultPersonalityTraitEffects.GenerosityUpkeepReductionEffect, ref bonuses2);
			}
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
		ExplainedNumber stat = ((troop.Level <= 1) ? new ExplainedNumber(10f) : ((troop.Level <= 6) ? new ExplainedNumber(20f) : ((troop.Level <= 11) ? new ExplainedNumber(50f) : ((troop.Level <= 16) ? new ExplainedNumber(100f) : ((troop.Level <= 21) ? new ExplainedNumber(200f) : ((troop.Level <= 26) ? new ExplainedNumber(400f) : ((troop.Level <= 31) ? new ExplainedNumber(600f) : ((troop.Level > 36) ? new ExplainedNumber(1500f) : new ExplainedNumber(1000f)))))))));
		if (troop.Equipment.Horse.Item != null && !withoutItemCost)
		{
			if (troop.Level < 26)
			{
				stat.Add(150f);
			}
			else
			{
				stat.Add(500f);
			}
		}
		bool flag = troop.Occupation == Occupation.Mercenary || troop.Occupation == Occupation.Gangster || troop.Occupation == Occupation.CaravanGuard;
		if (flag)
		{
			stat.Add(stat.BaseNumber * 2f);
		}
		if (buyerHero != null)
		{
			MobileParty partyBelongedTo = buyerHero.PartyBelongedTo;
			if (partyBelongedTo != null)
			{
				if (troop.Tier >= 2)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Throwing.HeadHunter, partyBelongedTo, isPrimaryBonus: false, ref stat);
				}
				if (troop.IsInfantry)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.OneHanded.ChinkInTheArmor, partyBelongedTo, isPrimaryBonus: false, ref stat);
					PerkHelper.AddPerkBonusForParty(DefaultPerks.TwoHanded.ShowOfStrength, partyBelongedTo, isPrimaryBonus: false, ref stat);
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Polearm.HardyFrontline, partyBelongedTo, isPrimaryBonus: false, ref stat);
				}
				else if (troop.IsRanged)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Bow.RenownedArcher, partyBelongedTo, isPrimaryBonus: false, ref stat);
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Crossbow.Piercer, partyBelongedTo, isPrimaryBonus: false, ref stat);
				}
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.Frugal, partyBelongedTo, isPrimaryBonus: false, ref stat);
			}
			if (troop.IsMounted)
			{
				FeatHelper.ApplyCultureFeat(buyerHero.Culture, DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat, ref stat);
			}
			if (flag)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Trade.SwordForBarter, BattleEnvironment.Any, buyerHero.CharacterObject, isPrimaryBonus: true, ref stat);
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Charm.SlickNegotiator, BattleEnvironment.Any, buyerHero.CharacterObject, isPrimaryBonus: true, ref stat);
			}
		}
		stat.LimitMin(1f);
		return stat;
	}
}
