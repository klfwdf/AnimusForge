using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class GameServerEntry
{
	[JsonProperty]
	public CustomBattleId Id { get; private set; }

	[JsonProperty]
	public string Address { get; private set; }

	[JsonProperty]
	public int Port { get; private set; }

	[JsonProperty]
	public string Region { get; private set; }

	[JsonProperty]
	public int PlayerCount { get; private set; }

	[JsonProperty]
	public int MaxPlayerCount { get; private set; }

	[JsonProperty]
	public string ServerName { get; private set; }

	[JsonProperty]
	public string GameModule { get; private set; }

	[JsonProperty]
	public string GameType { get; private set; }

	[JsonProperty]
	public string Map { get; private set; }

	[JsonProperty]
	public string UniqueMapId { get; private set; }

	[JsonProperty]
	public int Ping { get; private set; }

	[JsonProperty]
	public bool IsOfficial { get; private set; }

	[JsonProperty]
	public bool ByOfficialProvider { get; private set; }

	[JsonProperty]
	public bool PasswordProtected { get; private set; }

	[JsonProperty]
	public int Permission { get; private set; }

	[JsonProperty]
	public bool CrossplayEnabled { get; private set; }

	[JsonProperty]
	public PlayerId HostId { get; private set; }

	[JsonProperty]
	public string HostName { get; private set; }

	[JsonProperty]
	public List<ModuleInfoModel> LoadedModules { get; private set; }

	[JsonProperty]
	public bool AllowsOptionalModules { get; private set; }

	public GameServerEntry()
	{
	}

	public GameServerEntry(CustomBattleId id, string serverName, string address, int port, string region, string gameModule, string gameType, string map, string uniqueMapId, int playerCount, int maxPlayerCount, bool isOfficial, bool byOfficialProvider, bool crossplayEnabled, PlayerId hostId, string hostName, List<ModuleInfoModel> loadedModules, bool allowsOptionalModules, bool passwordProtected = false, int permission = 0)
	{
		Id = id;
		ServerName = serverName;
		Address = address;
		GameModule = gameModule;
		GameType = gameType;
		Map = map;
		UniqueMapId = uniqueMapId;
		PlayerCount = playerCount;
		MaxPlayerCount = maxPlayerCount;
		Port = port;
		Region = region;
		IsOfficial = isOfficial;
		ByOfficialProvider = byOfficialProvider;
		CrossplayEnabled = crossplayEnabled;
		HostId = hostId;
		HostName = hostName;
		LoadedModules = loadedModules;
		AllowsOptionalModules = allowsOptionalModules;
		PasswordProtected = passwordProtected;
		Permission = permission;
	}

	public static void FilterGameServerEntriesBasedOnCrossplay(ref List<GameServerEntry> serverList, bool hasCrossplayPrivilege)
	{
		bool flag = ApplicationPlatform.CurrentPlatform == Platform.GDKDesktop;
		bool flag2 = hasCrossplayPrivilege;
		if (flag && !flag2)
		{
			serverList.RemoveAll((GameServerEntry s) => s.CrossplayEnabled);
		}
		else if (!flag)
		{
			serverList.RemoveAll((GameServerEntry s) => !s.CrossplayEnabled);
		}
	}
}
