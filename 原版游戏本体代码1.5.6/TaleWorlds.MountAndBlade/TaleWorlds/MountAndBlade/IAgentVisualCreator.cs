namespace TaleWorlds.MountAndBlade;

public interface IAgentVisualCreator
{
	IAgentVisual Create(AgentVisualsData data, string name, bool needBatchedVersionForWeaponMeshes, bool forceUseFaceCache);
}
