using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetAnotherPlayerDataMessageResult : FunctionResult
{
	[JsonProperty]
	public PlayerData AnotherPlayerData { get; private set; }

	public GetAnotherPlayerDataMessageResult()
	{
	}

	public GetAnotherPlayerDataMessageResult(PlayerData playerData)
	{
		AnotherPlayerData = playerData;
	}
}
