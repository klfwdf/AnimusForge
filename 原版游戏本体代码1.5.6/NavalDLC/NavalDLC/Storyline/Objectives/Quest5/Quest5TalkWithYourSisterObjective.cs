using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest5;

public class Quest5TalkWithYourSisterObjective : MissionObjective
{
	private class TalkWithYourSisterObjectiveTarget : MissionObjectiveTarget
	{
		public readonly Agent TargetAgent;

		public TalkWithYourSisterObjectiveTarget(Agent sister)
		{
			TargetAgent = sister;
		}

		public override TextObject GetName()
		{
			return new TextObject("{=pY5bft0t}Cage for prisoners");
		}

		public override Vec3 GetGlobalPosition()
		{
			return TargetAgent.GetEyeGlobalPosition();
		}

		public override bool IsActive()
		{
			if (TargetAgent != null)
			{
				return TargetAgent.IsActive();
			}
			return false;
		}
	}

	private TalkWithYourSisterObjectiveTarget _target;

	public override string UniqueId => "quest_5_talk_with_your_sister_objective";

	public override TextObject Name => new TextObject("{=btfAQ47G}Find your sister");

	public override TextObject Description => new TextObject("{=VTjKuGYw}Find your sister in the hold of the prisoner ship.");

	public Quest5TalkWithYourSisterObjective(Mission mission, Agent sister)
		: base(mission)
	{
		_target = new TalkWithYourSisterObjectiveTarget(sister);
		AddTarget(_target);
	}

	protected override bool IsActivationRequirementsMet()
	{
		return _target != null;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return false;
	}
}
