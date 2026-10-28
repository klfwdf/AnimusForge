using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BasicLeaveMissionLogic : MissionLogic
{
	private readonly bool _askBeforeLeave;

	private readonly int _minRetreatDistance;

	public BasicLeaveMissionLogic()
		: this(askBeforeLeave: false)
	{
	}

	public BasicLeaveMissionLogic(bool askBeforeLeave)
		: this(askBeforeLeave, 5)
	{
	}

	public BasicLeaveMissionLogic(bool askBeforeLeave, int minRetreatDistance)
	{
		_askBeforeLeave = askBeforeLeave;
		_minRetreatDistance = minRetreatDistance;
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		if (base.Mission.MainAgent != null)
		{
			return !base.Mission.MainAgent.IsActive();
		}
		return false;
	}

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		if (base.Mission.MainAgent != null && base.Mission.MainAgent.IsActive() && (float)_minRetreatDistance > 0f && base.Mission.IsPlayerCloseToAnEnemy(_minRetreatDistance))
		{
			canPlayerLeave = false;
			MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat"));
		}
		else if (_askBeforeLeave)
		{
			return new InquiryData("", GameTexts.FindText("str_give_up_fight").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), base.Mission.OnEndMissionResult, null);
		}
		return null;
	}
}
