using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.Objects;

public class AnimalSpawnSettings : ScriptComponentBehavior
{
	public bool DisableWandering = true;

	public static void CheckAndSetAnimalAgentFlags(GameEntity spawnEntity, Agent animalAgent)
	{
		if (spawnEntity.HasScriptOfType<AnimalSpawnSettings>() && spawnEntity.GetFirstScriptOfType<AnimalSpawnSettings>().DisableWandering)
		{
			animalAgent.SetAgentFlags((AgentFlag)((uint)animalAgent.GetAgentFlags() & 0xFFFDFFFFu));
		}
	}
}
