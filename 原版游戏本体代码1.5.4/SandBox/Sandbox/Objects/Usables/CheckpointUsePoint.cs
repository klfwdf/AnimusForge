using SandBox.Missions;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables;

public class CheckpointUsePoint : UsableMachine
{
	public const string CheckpointSpawnPointTag = "sp_checkpoint";

	public int UniqueId;

	[EditorVisibleScriptComponentVariable(false)]
	private CheckpointMissionLogic _checkpointMissionLogic;

	[EditorVisibleScriptComponentVariable(false)]
	public GameEntity SpawnPoint { get; private set; }

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(TickRequirement.Tick);
	}

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
		base.OnTick(dt);
		if (_checkpointMissionLogic == null)
		{
			return;
		}
		Agent main = Agent.Main;
		if (main == null || !main.IsActive())
		{
			return;
		}
		for (int i = 0; i < base.StandingPoints.Count; i++)
		{
			if (base.StandingPoints[i].HasUser)
			{
				_checkpointMissionLogic.OnCheckpointUsed(UniqueId);
			}
		}
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		return new TextObject("{=G2IaEr2Z}Use");
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=eO7p1Q3C}Checkpoint");
	}
}
