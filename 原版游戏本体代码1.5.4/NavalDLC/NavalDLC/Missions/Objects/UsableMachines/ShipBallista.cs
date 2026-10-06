using NavalDLC.Missions.AI.UsableMachineAIs;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipBallista : Ballista
{
	private MissionShip _ship;

	[EditableScriptComponentVariable(true, "")]
	private float _horizontalAimSensitivity = 0.5f;

	[EditableScriptComponentVariable(true, "")]
	private float _verticalAimSensitivity = 0.5f;

	private NavalShipsLogic _navalShipsLogic;

	protected override float HorizontalAimSensitivity => _horizontalAimSensitivity;

	protected override float VerticalAimSensitivity => _verticalAimSensitivity;

	protected override bool WeaponMovesDownToReload
	{
		get
		{
			if (!(base.Ai as ShipBallistaAI).IsUnderDirectControl)
			{
				return base.PilotAgent.IsAIControlled;
			}
			return false;
		}
	}

	public override string MultipleProjectileId => "ballista_c_projectile_grape";

	public override string MultipleProjectileFlyingId => "ballista_c_projectile_grape_projectile";

	public override string MultipleFireProjectileId => "ballista_c_projectile_grape_fire";

	public override string MultipleFireProjectileFlyingId => "ballista_c_projectile_grape_fire_projectile";

	protected override void OnInit()
	{
		_ship = base.GameEntity.Root.GetFirstScriptOfType<MissionShip>();
		base.OnInit();
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.ShipSpawnedEvent += OnShipSpawned;
	}

	private void OnShipSpawned(MissionShip ship)
	{
		if (ship == _ship)
		{
			DefaultSide = ship.BattleSide;
		}
		_navalShipsLogic.ShipSpawnedEvent -= OnShipSpawned;
	}

	public override float GetTargetReleaseAngle(Vec3 target)
	{
		Vec3 globalVelocity = GetGlobalVelocity();
		float missileStartingSpeed = (ShootingSpeed * ShootingDirection + globalVelocity).Normalize();
		return Mission.GetMissileVerticalAimCorrection(target - base.MissileStartingGlobalPositionForSimulation, missileStartingSpeed, ref OriginalMissileWeaponStatsDataForTargeting, ItemObject.GetAirFrictionConstant(OriginalMissileItem.PrimaryWeapon.WeaponClass, OriginalMissileItem.PrimaryWeapon.WeaponFlags)) + base.GameEntity.GetGlobalFrame().rotation.GetEulerAngles().x.ToRadians();
	}

	public override Vec3 GetEstimatedTargetMovementVector(Vec3 targetPosition, Vec3 targetVelocity)
	{
		Vec3 vec = ShootingSpeed * ShootingDirection + GetGlobalVelocity();
		float num = vec.Normalize();
		float num2 = 0f;
		float missileTravelTimeApproximation = GetMissileTravelTimeApproximation(base.MissileStartingGlobalPositionForSimulation, targetPosition, vec * num, ItemObject.GetAirFrictionConstant(OriginalMissileItem.PrimaryWeapon.WeaponClass, OriginalMissileItem.PrimaryWeapon.WeaponFlags));
		Vec3 vec2 = targetPosition + targetVelocity * missileTravelTimeApproximation;
		int num3 = 0;
		while (MathF.Abs(missileTravelTimeApproximation - num2) > 1E-05f && num3++ < 10)
		{
			num2 = missileTravelTimeApproximation;
			missileTravelTimeApproximation = GetMissileTravelTimeApproximation(base.MissileStartingGlobalPositionForSimulation, vec2, vec * num, ItemObject.GetAirFrictionConstant(OriginalMissileItem.PrimaryWeapon.WeaponClass, OriginalMissileItem.PrimaryWeapon.WeaponFlags));
			vec2 = targetPosition + targetVelocity * missileTravelTimeApproximation;
		}
		return vec2 - targetPosition;
	}

	private float GetMissileTravelTimeApproximation(Vec3 startingPos, Vec3 targetPos, Vec3 velocity, float airFriction)
	{
		Vec3 vec = startingPos;
		float num = 0f;
		do
		{
			vec += velocity * 0.02f;
			velocity += MBGlobals.GravitationalAcceleration * 0.02f;
			float num2 = velocity.Normalize();
			num2 -= airFriction * num2 * num2 * 0.02f;
			velocity *= num2;
			num += 0.02f;
		}
		while (!(vec.DistanceSquared(targetPos) < 0.1f) && (!(vec.DistanceSquared(startingPos) > 100f) || !(vec.z < targetPos.z)));
		return num;
	}

	protected override Mission.Missile ShootProjectileAux(ItemObject missileItem, bool randomizeMissileSpeed)
	{
		SetupProjectileToShoot(randomizeMissileSpeed, out var direction, out var orientation, out var missileBaseSpeed, out var missileShootingSpeed);
		if (base.PlayerForceUse)
		{
			LastShooterAgent = Agent.Main;
		}
		MissionObject missionObjectToIgnore = base.GameEntity.Root.GetFirstScriptOfType<MissionObject>() ?? this;
		Mission.Missile missile = Mission.Current.AddCustomMissile(LastShooterAgent, new MissionWeapon(missileItem, null, LastShooterAgent.Origin?.Banner, 1), ProjectileEntityCurrentGlobalPosition, direction, orientation, missileShootingSpeed, missileBaseSpeed, addRigidBody: false, missionObjectToIgnore);
		_navalShipsLogic.AddShipSiegeEngineMissile(missile);
		return missile;
	}

	public override Vec3 GetGlobalVelocity()
	{
		return _ship.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(base.MissileStartingGlobalPositionForSimulation);
	}

	protected override bool CheckFriendlyFireForObjects(Vec3 globalTargetPosition)
	{
		if (base.CheckFriendlyFireForObjects(globalTargetPosition))
		{
			return true;
		}
		foreach (MissionShip allShip in _ship.ShipsLogic.AllShips)
		{
			if (allShip != _ship && allShip.Team != null && _ship.Team != null && allShip.Team.TeamSide == _ship.Team.TeamSide)
			{
				MatrixFrame globalFrame = allShip.GameEntity.GetGlobalFrame();
				Vec3 max = allShip.Physics.PhysicsBoundingBoxWithChildren.max;
				Vec3 min = allShip.Physics.PhysicsBoundingBoxWithChildren.min;
				Vec3 v = allShip.Physics.PhysicsBoundingBoxWithChildren.center;
				Oriented2DArea oriented2DArea = new Oriented2DArea(globalFrame.TransformToParent(in v).AsVec2, globalFrame.rotation.f.AsVec2.Normalized(), (max - min).AsVec2);
				LineSegment2D line = new LineSegment2D(globalTargetPosition.AsVec2, base.MissileStartingGlobalPositionForSimulation.AsVec2);
				if (oriented2DArea.Intersects(in line, 1f))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override float ProcessTargetValue(float baseValue, TargetFlags flags)
	{
		if (flags.HasAnyFlag(TargetFlags.NotAThreat))
		{
			return -1000f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsShip))
		{
			baseValue *= 2f;
		}
		if (flags.HasAnyFlag(TargetFlags.DebugThreat))
		{
			baseValue *= 10000f;
		}
		return baseValue;
	}

	protected override void DetermineDefaultBattleSide()
	{
		DefaultSide = _ship.BattleSide;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new ShipBallistaAI(this);
	}

	protected override void GetSoundEventIndices()
	{
		MoveSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/ballista_naval/move");
		ReloadSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/ballista_naval/reload");
		FireSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/ballista_naval/fire");
	}
}
