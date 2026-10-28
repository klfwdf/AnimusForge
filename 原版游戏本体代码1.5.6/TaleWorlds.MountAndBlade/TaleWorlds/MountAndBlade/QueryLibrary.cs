namespace TaleWorlds.MountAndBlade;

public static class QueryLibrary
{
	public static bool IsInfantry(Agent a)
	{
		if (!a.HasMount)
		{
			return !a.IsRangedCached;
		}
		return false;
	}

	public static bool IsInfantryWithoutBanner(Agent a)
	{
		if (a.Banner == null && !a.HasMount)
		{
			return !a.IsRangedCached;
		}
		return false;
	}

	public static bool HasShield(Agent a)
	{
		return a.HasShieldCached;
	}

	public static bool IsRanged(Agent a)
	{
		if (!a.HasMount)
		{
			return a.IsRangedCached;
		}
		return false;
	}

	public static bool IsRangedWithoutBanner(Agent a)
	{
		if (a.Banner == null && !a.HasMount)
		{
			return a.IsRangedCached;
		}
		return false;
	}

	public static bool IsCavalry(Agent a)
	{
		if (a.HasMount)
		{
			return !a.IsRangedCached;
		}
		return false;
	}

	public static bool IsCavalryWithoutBanner(Agent a)
	{
		if (a.Banner == null && a.HasMount)
		{
			return !a.IsRangedCached;
		}
		return false;
	}

	public static bool IsRangedCavalry(Agent a)
	{
		if (a.HasMount)
		{
			return a.IsRangedCached;
		}
		return false;
	}

	public static bool IsRangedCavalryWithoutBanner(Agent a)
	{
		if (a.Banner == null && a.HasMount)
		{
			return a.IsRangedCached;
		}
		return false;
	}

	public static bool HasSpear(Agent a)
	{
		return a.HasSpearCached;
	}

	public static bool HasThrown(Agent a)
	{
		return a.HasThrownCached;
	}

	public static bool IsHeavy(Agent a)
	{
		return MissionGameModels.Current.AgentStatCalculateModel.HasHeavyArmor(a);
	}
}
