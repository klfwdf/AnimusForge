using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Captivity;

public class CaptivityFreePrisonersObjective : MissionObjective
{
	private class CaptivityPrisonerTarget : MissionObjectiveTarget<AgentBindsMachine>
	{
		private readonly TextObject _name;

		public CaptivityPrisonerTarget(TextObject name, AgentBindsMachine agentBindMachine)
			: base(agentBindMachine)
		{
			_name = name;
		}

		public override Vec3 GetGlobalPosition()
		{
			if (Agent.Main == null)
			{
				return Vec3.Invalid;
			}
			return base.Target.GameEntity.GlobalPosition + base.Target.GameEntity.GetGlobalFrame().rotation.u * 1.5f;
		}

		public override TextObject GetName()
		{
			return _name;
		}

		public override bool IsActive()
		{
			return base.Target.HasCaptive;
		}
	}

	private readonly NavalStorylineCaptivityMissionController _captivityMissionController;

	private readonly TextObject _name;

	private readonly TextObject _description;

	private readonly TextObject _targetName;

	private MissionObjectiveProgressInfo _cachedProgress;

	public override string UniqueId => "CaptivityFreePrisonersObjective";

	public override TextObject Name => _name;

	public override TextObject Description => _description;

	public CaptivityFreePrisonersObjective(Mission mission, NavalStorylineCaptivityMissionController captivityMissionController)
		: base(mission)
	{
		_name = new TextObject("{=Kl4fHd5i}Escape Captivity");
		_description = new TextObject("{=57iHCBz9}Set all prisoners on the ship free.");
		_targetName = new TextObject("{=mx9zqEzQ}Unchain");
		_captivityMissionController = captivityMissionController;
		foreach (AgentBindsMachine markedAgentBind in _captivityMissionController.GetMarkedAgentBinds())
		{
			CaptivityPrisonerTarget target = new CaptivityPrisonerTarget(_targetName, markedAgentBind);
			AddTarget(target);
		}
	}

	protected override bool IsActivationRequirementsMet()
	{
		return true;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return _cachedProgress.CurrentProgressAmount == _cachedProgress.RequiredProgressAmount;
	}

	public override MissionObjectiveProgressInfo GetCurrentProgress()
	{
		return _cachedProgress;
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		MBReadOnlyList<CaptivityPrisonerTarget> targetsCopy = GetTargetsCopy<CaptivityPrisonerTarget>();
		_cachedProgress.CurrentProgressAmount = 0;
		_cachedProgress.RequiredProgressAmount = targetsCopy.Count;
		for (int i = 0; i < targetsCopy.Count; i++)
		{
			if (!targetsCopy[i].Target.HasCaptive)
			{
				_cachedProgress.CurrentProgressAmount++;
			}
		}
	}
}
