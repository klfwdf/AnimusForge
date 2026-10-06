using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.CustomBattle;

public class CustomBattleShip : IShipOrigin
{
	private readonly ShipHull _shipHull;

	private readonly Dictionary<string, ShipUpgradePiece> _shipPieces = new Dictionary<string, ShipUpgradePiece>();

	private readonly bool _isPlayerShip;

	private float _remainingHitPoints;

	private float _remainingFireHitPoints;

	private float _remainingSailHitPoints;

	public ShipHull Hull => _shipHull;

	public TextObject Name => _shipHull.Name;

	public string OriginShipId => _shipHull.MissionShipObjectId;

	public bool IsPlayerShip => _isPlayerShip;

	public float HitPoints
	{
		get
		{
			return _remainingHitPoints;
		}
		set
		{
			float remainingHitPoints = TaleWorlds.Library.MathF.Clamp(value, 0f, MaxHitPoints);
			_remainingHitPoints = remainingHitPoints;
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
			return (float)_shipHull.MaxHitPoints * num;
		}
	}

	public float FireHitPoints
	{
		get
		{
			return _remainingFireHitPoints;
		}
		set
		{
			float remainingFireHitPoints = TaleWorlds.Library.MathF.Clamp(value, 0f, MaxFireHitPoints);
			_remainingFireHitPoints = remainingFireHitPoints;
		}
	}

	public float MaxFireHitPoints
	{
		get
		{
			float num = 1f;
			return (float)_shipHull.MaxFireHitPoints * num;
		}
	}

	public float SailHitPoints
	{
		get
		{
			return _remainingSailHitPoints;
		}
		set
		{
			float remainingSailHitPoints = TaleWorlds.Library.MathF.Clamp(value, 0f, MaxSailHitPoints);
			_remainingSailHitPoints = remainingSailHitPoints;
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
			return (float)_shipHull.MaxSailHitPoints * num;
		}
	}

	public int TotalCrewCapacity
	{
		get
		{
			int totalCrewCapacity = _shipHull.TotalCrewCapacity;
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

	public int MainDeckCrewCapacity
	{
		get
		{
			int mainDeckCrewCapacity = _shipHull.MainDeckCrewCapacity;
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

	public int SkeletalCrewCapacity => _shipHull.SkeletalCrewCapacity;

	public int DefaultFormationGroupIndex => _shipHull.DefaultFormationGroup;

	public float ForwardDragFactor
	{
		get
		{
			float num = 0f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.DecreaseForwardDragMultiplier;
				}
			}
			return 0f - num;
		}
	}

	public float ShipWeightFactor
	{
		get
		{
			float num = 0f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.ShipWeightBonusMultiplier;
				}
			}
			return num;
		}
	}

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

	public int RandomValue => MBRandom.RandomInt(1, int.MaxValue);

	public string CustomSailPatternId { get; set; }

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

	public float MaxOarForceFactor
	{
		get
		{
			float num = 0f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.MaxOarForceBonusMultiplier;
				}
			}
			return num;
		}
	}

	public float SailForceFactor
	{
		get
		{
			float num = 0f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.SailForceBonusMultiplier;
				}
			}
			return num;
		}
	}

	public float MaxOarPowerFactor
	{
		get
		{
			float num = 0f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.MaxOarPowerBonusMultiplier;
				}
			}
			return num;
		}
	}

	public float SailRotationSpeedFactor
	{
		get
		{
			float num = 0f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.SailRotationSpeedBonusMultiplier;
				}
			}
			return num;
		}
	}

	public float FurlUnfurlSpeedFactor
	{
		get
		{
			float num = 0f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.FurlUnfurlSpeedBonusMultiplier;
				}
			}
			return num;
		}
	}

	public float CrewShieldHitPointsFactor
	{
		get
		{
			float num = 0f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.CrewShieldHitPointsBonusMultiplier;
				}
			}
			return num;
		}
	}

	public float CrewMeleeDamageFactor
	{
		get
		{
			float num = 0f;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.CrewMeleeDamageBonusMultiplier;
				}
			}
			return num;
		}
	}

	public int AdditionalArcherQuivers
	{
		get
		{
			int num = 0;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.ArcherQuiverBonus;
				}
			}
			return num;
		}
	}

	public int AdditionalThrowingWeaponStack
	{
		get
		{
			int num = 0;
			foreach (ShipUpgradePiece value in _shipPieces.Values)
			{
				if (value != null)
				{
					num += value.ThrowingWeaponStackBonus;
				}
			}
			return num;
		}
	}

	public CustomBattleShip(ShipHull shipHull, bool isPlayerShip)
	{
		_shipHull = shipHull;
		_isPlayerShip = isPlayerShip;
		_remainingHitPoints = MaxHitPoints;
		_remainingSailHitPoints = MaxSailHitPoints;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in shipHull.AvailableSlots)
		{
			_shipPieces.Add(availableSlot.Key, null);
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
		return list;
	}

	public List<ShipSlotAndPieceName> GetShipSlotAndPieceNames()
	{
		List<ShipSlotAndPieceName> list = new List<ShipSlotAndPieceName>();
		foreach (KeyValuePair<string, ShipUpgradePiece> shipPiece in _shipPieces)
		{
			if (shipPiece.Value != null)
			{
				list.Add(new ShipSlotAndPieceName(Hull.AvailableSlots[shipPiece.Key].GetSlotTypeName().ToString(), shipPiece.Value.GetName().ToString()));
			}
		}
		return list;
	}

	public void OnSailDamaged(float rawDamage, float inflictedDamage)
	{
		_remainingSailHitPoints -= inflictedDamage;
	}

	public void OnShipDamaged(float rawDamage, IShipOrigin rammingShip, out float modifiedDamage)
	{
		_remainingHitPoints -= rawDamage;
		modifiedDamage = 0f;
	}

	public void SetPieceAtSlot(string slotTag, ShipUpgradePiece upgradePiece)
	{
		float num = HitPoints / MaxHitPoints;
		_ = _shipHull.AvailableSlots[slotTag];
		_shipPieces[slotTag] = upgradePiece;
		HitPoints = Math.Max(1f, num * MaxHitPoints);
	}

	public CustomBattleShip GetCopy()
	{
		CustomBattleShip customBattleShip = new CustomBattleShip(_shipHull, IsPlayerShip);
		foreach (KeyValuePair<string, ShipUpgradePiece> shipPiece in _shipPieces)
		{
			customBattleShip.SetPieceAtSlot(shipPiece.Key, shipPiece.Value);
		}
		return customBattleShip;
	}
}
