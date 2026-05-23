using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000E0 RID: 224
	public static class WeaponComponentDataExtensions
	{
		// Token: 0x06000B68 RID: 2920 RVA: 0x00024CD3 File Offset: 0x00022ED3
		public static int GetModifiedThrustDamage(this WeaponComponentData componentData, ItemModifier itemModifier)
		{
			if (itemModifier != null && componentData.ThrustDamage > 0)
			{
				return itemModifier.ModifyDamage(componentData.ThrustDamage);
			}
			return componentData.ThrustDamage;
		}

		// Token: 0x06000B69 RID: 2921 RVA: 0x00024CF4 File Offset: 0x00022EF4
		public static int GetModifiedSwingDamage(this WeaponComponentData componentData, ItemModifier itemModifier)
		{
			if (itemModifier != null && componentData.SwingDamage > 0)
			{
				return itemModifier.ModifyDamage(componentData.SwingDamage);
			}
			return componentData.SwingDamage;
		}

		// Token: 0x06000B6A RID: 2922 RVA: 0x00024D15 File Offset: 0x00022F15
		public static int GetModifiedMissileDamage(this WeaponComponentData componentData, ItemModifier itemModifier)
		{
			if (itemModifier != null && componentData.MissileDamage > 0)
			{
				return itemModifier.ModifyDamage(componentData.MissileDamage);
			}
			return componentData.MissileDamage;
		}

		// Token: 0x06000B6B RID: 2923 RVA: 0x00024D36 File Offset: 0x00022F36
		public static int GetModifiedThrustSpeed(this WeaponComponentData componentData, ItemModifier itemModifier)
		{
			if (itemModifier != null && componentData.ThrustSpeed > 0)
			{
				return itemModifier.ModifySpeed(componentData.ThrustSpeed);
			}
			return componentData.ThrustSpeed;
		}

		// Token: 0x06000B6C RID: 2924 RVA: 0x00024D57 File Offset: 0x00022F57
		public static int GetModifiedSwingSpeed(this WeaponComponentData componentData, ItemModifier itemModifier)
		{
			if (itemModifier != null && componentData.SwingSpeed > 0)
			{
				return itemModifier.ModifySpeed(componentData.SwingSpeed);
			}
			return componentData.SwingSpeed;
		}

		// Token: 0x06000B6D RID: 2925 RVA: 0x00024D78 File Offset: 0x00022F78
		public static int GetModifiedMissileSpeed(this WeaponComponentData componentData, ItemModifier itemModifier)
		{
			if (itemModifier != null && componentData.MissileSpeed > 0)
			{
				return itemModifier.ModifyMissileSpeed(componentData.MissileSpeed);
			}
			return componentData.MissileSpeed;
		}

		// Token: 0x06000B6E RID: 2926 RVA: 0x00024D99 File Offset: 0x00022F99
		public static int GetModifiedHandling(this WeaponComponentData componentData, ItemModifier itemModifier)
		{
			if (itemModifier != null && componentData.Handling > 0)
			{
				return itemModifier.ModifySpeed(componentData.Handling);
			}
			return componentData.Handling;
		}

		// Token: 0x06000B6F RID: 2927 RVA: 0x00024DBA File Offset: 0x00022FBA
		public static short GetModifiedStackCount(this WeaponComponentData componentData, ItemModifier itemModifier)
		{
			if (itemModifier != null && componentData.MaxDataValue > 0)
			{
				return itemModifier.ModifyStackCount(componentData.MaxDataValue);
			}
			return componentData.MaxDataValue;
		}

		// Token: 0x06000B70 RID: 2928 RVA: 0x00024DDB File Offset: 0x00022FDB
		public static short GetModifiedMaximumHitPoints(this WeaponComponentData componentData, ItemModifier itemModifier)
		{
			if (itemModifier != null && componentData.MaxDataValue > 0)
			{
				return itemModifier.ModifyHitPoints(componentData.MaxDataValue);
			}
			return componentData.MaxDataValue;
		}

		// Token: 0x06000B71 RID: 2929 RVA: 0x00024DFC File Offset: 0x00022FFC
		public static int GetModifiedArmor(this WeaponComponentData componentData, ItemModifier itemModifier)
		{
			if (itemModifier != null && componentData.BodyArmor > 0)
			{
				return itemModifier.ModifyArmor(componentData.BodyArmor);
			}
			return componentData.BodyArmor;
		}
	}
}
