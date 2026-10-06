using System;
using Newtonsoft.Json;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromCustomBattleServer.ToCustomBattleServerManager;

[Serializable]
public class PlayerDisconnectData
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public DisconnectType Type { get; private set; }

	public PlayerDisconnectData()
	{
	}

	public PlayerDisconnectData(PlayerId playerId, DisconnectType type)
	{
		PlayerId = playerId;
		Type = type;
	}
}
