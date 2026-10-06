using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.Screens;

namespace NavalDLC.View.MissionViews.Order;

public class NavalOrderFlag : OrderFlag
{
	public NavalOrderFlag(Mission mission, MissionScreen missionScreen, float flagScale = 20f)
		: base(mission, missionScreen, flagScale)
	{
	}

	protected override Vec3 GetFlagPosition(out bool isOnValidGround, bool checkForTargetEntity, Vec3 targetCollisionPoint)
	{
		if (!_mission.IsNavalBattle)
		{
			return base.GetFlagPosition(out isOnValidGround, checkForTargetEntity, targetCollisionPoint);
		}
		if (_missionScreen.GetProjectedMousePositionOnWater(out var waterPosition))
		{
			waterPosition = new Vec3(waterPosition.x, waterPosition.y, _mission.Scene.GetWaterLevelAtPosition(waterPosition.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: true));
			WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, waterPosition, hasValidZ: false);
			isOnValidGround = IsPositionOnValidGround(worldPosition);
			return waterPosition;
		}
		isOnValidGround = false;
		return new Vec3(0f, 0f, -10000f);
	}

	public override bool IsPositionOnValidGround(WorldPosition worldPosition)
	{
		if (!_mission.IsNavalBattle)
		{
			return base.IsPositionOnValidGround(worldPosition);
		}
		if (Mission.Current.Mode == MissionMode.Deployment && Mission.Current.DeploymentPlan.HasDeploymentBoundaries(Mission.Current.PlayerTeam) && !Mission.Current.DeploymentPlan.IsPositionInsideDeploymentBoundaries(Mission.Current.PlayerTeam, worldPosition.AsVec2))
		{
			return false;
		}
		return true;
	}
}
