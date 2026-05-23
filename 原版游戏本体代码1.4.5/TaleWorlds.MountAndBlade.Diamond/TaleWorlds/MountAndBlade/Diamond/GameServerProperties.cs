using System;
using System.Collections.Generic;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class GameServerProperties
{
	public string Name { get; set; }

	public string Address { get; set; }

	public int Port { get; set; }

	public string Region { get; set; }

	public string GameModule { get; set; }

	public string GameType { get; set; }

	public string Map { get; set; }

	public string UniqueMapId { get; set; }

	public string GamePassword { get; set; }

	public string AdminPassword { get; set; }

	public int MaxPlayerCount { get; set; }

	public bool PasswordProtected { get; set; }

	public bool IsOfficial { get; set; }

	public bool ByOfficialProvider { get; set; }

	public bool CrossplayEnabled { get; set; }

	public int Permission { get; set; }

	public PlayerId HostId { get; set; }

	public string HostName { get; set; }

	public List<ModuleInfoModel> LoadedModules { get; set; }

	public bool AllowsOptionalModules { get; set; }

	public GameServerProperties()
	{
	}

	public GameServerProperties(string name, string address, int port, string region, string gameModule, string gameType, string map, string uniqueMapId, string gamePassword, string adminPassword, int maxPlayerCount, bool isOfficial, bool byOfficialProvider, bool crossplayEnabled, PlayerId hostId, string hostName, List<ModuleInfoModel> loadedModules, bool allowsOptionalModules, int permission)
	{
		Name = name;
		Address = address;
		Port = port;
		Region = region;
		GameModule = gameModule;
		GameType = gameType;
		Map = map;
		GamePassword = gamePassword;
		UniqueMapId = uniqueMapId;
		AdminPassword = adminPassword;
		MaxPlayerCount = maxPlayerCount;
		IsOfficial = isOfficial;
		ByOfficialProvider = byOfficialProvider;
		CrossplayEnabled = crossplayEnabled;
		HostId = hostId;
		HostName = hostName;
		LoadedModules = loadedModules;
		AllowsOptionalModules = allowsOptionalModules;
		PasswordProtected = gamePassword != null;
		Permission = permission;
	}

	public void CheckAndReplaceProxyAddress(IReadOnlyDictionary<string, string> proxyAddressMap)
	{
		if (proxyAddressMap != null && proxyAddressMap.TryGetValue(Address, out var value))
		{
			Address = value;
		}
	}
}
