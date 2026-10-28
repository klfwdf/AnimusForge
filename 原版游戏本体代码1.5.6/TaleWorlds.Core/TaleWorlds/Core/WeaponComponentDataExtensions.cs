namespace TaleWorlds.Core;

public static class WeaponComponentDataExtensions
{
	public static int GetModifiedThrustDamage(this WeaponComponentData componentData, ItemModifier itemModifier)
	{
		if (itemModifier != null && componentData.ThrustDamage > 0)
		{
			return itemModifier.ModifyDamage(componentData.ThrustDamage);
		}
		return componentData.ThrustDamage;
	}

	public static int GetModifiedSwingDamage(this WeaponComponentData componentData, ItemModifier itemModifier)
	{
		if (itemModifier != null && componentData.SwingDamage > 0)
		{
			return itemModifier.ModifyDamage(componentData.SwingDamage);
		}
		return componentData.SwingDamage;
	}

	public static int GetModifiedMissileDamage(this WeaponComponentData componentData, ItemModifier itemModifier)
	{
		if (itemModifier != null && componentData.MissileDamage > 0)
		{
			return itemModifier.ModifyDamage(componentData.MissileDamage);
		}
		return componentData.MissileDamage;
	}

	public static int GetModifiedThrustSpeed(this WeaponComponentData componentData, ItemModifier itemModifier)
	{
		if (itemModifier != null && componentData.ThrustSpeed > 0)
		{
			return itemModifier.ModifySpeed(componentData.ThrustSpeed);
		}
		return componentData.ThrustSpeed;
	}

	public static int GetModifiedSwingSpeed(this WeaponComponentData componentData, ItemModifier itemModifier)
	{
		if (itemModifier != null && componentData.SwingSpeed > 0)
		{
			return itemModifier.ModifySpeed(componentData.SwingSpeed);
		}
		return componentData.SwingSpeed;
	}

	public static int GetModifiedMissileSpeed(this WeaponComponentData componentData, ItemModifier itemModifier)
	{
		if (itemModifier != null && componentData.MissileSpeed > 0)
		{
			return itemModifier.ModifyMissileSpeed(componentData.MissileSpeed);
		}
		return componentData.MissileSpeed;
	}

	public static int GetModifiedHandling(this WeaponComponentData componentData, ItemModifier itemModifier)
	{
		if (itemModifier != null && componentData.Handling > 0)
		{
			return itemModifier.ModifySpeed(componentData.Handling);
		}
		return componentData.Handling;
	}

	public static short GetModifiedStackCount(this WeaponComponentData componentData, ItemModifier itemModifier)
	{
		if (itemModifier != null && componentData.MaxDataValue > 0)
		{
			return itemModifier.ModifyStackCount(componentData.MaxDataValue);
		}
		return componentData.MaxDataValue;
	}

	public static short GetModifiedMaximumHitPoints(this WeaponComponentData componentData, ItemModifier itemModifier)
	{
		if (itemModifier != null && componentData.MaxDataValue > 0)
		{
			return itemModifier.ModifyHitPoints(componentData.MaxDataValue);
		}
		return componentData.MaxDataValue;
	}

	public static int GetModifiedArmor(this WeaponComponentData componentData, ItemModifier itemModifier)
	{
		if (itemModifier != null && componentData.BodyArmor > 0)
		{
			return itemModifier.ModifyArmor(componentData.BodyArmor);
		}
		return componentData.BodyArmor;
	}
}
