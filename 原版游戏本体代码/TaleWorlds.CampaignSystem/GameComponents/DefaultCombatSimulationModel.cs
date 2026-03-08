using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000105 RID: 261
	public class DefaultCombatSimulationModel : CombatSimulationModel
	{
		// Token: 0x0600170D RID: 5901 RVA: 0x0006AEB8 File Offset: 0x000690B8
		public override ExplainedNumber SimulateHit(CharacterObject strikerTroop, CharacterObject struckTroop, PartyBase strikerParty, PartyBase struckParty, float strikerAdvantage, MapEvent battle, float strikerSideMorale, float struckSideMorale)
		{
			float troopPower = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(strikerTroop, strikerParty.Side, strikerParty.MapEvent.SimulationContext, strikerParty.MapEventSide.LeaderSimulationModifier);
			float troopPower2 = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(struckTroop, struckParty.Side, struckParty.MapEvent.SimulationContext, struckParty.MapEventSide.LeaderSimulationModifier);
			int num = (int)((0.5f + 0.5f * MBRandom.RandomFloat) * (40f * MathF.Pow(troopPower / troopPower2, 0.7f) * strikerAdvantage));
			ExplainedNumber result = new ExplainedNumber((float)num, false, null);
			if (strikerParty.IsMobile && struckParty.IsMobile)
			{
				DefaultCombatSimulationModel.CalculateSimulationDamagePerkEffects(strikerTroop, struckTroop, strikerParty.MobileParty, struckParty.MobileParty, ref result, battle);
			}
			DefaultCombatSimulationModel.CalculateSimulationMoraleEffects(strikerSideMorale, struckSideMorale, ref result);
			return result;
		}

		// Token: 0x0600170E RID: 5902 RVA: 0x0006AF96 File Offset: 0x00069196
		public override ExplainedNumber SimulateHit(Ship strikerShip, Ship struckShip, PartyBase strikerParty, PartyBase struckParty, SiegeEngineType siegeEngine, float strikerAdvantage, MapEvent battle, out int troopCasualties)
		{
			troopCasualties = 0;
			return new ExplainedNumber(0f, false, null);
		}

		// Token: 0x0600170F RID: 5903 RVA: 0x0006AFA8 File Offset: 0x000691A8
		private static void CalculateSimulationMoraleEffects(float strikerMorale, float struckMorale, ref ExplainedNumber effectiveDamage)
		{
			float num = MathF.Min(strikerMorale - 50f, 0f);
			float num2 = MathF.Max(struckMorale - 50f, 0f);
			effectiveDamage.AddFactor((num - num2) * 0.005f, null);
		}

		// Token: 0x06001710 RID: 5904 RVA: 0x0006AFEC File Offset: 0x000691EC
		private static void CalculateSimulationDamagePerkEffects(CharacterObject strikerTroop, CharacterObject struckTroop, MobileParty strikerParty, MobileParty struckParty, ref ExplainedNumber effectiveDamage, MapEvent battle)
		{
			if (!strikerParty.IsCurrentlyAtSea && strikerTroop.IsInfantry && struckTroop.IsMounted)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.TightFormations, strikerParty, true, ref effectiveDamage, false);
			}
			if (!strikerParty.IsCurrentlyAtSea && struckParty.HasPerk(DefaultPerks.Tactics.LooseFormations, false) && struckTroop.IsInfantry && strikerTroop.IsRanged)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.LooseFormations, struckParty, true, ref effectiveDamage, false);
			}
			TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(strikerParty.CurrentNavigationFace);
			if (faceTerrainType == TerrainType.Snow || faceTerrainType == TerrainType.Forest)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.ExtendedSkirmish, strikerParty, true, ref effectiveDamage, false);
			}
			if (faceTerrainType == TerrainType.Plain || faceTerrainType == TerrainType.Steppe || faceTerrainType == TerrainType.Desert)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.DecisiveBattle, strikerParty, true, ref effectiveDamage, false);
			}
			if (!strikerParty.IsCurrentlyAtSea && !strikerParty.IsBandit && struckParty.IsBandit)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.LawKeeper, strikerParty, true, ref effectiveDamage, false);
			}
			if (!strikerParty.IsCurrentlyAtSea)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Coaching, strikerParty, true, ref effectiveDamage, false);
			}
			if (!struckParty.IsCurrentlyAtSea && struckTroop.Tier >= 3)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.EliteReserves, struckParty, true, ref effectiveDamage, false);
			}
			if (!strikerParty.IsCurrentlyAtSea && strikerParty.MemberRoster.TotalHealthyCount > struckParty.MemberRoster.TotalHealthyCount)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Encirclement, strikerParty, true, ref effectiveDamage, false);
			}
			if (!strikerParty.IsCurrentlyAtSea && strikerParty.MemberRoster.TotalHealthyCount < struckParty.MemberRoster.TotalHealthyCount)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Counteroffensive, strikerParty, false, ref effectiveDamage, false);
			}
			bool flag = false;
			using (List<MapEventParty>.Enumerator enumerator = battle.PartiesOnSide(BattleSideEnum.Defender).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Party == struckParty.Party)
					{
						flag = true;
						break;
					}
				}
			}
			bool flag2 = !flag;
			bool flag3 = flag2;
			if (battle.IsSiegeAssault && flag2 && strikerParty.HasPerk(DefaultPerks.Tactics.Besieged, false))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Besieged, strikerParty, true, ref effectiveDamage, false);
			}
			if (flag && !strikerParty.IsCurrentlyAtSea && strikerParty.HasPerk(DefaultPerks.Scouting.Vanguard, false))
			{
				effectiveDamage.AddFactor(DefaultPerks.Scouting.Vanguard.PrimaryBonus, DefaultPerks.Scouting.Vanguard.Name);
			}
			if ((battle.IsSiegeOutside || battle.IsSallyOut) && flag3 && !strikerParty.IsCurrentlyAtSea)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Rearguard, strikerParty, false, ref effectiveDamage, false);
			}
			if (battle.IsSallyOut && flag && !strikerParty.IsCurrentlyAtSea && strikerParty.HasPerk(DefaultPerks.Scouting.Vanguard, true))
			{
				effectiveDamage.AddFactor(DefaultPerks.Scouting.Vanguard.SecondaryBonus, DefaultPerks.Scouting.Vanguard.Name);
			}
			if (battle.IsFieldBattle && flag2 && !strikerParty.IsCurrentlyAtSea)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Counteroffensive, strikerParty, true, ref effectiveDamage, false);
			}
			if (strikerParty.Army != null && strikerParty.LeaderHero != null && strikerParty.Army.LeaderParty == strikerParty)
			{
				PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Tactics.TacticalMastery, strikerParty.LeaderHero.CharacterObject, DefaultSkills.Tactics, true, ref effectiveDamage, Campaign.Current.Models.CharacterDevelopmentModel.MinSkillRequiredForEpicPerkBonus, strikerParty.IsCurrentlyAtSea);
			}
		}

		// Token: 0x06001711 RID: 5905 RVA: 0x0006B2F4 File Offset: 0x000694F4
		public override float GetMaximumSiegeEquipmentProgress(Settlement settlement)
		{
			float num = 0f;
			if (settlement.SiegeEvent != null && settlement.IsFortification)
			{
				foreach (SiegeEvent.SiegeEngineConstructionProgress siegeEngineConstructionProgress in settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.AllSiegeEngines())
				{
					if (!siegeEngineConstructionProgress.IsConstructed && siegeEngineConstructionProgress.Progress > num)
					{
						num = siegeEngineConstructionProgress.Progress;
					}
				}
			}
			return num;
		}

		// Token: 0x06001712 RID: 5906 RVA: 0x0006B37C File Offset: 0x0006957C
		public override int GetNumberOfEquipmentsBuilt(Settlement settlement)
		{
			if (settlement.SiegeEvent != null && settlement.IsFortification)
			{
				bool flag = false;
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				foreach (SiegeEvent.SiegeEngineConstructionProgress siegeEngineConstructionProgress in settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.AllSiegeEngines())
				{
					if (siegeEngineConstructionProgress.IsConstructed)
					{
						if (siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.Ram)
						{
							flag = true;
						}
						else if (siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.SiegeTower)
						{
							num++;
						}
						else if (siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.Trebuchet || siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.Onager || siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.Ballista)
						{
							num2++;
						}
						else if (siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.FireOnager || siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.FireBallista)
						{
							num3++;
						}
					}
				}
				return (flag ? 1 : 0) + num + num2 + num3;
			}
			return 0;
		}

		// Token: 0x06001713 RID: 5907 RVA: 0x0006B48C File Offset: 0x0006968C
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
				foreach (SiegeEvent.SiegeEngineConstructionProgress siegeEngineConstructionProgress in settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.AllSiegeEngines())
				{
					if (siegeEngineConstructionProgress.IsConstructed)
					{
						if (siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.Ram || siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.ImprovedRam)
						{
							if (siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.ImprovedRam)
							{
								flag2 = true;
							}
							flag = true;
						}
						else if (siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.SiegeTower)
						{
							num++;
						}
						else if (siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.Trebuchet || siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.Onager || siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.Ballista)
						{
							num2++;
						}
						else if (siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.FireOnager || siegeEngineConstructionProgress.SiegeEngine == DefaultSiegeEngineTypes.FireBallista)
						{
							num3++;
						}
					}
				}
				float num4 = 4f + (float)(wallLevel - 1);
				if (settlement.SettlementTotalWallHitPoints < 1E-05f)
				{
					num4 *= 0.25f;
				}
				float num5 = 1f + num4;
				float num6 = 1f + ((flag | (num > 0)) ? 0.25f : 0f) + (flag2 ? 0.24f : (flag ? 0.16f : 0f)) + ((num > 1) ? 0.24f : ((num == 1) ? 0.16f : 0f)) + (float)num2 * 0.08f + (float)num3 * 0.12f;
				float baseNumber = num5 / num6;
				ExplainedNumber explainedNumber = new ExplainedNumber(baseNumber, false, null);
				ISiegeEventSide siegeEventSide = settlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker);
				DefaultCombatSimulationModel.CalculateSettlementAdvantagePerkEffects(settlement, ref explainedNumber, siegeEventSide);
				return explainedNumber.ResultNumber;
			}
			if (settlement.IsVillage)
			{
				return 1.25f;
			}
			return 1f;
		}

		// Token: 0x06001714 RID: 5908 RVA: 0x0006B694 File Offset: 0x00069894
		private static void CalculateSettlementAdvantagePerkEffects(Settlement settlement, ref ExplainedNumber effectiveAdvantage, ISiegeEventSide opposingSide)
		{
			if (opposingSide.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege).Any((PartyBase x) => x.MobileParty.HasPerk(DefaultPerks.Tactics.OnTheMarch, false) && !x.MobileParty.IsCurrentlyAtSea))
			{
				effectiveAdvantage.AddFactor(DefaultPerks.Tactics.OnTheMarch.PrimaryBonus, DefaultPerks.Tactics.OnTheMarch.Name);
			}
			if (PerkHelper.GetPerkValueForTown(DefaultPerks.Tactics.OnTheMarch, settlement.Town))
			{
				PerkHelper.AddPerkBonusForTown(DefaultPerks.Tactics.OnTheMarch, settlement.Town, ref effectiveAdvantage);
			}
		}

		// Token: 0x06001715 RID: 5909 RVA: 0x0006B70C File Offset: 0x0006990C
		[return: TupleElementNames(new string[] { "defenderRounds", "attackerRounds" })]
		public override ValueTuple<int, int> GetSimulationTicksForBattleRound(MapEvent mapEvent)
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
					float num = this.GetSettlementAdvantage(mapEventSettlement) * 0.7f;
					item2 = MathF.Round(1.5f + MathF.Pow((float)numRemainingSimulationTroops, 0.3f)) * 2;
					item = MathF.Round(0.5f + MathF.Max(1f + MathF.Pow((float)numRemainingSimulationTroops, 0.3f) * num, (float)((numRemainingSimulationTroops + 1) / (numRemainingSimulationTroops2 + 1)))) * 2;
				}
				else if (numRemainingSimulationTroops <= 10)
				{
					item = Math.Max(MathF.Round(MathF.Min((float)numRemainingSimulationTroops2 * 3f, (float)numRemainingSimulationTroops * 0.3f)), 1);
					item2 = Math.Max(MathF.Round(MathF.Min((float)numRemainingSimulationTroops * 3f, (float)numRemainingSimulationTroops2 * 0.3f)), 1);
				}
				else
				{
					item = MathF.Round(MathF.Min((float)numRemainingSimulationTroops2 * 2f, MathF.Pow((float)numRemainingSimulationTroops, 0.6f)));
					item2 = MathF.Round(MathF.Min((float)numRemainingSimulationTroops * 2f, MathF.Pow((float)numRemainingSimulationTroops2, 0.6f)));
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
			return new ValueTuple<int, int>(item, item2);
		}

		// Token: 0x06001716 RID: 5910 RVA: 0x0006B890 File Offset: 0x00069A90
		public override void GetBattleAdvantage(MapEvent mapEvent, out ExplainedNumber defenderAdvantage, out ExplainedNumber attackerAdvantage)
		{
			defenderAdvantage = DefaultCombatSimulationModel.GetPartyBattleAdvantage(mapEvent, mapEvent.DefenderSide.LeaderParty, mapEvent.AttackerSide.LeaderParty);
			attackerAdvantage = DefaultCombatSimulationModel.GetPartyBattleAdvantage(mapEvent, mapEvent.AttackerSide.LeaderParty, mapEvent.DefenderSide.LeaderParty);
			if (mapEvent.EventType == MapEvent.BattleTypes.Siege)
			{
				attackerAdvantage.AddFactor(-0.1f, null);
			}
		}

		// Token: 0x06001717 RID: 5911 RVA: 0x0006B8F8 File Offset: 0x00069AF8
		private static ExplainedNumber GetPartyBattleAdvantage(MapEvent mapEvent, PartyBase party, PartyBase opposingParty)
		{
			ExplainedNumber result = new ExplainedNumber(1f, false, null);
			if (party.LeaderHero != null)
			{
				if (!mapEvent.IsNavalMapEvent)
				{
					SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.TacticsAdvantage, party.LeaderHero.CharacterObject, ref result);
				}
				if (party.IsMobile && opposingParty.Culture.IsBandit && !party.MobileParty.IsCurrentlyAtSea)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Patrols, party.MobileParty, false, ref result, false);
				}
			}
			if (party.IsMobile && !party.MobileParty.IsCurrentlyAtSea && opposingParty.IsMobile && party.LeaderHero != null && opposingParty.LeaderHero != null && party.MobileParty.HasPerk(DefaultPerks.Tactics.PreBattleManeuvers, true))
			{
				int num = party.LeaderHero.GetSkillValue(DefaultSkills.Tactics) - opposingParty.LeaderHero.GetSkillValue(DefaultSkills.Tactics);
				if (num > 0)
				{
					result.Add((float)num * 0.01f, null, null);
				}
			}
			return result;
		}

		// Token: 0x06001718 RID: 5912 RVA: 0x0006B9E8 File Offset: 0x00069BE8
		public override float GetShipSiegeEngineHitChance(Ship ship, SiegeEngineType siegeEngineType, BattleSideEnum battleSide)
		{
			return 0f;
		}

		// Token: 0x06001719 RID: 5913 RVA: 0x0006B9EF File Offset: 0x00069BEF
		public override int GetPursuitRoundCount(MapEvent mapEvent)
		{
			return 4;
		}

		// Token: 0x0600171A RID: 5914 RVA: 0x0006B9F2 File Offset: 0x00069BF2
		public override float GetBluntDamageChance(CharacterObject strikerTroop, CharacterObject strikedTroop, PartyBase strikerParty, PartyBase strikedParty, MapEvent battle)
		{
			return 0.1f;
		}
	}
}
