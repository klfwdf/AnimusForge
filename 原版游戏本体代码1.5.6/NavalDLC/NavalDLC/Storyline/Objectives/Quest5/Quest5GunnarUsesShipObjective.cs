using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest5;

public class Quest5GunnarUsesShipObjective : MissionObjective
{
	public override string UniqueId => "quest_5_gunnar_uses_ship_objective";

	public override TextObject Name => new TextObject("{=LBNwZ3HS}Keep Watch");

	public override TextObject Description => new TextObject("{=araGPQbp}Keep watch for approaching enemy ships.");

	public Quest5GunnarUsesShipObjective(Mission mission)
		: base(mission)
	{
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
