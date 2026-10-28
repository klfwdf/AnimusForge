using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetAnotherPlayerStateMessageResult : FunctionResult
{
	[JsonProperty]
	public AnotherPlayerData AnotherPlayerData { get; private set; }

	public GetAnotherPlayerStateMessageResult()
	{
	}

	public GetAnotherPlayerStateMessageResult(AnotherPlayerState anotherPlayerState, int anotherPlayerExperience)
	{
		AnotherPlayerData = new AnotherPlayerData(anotherPlayerState, anotherPlayerExperience);
	}
}
