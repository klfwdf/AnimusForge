using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetPlayerClanInfoResult : FunctionResult
{
	[JsonProperty]
	public ClanInfo ClanInfo { get; private set; }

	public GetPlayerClanInfoResult()
	{
	}

	public GetPlayerClanInfoResult(ClanInfo clanInfo)
	{
		ClanInfo = clanInfo;
	}
}
