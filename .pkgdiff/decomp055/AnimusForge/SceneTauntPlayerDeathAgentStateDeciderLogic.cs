using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed class SceneTauntPlayerDeathAgentStateDeciderLogic : MissionLogic, IAgentStateDecider, IMissionBehavior
{
	public AgentState GetAgentState(Agent effectedAgent, float deathProbability, out bool usedSurgery)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		usedSurgery = false;
		try
		{
			Mission current = Mission.Current;
			SceneTauntMissionBehavior sceneTauntMissionBehavior = ((current != null) ? current.GetMissionBehavior<SceneTauntMissionBehavior>() : null);
			if (sceneTauntMissionBehavior != null && sceneTauntMissionBehavior.TryUseSafeMainHeroDefeatState(effectedAgent, deathProbability, out var result))
			{
				return result;
			}
		}
		catch
		{
		}
		float num = deathProbability;
		if (num < 0f)
		{
			num = 0f;
		}
		if (num > 1f)
		{
			num = 1f;
		}
		return (AgentState)((MBRandom.RandomFloat <= num) ? 4 : 3);
	}
}
