using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200012A RID: 298
	public class DefaultMilitaryPowerModel : MilitaryPowerModel
	{
		// Token: 0x060018A4 RID: 6308 RVA: 0x00076DFC File Offset: 0x00074FFC
		public override float GetTroopPower(CharacterObject troop, BattleSideEnum side, MapEvent.PowerCalculationContext context, float leaderModifier)
		{
			float defaultTroopPower = Campaign.Current.Models.MilitaryPowerModel.GetDefaultTroopPower(troop);
			float num = 0f;
			if (context != MapEvent.PowerCalculationContext.Estimated)
			{
				num = Campaign.Current.Models.MilitaryPowerModel.GetContextModifier(troop, side, context);
			}
			return defaultTroopPower * (1f + leaderModifier + num);
		}

		// Token: 0x060018A5 RID: 6309 RVA: 0x00076E4C File Offset: 0x0007504C
		public override float GetPowerOfParty(PartyBase party, BattleSideEnum side, MapEvent.PowerCalculationContext context)
		{
			float num = 0f;
			Hero leaderHero = party.LeaderHero;
			float leaderModifier = ((leaderHero != null) ? leaderHero.PowerModifier : 0f);
			for (int i = 0; i < party.MemberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(i);
				if (elementCopyAtIndex.Character != null)
				{
					float troopPower = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(elementCopyAtIndex.Character, side, context, leaderModifier);
					num += (float)(elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber) * troopPower;
				}
			}
			float num2 = 1f;
			if (party.IsMobile)
			{
				if (context == MapEvent.PowerCalculationContext.Estimated)
				{
					num2 = MBMath.Map(party.MobileParty.Morale, 20f, 40f, 0.7f, 1f);
				}
				else if (party.MobileParty.Morale < 30f)
				{
					num2 = 0.7f;
				}
			}
			return num * num2;
		}

		// Token: 0x060018A6 RID: 6310 RVA: 0x00076F30 File Offset: 0x00075130
		public override float GetPowerModifierOfHero(Hero leaderHero)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			if (leaderHero != null)
			{
				foreach (PerkObject perkObject in PerkObject.All)
				{
					if (perkObject.PrimaryRole == PartyRole.Captain && leaderHero.GetPerkValue(perkObject))
					{
						float num5 = perkObject.RequiredSkillValue / (float)Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus;
						if (num5 <= 0.3f)
						{
							num++;
						}
						else if (num5 <= 0.6f)
						{
							num2++;
						}
						else if (num5 <= 0.9f)
						{
							num3++;
						}
						else
						{
							num4++;
						}
					}
				}
			}
			return (float)num * 0.01f + (float)num2 * 0.02f + (float)num3 * 0.03f + (float)num4 * 0.06f;
		}

		// Token: 0x060018A7 RID: 6311 RVA: 0x00077014 File Offset: 0x00075214
		public override float GetContextModifier(CharacterObject troop, BattleSideEnum battleSide, MapEvent.PowerCalculationContext context)
		{
			DefaultMilitaryPowerModel.PowerFlags powerFlags = this.GetTroopPowerContext(troop);
			switch (context)
			{
			case MapEvent.PowerCalculationContext.PlainBattle:
			case MapEvent.PowerCalculationContext.SteppeBattle:
			case MapEvent.PowerCalculationContext.DesertBattle:
			case MapEvent.PowerCalculationContext.DuneBattle:
			case MapEvent.PowerCalculationContext.SnowBattle:
				powerFlags |= DefaultMilitaryPowerModel.PowerFlags.Flat;
				break;
			case MapEvent.PowerCalculationContext.ForestBattle:
				powerFlags |= DefaultMilitaryPowerModel.PowerFlags.Forest;
				break;
			case MapEvent.PowerCalculationContext.RiverCrossingBattle:
			case MapEvent.PowerCalculationContext.SeaBattle:
			case MapEvent.PowerCalculationContext.OpenSeaBattle:
			case MapEvent.PowerCalculationContext.RiverBattle:
				powerFlags |= DefaultMilitaryPowerModel.PowerFlags.RiverCrossing;
				break;
			case MapEvent.PowerCalculationContext.Village:
				powerFlags |= DefaultMilitaryPowerModel.PowerFlags.Village;
				break;
			case MapEvent.PowerCalculationContext.Siege:
				powerFlags |= DefaultMilitaryPowerModel.PowerFlags.Siege;
				break;
			}
			powerFlags |= this.GetBattleSideContext(battleSide);
			return DefaultMilitaryPowerModel._battleModifiers[(uint)powerFlags];
		}

		// Token: 0x060018A8 RID: 6312 RVA: 0x000770A4 File Offset: 0x000752A4
		public override MapEvent.PowerCalculationContext GetContextForPosition(CampaignVec2 position)
		{
			TerrainType terrainTypeAtPosition = Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(position);
			if (position.IsOnLand)
			{
				MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(position.ToVec2());
				if (weatherEventInPosition == MapWeatherModel.WeatherEvent.Snowy || weatherEventInPosition == MapWeatherModel.WeatherEvent.Blizzard)
				{
					return MapEvent.PowerCalculationContext.SnowBattle;
				}
			}
			switch (terrainTypeAtPosition)
			{
			case TerrainType.Plain:
				return MapEvent.PowerCalculationContext.PlainBattle;
			case TerrainType.Desert:
				return MapEvent.PowerCalculationContext.DesertBattle;
			case TerrainType.Snow:
				return MapEvent.PowerCalculationContext.SnowBattle;
			case TerrainType.Forest:
				return MapEvent.PowerCalculationContext.ForestBattle;
			case TerrainType.Steppe:
				return MapEvent.PowerCalculationContext.SteppeBattle;
			case TerrainType.Fording:
				if (!position.IsOnLand)
				{
					return MapEvent.PowerCalculationContext.RiverCrossingBattle;
				}
				return MapEvent.PowerCalculationContext.PlainBattle;
			case TerrainType.Lake:
				return MapEvent.PowerCalculationContext.RiverCrossingBattle;
			case TerrainType.Water:
				return MapEvent.PowerCalculationContext.SeaBattle;
			case TerrainType.River:
				return MapEvent.PowerCalculationContext.RiverCrossingBattle;
			case TerrainType.Swamp:
				return MapEvent.PowerCalculationContext.PlainBattle;
			case TerrainType.Dune:
				return MapEvent.PowerCalculationContext.DuneBattle;
			case TerrainType.Bridge:
				return MapEvent.PowerCalculationContext.PlainBattle;
			case TerrainType.CoastalSea:
				return MapEvent.PowerCalculationContext.SeaBattle;
			case TerrainType.OpenSea:
				return MapEvent.PowerCalculationContext.OpenSeaBattle;
			case TerrainType.UnderBridge:
				return MapEvent.PowerCalculationContext.RiverCrossingBattle;
			}
			return MapEvent.PowerCalculationContext.PlainBattle;
		}

		// Token: 0x060018A9 RID: 6313 RVA: 0x0007718C File Offset: 0x0007538C
		public override float GetDefaultTroopPower(CharacterObject troop)
		{
			int num = (troop.IsHero ? (troop.HeroObject.Level / 4 + 1) : troop.Tier);
			return (float)((2 + num) * (10 + num)) * 0.02f * (troop.IsHero ? 1.5f : (troop.IsMounted ? 1.2f : 1f));
		}

		// Token: 0x060018AA RID: 6314 RVA: 0x000771EC File Offset: 0x000753EC
		public override float GetContextModifier(Ship ship, BattleSideEnum battleSideEnum, MapEvent.PowerCalculationContext context)
		{
			return 0f;
		}

		// Token: 0x060018AB RID: 6315 RVA: 0x000771F3 File Offset: 0x000753F3
		private DefaultMilitaryPowerModel.PowerFlags GetTroopPowerContext(CharacterObject troop)
		{
			if (troop.HasMount())
			{
				if (!troop.IsRanged)
				{
					return DefaultMilitaryPowerModel.PowerFlags.Cavalry;
				}
				return DefaultMilitaryPowerModel.PowerFlags.HorseArcher;
			}
			else
			{
				if (troop.IsRanged)
				{
					return DefaultMilitaryPowerModel.PowerFlags.Archer;
				}
				return DefaultMilitaryPowerModel.PowerFlags.Infantry;
			}
		}

		// Token: 0x060018AC RID: 6316 RVA: 0x00077216 File Offset: 0x00075416
		private DefaultMilitaryPowerModel.PowerFlags GetBattleSideContext(BattleSideEnum battleSide)
		{
			if (battleSide != BattleSideEnum.Attacker)
			{
				return DefaultMilitaryPowerModel.PowerFlags.Defender;
			}
			return DefaultMilitaryPowerModel.PowerFlags.Attacker;
		}

		// Token: 0x040007FB RID: 2043
		private const float LowTierCaptainPerkPowerBoost = 0.01f;

		// Token: 0x040007FC RID: 2044
		private const float MidTierCaptainPerkPowerBoost = 0.02f;

		// Token: 0x040007FD RID: 2045
		private const float HighTierCaptainPerkPowerBoost = 0.03f;

		// Token: 0x040007FE RID: 2046
		private const float UltraTierCaptainPerkPowerBoost = 0.06f;

		// Token: 0x040007FF RID: 2047
		private static readonly Dictionary<uint, float> _battleModifiers = new Dictionary<uint, float>
		{
			{ 69U, 0f },
			{ 133U, 0.05f },
			{ 261U, 0f },
			{ 517U, 0.05f },
			{ 1029U, 0f },
			{ 70U, 0f },
			{ 134U, 0.05f },
			{ 262U, 0.05f },
			{ 518U, 0.05f },
			{ 1030U, 0f },
			{ 73U, -0.2f },
			{ 137U, -0.1f },
			{ 265U, 0f },
			{ 521U, -0.1f },
			{ 1033U, 0f },
			{ 74U, 0.3f },
			{ 138U, 0.05f },
			{ 266U, 0.1f },
			{ 522U, -0.5f },
			{ 1034U, 0f },
			{ 81U, -0.1f },
			{ 145U, 0f },
			{ 273U, -0.15f },
			{ 529U, -0.2f },
			{ 1041U, 0.25f },
			{ 82U, -0.1f },
			{ 146U, -0.1f },
			{ 274U, -0.05f },
			{ 530U, -0.15f },
			{ 1042U, 0.1f },
			{ 97U, -0.2f },
			{ 161U, 0.1f },
			{ 289U, -0.1f },
			{ 545U, -0.3f },
			{ 1057U, 0.3f },
			{ 98U, 0.3f },
			{ 162U, 0f },
			{ 290U, 0f },
			{ 546U, -0.25f },
			{ 1058U, 0.15f }
		};

		// Token: 0x0200058C RID: 1420
		[Flags]
		private enum PowerFlags
		{
			// Token: 0x04001772 RID: 6002
			None = 0,
			// Token: 0x04001773 RID: 6003
			Attacker = 1,
			// Token: 0x04001774 RID: 6004
			Defender = 2,
			// Token: 0x04001775 RID: 6005
			Infantry = 4,
			// Token: 0x04001776 RID: 6006
			Archer = 8,
			// Token: 0x04001777 RID: 6007
			Cavalry = 16,
			// Token: 0x04001778 RID: 6008
			HorseArcher = 32,
			// Token: 0x04001779 RID: 6009
			Siege = 64,
			// Token: 0x0400177A RID: 6010
			Village = 128,
			// Token: 0x0400177B RID: 6011
			RiverCrossing = 256,
			// Token: 0x0400177C RID: 6012
			Forest = 512,
			// Token: 0x0400177D RID: 6013
			Flat = 1024
		}
	}
}
