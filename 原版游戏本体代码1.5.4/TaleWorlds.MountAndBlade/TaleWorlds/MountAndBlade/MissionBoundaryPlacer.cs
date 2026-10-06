using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MissionBoundaryPlacer : MissionLogic
{
	public override void EarlyStart()
	{
		AddMissionBoundaries();
	}

	public void AddMissionBoundaries()
	{
		MBList<Vec2> softBoundaryPoints = MBSceneUtilities.GetSoftBoundaryPoints(base.Mission.Scene);
		if (softBoundaryPoints.Count == 0)
		{
			base.Mission.Scene.GetBoundingBox(out var min, out var max);
			float num = MathF.Min(2f, max.x - min.x);
			float num2 = MathF.Min(2f, max.y - min.y);
			List<Vec2> collection = new List<Vec2>
			{
				new Vec2(min.x + num, min.y + num2),
				new Vec2(max.x - num, min.y + num2),
				new Vec2(max.x - num, max.y - num2),
				new Vec2(min.x + num, max.y - num2)
			};
			softBoundaryPoints.AddRange(collection);
		}
		base.Mission.Boundaries.Add("walk_area", softBoundaryPoints);
	}
}
