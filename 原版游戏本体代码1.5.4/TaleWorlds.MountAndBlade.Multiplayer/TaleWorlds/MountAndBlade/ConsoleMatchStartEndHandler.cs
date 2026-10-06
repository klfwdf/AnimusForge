using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade;

public class ConsoleMatchStartEndHandler : MissionNetwork
{
	private enum MatchState
	{
		NotPlaying,
		Playing
	}

	private MissionMultiplayerGameModeBaseClient _gameModeClient;

	private MultiplayerMissionAgentVisualSpawnComponent _visualSpawnComponent;

	private MatchState _matchState;

	private bool _inGameCheckActive;

	private float _playingCheckTimer;

	private List<VirtualPlayer> _activeOtherPlayers;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_activeOtherPlayers = new List<VirtualPlayer>();
		_matchState = MatchState.NotPlaying;
		_gameModeClient = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		_visualSpawnComponent = base.Mission.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>();
		_visualSpawnComponent.OnMyAgentSpawnedFromVisual += AgentVisualSpawnComponentOnOnMyAgentVisualSpawned;
		MissionPeer.OnTeamChanged += OnTeamChange;
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		MissionPeer.OnTeamChanged -= OnTeamChange;
		if (_matchState == MatchState.Playing)
		{
			_matchState = MatchState.NotPlaying;
			PlatformServices.MultiplayerGameStateChanged(isPlaying: false);
		}
	}

	private void AgentVisualSpawnComponentOnOnMyAgentVisualSpawned()
	{
		_visualSpawnComponent.OnMyAgentSpawnedFromVisual -= AgentVisualSpawnComponentOnOnMyAgentVisualSpawned;
		_inGameCheckActive = true;
	}

	private void OnTeamChange(NetworkCommunicator peer, Team previousTeam, Team newTeam)
	{
		if (newTeam.Side != BattleSideEnum.None)
		{
			return;
		}
		if (peer.IsMine)
		{
			_visualSpawnComponent.OnMyAgentVisualSpawned += AgentVisualSpawnComponentOnOnMyAgentVisualSpawned;
			_inGameCheckActive = false;
			PlatformServices.MultiplayerGameStateChanged(isPlaying: false);
			return;
		}
		int num = _activeOtherPlayers.IndexOf(peer.VirtualPlayer);
		if (num >= 0)
		{
			_activeOtherPlayers.RemoveAt(num);
		}
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		if (agent.MissionPeer != null && !agent.MissionPeer.IsMine && !_activeOtherPlayers.Contains(agent.MissionPeer.Peer))
		{
			_activeOtherPlayers.Add(agent.MissionPeer.Peer);
		}
	}

	public override void OnMissionTick(float dt)
	{
		_playingCheckTimer -= dt;
		if (!(_playingCheckTimer <= 0f))
		{
			return;
		}
		_playingCheckTimer += 1f;
		if (!_inGameCheckActive)
		{
			return;
		}
		if (_activeOtherPlayers.Count > 0)
		{
			if (_matchState == MatchState.NotPlaying)
			{
				_matchState = MatchState.Playing;
				PlatformServices.MultiplayerGameStateChanged(isPlaying: true);
			}
		}
		else if (_matchState == MatchState.Playing)
		{
			_matchState = MatchState.NotPlaying;
			PlatformServices.MultiplayerGameStateChanged(isPlaying: false);
		}
	}
}
