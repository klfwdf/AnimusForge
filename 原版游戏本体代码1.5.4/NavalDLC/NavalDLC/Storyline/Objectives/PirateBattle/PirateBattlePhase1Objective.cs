using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.PirateBattle;

public class PirateBattlePhase1Objective : MissionObjective
{
	private readonly PirateBattleMissionController _missionController;

	private readonly TextObject _name;

	private readonly TextObject _description;

	private MissionObjectiveProgressInfo _cachedProgress;

	public override string UniqueId => "PirateBattlePhase1Objective";

	public override TextObject Name => _name;

	public override TextObject Description => _description;

	public PirateBattlePhase1Objective(Mission mission, PirateBattleMissionController missionController)
		: base(mission)
	{
		_name = new TextObject("{=wKBtraSp}Defeat the Sea Hounds");
		_description = new TextObject("{=uPJWFjM8}Board the enemy ship and defeat their troops.");
		_missionController = missionController;
		_cachedProgress = default(MissionObjectiveProgressInfo);
		_cachedProgress.RequiredProgressAmount = 0;
	}

	protected override bool IsActivationRequirementsMet()
	{
		return true;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return _missionController.IsFirstShipCleared;
	}

	public override MissionObjectiveProgressInfo GetCurrentProgress()
	{
		return _cachedProgress;
	}
}
