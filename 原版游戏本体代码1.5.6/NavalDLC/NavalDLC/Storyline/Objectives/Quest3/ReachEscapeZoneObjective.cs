using System.Collections.Generic;
using NavalDLC.Missions.Objects;
using NavalDLC.Storyline.MissionControllers;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest3;

internal class ReachEscapeZoneObjective : MissionObjective
{
	private BlockedEstuaryMissionController _controller;

	private List<CheckpointObjectiveTarget> _targets = new List<CheckpointObjectiveTarget>();

	public override string UniqueId => "naval_storyline_quest_3_reach_position_objective";

	public override TextObject Name => new TextObject("{=nGpnbplB}Escape Zone");

	public override TextObject Description => new TextObject("{=4YtHaWFC}Reach the open seas by avoiding enemy ships.");

	internal ReachEscapeZoneObjective(Mission mission, MissionShip ship, Vec3 position)
		: base(mission)
	{
		_controller = base.Mission.GetMissionBehavior<BlockedEstuaryMissionController>();
		AddTarget(new ShipObjectiveTarget(ship, ship.ShipOrigin.Name, showController: true));
		List<GameEntity> list = CollectCheckpoints();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (GameEntity item in list)
		{
			CheckpointObjectiveTarget checkpointObjectiveTarget = new CheckpointObjectiveTarget(item);
			AddTarget(checkpointObjectiveTarget);
			_targets.Add(checkpointObjectiveTarget);
		}
		_targets[0].SetActive(isActive: true);
		_targets[_targets.Count - 1].SetName(new TextObject("{=nGpnbplB}Escape Zone"));
	}

	private List<GameEntity> CollectCheckpoints()
	{
		List<GameEntity> list = new List<GameEntity>();
		int num = 1;
		while (true)
		{
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("sp_escape_objective_" + num);
			if (gameEntity == null)
			{
				break;
			}
			list.Add(gameEntity);
			num++;
		}
		return list;
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (Agent.Main == null || !Agent.Main.IsActive())
		{
			return;
		}
		for (int i = 0; i < _targets.Count; i++)
		{
			CheckpointObjectiveTarget checkpointObjectiveTarget = _targets[i];
			if (checkpointObjectiveTarget.IsInside(Agent.Main.Position))
			{
				checkpointObjectiveTarget.SetActive(isActive: false);
				for (int num = i - 1; num >= 0; num--)
				{
					_targets[num].SetActive(isActive: false);
				}
				if (i < _targets.Count - 1)
				{
					_targets[i + 1].SetActive(isActive: true);
				}
			}
		}
	}

	protected override bool IsActivationRequirementsMet()
	{
		if (_controller != null)
		{
			return _controller.CurrentPhase == BlockedEstuaryMissionController.BattlePhase.Phase3;
		}
		return false;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return false;
	}
}
