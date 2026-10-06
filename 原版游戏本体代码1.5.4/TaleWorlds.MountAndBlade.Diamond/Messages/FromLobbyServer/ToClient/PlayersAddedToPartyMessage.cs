using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class PlayersAddedToPartyMessage : Message
{
	[JsonProperty]
	public List<(PlayerId PlayerId, string PlayerName, bool IsPartyLeader)> Players { get; private set; }

	[JsonProperty]
	public List<(PlayerId PlayerId, string PlayerName)> InvitedPlayers { get; private set; }

	public PlayersAddedToPartyMessage()
	{
		Players = new List<(PlayerId, string, bool)>();
		InvitedPlayers = new List<(PlayerId, string)>();
	}

	public PlayersAddedToPartyMessage(PlayerId playerId, string playerName, bool isPartyLeader)
		: this()
	{
		AddPlayer(playerId, playerName, isPartyLeader);
	}

	public void AddPlayer(PlayerId playerId, string playerName, bool isPartyLeader)
	{
		Players.Add((playerId, playerName, isPartyLeader));
	}

	public void AddInvitedPlayer(PlayerId playerId, string playerName)
	{
		InvitedPlayers.Add((playerId, playerName));
	}
}
