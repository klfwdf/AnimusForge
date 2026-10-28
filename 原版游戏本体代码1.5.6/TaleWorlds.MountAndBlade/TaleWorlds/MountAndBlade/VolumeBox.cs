using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class VolumeBox : MissionObject
{
	public delegate void VolumeBoxDelegate(VolumeBox volumeBox, List<Agent> agentsInVolume);

	private VolumeBoxDelegate _volumeBoxIsOccupiedDelegate;

	protected internal override void OnInit()
	{
	}

	public void AddToCheckList(Agent agent)
	{
	}

	public void RemoveFromCheckList(Agent agent)
	{
	}

	public void SetIsOccupiedDelegate(VolumeBoxDelegate volumeBoxDelegate)
	{
		_volumeBoxIsOccupiedDelegate = volumeBoxDelegate;
	}

	public bool HasAgentsInAttackerSide()
	{
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, globalFrame.origin.AsVec2, globalFrame.rotation.GetScaleVector().AsVec2.Length);
		while (searchStruct.LastFoundAgent != null)
		{
			Agent lastFoundAgent = searchStruct.LastFoundAgent;
			if (lastFoundAgent.Team != null && lastFoundAgent.Team.Side == BattleSideEnum.Attacker && IsPointIn(lastFoundAgent.Position))
			{
				return true;
			}
			AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
		}
		return false;
	}

	public bool IsPointIn(Vec3 point)
	{
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 scaleVector = globalFrame.rotation.GetScaleVector();
		globalFrame.rotation.ApplyScaleLocal(new Vec3(1f / scaleVector.x, 1f / scaleVector.y, 1f / scaleVector.z));
		point = globalFrame.TransformToLocal(in point);
		if (MathF.Abs(point.x) <= scaleVector.x / 2f && MathF.Abs(point.y) <= scaleVector.y / 2f)
		{
			return MathF.Abs(point.z) <= scaleVector.z / 2f;
		}
		return false;
	}
}
