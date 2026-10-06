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
	public CustomGameJoinType JoinType { get; private set; }

	public JoinCustomGameResultMessage()
	{
	}

	private JoinCustomGameResultMessage(JoinGameData joinGameData, bool success, CustomGameJoinResponse response, string matchId, CustomGameJoinType joinType)
	{
		JoinGameData = joinGameData;
		Success = success;
		Response = response;
		MatchId = matchId;
		JoinType = joinType;
	}

	public static JoinCustomGameResultMessage CreateSuccess(JoinGameData joinGameData, string matchId, CustomGameJoinType joinType)
	{
		return new JoinCustomGameResultMessage(joinGameData, success: true, CustomGameJoinResponse.Success, matchId, joinType);
	}

	public static JoinCustomGameResultMessage CreateFailed(CustomGameJoinResponse response)
	{
		return new JoinCustomGameResultMessage(null, success: false, response, null, CustomGameJoinType.Player);
	}
}
