using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Multiplayer;

public static class MultiplayerReportPlayerManager
{
	private static Dictionary<PlayerId, int> _reportsPerPlayer = new Dictionary<PlayerId, int>();

	private const int _maxReportsPerPlayer = 3;

	public static event Action<string, PlayerId, string, bool> ReportHandlers;

	public static void RequestReportPlayer(string gameId, PlayerId playerId, string playerName, bool isRequestedFromMission)
	{
		MultiplayerReportPlayerManager.ReportHandlers?.Invoke(gameId, playerId, playerName, isRequestedFromMission);
	}

	public static void OnPlayerReported(PlayerId playerId)
	{
		IncrementReportOfPlayer(playerId);
		GameNetwork.NetworkPeers.Find((NetworkCommunicator x) => x.VirtualPlayer.Id == playerId)?.GetComponent<MissionPeer>()?.SetMuted(isMuted: true);
		Game.Current.GetGameHandler<ChatBox>().SetPlayerMuted(playerId, isMuted: true);
	}

	public static bool IsPlayerReportedOverLimit(PlayerId player)
	{
		if (_reportsPerPlayer.TryGetValue(player, out var value))
		{
			return value == 3;
		}
		return false;
	}

	private static void IncrementReportOfPlayer(PlayerId player)
	{
		if (_reportsPerPlayer.ContainsKey(player))
		{
			_reportsPerPlayer[player]++;
		}
		else
		{
			_reportsPerPlayer.Add(player, 1);
		}
	}
}
