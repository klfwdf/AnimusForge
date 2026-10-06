namespace TaleWorlds.Core;

public static class TroopTraitsMaskExtensions
{
	public static bool HasMelee(this TroopTraitsMask troopTraitsMask)
	{
		return (troopTraitsMask & TroopTraitsMask.Melee) != 0;
	}

	public static bool HasRanged(this TroopTraitsMask troopTraitsMask)
	{
		return (troopTraitsMask & TroopTraitsMask.Ranged) != 0;
	}

	public static bool HasMount(this TroopTraitsMask troopTraitsMask)
	{
		return (troopTraitsMask & TroopTraitsMask.Mount) != 0;
	}

	public static bool HasArmor(this TroopTraitsMask troopTraitsMask)
	{
		return (troopTraitsMask & TroopTraitsMask.Armor) != 0;
	}

	public static bool HasThrown(this TroopTraitsMask troopTraitsMask)
	{
		return (troopTraitsMask & TroopTraitsMask.Thrown) != 0;
	}

	public static bool HasSpear(this TroopTraitsMask troopTraitsMask)
	{
		return (troopTraitsMask & TroopTraitsMask.Spear) != 0;
	}

	public static bool HasShield(this TroopTraitsMask troopTraitsMask)
	{
		return (troopTraitsMask & TroopTraitsMask.Shield) != 0;
	}

	public static bool HasLowTier(this TroopTraitsMask troopFilterMask)
	{
		return (troopFilterMask & TroopTraitsMask.LowTier) != 0;
	}

	public static bool HasHighTier(this TroopTraitsMask troopFilterMask)
	{
		return (troopFilterMask & TroopTraitsMask.HighTier) != 0;
	}

	public static string GetTroopTraitsText(this TroopTraitsMask troopTraitsMask)
	{
		string text = "";
		if (troopTraitsMask.HasMelee())
		{
			AddFlagToText(TroopTraitsMask.Melee, ref text);
		}
		else if (troopTraitsMask.HasRanged())
		{
			AddFlagToText(TroopTraitsMask.Ranged, ref text);
		}
		if (troopTraitsMask.HasMount())
		{
			AddFlagToText(TroopTraitsMask.Mount, ref text);
		}
		if (troopTraitsMask.HasArmor())
		{
			AddFlagToText(TroopTraitsMask.Armor, ref text);
		}
		if (troopTraitsMask.HasThrown())
		{
			AddFlagToText(TroopTraitsMask.Thrown, ref text);
		}
		if (troopTraitsMask.HasSpear())
		{
			AddFlagToText(TroopTraitsMask.Spear, ref text);
		}
		if (troopTraitsMask.HasShield())
		{
			AddFlagToText(TroopTraitsMask.Shield, ref text);
		}
		return text;
	}

	public static string GetTraitsFilterText(this TroopTraitsMask troopTraitsFilter)
	{
		string text = "";
		if (troopTraitsFilter.HasArmor())
		{
			AddFlagToText(TroopTraitsMask.Armor, ref text);
		}
		if (troopTraitsFilter.HasThrown())
		{
			AddFlagToText(TroopTraitsMask.Thrown, ref text);
		}
		if (troopTraitsFilter.HasSpear())
		{
			AddFlagToText(TroopTraitsMask.Spear, ref text);
		}
		if (troopTraitsFilter.HasShield())
		{
			AddFlagToText(TroopTraitsMask.Shield, ref text);
		}
		if (troopTraitsFilter.HasLowTier())
		{
			AddFlagToText(TroopTraitsMask.LowTier, ref text);
		}
		else if (troopTraitsFilter.HasHighTier())
		{
			AddFlagToText(TroopTraitsMask.HighTier, ref text);
		}
		return text;
	}

	public static string GetClassFilterText(this TroopTraitsMask troopTraitsFilter)
	{
		string text = "";
		if (troopTraitsFilter.HasMelee())
		{
			AddFlagToText(TroopTraitsMask.Melee, ref text);
		}
		if (troopTraitsFilter.HasRanged())
		{
			AddFlagToText(TroopTraitsMask.Ranged, ref text);
		}
		if (troopTraitsFilter.HasMount())
		{
			AddFlagToText(TroopTraitsMask.Mount, ref text);
		}
		return text;
	}

	private static void AddFlagToText(TroopTraitsMask flag, ref string text)
	{
		if (text.Length > 0)
		{
			text += "|";
		}
		string text2 = "";
		text += (string?)(flag switch
		{
			TroopTraitsMask.Melee => "Melee", 
			TroopTraitsMask.Ranged => "Ranged", 
			TroopTraitsMask.Mount => "Mount", 
			TroopTraitsMask.Armor => "Armor", 
			TroopTraitsMask.Thrown => "Thrown", 
			TroopTraitsMask.Spear => "Spear", 
			TroopTraitsMask.Shield => "Shield", 
			TroopTraitsMask.LowTier => "Low Tier", 
			TroopTraitsMask.HighTier => "High Tier", 
			_ => "", 
		});
	}
}
