using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetClanHomeInfoResult : FunctionResult
{
	[JsonProperty]
	public ClanHomeInfo ClanHomeInfo { get; private set; }

	public GetClanHomeInfoResult()
	{
	}

	public GetClanHomeInfoResult(ClanHomeInfo clanHomeInfo)
	{
		ClanHomeInfo = clanHomeInfo;
	}
}
