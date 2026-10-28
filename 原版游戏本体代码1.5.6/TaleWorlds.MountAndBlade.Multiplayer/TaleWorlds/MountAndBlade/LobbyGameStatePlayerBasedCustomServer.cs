using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade;

public sealed class LobbyGameStatePlayerBasedCustomServer : LobbyGameState
{
	private LobbyClient _gameClient;

	public void SetStartingParameters(LobbyGameClientHandler lobbyGameClientHandler)
	{
		_gameClient = lobbyGameClientHandler.GameClient;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (_gameClient != null && (_gameClient.AtLobby || !_gameClient.Connected))
		{
			base.GameStateManager.PopState();
		}
	}

	protected override void StartMultiplayer()
	{
		HandleServerStartMultiplayer();
	}

	private async void HandleServerStartMultiplayer()
	{
		GameNetwork.PreStartMultiplayerOnServer();
		BannerlordNetwork.StartMultiplayerLobbyMission(LobbyMissionType.Custom);
		if (!Module.CurrentModule.StartMultiplayerGame(_gameClient.CustomGameType, _gameClient.CustomGameScene))
		{
			Debug.FailedAssert("[DEBUG]Invalid multiplayer game type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\LobbyGameState.cs", "HandleServerStartMultiplayer", 346);
		}
		while (Mission.Current == null || Mission.Current.CurrentState != Mission.State.Continuing)
		{
			await Task.Delay(1);
		}
		GameNetwork.StartMultiplayerOnServer(9999);
		if (_gameClient.IsInGame)
		{
			BannerlordNetwork.CreateServerPeer();
			MBDebug.Print("Server: I finished loading and I am now visible to clients in the server list.", 0, Debug.DebugColor.White, 17179869184uL);
			if (!GameNetwork.IsDedicatedServer)
			{
				GameNetwork.ClientFinishedLoading(GameNetwork.MyPeer);
			}
		}
		PlatformServices.Instance?.CheckPrivilege(Privilege.Chat, displayResolveUI: false, delegate(bool result)
		{
			if (!result)
			{
				PlatformServices.Instance.ShowRestrictedInformation();
			}
		});
	}
}
