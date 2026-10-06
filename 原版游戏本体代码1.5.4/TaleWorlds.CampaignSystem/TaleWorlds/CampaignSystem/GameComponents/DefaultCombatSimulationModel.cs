using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultCombatSimulationModel : CombatSimulationModel
{
	public override ExplainedNumber SimulateHit(CharacterObject strikerTroop, CharacterObject struckTroop, PartyBase strikerParty, PartyBase struckParty, float strikerAdvantage, MapEvent battle, BattleEnvironment battleEnvironment, float strikerSideMorale, float struckSideMorale)
	{
		float troopPower = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(strikerTroop, strikerParty.Side, strikerParty.MapEvent.SimulationContext, strikerParty.MapEventSide.LeaderSimulationModifier);
		float troopPower2 = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(struckTroop, struckParty.Side, struckParty.MapEvent.SimulationContext, struckParty.MapEventSide.LeaderSimulationModifier);
		int num = (int)((0.5f + 0.5f * MBRandom.RandomFloat) * (40f * TaleWorlds.Library.MathF.Pow(troopPower / troopPower2, 0.7f) * strikerAdvantage));
		ExplainedNumber effectiveDamage = new ExplainedNumber(num);
		if (strikerParty.IsMobile && struckParty.IsMobile)
		{
			CalculateSimulationDamagePerkEffects(strikerTroop, struckTroop, strikerParty.MobileParty, struckParty.MobileParty, battle, battleEnvironment, ref effectiveDamage);
		}
		CalculateSimulationMoraleEffects(strikerSideMorale, struckSideMorale, ref effectiveDamage);
		return effectiveDamage;
	}

	public override ExplainedNumber SimulateHit(Ship strikerShip, Ship struckShip, PartyBase strikerParty, PartyBase struckParty, SiegeEngineType siegeEngine, float strikerAdvantage, MapEvent battle, out int troopCasualties)
	{
		troopCasualties = 0;
		return new ExplainedNumber(0f, includeDescriptions: false, null);
	}

	private static void CalculateSimulationMoraleEffects(float strikerMorale, float struckMorale, ref ExplainedNumber effectiveDamage)
	{
		float num = TaleWorlds.Library.MathF.Min(strikerMorale - 50f, 0f);
		float num2 = TaleWorlds.Library.MathF.Max(struckMorale - 50f, 0f);
		effectiveDamage.AddFactor((num - num2) * 0.005f);
	}

	private static void CalculateSimulationDamagePerkEffects(CharacterObject strikerTroop, CharacterObject struckTroop, MobileParty strikerParty, MobileParty struckParty, MapEvent battle, BattleEnvironment battleEnvironment, ref ExplainedNumber effectiveDamage)
	{
		if (strikerTroop.IsInfantry && struckTroop.IsMounted)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.TightFormations, battleEnvironment, strikerParty, isPrimaryBonus: true, ref effectiveDamage);
		}
		if (struckTroop.IsInfantry && strikerTroop.IsRanged)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.LooseFormations, battleEnvironment, struckParty, isPrimaryBonus: true, ref effectiveDamage);
		}
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(strikerParty.CurrentNavigationFace);
		if (faceTerrainType == TerrainType.Snow || faceTerrainType == TerrainType.Forest)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.ExtendedSkirmish, battleEnvironment, strikerParty, isPrimaryBonus: true, ref effectiveDamage);
		}
		if (faceTerrainType == TerrainType.Plain || faceTerrainType == TerrainType.Steppe || faceTerrainType == TerrainType.Desert)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.DecisiveBattle, battleEnvironment, strikerParty, isPrimaryBonus: true, ref effectiveDamage);
		}
		if (!strikerParty.IsBandit && struckParty.IsBandit)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.LawKeeper, battleEnvironment, strikerParty, isPrimaryBonus: true, ref effectiveDamage);
		}
		PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Coaching, battleEnvironment, strikerParty, isPrimaryBonus: true, ref effectiveDamage);
		if (struckTroop.Tier >= 3)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.EliteReserves, battleEnvironment, struckParty, isPrimaryBonus: true, ref effectiveDamage);
		}
		if (strikerParty.MemberRoster.TotalHealthyCount > struckParty.MemberRoster.TotalHealthyCount)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Encirclement, battleEnvironment, strikerParty, isPrimaryBonus: true, ref effectiveDamage);
		}
		if (strikerParty.MemberRoster.TotalHealthyCount < struckParty.MemberRoster.TotalHealthyCount)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Counteroffensive, battleEnvironment, strikerParty, isPrimaryBonus: false, ref effectiveDamage);
		}
		bool flag = false;
		foreach (MapEventParty item in battle.PartiesOnSide(BattleSideEnum.Defender))
		{
			if (item.Party == struckParty.Party)
			{
				flag = true;
				break;
			}
		}
		bool flag2 = !flag;
		bool flag3 = flag2;
		if (battle.IsSiegeAssault && flag2)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Besieged, battleEnvironment, strikerParty, isPrimaryBonus: true, ref effectiveDamage);
		}
		if (flag)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Vanguard, battleEnvironment, strikerParty, isPrimaryBonus: true, ref effectiveDamage);
		}
		if ((battle.IsSiegeOutside || battle.IsSallyOut) && flag3)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Rearguard, battleEnvironment, strikerParty, isPrimaryBonus: false, ref effectiveDamage);
		}
		if (battle.IsSallyOut && flag)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Vanguard, battleEnvironment, strikerParty, isPrimaryBonus: false, ref effectiveDamage);
		}
		if (battle.IsFieldBattle && flag2)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Counteroffensive, battleEnvironment, strikerParty, isPrimaryBonus: true, ref effectiveDamage);
		}
		if (strikerParty.Army != null && strikerParty.LeaderHero != null && strikerParty.Army.LeaderParty == strikerParty)
		{
			PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Tactics.TacticalMastery, battleEnvironment, strikerParty.LeaderHero.CharacterObject, DefaultSkills.Tactics, isPrimaryBonus: true, ref effectiveDamage, Campaign.Current.Models.CharacterDevelopmentModel.MinSkillRequiredForEpicPerkBonus);
		}
	}

	public override float GetMaximumSiegeEquipmentProgress(Settlement settlement)
	{
		float num = 0f;
		if (settlement.SiegeEvent != null && settlement.IsFortification)
		{
			foreach (SiegeEvent.SiegeEngineConstructionProgress item in settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.AllSiegeEngines())
			{
				if (!item.IsConstructed && item.Progress > num)
				{
					num = item.Progress;
				}
			}
		}
		return num;
	}

	public override int GetNumberOfEquipmentsBuilt(Settlement settlement)
	{
		if (settlement.SiegeEvent != null && settlement.IsFortification)
		{
			bool flag = false;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			foreach (SiegeEvent.SiegeEngineConstructionProgress item in settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.AllSiegeEngines())
			{
				if (item.IsConstructed)
				{
					if (item.SiegeEngine == DefaultSiegeEngineTypes.Ram)
					{
						flag = true;
					}
					else if (item.SiegeEngine == DefaultSiegeEngineTypes.SiegeTower)
					{
						num++;
					}
					else if (item.SiegeEngine == DefaultSiegeEngineTypes.Trebuchet || item.SiegeEngine == DefaultSiegeEngineTypes.Onager || item.SiegeEngine == DefaultSiegeEngineTypes.Ballista)
					{
						num2++;
					}
					else if (item.SiegeEngine == DefaultSiegeEngineTypes.FireOnager || item.SiegeEngine == DefaultSiegeEngineTypes.FireBallista)
					{
						num3++;
					}
				}
			}
			return (flag ? 1 : 0) + num + num2 + num3;
		}
		return 0;
	}

	public override float GetSettlementAdvantage(Settlement settlement)
	{
		if (settlement.SiegeEvent != null && settlement.IsFortification)
		{
			int wallLevel = settlement.Town.GetWallLevel();
			bool flag = false;
			bool flag2 = false;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			foreach (SiegeEvent.SiegeEngineConstructionProgress item in settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.AllSiegeEngines())
			{
				if (!item.IsConstructed)
				{
					continue;
				}
				if (item.SiegeEngine == DefaultSiegeEngineTypes.Ram || item.SiegeEngine == DefaultSiegeEngineTypes.ImprovedRam)
				{
					if (item.SiegeEngine == DefaultSiegeEngineTypes.ImprovedRam)
					{
						flag2 = true;
					}
					flag = true;
				}
				else if (item.SiegeEngine == DefaultSiegeEngineTypes.SiegeTower)
				{
					num++;
				}
				else if (item.SiegeEngine == DefaultSiegeEngineTypes.Trebuchet || item.SiegeEngine == DefaultSiegeEngineTypes.Onager || item.SiegeEngine == DefaultSiegeEngineTypes.Ballista)
				{
					num2++;
				}
				else if (item.SiegeEngine == DefaultSiegeEngineTypes.FireOnager || item.SiegeEngine == DefaultSiegeEngineTypes.FireBallista)
				{
					num3++;
				}
			}
			float num4 = 4f + (float)(wallLevel - 1);
			if (settlement.SettlementTotalWallHitPoints < 1E-05f)
			{
				num4 *= 0.25f;
			}
			float num5 = 1f + num4;
			float num6 = 1f + ((flag || num > 0) ? 0.25f : 0f) + (flag2 ? 0.24f : (flag ? 0.16f : 0f)) + ((num > 1) ? 0.24f : ((num == 1) ? 0.16f : 0f)) + (float)num2 * 0.08f + (float)num3 * 0.12f;
			float baseNumber = num5 / num6;
			ExplainedNumber effectiveAdvantage = new ExplainedNumber(baseNumber);
			ISiegeEventSide siegeEventSide = settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker);
			CalculateSettlementAdvantagePerkEffects(settlement, ref effectiveAdvantage, siegeEventSide);
			return effectiveAdvantage.ResultNumber;
		}
		if (settlement.IsVillage)
		{
			return 1.25f;
		}
		return 1f;
	}

	private static void CalculateSettlementAdvantagePerkEffects(Settlement settlement, ref ExplainedNumber effectiveAdvantage, ISiegeEventSide opposingSide)
	{
		foreach (PartyBase item in opposingSide.GetInvolvedPartiesForEventType())
		{
			if (PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.OnTheMarch, BattleEnvironment.Any, item.MobileParty, isPrimaryBonus: true, ref effectiveAdvantage))
			{
				break;
			}
		}
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Tactics.OnTheMarch, settlement.Town, isPrimaryBonus: false, ref effectiveAdvantage);
	}

	public override (int defenderRounds, int attackerRounds) GetSimulationTicksForBattleRound(MapEvent mapEvent)
	{
		MapEvent.BattleTypes eventType = mapEvent.EventType;
		Settlement mapEventSettlement = mapEvent.MapEventSettlement;
		int item = 0;
		int item2 = 0;
		int numRemainingSimulationTroops = mapEvent.DefenderSide.NumRemainingSimulationTroops;
		int numRemainingSimulationTroops2 = mapEvent.AttackerSide.NumRemainingSimulationTroops;
		if (!mapEvent.IsInvulnerable)
		{
			if (eventType == MapEvent.BattleTypes.Siege && mapEventSettlement.CurrentSiegeState != Settlement.SiegeState.InTheLordsHall && ((mapEventSettlement.IsTown && numRemainingSimulationTroops > 100) || (mapEventSettlement.IsCastle && numRemainingSimulationTroops > 30)))
			{
				float num = GetSettlementAdvantage(mapEventSettlement) * 0.7f;
				item2 = TaleWorlds.Library.MathF.Round(1.5f + TaleWorlds.Library.MathF.Pow(numRemainingSimulationTroops, 0.3f)) * 2;
				item = TaleWorlds.Library.MathF.Round(0.5f + TaleWorlds.Library.MathF.Max(1f + TaleWorlds.Library.MathF.Pow(numRemainingSimulationTroops, 0.3f) * num, (float)((numRemainingSimulationTroops + 1) / (numRemainingSimulationTroops2 + 1)))) * 2;
			}
			else if (numRemainingSimulationTroops <= 10)
			{
				item = Math.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Min((float)numRemainingSimulationTroops2 * 3f, (float)numRemainingSimulationTroops * 0.3f)), 1);
				item2 = Math.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Min((float)numRemainingSimulationTroops * 3f, (float)numRemainingSimulationTroops2 * 0.3f)), 1);
			}
			else
			{
				item = TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Min((float)numRemainingSimulationTroops2 * 2f, TaleWorlds.Library.MathF.Pow(numRemainingSimulationTroops, 0.6f)));
				item2 = TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Min((float)numRemainingSimulationTroops * 2f, TaleWorlds.Library.MathF.Pow(numRemainingSimulationTroops2, 0.6f)));
			}
			if (mapEvent.RetreatingSide != BattleSideEnum.None)
			{
				if (mapEvent.RetreatingSide == BattleSideEnum.Attacker)
				{
					item2 = 0;
				}
				else
				{
					item = 0;
				}
			}
		}
		return (defenderRounds: item, attackerRounds: item2);
	}

	public override void GetBattleAdvantage(MapEvent mapEvent, out ExplainedNumber defenderAdvantage, out ExplainedNumber attackerAdvantage)
	{
		defenderAdvantage = GetPartyBattleAdvantage(mapEvent, mapEvent.DefenderSide.LeaderParty, mapEvent.AttackerSide.LeaderParty);
		attackerAdvantage = GetPartyBattleAdvantage(mapEvent, mapEvent.AttackerSide.LeaderParty, mapEvent.DefenderSide.LeaderParty);
		if (mapEvent.EventType == MapEvent.BattleTypes.Siege)
		{
			attackerAdvantage.AddFactor(-0.1f);
		}
	}

	private static ExplainedNumber GetPartyBattleAdvantage(MapEvent mapEvent, PartyBase party, PartyBase opposingParty)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(1f);
		if (party.LeaderHero != null)
		{
			if (!mapEvent.IsNavalMapEvent)
			{
				SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.TacticsAdvantage, party.LeaderHero.CharacterObject, ref explainedNumber);
			}
			if (party.IsMobile && opposingParty.Culture.IsBandit)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Patrols, party.MobileParty, isPrimaryBonus: false, ref explainedNumber);
			}
		}
		Hero perkOwnerHero = null;
		if (party.IsMobile && opposingParty.IsMobile && opposingParty.LeaderHero != null && party.MobileParty.HasPerk(DefaultPerks.Tactics.PreBattleManeuvers, out perkOwnerHero, checkSecondaryRole: true))
		{
			int num = perkOwnerHero.GetSkillValue(DefaultSkills.Tactics) - opposingParty.LeaderHero.GetSkillValue(DefaultSkills.Tactics);
			if (num > 0)
			{
				float value = (float)num * 0.01f;
				explainedNumber.Add(value);
			}
		}
		return explainedNumber;
	}

	public override float GetShipSiegeEngineHitChance(Ship ship, SiegeEngineType siegeEngineType, BattleSideEnum battleSide)
	{
		return 0f;
	}

	public override int GetPursuitRoundCount(MapEvent mapEvent)
	{
		return 4;
	}

	public override float GetBluntDamageChance(CharacterObject strikerTroop, CharacterObject strikedTroop, PartyBase strikerParty, PartyBase strikedParty, MapEvent battle)
	{
		if (battle.IsPlayerMapEvent)
		{
			return 0.3f;
		}
		return 0.1f;
	}

	public override CampaignTime GetSimulationTickInterval(MapEvent mapEvent)
	{
		if (mapEvent.IsSiegeAssault)
		{
			return CampaignTime.Minutes(60L);
		}
		return CampaignTime.Minutes(30L);
	}

	public override int GetParticipatingTroopCount(MapEventSide side)
	{
		return side.HealthyTroopCountAtMapEventStart;
	}

	public override float GetShipCombatImportance(Ship ship)
	{
		return 0f;
	}

	public override float GetShipCombatScore(Ship ship)
	{
		return 0f;
	}
}
