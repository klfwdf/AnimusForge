using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest5;

public class Quest5DefeatPurigObjective : MissionObjective
{
	private readonly Agent _purigAgent;

	public override string UniqueId => "quest_5_defeat_purig_objective";

	public override TextObject Name => new TextObject("{=lJ5BA3k4}Defeat Purig - Duel");

	public override TextObject Description => new TextObject("{=oNBSZp8H}Defeat Purig in a duel.");

	public Quest5DefeatPurigObjective(Mission mission, Agent purigAgent)
		: base(mission)
	{
		_purigAgent = purigAgent;
	}

	protected override bool IsActivationRequirementsMet()
	{
		return _purigAgent != null;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		if (_purigAgent != null)
		{
			return !_purigAgent.IsActive();
		}
		return true;
	}
}
