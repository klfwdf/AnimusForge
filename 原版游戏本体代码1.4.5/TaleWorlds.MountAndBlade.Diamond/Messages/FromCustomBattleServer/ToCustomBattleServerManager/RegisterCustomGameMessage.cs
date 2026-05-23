using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromCustomBattleServer.ToCustomBattleServerManager;

[Serializable]
[MessageDescription("CustomBattleServer", "CustomBattleServerManager", true)]
public class RegisterCustomGameMessage : Message
{
	[JsonProperty]
	public int GameDefinitionId { get; private set; }

	[JsonProperty]
	public string GameModule { get; private set; }

	[JsonProperty]
	public string GameType { get; private set; }

	[JsonProperty]
	public string ServerName { get; private set; }

	[JsonProperty]
	public string ServerAddress { get; private set; }

	[JsonProperty]
	public int MaxPlayerCount { get; private set; }

	[JsonProperty]
	public string Map { get; private set; }

	[JsonProperty]
	public string UniqueMapId { get; private set; }

	[JsonProperty]
	public string GamePassword { get; private set; }

	[JsonProperty]
	public string AdminPassword { get; private set; }

	[JsonProperty]
	public int Port { get; private set; }

	[JsonProperty]
	public string Region { get; private set; }

	[JsonProperty]
	public int Permission { get; private set; }

	[JsonProperty]
	public bool IsOverridingIP { get; private set; }

	[JsonProperty]
	public bool CrossplayEnabled { get; private set; }

	public RegisterCustomGameMessage()
	{
	}

	public RegisterCustomGameMessage(int gameDefinitionId, string gameModule, string gameType, string serverName, string serverAddress, int maxPlayerCount, string map, string uniqueMapId, string gamePassword, string adminPassword, int port, string region, int permission, bool crossplayEnabled, bool isOverridingIP)
	{
		GameDefinitionId = gameDefinitionId;
		GameModule = gameModule;
		GameType = gameType;
		ServerName = serverName;
		ServerAddress = serverAddress;
		MaxPlayerCount = maxPlayerCount;
		Map = map;
		UniqueMapId = uniqueMapId;
		GamePassword = gamePassword;
		AdminPassword = adminPassword;
		Port = port;
		Region = region;
		Permission = permission;
		CrossplayEnabled = crossplayEnabled;
		IsOverridingIP = isOverridingIP;
	}
}
