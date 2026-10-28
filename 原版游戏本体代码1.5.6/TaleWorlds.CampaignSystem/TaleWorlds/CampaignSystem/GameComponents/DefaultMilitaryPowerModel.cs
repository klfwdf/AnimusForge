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

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultMilitaryPowerModel : MilitaryPowerModel
{
	[Flags]
	private enum PowerFlags
	{
		None = 0,
		Attacker = 1,
		Defender = 2,
		Infantry = 4,
		Archer = 8,
		Cavalry = 0x10,
		HorseArcher = 0x20,
		Siege = 0x40,
		Village = 0x80,
		RiverCrossing = 0x100,
		Forest = 0x200,
		Flat = 0x400
	}

	private const float LowTierCaptainPerkPowerBoost = 0.01f;

	private const float MidTierCaptainPerkPowerBoost = 0.02f;

	private const float HighTierCaptainPerkPowerBoost = 0.03f;

	private const float UltraTierCaptainPerkPowerBoost = 0.06f;

	private static readonly Dictionary<uint, float> _battleModifiers = new Dictionary<uint, float>
	{
		{ 69u, 0f },
		{ 133u, 0.05f },
		{ 261u, 0f },
		{ 517u, 0.05f },
		{ 1029u, 0f },
		{ 70u, 0f },
		{ 134u, 0.05f },
		{ 262u, 0.05f },
		{ 518u, 0.05f },
		{ 1030u, 0f },
		{ 73u, -0.2f },
		{ 137u, -0.1f },
		{ 265u, 0f },
		{ 521u, -0.1f },
		{ 1033u, 0f },
		{ 74u, 0.3f },
		{ 138u, 0.05f },
		{ 266u, 0.1f },
		{ 522u, -0.5f },
		{ 1034u, 0f },
		{ 81u, -0.1f },
		{ 145u, 0f },
		{ 273u, -0.15f },
		{ 529u, -0.2f },
		{ 1041u, 0.25f },
		{ 82u, -0.1f },
		{ 146u, -0.1f },
		{ 274u, -0.05f },
		{ 530u, -0.15f },
		{ 1042u, 0.1f },
		{ 97u, -0.2f },
		{ 161u, 0.1f },
		{ 289u, -0.1f },
		{ 545u, -0.3f },
		{ 1057u, 0.3f },
		{ 98u, 0.3f },
		{ 162u, 0f },
		{ 290u, 0f },
		{ 546u, -0.25f },
		{ 1058u, 0.15f }
	};

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

	public override float GetPowerOfParty(PartyBase party, BattleSideEnum side, MapEvent.PowerCalculationContext context)
	{
		float num = 0f;
		float leaderModifier = party.LeaderHero?.PowerModifier ?? 0f;
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

	public override float GetPowerModifierOfHero(Hero leaderHero)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		if (leaderHero != null)
		{
			foreach (PerkObject item in PerkObject.All)
			{
				if (item.PrimaryRole == PartyRole.Captain && leaderHero.GetPerkValue(item))
				{
					float num5 = item.RequiredSkillValue / (float)Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus;
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

	public override float GetContextModifier(CharacterObject troop, BattleSideEnum battleSide, MapEvent.PowerCalculationContext context)
	{
		PowerFlags powerFlags = GetTroopPowerContext(troop);
		switch (context)
		{
		case MapEvent.PowerCalculationContext.PlainBattle:
		case MapEvent.PowerCalculationContext.SteppeBattle:
		case MapEvent.PowerCalculationContext.DesertBattle:
		case MapEvent.PowerCalculationContext.DuneBattle:
		case MapEvent.PowerCalculationContext.SnowBattle:
			powerFlags |= PowerFlags.Flat;
			break;
		case MapEvent.PowerCalculationContext.ForestBattle:
			powerFlags |= PowerFlags.Forest;
			break;
		case MapEvent.PowerCalculationContext.RiverCrossingBattle:
		case MapEvent.PowerCalculationContext.SeaBattle:
		case MapEvent.PowerCalculationContext.OpenSeaBattle:
		case MapEvent.PowerCalculationContext.RiverBattle:
			powerFlags |= PowerFlags.RiverCrossing;
			break;
		case MapEvent.PowerCalculationContext.Village:
		case MapEvent.PowerCalculationContext.NavalRaid:
			powerFlags |= PowerFlags.Village;
			break;
		case MapEvent.PowerCalculationContext.Siege:
			powerFlags |= PowerFlags.Siege;
			break;
		}
		powerFlags |= GetBattleSideContext(battleSide);
		return _battleModifiers[(uint)powerFlags];
	}

	public override MapEvent.PowerCalculationContext GetContextForPosition(CampaignVec2 position)
	{
		TerrainType terrainTypeAtPosition = Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(in position);
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
		case TerrainType.Lake:
			return MapEvent.PowerCalculationContext.RiverCrossingBattle;
		case TerrainType.Water:
			return MapEvent.PowerCalculationContext.SeaBattle;
		case TerrainType.River:
			return MapEvent.PowerCalculationContext.RiverCrossingBattle;
		case TerrainType.UnderBridge:
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
		case TerrainType.Fording:
			if (!position.IsOnLand)
			{
				return MapEvent.PowerCalculationContext.RiverCrossingBattle;
			}
			return MapEvent.PowerCalculationContext.PlainBattle;
		default:
			return MapEvent.PowerCalculationContext.PlainBattle;
		}
	}

	public override float GetDefaultTroopPower(CharacterObject troop)
	{
		int num = (troop.IsHero ? (troop.HeroObject.Level / 4 + 1) : troop.Tier);
		float num2 = (float)((2 + num) * (10 + num)) * 0.02f;
		if (troop.IsHero)
		{
			num2 *= 1.5f;
		}
		return num2;
	}

	public override float GetContextModifier(Ship ship, BattleSideEnum battleSideEnum, MapEvent.PowerCalculationContext context)
	{
		return 0f;
	}

	private PowerFlags GetTroopPowerContext(CharacterObject troop)
	{
		if (troop.HasMount())
		{
			if (!troop.IsRanged)
			{
				return PowerFlags.Cavalry;
			}
			return PowerFlags.HorseArcher;
		}
		if (troop.IsRanged)
		{
			return PowerFlags.Archer;
		}
		return PowerFlags.Infantry;
	}

	private PowerFlags GetBattleSideContext(BattleSideEnum battleSide)
	{
		if (battleSide != BattleSideEnum.Attacker)
		{
			return PowerFlags.Defender;
		}
		return PowerFlags.Attacker;
	}
}
