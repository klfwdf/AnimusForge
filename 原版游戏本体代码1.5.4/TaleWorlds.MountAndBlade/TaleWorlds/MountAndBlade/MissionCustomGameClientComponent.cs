using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade;

public class MissionCustomGameClientComponent : MissionLobbyComponent
{
	private LobbyClient _lobbyClient;

	private bool _isServerEndedBeforeClientLoaded;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_lobbyClient = NetworkMain.GameClient;
	}

	public void SetServerEndingBeforeClientLoaded(bool isServerEndingBeforeClientLoaded)
	{
		_isServerEndedBeforeClientLoaded = isServerEndingBeforeClientLoaded;
	}

	public override void QuitMission()
	{
		base.QuitMission();
		if (GameNetwork.IsServer)
		{
			if (base.CurrentMultiplayerState != MultiplayerGameState.Ending && _lobbyClient.LoggedIn && _lobbyClient.CurrentState == LobbyClient.State.HostingCustomGame)
			{
				_lobbyClient.EndCustomGame();
			}
		}
		else if (!_isServerEndedBeforeClientLoaded && base.CurrentMultiplayerState != MultiplayerGameState.Ending && _lobbyClient.LoggedIn && _lobbyClient.CurrentState == LobbyClient.State.InCustomGame)
		{
			_lobbyClient.QuitFromCustomGame();
		}
	}
}
