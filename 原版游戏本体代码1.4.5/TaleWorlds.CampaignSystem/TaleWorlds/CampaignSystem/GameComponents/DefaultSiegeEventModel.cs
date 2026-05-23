using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSiegeEventModel : SiegeEventModel
{
	private readonly TextObject _baseConstructionSpeedText = new TextObject("{=MhGbcXJ4}Base construction speed");

	private readonly TextObject _constructionSpeedProjectBonusText = new TextObject("{=xoTWC8Sm}Project Bonus");

	private readonly TextObject _weatherConstructionPenalty = new TextObject("{=J6RjCKbk}Weather");

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
			int partyIndex = -1;
			for (PartyBase nextInvolvedPartyForEventType = siegeEvent.BesiegedSettlement.GetNextInvolvedPartyForEventType(ref partyIndex); nextInvolvedPartyForEventType != null; nextInvolvedPartyForEventType = siegeEvent.BesiegedSettlement.GetNextInvolvedPartyForEventType(ref partyIndex))
			{
				if (nextInvolvedPartyForEventType.LeaderHero != null)
				{
					int num2 = nextInvolvedPartyForEventType.MobileParty.EffectiveEngineer?.GetSkillValue(DefaultSkills.Engineering) ?? 0;
					if (num2 > num)
					{
						num = num2;
						result = nextInvolvedPartyForEventType.MobileParty;
					}
				}
			}
		}
		return result;
	}

	public override float GetCasualtyChance(MobileParty siegeParty, SiegeEvent siegeEvent, BattleSideEnum side)
	{
		float num = 1f;
		if (siegeParty != null && siegeParty.HasPerk(DefaultPerks.Engineering.CampBuilding, checkSecondaryRole: true))
		{
			num += DefaultPerks.Engineering.CampBuilding.SecondaryBonus;
		}
		if (siegeParty != null && siegeParty.HasPerk(DefaultPerks.Medicine.SiegeMedic, checkSecondaryRole: true))
		{
			num -= DefaultPerks.Medicine.SiegeMedic.SecondaryBonus;
		}
		if (side == BattleSideEnum.Defender && siegeEvent.BesiegedSettlement.Town?.Governor != null && siegeEvent.BesiegedSettlement.Town.Governor.GetPerkValue(DefaultPerks.Medicine.BattleHardened))
		{
			num += DefaultPerks.Medicine.BattleHardened.SecondaryBonus;
		}
		return num;
	}

	public override int GetSiegeEngineDestructionCasualties(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType destroyedSiegeEngine)
	{
		return 2;
	}

	public override int GetColleteralDamageCasualties(SiegeEngineType siegeEngineType, MobileParty party)
	{
		int num = 1;
		if (party != null && !party.IsCurrentlyAtSea && party.HasPerk(DefaultPerks.Crossbow.Terror) && MBRandom.RandomFloat < DefaultPerks.Crossbow.Terror.PrimaryBonus)
		{
			num++;
		}
		return num;
	}

	public override float GetSiegeEngineHitChance(SiegeEngineType siegeEngineType, BattleSideEnum battleSide, SiegeBombardTargets target, Town town)
	{
		float num = 0f;
		switch (target)
		{
		case SiegeBombardTargets.Wall:
		case SiegeBombardTargets.RangedEngines:
			num = siegeEngineType.HitChance;
			break;
		case SiegeBombardTargets.People:
			num = siegeEngineType.AntiPersonnelHitChance;
			break;
		default:
			throw new ArgumentOutOfRangeException("target", target, null);
		}
		ExplainedNumber bonuses = new ExplainedNumber(num);
		if (battleSide == BattleSideEnum.Attacker && target == SiegeBombardTargets.RangedEngines)
		{
			float num2 = 0f;
			switch (town.GetWallLevel())
			{
			case 1:
				num2 = 0.05f;
				break;
			case 2:
				num2 = 0.1f;
				break;
			case 3:
				num2 = 0.15f;
				break;
			}
			bonuses.Add(0f - num2, new TextObject("{=b9NaTqyr}Extra Defender Defense"));
		}
		if (battleSide == BattleSideEnum.Defender)
		{
			if (target == SiegeBombardTargets.RangedEngines && town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Engineering.DreadfulSieger))
			{
				bonuses.AddFactor(DefaultPerks.Engineering.DreadfulSieger.PrimaryBonus, DefaultPerks.Engineering.DreadfulSieger.Name);
			}
			if (siegeEngineType == DefaultSiegeEngineTypes.Ballista)
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Crossbow.Pavise, town, ref bonuses);
			}
		}
		SiegeEvent siegeEvent = town.Settlement.SiegeEvent;
		MobileParty effectiveSiegePartyForSide = GetEffectiveSiegePartyForSide(siegeEvent, battleSide);
		MobileParty effectiveSiegePartyForSide2 = GetEffectiveSiegePartyForSide(siegeEvent, battleSide.GetOppositeSide());
		if (effectiveSiegePartyForSide != null)
		{
			if ((siegeEngineType == DefaultSiegeEngineTypes.Trebuchet || siegeEngineType == DefaultSiegeEngineTypes.Onager || siegeEngineType == DefaultSiegeEngineTypes.FireOnager) && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Foreman))
			{
				bonuses.AddFactor(DefaultPerks.Engineering.Foreman.PrimaryBonus, DefaultPerks.Engineering.Foreman.Name);
			}
			if ((siegeEngineType == DefaultSiegeEngineTypes.Ballista || siegeEngineType == DefaultSiegeEngineTypes.FireBallista) && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Salvager))
			{
				bonuses.AddFactor(DefaultPerks.Engineering.Salvager.PrimaryBonus, DefaultPerks.Engineering.Salvager.Name);
			}
		}
		if (battleSide == BattleSideEnum.Defender && effectiveSiegePartyForSide2 != null && target == SiegeBombardTargets.RangedEngines && effectiveSiegePartyForSide2.HasPerk(DefaultPerks.Engineering.DungeonArchitect))
		{
			bonuses.AddFactor(DefaultPerks.Engineering.DungeonArchitect.PrimaryBonus, DefaultPerks.Engineering.DungeonArchitect.Name);
		}
		if (bonuses.ResultNumber < 0f)
		{
			bonuses = new ExplainedNumber(0f, includeDescriptions: false, null);
		}
		return bonuses.ResultNumber;
	}

	public override float GetSiegeStrategyScore(SiegeEvent siege, BattleSideEnum side, SiegeStrategy strategy)
	{
		if (strategy == DefaultSiegeStrategies.PreserveStrength)
		{
			return -9000f;
		}
		if (strategy == DefaultSiegeStrategies.Custom)
		{
			if (siege == PlayerSiege.PlayerSiegeEvent && side == PlayerSiege.PlayerSide && siege.BesiegerCamp != null && siege.BesiegerCamp.LeaderParty == MobileParty.MainParty)
			{
				return 9000f;
			}
			return -100f;
		}
		return MBRandom.RandomFloat;
	}

	public override float GetConstructionProgressPerHour(SiegeEngineType type, SiegeEvent siegeEvent, ISiegeEventSide side)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions: false, null);
		float availableManDayPower = GetAvailableManDayPower(side);
		float num = type.ManDayCost;
		explainedNumber.Add(1f / (num / availableManDayPower * (float)CampaignTime.HoursInDay), _baseConstructionSpeedText);
		MobileParty effectiveSiegePartyForSide = GetEffectiveSiegePartyForSide(siegeEvent, side.BattleSide);
		if (effectiveSiegePartyForSide != null && (effectiveSiegePartyForSide?.EffectiveEngineer?.GetSkillValue(DefaultSkills.Engineering) ?? 0) > 0)
		{
			SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.SiegeEngineProductionBonus, effectiveSiegePartyForSide, ref explainedNumber);
		}
		if (side.BattleSide == BattleSideEnum.Defender)
		{
			siegeEvent.BesiegedSettlement.Town.AddEffectOfBuildings(BuildingEffectEnum.SiegeEngineSpeed, ref explainedNumber);
			Hero governor = siegeEvent.BesiegedSettlement.Town.Governor;
			if (governor?.CurrentSettlement != null && governor.CurrentSettlement == siegeEvent.BesiegedSettlement)
			{
				SkillHelper.AddSkillBonusForTown(DefaultSkillEffects.SiegeEngineProductionBonus, siegeEvent.BesiegedSettlement.Town, ref explainedNumber);
			}
		}
		if (siegeEvent?.BesiegerCamp.LeaderParty != null && siegeEvent.BesiegerCamp.LeaderParty.HasPerk(DefaultPerks.Steward.Sweatshops, checkSecondaryRole: true))
		{
			explainedNumber.AddFactor(DefaultPerks.Steward.Sweatshops.SecondaryBonus);
		}
		if (effectiveSiegePartyForSide != null)
		{
			SiegeEvent.SiegeEngineConstructionProgress siegePreparations = side.SiegeEngines.SiegePreparations;
			if (siegePreparations != null && !siegePreparations.IsConstructed && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.ImprovedTools))
			{
				explainedNumber.AddFactor(DefaultPerks.Engineering.ImprovedTools.PrimaryBonus, DefaultPerks.Engineering.ImprovedTools.Name);
			}
			else
			{
				PerkObject perkObject = (type.IsRanged ? DefaultPerks.Engineering.TorsionEngines : DefaultPerks.Engineering.Scaffolds);
				if (effectiveSiegePartyForSide.HasPerk(perkObject))
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

	public override float GetAvailableManDayPower(ISiegeEventSide side)
	{
		int partyIndex = -1;
		PartyBase nextInvolvedPartyForEventType = side.GetNextInvolvedPartyForEventType(ref partyIndex);
		int num = 0;
		while (nextInvolvedPartyForEventType != null)
		{
			num += nextInvolvedPartyForEventType.NumberOfHealthyMembers;
			nextInvolvedPartyForEventType = side.GetNextInvolvedPartyForEventType(ref partyIndex);
		}
		return TaleWorlds.Library.MathF.Sqrt(num);
	}

	public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSettlement(Settlement settlement)
	{
		List<SiegeEngineType> list = new List<SiegeEngineType>();
		if (settlement.IsFortification)
		{
			Town town = settlement.Town;
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions: false, null);
			town.AddEffectOfBuildings(BuildingEffectEnum.BallistaOnSiegeStart, ref result);
			for (int i = 0; (float)i < result.ResultNumber; i++)
			{
				list.Add(DefaultSiegeEngineTypes.Ballista);
			}
			ExplainedNumber result2 = new ExplainedNumber(0f, includeDescriptions: false, null);
			town.AddEffectOfBuildings(BuildingEffectEnum.CatapultOnSiegeStart, ref result2);
			for (int j = 0; (float)j < result2.ResultNumber; j++)
			{
				list.Add(DefaultSiegeEngineTypes.Catapult);
			}
			if (town.Governor != null && town.Governor.GetPerkValue(DefaultPerks.Engineering.SiegeWorks))
			{
				list.Add(DefaultSiegeEngineTypes.Catapult);
			}
		}
		return list;
	}

	public override IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSiegeCamp(BesiegerCamp besiegerCamp)
	{
		List<SiegeEngineType> list = new List<SiegeEngineType>();
		if (besiegerCamp.LeaderParty.HasPerk(DefaultPerks.Engineering.Battlements))
		{
			list.Add(DefaultSiegeEngineTypes.Ballista);
		}
		return list;
	}

	public override float GetSiegeEngineHitPoints(SiegeEvent siegeEvent, SiegeEngineType siegeEngine, BattleSideEnum battleSide)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(siegeEngine.BaseHitPoints);
		Settlement besiegedSettlement = siegeEvent.BesiegedSettlement;
		MobileParty effectiveSiegePartyForSide = GetEffectiveSiegePartyForSide(siegeEvent, battleSide);
		if (battleSide == BattleSideEnum.Defender && besiegedSettlement.Town.Governor != null && besiegedSettlement.Town.Governor.GetPerkValue(DefaultPerks.Engineering.SiegeEngineer))
		{
			explainedNumber.AddFactor(DefaultPerks.Engineering.SiegeEngineer.PrimaryBonus, DefaultPerks.Engineering.SiegeEngineer.Name);
		}
		if (siegeEngine.IsRanged)
		{
			if (effectiveSiegePartyForSide != null && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.SiegeWorks))
			{
				explainedNumber.AddFactor(DefaultPerks.Engineering.SiegeWorks.PrimaryBonus, DefaultPerks.Engineering.SiegeWorks.Name);
			}
		}
		else if (battleSide == BattleSideEnum.Attacker && effectiveSiegePartyForSide != null && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Carpenters))
		{
			explainedNumber.AddFactor(DefaultPerks.Engineering.Carpenters.PrimaryBonus, DefaultPerks.Engineering.Carpenters.Name);
		}
		return explainedNumber.ResultNumber;
	}

	public override float GetSiegeEngineDamage(SiegeEvent siegeEvent, BattleSideEnum battleSide, SiegeEngineType siegeEngine, SiegeBombardTargets target)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(siegeEngine.Damage);
		MobileParty effectiveSiegePartyForSide = GetEffectiveSiegePartyForSide(siegeEvent, battleSide);
		if (effectiveSiegePartyForSide != null)
		{
			if (battleSide == BattleSideEnum.Attacker)
			{
				if (target == SiegeBombardTargets.Wall && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.WallBreaker))
				{
					explainedNumber.AddFactor(DefaultPerks.Engineering.WallBreaker.PrimaryBonus, DefaultPerks.Engineering.WallBreaker.Name);
				}
				if (target == SiegeBombardTargets.RangedEngines && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Tactics.MakeThemPay))
				{
					explainedNumber.AddFactor(DefaultPerks.Tactics.MakeThemPay.PrimaryBonus, DefaultPerks.Tactics.MakeThemPay.Name);
				}
			}
			if ((target == SiegeBombardTargets.RangedEngines || target == SiegeBombardTargets.Wall) && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Masterwork))
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

	public override int GetRangedSiegeEngineReloadTime(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngine)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(siegeEngine.CampaignRateOfFirePerDay);
		MobileParty effectiveSiegePartyForSide = GetEffectiveSiegePartyForSide(siegeEvent, side);
		if (effectiveSiegePartyForSide != null)
		{
			if ((siegeEngine == DefaultSiegeEngineTypes.Ballista || siegeEngine == DefaultSiegeEngineTypes.FireBallista) && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.Clockwork))
			{
				explainedNumber.AddFactor(DefaultPerks.Engineering.Clockwork.PrimaryBonus, DefaultPerks.Engineering.Clockwork.Name);
			}
			else if ((siegeEngine == DefaultSiegeEngineTypes.Onager || siegeEngine == DefaultSiegeEngineTypes.Trebuchet || siegeEngine == DefaultSiegeEngineTypes.FireOnager) && effectiveSiegePartyForSide.HasPerk(DefaultPerks.Engineering.ArchitecturalCommisions))
			{
				explainedNumber.AddFactor(DefaultPerks.Engineering.ArchitecturalCommisions.PrimaryBonus, DefaultPerks.Engineering.ArchitecturalCommisions.Name);
			}
		}
		return TaleWorlds.Library.MathF.Round((float)(CampaignTime.MinutesInHour * CampaignTime.HoursInDay) / explainedNumber.ResultNumber);
	}

	public override IEnumerable<SiegeEngineType> GetAvailableAttackerRangedSiegeEngines(PartyBase party)
	{
		bool hasFirePerks = party.MobileParty.HasPerk(DefaultPerks.Engineering.Stonecutters, checkSecondaryRole: true) || party.MobileParty.HasPerk(DefaultPerks.Engineering.SiegeEngineer, checkSecondaryRole: true);
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
	}

	public override IEnumerable<SiegeEngineType> GetAvailableDefenderSiegeEngines(PartyBase party)
	{
		bool hasFirePerks = party.MobileParty.HasPerk(DefaultPerks.Engineering.Stonecutters, checkSecondaryRole: true) || party.MobileParty.HasPerk(DefaultPerks.Engineering.SiegeEngineer, checkSecondaryRole: true);
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
	}

	public override IEnumerable<SiegeEngineType> GetAvailableAttackerRamSiegeEngines(PartyBase party)
	{
		yield return DefaultSiegeEngineTypes.Ram;
	}

	public override IEnumerable<SiegeEngineType> GetAvailableAttackerTowerSiegeEngines(PartyBase party)
	{
		yield return DefaultSiegeEngineTypes.SiegeTower;
	}

	public override FlattenedTroopRoster GetPriorityTroopsForSallyOutAmbush()
	{
		FlattenedTroopRoster flattenedTroopRoster = new FlattenedTroopRoster();
		foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			if (IsPriorityTroopForSallyOutAmbush(item))
			{
				flattenedTroopRoster.Add(item);
			}
		}
		SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
		if (playerSiegeEvent.BesiegedSettlement.OwnerClan == Clan.PlayerClan && playerSiegeEvent.BesiegedSettlement.Town.GarrisonParty != null && playerSiegeEvent.BesiegedSettlement.Town.GarrisonParty.MemberRoster.Count > 0)
		{
			foreach (TroopRosterElement item2 in playerSiegeEvent.BesiegedSettlement.Town.GarrisonParty.MemberRoster.GetTroopRoster())
			{
				if (IsPriorityTroopForSallyOutAmbush(item2))
				{
					flattenedTroopRoster.Add(item2);
				}
			}
		}
		if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty)
		{
			foreach (PartyBase item3 in playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender).GetInvolvedPartiesForEventType())
			{
				if (item3 == PartyBase.MainParty)
				{
					continue;
				}
				foreach (TroopRosterElement item4 in item3.MemberRoster.GetTroopRoster())
				{
					if (IsPriorityTroopForSallyOutAmbush(item4))
					{
						flattenedTroopRoster.Add(item4);
					}
				}
			}
		}
		return flattenedTroopRoster;
	}

	private bool IsPriorityTroopForSallyOutAmbush(TroopRosterElement troop)
	{
		CharacterObject character = troop.Character;
		if (!character.IsHero)
		{
			return character.HasMount();
		}
		return true;
	}
}
