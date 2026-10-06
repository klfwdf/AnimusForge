using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions;

namespace TaleWorlds.MountAndBlade;

public class AgentProximityMap
{
	public struct ProximityMapSearchStruct
	{
		internal ProximityMapSearchStructInternal SearchStructInternal;

		internal bool LoopAllAgents;

		internal int LastAgentLoopIndex;

		public Agent LastFoundAgent { get; internal set; }

		internal void RefreshLastFoundAgent(Mission mission)
		{
			LastFoundAgent = SearchStructInternal.GetCurrentAgent(mission);
		}
	}

	[Serializable]
	[EngineStruct("Managed_proximity_map_search_struct", false, null)]
	internal struct ProximityMapSearchStructInternal
	{
		internal int CurrentElementIndex;

		internal Vec2i Loc;

		internal Vec2i GridMin;

		internal Vec2i GridMax;

		internal Vec2 SearchPos;

		internal float SearchDistSq;

		internal Agent GetCurrentAgent(Mission mission)
		{
			return mission.FindAgentWithIndex(CurrentElementIndex);
		}
	}

	public static bool CanSearchRadius(float searchRadius)
	{
		float num = Mission.Current.ProximityMapMaxSearchRadius();
		return searchRadius <= num;
	}

	public static ProximityMapSearchStruct BeginSearch(Mission mission, Vec2 searchPos, float searchRadius, bool extendRangeByBiggestAgentCollisionPadding = false)
	{
		if (extendRangeByBiggestAgentCollisionPadding)
		{
			searchRadius += mission.GetBiggestAgentCollisionPadding() + 1f;
		}
		ProximityMapSearchStruct result = default(ProximityMapSearchStruct);
		float num = mission.ProximityMapMaxSearchRadius();
		result.LoopAllAgents = searchRadius > num;
		if (result.LoopAllAgents)
		{
			result.SearchStructInternal.SearchPos = searchPos;
			result.SearchStructInternal.SearchDistSq = searchRadius * searchRadius;
			result.LastAgentLoopIndex = 0;
			result.LastFoundAgent = null;
			AgentReadOnlyList agents = mission.Agents;
			while (agents.Count > result.LastAgentLoopIndex)
			{
				Agent agent = agents[result.LastAgentLoopIndex];
				if (agent.Position.AsVec2.DistanceSquared(searchPos) <= result.SearchStructInternal.SearchDistSq)
				{
					result.LastFoundAgent = agent;
					break;
				}
				result.LastAgentLoopIndex++;
			}
		}
		else
		{
			result.SearchStructInternal = mission.ProximityMapBeginSearch(searchPos, searchRadius);
			result.RefreshLastFoundAgent(mission);
		}
		return result;
	}

	public static void FindNext(Mission mission, ref ProximityMapSearchStruct searchStruct)
	{
		if (searchStruct.LoopAllAgents)
		{
			searchStruct.LastAgentLoopIndex++;
			searchStruct.LastFoundAgent = null;
			AgentReadOnlyList agents = mission.Agents;
			while (agents.Count > searchStruct.LastAgentLoopIndex)
			{
				Agent agent = agents[searchStruct.LastAgentLoopIndex];
				if (agent.Position.AsVec2.DistanceSquared(searchStruct.SearchStructInternal.SearchPos) <= searchStruct.SearchStructInternal.SearchDistSq)
				{
					searchStruct.LastFoundAgent = agent;
					break;
				}
				searchStruct.LastAgentLoopIndex++;
			}
		}
		else
		{
			mission.ProximityMapFindNext(ref searchStruct.SearchStructInternal);
			searchStruct.RefreshLastFoundAgent(mission);
		}
	}
}
