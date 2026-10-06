using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.Missions.BattleScore;

public class CustomBattleScoreContext : BattleScoreContext
{
	private readonly Mission _mission;

	public override bool IsPowerComparisonRelevant => _mission.Mode != MissionMode.Deployment;

	public CustomBattleScoreContext(Mission mission)
	{
		_mission = mission;
	}

	public override Banner GetAttackerBanner()
	{
		return GetSideBannerInfo(BattleSideEnum.Attacker);
	}

	public override Banner GetDefenderBanner()
	{
		return GetSideBannerInfo(BattleSideEnum.Defender);
	}

	private Banner GetSideBannerInfo(BattleSideEnum sideEnum)
	{
		return _mission.GetMissionBehavior<MissionCombatantsLogic>()?.GetBannerForSide(sideEnum);
	}
}
