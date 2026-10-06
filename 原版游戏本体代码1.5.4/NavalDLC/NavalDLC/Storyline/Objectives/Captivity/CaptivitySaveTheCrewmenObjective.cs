using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Captivity;

public class CaptivitySaveTheCrewmenObjective : MissionObjective
{
	private class CaptivityCrewmenTarget : MissionObjectiveTarget<Agent>
	{
		private readonly TextObject _name;

		public CaptivityCrewmenTarget(TextObject name, Agent agent)
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
			return !base.Target.IsOnLand();
		}
	}

	private readonly NavalStorylineCaptivityMissionController _captivityMissionController;

	private readonly TextObject _name;

	private readonly TextObject _description;

	private readonly TextObject _targetName;

	private MissionObjectiveProgressInfo _cachedProgress;

	public override string UniqueId => "CaptivitySaveTheCrewmenObjective";

	public override TextObject Name => _name;

	public override TextObject Description => _description;

	public CaptivitySaveTheCrewmenObjective(Mission mission, NavalStorylineCaptivityMissionController captivityMissionController)
		: base(mission)
	{
		_name = new TextObject("{=tvGCC1BF}Save the Crewmen");
		_description = new TextObject("{=Ed0TIDfv}Steer the ship to save the crewmen in the water.");
		_targetName = new TextObject("{=i0ELqRca}Rescue");
		_captivityMissionController = captivityMissionController;
		foreach (Agent scatteredCrewman in _captivityMissionController.GetScatteredCrewmen())
		{
			CaptivityCrewmenTarget target = new CaptivityCrewmenTarget(_targetName, scatteredCrewman);
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
		MBReadOnlyList<CaptivityCrewmenTarget> targetsCopy = GetTargetsCopy<CaptivityCrewmenTarget>();
		_cachedProgress.CurrentProgressAmount = 0;
		_cachedProgress.RequiredProgressAmount = targetsCopy.Count;
		for (int i = 0; i < targetsCopy.Count; i++)
		{
			if (targetsCopy[i].Target.IsOnLand())
			{
				_cachedProgress.CurrentProgressAmount++;
			}
		}
	}
}
