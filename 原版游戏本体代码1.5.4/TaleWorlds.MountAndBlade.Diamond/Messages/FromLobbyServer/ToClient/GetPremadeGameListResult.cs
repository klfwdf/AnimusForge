using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetPremadeGameListResult : FunctionResult
{
	[JsonProperty]
	public PremadeGameList GameList { get; private set; }

	public GetPremadeGameListResult()
	{
	}

	public GetPremadeGameListResult(PremadeGameList gameList)
	{
		GameList = gameList;
	}
}
