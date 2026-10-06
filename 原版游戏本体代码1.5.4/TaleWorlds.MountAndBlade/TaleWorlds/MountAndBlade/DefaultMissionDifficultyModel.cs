using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class DefaultMissionDifficultyModel : MissionDifficultyModel
{
	public override float GetDamageMultiplierOfCombatDifficulty(Agent victimAgent, Agent attackerAgent = null)
	{
		float result = 1f;
		victimAgent = (victimAgent.IsMount ? victimAgent.RiderAgent : victimAgent);
		if (victimAgent != null)
		{
			if (victimAgent.IsMainAgent)
			{
				result = Mission.Current.DamageToPlayerMultiplier;
			}
			else
			{
				Agent agent = Mission.Current?.MainAgent;
				if (agent != null && victimAgent.IsFriendOf(agent))
				{
					result = ((attackerAgent == null || attackerAgent != agent) ? Mission.Current.DamageToFriendsMultiplier : Mission.Current.DamageFromPlayerToFriendsMultiplier);
				}
			}
		}
		return result;
	}
}
