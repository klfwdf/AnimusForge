using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x02000066 RID: 102
	public static class FormationClassExtensions
	{
		// Token: 0x0600073C RID: 1852 RVA: 0x00018F36 File Offset: 0x00017136
		public static string GetName(this FormationClass formationClass)
		{
			if (formationClass == FormationClass.NumberOfDefaultFormations)
			{
				return "Skirmisher";
			}
			if (formationClass == FormationClass.NumberOfRegularFormations)
			{
				return "General";
			}
			if (formationClass != FormationClass.NumberOfAllFormations)
			{
				return formationClass.ToString();
			}
			return "Unset";
		}

		// Token: 0x0600073D RID: 1853 RVA: 0x00018F68 File Offset: 0x00017168
		public static TextObject GetLocalizedName(this FormationClass formationClass)
		{
			string id = "str_troop_group_name";
			int num = (int)formationClass;
			return GameTexts.FindText(id, num.ToString());
		}

		// Token: 0x0600073E RID: 1854 RVA: 0x00018F88 File Offset: 0x00017188
		public static TroopUsageFlags GetTroopUsageFlags(this FormationClass troopClass)
		{
			switch (troopClass)
			{
			case FormationClass.Ranged:
				return TroopUsageFlags.OnFoot | TroopUsageFlags.Ranged | TroopUsageFlags.BowUser | TroopUsageFlags.ThrownUser | TroopUsageFlags.CrossbowUser;
			case FormationClass.Cavalry:
				return TroopUsageFlags.Mounted | TroopUsageFlags.Melee | TroopUsageFlags.OneHandedUser | TroopUsageFlags.ShieldUser | TroopUsageFlags.TwoHandedUser | TroopUsageFlags.PolearmUser;
			case FormationClass.HorseArcher:
				return TroopUsageFlags.Mounted | TroopUsageFlags.Ranged | TroopUsageFlags.BowUser | TroopUsageFlags.ThrownUser | TroopUsageFlags.CrossbowUser;
			}
			return TroopUsageFlags.OnFoot | TroopUsageFlags.Melee | TroopUsageFlags.OneHandedUser | TroopUsageFlags.ShieldUser | TroopUsageFlags.TwoHandedUser | TroopUsageFlags.PolearmUser;
		}

		// Token: 0x0600073F RID: 1855 RVA: 0x00018FBC File Offset: 0x000171BC
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
				Debug.FailedAssert(string.Format("Undefined formation class {0} for TroopType!", formationClass), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\FormationClass.cs", "GetTroopTypeForRegularFormation", 321);
				break;
			}
			return result;
		}

		// Token: 0x06000740 RID: 1856 RVA: 0x00019024 File Offset: 0x00017224
		public static bool IsDefaultFormationClass(this FormationClass formationClass)
		{
			return formationClass >= FormationClass.Infantry && formationClass < FormationClass.NumberOfDefaultFormations;
		}

		// Token: 0x06000741 RID: 1857 RVA: 0x00019040 File Offset: 0x00017240
		public static bool IsRegularFormationClass(this FormationClass formationClass)
		{
			return formationClass >= FormationClass.Infantry && formationClass < FormationClass.NumberOfRegularFormations;
		}

		// Token: 0x06000742 RID: 1858 RVA: 0x00019059 File Offset: 0x00017259
		public static FormationClass FallbackClass(this FormationClass formationClass)
		{
			if (formationClass == FormationClass.Ranged || formationClass == FormationClass.NumberOfDefaultFormations)
			{
				return FormationClass.Ranged;
			}
			if (formationClass == FormationClass.Cavalry || formationClass == FormationClass.HeavyCavalry)
			{
				return FormationClass.Cavalry;
			}
			if (formationClass == FormationClass.HorseArcher || formationClass == FormationClass.LightCavalry)
			{
				return FormationClass.HorseArcher;
			}
			return FormationClass.Infantry;
		}

		// Token: 0x040003D9 RID: 985
		public const TroopUsageFlags DefaultInfantryTroopUsageFlags = TroopUsageFlags.OnFoot | TroopUsageFlags.Melee | TroopUsageFlags.OneHandedUser | TroopUsageFlags.ShieldUser | TroopUsageFlags.TwoHandedUser | TroopUsageFlags.PolearmUser;

		// Token: 0x040003DA RID: 986
		public const TroopUsageFlags DefaultRangedTroopUsageFlags = TroopUsageFlags.OnFoot | TroopUsageFlags.Ranged | TroopUsageFlags.BowUser | TroopUsageFlags.ThrownUser | TroopUsageFlags.CrossbowUser;

		// Token: 0x040003DB RID: 987
		public const TroopUsageFlags DefaultCavalryTroopUsageFlags = TroopUsageFlags.Mounted | TroopUsageFlags.Melee | TroopUsageFlags.OneHandedUser | TroopUsageFlags.ShieldUser | TroopUsageFlags.TwoHandedUser | TroopUsageFlags.PolearmUser;

		// Token: 0x040003DC RID: 988
		public const TroopUsageFlags DefaultHorseArcherTroopUsageFlags = TroopUsageFlags.Mounted | TroopUsageFlags.Ranged | TroopUsageFlags.BowUser | TroopUsageFlags.ThrownUser | TroopUsageFlags.CrossbowUser;

		// Token: 0x040003DD RID: 989
		public static FormationClass[] FormationClassValues = (FormationClass[])Enum.GetValues(typeof(FormationClass));
	}
}
