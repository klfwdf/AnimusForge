using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class CreateClanMessage : Message
{
	[JsonProperty]
	public string ClanName { get; private set; }

	[JsonProperty]
	public string ClanTag { get; private set; }

	[JsonProperty]
	public string ClanFaction { get; private set; }

	[JsonProperty]
	public string ClanSigil { get; private set; }

	public CreateClanMessage()
	{
	}

	public CreateClanMessage(string clanName, string clanTag, string clanFaction, string clanSigil)
	{
		ClanName = clanName;
		ClanTag = clanTag;
		ClanFaction = clanFaction;
		ClanSigil = clanSigil;
	}
}
