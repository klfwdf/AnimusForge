using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class MissionBoundaryWallView : MissionView
{
	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		foreach (ICollection<Vec2> value in base.Mission.Boundaries.Values)
		{
			CreateBoundaryEntity(value);
		}
	}

	private void CreateBoundaryEntity(ICollection<Vec2> boundaryPoints)
	{
		Mesh mesh = BoundaryWallView.CreateBoundaryMesh(base.Mission.Scene, boundaryPoints);
		if (mesh != null)
		{
			GameEntity gameEntity = GameEntity.CreateEmpty(base.Mission.Scene);
			gameEntity.AddMesh(mesh);
			gameEntity.SetGlobalFrame(MatrixFrame.Identity);
			gameEntity.Name = "boundary_wall";
			gameEntity.SetMobility(GameEntity.Mobility.Stationary);
			gameEntity.EntityFlags |= EntityFlags.DoNotRenderToEnvmap;
		}
	}
}
