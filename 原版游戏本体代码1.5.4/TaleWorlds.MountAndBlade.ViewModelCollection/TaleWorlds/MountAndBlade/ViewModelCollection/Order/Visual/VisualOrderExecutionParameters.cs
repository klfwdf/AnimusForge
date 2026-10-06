using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

public readonly struct VisualOrderExecutionParameters
{
	public readonly bool HasWorldPosition;

	public readonly WorldPosition WorldPosition;

	public readonly bool HasAgent;

	public readonly Agent Agent;

	public readonly bool HasFormation;

	public readonly Formation Formation;

	public VisualOrderExecutionParameters(Agent agent = null, Formation formation = null, WorldPosition? worldPosition = null)
	{
		HasWorldPosition = worldPosition.HasValue;
		WorldPosition = (worldPosition.HasValue ? worldPosition.Value : WorldPosition.Invalid);
		HasAgent = agent != null;
		Agent = agent;
		HasFormation = formation != null;
		Formation = formation;
	}
}
