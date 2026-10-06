using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Captivity;

public class CaptivityDefeatCaptorsObjective : MissionObjective
{
	private class CaptivityEnemyTarget : MissionObjectiveTarget<Agent>
	{
		private readonly TextObject _name;

		public CaptivityEnemyTarget(TextObject name, Agent agent)
			: base(agent)
		{
			_name = name;
		}

		public override Vec3 GetGlobalPosition()
		{
			if (Agent.Main == null)
			{
				return Vec3.Invalid;
			}
			return base.Target.Position + base.Target.Frame.rotation.u * 1.5f;
		}

		public override TextObject GetName()
		{
			return _name;
		}

		public override bool IsActive()
		{
			return base.Target.IsActive();
		}
	}

	private readonly NavalStorylineCaptivityMissionController _captivityMissionController;

	private readonly TextObject _name;

	private readonly TextObject _description;

	private readonly TextObject _targetName;

	private MissionObjectiveProgressInfo _cachedProgress;

	public override string UniqueId => "CaptivityDefeatCaptorsObjective";

	public override TextObject Name => _name;

	public override TextObject Description => _description;

	public CaptivityDefeatCaptorsObjective(Mission mission, NavalStorylineCaptivityMissionController captivityMissionController)
		: base(mission)
	{
		_name = new TextObject("{=Kl4fHd5i}Escape Captivity");
		_description = new TextObject("{=sHQ5b9fV}Defeat your captors.");
		_targetName = new TextObject("{=defeatVerb}Defeat");
		_captivityMissionController = captivityMissionController;
		foreach (Agent captorAgent in _captivityMissionController.GetCaptorAgents())
		{
			CaptivityEnemyTarget target = new CaptivityEnemyTarget(_targetName, captorAgent);
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
		MBReadOnlyList<CaptivityEnemyTarget> targetsCopy = GetTargetsCopy<CaptivityEnemyTarget>();
		_cachedProgress.CurrentProgressAmount = 0;
		_cachedProgress.RequiredProgressAmount = targetsCopy.Count;
		for (int i = 0; i < targetsCopy.Count; i++)
		{
			if (!targetsCopy[i].Target.IsActive())
			{
				_cachedProgress.CurrentProgressAmount++;
			}
		}
	}
}
