using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade;

public sealed class LobbyGameStateCommunityClient : LobbyGameState
{
	private CommunityClient _communityClient;

	private string _address;

	private int _port;

	private int _peerIndex;

	private int _sessionKey;

	public void SetStartingParameters(CommunityClient communityClient, string address, int port, int peerIndex, int sessionKey)
	{
		_communityClient = communityClient;
		_address = address;
		_port = port;
		_peerIndex = peerIndex;
		_sessionKey = sessionKey;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (_communityClient != null && !_communityClient.IsInGame)
		{
			base.GameStateManager.PopState();
		}
	}

	protected override void StartMultiplayer()
	{
		MBDebug.Print("COMMUNITY GAME SERVER ADDRESS: " + _address);
		GameNetwork.StartMultiplayerOnClient(_address, _port, _sessionKey, _peerIndex);
		BannerlordNetwork.StartMultiplayerLobbyMission(LobbyMissionType.Community);
		PlatformServices.Instance?.CheckPrivilege(Privilege.Chat, displayResolveUI: true, delegate(bool result)
		{
			if (!result)
			{
				PlatformServices.Instance.ShowRestrictedInformation();
			}
		});
	}

	protected override void OnDisconnectedFromServer()
	{
		base.OnDisconnectedFromServer();
		if (Game.Current.GameStateManager.ActiveState == this)
		{
			base.GameStateManager.PopState();
		}
	}
}
