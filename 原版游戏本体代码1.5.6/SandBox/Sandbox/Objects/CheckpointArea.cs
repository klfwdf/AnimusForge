using SandBox.Missions;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects;

public class CheckpointArea : VolumeBox
{
	public const string CheckpointSpawnPointTag = "sp_checkpoint";

	public int UniqueId;

	[EditorVisibleScriptComponentVariable(false)]
	private CheckpointMissionLogic _checkpointMissionLogic;

	[EditorVisibleScriptComponentVariable(false)]
	public GameEntity SpawnPoint { get; private set; }

	public override void AfterMissionStart()
	{
		_checkpointMissionLogic = Mission.Current.GetMissionBehavior<CheckpointMissionLogic>();
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			if (child.HasTag("sp_checkpoint"))
			{
				SpawnPoint = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child);
				break;
			}
		}
	}

	protected override void OnTick(float dt)
	{
		if (_checkpointMissionLogic != null)
		{
			Agent main = Agent.Main;
			if (main != null && main.IsActive() && IsPointIn(Agent.Main.Position))
			{
				_checkpointMissionLogic.OnCheckpointUsed(UniqueId);
			}
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}
}
