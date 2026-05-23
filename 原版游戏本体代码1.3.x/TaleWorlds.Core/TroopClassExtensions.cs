using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000065 RID: 101
	public static class TroopClassExtensions
	{
		// Token: 0x06000732 RID: 1842 RVA: 0x00018DA4 File Offset: 0x00016FA4
		public static bool IsRanged(this FormationClass troopClass)
		{
			FormationClass formationClass = troopClass.DefaultClass();
			return formationClass == FormationClass.Ranged || formationClass == FormationClass.HorseArcher;
		}

		// Token: 0x06000733 RID: 1843 RVA: 0x00018DC2 File Offset: 0x00016FC2
		public static bool IsMounted(this FormationClass troopClass)
		{
			troopClass.DefaultClass();
			return troopClass == FormationClass.Cavalry || troopClass == FormationClass.HorseArcher;
		}

		// Token: 0x06000734 RID: 1844 RVA: 0x00018DD5 File Offset: 0x00016FD5
		public static bool IsMeleeInfantry(this FormationClass troopClass)
		{
			troopClass.DefaultClass();
			return troopClass == FormationClass.Infantry;
		}

		// Token: 0x06000735 RID: 1845 RVA: 0x00018DE2 File Offset: 0x00016FE2
		public static bool IsMeleeCavalry(this FormationClass troopClass)
		{
			return troopClass.DefaultClass() == FormationClass.Cavalry;
		}

		// Token: 0x06000736 RID: 1846 RVA: 0x00018DF0 File Offset: 0x00016FF0
		public static FormationClass DefaultClass(this FormationClass troopClass)
		{
			if (troopClass.IsRegularFormationClass())
			{
				FormationClass result = troopClass;
				switch (troopClass)
				{
				case FormationClass.NumberOfDefaultFormations:
					result = FormationClass.Ranged;
					break;
				case FormationClass.HeavyInfantry:
					result = FormationClass.Infantry;
					break;
				case FormationClass.LightCavalry:
					result = FormationClass.HorseArcher;
					break;
				case FormationClass.HeavyCavalry:
					result = FormationClass.Cavalry;
					break;
				}
				return result;
			}
			return FormationClass.Infantry;
		}

		// Token: 0x06000737 RID: 1847 RVA: 0x00018E32 File Offset: 0x00017032
		public static FormationClass AlternativeClass(this FormationClass troopClass)
		{
			switch (troopClass)
			{
			case FormationClass.Infantry:
				return FormationClass.Ranged;
			case FormationClass.Ranged:
				return FormationClass.Infantry;
			case FormationClass.Cavalry:
				return FormationClass.HorseArcher;
			case FormationClass.HorseArcher:
				return FormationClass.Cavalry;
			case FormationClass.NumberOfDefaultFormations:
				return FormationClass.HeavyInfantry;
			case FormationClass.HeavyInfantry:
				return FormationClass.NumberOfDefaultFormations;
			case FormationClass.LightCavalry:
				return FormationClass.HeavyCavalry;
			case FormationClass.HeavyCavalry:
				return FormationClass.LightCavalry;
			default:
				return troopClass;
			}
		}

		// Token: 0x06000738 RID: 1848 RVA: 0x00018E70 File Offset: 0x00017070
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

		// Token: 0x06000739 RID: 1849 RVA: 0x00018EB0 File Offset: 0x000170B0
		public static bool IsDefaultTroopClass(this FormationClass troopClass)
		{
			return troopClass >= FormationClass.Infantry && troopClass < FormationClass.NumberOfDefaultFormations;
		}

		// Token: 0x0600073A RID: 1850 RVA: 0x00018ECC File Offset: 0x000170CC
		public static bool IsRegularTroopClass(this FormationClass troopClass)
		{
			return troopClass >= FormationClass.Infantry && troopClass < FormationClass.NumberOfRegularFormations;
		}

		// Token: 0x0600073B RID: 1851 RVA: 0x00018EE8 File Offset: 0x000170E8
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
}
