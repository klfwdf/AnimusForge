using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest5;

public class Quest5EscapeObjective : MissionObjective
{
	private readonly TextObject _description;

	public override string UniqueId => "quest_5_escape_objective";

	public override TextObject Name => new TextObject("{=BkIpqqTD}Hold Off Sea Hounds");

	public override TextObject Description => new TextObject("{=ZTiHqbi9}Fight off any Sea Hound pursuers until Gunnar can steer the ship to safety.");

	public Quest5EscapeObjective(Mission mission, TextObject description)
		: base(mission)
	{
		_description = description;
	}

	protected override bool IsActivationRequirementsMet()
	{
		return true;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return false;
	}
}
