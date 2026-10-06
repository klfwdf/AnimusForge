using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class InvitationToClanMessage : Message
{
	[JsonProperty]
	public PlayerId InviterId { get; private set; }

	[JsonProperty]
	public string ClanName { get; private set; }

	[JsonProperty]
	public string ClanTag { get; private set; }

	[JsonProperty]
	public int ClanPlayerCount { get; private set; }

	public InvitationToClanMessage()
	{
	}

	public InvitationToClanMessage(PlayerId inviterId, string clanName, string clanTag, int clanPlayerCount)
	{
		InviterId = inviterId;
		ClanName = clanName;
		ClanTag = clanTag;
		ClanPlayerCount = clanPlayerCount;
	}
}
