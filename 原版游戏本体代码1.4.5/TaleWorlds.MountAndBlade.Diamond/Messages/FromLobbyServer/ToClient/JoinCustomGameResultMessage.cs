using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class JoinCustomGameResultMessage : Message
{
	[JsonProperty]
	public JoinGameData JoinGameData { get; private set; }

	[JsonProperty]
	public bool Success { get; private set; }

	[JsonProperty]
	public CustomGameJoinResponse Response { get; private set; }

	[JsonProperty]
	public string MatchId { get; private set; }

	[JsonProperty]
	public bool IsAdmin { get; private set; }

	public JoinCustomGameResultMessage()
	{
	}

	private JoinCustomGameResultMessage(JoinGameData joinGameData, bool success, CustomGameJoinResponse response, string matchId, bool isAdmin)
	{
		JoinGameData = joinGameData;
		Success = success;
		Response = response;
		MatchId = matchId;
		IsAdmin = isAdmin;
	}

	public static JoinCustomGameResultMessage CreateSuccess(JoinGameData joinGameData, string matchId, bool isAdmin)
	{
		return new JoinCustomGameResultMessage(joinGameData, success: true, CustomGameJoinResponse.Success, matchId, isAdmin);
	}

	public static JoinCustomGameResultMessage CreateFailed(CustomGameJoinResponse response)
	{
		return new JoinCustomGameResultMessage(null, success: false, response, null, isAdmin: false);
	}
}
