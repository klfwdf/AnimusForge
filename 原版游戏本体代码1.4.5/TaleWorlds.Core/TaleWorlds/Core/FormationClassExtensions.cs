using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public static class FormationClassExtensions
{
	public const TroopUsageFlags DefaultInfantryTroopUsageFlags = TroopUsageFlags.OnFoot | TroopUsageFlags.Melee | TroopUsageFlags.OneHandedUser | TroopUsageFlags.ShieldUser | TroopUsageFlags.TwoHandedUser | TroopUsageFlags.PolearmUser;

	public const TroopUsageFlags DefaultRangedTroopUsageFlags = TroopUsageFlags.OnFoot | TroopUsageFlags.Ranged | TroopUsageFlags.BowUser | TroopUsageFlags.ThrownUser | TroopUsageFlags.CrossbowUser;

	public const TroopUsageFlags DefaultCavalryTroopUsageFlags = TroopUsageFlags.Mounted | TroopUsageFlags.Melee | TroopUsageFlags.OneHandedUser | TroopUsageFlags.ShieldUser | TroopUsageFlags.TwoHandedUser | TroopUsageFlags.PolearmUser;

	public const TroopUsageFlags DefaultHorseArcherTroopUsageFlags = TroopUsageFlags.Mounted | TroopUsageFlags.Ranged | TroopUsageFlags.BowUser | TroopUsageFlags.ThrownUser | TroopUsageFlags.CrossbowUser;

	public static FormationClass[] FormationClassValues = (FormationClass[])Enum.GetValues(typeof(FormationClass));

	public static string GetName(this FormationClass formationClass)
	{
		return formationClass switch
		{
			FormationClass.NumberOfDefaultFormations => "Skirmisher", 
			FormationClass.NumberOfRegularFormations => "General", 
			FormationClass.NumberOfAllFormations => "Unset", 
			_ => formationClass.ToString(), 
		};
	}

	public static TextObject GetLocalizedName(this FormationClass formationClass)
	{
		int num = (int)formationClass;
		return GameTexts.FindText("str_troop_group_name", num.ToString());
	}

	public static TroopUsageFlags GetTroopUsageFlags(this FormationClass troopClass)
	{
		return troopClass switch
		{
			FormationClass.Ranged => TroopUsageFlags.OnFoot | TroopUsageFlags.Ranged | TroopUsageFlags.BowUser | TroopUsageFlags.ThrownUser | TroopUsageFlags.CrossbowUser, 
			FormationClass.Cavalry => TroopUsageFlags.Mounted | TroopUsageFlags.Melee | TroopUsageFlags.OneHandedUser | TroopUsageFlags.ShieldUser | TroopUsageFlags.TwoHandedUser | TroopUsageFlags.PolearmUser, 
			FormationClass.HorseArcher => TroopUsageFlags.Mounted | TroopUsageFlags.Ranged | TroopUsageFlags.BowUser | TroopUsageFlags.ThrownUser | TroopUsageFlags.CrossbowUser, 
			_ => TroopUsageFlags.OnFoot | TroopUsageFlags.Melee | TroopUsageFlags.OneHandedUser | TroopUsageFlags.ShieldUser | TroopUsageFlags.TwoHandedUser | TroopUsageFlags.PolearmUser, 
		};
	}

	public static TroopType GetTroopTypeForRegularFormation(this FormationClass formationClass)
	{
		TroopType result = TroopType.Invalid;
		switch (formationClass)
		{
		case FormationClass.Infantry:
		case FormationClass.HeavyInfantry:
			result = TroopType.Infantry;
			break;
		case FormationClass.Ranged:
		case FormationClass.NumberOfDefaultFormations:
			result = TroopType.Ranged;
			break;
		case FormationClass.Cavalry:
		case FormationClass.HorseArcher:
		case FormationClass.LightCavalry:
		case FormationClass.HeavyCavalry:
			result = TroopType.Cavalry;
			break;
		default:
			Debug.FailedAssert($"Undefined formation class {formationClass} for TroopType!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\FormationClass.cs", "GetTroopTypeForRegularFormation", 321);
			break;
		}
		return result;
	}

	public static bool IsDefaultFormationClass(this FormationClass formationClass)
	{
		if (formationClass >= FormationClass.Infantry)
		{
			return formationClass < FormationClass.NumberOfDefaultFormations;
		}
		return false;
	}

	public static bool IsRegularFormationClass(this FormationClass formationClass)
	{
		if (formationClass >= FormationClass.Infantry)
		{
			return formationClass < FormationClass.NumberOfRegularFormations;
		}
		return false;
	}

	public static FormationClass FallbackClass(this FormationClass formationClass)
	{
		switch (formationClass)
		{
		case FormationClass.Ranged:
		case FormationClass.NumberOfDefaultFormations:
			return FormationClass.Ranged;
		case FormationClass.Cavalry:
		case FormationClass.HeavyCavalry:
			return FormationClass.Cavalry;
		case FormationClass.HorseArcher:
		case FormationClass.LightCavalry:
			return FormationClass.HorseArcher;
		default:
			return FormationClass.Infantry;
		}
	}
}
