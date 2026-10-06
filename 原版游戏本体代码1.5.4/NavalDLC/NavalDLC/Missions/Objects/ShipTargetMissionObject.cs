using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects;

public class ShipTargetMissionObject : MissionObject, ITargetable
{
	private readonly Vec3 BoundingBoxOffset = Vec3.One;

	private MissionShip _ship;

	private NavalAgentsLogic _navalAgentsLogic;

	protected override void OnInit()
	{
		_ship = base.GameEntity.Root.GetFirstScriptOfType<MissionShip>();
		_navalAgentsLogic = Mission.Current.GetMissionBehavior<NavalAgentsLogic>();
	}

	public TargetFlags GetTargetFlags()
	{
		TargetFlags targetFlags = TargetFlags.IsMoving | TargetFlags.IsShip;
		if (_ship.IsSinking)
		{
			targetFlags |= TargetFlags.NotAThreat;
		}
		return targetFlags;
	}

	public float GetTargetValue(List<Vec3> weaponPositions)
	{
		return 500f * GetMultiplierOfShip();
	}

	public WeakGameEntity GetTargetEntity()
	{
		return base.GameEntity;
	}

	public Vec3 GetTargetingOffset()
	{
		return Vec3.Zero;
	}

	public BattleSideEnum GetSide()
	{
		return _ship.BattleSide;
	}

	public WeakGameEntity Entity()
	{
		return base.GameEntity;
	}

	public (Vec3, Vec3) ComputeGlobalPhysicsBoundingBoxMinMax()
	{
		Vec3 globalPosition = base.GameEntity.GlobalPosition;
		return (globalPosition - BoundingBoxOffset, globalPosition + BoundingBoxOffset);
	}

	public Vec3 GetTargetGlobalVelocity()
	{
		return _ship.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(base.GameEntity.GlobalPosition);
	}

	public bool IsDestructable()
	{
		return true;
	}

	private float GetMultiplierOfShip()
	{
		float num = (float)_navalAgentsLogic.GetActiveAgentCountOfShip(_ship) / ((float)_ship.CrewSizeOnMainDeck * 1f);
		num *= num;
		if (num < 0.0025000002f)
		{
			num = 0f;
		}
		float num2 = MathF.Max(1f, 2f - MathF.Log10(_ship.HitPoints / _ship.MaxHealth * 10f + 1f));
		return num * num2;
	}
}
