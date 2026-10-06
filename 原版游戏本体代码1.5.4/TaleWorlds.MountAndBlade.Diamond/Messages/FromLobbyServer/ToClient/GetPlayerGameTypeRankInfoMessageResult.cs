using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Ranked;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetPlayerGameTypeRankInfoMessageResult : FunctionResult
{
	[JsonProperty]
	public GameTypeRankInfo[] GameTypeRankInfo { get; private set; }

	public GetPlayerGameTypeRankInfoMessageResult()
	{
	}

	public GetPlayerGameTypeRankInfoMessageResult(GameTypeRankInfo[] gameTypeRankInfo)
	{
		GameTypeRankInfo = gameTypeRankInfo;
	}
}
