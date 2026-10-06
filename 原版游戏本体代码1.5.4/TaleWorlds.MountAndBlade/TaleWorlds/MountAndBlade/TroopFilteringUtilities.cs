using System;
using System.Linq;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public static class TroopFilteringUtilities
{
	public const int MinPriority = 1;

	public const int EquipmentPriority = 10;

	public const int EngagementTypePriority = 100;

	public const int MountedPriority = 1000;

	public static TroopTraitsMask GetFilter(bool isMounted, bool isRanged, bool isMelee, bool hasHeavyArmor, bool hasThrown, bool hasSpear, bool hasShield)
	{
		TroopTraitsMask troopTraitsMask = TroopTraitsMask.None;
		if (hasHeavyArmor)
		{
			troopTraitsMask |= TroopTraitsMask.Armor;
		}
		if (hasThrown)
		{
			troopTraitsMask |= TroopTraitsMask.Thrown;
		}
		if (hasSpear)
		{
			troopTraitsMask |= TroopTraitsMask.Spear;
		}
		if (hasShield)
		{
			troopTraitsMask |= TroopTraitsMask.Shield;
		}
		if (isMelee)
		{
			troopTraitsMask |= TroopTraitsMask.Melee;
		}
		if (isRanged)
		{
			troopTraitsMask |= TroopTraitsMask.Ranged;
		}
		if (isMounted)
		{
			troopTraitsMask |= TroopTraitsMask.Mount;
		}
		return troopTraitsMask;
	}

	public static TroopTraitsMask GetFilter(params FormationClass[] formationClasses)
	{
		TroopTraitsMask result = TroopTraitsMask.None;
		if (formationClasses.Length == 1)
		{
			switch (formationClasses[0])
			{
			case FormationClass.Infantry:
				result = TroopTraitsMask.Melee;
				break;
			case FormationClass.Ranged:
				result = TroopTraitsMask.Ranged;
				break;
			case FormationClass.Cavalry:
				result = TroopTraitsMask.Melee | TroopTraitsMask.Mount;
				break;
			case FormationClass.HorseArcher:
				result = TroopTraitsMask.Ranged | TroopTraitsMask.Mount;
				break;
			}
		}
		else if (formationClasses.Length == 2)
		{
			if (formationClasses[0] == FormationClass.Infantry && formationClasses[1] == FormationClass.Ranged)
			{
				result = TroopTraitsMask.Melee | TroopTraitsMask.Ranged;
			}
			if (formationClasses[0] == FormationClass.Cavalry && formationClasses[1] == FormationClass.HorseArcher)
			{
				result = TroopTraitsMask.Melee | TroopTraitsMask.Ranged | TroopTraitsMask.Mount;
			}
		}
		return result;
	}

	public static TroopTraitsMask GetFilter(params FormationFilterType[] filterTypes)
	{
		TroopTraitsMask troopTraitsMask = TroopTraitsMask.None;
		if (filterTypes.Length != 0)
		{
			if (filterTypes.Any((FormationFilterType f) => f == FormationFilterType.Heavy))
			{
				troopTraitsMask |= TroopTraitsMask.Armor;
			}
			if (filterTypes.Any((FormationFilterType f) => f == FormationFilterType.Shield))
			{
				troopTraitsMask |= TroopTraitsMask.Shield;
			}
			if (filterTypes.Any((FormationFilterType f) => f == FormationFilterType.Thrown))
			{
				troopTraitsMask |= TroopTraitsMask.Thrown;
			}
			if (filterTypes.Any((FormationFilterType f) => f == FormationFilterType.Spear))
			{
				troopTraitsMask |= TroopTraitsMask.Spear;
			}
			if (filterTypes.Any((FormationFilterType f) => f == FormationFilterType.HighTier))
			{
				troopTraitsMask |= TroopTraitsMask.HighTier;
			}
			if (filterTypes.Any((FormationFilterType f) => f == FormationFilterType.LowTier))
			{
				troopTraitsMask |= TroopTraitsMask.LowTier;
			}
		}
		return troopTraitsMask;
	}

	public static void GetPriorityFunction(TroopTraitsMask filter, out Func<Agent, int> priorityFunc)
	{
		priorityFunc = (Agent agent) => (agent == null || agent.Character == null) ? GetMaxPriority(filter) : GetTroopPriority(agent.GetTraitsMask(), agent.Character.GetBattleTier(), filter);
	}

	public static void GetPriorityFunction(TroopTraitsMask filter, out Func<IAgentOriginBase, int> priorityFunc)
	{
		priorityFunc = (IAgentOriginBase agentOrigin) => (agentOrigin == null || agentOrigin.Troop == null) ? GetMaxPriority(filter) : GetTroopPriority(agentOrigin.GetTraitsMask(), agentOrigin.Troop.GetBattleTier(), filter);
	}

	public static int GetTroopPriority(TroopTraitsMask troopMask, int battleTier, TroopTraitsMask filter)
	{
		int num = 1;
		if ((filter & TroopTraitsMask.HighTier) != TroopTraitsMask.None)
		{
			num += battleTier;
		}
		if ((filter & TroopTraitsMask.LowTier) != TroopTraitsMask.None)
		{
			num += 7 - battleTier;
		}
		TroopTraitsMask troopTraitsMask = filter & troopMask;
		if ((troopTraitsMask & TroopTraitsMask.Shield) != TroopTraitsMask.None)
		{
			num += 10;
		}
		if ((troopTraitsMask & TroopTraitsMask.Spear) != TroopTraitsMask.None)
		{
			num += 10;
		}
		if ((troopTraitsMask & TroopTraitsMask.Thrown) != TroopTraitsMask.None)
		{
			num += 10;
		}
		if ((troopTraitsMask & TroopTraitsMask.Armor) != TroopTraitsMask.None)
		{
			num += 10;
		}
		if ((troopTraitsMask & TroopTraitsMask.Melee) != TroopTraitsMask.None || (troopTraitsMask & TroopTraitsMask.Ranged) != TroopTraitsMask.None)
		{
			num += 100;
		}
		if ((troopTraitsMask & TroopTraitsMask.Mount) != TroopTraitsMask.None)
		{
			num += 1000;
		}
		return num;
	}

	public static int GetMaxPriority(TroopTraitsMask filter)
	{
		return 1 + (((filter & TroopTraitsMask.HighTier) != TroopTraitsMask.None) ? 7 : 0) + (((filter & TroopTraitsMask.LowTier) != TroopTraitsMask.None) ? 7 : 0) + (((filter & TroopTraitsMask.Shield) != TroopTraitsMask.None) ? 10 : 0) + (((filter & TroopTraitsMask.Spear) != TroopTraitsMask.None) ? 10 : 0) + (((filter & TroopTraitsMask.Thrown) != TroopTraitsMask.None) ? 10 : 0) + (((filter & TroopTraitsMask.Armor) != TroopTraitsMask.None) ? 10 : 0) + (((filter & TroopTraitsMask.Melee) != TroopTraitsMask.None || (filter & TroopTraitsMask.Ranged) != TroopTraitsMask.None) ? 100 : 0) + (((filter & TroopTraitsMask.Mount) != TroopTraitsMask.None) ? 1000 : 0);
	}
}
