using System;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade;

public class MBMultiplayerData
{
	public delegate void GameServerInfoReceivedDelegate(CustomBattleId id, string gameServer, string gameModule, string gameType, string map, int currentPlayerCount, int maxPlayerCount, string address, int port);

	public static string ServerName;

	public static string GameModule;

	public static string GameType;

	public static string Map;

	public static int PlayerCountLimit;

	public static Guid ServerId { get; set; }

	public static event GameServerInfoReceivedDelegate GameServerInfoReceived;

	[MBCallback(null, false)]
	public static string GetServerId()
	{
		return ServerId.ToString();
	}

	[MBCallback(null, false)]
	public static string GetServerName()
	{
		return ServerName;
	}

	[MBCallback(null, false)]
	public static string GetGameModule()
	{
		return GameModule;
	}

	[MBCallback(null, false)]
	public static string GetGameType()
	{
		return GameType;
	}

	[MBCallback(null, false)]
	public static string GetMap()
	{
		return Map;
	}

	[MBCallback(null, false)]
	public static int GetCurrentPlayerCount()
	{
		return GameNetwork.NetworkPeerCount;
	}

	[MBCallback(null, false)]
	public static int GetPlayerCountLimit()
	{
		return PlayerCountLimit;
	}

	[MBCallback(null, false)]
	public static void UpdateGameServerInfo(string id, string gameServer, string gameModule, string gameType, string map, int currentPlayerCount, int maxPlayerCount, string address, int port)
	{
		if (MBMultiplayerData.GameServerInfoReceived != null)
		{
			MBMultiplayerData.GameServerInfoReceived(new CustomBattleId(Guid.Parse(id)), gameServer, gameModule, gameType, map, currentPlayerCount, maxPlayerCount, address, port);
		}
	}
}
