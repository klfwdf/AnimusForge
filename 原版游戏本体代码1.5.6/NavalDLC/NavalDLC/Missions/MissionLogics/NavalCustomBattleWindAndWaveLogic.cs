using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class NavalCustomBattleWindAndWaveLogic : MissionLogic
{
	private NavalCustomBattleWindConfig.Direction _windDirection;

	private TerrainType _terrainType;

	private DeploymentMissionController _deploymentMissionController;

	public NavalCustomBattleWindAndWaveLogic(NavalCustomBattleWindConfig.Direction windDirection, TerrainType terrainType)
	{
		_windDirection = windDirection;
		_terrainType = terrainType;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_deploymentMissionController = base.Mission.GetMissionBehavior<DeploymentMissionController>();
		_deploymentMissionController.OnAfterSetupTeams += OnAfterSetupTeams;
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		_deploymentMissionController.OnAfterSetupTeams -= OnAfterSetupTeams;
	}

	public override void AfterStart()
	{
	}

	public void OnAfterSetupTeams()
	{
		UpdateSceneWindDirection();
		UpdateSceneWaterStrength();
	}

	private void UpdateSceneWindDirection()
	{
		Vec2 zero = Vec2.Zero;
		Vec2 zero2 = Vec2.Zero;
		int num = 0;
		int num2 = 0;
		foreach (Team team in Mission.Current.Teams)
		{
			Vec2 halfExtents;
			if (team.Side == BattleSideEnum.Attacker)
			{
				zero += base.Mission.DeploymentPlan.GetFormationsCenterFrameAndExtents(team, out halfExtents).origin.AsVec2;
				num++;
			}
			else if (team.Side == BattleSideEnum.Defender)
			{
				zero2 += base.Mission.DeploymentPlan.GetFormationsCenterFrameAndExtents(team, out halfExtents).origin.AsVec2;
				num2++;
			}
		}
		zero /= (float)num;
		zero2 /= (float)num2;
		Vec2 vec = (zero2 - zero).Normalized();
		float length = Mission.Current.Scene.GetGlobalWindVelocity().Length;
		Vec2 windVector = length * vec;
		switch (_windDirection)
		{
		case NavalCustomBattleWindConfig.Direction.TowardsDefender:
			windVector.RotateCCW(-System.MathF.PI / 6f);
			break;
		case NavalCustomBattleWindConfig.Direction.TowardsAttacker:
			windVector *= -1f;
			windVector.RotateCCW(-System.MathF.PI / 6f);
			break;
		case NavalCustomBattleWindConfig.Direction.Side:
			windVector = Vec3.CrossProduct(Vec3.Up, vec.ToVec3()).AsVec2 * length;
			break;
		case NavalCustomBattleWindConfig.Direction.Random:
			windVector = length * new Vec2(MBRandom.RandomFloatNormal, MBRandom.RandomFloatNormal).Normalized();
			break;
		}
		Mission.Current.Scene.SetGlobalWindVelocity(in windVector);
	}

	private void UpdateSceneWaterStrength()
	{
		if (_terrainType == TerrainType.River)
		{
			Mission.Current.Scene.SetWaterStrength(0.5f);
		}
	}
}
