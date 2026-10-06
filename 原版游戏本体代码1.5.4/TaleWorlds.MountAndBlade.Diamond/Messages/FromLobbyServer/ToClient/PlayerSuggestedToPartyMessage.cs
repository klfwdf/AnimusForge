using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class PlayerSuggestedToPartyMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public string PlayerName { get; private set; }

	[JsonProperty]
	public PlayerId SuggestingPlayerId { get; private set; }

	[JsonProperty]
	public string SuggestingPlayerName { get; private set; }

	public PlayerSuggestedToPartyMessage()
	{
	}

	public PlayerSuggestedToPartyMessage(PlayerId playerId, string playerName, PlayerId suggestingPlayerId, string suggestingPlayerName)
	{
		PlayerId = playerId;
		PlayerName = playerName;
		SuggestingPlayerId = suggestingPlayerId;
		SuggestingPlayerName = suggestingPlayerName;
	}
}
