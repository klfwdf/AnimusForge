using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade;

public sealed class LobbyGameStateMatchmakerClient : LobbyGameState
{
	private LobbyClient _gameClient;

	private int _playerIndex;

	private int _sessionKey;

	private string _address;

	private int _assignedPort;

	private string _multiplayerGameType;

	private string _scene;

	private LobbyGameClientHandler _lobbyGameClientHandler;

	public void SetStartingParameters(LobbyGameClientHandler lobbyGameClientHandler, int playerIndex, int sessionKey, string address, int assignedPort, string multiplayerGameType, string scene)
	{
		_lobbyGameClientHandler = lobbyGameClientHandler;
		_gameClient = lobbyGameClientHandler.GameClient;
		_playerIndex = playerIndex;
		_sessionKey = sessionKey;
		_address = address;
		_assignedPort = assignedPort;
		_multiplayerGameType = multiplayerGameType;
		_scene = scene;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (_gameClient != null && (_gameClient.CurrentState == LobbyClient.State.AtLobby || _gameClient.CurrentState == LobbyClient.State.QuittingFromBattle || !_gameClient.Connected))
		{
			base.GameStateManager.PopState();
		}
	}

	protected override void StartMultiplayer()
	{
		GameNetwork.StartMultiplayerOnClient(_address, _assignedPort, _sessionKey, _playerIndex);
		BannerlordNetwork.StartMultiplayerLobbyMission(LobbyMissionType.Matchmaker);
		if (!Module.CurrentModule.StartMultiplayerGame(_multiplayerGameType, _scene))
		{
			Debug.FailedAssert("[DEBUG]Invalid multiplayer game type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\LobbyGameState.cs", "StartMultiplayer", 301);
		}
		PlatformServices.Instance?.CheckPrivilege(Privilege.Chat, displayResolveUI: true, delegate(bool result)
		{
			if (!result)
			{
				PlatformServices.Instance.ShowRestrictedInformation();
			}
		});
	}
}
