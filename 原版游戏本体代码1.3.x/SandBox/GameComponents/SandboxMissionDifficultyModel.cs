using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace SandBox.GameComponents
{
	// Token: 0x020000C3 RID: 195
	public class SandboxMissionDifficultyModel : MissionDifficultyModel
	{
		// Token: 0x06000807 RID: 2055 RVA: 0x00039BE0 File Offset: 0x00037DE0
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
					IAgentOriginBase origin = victimAgent.Origin;
					PartyBase partyBase;
					if ((partyBase = ((origin != null) ? origin.BattleCombatant : null) as PartyBase) != null)
					{
						Mission mission = Mission.Current;
						object obj;
						if (mission == null)
						{
							obj = null;
						}
						else
						{
							Agent mainAgent = mission.MainAgent;
							if (mainAgent == null)
							{
								obj = null;
							}
							else
							{
								IAgentOriginBase origin2 = mainAgent.Origin;
								obj = ((origin2 != null) ? origin2.BattleCombatant : null);
							}
						}
						PartyBase partyBase2;
						if ((partyBase2 = obj as PartyBase) != null && partyBase == partyBase2)
						{
							if (attackerAgent != null)
							{
								Mission mission2 = Mission.Current;
								if (attackerAgent == ((mission2 != null) ? mission2.MainAgent : null))
								{
									return Mission.Current.DamageFromPlayerToFriendsMultiplier;
								}
							}
							result = Mission.Current.DamageToFriendsMultiplier;
						}
					}
				}
			}
			return result;
		}
	}
}
