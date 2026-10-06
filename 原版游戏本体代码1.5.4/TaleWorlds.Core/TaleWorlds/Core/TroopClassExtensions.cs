namespace TaleWorlds.Core;

public static class TroopClassExtensions
{
	public static bool IsRanged(this FormationClass troopClass)
	{
		FormationClass formationClass = troopClass.DefaultClass();
		if (formationClass != FormationClass.Ranged)
		{
			return formationClass == FormationClass.HorseArcher;
		}
		return true;
	}

	public static bool IsMounted(this FormationClass troopClass)
	{
		if (troopClass.DefaultClass() != FormationClass.Cavalry)
		{
			return troopClass == FormationClass.HorseArcher;
		}
		return true;
	}

	public static bool IsMeleeInfantry(this FormationClass troopClass)
	{
		return troopClass.DefaultClass() == FormationClass.Infantry;
	}

	public static bool IsMeleeCavalry(this FormationClass troopClass)
	{
		return troopClass.DefaultClass() == FormationClass.Cavalry;
	}

	public static FormationClass DefaultClass(this FormationClass troopClass)
	{
		if (troopClass.IsRegularFormationClass())
		{
			FormationClass result = troopClass;
			switch (troopClass)
			{
			case FormationClass.HeavyInfantry:
				result = FormationClass.Infantry;
				break;
			case FormationClass.NumberOfDefaultFormations:
				result = FormationClass.Ranged;
				break;
			case FormationClass.HeavyCavalry:
				result = FormationClass.Cavalry;
				break;
			case FormationClass.LightCavalry:
				result = FormationClass.HorseArcher;
				break;
			}
			return result;
		}
		return FormationClass.Infantry;
	}

	public static FormationClass AlternativeClass(this FormationClass troopClass)
	{
		return troopClass switch
		{
			FormationClass.Infantry => FormationClass.Ranged, 
			FormationClass.Ranged => FormationClass.Infantry, 
			FormationClass.Cavalry => FormationClass.HorseArcher, 
			FormationClass.HorseArcher => FormationClass.Cavalry, 
			FormationClass.NumberOfDefaultFormations => FormationClass.HeavyInfantry, 
			FormationClass.HeavyInfantry => FormationClass.NumberOfDefaultFormations, 
			FormationClass.LightCavalry => FormationClass.HeavyCavalry, 
			FormationClass.HeavyCavalry => FormationClass.LightCavalry, 
			_ => troopClass, 
		};
	}

	public static FormationClass DismountedClass(this FormationClass troopClass)
	{
		FormationClass result = troopClass;
		switch (troopClass)
		{
		case FormationClass.Cavalry:
			result = FormationClass.Infantry;
			break;
		case FormationClass.HorseArcher:
			result = FormationClass.Ranged;
			break;
		case FormationClass.LightCavalry:
			result = FormationClass.NumberOfDefaultFormations;
			break;
		case FormationClass.HeavyCavalry:
			result = FormationClass.HeavyInfantry;
			break;
		}
		return result;
	}

	public static bool IsDefaultTroopClass(this FormationClass troopClass)
	{
		if (troopClass >= FormationClass.Infantry)
		{
			return troopClass < FormationClass.NumberOfDefaultFormations;
		}
		return false;
	}

	public static bool IsRegularTroopClass(this FormationClass troopClass)
	{
		if (troopClass >= FormationClass.Infantry)
		{
			return troopClass < FormationClass.NumberOfRegularFormations;
		}
		return false;
	}

	public static FormationClass GetNextSpawnPrioritizedClass(this FormationClass troopClass)
	{
		if (troopClass.IsRegularTroopClass())
		{
			switch (troopClass)
			{
			case FormationClass.Infantry:
				return FormationClass.HeavyInfantry;
			case FormationClass.Ranged:
				return FormationClass.LightCavalry;
			case FormationClass.Cavalry:
				return FormationClass.HeavyCavalry;
			case FormationClass.HorseArcher:
				return FormationClass.HorseArcher;
			case FormationClass.NumberOfDefaultFormations:
				return FormationClass.Ranged;
			case FormationClass.HeavyInfantry:
				return FormationClass.Cavalry;
			case FormationClass.LightCavalry:
				return FormationClass.HorseArcher;
			case FormationClass.HeavyCavalry:
				return FormationClass.HeavyCavalry;
			}
		}
		return troopClass;
	}
}
