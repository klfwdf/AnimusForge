using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using TaleWorlds.Core;

namespace AnimusForge;

internal sealed class PlayerRpCraftItemStatsSnapshot
{
	public const int CurrentSchemaVersion = 1;

	public int SchemaVersion = CurrentSchemaVersion;

	public string TemplateStringId;

	public int ItemType;

	public bool Underfunded;

	public double AppliedMultiplier = 1d;

	public int AppliedAdditiveBonus;

	public float Weight;

	public List<PlayerRpCraftWeaponModeStatsSnapshot> WeaponModes = new List<PlayerRpCraftWeaponModeStatsSnapshot>();

	public PlayerRpCraftArmorStatsSnapshot Armor;
}

internal sealed class PlayerRpCraftWeaponModeStatsSnapshot
{
	public int Index;

	public int WeaponClass;

	public ulong WeaponFlags;

	public long AppliedFields;

	public int BodyArmor;

	public int ThrustSpeed;

	public int SwingSpeed;

	public int MissileSpeed;

	public int ThrustDamage;

	public int SwingDamage;

	public int FireDamage;

	public int Accuracy;

	public int Handling;

	public int MaxDataValue;
}

internal sealed class PlayerRpCraftArmorStatsSnapshot
{
	public long AppliedFields;

	public int HeadArmor;

	public int BodyArmor;

	public int LegArmor;

	public int ArmArmor;

	public int StealthFactor;

	public int ManeuverBonus;

	public int SpeedBonus;

	public int ChargeBonus;
}

internal static class PlayerRpCraftItemComponentService
{
	[Flags]
	private enum WeaponStatFields : long
	{
		None = 0L,
		BodyArmor = 1L << 0,
		ThrustSpeed = 1L << 1,
		SwingSpeed = 1L << 2,
		MissileSpeed = 1L << 3,
		ThrustDamage = 1L << 4,
		SwingDamage = 1L << 5,
		FireDamage = 1L << 6,
		Accuracy = 1L << 7,
		Handling = 1L << 8,
		MaxDataValue = 1L << 9
	}

	[Flags]
	private enum ArmorStatFields : long
	{
		None = 0L,
		HeadArmor = 1L << 0,
		BodyArmor = 1L << 1,
		LegArmor = 1L << 2,
		ArmArmor = 1L << 3,
		StealthFactor = 1L << 4,
		ManeuverBonus = 1L << 5,
		SpeedBonus = 1L << 6,
		ChargeBonus = 1L << 7
	}

	private const double WeightReductionPerBonusPoint = 0.98d;
	private const float MinimumPositiveWeight = 0.0001f;

