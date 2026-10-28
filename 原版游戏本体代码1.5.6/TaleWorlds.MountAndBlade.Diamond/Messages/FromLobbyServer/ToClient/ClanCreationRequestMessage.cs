using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class ClanCreationRequestMessage : Message
{
	[JsonProperty]
	public string CreatorPlayerName { get; private set; }

	[JsonProperty]
	public PlayerId CreatorPlayerId { get; private set; }

	[JsonProperty]
	public string ClanName { get; private set; }

	[JsonProperty]
	public string ClanTag { get; private set; }

	public ClanCreationRequestMessage()
	{
	}

	public ClanCreationRequestMessage(PlayerId creatorPlayerId, string creatorPlayerName, string clanName, string clanTag)
	{
		CreatorPlayerId = creatorPlayerId;
		CreatorPlayerName = creatorPlayerName;
		ClanName = clanName;
		ClanTag = clanTag;
	}
}
