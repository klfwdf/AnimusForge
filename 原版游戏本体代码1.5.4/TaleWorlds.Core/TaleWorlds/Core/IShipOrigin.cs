using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public interface IShipOrigin
{
	ShipHull Hull { get; }

	TextObject Name { get; }

	string OriginShipId { get; }

	bool IsPlayerShip { get; }

	float HitPoints { get; }

	float MaxHitPoints { get; }

	float MaxFireHitPoints { get; }

	float SailHitPoints { get; }

	float MaxSailHitPoints { get; }

	int TotalCrewCapacity { get; }

	int MainDeckCrewCapacity { get; }

	int SkeletalCrewCapacity { get; }

	int DefaultFormationGroupIndex { get; }

	float ForwardDragFactor { get; }

	float ShipWeightFactor { get; }

	float RudderSurfaceAreaFactor { get; }

	int RandomValue { get; }

	string CustomSailPatternId { get; }

	float MaxRudderForceFactor { get; }

	float MaxOarForceFactor { get; }

	float SailForceFactor { get; }

	float MaxOarPowerFactor { get; }

	float SailRotationSpeedFactor { get; }

	float FurlUnfurlSpeedFactor { get; }

	float CrewShieldHitPointsFactor { get; }

	float CrewMeleeDamageFactor { get; }

	int AdditionalArcherQuivers { get; }

	int AdditionalThrowingWeaponStack { get; }

	void OnShipDamaged(float rawDamage, IShipOrigin rammingShip, out float modifiedDamage);

	void OnSailDamaged(float rawDamage, float inflictedDamage);

	List<ShipVisualSlotInfo> GetShipVisualSlotInfos();

	List<ShipSlotAndPieceName> GetShipSlotAndPieceNames();
}
