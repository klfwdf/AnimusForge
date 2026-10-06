using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class AvailableCustomGames
{
	[JsonProperty]
	public List<GameServerEntry> CustomGameServerInfos { get; private set; }

	public AvailableCustomGames()
	{
		CustomGameServerInfos = new List<GameServerEntry>();
	}

	public AvailableCustomGames GetCustomGamesByPermission(int playerPermission)
	{
		AvailableCustomGames availableCustomGames = new AvailableCustomGames();
		foreach (GameServerEntry customGameServerInfo in CustomGameServerInfos)
		{
			if (customGameServerInfo.Permission <= playerPermission)
			{
				availableCustomGames.CustomGameServerInfos.Add(customGameServerInfo);
			}
		}
		return availableCustomGames;
	}
}
