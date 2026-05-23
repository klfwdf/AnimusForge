using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Naval;

public sealed class Ship : IShipOrigin, IRandomOwner
{
	[SaveableField(1)]
	private readonly Dictionary<string, ShipUpgradePiece> _shipPieces = new Dictionary<string, ShipUpgradePiece>();

	[SaveableField(2)]
	public readonly ShipHull ShipHull;

	[SaveableField(3)]
	private float _hitPoints;

	[SaveableField(12)]
	private float _sailHitPoints;

	[SaveableField(4)]
	private PartyBase _owner;

	[SaveableField(5)]
	private TextObject _name;

	[SaveableField(6)]
	private MBList<ShipUpgradePiece> _unlockedUpgradePieces;

	private uint _versionNo;

	private bool _isVersionDirty = true;

	[SaveableProperty(7)]
	public Figurehead Figurehead { get; private set; }

	[SaveableProperty(8)]
	public bool IsInvulnerable { get; set; }

	[SaveableProperty(9)]
	public bool IsTradeable { get; set; } = true;

	[SaveableProperty(10)]
	public bool IsUsedByQuest { get; set; }

	[SaveableProperty(11)]
	public int RandomValue { get; private set; } = MBRandom.RandomInt(1, int.MaxValue);

	public string CustomSailPatternId { get; set; } = "";

	public MBReadOnlyList<ShipUpgradePiece> UnlockedUpgradePieces => _unlockedUpgradePieces;

	public TextObject Name
	{
		get
		{
			if (!TextObject.IsNullOrEmpty(_name))
			{
				return _name;
			}
			return ShipHull.Name;
		}
	}

	public uint VersionNo
	{
		get
		{
			if (_isVersionDirty)
			{
				_isVersionDirty = false;
				if (ShipHull == null)
				{
					return 0u;
				}
				uint internalValue = ShipHull.Id.InternalValue;
				uint num = 0u;
				foreach (KeyValuePair<string, ShipUpgradePiece> shipPiece in _shipPieces)
				{
					num = (num * 31) ^ (shipPiece.Value?.Id.InternalValue ?? 0);
				}
				if (Figurehead != null)
				{
					num ^= Figurehead.Id.InternalValue;
				}
				if (!string.IsNullOrEmpty(CustomSailPatternId))
				{
					num = (num * 31) ^ (uint)CustomSailPatternId.GetHashCode();
				}
				internalValue += num;
				_versionNo = internalValue;
			}
			return _versionNo;
		}
	}

	public PartyBase Owner
	{
		get
		{
			return _owner;
		}
		set
		{
			if (_owner != value)
			{
				PartyBase owner = _owner;
				_owner = value;
				owner?.RemoveShipInternal(this);
				_owner?.AddShipInternal(this);
				ResetUnlockedUpgradePieces();
			}
		}
	}

	public float HitPoints
	{
		get
		{
			return _hitPoints;
		}
		set
		{
			float hitPoints = TaleWorlds.Library.MathF.Clamp(value, 0f, MaxHitPoints);
			_hitPoints = hitPoints;
		}
	}

