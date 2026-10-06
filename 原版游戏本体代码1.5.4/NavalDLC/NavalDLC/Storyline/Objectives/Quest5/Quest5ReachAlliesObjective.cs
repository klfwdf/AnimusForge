using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest5;

public class Quest5ReachAlliesObjective : MissionObjective
{
	private readonly VolumeBox _targetVolumeBox;

	public override string UniqueId => "quest_5_reach_allies_objective";

	public override TextObject Name => new TextObject("{=LBNwZ3HS}Keep Watch");

	public override TextObject Description => new TextObject("{=araGPQbp}Keep watch for approaching enemy ships.");

	public Quest5ReachAlliesObjective(Mission mission, VolumeBox targetVolumeBox)
		: base(mission)
	{
		_targetVolumeBox = targetVolumeBox;
	}

	protected override bool IsActivationRequirementsMet()
	{
		return true;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		Agent main = Agent.Main;
		if (main != null && main.IsActive())
		{
			return _targetVolumeBox.IsPointIn(Agent.Main.Position);
		}
		return false;
	}
}
