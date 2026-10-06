using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromCustomBattleServerManager.ToCustomBattleServer;

[Serializable]
[MessageDescription("CustomBattleServerManager", "CustomBattleServer", true)]
public class ClientQuitFromCustomGameMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	public ClientQuitFromCustomGameMessage()
	{
	}

	public ClientQuitFromCustomGameMessage(PlayerId playerId)
	{
		PlayerId = playerId;
	}
}