	public float MaxHitPoints
	{
		get
		{
			float num = 1f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.MaxHitPointsBonusMultiplier;
				}
			}
			return (float)ShipHull.MaxHitPoints * num;
		}
	}

	public float MaxFireHitPoints
	{
		get
		{
			float num = 1f;
			return (float)ShipHull.MaxFireHitPoints * num;
		}
	}

	public float SailHitPoints
	{
		get
		{
			return _sailHitPoints;
		}
		set
		{
			float sailHitPoints = TaleWorlds.Library.MathF.Clamp(value, 0f, MaxSailHitPoints);
			_sailHitPoints = sailHitPoints;
		}
	}

	public int TotalCrewCapacity
	{
		get
		{
			int totalCrewCapacity = ShipHull.TotalCrewCapacity;
			float num = 1f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.CrewCapacityBonusMultiplier;
				}
			}
			return (int)((float)totalCrewCapacity * num);
		}
	}

	public float MaxSailHitPoints
	{
		get
		{
			float num = 1f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.MaxSailHitPointsBonusMultiplier;
				}
			}
			return (float)ShipHull.MaxSailHitPoints * num;
		}
	}

	public int SeaWorthiness
	{
		get
		{
			int num = 0;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.SeaWorthinessBonus;
				}
			}
			return ShipHull.SeaWorthiness + num;
		}
	}

	public float FlagshipScore => Campaign.Current.Models.ShipStatModel.GetShipFlagshipScore(this);

	ShipHull IShipOrigin.Hull => ShipHull;

	public int MainDeckCrewCapacity
	{
		get
		{
			int mainDeckCrewCapacity = ShipHull.MainDeckCrewCapacity;
			float num = 1f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.CrewCapacityBonusMultiplier;
				}
			}
			return (int)((float)mainDeckCrewCapacity * num);
		}
	}

	public float InventoryCapacity
	{
		get
		{
			float num = 1f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.InventoryCapacityBonusMultiplier;
				}
			}
			return (float)ShipHull.InventoryCapacity * num;
		}
	}

	public int SkeletalCrewCapacity => ShipHull.SkeletalCrewCapacity;

	public float CrewCapacityBonusFactor => Campaign.Current.Models.CampaignShipParametersModel.GetCrewCapacityBonusFactor(this);

	public float ShipWeightFactor => Campaign.Current.Models.CampaignShipParametersModel.GetShipWeightFactor(this);

	public float ForwardDragFactor => Campaign.Current.Models.CampaignShipParametersModel.GetForwardDragFactor(this);

	public float CrewShieldHitPointsFactor => Campaign.Current.Models.CampaignShipParametersModel.GetCrewShieldHitPointsFactor(this);

	public int AdditionalAmmo => Campaign.Current.Models.CampaignShipParametersModel.GetAdditionalAmmoBonus(this);

	public float MaxOarPowerFactor => Campaign.Current.Models.CampaignShipParametersModel.GetMaxOarPowerFactor(this);

	public float MaxOarForceFactor => Campaign.Current.Models.CampaignShipParametersModel.GetMaxOarForceFactor(this);

	public float SailForceFactor => Campaign.Current.Models.CampaignShipParametersModel.GetSailForceFactor(this);

	public float CrewMeleeDamageFactor => Campaign.Current.Models.CampaignShipParametersModel.GetCrewMeleeDamageFactor(this);

	public int AdditionalArcherQuivers => Campaign.Current.Models.CampaignShipParametersModel.GetAdditionalArcherQuivers(this);

	public int AdditionalThrowingWeaponStack => Campaign.Current.Models.CampaignShipParametersModel.GetAdditionalThrowingWeaponStack(this);

	public float SailRotationSpeedFactor => Campaign.Current.Models.CampaignShipParametersModel.GetSailRotationSpeedFactor(this);

	public float FurlUnfurlSpeedFactor => Campaign.Current.Models.CampaignShipParametersModel.GetFurlUnfurlSpeedFactor(this);

	public float RudderSurfaceAreaFactor
	{
		get
		{
			float num = 0f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.RudderSurfaceAreaBonusMultiplier;
				}
			}
			return num;
		}
	}

	public float MaxRudderForceFactor
	{
		get
		{
			float num = 0f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.MaxRudderForceBonusMultiplier;
				}
			}
			return num;
		}
	}

	public bool CanEquipFigurehead => ShipHull.CanEquipFigurehead;

	int IShipOrigin.DefaultFormationGroupIndex => ShipHull.DefaultFormationGroup;

	string IShipOrigin.OriginShipId => ShipHull.MissionShipObjectId;

	bool IShipOrigin.IsPlayerShip => _owner == PartyBase.MainParty;

	public float CampaignSpeedBonusFactor => Campaign.Current.Models.CampaignShipParametersModel.GetCampaignSpeedBonusFactor(this);

	internal static void AutoGeneratedStaticCollectObjectsShip(object o, List<object> collectedObjects)
	{
		((Ship)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		collectedObjects.Add(ShipHull);
		collectedObjects.Add(_shipPieces);
		collectedObjects.Add(_owner);
		collectedObjects.Add(_name);
		collectedObjects.Add(_unlockedUpgradePieces);
		collectedObjects.Add(Figurehead);
	}

	internal static object AutoGeneratedGetMemberValueFigurehead(object o)
	{
		return ((Ship)o).Figurehead;
	}

	internal static object AutoGeneratedGetMemberValueIsInvulnerable(object o)
	{
		return ((Ship)o).IsInvulnerable;
	}

	internal static object AutoGeneratedGetMemberValueIsTradeable(object o)
	{
		return ((Ship)o).IsTradeable;
	}

	internal static object AutoGeneratedGetMemberValueIsUsedByQuest(object o)
	{
		return ((Ship)o).IsUsedByQuest;
	}

	internal static object AutoGeneratedGetMemberValueRandomValue(object o)
	{
		return ((Ship)o).RandomValue;
	}

	internal static object AutoGeneratedGetMemberValueShipHull(object o)
	{
		return ((Ship)o).ShipHull;
	}

	internal static object AutoGeneratedGetMemberValue_shipPieces(object o)
	{
		return ((Ship)o)._shipPieces;
	}

	internal static object AutoGeneratedGetMemberValue_hitPoints(object o)
	{
		return ((Ship)o)._hitPoints;
	}

	internal static object AutoGeneratedGetMemberValue_sailHitPoints(object o)
	{
		return ((Ship)o)._sailHitPoints;
	}

	internal static object AutoGeneratedGetMemberValue_owner(object o)
	{
		return ((Ship)o)._owner;
	}

	internal static object AutoGeneratedGetMemberValue_name(object o)
	{
		return ((Ship)o)._name;
	}

	internal static object AutoGeneratedGetMemberValue_unlockedUpgradePieces(object o)
	{
		return ((Ship)o)._unlockedUpgradePieces;
	}

	public void ChangeFigurehead(Figurehead figurehead)
	{
		if (CanEquipFigurehead)
		{
			Figurehead = figurehead;
			UpdateVersionNo();
		}
	}

	public ShipUpgradePiece GetPieceAtSlot(string slotTag)
	{
		return _shipPieces[slotTag];
	}

	public void EquipUpgradePiece(string slotTag, ShipUpgradePiece newUpgradePiece)
	{
		GetPieceAtSlot(slotTag);
		if (Owner == PartyBase.MainParty && newUpgradePiece != null)
		{
			AddToUnlockedPieces(newUpgradePiece);
		}
		SetPieceAtSlot(slotTag, newUpgradePiece);
	}

	private void SetPieceAtSlot(string slotTag, ShipUpgradePiece upgradePiece)
	{
		float num = HitPoints / MaxHitPoints;
		_ = ShipHull.AvailableSlots[slotTag];
		_shipPieces[slotTag] = upgradePiece;
		HitPoints = Math.Max(1f, num * MaxHitPoints);
		Owner?.MobileParty?.SetNavalVisualAsDirty();
		UpdateVersionNo();
	}

	private void AddToUnlockedPieces(ShipUpgradePiece upgradePiece)
	{
		if (!_unlockedUpgradePieces.Contains(upgradePiece))
		{
			_unlockedUpgradePieces.Add(upgradePiece);
		}
	}

	public bool HasSlot(string slotTag)
	{
		return _shipPieces.ContainsKey(slotTag);
	}

	public void SetName(TextObject name)
	{
		_name = name;
	}

	public Ship(ShipHull shipHull)
	{
		ShipHull = shipHull;
		InitializeFromTemplate(shipHull);
		HitPoints = MaxHitPoints;
		SailHitPoints = MaxSailHitPoints;
	}

	public float GetCampaignSpeed()
	{
		return ShipHull.BaseSpeed * (1f + CampaignSpeedBonusFactor);
	}

	public MBList<SiegeEngineType> GetSiegeEngines()
	{
		MBList<SiegeEngineType> mBList = new MBList<SiegeEngineType>();
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ShipHull.AvailableSlots)
		{
			SiegeEngineType siegeEngineType = GetPieceAtSlot(availableSlot.Key)?.SiegeEngine;
			if (siegeEngineType != null)
			{
				mBList.Add(siegeEngineType);
			}
		}
		return mBList;
	}

	private void ResetUnlockedUpgradePieces()
	{
		if (Owner == PartyBase.MainParty)
		{
			_unlockedUpgradePieces = new MBList<ShipUpgradePiece>(_shipPieces.Count);
			{
				foreach (KeyValuePair<string, ShipUpgradePiece> shipPiece in _shipPieces)
				{
					if (shipPiece.Value != null)
					{
						_unlockedUpgradePieces.Add(shipPiece.Value);
					}
				}
				return;
			}
		}
		_unlockedUpgradePieces = null;
	}

	public void UpdateVersionNo()
	{
		_isVersionDirty = true;
	}

	public float GetCombatFactor()
	{
		float defaultCombatFactor = Campaign.Current.Models.CampaignShipParametersModel.GetDefaultCombatFactor(ShipHull);
		float num = 1f;
		foreach (ShipUpgradePiece value in _shipPieces.Values)
		{
			if (value != null)
			{
				num += value.CombatFactor;
			}
		}
		return num * defaultCombatFactor;
	}

	private void InitializeFromTemplate(ShipHull shipHull)
	{
		foreach (KeyValuePair<string, ShipSlot> availableSlot in shipHull.AvailableSlots)
		{
			_shipPieces.Add(availableSlot.Key, null);
		}
	}

	public void OnShipDamaged(float rawDamage, IShipOrigin rammingShip, out float modifiedDamage)
	{
		if (HitPoints <= 0f || IsInvulnerable || rawDamage.ApproximatelyEqualsTo(0f))
		{
			modifiedDamage = 0f;
			return;
		}
		float shipDamage = Campaign.Current.Models.CampaignShipDamageModel.GetShipDamage(this, rammingShip as Ship, rawDamage);
		HitPoints -= shipDamage;
		SkillLevelingManager.OnShipDamaged(this, rawDamage, shipDamage);
		modifiedDamage = shipDamage - rawDamage;
		if (HitPoints <= 0f)
		{
			DestroyShipAction.Apply(this);
		}
	}

	void IShipOrigin.OnSailDamaged(float rawDamage, float inflictedDamage)
	{
		if (!(SailHitPoints <= 0f) && !IsInvulnerable && !rawDamage.ApproximatelyEqualsTo(0f))
		{
			SailHitPoints -= inflictedDamage;
			SkillLevelingManager.OnShipDamaged(this, rawDamage, inflictedDamage);
		}
	}

	public List<ShipVisualSlotInfo> GetShipVisualSlotInfos()
	{
		List<ShipVisualSlotInfo> list = new List<ShipVisualSlotInfo>();
		foreach (KeyValuePair<string, ShipUpgradePiece> shipPiece in _shipPieces)
		{
			if (shipPiece.Value != null)
			{
				list.Add(new ShipVisualSlotInfo(shipPiece.Key, shipPiece.Value.SlotPrefabChildTagId));
			}
		}
		if (Figurehead != null)
		{
			list.Add(new ShipVisualSlotInfo("figurehead", Figurehead.StringId));
		}
		return list;
	}

	public List<ShipSlotAndPieceName> GetShipSlotAndPieceNames()
	{
		List<ShipSlotAndPieceName> list = new List<ShipSlotAndPieceName>();
		foreach (KeyValuePair<string, ShipUpgradePiece> shipPiece in _shipPieces)
		{
			if (shipPiece.Value != null)
			{
				list.Add(new ShipSlotAndPieceName(ShipHull.AvailableSlots[shipPiece.Key].GetSlotTypeName().ToString(), shipPiece.Value.GetName().ToString()));
			}
		}
		if (Figurehead != null)
		{
			list.Add(new ShipSlotAndPieceName(new TextObject("{=YLbBHN0Z}Figurehead").ToString(), Figurehead.GetName().ToString()));
		}
		return list;
	}

	public void OnPlayerCharacterChanged()
	{
		ResetUnlockedUpgradePieces();
	}
}
