using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Objects.AreaMarkers;

public class StealthAreaMarker : AreaMarker
{
	private const string ReinforcementAllyGroupSpawnPointTag = "reinforcement_ally_group_spawn_point_tag";

	private const string WaitPointTag = "wait_point_tag";

	public string ReinforcementAllyGroupId;

	public GameEntity ReinforcementAllyGroupSpawnPoint { get; private set; }

	public GameEntity WaitPoint { get; private set; }

	public override void AfterMissionStart()
	{
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			if (child.HasTag("reinforcement_ally_group_spawn_point_tag"))
			{
				ReinforcementAllyGroupSpawnPoint = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
			}
			if (child.HasTag("wait_point_tag"))
			{
				WaitPoint = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
			}
		}
	}
}
