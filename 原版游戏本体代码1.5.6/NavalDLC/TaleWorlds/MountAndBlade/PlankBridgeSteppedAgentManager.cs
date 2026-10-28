using System.Collections.Generic;
using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class PlankBridgeSteppedAgentManager : ScriptComponentBehavior
{
	private Dictionary<int, float> _accumulatedCostDict;

	public Vec3 WeightedPosition { get; private set; }

	public float TotalMass { get; private set; }

	public int AgentCount { get; private set; }

	public bool AttackerSideAgentExists { get; private set; }

	public bool DefenderSideAgentExists { get; private set; }

	public ShipAttachmentMachine.ShipBridgeNavmeshHolder NavmeshHolder { get; private set; }

	public void SetNavmeshHolder(ShipAttachmentMachine.ShipBridgeNavmeshHolder navmeshHolder)
	{
		NavmeshHolder = navmeshHolder;
	}

	protected override void OnInit()
	{
		base.OnInit();
		WeightedPosition = Vec3.Zero;
		TotalMass = 0f;
		AgentCount = 0;
		_accumulatedCostDict = new Dictionary<int, float>();
	}

	public void ClearAgentWeightAndPositionInformation()
	{
		WeightedPosition = Vec3.Zero;
		TotalMass = 0f;
		AgentCount = 0;
		NavmeshHolder?.GameEntity.SetCostAdderForAttachedFaces(0f);
		_accumulatedCostDict.Clear();
		AttackerSideAgentExists = false;
		DefenderSideAgentExists = false;
	}

	public void AddAgentWeightAndPositionInformation(Agent agent)
	{
		if (!AttackerSideAgentExists && agent.Team.Side == BattleSideEnum.Attacker)
		{
			AttackerSideAgentExists = true;
		}
		else if (!DefenderSideAgentExists && agent.Team.Side == BattleSideEnum.Defender)
		{
			DefenderSideAgentExists = true;
		}
		float totalMass = agent.GetTotalMass();
		int currentNavigationFaceId = agent.GetCurrentNavigationFaceId();
		if (NavmeshHolder != null && _accumulatedCostDict.ContainsKey(currentNavigationFaceId))
		{
			_accumulatedCostDict[currentNavigationFaceId] += 7.5f;
			Mission.Current.SetNavigationFaceCostWithIdAroundPosition(currentNavigationFaceId, agent.Position, _accumulatedCostDict[currentNavigationFaceId]);
		}
		Vec3 position = agent.Position;
		if (base.GameEntity.GetGlobalFrame().origin.DistanceSquared(position) < 25f)
		{
			WeightedPosition += totalMass * agent.Position;
			TotalMass += totalMass;
			AgentCount++;
		}
	}
}
