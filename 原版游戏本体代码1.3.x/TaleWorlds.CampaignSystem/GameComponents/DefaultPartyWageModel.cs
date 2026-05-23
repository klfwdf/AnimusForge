using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200013C RID: 316
	public class DefaultPartyWageModel : PartyWageModel
	{
		// Token: 0x1700068F RID: 1679
		// (get) Token: 0x06001953 RID: 6483 RVA: 0x0007DBF5 File Offset: 0x0007BDF5
		public override int MaxWagePaymentLimit
		{
			get
			{
				return 10000;
			}
		}

		// Token: 0x06001954 RID: 6484 RVA: 0x0007DBFC File Offset: 0x0007BDFC
		public override int GetCharacterWage(CharacterObject character)
		{
			int num;
			switch (character.Tier)
			{
			case 0:
				num = 1;
				break;
			case 1:
				num = 2;
				break;
			case 2:
				num = 3;
				break;
			case 3:
				num = 5;
				break;
			case 4:
				num = 8;
				break;
			case 5:
				num = 12;
				break;
			case 6:
				num = 17;
				break;
			default:
				num = 23;
				break;
			}
			if (character.Occupation == Occupation.Mercenary)
			{
				num = (int)((float)num * 1.5f);
			}
			return num;
		}

		// Token: 0x06001955 RID: 6485 RVA: 0x0007DC6C File Offset: 0x0007BE6C
		public override ExplainedNumber GetTotalWage(MobileParty mobileParty, TroopRoster troopRoster, bool includeDescriptions = false)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			bool flag = !mobileParty.HasPerk(DefaultPerks.Steward.AidCorps, false);
			int num7 = 0;
			int num8 = 0;
			for (int i = 0; i < troopRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = troopRoster.GetElementCopyAtIndex(i);
				CharacterObject character = elementCopyAtIndex.Character;
				if (!flag)
				{
					int number = elementCopyAtIndex.Number;
					int woundedNumber = elementCopyAtIndex.WoundedNumber;
				}
				else
				{
					int number2 = elementCopyAtIndex.Number;
				}
				if (character.IsHero)
				{
					bool flag2 = mobileParty.IsMainParty && character.HeroObject.Clan == Clan.PlayerClan && character.HeroObject.Occupation == Occupation.Lord;
					Hero heroObject = elementCopyAtIndex.Character.HeroObject;
					Clan clan = character.HeroObject.Clan;
					if (heroObject != ((clan != null) ? clan.Leader : null) && !flag2)
					{
						if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Steward.PaidInPromise))
						{
							num += MathF.Round((float)character.TroopWage * (1f + DefaultPerks.Steward.PaidInPromise.PrimaryBonus));
						}
						else
						{
							num += character.TroopWage;
						}
					}
				}
				else
				{
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
			}
			if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Roguery.DeepPockets))
			{
				num -= num6;
				ExplainedNumber explainedNumber = new ExplainedNumber((float)num6, false, null);
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.DeepPockets, mobileParty.LeaderHero.CharacterObject, false, ref explainedNumber, false);
				num += (int)explainedNumber.ResultNumber;
			}
			if (num5 > 0)
			{
				num -= num5;
				ExplainedNumber explainedNumber2 = new ExplainedNumber((float)num5, false, null);
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Crossbow.PickedShots, mobileParty, true, ref explainedNumber2, mobileParty.IsCurrentlyAtSea);
				num += (int)explainedNumber2.ResultNumber;
			}
			ExplainedNumber result = new ExplainedNumber((float)num, includeDescriptions, null);
			result.LimitMin(0f);
			ExplainedNumber explainedNumber3 = new ExplainedNumber(1f, false, null);
			if (mobileParty.IsGarrison)
			{
				Settlement currentSettlement = mobileParty.CurrentSettlement;
				if (((currentSettlement != null) ? currentSettlement.Town : null) != null)
				{
					if (mobileParty.CurrentSettlement.IsFortification)
					{
						PerkHelper.AddPerkBonusForTown(DefaultPerks.OneHanded.MilitaryTradition, mobileParty.CurrentSettlement.Town, ref result);
						PerkHelper.AddPerkBonusForTown(DefaultPerks.TwoHanded.Berserker, mobileParty.CurrentSettlement.Town, ref result);
						PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.DrillSergant, mobileParty.CurrentSettlement.Town, ref result);
						float troopRatio = (float)num2 / result.BaseNumber;
						this.CalculatePartialGarrisonWageReduction(troopRatio, mobileParty, DefaultPerks.Polearm.StandardBearer, ref result, true);
						float troopRatio2 = (float)num4 / result.BaseNumber;
						this.CalculatePartialGarrisonWageReduction(troopRatio2, mobileParty, DefaultPerks.Crossbow.PeasantLeader, ref result, true);
						float troopRatio3 = (float)num3 / result.BaseNumber;
						this.CalculatePartialGarrisonWageReduction(troopRatio3, mobileParty, DefaultPerks.Riding.CavalryTactics, ref result, true);
					}
					if (mobileParty.CurrentSettlement.IsCastle)
					{
						PerkHelper.AddPerkBonusForTown(DefaultPerks.Bow.HunterClan, mobileParty.CurrentSettlement.Town, ref result);
						PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.StiffUpperLip, mobileParty.CurrentSettlement.Town, ref result);
					}
					if (mobileParty.CurrentSettlement.Owner.Culture.HasFeat(DefaultCulturalFeats.EmpireGarrisonWageFeat))
					{
						result.AddFactor(DefaultCulturalFeats.EmpireGarrisonWageFeat.EffectBonus, this._cultureText);
					}
					mobileParty.CurrentSettlement.Town.AddEffectOfBuildings(BuildingEffectEnum.GarrisonWageReduction, ref explainedNumber3);
				}
			}
			float value = ((mobileParty.LeaderHero != null && mobileParty.LeaderHero.Clan.Kingdom != null && !mobileParty.LeaderHero.Clan.IsUnderMercenaryService && mobileParty.LeaderHero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.MilitaryCoronae)) ? 0.1f : 0f);
			if (mobileParty.HasPerk(DefaultPerks.Trade.SwordForBarter, true))
			{
				float num10 = (float)num7 / result.BaseNumber;
				if (num10 > 0f)
				{
					float value2 = DefaultPerks.Trade.SwordForBarter.SecondaryBonus * num10;
					result.AddFactor(value2, DefaultPerks.Trade.SwordForBarter.Name);
				}
			}
			if (mobileParty.HasPerk(DefaultPerks.Steward.Contractors, false))
			{
				float num11 = (float)num8 / result.BaseNumber;
				if (num11 > 0f)
				{
					float value3 = DefaultPerks.Steward.Contractors.PrimaryBonus * num11;
					result.AddFactor(value3, DefaultPerks.Steward.Contractors.Name);
				}
			}
			if (mobileParty.HasPerk(DefaultPerks.Trade.MercenaryConnections, true))
			{
				float num12 = (float)num8 / result.BaseNumber;
				if (num12 > 0f)
				{
					float value4 = DefaultPerks.Trade.MercenaryConnections.SecondaryBonus * num12;
					result.AddFactor(value4, DefaultPerks.Trade.MercenaryConnections.Name);
				}
			}
			result.AddFactor(value, DefaultPolicies.MilitaryCoronae.Name);
			result.AddFactor(explainedNumber3.ResultNumber - 1f, this._buildingEffects);
			if (PartyBaseHelper.HasFeat(mobileParty.Party, DefaultCulturalFeats.AseraiIncreasedWageFeat))
			{
				result.AddFactor(DefaultCulturalFeats.AseraiIncreasedWageFeat.EffectBonus, this._cultureText);
			}
			if (!mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Steward.Frugal, false))
			{
				result.AddFactor(DefaultPerks.Steward.Frugal.PrimaryBonus, DefaultPerks.Steward.Frugal.Name);
			}
			if (mobileParty.Army != null)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.EfficientCampaigner, mobileParty, false, ref result, mobileParty.IsCurrentlyAtSea);
			}
			if (mobileParty.SiegeEvent != null && mobileParty.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(mobileParty.Party, MapEvent.BattleTypes.Siege) && mobileParty.HasPerk(DefaultPerks.Steward.MasterOfWarcraft, false))
			{
				result.AddFactor(DefaultPerks.Steward.MasterOfWarcraft.PrimaryBonus, DefaultPerks.Steward.MasterOfWarcraft.Name);
			}
			if (mobileParty.EffectiveQuartermaster != null)
			{
				PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Steward.PriceOfLoyalty, mobileParty.EffectiveQuartermaster.CharacterObject, DefaultSkills.Steward, true, ref result, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus, false);
			}
			if (mobileParty.CurrentSettlement != null && mobileParty.LeaderHero != null && mobileParty.LeaderHero.GetPerkValue(DefaultPerks.Trade.ContentTrades))
			{
				result.AddFactor(DefaultPerks.Trade.ContentTrades.SecondaryBonus, DefaultPerks.Trade.ContentTrades.Name);
			}
			return result;
		}

		// Token: 0x06001956 RID: 6486 RVA: 0x0007E2BC File Offset: 0x0007C4BC
		private void CalculatePartialGarrisonWageReduction(float troopRatio, MobileParty mobileParty, PerkObject perk, ref ExplainedNumber garrisonWageReductionMultiplier, bool isSecondaryEffect)
		{
			if (troopRatio > 0f && mobileParty.CurrentSettlement.Town.Governor != null && PerkHelper.GetPerkValueForTown(perk, mobileParty.CurrentSettlement.Town))
			{
				garrisonWageReductionMultiplier.AddFactor(isSecondaryEffect ? (perk.SecondaryBonus * troopRatio) : (perk.PrimaryBonus * troopRatio), perk.Name);
			}
		}

		// Token: 0x06001957 RID: 6487 RVA: 0x0007E31C File Offset: 0x0007C51C
		public override ExplainedNumber GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
		{
			ExplainedNumber result;
			if (troop.Level <= 1)
			{
				result = new ExplainedNumber(10f, false, null);
			}
			else if (troop.Level <= 6)
			{
				result = new ExplainedNumber(20f, false, null);
			}
			else if (troop.Level <= 11)
			{
				result = new ExplainedNumber(50f, false, null);
			}
			else if (troop.Level <= 16)
			{
				result = new ExplainedNumber(100f, false, null);
			}
			else if (troop.Level <= 21)
			{
				result = new ExplainedNumber(200f, false, null);
			}
			else if (troop.Level <= 26)
			{
				result = new ExplainedNumber(400f, false, null);
			}
			else if (troop.Level <= 31)
			{
				result = new ExplainedNumber(600f, false, null);
			}
			else if (troop.Level <= 36)
			{
				result = new ExplainedNumber(1000f, false, null);
			}
			else
			{
				result = new ExplainedNumber(1500f, false, null);
			}
			if (troop.Equipment.Horse.Item != null && !withoutItemCost)
			{
				if (troop.Level < 26)
				{
					result.Add(150f, null, null);
				}
				else
				{
					result.Add(500f, null, null);
				}
			}
			bool flag = troop.Occupation == Occupation.Mercenary || troop.Occupation == Occupation.Gangster || troop.Occupation == Occupation.CaravanGuard;
			if (flag)
			{
				result.Add(result.BaseNumber * 2f, null, null);
			}
			if (buyerHero != null)
			{
				if (troop.Tier >= 2 && buyerHero.GetPerkValue(DefaultPerks.Throwing.HeadHunter))
				{
					result.AddFactor(DefaultPerks.Throwing.HeadHunter.SecondaryBonus, null);
				}
				if (troop.IsInfantry)
				{
					if (buyerHero.GetPerkValue(DefaultPerks.OneHanded.ChinkInTheArmor))
					{
						result.AddFactor(DefaultPerks.OneHanded.ChinkInTheArmor.SecondaryBonus, null);
					}
					if (buyerHero.GetPerkValue(DefaultPerks.TwoHanded.ShowOfStrength))
					{
						result.AddFactor(DefaultPerks.TwoHanded.ShowOfStrength.SecondaryBonus, null);
					}
					if (buyerHero.GetPerkValue(DefaultPerks.Polearm.HardyFrontline))
					{
						result.AddFactor(DefaultPerks.Polearm.HardyFrontline.SecondaryBonus, null);
					}
				}
				else if (troop.IsRanged)
				{
					if (buyerHero.GetPerkValue(DefaultPerks.Bow.RenownedArcher))
					{
						result.AddFactor(DefaultPerks.Bow.RenownedArcher.SecondaryBonus, null);
					}
					if (buyerHero.GetPerkValue(DefaultPerks.Crossbow.Piercer))
					{
						result.AddFactor(DefaultPerks.Crossbow.Piercer.SecondaryBonus, null);
					}
				}
				if (troop.IsMounted && buyerHero.Culture.HasFeat(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat))
				{
					result.AddFactor(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat.EffectBonus, this._cultureText);
				}
				if (buyerHero.IsPartyLeader && buyerHero.GetPerkValue(DefaultPerks.Steward.Frugal))
				{
					result.AddFactor(DefaultPerks.Steward.Frugal.SecondaryBonus, null);
				}
				if (flag)
				{
					if (buyerHero.GetPerkValue(DefaultPerks.Trade.SwordForBarter))
					{
						result.AddFactor(DefaultPerks.Trade.SwordForBarter.PrimaryBonus, null);
					}
					if (buyerHero.GetPerkValue(DefaultPerks.Charm.SlickNegotiator))
					{
						result.AddFactor(DefaultPerks.Charm.SlickNegotiator.PrimaryBonus, null);
					}
				}
				result.LimitMin(1f);
			}
			return result;
		}

		// Token: 0x04000865 RID: 2149
		private readonly TextObject _cultureText = GameTexts.FindText("str_culture", null);

		// Token: 0x04000866 RID: 2150
		private readonly TextObject _buildingEffects = GameTexts.FindText("str_building_effects", null);

		// Token: 0x04000867 RID: 2151
		private const float MercenaryWageFactor = 1.5f;
	}
}
