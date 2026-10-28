using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Captivity;

public class HelpingAnAllyMissionObjective : MissionObjective
{
	private readonly TextObject _name;

	private readonly TextObject _description;

	private MissionObjectiveProgressInfo _cachedProgress;

	public override string UniqueId => "HelpingAnAllyMissionObjective";

	public override TextObject Name => _name;

	public override TextObject Description => _description;

	public HelpingAnAllyMissionObjective(Mission mission)
		: base(mission)
	{
		_name = new TextObject("{=J9ruJTIQ}Protect the Merchants");
		_description = new TextObject("{=u2q4PdaI}Defeat all Sea Hounds before they capture the Vlandian merchantman");
		_cachedProgress = default(MissionObjectiveProgressInfo);
	}

	protected override bool IsActivationRequirementsMet()
	{
		return true;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return false;
	}

	public override MissionObjectiveProgressInfo GetCurrentProgress()
	{
		return _cachedProgress;
	}
}
