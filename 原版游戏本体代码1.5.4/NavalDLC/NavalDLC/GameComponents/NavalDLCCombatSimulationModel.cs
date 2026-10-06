using System;
using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCCombatSimulationModel : CombatSimulationModel
{
	public override CampaignTime GetSimulationTickInterval(MapEvent mapEvent)
	{
		if (mapEvent.IsNavalMapEvent)
		{
			return CampaignTime.Minutes(60L);
		}
		return base.BaseModel.GetSimulationTickInterval(mapEvent);
	}

	public override void GetBattleAdvantage(MapEvent mapEvent, out ExplainedNumber defenderAdvantage, out ExplainedNumber attackerAdvantage)
	{
		base.BaseModel.GetBattleAdvantage(mapEvent, out defenderAdvantage, out attackerAdvantage);
		if (!mapEvent.IsNavalMapEvent)
		{
			return;
		}
		PartyBase leaderParty = mapEvent.GetLeaderParty(BattleSideEnum.Defender);
		PartyBase leaderParty2 = mapEvent.GetLeaderParty(BattleSideEnum.Attacker);
		if (!leaderParty.IsMobile)
		{
			return;
		}
		SkillHelper.AddSkillBonusForParty(NavalSkillEffects.NavalAutoBattleSimulationAdvantage, leaderParty.MobileParty, ref defenderAdvantage);
		if (leaderParty2.IsMobile)
		{
			SkillHelper.AddSkillBonusForParty(NavalSkillEffects.NavalAutoBattleSimulationAdvantage, leaderParty.MobileParty, ref attackerAdvantage);
			if (leaderParty.MobileParty.IsBandit)
			{
				PerkHelper.AddPerkBonusForParty(NavalPerks.Mariner.PirateHunter, leaderParty2.MobileParty, isPrimaryBonus: true, ref attackerAdvantage);
			}
		}
	}

	public override int GetPursuitRoundCount(MapEvent mapEvent)
	{
		return base.BaseModel.GetPursuitRoundCount(mapEvent);
	}

	public override float GetMaximumSiegeEquipmentProgress(Settlement settlement)
	{
		return base.BaseModel.GetMaximumSiegeEquipmentProgress(settlement);
	}

	public override int GetNumberOfEquipmentsBuilt(Settlement settlement)
	{
		return base.BaseModel.GetNumberOfEquipmentsBuilt(settlement);
	}

	public override float GetSettlementAdvantage(Settlement settlement)
	{
		return base.BaseModel.GetSettlementAdvantage(settlement);
	}

	public override float GetShipSiegeEngineHitChance(Ship ship, SiegeEngineType siegeEngineType, BattleSideEnum battleSide)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0.3f);
		ShipHull.ShipType type = ship.ShipHull.Type;
		if (!siegeEngineType.IsRanged)
		{
			if (battleSide == BattleSideEnum.Attacker)
			{
				switch (type)
				{
				case ShipHull.ShipType.Light:
					explainedNumber.Add(0.05f);
					break;
				case ShipHull.ShipType.Heavy:
					explainedNumber.Add(-0.05f);
					break;
				}
			}
			else
			{
				switch (type)
				{
				case ShipHull.ShipType.Light:
					explainedNumber.Add(-0.05f);
					break;
				case ShipHull.ShipType.Heavy:
					explainedNumber.Add(0.05f);
					break;
				}
			}
		}
		else if (battleSide == BattleSideEnum.Defender)
		{
			switch (type)
			{
			case ShipHull.ShipType.Light:
				explainedNumber.Add(-0.1f);
				break;
			case ShipHull.ShipType.Heavy:
				explainedNumber.Add(0.1f);
				break;
			}
		}
		return explainedNumber.ResultNumber;
	}

	public override (int defenderRounds, int attackerRounds) GetSimulationTicksForBattleRound(MapEvent mapEvent)
	{
		if (mapEvent.IsNavalMapEvent)
		{
			MapEvent.BattleTypes eventType = mapEvent.EventType;
			Settlement mapEventSettlement = mapEvent.MapEventSettlement;
			int item = 0;
			int item2 = 0;
			if (!mapEvent.IsInvulnerable)
			{
				int totalCrewCapacity = GetTotalCrewCapacity(mapEvent.DefenderSide);
				int totalCrewCapacity2 = GetTotalCrewCapacity(mapEvent.AttackerSide);
				int num = Math.Min(mapEvent.DefenderSide.NumRemainingSimulationTroops, totalCrewCapacity);
				int num2 = Math.Min(mapEvent.AttackerSide.NumRemainingSimulationTroops, totalCrewCapacity2);
				if (eventType == MapEvent.BattleTypes.Siege && ((mapEventSettlement.IsTown && num > 100) || (mapEventSettlement.IsCastle && num > 30)))
				{
					float num3 = GetSettlementAdvantage(mapEventSettlement) * 0.7f;
					item2 = TaleWorlds.Library.MathF.Round(1.5f + TaleWorlds.Library.MathF.Pow(num, 0.3f)) * 2;
					item = TaleWorlds.Library.MathF.Round(0.5f + TaleWorlds.Library.MathF.Max(1f + TaleWorlds.Library.MathF.Pow(num, 0.3f) * num3, (float)((num + 1) / (num2 + 1)))) * 2;
				}
				else if (num <= 10)
				{
					item = Math.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Min((float)num2 * 3f, (float)num * 0.3f)), 1);
					item2 = Math.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Min((float)num * 3f, (float)num2 * 0.3f)), 1);
				}
				else
				{
					item = TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Min((float)num2 * 2f, TaleWorlds.Library.MathF.Pow(num, 0.6f)));
					item2 = TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Min((float)num * 2f, TaleWorlds.Library.MathF.Pow(num2, 0.6f)));
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
		if (mapEvent.IsRaid)
		{
			MobileParty mobileParty = mapEvent.AttackerSide.LeaderParty.MobileParty;
			if (mobileParty != null && mobileParty.IsCurrentlyAtSea)
			{
				int num4 = 0;
				int num5 = 0;
				int totalCrewCapacity3 = GetTotalCrewCapacity(mapEvent.AttackerSide);
				int num6 = Math.Min(mapEvent.AttackerSide.NumRemainingSimulationTroops, totalCrewCapacity3);
				int numRemainingSimulationTroops = mapEvent.DefenderSide.NumRemainingSimulationTroops;
				num4 = Math.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Min((float)num6 * 3f, (float)numRemainingSimulationTroops * 0.3f)), 1);
				num5 = Math.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Min((float)numRemainingSimulationTroops * 3f, (float)num6 * 0.3f)), 1);
				if (mapEvent.RetreatingSide != BattleSideEnum.None)
				{
					if (mapEvent.RetreatingSide == BattleSideEnum.Attacker)
					{
						num5 = 0;
					}
					else
					{
						Debug.FailedAssert("Defender cant retreat in raid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\GameComponents\\NavalDLCCombatSimulationModel.cs", "GetSimulationTicksForBattleRound", 204);
						num4 = 0;
					}
				}
				return (defenderRounds: num4, attackerRounds: num5);
			}
		}
		return base.BaseModel.GetSimulationTicksForBattleRound(mapEvent);
	}

	public override ExplainedNumber SimulateHit(CharacterObject strikerTroop, CharacterObject struckTroop, PartyBase strikerParty, PartyBase struckParty, float strikerAdvantage, MapEvent battle, BattleEnvironment battleEnvironment, float strikerSideMorale, float struckSideMorale)
	{
		ExplainedNumber result = base.BaseModel.SimulateHit(strikerTroop, struckTroop, strikerParty, struckParty, strikerAdvantage, battle, battleEnvironment, strikerSideMorale, struckSideMorale);
		if (battle.IsNavalMapEvent)
		{
			float weightedShipCombatFactor = battle.GetMapEventSide(strikerParty.Side).WeightedShipCombatFactor;
			result.AddFactor(weightedShipCombatFactor);
		}
		return result;
	}

	public override ExplainedNumber SimulateHit(Ship strikerShip, Ship struckShip, PartyBase strikerParty, PartyBase struckParty, SiegeEngineType siegeEngine, float strikerAdvantage, MapEvent battle, out int troopCasualties)
	{
		troopCasualties = 0;
		ExplainedNumber stat;
		if (siegeEngine.IsRanged)
		{
			stat = new ExplainedNumber(siegeEngine.Damage);
			troopCasualties = 1;
		}
		else
		{
			int num = 1;
			switch (strikerShip.ShipHull.Type)
			{
			case ShipHull.ShipType.Light:
				num = 1;
				break;
			case ShipHull.ShipType.Medium:
				num = 2;
				break;
			case ShipHull.ShipType.Heavy:
				num = 3;
				break;
			default:
				Debug.FailedAssert("Unhandled ship type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\GameComponents\\NavalDLCCombatSimulationModel.cs", "SimulateHit", 256);
				break;
			}
			stat = new ExplainedNumber(siegeEngine.Damage * num);
			if (struckParty.IsMobile)
			{
				PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.SeaborneFortress, struckParty.MobileParty, isPrimaryBonus: true, ref stat);
			}
		}
		Hero perkOwnerHero = null;
		if (strikerParty.IsMobile && strikerParty.MobileParty.HasPerk(DefaultPerks.Crossbow.Terror, out perkOwnerHero) && strikerParty.RandomFloatWithSeed((uint)battle.UpdateCount) < DefaultPerks.Crossbow.Terror.GetPrimaryBonus(strikerParty.MobileParty.CurrentBattleEnvironment))
		{
			troopCasualties++;
		}
		return stat;
	}

	private int GetTotalCrewCapacity(MapEventSide side)
	{
		int num = 0;
		for (int i = 0; i < side.SimulationShipList.Count; i++)
		{
			Ship ship = side.SimulationShipList[i];
			num += ship.MainDeckCrewCapacity;
		}
		return num;
	}

	public override float GetBluntDamageChance(CharacterObject strikerTroop, CharacterObject strikedTroop, PartyBase strikerParty, PartyBase strikedParty, MapEvent battle)
	{
		return base.BaseModel.GetBluntDamageChance(strikerTroop, strikedTroop, strikerParty, strikedParty, battle);
	}

	public override int GetParticipatingTroopCount(MapEventSide side)
	{
		int participatingTroopCount = base.BaseModel.GetParticipatingTroopCount(side);
		if (MapEventHelper.IsNavalRaid(side.MapEvent) && side.MissionSide == BattleSideEnum.Attacker && side.MapEvent.SimulationContext == MapEvent.PowerCalculationContext.NavalRaid)
		{
			return Math.Min(GetShallowShipDeckCrewCapacity(side), participatingTroopCount);
		}
		return participatingTroopCount;
	}

	public override float GetShipCombatImportance(Ship ship)
	{
		float num = TaleWorlds.Library.MathF.Clamp(ship.HitPoints / ship.MaxHitPoints, 0f, 1f);
		float y = ((num < 0.3f) ? 2f : 0.6f);
		return Campaign.Current.Models.CombatSimulationModel.GetShipCombatScore(ship) * TaleWorlds.Library.MathF.Pow(num, y);
	}

	public override float GetShipCombatScore(Ship ship)
	{
		float combatFactor = ship.GetCombatFactor();
		float num = TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Clamp(ship.HitPoints / ship.MaxHitPoints, 0f, 1f), 0.7f);
		return combatFactor * num;
	}

	private int GetShallowShipDeckCrewCapacity(MapEventSide side)
	{
		int num = 0;
		foreach (MapEventParty party in side.Parties)
		{
			foreach (Ship ship in party.Ships)
			{
				if (ship.ShipHull.CanNavigateShallowWater)
				{
					num += ship.MainDeckCrewCapacity;
				}
			}
		}
		return num;
	}
}
