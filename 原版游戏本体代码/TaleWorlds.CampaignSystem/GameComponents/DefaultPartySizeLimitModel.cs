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

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000136 RID: 310
	public class DefaultPartySizeLimitModel : PartySizeLimitModel
	{
		// Token: 0x1700068B RID: 1675
		// (get) Token: 0x0600190F RID: 6415 RVA: 0x0007B43F File Offset: 0x0007963F
		public override int MinimumNumberOfVillagersAtVillagerParty
		{
			get
			{
				return 12;
			}
		}

		// Token: 0x06001911 RID: 6417 RVA: 0x0007B534 File Offset: 0x00079734
		public override ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			if (!party.IsMobile)
			{
				return result;
			}
			if (party.MobileParty.IsGarrison)
			{
				return this.CalculateGarrisonPartySizeLimit(party.MobileParty.GarrisonPartyComponent.Settlement, includeDescriptions);
			}
			if (party.MobileParty.IsPatrolParty)
			{
				return this.CalculatePatrolPartySizeLimit(party.MobileParty, includeDescriptions);
			}
			return this.CalculateMobilePartyMemberSizeLimit(party.MobileParty, includeDescriptions);
		}

		// Token: 0x06001912 RID: 6418 RVA: 0x0007B5A8 File Offset: 0x000797A8
		private ExplainedNumber CalculatePatrolPartySizeLimit(MobileParty mobileParty, bool includeDescriptions)
		{
			new ExplainedNumber(10f, includeDescriptions, null);
			foreach (Building building in mobileParty.HomeSettlement.Town.Buildings)
			{
				if (building.BuildingType == DefaultBuildingTypes.SettlementGuardHouse)
				{
					return new ExplainedNumber((float)this.GetPatrolPartySizeLimitFromGuardHouseLevel(building.CurrentLevel), includeDescriptions, null);
				}
			}
			return new ExplainedNumber(0f, includeDescriptions, null);
		}

		// Token: 0x06001913 RID: 6419 RVA: 0x0007B640 File Offset: 0x00079840
		private int GetPatrolPartySizeLimitFromGuardHouseLevel(int level)
		{
			return 10 + 5 * level;
		}

		// Token: 0x06001914 RID: 6420 RVA: 0x0007B648 File Offset: 0x00079848
		public override ExplainedNumber GetPartyPrisonerSizeLimit(PartyBase party, bool includeDescriptions = false)
		{
			if (party.IsSettlement)
			{
				return this.CalculateSettlementPartyPrisonerSizeLimitInternal(party.Settlement, includeDescriptions);
			}
			return this.CalculateMobilePartyPrisonerSizeLimitInternal(party, includeDescriptions);
		}

		// Token: 0x06001915 RID: 6421 RVA: 0x0007B668 File Offset: 0x00079868
		private ExplainedNumber CalculateMobilePartyMemberSizeLimit(MobileParty party, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(20f, includeDescriptions, this._baseSizeText);
			if (party.LeaderHero != null && party.LeaderHero.Clan != null && !party.IsCaravan)
			{
				this.CalculateBaseMemberSize(party.LeaderHero, party.MapFaction, party.ActualClan, ref result);
				SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.StewardPartySizeBonus, party, ref result);
				if (DefaultPartySizeLimitModel._addAdditionalPartySizeAsCheat && party.IsMainParty && Game.Current.CheatMode)
				{
					result.Add(5000f, new TextObject("{=!}Additional size from extra party cheat", null), null);
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
					result.Add((float)num, this._randomSizeBonusTemporary, null);
				}
				else
				{
					Hero owner = party.Party.Owner;
					if (owner != null && owner.IsNotable)
					{
						result.Add((float)(10 * ((party.Party.Owner.Power < 100f) ? 1 : ((party.Party.Owner.Power < 200f) ? 2 : 3))), this._randomSizeBonusTemporary, null);
					}
				}
			}
			else if (party.IsVillager)
			{
				result.Add(40f, this._randomSizeBonusTemporary, null);
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

		// Token: 0x06001916 RID: 6422 RVA: 0x0007B84C File Offset: 0x00079A4C
		public override ExplainedNumber CalculateGarrisonPartySizeLimit(Settlement settlement, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(200f, includeDescriptions, this._baseSizeText);
			SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.LeadershipGarrisonSizeBonus, settlement.OwnerClan.Leader.CharacterObject, ref result);
			if (settlement.IsTown)
			{
				result.Add(200f, this._townBonusText, null);
			}
			this.AddGarrisonOwnerPerkEffects(settlement, ref result);
			this.AddSettlementProjectBonuses(settlement, ref result);
			return result;
		}

		// Token: 0x06001917 RID: 6423 RVA: 0x0007B8B8 File Offset: 0x00079AB8
		private ExplainedNumber CalculateSettlementPartyPrisonerSizeLimitInternal(Settlement settlement, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(60f, includeDescriptions, this._baseSizeText);
			Town town = settlement.Town;
			int num = ((town != null) ? town.GetWallLevel() : 0);
			if (num > 0)
			{
				result.Add((float)(num * 40), this._wallLevelBonusText, null);
			}
			this.AddSettlementProjectPrisonerBonuses(settlement, ref result);
			return result;
		}

		// Token: 0x06001918 RID: 6424 RVA: 0x0007B90C File Offset: 0x00079B0C
		private ExplainedNumber CalculateMobilePartyPrisonerSizeLimitInternal(PartyBase party, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(10f, includeDescriptions, this._baseSizeText);
			result.Add((float)this.GetCurrentPartySizeEffect(party), this._currentPartySizeBonusText, null);
			this.AddMobilePartyLeaderPrisonerSizePerkEffects(party, ref result);
			if (DefaultPartySizeLimitModel._addAdditionalPrisonerSizeAsCheat && party.IsMobile && party.MobileParty.IsMainParty && Game.Current.CheatMode)
			{
				result.Add(5000f, new TextObject("{=!}Additional size from extra prisoner cheat", null), null);
			}
			return result;
		}

		// Token: 0x06001919 RID: 6425 RVA: 0x0007B98C File Offset: 0x00079B8C
		private void AddMobilePartyLeaderPrisonerSizePerkEffects(PartyBase party, ref ExplainedNumber result)
		{
			if (party.LeaderHero != null)
			{
				if (party.LeaderHero.GetPerkValue(DefaultPerks.TwoHanded.Terror))
				{
					result.Add(DefaultPerks.TwoHanded.Terror.SecondaryBonus, DefaultPerks.TwoHanded.Terror.Name, null);
				}
				if (!party.MobileParty.IsCurrentlyAtSea && party.LeaderHero.GetPerkValue(DefaultPerks.Athletics.Stamina))
				{
					result.Add(DefaultPerks.Athletics.Stamina.SecondaryBonus, DefaultPerks.Athletics.Stamina.Name, null);
				}
				if (party.LeaderHero.GetPerkValue(DefaultPerks.Roguery.Manhunter))
				{
					result.Add(DefaultPerks.Roguery.Manhunter.SecondaryBonus, DefaultPerks.Roguery.Manhunter.Name, null);
				}
				if (party.LeaderHero != null && party.LeaderHero.GetPerkValue(DefaultPerks.Scouting.VantagePoint))
				{
					result.Add(DefaultPerks.Scouting.VantagePoint.SecondaryBonus, DefaultPerks.Scouting.VantagePoint.Name, null);
				}
			}
		}

		// Token: 0x0600191A RID: 6426 RVA: 0x0007BA6D File Offset: 0x00079C6D
		private void AddGarrisonOwnerPerkEffects(Settlement currentSettlement, ref ExplainedNumber result)
		{
			if (currentSettlement != null && currentSettlement.IsFortification)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.OneHanded.CorpsACorps, currentSettlement.Town, ref result);
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Leadership.VeteransRespect, currentSettlement.Town, ref result);
			}
		}

		// Token: 0x0600191B RID: 6427 RVA: 0x0007BA9C File Offset: 0x00079C9C
		public override int GetNextClanTierPartySizeEffectChangeForHero(Hero hero)
		{
			int tierEffectInternal = this.GetTierEffectInternal(hero.Clan.Tier, hero.Clan.Leader == hero);
			return this.GetTierEffectInternal(hero.Clan.Tier + 1, hero.Clan.Leader == hero) - tierEffectInternal;
		}

		// Token: 0x0600191C RID: 6428 RVA: 0x0007BAEC File Offset: 0x00079CEC
		private int GetTierEffectInternal(int tier, bool isHeroClanLeader)
		{
			if (tier < 1)
			{
				return 0;
			}
			if (isHeroClanLeader)
			{
				return 25 * tier;
			}
			return 15 * tier;
		}

		// Token: 0x0600191D RID: 6429 RVA: 0x0007BB00 File Offset: 0x00079D00
		public override int GetAssumedPartySizeForLordParty(Hero leaderHero, IFaction partyMapFaction, Clan actualClan)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(20f, false, this._baseSizeText);
			if (leaderHero != null && leaderHero.Clan != null)
			{
				this.CalculateBaseMemberSize(leaderHero, partyMapFaction, actualClan, ref explainedNumber);
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.StewardPartySizeBonus, ref explainedNumber, leaderHero.GetSkillValue(DefaultSkills.Steward));
			}
			return (int)explainedNumber.ResultNumber;
		}

		// Token: 0x0600191E RID: 6430 RVA: 0x0007BB55 File Offset: 0x00079D55
		public override int GetClanTierPartySizeEffectForHero(Hero hero)
		{
			return this.GetTierEffectInternal(hero.Clan.Tier, hero.Clan.Leader == hero);
		}

		// Token: 0x0600191F RID: 6431 RVA: 0x0007BB76 File Offset: 0x00079D76
		private void AddSettlementProjectBonuses(Settlement settlement, ref ExplainedNumber result)
		{
			if (settlement != null && settlement.IsFortification)
			{
				settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.GarrisonCapacity, ref result);
			}
		}

		// Token: 0x06001920 RID: 6432 RVA: 0x0007BB90 File Offset: 0x00079D90
		private void AddSettlementProjectPrisonerBonuses(Settlement settlement, ref ExplainedNumber result)
		{
			if (settlement != null && settlement.IsFortification)
			{
				settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.PrisonCapacity, ref result);
			}
		}

		// Token: 0x06001921 RID: 6433 RVA: 0x0007BBAB File Offset: 0x00079DAB
		private int GetCurrentPartySizeEffect(PartyBase party)
		{
			return party.NumberOfHealthyMembers / 2;
		}

		// Token: 0x06001922 RID: 6434 RVA: 0x0007BBB8 File Offset: 0x00079DB8
		private void CalculateBaseMemberSize(Hero partyLeader, IFaction partyMapFaction, Clan actualClan, ref ExplainedNumber result)
		{
			if (partyMapFaction != null && partyMapFaction.IsKingdomFaction && partyLeader.MapFaction.Leader == partyLeader)
			{
				result.Add(20f, this._factionLeaderText, null);
			}
			if (partyLeader.GetPerkValue(DefaultPerks.OneHanded.Prestige))
			{
				result.Add(DefaultPerks.OneHanded.Prestige.SecondaryBonus, DefaultPerks.OneHanded.Prestige.Name, null);
			}
			if (partyLeader.GetPerkValue(DefaultPerks.TwoHanded.Hope))
			{
				result.Add(DefaultPerks.TwoHanded.Hope.SecondaryBonus, DefaultPerks.TwoHanded.Hope.Name, null);
			}
			if (partyLeader.GetPerkValue(DefaultPerks.Athletics.ImposingStature))
			{
				result.Add(DefaultPerks.Athletics.ImposingStature.SecondaryBonus, DefaultPerks.Athletics.ImposingStature.Name, null);
			}
			if (partyLeader.GetPerkValue(DefaultPerks.Bow.MerryMen))
			{
				result.Add(DefaultPerks.Bow.MerryMen.PrimaryBonus, DefaultPerks.Bow.MerryMen.Name, null);
			}
			if (partyLeader.GetPerkValue(DefaultPerks.Tactics.HordeLeader))
			{
				result.Add(DefaultPerks.Tactics.HordeLeader.PrimaryBonus, DefaultPerks.Tactics.HordeLeader.Name, null);
			}
			if (partyLeader.GetPerkValue(DefaultPerks.Scouting.MountedScouts))
			{
				result.Add(DefaultPerks.Scouting.MountedScouts.SecondaryBonus, DefaultPerks.Scouting.MountedScouts.Name, null);
			}
			if (partyLeader.GetPerkValue(DefaultPerks.Leadership.Authority))
			{
				result.Add(DefaultPerks.Leadership.Authority.SecondaryBonus, DefaultPerks.Leadership.Authority.Name, null);
			}
			if (partyLeader.GetPerkValue(DefaultPerks.Leadership.UpliftingSpirit))
			{
				result.Add(DefaultPerks.Leadership.UpliftingSpirit.SecondaryBonus, DefaultPerks.Leadership.UpliftingSpirit.Name, null);
			}
			if (partyLeader.GetPerkValue(DefaultPerks.Leadership.TalentMagnet))
			{
				result.Add(DefaultPerks.Leadership.TalentMagnet.PrimaryBonus, DefaultPerks.Leadership.TalentMagnet.Name, null);
			}
			if (partyLeader.GetSkillValue(DefaultSkills.Leadership) > Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus && partyLeader.GetPerkValue(DefaultPerks.Leadership.UltimateLeader))
			{
				int num = partyLeader.GetSkillValue(DefaultSkills.Leadership) - Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus;
				result.Add((float)num * DefaultPerks.Leadership.UltimateLeader.PrimaryBonus, this._leadershipPerkUltimateLeaderBonusText, null);
			}
			if (actualClan != null)
			{
				Hero leader = actualClan.Leader;
				bool? flag = ((leader != null) ? new bool?(leader.GetPerkValue(DefaultPerks.Leadership.LeaderOfMasses)) : null);
				bool flag2 = true;
				if ((flag.GetValueOrDefault() == flag2) & (flag != null))
				{
					int num2 = 0;
					using (List<Settlement>.Enumerator enumerator = actualClan.Settlements.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (enumerator.Current.IsTown)
							{
								num2++;
							}
						}
					}
					float num3 = (float)num2 * DefaultPerks.Leadership.LeaderOfMasses.PrimaryBonus;
					if (num3 > 0f)
					{
						result.Add(num3, DefaultPerks.Leadership.LeaderOfMasses.Name, null);
					}
				}
			}
			if (partyLeader.Clan.Leader == partyLeader)
			{
				if (partyLeader.Clan.Tier >= 5 && partyMapFaction.IsKingdomFaction && ((Kingdom)partyMapFaction).ActivePolicies.Contains(DefaultPolicies.NobleRetinues))
				{
					result.Add(40f, DefaultPolicies.NobleRetinues.Name, null);
				}
				if (partyMapFaction.IsKingdomFaction && partyMapFaction.Leader == partyLeader && ((Kingdom)partyMapFaction).ActivePolicies.Contains(DefaultPolicies.RoyalGuard))
				{
					result.Add(60f, DefaultPolicies.RoyalGuard.Name, null);
				}
			}
			result.Add((float)Campaign.Current.Models.PartySizeLimitModel.GetClanTierPartySizeEffectForHero(partyLeader), this._clanTierText, null);
		}

		// Token: 0x06001923 RID: 6435 RVA: 0x0007BF44 File Offset: 0x0007A144
		private float GetPartySizeRatioForSize(PartyTemplateObject partyTemplate, int desiredSize)
		{
			int num = partyTemplate.Stacks.Sum((PartyTemplateStack s) => s.MinValue);
			int num2 = partyTemplate.Stacks.Sum((PartyTemplateStack s) => s.MaxValue);
			float result;
			if (desiredSize < num)
			{
				result = (float)desiredSize / (float)num - 1f;
			}
			else if (num <= desiredSize && desiredSize <= num2)
			{
				result = (float)(desiredSize - num) / (float)(num2 - num);
			}
			else
			{
				result = (float)desiredSize / (float)num2;
			}
			return result;
		}

		// Token: 0x06001924 RID: 6436 RVA: 0x0007BFDC File Offset: 0x0007A1DC
		private float GetInitialPartySizeRatioForMobileParty(MobileParty party, PartyTemplateObject partyTemplate)
		{
			float result;
			if (party.IsBandit)
			{
				if (!partyTemplate.ShipHulls.IsEmpty<ShipTemplateStack>())
				{
					result = ((MBRandom.RandomFloat < 0.4f) ? MBRandom.RandomFloatRanged(0f, 0.33f) : MBRandom.RandomFloatRanged(0.66f, 1f));
				}
				else
				{
					float playerProgress = Campaign.Current.PlayerProgress;
					float num = 0.4f + 0.8f * playerProgress;
					float num2 = MBRandom.RandomFloatRanged(0.2f, 0.8f);
					result = num * num2;
				}
			}
			else if (party.IsCaravan && party.Owner == Hero.MainHero)
			{
				result = 1f;
			}
			else if (party.IsPatrolParty)
			{
				result = 1f;
			}
			else
			{
				result = party.RandomFloat();
			}
			return result;
		}

		// Token: 0x06001925 RID: 6437 RVA: 0x0007C098 File Offset: 0x0007A298
		public override int GetIdealVillagerPartySize(Village village)
		{
			float num = 0f;
			foreach (ValueTuple<ItemObject, float> valueTuple in village.VillageType.Productions)
			{
				float resultNumber = Campaign.Current.Models.VillageProductionCalculatorModel.CalculateDailyProductionAmount(village, valueTuple.Item1).ResultNumber;
				num += resultNumber;
			}
			float num2 = ((num > 10f) ? (40f * (1f - (MathF.Min(40f, num) - 10f) / 60f)) : 40f);
			return this.MinimumNumberOfVillagersAtVillagerParty + (int)(village.Hearth / num2);
		}

		// Token: 0x06001926 RID: 6438 RVA: 0x0007C160 File Offset: 0x0007A360
		public override TroopRoster FindAppropriateInitialRosterForMobileParty(MobileParty party, PartyTemplateObject partyTemplate)
		{
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			float initialPartySizeRatioForMobileParty = this.GetInitialPartySizeRatioForMobileParty(party, partyTemplate);
			for (int i = 0; i < partyTemplate.Stacks.Count; i++)
			{
				int minValue = partyTemplate.Stacks[i].MinValue;
				int maxValue = partyTemplate.Stacks[i].MaxValue;
				int num;
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
					Settlement bound = village.Bound;
					bool flag;
					if (bound == null)
					{
						flag = null != null;
					}
					else
					{
						Town town = bound.Town;
						flag = ((town != null) ? town.Governor : null) != null;
					}
					if (flag && village.Bound.Town.Governor.GetPerkValue(DefaultPerks.Scouting.VillageNetwork))
					{
						num = MathF.Round((float)num * (1f + DefaultPerks.Scouting.VillageNetwork.SecondaryBonus));
					}
				}
				if (num > 0)
				{
					CharacterObject character = partyTemplate.Stacks[i].Character;
					troopRoster.AddToCounts(character, num, false, 0, 0, true, -1);
				}
			}
			return troopRoster;
		}

		// Token: 0x06001927 RID: 6439 RVA: 0x0007C29C File Offset: 0x0007A49C
		public override List<Ship> FindAppropriateInitialShipsForMobileParty(MobileParty party, PartyTemplateObject partyTemplate)
		{
			List<Ship> list = new List<Ship>();
			float initialPartySizeRatioForMobileParty = this.GetInitialPartySizeRatioForMobileParty(party, partyTemplate);
			if (partyTemplate.ShipHulls != null && partyTemplate.ShipHulls.Count > 0)
			{
				foreach (ShipTemplateStack shipTemplateStack in partyTemplate.ShipHulls)
				{
					int minValue = shipTemplateStack.MinValue;
					int maxValue = shipTemplateStack.MaxValue;
					int num;
					if (initialPartySizeRatioForMobileParty <= 0f)
					{
						num = MBRandom.RoundRandomized(Math.Max(0f, (float)minValue + (float)minValue * initialPartySizeRatioForMobileParty));
					}
					else if (initialPartySizeRatioForMobileParty <= 1f)
					{
						num = MBRandom.RoundRandomized((float)minValue + (float)(maxValue - minValue) * initialPartySizeRatioForMobileParty);
					}
					else
					{
						num = MBRandom.RoundRandomized((float)maxValue * initialPartySizeRatioForMobileParty);
					}
					for (int i = 0; i < num; i++)
					{
						list.Add(new Ship(shipTemplateStack.ShipHull));
					}
				}
			}
			return list;
		}

		// Token: 0x04000820 RID: 2080
		private const int BaseMobilePartySize = 20;

		// Token: 0x04000821 RID: 2081
		private const int BaseMobilePartyPrisonerSize = 10;

		// Token: 0x04000822 RID: 2082
		private const int BaseSettlementPrisonerSize = 60;

		// Token: 0x04000823 RID: 2083
		private const int SettlementPrisonerSizeBonusPerWallLevel = 40;

		// Token: 0x04000824 RID: 2084
		private const int BaseGarrisonPartySize = 200;

		// Token: 0x04000825 RID: 2085
		private const int BasePatrolPartySize = 10;

		// Token: 0x04000826 RID: 2086
		private const int TownGarrisonSizeBonus = 200;

		// Token: 0x04000827 RID: 2087
		private const int AdditionalPartySizeForCheat = 5000;

		// Token: 0x04000828 RID: 2088
		private const int OneVillagerPerHearth = 40;

		// Token: 0x04000829 RID: 2089
		private const int AdditionalPartySizeLimitPerTier = 15;

		// Token: 0x0400082A RID: 2090
		private const int AdditionalPartySizeLimitForLeaderPerTier = 25;

		// Token: 0x0400082B RID: 2091
		private readonly TextObject _leadershipSkillLevelBonusText = GameTexts.FindText("str_leadership_skill_level_bonus", null);

		// Token: 0x0400082C RID: 2092
		private readonly TextObject _leadershipPerkUltimateLeaderBonusText = GameTexts.FindText("str_leadership_perk_bonus", null);

		// Token: 0x0400082D RID: 2093
		private readonly TextObject _wallLevelBonusText = GameTexts.FindText("str_map_tooltip_wall_level", null);

		// Token: 0x0400082E RID: 2094
		private readonly TextObject _baseSizeText = GameTexts.FindText("str_base_size", null);

		// Token: 0x0400082F RID: 2095
		private readonly TextObject _clanTierText = GameTexts.FindText("str_clan_tier_bonus", null);

		// Token: 0x04000830 RID: 2096
		private readonly TextObject _renownText = GameTexts.FindText("str_renown_bonus", null);

		// Token: 0x04000831 RID: 2097
		private readonly TextObject _clanLeaderText = GameTexts.FindText("str_clan_leader_bonus", null);

		// Token: 0x04000832 RID: 2098
		private readonly TextObject _factionLeaderText = GameTexts.FindText("str_faction_leader_bonus", null);

		// Token: 0x04000833 RID: 2099
		private readonly TextObject _leaderLevelText = GameTexts.FindText("str_leader_level_bonus", null);

		// Token: 0x04000834 RID: 2100
		private readonly TextObject _townBonusText = GameTexts.FindText("str_town_bonus", null);

		// Token: 0x04000835 RID: 2101
		private readonly TextObject _minorFactionText = GameTexts.FindText("str_minor_faction_bonus", null);

		// Token: 0x04000836 RID: 2102
		private readonly TextObject _currentPartySizeBonusText = GameTexts.FindText("str_current_party_size_bonus", null);

		// Token: 0x04000837 RID: 2103
		private readonly TextObject _randomSizeBonusTemporary = new TextObject("{=hynFV8jC}Extra size bonus (Perk-like Effect)", null);

		// Token: 0x04000838 RID: 2104
		private static bool _addAdditionalPartySizeAsCheat;

		// Token: 0x04000839 RID: 2105
		private static bool _addAdditionalPrisonerSizeAsCheat;

		// Token: 0x0200058F RID: 1423
		private enum LimitType
		{
			// Token: 0x04001783 RID: 6019
			MobilePartySizeLimit,
			// Token: 0x04001784 RID: 6020
			GarrisonPartySizeLimit,
			// Token: 0x04001785 RID: 6021
			PrisonerSizeLimit
		}
	}
}
