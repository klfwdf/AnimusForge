using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public interface IDetachment
{
	MBReadOnlyList<Formation> UserFormations { get; }

	bool IsLoose { get; }

	bool IsAgentUsingOrInterested(Agent agent);

	float? GetWeightOfNextSlot(BattleSideEnum side);

	float GetDetachmentWeight(BattleSideEnum side);

	float ComputeAndCacheDetachmentWeight(BattleSideEnum side);

	float GetDetachmentWeightFromCache();

	void GetSlotIndexWeightTuples(List<(int, float)> slotIndexWeightTuples);

	bool IsSlotAtIndexAvailableForAgent(int slotIndex, Agent agent);

	bool IsAgentEligible(Agent agent);

	void AddAgentAtSlotIndex(Agent agent, int slotIndex);

	Agent GetMovingAgentAtSlotIndex(int slotIndex);

	void MarkSlotAtIndex(int slotIndex);

	bool IsDetachmentRecentlyEvaluated();

	void UnmarkDetachment();

	float? GetWeightOfAgentAtNextSlot(List<Agent> candidates, out Agent match);

	float? GetWeightOfAgentAtNextSlot(List<(Agent, float)> agentTemplateScores, out Agent match);

	float GetTemplateWeightOfAgent(Agent candidate);

	List<float> GetTemplateCostsOfAgent(Agent candidate, List<float> oldValue);

	float GetExactCostOfAgentAtSlot(Agent candidate, int slotIndex);

	float GetWeightOfOccupiedSlot(Agent detachedAgent);

	float? GetWeightOfAgentAtOccupiedSlot(Agent detachedAgent, List<Agent> candidates, out Agent match);

	bool IsStandingPointAvailableForAgent(Agent agent);

	void AddAgent(Agent agent, int slotIndex = -1, Agent.AIScriptedFrameFlags customFlags = Agent.AIScriptedFrameFlags.None);

	void RemoveAgent(Agent detachedAgent);

	int GetNumberOfUsableSlots();

	void FormationStartUsing(Formation formation);

	void FormationStopUsing(Formation formation);

	bool IsUsedByFormation(Formation formation);

	WorldFrame? GetAgentFrame(Agent detachedAgent);

	void ResetEvaluation();

	bool IsEvaluated();

	void SetAsEvaluated();

	void OnFormationLeave(Formation formation);
}
