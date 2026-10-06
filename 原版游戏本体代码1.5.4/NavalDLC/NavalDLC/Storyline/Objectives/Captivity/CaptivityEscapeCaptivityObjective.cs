using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Captivity;

public class CaptivityEscapeCaptivityObjective : MissionObjective
{
	private readonly NavalStorylineCaptivityMissionController _captivityMissionController;

	private readonly TextObject _name;

	private readonly TextObject _description;

	private MissionObjectiveProgressInfo _cachedProgress;

	public override string UniqueId => "CaptivityEscapeCaptivityObjective";

	public override TextObject Name => _name;

	public override TextObject Description => _description;

	public CaptivityEscapeCaptivityObjective(Mission mission, NavalStorylineCaptivityMissionController captivityMissionController)
		: base(mission)
	{
		_name = new TextObject("{=Kl4fHd5i}Escape Captivity");
		_description = new TextObject("{=3Tvyyz7p}Unchain yourself from the oar bench.");
		_captivityMissionController = captivityMissionController;
		_cachedProgress.RequiredProgressAmount = 0;
		_cachedProgress.CurrentProgressAmount = 0;
	}

	protected override bool IsActivationRequirementsMet()
	{
		return true;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return _captivityMissionController.IsPlayerFree;
	}

	public override MissionObjectiveProgressInfo GetCurrentProgress()
	{
		return _cachedProgress;
	}
}
