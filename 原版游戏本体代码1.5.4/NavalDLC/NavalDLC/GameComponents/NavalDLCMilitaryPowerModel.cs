using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace NavalDLC.GameComponents;

public class NavalDLCMilitaryPowerModel : MilitaryPowerModel
{
	private const float MarinerTroopSeaBattlePowerBonus = 1.2f;

	private static readonly Dictionary<MapEvent.PowerCalculationContext, float> _lightShipAttackerModifiers = new Dictionary<MapEvent.PowerCalculationContext, float>
	{
		{
			MapEvent.PowerCalculationContext.SeaBattle,
			0.2f
		},
		{
			MapEvent.PowerCalculationContext.OpenSeaBattle,
			-0.2f
		},
		{
			MapEvent.PowerCalculationContext.RiverBattle,
			0.2f
		}
	};

	private static readonly Dictionary<MapEvent.PowerCalculationContext, float> _lightShipDefenderModifiers = new Dictionary<MapEvent.PowerCalculationContext, float>
	{
		{
			MapEvent.PowerCalculationContext.SeaBattle,
			0.2f
		},
		{
			MapEvent.PowerCalculationContext.OpenSeaBattle,
			-0.2f
		},
		{
			MapEvent.PowerCalculationContext.RiverBattle,
			0.2f
		}
	};

	private static readonly Dictionary<MapEvent.PowerCalculationContext, float> _mediumShipAttackerModifiers = new Dictionary<MapEvent.PowerCalculationContext, float>
	{
		{
			MapEvent.PowerCalculationContext.SeaBattle,
			0f
		},
		{
			MapEvent.PowerCalculationContext.OpenSeaBattle,
			0f
		},
		{
			MapEvent.PowerCalculationContext.RiverBattle,
			0f
		}
	};

	private static readonly Dictionary<MapEvent.PowerCalculationContext, float> _mediumShipDefenderModifiers = new Dictionary<MapEvent.PowerCalculationContext, float>
	{
		{
			MapEvent.PowerCalculationContext.SeaBattle,
			0f
		},
		{
			MapEvent.PowerCalculationContext.OpenSeaBattle,
			0f
		},
		{
			MapEvent.PowerCalculationContext.RiverBattle,
			0f
		}
	};

	private static readonly Dictionary<MapEvent.PowerCalculationContext, float> _heavyShipAttackerModifiers = new Dictionary<MapEvent.PowerCalculationContext, float>
	{
		{
			MapEvent.PowerCalculationContext.SeaBattle,
			-0.2f
		},
		{
			MapEvent.PowerCalculationContext.OpenSeaBattle,
			0.2f
		},
		{
			MapEvent.PowerCalculationContext.RiverBattle,
			-0.2f
		}
	};

	private static readonly Dictionary<MapEvent.PowerCalculationContext, float> _heavyShipDefenderModifiers = new Dictionary<MapEvent.PowerCalculationContext, float>
	{
		{
			MapEvent.PowerCalculationContext.SeaBattle,
			-0.2f
		},
		{
			MapEvent.PowerCalculationContext.OpenSeaBattle,
			0.2f
		},
		{
			MapEvent.PowerCalculationContext.RiverBattle,
			-0.2f
		}
	};

	public override float GetPowerOfParty(PartyBase party, BattleSideEnum side, MapEvent.PowerCalculationContext context)
	{
		float num = base.BaseModel.GetPowerOfParty(party, side, context);
		switch (context)
		{
		case MapEvent.PowerCalculationContext.SeaBattle:
		case MapEvent.PowerCalculationContext.OpenSeaBattle:
		case MapEvent.PowerCalculationContext.RiverBattle:
		{
			if (party.Ships.Count == 0)
			{
				return 0f;
			}
			float num2 = party.Ships.AverageQ((Ship x) => x.GetCombatFactor());
			num *= num2;
			num *= GetTroopAccommodationRatio(party);
			break;
		}
		case MapEvent.PowerCalculationContext.Estimated:
			if (party.IsMobile && party.MobileParty.IsCurrentlyAtSea)
			{
				num *= GetTroopAccommodationRatio(party);
			}
			break;
		}
		return num;
	}

