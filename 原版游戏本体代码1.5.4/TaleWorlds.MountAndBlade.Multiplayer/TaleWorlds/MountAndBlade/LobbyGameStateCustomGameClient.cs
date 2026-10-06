using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade;

public sealed class LobbyGameStateCustomGameClient : LobbyGameState
{
	private LobbyClient _gameClient;

	private string _address;

	private int _port;

	private int _peerIndex;

	private int _sessionKey;

	private Timer _inactivityTimer;

	private static readonly float InactivityThreshold = 2f;

	public void SetStartingParameters(LobbyClient gameClient, string address, int port, int peerIndex, int sessionKey)
	{
		_gameClient = gameClient;
		_address = address;
		_port = port;
		_peerIndex = peerIndex;
		_sessionKey = sessionKey;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		_inactivityTimer = new Timer(MBCommon.GetApplicationTime(), InactivityThreshold);
		if (_gameClient != null && (_gameClient.AtLobby || !_gameClient.Connected))
		{
			base.GameStateManager.PopState();
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (GameNetwork.IsClient && _inactivityTimer.Check(MBCommon.GetApplicationTime()) && _gameClient != null)
		{
			_gameClient.IsInCriticalState = GameNetwork.ElapsedTimeSinceLastUdpPacketArrived() > (double)InactivityThreshold;
		}
	}

	protected override void StartMultiplayer()
	{
		MBDebug.Print("CUSTOM GAME SERVER ADDRESS: " + _address);
		GameNetwork.StartMultiplayerOnClient(_address, _port, _sessionKey, _peerIndex);
		BannerlordNetwork.StartMultiplayerLobbyMission(LobbyMissionType.Custom);
		PlatformServices.Instance?.CheckPrivilege(Privilege.Chat, displayResolveUI: true, delegate(bool result)
		{
			if (!result)
			{
				PlatformServices.Instance.ShowRestrictedInformation();
			}
		});
	}
}
