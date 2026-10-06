using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Source.Missions.Handlers;

public class BasicMissionHandler : MissionLogic
{
	private bool _isSurrender;

	public bool IsWarningWidgetOpened { get; private set; }

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		IsWarningWidgetOpened = false;
	}

	public void CreateWarningWidgetForResult(BattleEndLogic.ExitResult result)
	{
		if (!GameNetwork.IsClient)
		{
			MBCommon.PauseGameEngine();
		}
		_isSurrender = result == BattleEndLogic.ExitResult.SurrenderSiege;
		InformationManager.ShowInquiry(_isSurrender ? GetSurrenderPopupData() : GetRetreatPopUpData(), pauseGameActiveState: true);
		IsWarningWidgetOpened = true;
	}

	private void CloseSelectionWidget()
	{
		if (IsWarningWidgetOpened)
		{
			IsWarningWidgetOpened = false;
			if (!GameNetwork.IsClient)
			{
				MBCommon.UnPauseGameEngine();
			}
		}
	}

	private void OnEventCancelSelectionWidget()
	{
		CloseSelectionWidget();
	}

	private void OnEventAcceptSelectionWidget()
	{
		MissionLogic[] array = base.Mission.MissionLogics.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnBattleEnded();
		}
		CloseSelectionWidget();
		if (_isSurrender)
		{
			base.Mission.SurrenderMission();
		}
		else
		{
			base.Mission.RetreatMission();
		}
	}

	private InquiryData GetRetreatPopUpData()
	{
		return new InquiryData("", GameTexts.FindText("str_retreat_question").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), OnEventAcceptSelectionWidget, OnEventCancelSelectionWidget);
	}

	private InquiryData GetSurrenderPopupData()
	{
		return new InquiryData(GameTexts.FindText("str_surrender").ToString(), GameTexts.FindText("str_surrender_question").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), OnEventAcceptSelectionWidget, OnEventCancelSelectionWidget);
	}
}
