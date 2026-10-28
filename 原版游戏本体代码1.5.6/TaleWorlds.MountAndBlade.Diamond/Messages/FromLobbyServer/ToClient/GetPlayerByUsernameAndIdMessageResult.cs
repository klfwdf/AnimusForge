using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetPlayerByUsernameAndIdMessageResult : FunctionResult
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	public GetPlayerByUsernameAndIdMessageResult()
	{
	}

	public GetPlayerByUsernameAndIdMessageResult(PlayerId playerId)
	{
		PlayerId = playerId;
	}
}
