using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade;

public class MissionBattleSchedulerClientComponent : MissionLobbyComponent
{
	public override void QuitMission()
	{
		base.QuitMission();
		if (base.CurrentMultiplayerState != MultiplayerGameState.Ending && NetworkMain.GameClient.LoggedIn && NetworkMain.GameClient.CurrentState == LobbyClient.State.AtBattle)
		{
			NetworkMain.GameClient.QuitFromMatchmakerGame();
		}
	}
}
