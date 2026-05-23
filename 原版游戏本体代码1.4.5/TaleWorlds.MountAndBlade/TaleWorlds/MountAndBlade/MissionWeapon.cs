using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public struct MissionWeapon
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ImpactSoundModifier
	{
		public const string ModifierName = "impactModifier";

		public const float None = 0f;

		public const float ActiveBlock = 0.1f;

		public const float ChamberBlocked = 0.2f;

		public const float CrushThrough = 0.3f;
	}

	private class MissionSubWeapon
	{
		public MissionWeapon Value { get; private set; }

		public MissionSubWeapon(MissionWeapon subWeapon)
		{
			Value = subWeapon;
		}
	}

	public delegate void OnGetWeaponDataDelegate(ref WeaponData weaponData, MissionWeapon weapon, bool isFemale, Banner banner, bool needBatchedVersion);

	public const short ReloadPhaseCountMax = 10;

	public static OnGetWeaponDataDelegate OnGetWeaponDataHandler;

	public static readonly MissionWeapon Invalid = new MissionWeapon(null, null, null);

	private readonly List<WeaponComponentData> _weapons;

	public int CurrentUsageIndex;

	private bool _hasAnyConsumableUsage;

	private short _dataValue;

	private short _modifiedMaxDataValue;

	private MissionSubWeapon _ammoWeapon;

	private List<MissionSubWeapon> _attachedWeapons;

	private List<MatrixFrame> _attachedWeaponFrames;

	public ItemObject Item { get; private set; }

	public ItemModifier ItemModifier { get; private set; }

	public int WeaponsCount => _weapons.Count;

	public WeaponComponentData CurrentUsageItem
	{
		get
		{
			if (_weapons == null || _weapons.Count == 0)
			{
				return null;
			}
			return _weapons[CurrentUsageIndex];
		}
	}

	public short ReloadPhase { get; set; }

	public short ReloadPhaseCount
	{
		get
		{
			short result = 1;
			if (CurrentUsageItem != null)
			{
				result = CurrentUsageItem.ReloadPhaseCount;
			}
			return result;
		}
	}

	public bool IsReloading => ReloadPhase < ReloadPhaseCount;

	public Banner Banner { get; private set; }

	public float GlossMultiplier { get; private set; }

	public short RawDataForNetwork => _dataValue;

	public short HitPoints
	{
		get
		{
			return _dataValue;
		}
		set
		{
			_dataValue = value;
		}
	}

	public short Amount
	{
		get
		{
			return _dataValue;
		}
		set
		{
			_dataValue = value;
		}
	}

	public short Ammo => _ammoWeapon?.Value._dataValue ?? 0;

	public MissionWeapon AmmoWeapon => _ammoWeapon?.Value ?? Invalid;

	public short MaxAmmo => _modifiedMaxDataValue;

	public short ModifiedMaxAmount => _modifiedMaxDataValue;

	public short ModifiedMaxHitPoints => _modifiedMaxDataValue;

	public bool IsEmpty => CurrentUsageItem == null;

	public MissionWeapon(ItemObject item, ItemModifier itemModifier, Banner banner)
	{
		Item = item;
		ItemModifier = itemModifier;
		Banner = banner;
		CurrentUsageIndex = 0;
		_weapons = new List<WeaponComponentData>(1);
		_modifiedMaxDataValue = 0;
		_hasAnyConsumableUsage = false;
		if (item != null && item.Weapons != null)
		{
			foreach (WeaponComponentData weapon in item.Weapons)
			{
				_weapons.Add(weapon);
				bool isConsumable = weapon.IsConsumable;
				if (isConsumable || weapon.IsRangedWeapon || weapon.WeaponFlags.HasAnyFlag(WeaponFlags.HasHitPoints))
				{
					_modifiedMaxDataValue = weapon.MaxDataValue;
					if (itemModifier != null)
					{
						if (weapon.WeaponFlags.HasAnyFlag(WeaponFlags.HasHitPoints))
						{
							_modifiedMaxDataValue = weapon.GetModifiedMaximumHitPoints(itemModifier);
						}
						else if (isConsumable)
						{
							_modifiedMaxDataValue = weapon.GetModifiedStackCount(itemModifier);
						}
					}
				}
				if (isConsumable)
				{
					_hasAnyConsumableUsage = true;
				}
			}
		}
		_dataValue = _modifiedMaxDataValue;
		ReloadPhase = 0;
		_ammoWeapon = null;
		_attachedWeapons = null;
		_attachedWeaponFrames = null;
		GlossMultiplier = 1f;
	}

	public MissionWeapon(ItemObject primaryItem, ItemModifier itemModifier, Banner banner, short dataValue)
		: this(primaryItem, itemModifier, banner)
	{
		_dataValue = dataValue;
	}

	public MissionWeapon(ItemObject primaryItem, ItemModifier itemModifier, Banner banner, short dataValue, short reloadPhase, MissionWeapon? ammoWeapon)
		: this(primaryItem, itemModifier, banner, dataValue)
	{
		ReloadPhase = reloadPhase;
		_ammoWeapon = (ammoWeapon.HasValue ? new MissionSubWeapon(ammoWeapon.Value) : null);
	}

	public TextObject GetModifiedItemName()
	{
		if (ItemModifier == null)
		{
			return Item.Name;
		}
		TextObject name = ItemModifier.Name;
		name.SetTextVariable("ITEMNAME", Item.Name);
		return name;
	}

	public bool IsEqualTo(MissionWeapon other)
	{
		return Item == other.Item;
	}

	public bool IsSameType(MissionWeapon other)
	{
		return Item.PrimaryWeapon.WeaponClass == other.Item.PrimaryWeapon.WeaponClass;
	}

	public float GetWeight()
	{
		return (Item.PrimaryWeapon.IsConsumable ? (GetBaseWeight() * (float)_dataValue) : GetBaseWeight()) + (_ammoWeapon?.Value.GetWeight() ?? 0f);
	}

	private float GetBaseWeight()
	{
		return Item.Weight;
	}

	public WeaponComponentData GetWeaponComponentDataForUsage(int usageIndex)
	{
		return _weapons[usageIndex];
	}

	public int GetGetModifiedArmorForCurrentUsage()
	{
		return _weapons[CurrentUsageIndex].GetModifiedArmor(ItemModifier);
	}

	public int GetModifiedThrustDamageForCurrentUsage()
	{
		return _weapons[CurrentUsageIndex].GetModifiedThrustDamage(ItemModifier);
	}

	public int GetModifiedSwingDamageForCurrentUsage()
	{
		return _weapons[CurrentUsageIndex].GetModifiedSwingDamage(ItemModifier);
	}

	public int GetModifiedMissileDamageForCurrentUsage()
	{
		return _weapons[CurrentUsageIndex].GetModifiedMissileDamage(ItemModifier);
	}

	public int GetModifiedThrustSpeedForCurrentUsage()
	{
		return _weapons[CurrentUsageIndex].GetModifiedThrustSpeed(ItemModifier);
	}

	public int GetModifiedSwingSpeedForCurrentUsage()
	{
		return _weapons[CurrentUsageIndex].GetModifiedSwingSpeed(ItemModifier);
	}

	public int GetModifiedMissileSpeedForCurrentUsage()
	{
		return _weapons[CurrentUsageIndex].GetModifiedMissileSpeed(ItemModifier);
	}

	public int GetModifiedMissileSpeedForUsage(int usageIndex)
	{
		return _weapons[usageIndex].GetModifiedMissileSpeed(ItemModifier);
	}

	public int GetModifiedHandlingForCurrentUsage()
	{
		return _weapons[CurrentUsageIndex].GetModifiedHandling(ItemModifier);
	}

	public WeaponData GetWeaponData(bool needBatchedVersionForMeshes)
	{
		if (!IsEmpty && Item.WeaponComponent != null)
		{
			WeaponComponent weaponComponent = Item.WeaponComponent;
			WeaponData weaponData = new WeaponData
			{
				WeaponKind = (int)Item.Id.InternalValue,
				ItemHolsterIndices = Item.GetItemHolsterIndices(),
				ReloadPhase = ReloadPhase,
				Difficulty = Item.Difficulty,
				BaseWeight = GetBaseWeight(),
				HasFlagAnimation = false,
				WeaponFrame = weaponComponent.PrimaryWeapon.Frame,
				ScaleFactor = Item.ScaleFactor,
				TotalInertia = weaponComponent.PrimaryWeapon.TotalInertia,
				CenterOfMass = weaponComponent.PrimaryWeapon.CenterOfMass,
				CenterOfMass3D = weaponComponent.PrimaryWeapon.CenterOfMass3D,
				HolsterPositionShift = Item.HolsterPositionShift,
				TrailParticleName = weaponComponent.PrimaryWeapon.TrailParticleName,
				SkeletonName = Item.SkeletonName,
				StaticAnimationName = Item.StaticAnimationName,
				AmmoOffset = weaponComponent.PrimaryWeapon.AmmoOffset
			};
			string physicsMaterial = weaponComponent.PrimaryWeapon.PhysicsMaterial;
			weaponData.PhysicsMaterialIndex = (string.IsNullOrEmpty(physicsMaterial) ? PhysicsMaterial.InvalidPhysicsMaterial.Index : PhysicsMaterial.GetFromName(physicsMaterial).Index);
			weaponData.FlyingSoundCode = SoundManager.GetEventGlobalIndex(weaponComponent.PrimaryWeapon.FlyingSoundCode);
			weaponData.PassbySoundCode = SoundManager.GetEventGlobalIndex(weaponComponent.PrimaryWeapon.PassbySoundCode);
			weaponData.StickingFrame = weaponComponent.PrimaryWeapon.StickingFrame;
			weaponData.CollisionShape = ((!needBatchedVersionForMeshes || string.IsNullOrEmpty(Item.CollisionBodyName)) ? null : PhysicsShape.GetFromResource(Item.CollisionBodyName));
			weaponData.Shape = ((!needBatchedVersionForMeshes || string.IsNullOrEmpty(Item.BodyName)) ? null : PhysicsShape.GetFromResource(Item.BodyName));
			weaponData.DataValue = _dataValue;
			weaponData.CurrentUsageIndex = CurrentUsageIndex;
			int rangedUsageIndex = GetRangedUsageIndex();
			if (GetConsumableIfAny(out var consumableWeapon))
			{
				weaponData.AirFrictionConstant = ItemObject.GetAirFrictionConstant(consumableWeapon.WeaponClass, consumableWeapon.WeaponFlags);
			}
			else if (rangedUsageIndex >= 0)
			{
				weaponData.AirFrictionConstant = ItemObject.GetAirFrictionConstant(GetWeaponComponentDataForUsage(rangedUsageIndex).WeaponClass, GetWeaponComponentDataForUsage(rangedUsageIndex).WeaponFlags);
			}
			weaponData.GlossMultiplier = GlossMultiplier;
			weaponData.HasLowerHolsterPriority = Item.HasLowerHolsterPriority;
			OnGetWeaponDataHandler?.Invoke(ref weaponData, this, isFemale: false, Banner, needBatchedVersionForMeshes);
			return weaponData;
		}
		return WeaponData.InvalidWeaponData;
	}

	public WeaponStatsData[] GetWeaponStatsData()
	{
		WeaponStatsData[] array = new WeaponStatsData[_weapons.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = GetWeaponStatsDataForUsage(i);
		}
		return array;
	}

	public WeaponStatsData GetWeaponStatsDataForUsage(int usageIndex)
	{
		WeaponStatsData result = default(WeaponStatsData);
		WeaponComponentData weaponComponentData = _weapons[usageIndex];
		result.WeaponClass = (int)weaponComponentData.WeaponClass;
		result.AmmoClass = (int)weaponComponentData.AmmoClass;
		result.Properties = (uint)Item.ItemFlags;
		result.WeaponFlags = (ulong)weaponComponentData.WeaponFlags;
		result.ItemUsageIndex = (string.IsNullOrEmpty(weaponComponentData.ItemUsage) ? (-1) : weaponComponentData.GetItemUsageIndex());
		result.ThrustSpeed = weaponComponentData.GetModifiedThrustSpeed(ItemModifier);
		result.SwingSpeed = weaponComponentData.GetModifiedSwingSpeed(ItemModifier);
		result.MissileSpeed = weaponComponentData.GetModifiedMissileSpeed(ItemModifier);
		result.ShieldArmor = weaponComponentData.GetModifiedArmor(ItemModifier);
		result.Accuracy = weaponComponentData.Accuracy;
		result.WeaponLength = weaponComponentData.WeaponLength;
		result.WeaponBalance = weaponComponentData.WeaponBalance;
		result.ThrustDamage = weaponComponentData.GetModifiedThrustDamage(ItemModifier);
		result.ThrustDamageType = (int)weaponComponentData.ThrustDamageType;
		result.SwingDamage = weaponComponentData.GetModifiedSwingDamage(ItemModifier);
		result.SwingDamageType = (int)weaponComponentData.SwingDamageType;
		result.DefendSpeed = weaponComponentData.GetModifiedHandling(ItemModifier);
		result.SweetSpot = weaponComponentData.SweetSpotReach;
		result.MaxDataValue = _modifiedMaxDataValue;
		result.WeaponFrame = weaponComponentData.Frame;
		result.RotationSpeed = weaponComponentData.RotationSpeed;
		result.ReloadPhaseCount = weaponComponentData.ReloadPhaseCount;
		return result;
	}

	public WeaponData GetAmmoWeaponData(bool needBatchedVersion)
	{
		return AmmoWeapon.GetWeaponData(needBatchedVersion);
	}

	public WeaponStatsData[] GetAmmoWeaponStatsData()
	{
		return AmmoWeapon.GetWeaponStatsData();
	}

	public int GetAttachedWeaponsCount()
	{
		return _attachedWeapons?.Count ?? 0;
	}

	public MissionWeapon GetAttachedWeapon(int attachmentIndex)
	{
		return _attachedWeapons[attachmentIndex].Value;
	}

	public MatrixFrame GetAttachedWeaponFrame(int attachmentIndex)
	{
		return _attachedWeaponFrames[attachmentIndex];
	}

	public bool IsShield()
	{
		if (_weapons.Count == 1)
		{
			return _weapons[0].IsShield;
		}
		return false;
	}

	public bool IsBanner()
	{
		if (_weapons.Count == 1)
		{
			return _weapons[0].WeaponClass == WeaponClass.Banner;
		}
		return false;
	}

	public bool IsAnyAmmo()
	{
		foreach (WeaponComponentData weapon in _weapons)
		{
			if (weapon.IsAmmo)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasAnyUsageWithWeaponClass(WeaponClass weaponClass)
	{
		foreach (WeaponComponentData weapon in _weapons)
		{
			if (weapon.WeaponClass == weaponClass)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasAnyUsageWithAmmoClass(WeaponClass ammoClass)
	{
		foreach (WeaponComponentData weapon in _weapons)
		{
			if (weapon.AmmoClass == ammoClass)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasAllUsagesWithAnyWeaponFlag(WeaponFlags flags)
	{
		foreach (WeaponComponentData weapon in _weapons)
		{
			if (!weapon.WeaponFlags.HasAnyFlag(flags))
			{
				return false;
			}
		}
		return true;
	}

	public bool HasAnyUsageWithoutWeaponFlag(WeaponFlags flags)
	{
		foreach (WeaponComponentData weapon in _weapons)
		{
			if (!weapon.WeaponFlags.HasAnyFlag(flags))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasAnyUsageWithItemUsageSetFlags(ItemObject.ItemUsageSetFlags flags)
	{
		foreach (WeaponComponentData weapon in _weapons)
		{
			if (MBItem.GetItemUsageSetFlags(weapon.ItemUsage).HasAllFlags(flags))
			{
				return true;
			}
		}
		return false;
	}

	public void GatherInformationFromWeapon(out bool weaponHasMelee, out bool weaponHasShield, out bool weaponHasPolearm, out bool weaponHasNonConsumableRanged, out bool weaponHasThrown, out WeaponClass rangedAmmoClass)
	{
		weaponHasMelee = false;
		weaponHasShield = false;
		weaponHasPolearm = false;
		weaponHasNonConsumableRanged = false;
		weaponHasThrown = false;
		rangedAmmoClass = WeaponClass.Undefined;
		foreach (WeaponComponentData weapon in _weapons)
		{
			weaponHasMelee = weaponHasMelee || weapon.IsMeleeWeapon;
			weaponHasShield = weaponHasShield || weapon.IsShield;
			weaponHasPolearm = weapon.IsPolearm;
			if (weapon.IsRangedWeapon)
			{
				weaponHasThrown = weapon.IsConsumable;
				weaponHasNonConsumableRanged = !weaponHasThrown;
				rangedAmmoClass = weapon.AmmoClass;
			}
		}
	}

	public bool GetConsumableIfAny(out WeaponComponentData consumableWeapon)
	{
		consumableWeapon = null;
		if (_hasAnyConsumableUsage)
		{
			foreach (WeaponComponentData weapon in _weapons)
			{
				if (weapon.IsConsumable)
				{
					consumableWeapon = weapon;
					break;
				}
			}
			return true;
		}
		return false;
	}

	public bool IsAnyConsumable()
	{
		return _hasAnyConsumableUsage;
	}

	public int GetRangedUsageIndex()
	{
		for (int i = 0; i < _weapons.Count; i++)
		{
			if (_weapons[i].IsRangedWeapon)
			{
				return i;
			}
		}
		return -1;
	}

	public MissionWeapon Consume(short count)
	{
		Amount -= count;
		return new MissionWeapon(Item, ItemModifier, Banner, count, 0, null);
	}

	public void ConsumeAmmo(short count)
	{
		if (count > 0)
		{
			MissionWeapon value = _ammoWeapon.Value;
			value.Amount = count;
			_ammoWeapon = new MissionSubWeapon(value);
		}
		else
		{
			_ammoWeapon = null;
		}
	}

	public void SetAmmo(MissionWeapon ammoWeapon)
	{
		_ammoWeapon = new MissionSubWeapon(ammoWeapon);
	}

	public void ReloadAmmo(MissionWeapon ammoWeapon, short reloadPhase)
	{
		if (_ammoWeapon != null && _ammoWeapon.Value.Amount >= 0)
		{
			ammoWeapon.Amount += _ammoWeapon.Value.Amount;
		}
		_ammoWeapon = new MissionSubWeapon(ammoWeapon);
		ReloadPhase = reloadPhase;
	}

	public void AttachWeapon(MissionWeapon attachedWeapon, ref MatrixFrame attachFrame)
	{
		if (_attachedWeapons == null)
		{
			_attachedWeapons = new List<MissionSubWeapon>();
			_attachedWeaponFrames = new List<MatrixFrame>();
		}
		_attachedWeapons.Add(new MissionSubWeapon(attachedWeapon));
		_attachedWeaponFrames.Add(attachFrame);
	}

	public void RemoveAttachedWeapon(int attachmentIndex)
	{
		_attachedWeapons.RemoveAt(attachmentIndex);
		_attachedWeaponFrames.RemoveAt(attachmentIndex);
	}

	public bool HasEnoughSpaceForAmount(int amount)
	{
		return ModifiedMaxAmount - Amount >= amount;
	}

	public void SetRandomGlossMultiplier(int seed)
	{
		Random random = new Random(seed);
		float glossMultiplier = 1f + (random.NextFloat() * 2f - 1f) * 0.3f;
		GlossMultiplier = glossMultiplier;
	}

	public void AddExtraModifiedMaxValue(short extraValue)
	{
		_modifiedMaxDataValue += extraValue;
	}
}
