using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer.Missions;

public class MultiplayerPracticeMissionComponent : MissionLogic
{
	private LobbyClient _lobbyClient;

	private float _lastMessagePrintPassedTime;

	private bool _shutDownMissionTriggered;

	private float _shutDownMissionTimer;

	private int _shutDownMissionCount;

	private const int ShutDownDurationInSeconds = 3;

	public override void AfterStart()
	{
		base.AfterStart();
		_lobbyClient = NetworkMain.GameClient;
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		_lastMessagePrintPassedTime += dt;
		if (_shutDownMissionTriggered)
		{
			_shutDownMissionTimer += dt;
			if (_shutDownMissionTimer >= 1f)
			{
				_shutDownMissionTimer -= 1f;
				_shutDownMissionCount++;
				if (_shutDownMissionCount >= 3)
				{
					base.Mission.EndMission();
				}
				else
				{
					InformMissionDuration();
				}
			}
		}
		else if (_lobbyClient.CurrentState == LobbyClient.State.SearchingBattle)
		{
			if (_lastMessagePrintPassedTime > 5f)
			{
				InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=MrEhLbht}Still searching for a battle...").ToString()));
				_lastMessagePrintPassedTime = 0f;
			}
		}
		else if (_lobbyClient.CurrentState == LobbyClient.State.AtBattle && !_shutDownMissionTriggered)
		{
			_shutDownMissionTriggered = true;
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=BN1Pmhho}Found a battle by matchmaker!").ToString()));
			InformMissionDuration();
		}
	}

	private void InformMissionDuration()
	{
		int num = 3 - _shutDownMissionCount;
		TextObject textObject = new TextObject("{=aNMmlya4}Shutting down mission in {REMAINING_SECONDS_TO_SHUT_DOWN_MISSION} seconds!");
		textObject.SetTextVariable("REMAINING_SECONDS_TO_SHUT_DOWN_MISSION", num.ToString());
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
	}
}
