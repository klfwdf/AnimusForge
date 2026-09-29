using System.Collections.Generic;

namespace AnimusForge;

// Audience rules for the persistent scene session. Pure (no game types) so it is unit-tested directly.
// Participating: answers while within the configured shout range. Excluded: never an audience member,
// never auto-absorbed again. Locked: stays in the audience at any distance while the agent is alive.
public enum ScenePresentationParticipantState
{
	Participating = 0,
	Excluded = 1,
	Locked = 2
}

internal static class ScenePresentationPolicy
{
	// Click cycle on the seal: 参与 → 屏蔽 → 锁定 → 参与. The addressee can never be excluded.
	internal static ScenePresentationParticipantState NextState(ScenePresentationParticipantState current, bool isAddressee)
	{
		ScenePresentationParticipantState next = current == ScenePresentationParticipantState.Participating
			? ScenePresentationParticipantState.Excluded
			: current == ScenePresentationParticipantState.Excluded
				? ScenePresentationParticipantState.Locked
				: ScenePresentationParticipantState.Participating;
		return isAddressee && next == ScenePresentationParticipantState.Excluded ? ScenePresentationParticipantState.Locked : next;
	}

	internal static bool IsInRange(ScenePresentationParticipantState state, float distanceSquared, float maxRangeSquared)
	{
		return state == ScenePresentationParticipantState.Locked || distanceSquared <= maxRangeSquared;
	}

	internal static bool IsAudience(ScenePresentationParticipantState state, bool alive, bool inRange)
	{
		return alive && inRange && state != ScenePresentationParticipantState.Excluded;
	}

	// Locked members are preferred as the next addressee, then participating ones, in join order.
	internal static int ChooseAddressee(IReadOnlyList<int> agentIndices, IReadOnlyList<ScenePresentationParticipantState> states, IReadOnlyList<bool> audience)
	{
		int fallback = -1;
		for (int i = 0; i < agentIndices.Count; i++)
		{
			if (!audience[i])
			{
				continue;
			}
			if (states[i] == ScenePresentationParticipantState.Locked)
			{
				return agentIndices[i];
			}
			if (fallback < 0)
			{
				fallback = agentIndices[i];
			}
		}
		return fallback;
	}
}
