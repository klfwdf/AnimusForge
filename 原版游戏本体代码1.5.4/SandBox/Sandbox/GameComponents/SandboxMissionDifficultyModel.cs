using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace SandBox.GameComponents;

public class SandboxMissionDifficultyModel : MissionDifficultyModel
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
			else if (victimAgent.Origin?.BattleCombatant is PartyBase partyBase && Mission.Current?.MainAgent?.Origin?.BattleCombatant is PartyBase partyBase2 && partyBase == partyBase2)
			{
				result = ((attackerAgent == null || attackerAgent != Mission.Current?.MainAgent) ? Mission.Current.DamageToFriendsMultiplier : Mission.Current.DamageFromPlayerToFriendsMultiplier);
			}
		}
		return result;
	}
}
