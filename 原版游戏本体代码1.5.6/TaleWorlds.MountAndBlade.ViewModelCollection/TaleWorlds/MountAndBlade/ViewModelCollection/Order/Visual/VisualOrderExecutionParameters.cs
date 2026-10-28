using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

public readonly struct VisualOrderExecutionParameters(Agent agent = null, Formation formation = null, WorldPosition? worldPosition = null)
{
	public readonly bool HasWorldPosition = worldPosition.HasValue;

	public readonly WorldPosition WorldPosition = (worldPosition.HasValue ? worldPosition.Value : WorldPosition.Invalid);

	public readonly bool HasAgent = agent != null;

	public readonly Agent Agent = agent;

	public readonly bool HasFormation = formation != null;

	public readonly Formation Formation = formation;
}
