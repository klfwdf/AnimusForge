using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerJoinGameData
{
	public PlayerData PlayerData { get; set; }

	public PlayerId PlayerId => PlayerData.PlayerId;

	public string Name { get; set; }

	public Guid? PartyId { get; set; }

	public Dictionary<string, List<string>> UsedCosmetics { get; set; }

	[JsonProperty]
	public string IpAddress { get; private set; }

	[JsonProperty]
	public CustomGameJoinType JoinType { get; private set; }

	public PlayerJoinGameData()
	{
	}

	public PlayerJoinGameData(PlayerData playerData, string name, Guid? partyId, Dictionary<string, List<string>> usedCosmetics, string ipAddress, CustomGameJoinType joinType)
	{
		PlayerData = playerData;
		Name = name;
		PartyId = partyId;
		UsedCosmetics = usedCosmetics;
		IpAddress = ipAddress;
		JoinType = joinType;
	}

	public override string ToString()
	{
		return $"Player Join Game Data: {PlayerId}, name={Name}, party={PartyId}, cosmetics={UsedCosmetics.Count}, ip={IpAddress}, joinType={JoinType}";
	}
}
