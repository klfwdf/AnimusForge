using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000155 RID: 341
	public class DefaultSiegeEventModel : SiegeEventModel
	{
		// Token: 0x06001A41 RID: 6721 RVA: 0x00084144 File Offset: 0x00082344
		public override string GetSiegeEngineMapPrefabName(SiegeEngineType type, int wallLevel, BattleSideEnum side)
		{
			string result = null;
			if (type == DefaultSiegeEngineTypes.Onager)
			{
				result = "mangonel_a_mapicon";
			}
			else if (type == DefaultSiegeEngineTypes.Catapult)
			{
				result = "mangonel_b_mapicon";
			}
			else if (type == DefaultSiegeEngineTypes.FireOnager)
			{
				result = "mangonel_a_fire_mapicon";
			}
			else if (type == DefaultSiegeEngineTypes.FireCatapult)
			{
				result = "mangonel_b_fire_mapicon";
			}
			else if (type == DefaultSiegeEngineTypes.Ballista)
			{
				result = ((side == BattleSideEnum.Attacker) ? "ballista_a_mapicon" : "ballista_b_mapicon");
			}
			else if (type == DefaultSiegeEngineTypes.FireBallista)
			{
				result = ((side == BattleSideEnum.Attacker) ? "ballista_a_fire_mapicon" : "ballista_b_fire_mapicon");
			}
			else if (type == DefaultSiegeEngineTypes.Trebuchet)
			{
				result = "trebuchet_a_mapicon";
			}
			else if (type == DefaultSiegeEngineTypes.Bricole)
			{
				result = "trebuchet_b_mapicon";
			}
			else if (type == DefaultSiegeEngineTypes.Ram)
			{
				result = "batteringram_a_mapicon";
			}
			else if (type == DefaultSiegeEngineTypes.SiegeTower)
			{
				switch (wallLevel)
				{
				case 1:
					result = "siegetower_5m_mapicon";
					break;
				case 2:
					result = "siegetower_9m_mapicon";
					break;
				case 3:
					result = "siegetower_12m_mapicon";
					break;
				}
			}
			return result;
		}

		// Token: 0x06001A42 RID: 6722 RVA: 0x0008423C File Offset: 0x0008243C
		public override string GetSiegeEngineMapProjectilePrefabName(SiegeEngineType type)
		{
			string result = null;
			if (type == DefaultSiegeEngineTypes.Onager || type == DefaultSiegeEngineTypes.Catapult || type == DefaultSiegeEngineTypes.Trebuchet || type == DefaultSiegeEngineTypes.Bricole)
			{
				result = "mangonel_mapicon_projectile";
			}
			else if (type == DefaultSiegeEngineTypes.FireOnager || type == DefaultSiegeEngineTypes.FireCatapult)
			{
				result = "mangonel_fire_mapicon_projectile";
			}
			else if (type == DefaultSiegeEngineTypes.Ballista)
			{
				result = "ballista_mapicon_projectile";
			}
			else if (type == DefaultSiegeEngineTypes.FireBallista)
			{
				result = "ballista_fire_mapicon_projectile";
			}
			return result;
		}

		// Token: 0x06001A43 RID: 6723 RVA: 0x000842AC File Offset: 0x000824AC
		public override string GetSiegeEngineMapReloadAnimationName(SiegeEngineType type, BattleSideEnum side)
		{
			string result = null;
			if (type == DefaultSiegeEngineTypes.Onager)
			{
				result = "mangonel_a_mapicon_reload";
			}
			else if (type == DefaultSiegeEngineTypes.Catapult)
			{
				result = "mangonel_b_mapicon_reload";
			}
			else if (type == DefaultSiegeEngineTypes.FireOnager)
			{
				result = "mangonel_a_fire_mapicon_reload";
			}
			else if (type == DefaultSiegeEngineTypes.FireCatapult)
			{
				result = "mangonel_b_fire_mapicon_reload";
			}
			else if (type == DefaultSiegeEngineTypes.Ballista)
			{
				result = ((side == BattleSideEnum.Attacker) ? "ballista_a_mapicon_reload" : "ballista_b_mapicon_reload");
			}
			else if (type == DefaultSiegeEngineTypes.FireBallista)
			{
				result = ((side == BattleSideEnum.Attacker) ? "ballista_a_fire_mapicon_reload" : "ballista_b_fire_mapicon_reload");
			}
			else if (type == DefaultSiegeEngineTypes.Trebuchet)
			{
				result = "trebuchet_a_mapicon_reload";
			}
			else if (type == DefaultSiegeEngineTypes.Bricole)
			{
				result = "trebuchet_b_mapicon_reload";
			}
			return result;
		}

		// Token: 0x06001A44 RID: 6724 RVA: 0x00084354 File Offset: 0x00082554
		public override string GetSiegeEngineMapFireAnimationName(SiegeEngineType type, BattleSideEnum side)
		{
			string result = null;
			if (type == DefaultSiegeEngineTypes.Onager)
			{
				result = "mangonel_a_mapicon_fire";
			}
			else if (type == DefaultSiegeEngineTypes.Catapult)
			{
				result = "mangonel_b_mapicon_fire";
			}
			else if (type == DefaultSiegeEngineTypes.FireOnager)
			{
				result = "mangonel_a_fire_mapicon_fire";
			}
			else if (type == DefaultSiegeEngineTypes.FireCatapult)
			{
				result = "mangonel_b_fire_mapicon_fire";
			}
			else if (type == DefaultSiegeEngineTypes.Ballista)
			{
				result = ((side == BattleSideEnum.Attacker) ? "ballista_a_mapicon_fire" : "ballista_b_mapicon_fire");
			}
			else if (type == DefaultSiegeEngineTypes.FireBallista)
			{
				result = ((side == BattleSideEnum.Attacker) ? "ballista_a_fire_mapicon_fire" : "ballista_b_fire_mapicon_fire");
			}
			else if (type == DefaultSiegeEngineTypes.Trebuchet)
			{
				result = "trebuchet_a_mapicon_fire";
			}
			else if (type == DefaultSiegeEngineTypes.Bricole)
			{
				result = "trebuchet_b_mapicon_fire";
			}
			return result;
		}

		// Token: 0x06001A45 RID: 6725 RVA: 0x000843FC File Offset: 0x000825FC
		public override sbyte GetSiegeEngineMapProjectileBoneIndex(SiegeEngineType type, BattleSideEnum side)
		{
			if (type == DefaultSiegeEngineTypes.Onager || type == DefaultSiegeEngineTypes.FireOnager)
			{
				return 2;
			}
			if (type == DefaultSiegeEngineTypes.Catapult || type == DefaultSiegeEngineTypes.FireCatapult)
			{
				return 2;
			}
			if (type == DefaultSiegeEngineTypes.Ballista || type == DefaultSiegeEngineTypes.FireBallista)
			{
				return 7;
			}
			if (type == DefaultSiegeEngineTypes.Trebuchet)
			{
				return 4;
			}
			if (type == DefaultSiegeEngineTypes.Bricole)
			{
				return 20;
			}
			return -1;
		}

		// Token: 0x06001A46 RID: 6726 RVA: 0x00084458 File Offset: 0x00082658
		public override MobileParty GetEffectiveSiegePartyForSide(SiegeEvent siegeEvent, BattleSideEnum battleSide)
		{
			MobileParty result = null;
			if (battleSide == BattleSideEnum.Attacker)
			{
				result = siegeEvent.BesiegerCamp.LeaderParty;
			}
			else
			{
				int num = 0;
				int num2 = -1;
				for (PartyBase nextInvolvedPartyForEventType = siegeEvent.BesiegedSettlement.GetNextInvolvedPartyForEventType(ref num2, MapEvent.BattleTypes.Siege); nextInvolvedPartyForEventType != null; nextInvolvedPartyForEventType = siegeEvent.BesiegedSettlement.GetNextInvolvedPartyForEventType(ref num2, MapEvent.BattleTypes.Siege))
				{
					if (nextInvolvedPartyForEventType.LeaderHero != null)
					{
						Hero effectiveEngineer = nextInvolvedPartyForEventType.MobileParty.EffectiveEngineer;
						int num3 = ((effectiveEngineer != null) ? effectiveEngineer.GetSkillValue(DefaultSkills.Engineering) : 0);
						if (num3 > num)
						{
							num = num3;
							result = nextInvolvedPartyForEventType.MobileParty;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06001A47 RID: 6727 RVA: 0x000844D8 File Offset: 0x000826D8
		public override float GetCasualtyChance(MobileParty siegeParty, SiegeEvent siegeEvent, BattleSideEnum side)
		{
			float num = 1f;
			if (siegeParty != null && siegeParty.HasPerk(DefaultPerks.Engineering.CampBuilding, true))
			{
				num += DefaultPerks.Engineering.CampBuilding.SecondaryBonus;
			}
			if (siegeParty != null && siegeParty.HasPerk(DefaultPerks.Medicine.SiegeMedic, true))
			{
				num -= DefaultPerks.Medicine.SiegeMedic.SecondaryBonus;
			}
			if (side == BattleSideEnum.Defender)
			{
				Town town = siegeEvent.BesiegedSettlement.Town;
				if (((town != null) ? town.Governor : null) != null && siegeEvent.BesiegedSettlement.Town.Governor.GetPerkValue(DefaultPerks.Medicine.BattleHardened))
				{
					num += DefaultPerks.Medicine.BattleHardened.SecondaryBonus;
				}
			}
			return num;
		}

		// Token: 0x06001A48 RID: 6728 RVA: 0x0008456D File Offset: 0x0008276D
		public override int GetSiegeEngineDestructionCasualties(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType destroyedSiegeEngine)
		{
			return 2;
		}

		// Token: 0x06001A49 RID: 6729 RVA: 0x00084570 File Offset: 0x00082770
		public override int GetColleteralDamageCasualties(SiegeEngineType siegeEngineType, MobileParty party)
		{
			int num = 1;
			if (party != null && !party.IsCurrentlyAtSea && party.HasPerk(DefaultPerks.Crossbow.Terror, false) && MBRandom.RandomFloat < DefaultPerks.Crossbow.Terror.PrimaryBonus)
			{
				num++;
			}
			return num;
		}

		// Token: 0x06001A4A RID: 6730 RVA: 0x000845B0 File Offset: 0x000827B0
		public override float GetSiegeEngineHitChance(SiegeEngineType siegeEngineType, BattleSideEnum battleSide, SiegeBombardTargets target, Town town)
		{
			float baseNumber;
			if (target - SiegeBombardTargets.Wall > 1)
			{
				if (target != SiegeBombardTargets.People)
				{
					throw new ArgumentOutOfRangeException("target", target, null);
				}
				baseNumber = siegeEngineType.AntiPersonnelHitChance;
			}
			else
			{
				baseNumber = siegeEngineType.HitChance;
			}
			ExplainedNumber explainedNumber = new ExplainedNumber(baseNumber, false, null);
			if (battleSide == BattleSideEnum.Attacker && target == SiegeBombardTargets.RangedEngines)
			{
				float num = 0f;
				switch (town.GetWallLevel())
				{
				case 1:
					num = 0.05f;
					break;
				case 2:
					num = 0.1f;
					break;
				case 3:
					num = 0.15f;
					break;
				}
				explainedNumber.Add(-num, new TextObject("{=b9NaTqyr}Extra Defender Defense", null), null);
			}
			if (battleSide == BattleSideEnum.Defender)
			{
				if (target == SiegeBombardTargets.RangedEngines && town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Engineering.DreadfulSieger))
				{
					explainedNumber.AddFactor(DefaultPerks.Engineering.DreadfulSieger.PrimaryBonus, DefaultPerks.Engineering.DreadfulSieger.Name);
				}
				if (siegeEngineType == DefaultSiegeEngineTypes.Ballista)
				{
					PerkHelper.AddPerkBonusForTown(DefaultPerks.Crossbow.Pavise, town, ref explainedNumber);
				}
			}
			SiegeEvent siegeEvent = town.Settlement.SiegeEvent;
			MobileParty effectiveSiegePartyForSide = this.GetEffectiveSiegePartyForSide(siegeEvent, battleSide);
			MobileParty effectiveSiegePartyForSide2 = this.GetEffectiveSiegePartyForSide(siegeEvent, battleSide.GetOppositeSide());
			if (effectiveSiegePartyForSide != null)
			{
				if ((siegeEngineType == DefaultSiegeEngineTypes.Trebuchet || siegeEngineType == DefaultSiegeEngineTypes.Onager || siegeEngineType == DefaultSiegeEngineTypes.FireOnager) && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Foreman, false))
				{
					explainedNumber.AddFactor(DefaultPerks.Engineering.Foreman.PrimaryBonus, DefaultPerks.Engineering.Foreman.Name);
				}
				if ((siegeEngineType == DefaultSiegeEngineTypes.Ballista || siegeEngineType == DefaultSiegeEngineTypes.FireBallista) && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Salvager, false))
				{
					explainedNumber.AddFactor(DefaultPerks.Engineering.Salvager.PrimaryBonus, DefaultPerks.Engineering.Salvager.Name);
				}
			}
			if (battleSide == BattleSideEnum.Defender && effectiveSiegePartyForSide2 != null && target == SiegeBombardTargets.RangedEngines && effectiveSiegePartyForSide2.HasPerk(DefaultPerks.Engineering.DungeonArchitect, false))
			{
				explainedNumber.AddFactor(DefaultPerks.Engineering.DungeonArchitect.PrimaryBonus, DefaultPerks.Engineering.DungeonArchitect.Name);
			}
			if (explainedNumber.ResultNumber < 0f)
			{
				explainedNumber = new ExplainedNumber(0f, false, null);
			}
			return explainedNumber.ResultNumber;
		}

		// Token: 0x06001A4B RID: 6731 RVA: 0x000847AC File Offset: 0x000829AC
		public override float GetSiegeStrategyScore(SiegeEvent siege, BattleSideEnum side, SiegeStrategy strategy)
		{
			if (strategy == DefaultSiegeStrategies.PreserveStrength)
			{
				return -9000f;
			}
			if (strategy != DefaultSiegeStrategies.Custom)
			{
				return MBRandom.RandomFloat;
			}
			if (siege == PlayerSiege.PlayerSiegeEvent && side == PlayerSiege.PlayerSide && siege.BesiegerCamp != null && siege.BesiegerCamp.LeaderParty == MobileParty.MainParty)
			{
				return 9000f;
			}
			return -100f;
		}

		// Token: 0x06001A4C RID: 6732 RVA: 0x0008480C File Offset: 0x00082A0C
		public override float GetConstructionProgressPerHour(SiegeEngineType type, SiegeEvent siegeEvent, ISiegeEventSide side)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, false, null);
			float availableManDayPower = this.GetAvailableManDayPower(side);
			float num = (float)type.ManDayCost;
			explainedNumber.Add(1f / (num / availableManDayPower * (float)CampaignTime.HoursInDay), this._baseConstructionSpeedText, null);
			MobileParty effectiveSiegePartyForSide = this.GetEffectiveSiegePartyForSide(siegeEvent, side.BattleSide);
			if (effectiveSiegePartyForSide != null)
			{
				int? num2;
				if (effectiveSiegePartyForSide == null)
				{
					num2 = null;
				}
				else
				{
					Hero effectiveEngineer = effectiveSiegePartyForSide.EffectiveEngineer;
					num2 = ((effectiveEngineer != null) ? new int?(effectiveEngineer.GetSkillValue(DefaultSkills.Engineering)) : null);
				}
				if ((num2 ?? 0) > 0)
				{
					SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.SiegeEngineProductionBonus, effectiveSiegePartyForSide, ref explainedNumber);
				}
			}
			if (side.BattleSide == BattleSideEnum.Defender)
			{
				siegeEvent.BesiegedSettlement.Town.AddEffectOfBuildings(BuildingEffectEnum.SiegeEngineSpeed, ref explainedNumber);
				Hero governor = siegeEvent.BesiegedSettlement.Town.Governor;
				if (((governor != null) ? governor.CurrentSettlement : null) != null && governor.CurrentSettlement == siegeEvent.BesiegedSettlement)
				{
					SkillHelper.AddSkillBonusForTown(DefaultSkillEffects.SiegeEngineProductionBonus, siegeEvent.BesiegedSettlement.Town, ref explainedNumber);
				}
			}
			if (((siegeEvent != null) ? siegeEvent.BesiegerCamp.LeaderParty : null) != null && siegeEvent.BesiegerCamp.LeaderParty.HasPerk(DefaultPerks.Steward.Sweatshops, true))
			{
				explainedNumber.AddFactor(DefaultPerks.Steward.Sweatshops.SecondaryBonus, null);
			}
			if (effectiveSiegePartyForSide != null)
			{
				SiegeEvent.SiegeEngineConstructionProgress siegePreparations = side.SiegeEngines.SiegePreparations;
				if (siegePreparations != null && !siegePreparations.IsConstructed && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.ImprovedTools, false))
				{
					explainedNumber.AddFactor(DefaultPerks.Engineering.ImprovedTools.PrimaryBonus, DefaultPerks.Engineering.ImprovedTools.Name);
				}
				else
				{
					PerkObject perkObject = (type.IsRanged ? DefaultPerks.Engineering.TorsionEngines : DefaultPerks.Engineering.Scaffolds);
					if (effectiveSiegePartyForSide.HasPerk(perkObject, false))
					{
						explainedNumber.AddFactor(perkObject.PrimaryBonus, perkObject.Name);
					}
				}
			}
			if (side.BattleSide == BattleSideEnum.Defender)
			{
				Settlement besiegedSettlement = siegeEvent.BesiegedSettlement;
				PerkObject salvager = DefaultPerks.Engineering.Salvager;
				if (PerkHelper.GetPerkValueForTown(salvager, besiegedSettlement.Town))
				{
					explainedNumber.AddFactor(salvager.SecondaryBonus * besiegedSettlement.Militia, salvager.Name);
				}
			}
			return explainedNumber.ResultNumber;
		}

		// Token: 0x06001A4D RID: 6733 RVA: 0x00084A2C File Offset: 0x00082C2C
		public override float GetAvailableManDayPower(ISiegeEventSide side)
		{
			int num = -1;
			PartyBase nextInvolvedPartyForEventType = side.GetNextInvolvedPartyForEventType(ref num, MapEvent.BattleTypes.Siege);
			int num2 = 0;
			while (nextInvolvedPartyForEventType != null)
			{
				num2 += nextInvolvedPartyForEventType.NumberOfHealthyMembers;
				nextInvolvedPartyForEventType = side.GetNextInvolvedPartyForEventType(ref num, MapEvent.BattleTypes.Siege);
			}
			return MathF.Sqrt((float)num2);
		}

		// Token: 0x06001A4E RID: 6734 RVA: 0x00084A68 File Offset: 0x00082C68
		public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSettlement(Settlement settlement)
		{
			List<SiegeEngineType> list = new List<SiegeEngineType>();
			if (settlement.IsFortification)
			{
				Town town = settlement.Town;
				ExplainedNumber explainedNumber = new ExplainedNumber(0f, false, null);
				town.AddEffectOfBuildings(BuildingEffectEnum.BallistaOnSiegeStart, ref explainedNumber);
				int num = 0;
				while ((float)num < explainedNumber.ResultNumber)
				{
					list.Add(DefaultSiegeEngineTypes.Ballista);
					num++;
				}
				ExplainedNumber explainedNumber2 = new ExplainedNumber(0f, false, null);
				town.AddEffectOfBuildings(BuildingEffectEnum.CatapultOnSiegeStart, ref explainedNumber2);
				int num2 = 0;
				while ((float)num2 < explainedNumber2.ResultNumber)
				{
					list.Add(DefaultSiegeEngineTypes.Catapult);
					num2++;
				}
				if (town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Engineering.SiegeWorks))
				{
					list.Add(DefaultSiegeEngineTypes.Catapult);
				}
			}
			return list;
		}

		// Token: 0x06001A4F RID: 6735 RVA: 0x00084B28 File Offset: 0x00082D28
		public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSiegeCamp(BesiegerCamp besiegerCamp)
		{
			List<SiegeEngineType> list = new List<SiegeEngineType>();
			if (besiegerCamp.LeaderParty.HasPerk(DefaultPerks.Engineering.Battlements, false))
			{
				list.Add(DefaultSiegeEngineTypes.Ballista);
			}
			return list;
		}

		// Token: 0x06001A50 RID: 6736 RVA: 0x00084B5C File Offset: 0x00082D5C
		public override float GetSiegeEngineHitPoints(SiegeEvent siegeEvent, SiegeEngineType siegeEngine, BattleSideEnum battleSide)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber((float)siegeEngine.BaseHitPoints, false, null);
			Settlement besiegedSettlement = siegeEvent.BesiegedSettlement;
			MobileParty effectiveSiegePartyForSide = this.GetEffectiveSiegePartyForSide(siegeEvent, battleSide);
			if (battleSide == BattleSideEnum.Defender && besiegedSettlement.Town.Governor != null && besiegedSettlement.Town.Governor.GetPerkValue(DefaultPerks.Engineering.SiegeEngineer))
			{
				explainedNumber.AddFactor(DefaultPerks.Engineering.SiegeEngineer.PrimaryBonus, DefaultPerks.Engineering.SiegeEngineer.Name);
			}
			if (siegeEngine.IsRanged)
			{
				if (effectiveSiegePartyForSide != null && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.SiegeWorks, false))
				{
					explainedNumber.AddFactor(DefaultPerks.Engineering.SiegeWorks.PrimaryBonus, DefaultPerks.Engineering.SiegeWorks.Name);
				}
			}
			else if (battleSide == BattleSideEnum.Attacker && effectiveSiegePartyForSide != null && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Carpenters, false))
			{
				explainedNumber.AddFactor(DefaultPerks.Engineering.Carpenters.PrimaryBonus, DefaultPerks.Engineering.Carpenters.Name);
			}
			return explainedNumber.ResultNumber;
		}

		// Token: 0x06001A51 RID: 6737 RVA: 0x00084C38 File Offset: 0x00082E38
		public override float GetSiegeEngineDamage(SiegeEvent siegeEvent, BattleSideEnum battleSide, SiegeEngineType siegeEngine, SiegeBombardTargets target)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber((float)siegeEngine.Damage, false, null);
			MobileParty effectiveSiegePartyForSide = this.GetEffectiveSiegePartyForSide(siegeEvent, battleSide);
			if (effectiveSiegePartyForSide != null)
			{
				if (battleSide == BattleSideEnum.Attacker)
				{
					if (target == SiegeBombardTargets.Wall && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.WallBreaker, false))
					{
						explainedNumber.AddFactor(DefaultPerks.Engineering.WallBreaker.PrimaryBonus, DefaultPerks.Engineering.WallBreaker.Name);
					}
					if (target == SiegeBombardTargets.RangedEngines && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Tactics.MakeThemPay, false))
					{
						explainedNumber.AddFactor(DefaultPerks.Tactics.MakeThemPay.PrimaryBonus, DefaultPerks.Tactics.MakeThemPay.Name);
					}
				}
				if ((target == SiegeBombardTargets.RangedEngines || target == SiegeBombardTargets.Wall) && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Masterwork, false))
				{
					int num = effectiveSiegePartyForSide.LeaderHero.GetSkillValue(DefaultSkills.Engineering) - Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus;
					if (num > 0)
					{
						float value = (float)num * DefaultPerks.Engineering.Masterwork.PrimaryBonus;
						explainedNumber.AddFactor(value, DefaultPerks.Engineering.Masterwork.Name);
					}
				}
			}
			if (battleSide == BattleSideEnum.Defender && target == SiegeBombardTargets.RangedEngines)
			{
				Hero governor = siegeEvent.BesiegedSettlement.Town.Governor;
				if (governor != null && governor.GetPerkValue(DefaultPerks.Tactics.MakeThemPay))
				{
					explainedNumber.AddFactor(DefaultPerks.Tactics.MakeThemPay.SecondaryBonus, DefaultPerks.Tactics.MakeThemPay.Name);
				}
			}
			return explainedNumber.ResultNumber;
		}

		// Token: 0x06001A52 RID: 6738 RVA: 0x00084D74 File Offset: 0x00082F74
		public override int GetRangedSiegeEngineReloadTime(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngine)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(siegeEngine.CampaignRateOfFirePerDay, false, null);
			MobileParty effectiveSiegePartyForSide = this.GetEffectiveSiegePartyForSide(siegeEvent, side);
			if (effectiveSiegePartyForSide != null)
			{
				if ((siegeEngine == DefaultSiegeEngineTypes.Ballista || siegeEngine == DefaultSiegeEngineTypes.FireBallista) && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Clockwork, false))
				{
					explainedNumber.AddFactor(DefaultPerks.Engineering.Clockwork.PrimaryBonus, DefaultPerks.Engineering.Clockwork.Name);
				}
				else if ((siegeEngine == DefaultSiegeEngineTypes.Onager || siegeEngine == DefaultSiegeEngineTypes.Trebuchet || siegeEngine == DefaultSiegeEngineTypes.FireOnager) && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.ArchitecturalCommisions, false))
				{
					explainedNumber.AddFactor(DefaultPerks.Engineering.ArchitecturalCommisions.PrimaryBonus, DefaultPerks.Engineering.ArchitecturalCommisions.Name);
				}
			}
			return MathF.Round((float)(CampaignTime.MinutesInHour * CampaignTime.HoursInDay) / explainedNumber.ResultNumber);
		}

		// Token: 0x06001A53 RID: 6739 RVA: 0x00084E31 File Offset: 0x00083031
		public override IEnumerable<SiegeEngineType> GetAvailableAttackerRangedSiegeEngines(PartyBase party)
		{
			bool hasFirePerks = party.MobileParty.HasPerk(DefaultPerks.Engineering.Stonecutters, true) || party.MobileParty.HasPerk(DefaultPerks.Engineering.SiegeEngineer, true);
			yield return DefaultSiegeEngineTypes.Ballista;
			if (hasFirePerks)
			{
				yield return DefaultSiegeEngineTypes.FireBallista;
			}
			yield return DefaultSiegeEngineTypes.Onager;
			if (hasFirePerks)
			{
				yield return DefaultSiegeEngineTypes.FireOnager;
			}
			yield return DefaultSiegeEngineTypes.Trebuchet;
			yield break;
		}

		// Token: 0x06001A54 RID: 6740 RVA: 0x00084E41 File Offset: 0x00083041
		public override IEnumerable<SiegeEngineType> GetAvailableDefenderSiegeEngines(PartyBase party)
		{
			bool hasFirePerks = party.MobileParty.HasPerk(DefaultPerks.Engineering.Stonecutters, true) || party.MobileParty.HasPerk(DefaultPerks.Engineering.SiegeEngineer, true);
			yield return DefaultSiegeEngineTypes.Ballista;
			if (hasFirePerks)
			{
				yield return DefaultSiegeEngineTypes.FireBallista;
			}
			yield return DefaultSiegeEngineTypes.Catapult;
			if (hasFirePerks)
			{
				yield return DefaultSiegeEngineTypes.FireCatapult;
			}
			yield break;
		}

		// Token: 0x06001A55 RID: 6741 RVA: 0x00084E51 File Offset: 0x00083051
		public override IEnumerable<SiegeEngineType> GetAvailableAttackerRamSiegeEngines(PartyBase party)
		{
			yield return DefaultSiegeEngineTypes.Ram;
			yield break;
		}

		// Token: 0x06001A56 RID: 6742 RVA: 0x00084E5A File Offset: 0x0008305A
		public override IEnumerable<SiegeEngineType> GetAvailableAttackerTowerSiegeEngines(PartyBase party)
		{
			yield return DefaultSiegeEngineTypes.SiegeTower;
			yield break;
		}

		// Token: 0x06001A57 RID: 6743 RVA: 0x00084E64 File Offset: 0x00083064
		public override FlattenedTroopRoster GetPriorityTroopsForSallyOutAmbush()
		{
			FlattenedTroopRoster flattenedTroopRoster = new FlattenedTroopRoster(4);
			foreach (TroopRosterElement troop in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (this.IsPriorityTroopForSallyOutAmbush(troop))
				{
					flattenedTroopRoster.Add(troop);
				}
			}
			SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
			if (playerSiegeEvent.BesiegedSettlement.OwnerClan == Clan.PlayerClan && playerSiegeEvent.BesiegedSettlement.Town.GarrisonParty != null && playerSiegeEvent.BesiegedSettlement.Town.GarrisonParty.MemberRoster.Count > 0)
			{
				foreach (TroopRosterElement troop2 in playerSiegeEvent.BesiegedSettlement.Town.GarrisonParty.MemberRoster.GetTroopRoster())
				{
					if (this.IsPriorityTroopForSallyOutAmbush(troop2))
					{
						flattenedTroopRoster.Add(troop2);
					}
				}
			}
			if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
			{
				foreach (PartyBase partyBase in playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender).GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege))
				{
					if (partyBase != PartyBase.MainParty)
					{
						foreach (TroopRosterElement troop3 in partyBase.MemberRoster.GetTroopRoster())
						{
							if (this.IsPriorityTroopForSallyOutAmbush(troop3))
							{
								flattenedTroopRoster.Add(troop3);
							}
						}
					}
				}
			}
			return flattenedTroopRoster;
		}

		// Token: 0x06001A58 RID: 6744 RVA: 0x00085044 File Offset: 0x00083244
		private bool IsPriorityTroopForSallyOutAmbush(TroopRosterElement troop)
		{
			CharacterObject character = troop.Character;
			return character.IsHero || character.HasMount();
		}

		// Token: 0x040008CA RID: 2250
		private readonly TextObject _baseConstructionSpeedText = new TextObject("{=MhGbcXJ4}Base construction speed", null);

		// Token: 0x040008CB RID: 2251
		private readonly TextObject _constructionSpeedProjectBonusText = new TextObject("{=xoTWC8Sm}Project Bonus", null);

		// Token: 0x040008CC RID: 2252
		private readonly TextObject _weatherConstructionPenalty = new TextObject("{=J6RjCKbk}Weather", null);
	}
}
