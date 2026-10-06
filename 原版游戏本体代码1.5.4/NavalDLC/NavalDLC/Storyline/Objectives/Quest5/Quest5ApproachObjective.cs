using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest5;

public class Quest5ApproachObjective : MissionObjective
{
	private class ApproachObjectiveTarget : MissionObjectiveTarget
	{
		public readonly MatrixFrame ApproachTargetFrame;

		public ApproachObjectiveTarget(MatrixFrame approachTargetFrame)
		{
			ApproachTargetFrame = approachTargetFrame;
		}

		public override TextObject GetName()
		{
			return new TextObject("{=9pyEoT2i}Hailing point");
		}

		public override Vec3 GetGlobalPosition()
		{
			return ApproachTargetFrame.origin;
		}

		public override bool IsActive()
		{
			return true;
		}
	}

	private readonly MissionShip _playerShip;

	private readonly float _completionDistance;

	private ApproachObjectiveTarget _target;

	public override string UniqueId => "quest_5_approach_objective";

	public override TextObject Name => new TextObject("{=s8t5kclT}Approach the meeting zone");

	public override TextObject Description => new TextObject("{=EmIS3tfC}Sail to within hailing distance of the Sea Hound ship.");

	public Quest5ApproachObjective(Mission mission, MissionShip playerShip, MatrixFrame approachTargetFrame, float completionDistance)
		: base(mission)
	{
		_playerShip = playerShip;
		_completionDistance = completionDistance;
		_target = new ApproachObjectiveTarget(approachTargetFrame);
		AddTarget(_target);
	}

	protected override bool IsActivationRequirementsMet()
	{
		return _target != null;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		if (_target != null)
		{
			return _target.ApproachTargetFrame.origin.Distance(_playerShip.GameEntity.GetGlobalFrame().origin) <= _completionDistance;
		}
		return false;
	}
}
