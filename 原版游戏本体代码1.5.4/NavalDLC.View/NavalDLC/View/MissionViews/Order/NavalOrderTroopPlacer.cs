using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;

namespace NavalDLC.View.MissionViews.Order;

public class NavalOrderTroopPlacer : OrderTroopPlacer
{
	private NavalShipsLogic _navalShipsLogic;

	public NavalOrderTroopPlacer(OrderController orderController)
		: base(orderController)
	{
	}

	public override void AfterStart()
	{
		base.AfterStart();
		base.OrderFlag.IsVisible = false;
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
	}

	protected override bool CanUpdate()
	{
		bool num = base.OrderController == Mission.Current.PlayerEnemyTeam.MasterOrderController;
		bool flag = base.Mission.IsNavalRaidBattle && base.OrderController.Team.Side == BattleSideEnum.Defender;
		if (num || flag)
		{
			return base.CanUpdate();
		}
		if (base.CanUpdate())
		{
			NavalShipsLogic navalShipsLogic = _navalShipsLogic;
			if (navalShipsLogic == null)
			{
				return false;
			}
			return navalShipsLogic.GetNumTeamShips(TeamSideEnum.PlayerTeam) > 0;
		}
		return false;
	}

	protected override bool CanGiveMovementOrder()
	{
		if (base.Mission.IsNavalBattle && base.Mission.IsDeploymentFinished && !NavalDLCHelpers.IsNavalRaidMissionOpen() && !NavalDLCHelpers.IsShipOrdersAvailable())
		{
			return false;
		}
		return base.CanGiveMovementOrder();
	}

	protected override OrderFlag CreateOrderFlag()
	{
		return new NavalOrderFlag(base.Mission, base.MissionScreen);
	}

	protected override CursorState GetCursorState()
	{
		if (base.Mission.IsNavalBattle)
		{
			return GetGroundOrNormalCursor();
		}
		return base.GetCursorState();
	}

	protected override bool TryGetScreenMiddleToWorldPosition(out WorldPosition worldPosition, out float collisionDistance, out WeakGameEntity collidedEntity)
	{
		if (base.Mission.IsNavalBattle)
		{
			if (base.MissionScreen.GetProjectedMousePositionOnWater(out var waterPosition))
			{
				worldPosition = new WorldPosition(base.Mission.Scene, waterPosition);
				collisionDistance = (waterPosition - base.Mission.GetCameraFrame().origin).Length;
				collidedEntity = WeakGameEntity.Invalid;
				return true;
			}
			worldPosition = WorldPosition.Invalid;
			collisionDistance = 0f;
			collidedEntity = WeakGameEntity.Invalid;
			return false;
		}
		return base.TryGetScreenMiddleToWorldPosition(out worldPosition, out collisionDistance, out collidedEntity);
	}

	protected override Vec3 GetGroundedVec3(WorldPosition worldPosition)
	{
		if (base.Mission.IsNavalBattle)
		{
			Vec2 asVec = worldPosition.AsVec2;
			return new Vec3(asVec.X, asVec.Y, base.Mission.Scene.GetWaterLevelAtPosition(asVec, useWaterRenderer: true, checkWaterBodyEntities: true));
		}
		return base.GetGroundedVec3(worldPosition);
	}
}
