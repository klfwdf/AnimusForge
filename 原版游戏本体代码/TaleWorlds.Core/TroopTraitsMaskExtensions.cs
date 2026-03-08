using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000D8 RID: 216
	public static class TroopTraitsMaskExtensions
	{
		// Token: 0x06000B30 RID: 2864 RVA: 0x000244A6 File Offset: 0x000226A6
		public static bool HasMelee(this TroopTraitsMask troopTraitsMask)
		{
			return (troopTraitsMask & TroopTraitsMask.Melee) > TroopTraitsMask.None;
		}

		// Token: 0x06000B31 RID: 2865 RVA: 0x000244AE File Offset: 0x000226AE
		public static bool HasRanged(this TroopTraitsMask troopTraitsMask)
		{
			return (troopTraitsMask & TroopTraitsMask.Ranged) > TroopTraitsMask.None;
		}

		// Token: 0x06000B32 RID: 2866 RVA: 0x000244B6 File Offset: 0x000226B6
		public static bool HasMount(this TroopTraitsMask troopTraitsMask)
		{
			return (troopTraitsMask & TroopTraitsMask.Mount) > TroopTraitsMask.None;
		}

		// Token: 0x06000B33 RID: 2867 RVA: 0x000244BE File Offset: 0x000226BE
		public static bool HasArmor(this TroopTraitsMask troopTraitsMask)
		{
			return (troopTraitsMask & TroopTraitsMask.Armor) > TroopTraitsMask.None;
		}

		// Token: 0x06000B34 RID: 2868 RVA: 0x000244C6 File Offset: 0x000226C6
		public static bool HasThrown(this TroopTraitsMask troopTraitsMask)
		{
			return (troopTraitsMask & TroopTraitsMask.Thrown) > TroopTraitsMask.None;
		}

		// Token: 0x06000B35 RID: 2869 RVA: 0x000244CF File Offset: 0x000226CF
		public static bool HasSpear(this TroopTraitsMask troopTraitsMask)
		{
			return (troopTraitsMask & TroopTraitsMask.Spear) > TroopTraitsMask.None;
		}

		// Token: 0x06000B36 RID: 2870 RVA: 0x000244D8 File Offset: 0x000226D8
		public static bool HasShield(this TroopTraitsMask troopTraitsMask)
		{
			return (troopTraitsMask & TroopTraitsMask.Shield) > TroopTraitsMask.None;
		}

		// Token: 0x06000B37 RID: 2871 RVA: 0x000244E1 File Offset: 0x000226E1
		public static bool HasLowTier(this TroopTraitsMask troopFilterMask)
		{
			return (troopFilterMask & TroopTraitsMask.LowTier) > TroopTraitsMask.None;
		}

		// Token: 0x06000B38 RID: 2872 RVA: 0x000244ED File Offset: 0x000226ED
		public static bool HasHighTier(this TroopTraitsMask troopFilterMask)
		{
			return (troopFilterMask & TroopTraitsMask.HighTier) > TroopTraitsMask.None;
		}

		// Token: 0x06000B39 RID: 2873 RVA: 0x000244FC File Offset: 0x000226FC
		public static string GetTroopTraitsText(this TroopTraitsMask troopTraitsMask)
		{
			string result = "";
			if (troopTraitsMask.HasMelee())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Melee, ref result);
			}
			else if (troopTraitsMask.HasRanged())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Ranged, ref result);
			}
			if (troopTraitsMask.HasMount())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Mount, ref result);
			}
			if (troopTraitsMask.HasArmor())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Armor, ref result);
			}
			if (troopTraitsMask.HasThrown())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Thrown, ref result);
			}
			if (troopTraitsMask.HasSpear())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Spear, ref result);
			}
			if (troopTraitsMask.HasShield())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Shield, ref result);
			}
			return result;
		}

		// Token: 0x06000B3A RID: 2874 RVA: 0x00024588 File Offset: 0x00022788
		public static string GetTraitsFilterText(this TroopTraitsMask troopTraitsFilter)
		{
			string result = "";
			if (troopTraitsFilter.HasArmor())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Armor, ref result);
			}
			if (troopTraitsFilter.HasThrown())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Thrown, ref result);
			}
			if (troopTraitsFilter.HasSpear())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Spear, ref result);
			}
			if (troopTraitsFilter.HasShield())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Shield, ref result);
			}
			if (troopTraitsFilter.HasLowTier())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.LowTier, ref result);
			}
			else if (troopTraitsFilter.HasHighTier())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.HighTier, ref result);
			}
			return result;
		}

		// Token: 0x06000B3B RID: 2875 RVA: 0x0002460C File Offset: 0x0002280C
		public static string GetClassFilterText(this TroopTraitsMask troopTraitsFilter)
		{
			string result = "";
			if (troopTraitsFilter.HasMelee())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Melee, ref result);
			}
			if (troopTraitsFilter.HasRanged())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Ranged, ref result);
			}
			if (troopTraitsFilter.HasMount())
			{
				TroopTraitsMaskExtensions.AddFlagToText(TroopTraitsMask.Mount, ref result);
			}
			return result;
		}

		// Token: 0x06000B3C RID: 2876 RVA: 0x00024650 File Offset: 0x00022850
		private static void AddFlagToText(TroopTraitsMask flag, ref string text)
		{
			if (text.Length > 0)
			{
				text += "|";
			}
			string str;
			if (flag <= TroopTraitsMask.Thrown)
			{
				switch (flag)
				{
				case TroopTraitsMask.Melee:
					str = "Melee";
					goto IL_B7;
				case TroopTraitsMask.Ranged:
					str = "Ranged";
					goto IL_B7;
				case TroopTraitsMask.Melee | TroopTraitsMask.Ranged:
					break;
				case TroopTraitsMask.Mount:
					str = "Mount";
					goto IL_B7;
				default:
					if (flag == TroopTraitsMask.Armor)
					{
						str = "Armor";
						goto IL_B7;
					}
					if (flag == TroopTraitsMask.Thrown)
					{
						str = "Thrown";
						goto IL_B7;
					}
					break;
				}
			}
			else if (flag <= TroopTraitsMask.Shield)
			{
				if (flag == TroopTraitsMask.Spear)
				{
					str = "Spear";
					goto IL_B7;
				}
				if (flag == TroopTraitsMask.Shield)
				{
					str = "Shield";
					goto IL_B7;
				}
			}
			else
			{
				if (flag == TroopTraitsMask.LowTier)
				{
					str = "Low Tier";
					goto IL_B7;
				}
				if (flag == TroopTraitsMask.HighTier)
				{
					str = "High Tier";
					goto IL_B7;
				}
			}
			str = "";
			IL_B7:
			text += str;
		}
	}
}
