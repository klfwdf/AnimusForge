using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class RegisterCustomGameMessage : Message
{
	[JsonProperty]
	public string GameModule { get; private set; }

	[JsonProperty]
	public string GameType { get; private set; }

	[JsonProperty]
	public string ServerName { get; private set; }

	[JsonProperty]
	public int MaxPlayerCount { get; private set; }

	[JsonProperty]
	public string Map { get; private set; }

	[JsonProperty]
	public string UniqueMapId { get; private set; }

	[JsonProperty]
	public int Port { get; private set; }

	[JsonProperty]
	public string GamePassword { get; private set; }

	[JsonProperty]
	public string AdminPassword { get; private set; }

	public RegisterCustomGameMessage()
	{
	}

	public RegisterCustomGameMessage(string gameModule, string gameType, string serverName, int maxPlayerCount, string map, string uniqueMapId, string gamePassword, string adminPassword, int port)
	{
		GameModule = gameModule;
		GameType = gameType;
		ServerName = serverName;
		MaxPlayerCount = maxPlayerCount;
		Map = map;
		UniqueMapId = uniqueMapId;
		GamePassword = gamePassword;
		AdminPassword = adminPassword;
		Port = port;
	}
}