	public override float GetContextModifier(CharacterObject troop, BattleSideEnum battleSideEnum, MapEvent.PowerCalculationContext context)
	{
		switch (context)
		{
		case MapEvent.PowerCalculationContext.SeaBattle:
		case MapEvent.PowerCalculationContext.OpenSeaBattle:
		case MapEvent.PowerCalculationContext.RiverBattle:
			return 0f;
		case MapEvent.PowerCalculationContext.NavalRaid:
			switch (battleSideEnum)
			{
			case BattleSideEnum.Defender:
				if (troop.IsRanged)
				{
					return 0.1f;
				}
				break;
			case BattleSideEnum.Attacker:
				if (troop.IsRanged && troop.HasMount())
				{
					return -0.5f;
				}
				break;
			}
			break;
		}
		return base.BaseModel.GetContextModifier(troop, battleSideEnum, context);
	}

	public override float GetContextModifier(Ship ship, BattleSideEnum battleSide, MapEvent.PowerCalculationContext context)
	{
		if (context == MapEvent.PowerCalculationContext.SeaBattle || context == MapEvent.PowerCalculationContext.OpenSeaBattle || context == MapEvent.PowerCalculationContext.RiverBattle)
		{
			switch (ship.ShipHull.Type)
			{
			case ShipHull.ShipType.Light:
				return GetLightShipContextModifier(ship, battleSide, context);
			case ShipHull.ShipType.Medium:
				return GetMediumShipContextModifier(ship, battleSide, context);
			case ShipHull.ShipType.Heavy:
				return GetHeavyShipContextModifier(ship, battleSide, context);
			}
			Debug.FailedAssert("unhandled ship type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\GameComponents\\NavalDLCMilitaryPowerModel.cs", "GetContextModifier", 136);
		}
		return base.BaseModel.GetContextModifier(ship, battleSide, context);
	}

	public override MapEvent.PowerCalculationContext GetContextForPosition(CampaignVec2 position)
	{
		return base.BaseModel.GetContextForPosition(position);
	}

	public override float GetDefaultTroopPower(CharacterObject troop)
	{
		return base.BaseModel.GetDefaultTroopPower(troop);
	}

	public override float GetPowerModifierOfHero(Hero leaderHero)
	{
		return base.BaseModel.GetPowerModifierOfHero(leaderHero);
	}

	public override float GetTroopPower(CharacterObject troop, BattleSideEnum side, MapEvent.PowerCalculationContext context, float leaderModifier)
	{
		float num = base.BaseModel.GetTroopPower(troop, side, context, leaderModifier);
		if ((context == MapEvent.PowerCalculationContext.SeaBattle || context == MapEvent.PowerCalculationContext.NavalRaid) && !troop.IsHero && troop.IsMariner)
		{
			num *= 1.2f;
		}
		return num;
	}

	private float GetLightShipContextModifier(Ship ship, BattleSideEnum battleSide, MapEvent.PowerCalculationContext context)
	{
		if (battleSide != BattleSideEnum.Attacker)
		{
			return _lightShipDefenderModifiers[context];
		}
		return _lightShipAttackerModifiers[context];
	}

	private float GetMediumShipContextModifier(Ship ship, BattleSideEnum battleSide, MapEvent.PowerCalculationContext context)
	{
		if (battleSide != BattleSideEnum.Attacker)
		{
			return _mediumShipDefenderModifiers[context];
		}
		return _mediumShipAttackerModifiers[context];
	}

	private float GetHeavyShipContextModifier(Ship ship, BattleSideEnum battleSide, MapEvent.PowerCalculationContext context)
	{
		if (battleSide != BattleSideEnum.Attacker)
		{
			return _heavyShipDefenderModifiers[context];
		}
		return _heavyShipAttackerModifiers[context];
	}

	private float GetTroopAccommodationRatio(PartyBase party)
	{
		float result = 1f;
		float num = party.Ships.SumQ((Ship x) => x.TotalCrewCapacity);
		if ((float)party.NumberOfAllMembers > num)
		{
			result = num / (float)party.NumberOfAllMembers;
		}
		return result;
	}
}
