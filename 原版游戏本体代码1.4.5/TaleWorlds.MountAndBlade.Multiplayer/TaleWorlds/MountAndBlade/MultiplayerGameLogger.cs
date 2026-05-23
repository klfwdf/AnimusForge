using System.Collections.Generic;
using System.Threading;
using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerGameLogger : GameHandler
{
	public const int PreInitialLogId = 0;

	private ChatBox _chatBox;

	private int _lastLogId;

	private List<GameLog> _gameLogs;

	public IReadOnlyList<GameLog> GameLogs => _gameLogs.AsReadOnly();

	public MultiplayerGameLogger()
	{
		_lastLogId = 0;
		_gameLogs = new List<GameLog>();
	}

	public void Log(GameLog log)
	{
		log.Id = Interlocked.Increment(ref _lastLogId);
		_gameLogs?.Add(log);
	}

	protected override void OnGameStart()
	{
	}

	public override void OnBeforeSave()
	{
	}

	public override void OnAfterSave()
	{
	}

	protected override void OnGameNetworkBegin()
	{
		_chatBox = Game.Current.GetGameHandler<ChatBox>();
		GameNetwork.NetworkMessageHandlerRegisterer networkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegisterer(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
		if (GameNetwork.IsServer)
		{
			networkMessageHandlerRegisterer.Register<PlayerMessageAll>(HandleClientEventPlayerMessageAll);
			networkMessageHandlerRegisterer.Register<PlayerMessageTeam>(HandleClientEventPlayerMessageTeam);
		}
	}

	private bool HandleClientEventPlayerMessageAll(NetworkCommunicator networkPeer, PlayerMessageAll message)
	{
		GameLog gameLog = new GameLog(GameLogType.ChatMessage, networkPeer.VirtualPlayer.Id, MBCommon.GetTotalMissionTime());
		gameLog.Data.Add("Message", message.Message);
		gameLog.Data.Add("IsTeam", false.ToString());
		gameLog.Data.Add("IsMuted", (_chatBox?.IsPlayerMuted(networkPeer.VirtualPlayer.Id)).ToString());
		gameLog.Data.Add("IsGlobalMuted", networkPeer.IsMuted.ToString());
		Log(gameLog);
		return true;
	}

	private bool HandleClientEventPlayerMessageTeam(NetworkCommunicator networkPeer, PlayerMessageTeam message)
	{
		GameLog gameLog = new GameLog(GameLogType.ChatMessage, networkPeer.VirtualPlayer.Id, MBCommon.GetTotalMissionTime());
		gameLog.Data.Add("Message", message.Message);
		gameLog.Data.Add("IsTeam", true.ToString());
		gameLog.Data.Add("IsMuted", (_chatBox?.IsPlayerMuted(networkPeer.VirtualPlayer.Id)).ToString());
		gameLog.Data.Add("IsGlobalMuted", networkPeer.IsMuted.ToString());
		Log(gameLog);
		return true;
	}
}
