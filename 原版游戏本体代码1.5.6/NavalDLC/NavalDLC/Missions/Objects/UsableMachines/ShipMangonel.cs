using System;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.DotNet;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipMangonel : Mangonel
{
	private MissionShip _ship;

	private NavalShipsLogic _navalShipsLogic;

	[EditableScriptComponentVariable(true, "")]
	private float _directionRestriction = MathF.PI * 2f / 3f;

	public override string MultipleProjectileId => "mangonel_c_grapeshot_stack";

	public override float DirectionRestriction => _directionRestriction;

	public override string MultipleProjectileFlyingId => "mangonel_c_grapeshot_projectile";

	public override string MultipleFireProjectileId => "mangonel_c_grapeshot_fire_stack";

	public override string MultipleFireProjectileFlyingId => "mangonel_c_grapeshot_fire_projectile";

	protected override float ReloadSpeedMultiplier => 6.2f;

	protected override float HorizontalAimSensitivity => 0.5f;

	protected override void OnInit()
	{
		_ship = base.GameEntity.Root.GetFirstScriptOfType<MissionShip>();
		base.OnInit();
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalShipsLogic.ShipSpawnedEvent += OnShipSpawned;
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.IsDisabledForPlayers = true;
		}
	}

	private void OnShipSpawned(MissionShip ship)
	{
		if (ship == _ship)
		{
			DefaultSide = ship.BattleSide;
		}
		_navalShipsLogic.ShipSpawnedEvent -= OnShipSpawned;
	}
}
