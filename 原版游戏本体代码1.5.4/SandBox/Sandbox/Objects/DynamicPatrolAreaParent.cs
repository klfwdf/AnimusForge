using TaleWorlds.MountAndBlade;

namespace SandBox.Objects;

public class DynamicPatrolAreaParent : MissionObject
{
	public bool DrawPath = true;

	public int UniqueId = -1;

	protected override void OnEditorTick(float dt)
	{
	}
}
