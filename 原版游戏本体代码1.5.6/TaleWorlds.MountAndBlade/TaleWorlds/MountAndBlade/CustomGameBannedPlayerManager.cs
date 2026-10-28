using System;
using System.Collections.Generic;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public static class CustomGameBannedPlayerManager
{
	private struct BannedPlayer
	{
		public PlayerId PlayerId { get; set; }

		public int BanDueTime { get; set; }
	}

	private static Dictionary<PlayerId, BannedPlayer> _bannedPlayersInternal;

	private static Dictionary<PlayerId, BannedPlayer> _bannedPlayers
	{
		get
		{
			if (_bannedPlayersInternal == null)
			{
				_bannedPlayersInternal = new Dictionary<PlayerId, BannedPlayer>();
			}
			return _bannedPlayersInternal;
		}
	}

	public static void AddBannedPlayer(PlayerId playerId, int banDueTime)
	{
		_bannedPlayers[playerId] = new BannedPlayer
		{
			PlayerId = playerId,
			BanDueTime = banDueTime
		};
	}

	public static bool IsUserBanned(PlayerId playerId)
	{
		if (_bannedPlayers.ContainsKey(playerId))
		{
			return _bannedPlayers[playerId].BanDueTime > Environment.TickCount;
		}
		return false;
	}
}
