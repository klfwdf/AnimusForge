using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetOtherPlayersStateMessageResult : FunctionResult
{
	[JsonProperty]
	public List<(PlayerId, AnotherPlayerData)> States { get; private set; }

	public GetOtherPlayersStateMessageResult()
	{
	}

	public GetOtherPlayersStateMessageResult(List<(PlayerId, AnotherPlayerData)> states)
	{
		States = states;
	}
}