	private static readonly PropertyInfo ItemObjectItemComponentProperty = typeof(ItemObject).GetProperty(
		"ItemComponent",
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private static readonly PropertyInfo ItemObjectWeightProperty = typeof(ItemObject).GetProperty(
		"Weight",
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private static readonly PropertyInfo ItemComponentModifierGroupProperty = typeof(ItemComponent).GetProperty(
		"ItemModifierGroup",
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private static readonly PropertyInfo WeaponBodyArmorProperty = GetRequiredProperty(typeof(WeaponComponentData), "BodyArmor");
	private static readonly PropertyInfo WeaponThrustSpeedProperty = GetRequiredProperty(typeof(WeaponComponentData), "ThrustSpeed");
	private static readonly PropertyInfo WeaponSwingSpeedProperty = GetRequiredProperty(typeof(WeaponComponentData), "SwingSpeed");
	private static readonly PropertyInfo WeaponMissileSpeedProperty = GetRequiredProperty(typeof(WeaponComponentData), "MissileSpeed");
	private static readonly PropertyInfo WeaponThrustDamageProperty = GetRequiredProperty(typeof(WeaponComponentData), "ThrustDamage");
	private static readonly PropertyInfo WeaponSwingDamageProperty = GetRequiredProperty(typeof(WeaponComponentData), "SwingDamage");
	private static readonly PropertyInfo WeaponFireDamageProperty = GetRequiredProperty(typeof(WeaponComponentData), "FireDamage");
	private static readonly PropertyInfo WeaponAccuracyProperty = GetRequiredProperty(typeof(WeaponComponentData), "Accuracy");
	private static readonly PropertyInfo WeaponHandlingProperty = GetRequiredProperty(typeof(WeaponComponentData), "Handling");
	private static readonly PropertyInfo WeaponMaxDataValueProperty = GetRequiredProperty(typeof(WeaponComponentData), "MaxDataValue");

	private static readonly PropertyInfo ArmorHeadArmorProperty = GetRequiredProperty(typeof(ArmorComponent), "HeadArmor");
	private static readonly PropertyInfo ArmorBodyArmorProperty = GetRequiredProperty(typeof(ArmorComponent), "BodyArmor");
	private static readonly PropertyInfo ArmorLegArmorProperty = GetRequiredProperty(typeof(ArmorComponent), "LegArmor");
	private static readonly PropertyInfo ArmorArmArmorProperty = GetRequiredProperty(typeof(ArmorComponent), "ArmArmor");
	private static readonly PropertyInfo ArmorStealthFactorProperty = GetRequiredProperty(typeof(ArmorComponent), "StealthFactor");
	private static readonly PropertyInfo ArmorManeuverBonusProperty = GetRequiredProperty(typeof(ArmorComponent), "ManeuverBonus");
	private static readonly PropertyInfo ArmorSpeedBonusProperty = GetRequiredProperty(typeof(ArmorComponent), "SpeedBonus");
	private static readonly PropertyInfo ArmorChargeBonusProperty = GetRequiredProperty(typeof(ArmorComponent), "ChargeBonus");
	private static readonly PropertyInfo ArmorIsNoSlimProperty = typeof(ArmorComponent).GetProperty(
		"IsNoSlim",
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private static readonly PropertyInfo[] WeaponStateProperties = BuildPropertyCache(
		typeof(WeaponComponentData),
		"WeaponTier",
		"WeaponDescriptionId",
		"BodyArmor",
		"PhysicsMaterial",
		"FlyingSoundCode",
		"PassbySoundCode",
		"ItemUsage",
		"ThrustSpeed",
		"SwingSpeed",
		"MissileSpeed",
		"WeaponLength",
		"WeaponBalance",
		"ThrustDamage",
		"ThrustDamageType",
		"SwingDamage",
		"SwingDamageType",
		"FireDamage",
		"Accuracy",
		"WeaponClass",
		"AmmoClass",
		"TotalInertia",
		"CenterOfMass",
		"CenterOfMass3D",
		"SwingDamageFactor",
		"ThrustDamageFactor",
		"Handling",
		"SweetSpotReach",
		"TrailParticleName",
		"StickingFrame",
		"AmmoOffset",
		"MaxDataValue",
		"Frame",
		"RotationSpeed",
		"ReloadPhaseCount");

	private static readonly PropertyInfo[] ArmorStateProperties = BuildPropertyCache(
		typeof(ArmorComponent),
		"HeadArmor",
		"BodyArmor",
		"LegArmor",
		"ArmArmor",
		"ManeuverBonus",
		"SpeedBonus",
		"ChargeBonus",
		"FamilyType",
		"MultiMeshHasGenderVariations",
		"MaterialType",
		"MeshesMask",
		"BodyMeshType",
		"BodyDeformType",
		"HairCoverType",
		"BeardCoverType",
		"ManeCoverType",
		"TailCoverType",
		"StealthFactor",
		"ReinsMesh");

	internal static bool TryCreateSnapshot(
		ItemObject template,
		bool underfunded,
		double multiplier,
		int additiveBonus,
		out PlayerRpCraftItemStatsSnapshot snapshot,
		out string error)
	{
		snapshot = null;
		error = null;
		if (!IsSafeEquipmentTemplate(template, out error))
		{
			return false;
		}
		if (additiveBonus < 0)
		{
			error = "negative_additive_bonus";
			return false;
		}
		if (underfunded && (!IsFinite(multiplier) || multiplier <= 0d || multiplier > 1d))
		{
			error = "invalid_underfunded_multiplier";
			return false;
		}
		if (!underfunded)
		{
			multiplier = 1d;
		}

		try
		{
			PlayerRpCraftItemStatsSnapshot result = new PlayerRpCraftItemStatsSnapshot
			{
				TemplateStringId = template.StringId ?? "",
				ItemType = (int)template.Type,
				Underfunded = underfunded,
				AppliedMultiplier = multiplier,
				AppliedAdditiveBonus = additiveBonus,
				Weight = CalculateFinalWeight(template.Weight, underfunded, multiplier, additiveBonus)
			};

			if (template.WeaponComponent != null)
			{
				for (int i = 0; i < template.WeaponComponent.Weapons.Count; i++)
				{
					WeaponComponentData weapon = template.WeaponComponent.Weapons[i];
					if (weapon == null)
					{
						error = "null_weapon_mode_" + i.ToString(CultureInfo.InvariantCulture);
						return false;
					}
					result.WeaponModes.Add(CreateWeaponModeSnapshot(template.Type, weapon, i, underfunded, multiplier, additiveBonus));
				}
			}
			else if (template.ArmorComponent != null)
			{
				result.Armor = CreateArmorSnapshot(template.Type, template.ArmorComponent, underfunded, multiplier, additiveBonus);
			}
			else
			{
				error = "missing_supported_component";
				return false;
			}

			snapshot = result;
			return true;
		}
		catch (Exception ex)
		{
			error = "snapshot_failed:" + ex.GetType().Name + ":" + ex.Message;
			snapshot = null;
			return false;
		}
	}

	internal static bool TryApplySnapshot(
		ItemObject target,
		ItemObject template,
		PlayerRpCraftItemStatsSnapshot snapshot,
		out string error)
	{
		error = null;
		if (target == null)
		{
			error = "missing_target";
			return false;
		}
		if (!IsSafeEquipmentTemplate(template, out error)
			|| !IsStructurallyCompatible(template, snapshot, out error))
		{
			return false;
		}
		if (ItemObjectItemComponentProperty?.GetSetMethod(true) == null
			|| ItemObjectWeightProperty?.GetSetMethod(true) == null)
		{
			error = "item_object_setter_unavailable";
			return false;
		}

		ItemComponent previousComponent = target.ItemComponent;
		float previousWeight = target.Weight;
		try
		{
			ItemComponent replacementComponent;
			if (template.WeaponComponent != null)
			{
				replacementComponent = CreateWeaponComponentCopy(target, template.WeaponComponent, snapshot);
			}
			else
			{
				replacementComponent = CreateArmorComponentCopy(target, template.ArmorComponent, snapshot.Armor);
			}
			if (replacementComponent == null)
			{
				error = "component_copy_failed";
				return false;
			}

			ItemObjectItemComponentProperty.SetValue(target, replacementComponent, null);
			ItemObjectWeightProperty.SetValue(target, ClampWeight(snapshot.Weight), null);
			if (!ReferenceEquals(target.ItemComponent, replacementComponent)
				|| !MatchesSnapshot(target, snapshot))
			{
				throw new InvalidOperationException("snapshot_commit_validation_failed");
			}
			return true;
		}
		catch (Exception ex)
		{
			try
			{
				ItemObjectItemComponentProperty?.SetValue(target, previousComponent, null);
				ItemObjectWeightProperty?.SetValue(target, previousWeight, null);
			}
			catch
			{
			}
			error = "apply_failed:" + ex.GetType().Name + ":" + ex.Message;
			return false;
		}
	}

	internal static bool MatchesSnapshot(
		ItemObject target,
		PlayerRpCraftItemStatsSnapshot snapshot)
	{
		if (target == null
			|| snapshot == null
			|| snapshot.SchemaVersion <= 0
			|| snapshot.SchemaVersion > PlayerRpCraftItemStatsSnapshot.CurrentSchemaVersion
			|| snapshot.ItemType != (int)target.Type
			|| !IsFinite(snapshot.Weight)
			|| snapshot.Weight <= 0f
			|| target.Weight != snapshot.Weight)
		{
			return false;
		}

		if (snapshot.Armor != null)
		{
			if ((snapshot.WeaponModes != null && snapshot.WeaponModes.Count != 0)
				|| target.ItemComponent == null
				|| target.ItemComponent.GetType() != typeof(ArmorComponent))
			{
				return false;
			}
			ArmorComponent armor = target.ArmorComponent;
			PlayerRpCraftArmorStatsSnapshot savedArmor = snapshot.Armor;
			if (armor == null || !ReferenceEquals(armor.Item, target))
			{
				return false;
			}
			const ArmorStatFields knownArmorFields =
				ArmorStatFields.HeadArmor
				| ArmorStatFields.BodyArmor
				| ArmorStatFields.LegArmor
				| ArmorStatFields.ArmArmor
				| ArmorStatFields.StealthFactor
				| ArmorStatFields.ManeuverBonus
				| ArmorStatFields.SpeedBonus
				| ArmorStatFields.ChargeBonus;
			ArmorStatFields armorFields = (ArmorStatFields)savedArmor.AppliedFields;
			return (armorFields & ~knownArmorFields) == 0
				&& ((armorFields & ArmorStatFields.HeadArmor) == 0 || savedArmor.HeadArmor == armor.HeadArmor)
				&& ((armorFields & ArmorStatFields.BodyArmor) == 0 || savedArmor.BodyArmor == armor.BodyArmor)
				&& ((armorFields & ArmorStatFields.LegArmor) == 0 || savedArmor.LegArmor == armor.LegArmor)
				&& ((armorFields & ArmorStatFields.ArmArmor) == 0 || savedArmor.ArmArmor == armor.ArmArmor)
				&& ((armorFields & ArmorStatFields.StealthFactor) == 0 || savedArmor.StealthFactor == armor.StealthFactor)
				&& ((armorFields & ArmorStatFields.ManeuverBonus) == 0 || savedArmor.ManeuverBonus == armor.ManeuverBonus)
				&& ((armorFields & ArmorStatFields.SpeedBonus) == 0 || savedArmor.SpeedBonus == armor.SpeedBonus)
				&& ((armorFields & ArmorStatFields.ChargeBonus) == 0 || savedArmor.ChargeBonus == armor.ChargeBonus);
		}

		if (snapshot.WeaponModes == null
			|| snapshot.WeaponModes.Count == 0
			|| target.ItemComponent == null
			|| target.ItemComponent.GetType() != typeof(WeaponComponent))
		{
			return false;
		}
		WeaponComponent component = target.WeaponComponent;
		if (component == null
			|| !ReferenceEquals(component.Item, target)
			|| component.Weapons == null
			|| component.Weapons.Count != snapshot.WeaponModes.Count)
		{
			return false;
		}
		for (int i = 0; i < component.Weapons.Count; i++)
		{
			WeaponComponentData mode = component.Weapons[i];
			PlayerRpCraftWeaponModeStatsSnapshot savedMode = snapshot.WeaponModes[i];
			if (mode == null
				|| mode.GetType() != typeof(WeaponComponentData)
				|| savedMode == null
				|| savedMode.Index != i
				|| savedMode.WeaponClass != (int)mode.WeaponClass
				|| !HaveCompatibleWeaponSemantics((WeaponFlags)savedMode.WeaponFlags, mode.WeaponFlags))
			{
				return false;
			}
			const WeaponStatFields knownWeaponFields =
				WeaponStatFields.BodyArmor
				| WeaponStatFields.ThrustSpeed
				| WeaponStatFields.SwingSpeed
				| WeaponStatFields.MissileSpeed
				| WeaponStatFields.ThrustDamage
				| WeaponStatFields.SwingDamage
				| WeaponStatFields.FireDamage
				| WeaponStatFields.Accuracy
				| WeaponStatFields.Handling
				| WeaponStatFields.MaxDataValue;
			WeaponStatFields weaponFields = (WeaponStatFields)savedMode.AppliedFields;
			if ((weaponFields & ~knownWeaponFields) != 0
				|| ((weaponFields & WeaponStatFields.BodyArmor) != 0 && savedMode.BodyArmor != mode.BodyArmor)
				|| ((weaponFields & WeaponStatFields.ThrustSpeed) != 0 && savedMode.ThrustSpeed != mode.ThrustSpeed)
				|| ((weaponFields & WeaponStatFields.SwingSpeed) != 0 && savedMode.SwingSpeed != mode.SwingSpeed)
				|| ((weaponFields & WeaponStatFields.MissileSpeed) != 0 && savedMode.MissileSpeed != mode.MissileSpeed)
				|| ((weaponFields & WeaponStatFields.ThrustDamage) != 0 && savedMode.ThrustDamage != mode.ThrustDamage)
				|| ((weaponFields & WeaponStatFields.SwingDamage) != 0 && savedMode.SwingDamage != mode.SwingDamage)
				|| ((weaponFields & WeaponStatFields.FireDamage) != 0 && savedMode.FireDamage != mode.FireDamage)
				|| ((weaponFields & WeaponStatFields.Accuracy) != 0 && savedMode.Accuracy != mode.Accuracy)
				|| ((weaponFields & WeaponStatFields.Handling) != 0 && savedMode.Handling != mode.Handling)
				|| ((weaponFields & WeaponStatFields.MaxDataValue) != 0 && savedMode.MaxDataValue != mode.MaxDataValue))
			{
				return false;
			}
		}
		return true;
	}

	internal static bool IsStructurallyCompatible(
		ItemObject template,
		PlayerRpCraftItemStatsSnapshot snapshot,
		out string error)
	{
		error = null;
		if (template == null)
		{
			error = "missing_template";
			return false;
		}
		if (snapshot == null)
		{
			error = "missing_snapshot";
			return false;
		}
		if (snapshot.SchemaVersion <= 0 || snapshot.SchemaVersion > PlayerRpCraftItemStatsSnapshot.CurrentSchemaVersion)
		{
			error = "unsupported_snapshot_schema";
			return false;
		}
		if (snapshot.ItemType != (int)template.Type)
		{
			error = "item_type_mismatch";
			return false;
		}
		if (!IsFinite(snapshot.Weight) || snapshot.Weight <= 0f)
		{
			error = "invalid_snapshot_weight";
			return false;
		}

		if (template.WeaponComponent != null)
		{
			if (snapshot.Armor != null)
			{
				error = "snapshot_component_kind_mismatch";
				return false;
			}
			if (snapshot.WeaponModes == null || snapshot.WeaponModes.Count != template.WeaponComponent.Weapons.Count)
			{
				error = "weapon_mode_count_mismatch";
				return false;
			}
			for (int i = 0; i < template.WeaponComponent.Weapons.Count; i++)
			{
				WeaponComponentData templateMode = template.WeaponComponent.Weapons[i];
				PlayerRpCraftWeaponModeStatsSnapshot savedMode = snapshot.WeaponModes[i];
				if (templateMode == null || savedMode == null || savedMode.Index != i)
				{
					error = "weapon_mode_index_mismatch_" + i.ToString(CultureInfo.InvariantCulture);
					return false;
				}
				if (savedMode.WeaponClass != (int)templateMode.WeaponClass
					|| !HaveCompatibleWeaponSemantics((WeaponFlags)savedMode.WeaponFlags, templateMode.WeaponFlags))
				{
					error = "weapon_mode_structure_mismatch_" + i.ToString(CultureInfo.InvariantCulture);
					return false;
				}
			}
			return true;
		}

		if (template.ArmorComponent == null || snapshot.Armor == null)
		{
			error = "snapshot_component_kind_mismatch";
			return false;
		}
		if (snapshot.WeaponModes != null && snapshot.WeaponModes.Count > 0)
		{
			error = "snapshot_contains_weapon_modes_for_armor";
			return false;
		}
		return true;
	}

	internal static bool IsStructurallyCompatible(ItemObject template, PlayerRpCraftItemStatsSnapshot snapshot)
	{
		return IsStructurallyCompatible(template, snapshot, out _);
	}

	internal static bool IsSafeEquipmentTemplate(ItemObject template, out string error)
	{
		error = null;
		if (template == null)
		{
			error = "missing_template";
			return false;
		}
		if (IsRuntimeCraftedWeapon(template))
		{
			error = "runtime_crafted_weapon_template_rejected";
			return false;
		}
		if (!IsSupportedEquipmentType(template.Type))
		{
			error = "unsupported_item_type";
			return false;
		}
		if (template.WeaponComponent != null)
		{
			if (template.IsCraftedWeapon
				&& template.WeaponDesign?.Template == null)
			{
				error = "catalog_crafted_weapon_missing_design_template";
				return false;
			}
			if (template.WeaponComponent.GetType() != typeof(WeaponComponent))
			{
				error = "custom_weapon_component_not_supported";
				return false;
			}
			if (template.WeaponComponent.Weapons == null || template.WeaponComponent.Weapons.Count <= 0)
			{
				error = "missing_weapon_modes";
				return false;
			}
			for (int i = 0; i < template.WeaponComponent.Weapons.Count; i++)
			{
				WeaponComponentData mode = template.WeaponComponent.Weapons[i];
				if (mode == null || mode.GetType() != typeof(WeaponComponentData))
				{
					error = "custom_or_missing_weapon_mode_" + i.ToString(CultureInfo.InvariantCulture);
					return false;
				}
			}
			if (template.WeaponComponent.PrimaryWeapon == null)
			{
				error = "missing_primary_weapon";
				return false;
			}
		}
		else if (template.ArmorComponent == null)
		{
			error = "missing_equipment_component";
			return false;
		}
		else if (template.ArmorComponent.GetType() != typeof(ArmorComponent))
		{
			error = "custom_armor_component_not_supported";
			return false;
		}

		if (template.IsCraftedWeapon)
		{
			// Native catalog swords/axes/polearms are static CraftedItem entries.
			// Their inventory tableau is built from WeaponDesign rather than MultiMeshName.
			// Player-forged runtime items were rejected above.
		}
		else if (IsAmmoType(template.Type))
		{
			if (string.IsNullOrWhiteSpace(template.HolsterMeshName))
			{
				error = "missing_ammo_holster_mesh";
				return false;
			}
		}
		else if (string.IsNullOrWhiteSpace(template.MultiMeshName))
		{
			error = "missing_inventory_mesh";
			return false;
		}

		if (template.Type == ItemObject.ItemTypeEnum.Shield
			&& (template.ItemHolsters == null
				|| template.ItemHolsters.Length <= 0
				|| string.IsNullOrWhiteSpace(template.ItemHolsters[0])))
		{
			error = "missing_shield_holster";
			return false;
		}
		return true;
	}

	private static bool IsRuntimeCraftedWeapon(ItemObject template)
	{
		if (template?.IsCraftedWeapon != true)
		{
			return false;
		}
		if (template.IsCraftedByPlayer)
		{
			return true;
		}
		string stringId = (template.StringId ?? "").Trim();
		return stringId.StartsWith(
			"crafted_item_",
			StringComparison.OrdinalIgnoreCase);
	}

	internal static string BuildAttributeSummary(PlayerRpCraftItemStatsSnapshot snapshot)
	{
		if (snapshot == null)
		{
			return "";
		}
		StringBuilder builder = new StringBuilder(256);
		builder.Append("weight=")
			.Append(snapshot.Weight.ToString("0.####", CultureInfo.InvariantCulture));
		if (snapshot.WeaponModes != null)
		{
			foreach (PlayerRpCraftWeaponModeStatsSnapshot mode in snapshot.WeaponModes)
			{
				if (mode == null)
				{
					continue;
				}
				WeaponStatFields fields = (WeaponStatFields)mode.AppliedFields;
				builder.Append(";mode")
					.Append(mode.Index.ToString(CultureInfo.InvariantCulture))
					.Append('[')
					.Append(((WeaponClass)mode.WeaponClass).ToString());
				AppendStat(builder, fields, WeaponStatFields.SwingDamage, "swingDamage", mode.SwingDamage);
				AppendStat(builder, fields, WeaponStatFields.ThrustDamage, "thrustDamage", mode.ThrustDamage);
				AppendStat(builder, fields, WeaponStatFields.FireDamage, "fireDamage", mode.FireDamage);
				AppendStat(builder, fields, WeaponStatFields.SwingSpeed, "swingSpeed", mode.SwingSpeed);
				AppendStat(builder, fields, WeaponStatFields.ThrustSpeed, "thrustSpeed", mode.ThrustSpeed);
				AppendStat(builder, fields, WeaponStatFields.Handling, "handling", mode.Handling);
				AppendStat(builder, fields, WeaponStatFields.MissileSpeed, "missileSpeed", mode.MissileSpeed);
				AppendStat(builder, fields, WeaponStatFields.Accuracy, "accuracy", mode.Accuracy);
				AppendStat(builder, fields, WeaponStatFields.BodyArmor, "armor", mode.BodyArmor);
				AppendStat(builder, fields, WeaponStatFields.MaxDataValue, "hitPoints", mode.MaxDataValue);
				builder.Append(']');
			}
		}
		if (snapshot.Armor != null)
		{
			PlayerRpCraftArmorStatsSnapshot armor = snapshot.Armor;
			ArmorStatFields fields = (ArmorStatFields)armor.AppliedFields;
			builder.Append(";armor[");
			AppendStat(builder, fields, ArmorStatFields.HeadArmor, "head", armor.HeadArmor);
			AppendStat(builder, fields, ArmorStatFields.BodyArmor, "body", armor.BodyArmor);
			AppendStat(builder, fields, ArmorStatFields.ArmArmor, "arm", armor.ArmArmor);
			AppendStat(builder, fields, ArmorStatFields.LegArmor, "leg", armor.LegArmor);
			AppendStat(builder, fields, ArmorStatFields.StealthFactor, "stealth", armor.StealthFactor);
			AppendStat(builder, fields, ArmorStatFields.ManeuverBonus, "maneuver", armor.ManeuverBonus);
			AppendStat(builder, fields, ArmorStatFields.SpeedBonus, "speed", armor.SpeedBonus);
			AppendStat(builder, fields, ArmorStatFields.ChargeBonus, "charge", armor.ChargeBonus);
			builder.Append(']');
		}
		return builder.ToString();
	}

	private static PlayerRpCraftWeaponModeStatsSnapshot CreateWeaponModeSnapshot(
		ItemObject.ItemTypeEnum itemType,
		WeaponComponentData weapon,
		int index,
		bool underfunded,
		double multiplier,
		int additiveBonus)
	{
		WeaponStatFields fields = GetWeaponStatFields(itemType, weapon);
		PlayerRpCraftWeaponModeStatsSnapshot result = new PlayerRpCraftWeaponModeStatsSnapshot
		{
			Index = index,
			WeaponClass = (int)weapon.WeaponClass,
			WeaponFlags = (ulong)weapon.WeaponFlags,
			AppliedFields = (long)fields,
			BodyArmor = weapon.BodyArmor,
			ThrustSpeed = weapon.ThrustSpeed,
			SwingSpeed = weapon.SwingSpeed,
			MissileSpeed = weapon.MissileSpeed,
			ThrustDamage = weapon.ThrustDamage,
			SwingDamage = weapon.SwingDamage,
			FireDamage = weapon.FireDamage,
			Accuracy = weapon.Accuracy,
			Handling = weapon.Handling,
			MaxDataValue = weapon.MaxDataValue
		};

		result.BodyArmor = ApplyPositiveStat(result.BodyArmor, fields, WeaponStatFields.BodyArmor, underfunded, multiplier, additiveBonus);
		result.ThrustSpeed = ApplyPositiveStat(result.ThrustSpeed, fields, WeaponStatFields.ThrustSpeed, underfunded, multiplier, additiveBonus);
		result.SwingSpeed = ApplyPositiveStat(result.SwingSpeed, fields, WeaponStatFields.SwingSpeed, underfunded, multiplier, additiveBonus);
		result.MissileSpeed = ApplyPositiveStat(result.MissileSpeed, fields, WeaponStatFields.MissileSpeed, underfunded, multiplier, additiveBonus);
		result.ThrustDamage = ApplyPositiveStat(result.ThrustDamage, fields, WeaponStatFields.ThrustDamage, underfunded, multiplier, additiveBonus);
		result.SwingDamage = ApplyPositiveStat(result.SwingDamage, fields, WeaponStatFields.SwingDamage, underfunded, multiplier, additiveBonus);
		result.FireDamage = ApplyPositiveStat(result.FireDamage, fields, WeaponStatFields.FireDamage, underfunded, multiplier, additiveBonus);
		result.Accuracy = ApplyPositiveStat(result.Accuracy, fields, WeaponStatFields.Accuracy, underfunded, multiplier, additiveBonus);
		result.Handling = ApplyPositiveStat(result.Handling, fields, WeaponStatFields.Handling, underfunded, multiplier, additiveBonus);
		if ((fields & WeaponStatFields.MaxDataValue) != 0)
		{
			result.MaxDataValue = ClampPositiveShort(ApplyBenefit(result.MaxDataValue, underfunded, multiplier, additiveBonus));
		}
		return result;
	}

	private static PlayerRpCraftArmorStatsSnapshot CreateArmorSnapshot(
		ItemObject.ItemTypeEnum itemType,
		ArmorComponent armor,
		bool underfunded,
		double multiplier,
		int additiveBonus)
	{
		ArmorStatFields fields = GetArmorStatFields(itemType, armor);
		PlayerRpCraftArmorStatsSnapshot result = new PlayerRpCraftArmorStatsSnapshot
		{
			AppliedFields = (long)fields,
			HeadArmor = armor.HeadArmor,
			BodyArmor = armor.BodyArmor,
			LegArmor = armor.LegArmor,
			ArmArmor = armor.ArmArmor,
			StealthFactor = armor.StealthFactor,
			ManeuverBonus = armor.ManeuverBonus,
			SpeedBonus = armor.SpeedBonus,
			ChargeBonus = armor.ChargeBonus
		};

		result.HeadArmor = ApplyPositiveStat(result.HeadArmor, fields, ArmorStatFields.HeadArmor, underfunded, multiplier, additiveBonus);
		result.BodyArmor = ApplyPositiveStat(result.BodyArmor, fields, ArmorStatFields.BodyArmor, underfunded, multiplier, additiveBonus);
		result.LegArmor = ApplyPositiveStat(result.LegArmor, fields, ArmorStatFields.LegArmor, underfunded, multiplier, additiveBonus);
		result.ArmArmor = ApplyPositiveStat(result.ArmArmor, fields, ArmorStatFields.ArmArmor, underfunded, multiplier, additiveBonus);
		result.StealthFactor = ApplyPositiveStat(result.StealthFactor, fields, ArmorStatFields.StealthFactor, underfunded, multiplier, additiveBonus);
		result.ManeuverBonus = ApplyPositiveStat(result.ManeuverBonus, fields, ArmorStatFields.ManeuverBonus, underfunded, multiplier, additiveBonus);
		result.SpeedBonus = ApplyPositiveStat(result.SpeedBonus, fields, ArmorStatFields.SpeedBonus, underfunded, multiplier, additiveBonus);
		result.ChargeBonus = ApplyPositiveStat(result.ChargeBonus, fields, ArmorStatFields.ChargeBonus, underfunded, multiplier, additiveBonus);
		return result;
	}

	private static WeaponComponent CreateWeaponComponentCopy(
		ItemObject target,
		WeaponComponent source,
		PlayerRpCraftItemStatsSnapshot snapshot)
	{
		WeaponComponent copy = new WeaponComponent(target);
		for (int i = 0; i < source.Weapons.Count; i++)
		{
			WeaponComponentData sourceMode = source.Weapons[i];
			PlayerRpCraftWeaponModeStatsSnapshot savedMode = snapshot.WeaponModes[i];
			WeaponComponentData modeCopy = new WeaponComponentData(target, sourceMode.WeaponClass, sourceMode.WeaponFlags);
			CopyProperties(sourceMode, modeCopy, WeaponStateProperties);
			modeCopy.WeaponFlags = sourceMode.WeaponFlags;
			ApplyWeaponModeSnapshot(modeCopy, savedMode);
			copy.AddWeapon(modeCopy, source.ItemModifierGroup);
		}
		return copy;
	}

	private static ArmorComponent CreateArmorComponentCopy(
		ItemObject target,
		ArmorComponent source,
		PlayerRpCraftArmorStatsSnapshot snapshot)
	{
		ArmorComponent copy = new ArmorComponent(target);
		CopyProperties(source, copy, ArmorStateProperties);
		CopyOptionalProperty(source, copy, ArmorIsNoSlimProperty);
		CopyOptionalProperty(source, copy, ItemComponentModifierGroupProperty);
		ApplyArmorSnapshot(copy, snapshot);
		return copy;
	}

	private static void ApplyWeaponModeSnapshot(
		WeaponComponentData target,
		PlayerRpCraftWeaponModeStatsSnapshot snapshot)
	{
		WeaponStatFields fields = (WeaponStatFields)snapshot.AppliedFields;
		SetIfApplied(target, WeaponBodyArmorProperty, fields, WeaponStatFields.BodyArmor, ClampPositiveInt(snapshot.BodyArmor));
		SetIfApplied(target, WeaponThrustSpeedProperty, fields, WeaponStatFields.ThrustSpeed, ClampPositiveInt(snapshot.ThrustSpeed));
		SetIfApplied(target, WeaponSwingSpeedProperty, fields, WeaponStatFields.SwingSpeed, ClampPositiveInt(snapshot.SwingSpeed));
		SetIfApplied(target, WeaponMissileSpeedProperty, fields, WeaponStatFields.MissileSpeed, ClampPositiveInt(snapshot.MissileSpeed));
		SetIfApplied(target, WeaponThrustDamageProperty, fields, WeaponStatFields.ThrustDamage, ClampPositiveInt(snapshot.ThrustDamage));
		SetIfApplied(target, WeaponSwingDamageProperty, fields, WeaponStatFields.SwingDamage, ClampPositiveInt(snapshot.SwingDamage));
		SetIfApplied(target, WeaponFireDamageProperty, fields, WeaponStatFields.FireDamage, ClampPositiveInt(snapshot.FireDamage));
		SetIfApplied(target, WeaponAccuracyProperty, fields, WeaponStatFields.Accuracy, ClampPositiveInt(snapshot.Accuracy));
		SetIfApplied(target, WeaponHandlingProperty, fields, WeaponStatFields.Handling, ClampPositiveInt(snapshot.Handling));
		if ((fields & WeaponStatFields.MaxDataValue) != 0)
		{
			WeaponMaxDataValueProperty.SetValue(target, (short)ClampPositiveShort(snapshot.MaxDataValue), null);
		}
	}

	private static void ApplyArmorSnapshot(ArmorComponent target, PlayerRpCraftArmorStatsSnapshot snapshot)
	{
		ArmorStatFields fields = (ArmorStatFields)snapshot.AppliedFields;
		SetIfApplied(target, ArmorHeadArmorProperty, fields, ArmorStatFields.HeadArmor, ClampPositiveInt(snapshot.HeadArmor));
		SetIfApplied(target, ArmorBodyArmorProperty, fields, ArmorStatFields.BodyArmor, ClampPositiveInt(snapshot.BodyArmor));
		SetIfApplied(target, ArmorLegArmorProperty, fields, ArmorStatFields.LegArmor, ClampPositiveInt(snapshot.LegArmor));
		SetIfApplied(target, ArmorArmArmorProperty, fields, ArmorStatFields.ArmArmor, ClampPositiveInt(snapshot.ArmArmor));
		SetIfApplied(target, ArmorStealthFactorProperty, fields, ArmorStatFields.StealthFactor, ClampPositiveInt(snapshot.StealthFactor));
		SetIfApplied(target, ArmorManeuverBonusProperty, fields, ArmorStatFields.ManeuverBonus, snapshot.ManeuverBonus);
		SetIfApplied(target, ArmorSpeedBonusProperty, fields, ArmorStatFields.SpeedBonus, snapshot.SpeedBonus);
		SetIfApplied(target, ArmorChargeBonusProperty, fields, ArmorStatFields.ChargeBonus, snapshot.ChargeBonus);
	}

	private static WeaponStatFields GetWeaponStatFields(ItemObject.ItemTypeEnum itemType, WeaponComponentData weapon)
	{
		if (weapon == null)
		{
			return WeaponStatFields.None;
		}
		if (weapon.IsShield
			|| (itemType == ItemObject.ItemTypeEnum.Shield
				&& !weapon.IsMeleeWeapon
				&& !weapon.IsRangedWeapon))
		{
			WeaponStatFields shield = WeaponStatFields.None;
			AddIfPositive(ref shield, WeaponStatFields.BodyArmor, weapon.BodyArmor);
			AddIfPositive(ref shield, WeaponStatFields.SwingSpeed, weapon.SwingSpeed);
			AddIfPositive(ref shield, WeaponStatFields.ThrustSpeed, weapon.ThrustSpeed);
			AddIfPositive(ref shield, WeaponStatFields.Handling, weapon.Handling);
			AddIfPositive(ref shield, WeaponStatFields.MaxDataValue, weapon.MaxDataValue);
			return shield;
		}

		WeaponStatFields fields = WeaponStatFields.None;
		bool melee = itemType == ItemObject.ItemTypeEnum.OneHandedWeapon
			|| itemType == ItemObject.ItemTypeEnum.TwoHandedWeapon
			|| itemType == ItemObject.ItemTypeEnum.Polearm
			|| itemType == ItemObject.ItemTypeEnum.Thrown
			|| weapon.IsMeleeWeapon;
		bool ranged = itemType == ItemObject.ItemTypeEnum.Bow
			|| itemType == ItemObject.ItemTypeEnum.Crossbow
			|| itemType == ItemObject.ItemTypeEnum.Sling
			|| itemType == ItemObject.ItemTypeEnum.Pistol
			|| itemType == ItemObject.ItemTypeEnum.Musket
			|| itemType == ItemObject.ItemTypeEnum.Thrown
			|| IsAmmoType(itemType)
			|| weapon.IsRangedWeapon
			|| weapon.IsAmmo;

		if (melee)
		{
			AddIfPositive(ref fields, WeaponStatFields.SwingDamage, weapon.SwingDamage);
			AddIfPositive(ref fields, WeaponStatFields.ThrustDamage, weapon.ThrustDamage);
			AddIfPositive(ref fields, WeaponStatFields.SwingSpeed, weapon.SwingSpeed);
			AddIfPositive(ref fields, WeaponStatFields.ThrustSpeed, weapon.ThrustSpeed);
			AddIfPositive(ref fields, WeaponStatFields.Handling, weapon.Handling);
		}
		if (ranged)
		{
			AddIfPositive(ref fields, WeaponStatFields.SwingDamage, weapon.SwingDamage);
			AddIfPositive(ref fields, WeaponStatFields.ThrustDamage, weapon.ThrustDamage);
			AddIfPositive(ref fields, WeaponStatFields.SwingSpeed, weapon.SwingSpeed);
			AddIfPositive(ref fields, WeaponStatFields.ThrustSpeed, weapon.ThrustSpeed);
			AddIfPositive(ref fields, WeaponStatFields.MissileSpeed, weapon.MissileSpeed);
			AddIfPositive(ref fields, WeaponStatFields.Accuracy, weapon.Accuracy);
			AddIfPositive(ref fields, WeaponStatFields.Handling, weapon.Handling);
		}
		AddIfPositive(ref fields, WeaponStatFields.FireDamage, weapon.FireDamage);
		return fields;
	}

	private static ArmorStatFields GetArmorStatFields(ItemObject.ItemTypeEnum itemType, ArmorComponent armor)
	{
		ArmorStatFields fields = ArmorStatFields.None;
		if (itemType == ItemObject.ItemTypeEnum.HorseHarness)
		{
			AddIfPositive(ref fields, ArmorStatFields.BodyArmor, armor.BodyArmor);
			AddIfPositive(ref fields, ArmorStatFields.ManeuverBonus, armor.ManeuverBonus);
			AddIfPositive(ref fields, ArmorStatFields.SpeedBonus, armor.SpeedBonus);
			AddIfPositive(ref fields, ArmorStatFields.ChargeBonus, armor.ChargeBonus);
		}
		else
		{
			AddIfPositive(ref fields, ArmorStatFields.HeadArmor, armor.HeadArmor);
			AddIfPositive(ref fields, ArmorStatFields.BodyArmor, armor.BodyArmor);
			AddIfPositive(ref fields, ArmorStatFields.LegArmor, armor.LegArmor);
			AddIfPositive(ref fields, ArmorStatFields.ArmArmor, armor.ArmArmor);
		}
		return fields;
	}

	private static float CalculateFinalWeight(float baseWeight, bool underfunded, double multiplier, int additiveBonus)
	{
		double safeBase = IsFinite(baseWeight) && baseWeight > 0f ? baseWeight : MinimumPositiveWeight;
		double result = underfunded
			? safeBase / Math.Sqrt(multiplier)
			: safeBase * Math.Pow(WeightReductionPerBonusPoint, additiveBonus);
		return ClampWeight(result);
	}

	private static int ApplyPositiveStat<TField>(
		int value,
		TField fields,
		TField field,
		bool underfunded,
		double multiplier,
		int additiveBonus)
		where TField : struct
	{
		long mask = Convert.ToInt64(fields, CultureInfo.InvariantCulture);
		long flag = Convert.ToInt64(field, CultureInfo.InvariantCulture);
		if ((mask & flag) == 0L)
		{
			return value;
		}
		return ClampPositiveInt(ApplyBenefit(value, underfunded, multiplier, additiveBonus));
	}

	private static double ApplyBenefit(int value, bool underfunded, double multiplier, int additiveBonus)
	{
		return underfunded ? (double)value * multiplier : (double)value + additiveBonus;
	}

	private static void CopyProperties(object source, object target, PropertyInfo[] properties)
	{
		foreach (PropertyInfo property in properties)
		{
			CopyOptionalProperty(source, target, property);
		}
	}

	private static void CopyOptionalProperty(object source, object target, PropertyInfo property)
	{
		if (source == null || target == null || property == null || property.GetSetMethod(true) == null)
		{
			return;
		}
		property.SetValue(target, property.GetValue(source, null), null);
	}

	private static PropertyInfo[] BuildPropertyCache(Type type, params string[] propertyNames)
	{
		List<PropertyInfo> properties = new List<PropertyInfo>(propertyNames?.Length ?? 0);
		foreach (string propertyName in propertyNames ?? Array.Empty<string>())
		{
			PropertyInfo property = type.GetProperty(
				propertyName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null && property.CanRead && property.GetSetMethod(true) != null)
			{
				properties.Add(property);
			}
		}
		return properties.ToArray();
	}

	private static PropertyInfo GetRequiredProperty(Type type, string propertyName)
	{
		return type.GetProperty(
			propertyName,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	}

	private static bool HaveCompatibleWeaponSemantics(WeaponFlags saved, WeaponFlags current)
	{
		const WeaponFlags structuralMask =
			WeaponFlags.WeaponMask
			| WeaponFlags.FirearmAmmo
			| WeaponFlags.HandUsageMask
			| WeaponFlags.WideGrip
			| WeaponFlags.AttachAmmoToVisual
			| WeaponFlags.Consumable
			| WeaponFlags.DataValueMask
			| WeaponFlags.StringHeldByHand
			| WeaponFlags.UnloadWhenSheathed
			| WeaponFlags.CantReloadOnHorseback
			| WeaponFlags.AutoReload
			| WeaponFlags.PenaltyWithShield
			| WeaponFlags.CanBlockRanged
			| WeaponFlags.MissileWithPhysics
			| WeaponFlags.UseHandAsThrowBase
			| WeaponFlags.HeldBackwards;
		return (saved & structuralMask) == (current & structuralMask);
	}

	private static bool IsSupportedEquipmentType(ItemObject.ItemTypeEnum type)
	{
		return (type >= ItemObject.ItemTypeEnum.OneHandedWeapon && type <= ItemObject.ItemTypeEnum.Thrown)
			|| (type >= ItemObject.ItemTypeEnum.HeadArmor && type <= ItemObject.ItemTypeEnum.HandArmor)
			|| (type >= ItemObject.ItemTypeEnum.Pistol && type <= ItemObject.ItemTypeEnum.Bullets)
			|| type == ItemObject.ItemTypeEnum.ChestArmor
			|| type == ItemObject.ItemTypeEnum.Cape
			|| type == ItemObject.ItemTypeEnum.HorseHarness;
	}

	private static bool IsAmmoType(ItemObject.ItemTypeEnum type)
	{
		return type == ItemObject.ItemTypeEnum.Arrows
			|| type == ItemObject.ItemTypeEnum.Bolts
			|| type == ItemObject.ItemTypeEnum.SlingStones
			|| type == ItemObject.ItemTypeEnum.Bullets;
	}

	private static bool IsFinite(double value)
	{
		return !double.IsNaN(value) && !double.IsInfinity(value);
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	private static int ClampPositiveInt(double value)
	{
		if (double.IsNaN(value))
		{
			return 1;
		}
		if (value >= int.MaxValue)
		{
			return int.MaxValue;
		}
		if (value <= 1d)
		{
			return 1;
		}
		return (int)Math.Round(value, MidpointRounding.AwayFromZero);
	}

	private static int ClampPositiveShort(double value)
	{
		if (double.IsNaN(value))
		{
			return 1;
		}
		if (value >= short.MaxValue)
		{
			return short.MaxValue;
		}
		if (value <= 1d)
		{
			return 1;
		}
		return (int)Math.Round(value, MidpointRounding.AwayFromZero);
	}

	private static float ClampWeight(double value)
	{
		if (double.IsNaN(value) || value <= MinimumPositiveWeight)
		{
			return MinimumPositiveWeight;
		}
		if (double.IsInfinity(value) || value >= float.MaxValue)
		{
			return float.MaxValue;
		}
		return (float)value;
	}

	private static void AddIfPositive(ref WeaponStatFields fields, WeaponStatFields field, int value)
	{
		if (value > 0)
		{
			fields |= field;
		}
	}

	private static void AddIfPositive(ref ArmorStatFields fields, ArmorStatFields field, int value)
	{
		if (value > 0)
		{
			fields |= field;
		}
	}

	private static void SetIfApplied<TField>(
		object target,
		PropertyInfo property,
		TField fields,
		TField field,
		int value)
		where TField : struct
	{
		long mask = Convert.ToInt64(fields, CultureInfo.InvariantCulture);
		long flag = Convert.ToInt64(field, CultureInfo.InvariantCulture);
		if ((mask & flag) != 0L)
		{
			if (property?.GetSetMethod(true) == null)
			{
				throw new MissingMemberException(property?.DeclaringType?.FullName ?? "", property?.Name ?? "");
			}
			property.SetValue(target, value, null);
		}
	}

	private static void AppendStat<TField>(
		StringBuilder builder,
		TField fields,
		TField field,
		string name,
		int value)
		where TField : struct
	{
		long mask = Convert.ToInt64(fields, CultureInfo.InvariantCulture);
		long flag = Convert.ToInt64(field, CultureInfo.InvariantCulture);
		if ((mask & flag) == 0L)
		{
			return;
		}
		if (builder.Length > 0 && builder[builder.Length - 1] != '[')
		{
			builder.Append(',');
		}
		builder.Append(name)
			.Append('=')
			.Append(value.ToString(CultureInfo.InvariantCulture));
	}
}
