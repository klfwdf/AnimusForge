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
	public bool IsAdmin { get; private set; }

	public PlayerJoinGameData()
	{
	}

	public PlayerJoinGameData(PlayerData playerData, string name, Guid? partyId, Dictionary<string, List<string>> usedCosmetics, string ipAddress, bool isAdmin)
	{
		PlayerData = playerData;
		Name = name;
		PartyId = partyId;
		UsedCosmetics = usedCosmetics;
		IpAddress = ipAddress;
		IsAdmin = isAdmin;
	}

	public override string ToString()
	{
		return $"Player Join Game Data: {PlayerId}, name={Name}, party={PartyId}, cosmetics={UsedCosmetics.Count}, ip={IpAddress}, isAdmin={IsAdmin}";
	}
}
